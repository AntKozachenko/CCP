using Core.Domain;

namespace Core.Abstractions;

// Варіант "Замовлення": асинхронний імпорт замовлень разом з їхніми рядками.
public interface IOrderImporter
{
    Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default);
}
