using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonCatalogImporter : ICatalogImporter
{
    public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();   // до відкриття файлу: скасований токен не має впертися у FileNotFound
        await using var stream = JsonFile.OpenRead(path);

        List<ProductDto?> dtos;
        try
        {
            dtos = await JsonSerializer.DeserializeAsync<List<ProductDto?>>(stream, JsonFile.Options, ct)
                   ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");
        }
        catch (JsonException ex)   // технічна помилка: обгортаємо в StorageException, початковий виняток зберігаємо
        {
            throw new StorageException($"Файл {Path.GetFileName(path)} пошкоджено: {JsonFile.OneLine(ex)}", ex);
        }

        var result = new List<Product>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            result.Add(ToProduct(dtos[i], i + 1));
        }
        return result;
    }

    // Додаткове 1. [EnumeratorCancellation]: токен, переданий через WithCancellation(...), потрапляє в ct.
    public async IAsyncEnumerable<Product> StreamAsync(
        string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await using var stream = JsonFile.OpenRead(path);
        await using var items = JsonSerializer
            .DeserializeAsyncEnumerable<ProductDto>(stream, JsonFile.Options, ct)
            .GetAsyncEnumerator(ct);

        for (var number = 1; ; number++)
        {
            // yield return не можна в try з catch, тому перебір вручну: try лише навколо MoveNextAsync
            bool hasNext;
            try { hasNext = await items.MoveNextAsync(); }
            catch (JsonException ex)
            {
                throw new StorageException($"Файл {Path.GetFileName(path)} пошкоджено: {JsonFile.OneLine(ex)}", ex);
            }
            if (!hasNext) yield break;

            ct.ThrowIfCancellationRequested();
            yield return ToProduct(items.Current, number);
        }
    }

    // Некоректний запис це порушення даних, тому ArgumentException перетворюється на DomainException з номером запису.
    private static Product ToProduct(ProductDto? dto, int number)
    {
        if (dto is null)
            throw new DomainException($"Запис {number}: порожній елемент.");
        try
        {
            return Product.FromDto(dto);
        }
        catch (ArgumentException ex)   // ловить і ArgumentOutOfRangeException
        {
            throw new DomainException($"Запис {number}: {JsonFile.OneLine(ex)}", ex);
        }
    }
}
