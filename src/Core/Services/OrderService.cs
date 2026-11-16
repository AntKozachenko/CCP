using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class OrderService(IOrderStore store, IEnumerable<INotificationSink>? sinks = null)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink[] _sinks = sinks?.ToArray() ?? [];

    // event, а не public-поле делегата: ззовні можна лише += і -=, викликати може тільки цей клас.
    // Поки підписників немає, Changed дорівнює null (тому нижче ?.Invoke).
    public event EventHandler<CatalogChangedEventArgs>? Changed;

    // Єдине місце, де сервіс повідомляє про зміни. Викликається ЛИШЕ після успішної зміни стану:
    // якщо Product.Create чи сховище кинули виняток, до цього рядка виконання не доходить.
    private void OnChanged(string id, CatalogChangeKind kind, decimal? oldPrice, decimal? newPrice)
    {
        var args = new CatalogChangedEventArgs(id, kind, oldPrice, newPrice);

        Changed?.Invoke(this, args);           // підписники події, по черзі в порядку += 

        foreach (INotificationSink sink in _sinks)   // додаткове 1: приймачі, передані через конструктор
            sink.Notify(args);
    }

    public Product Add(string name, decimal price)
    {
        var product = Product.Create("P-" + Guid.NewGuid().ToString("N")[..8], name, price); // інваріанти перевіряє Product.Create
        _store.Add(product);
        OnChanged(product.Id, CatalogChangeKind.Added, null, product.Price);   // після зміни, не до
        return product;
    }

    // Product незмінний (тиждень 4: лише get), тож "змінити ціну" це створити новий Product
    // з тим самим id через фабрику (інваріант "ціна > 0" спрацює знову) і замінити старий.
    public Product ChangePrice(string id, decimal newPrice)
    {
        var current = GetOrThrow(id);
        var updated = Product.Create(current.Id, current.Name, newPrice);   // нульова ціна кине виняток тут, події не буде
        _store.Update(updated);
        OnChanged(updated.Id, CatalogChangeKind.PriceChanged, current.Price, updated.Price);
        return updated;
    }

    public void Remove(string id)
    {
        var existing = GetOrThrow(id);   // потрібен для OldPrice у події; повідомлення помилки те саме
        if (!_store.Remove(id))
            throw new InvalidOperationException($"Немає товару з id={id}.");
        OnChanged(existing.Id, CatalogChangeKind.Removed, existing.Price, null);
    }

    public IReadOnlyList<Product> All() => _store.List();

    public Product? Find(string id) => _store.GetById(id);

    // Func, а не подія: тут викликачу потрібна ВІДПОВІДЬ (список), а не сповіщення "щось сталося".
    // Умову задає той, хто викликає: service.Search(p => p.Price < 100m).
    // В інтерфейс сховища не додаємо: фільтрація це логіка сервісу, контракт лишається з 5 методів.
    public IReadOnlyList<Product> Search(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    // ===== Лаб. 8: асинхронний імпорт =====

    // Імпортер приходить параметром (ручний DI), токен іде далі в LoadAsync і перевіряється в циклі.
    // Подія піднімається для кожного ДОДАНОГО запису. Імпорт ідемпотентний: уже наявні id пропускаються.
    // Скасування не відкочує вже додані записи: вони лишаються в сховищі разом із подіями.
    // progress (додаткове 3) отримує відсотки 0..100 лише тоді, коли відсоток змінився.
    // Сховище лишається синхронним: працює в пам'яті, а файлове сховище тут не є вузьким місцем.
    public async Task<int> ImportAsync(ICatalogImporter importer, string path,
        CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ct.ThrowIfCancellationRequested();

        var items = await importer.LoadAsync(path, ct);

        var added = 0;
        var lastPercent = -1;
        for (var i = 0; i < items.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var item = items[i];
            if (_store.GetById(item.Id) is null)
            {
                _store.Add(item);
                OnChanged(item.Id, CatalogChangeKind.Added, null, item.Price);
                added++;
            }

            var percent = (i + 1) * 100 / items.Count;
            if (percent != lastPercent)
            {
                lastPercent = percent;
                progress?.Report(percent);
            }
        }
        return added;
    }

    // Додаткове 2: зв'язані токени. Скасувати може і користувач (ct), і таймер (timeoutCts).
    // Таймаут відрізняємо від скасування користувачем: токен користувача не скасований, а таймерний скасований.
    public async Task<int> ImportWithTimeoutAsync(ICatalogImporter importer, string path, TimeSpan timeout,
        CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        try
        {
            return await ImportAsync(importer, path, linked.Token, progress);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Імпорт не вкластився в {timeout.TotalMilliseconds:F0} мс.", ex);
        }
    }

    // Додаткове 1: потоковий імпорт через await foreach. Файл у пам'ять не читається цілком.
    // Не атомарний: записи до першого поганого вже додані, а сам поганий дає DomainException з номером.
    public async Task<int> ImportStreamAsync(ICatalogImporter importer, string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var added = 0;
        await foreach (var item in importer.StreamAsync(path, ct).WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            if (_store.GetById(item.Id) is not null) continue;
            _store.Add(item);
            OnChanged(item.Id, CatalogChangeKind.Added, null, item.Price);
            added++;
        }
        return added;
    }

    private Product GetOrThrow(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає товару з id={id}.");
}
