using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Додаткове 1. Декоратор: той самий контракт IOrderStore, нова поведінка (кеш читання)
// поверх будь-якого іншого сховища. Будь-яка зміна скидає кеш повністю (просто й безпечно).
public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, Product?> _byId = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Product>? _list;

    // Лічильники для демо: видно, що кеш справді працює.
    public int Hits { get; private set; }
    public int Misses { get; private set; }

    public IReadOnlyList<Product> List()
    {
        if (_list is not null) { Hits++; return _list; }
        Misses++;
        return _list = _inner.List();
    }

    public Product? GetById(string id)
    {
        if (_byId.TryGetValue(id, out var cached)) { Hits++; return cached; }
        Misses++;
        return _byId[id] = _inner.GetById(id);   // null теж кешуємо: "немає" це теж відповідь
    }

    public void Add(Product item)
    {
        _inner.Add(item);
        Invalidate();
    }

    public void Update(Product item)
    {
        _inner.Update(item);
        Invalidate();
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        if (removed) Invalidate();
        return removed;
    }

    private void Invalidate()
    {
        _list = null;
        _byId.Clear();
    }
}
