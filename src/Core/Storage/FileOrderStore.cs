using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Той самий контракт, інша механіка: кеш у пам'яті, дозавантаження при першому
// зверненні, запис на диск після кожної зміни. Лаб. 9: шлях надходить із конфігурації,
// формат і атомарний запис (.tmp + File.Move) живуть у JsonCatalogFile.
public sealed class FileOrderStore(string path) : IOrderStore
{
    private readonly Dictionary<string, Product> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;
        if (File.Exists(_path))
        {
            _cache.Clear();
            foreach (var product in JsonCatalogFile.Read(_path))
                _cache[product.Id] = product;
        }
        _loaded = true;
    }

    private void Flush() => JsonCatalogFile.Write(_path, _cache.Values);

    public IReadOnlyList<Product> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public Product? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Товар з id={item.Id} уже існує.");
        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (!_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Немає товару з id={item.Id}: оновлювати нічого.");
        _cache[item.Id] = item;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush();
        return true;
    }
}
