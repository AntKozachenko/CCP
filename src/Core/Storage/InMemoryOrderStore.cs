using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class InMemoryOrderStore(IEnumerable<Product>? seed = null) : IOrderStore
{
    // Ключі без урахування регістру: "p-001" і "P-001" це той самий товар.
    // Дубль у seed кине виняток одразу при створенні сховища.
    private readonly Dictionary<string, Product> _items =
        (seed ?? []).ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Product> List() => _items.Values.ToList();

    public Product? GetById(string id) => _items.GetValueOrDefault(id);

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Товар з id={item.Id} уже існує.");
        _items.Add(item.Id, item);
    }

    public void Update(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Немає товару з id={item.Id}: оновлювати нічого.");
        _items[item.Id] = item;
    }

    public bool Remove(string id) => _items.Remove(id);
}
