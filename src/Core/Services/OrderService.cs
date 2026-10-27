using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

public sealed class OrderService(IOrderStore store)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Product Add(string name, decimal price)
    {
        var product = Product.Create("P-" + Guid.NewGuid().ToString("N")[..8], name, price); // інваріанти перевіряє Product.Create
        _store.Add(product);
        return product;
    }

    // Product незмінний (тиждень 4: лише get), тож "змінити ціну" це створити новий Product
    // з тим самим id через фабрику (інваріант "ціна > 0" спрацює знову) і замінити старий.
    public Product ChangePrice(string id, decimal newPrice)
    {
        var current = GetOrThrow(id);
        var updated = Product.Create(current.Id, current.Name, newPrice);
        _store.Update(updated);
        return updated;
    }

    public void Remove(string id)
    {
        if (!_store.Remove(id))
            throw new InvalidOperationException($"Немає товару з id={id}.");
    }

    public IReadOnlyList<Product> All() => _store.List();

    public Product? Find(string id) => _store.GetById(id);

    // Додаткове 2: пошук за довільною умовою. В інтерфейс сховища НЕ додаємо:
    // фільтрація це логіка сервісу, а контракт лишається з 5 методів.
    public IReadOnlyList<Product> Search(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    private Product GetOrThrow(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає товару з id={id}.");
}
