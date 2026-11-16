using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Варіант "Замовлення": асинхронний імпорт замовлень з рядками. Order.FromDto проганяє дані через ті самі інваріанти.
public sealed class JsonOrderImporter : IOrderImporter
{
    public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await using var stream = JsonFile.OpenRead(path);

        List<OrderDto?> dtos;
        try
        {
            dtos = await JsonSerializer.DeserializeAsync<List<OrderDto?>>(stream, JsonFile.Options, ct)
                   ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");
        }
        catch (JsonException ex)
        {
            throw new StorageException($"Файл {Path.GetFileName(path)} пошкоджено: {JsonFile.OneLine(ex)}", ex);
        }

        var result = new List<Order>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            result.Add(ToOrder(dtos[i], i + 1));
        }
        return result;
    }

    private static Order ToOrder(OrderDto? dto, int number)
    {
        if (dto is null)
            throw new DomainException($"Замовлення {number}: порожній елемент.");
        if (dto.Lines is null || dto.Lines.Any(l => l is null))
            throw new DomainException($"Замовлення {number}: відсутній або пошкоджений перелік рядків.");
        try
        {
            return Order.FromDto(dto);
        }
        catch (Exception ex) when (ex is ArgumentException or DomainException)
        {
            throw new DomainException($"Замовлення {number}: {JsonFile.OneLine(ex)}", ex);
        }
    }
}
