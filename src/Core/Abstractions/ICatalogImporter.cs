using Core.Domain;

namespace Core.Abstractions;

public interface ICatalogImporter
{
    // Читає файл повністю й повертає товари. Усе або нічого: поганий запис дає DomainException.
    Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default);

    // Додаткове 1: потоковий варіант. Товари віддаються по одному, файл у пам'ять не завантажується.
    IAsyncEnumerable<Product> StreamAsync(string path, CancellationToken ct = default);
}
