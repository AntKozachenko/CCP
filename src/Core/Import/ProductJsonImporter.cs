using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        List<ProductDto?> raw;
        try
        {
            raw = JsonSerializer.Deserialize<List<ProductDto?>>(File.ReadAllText(path), Options) ?? [];
        }
        catch (JsonException ex)
        {
            errors.Add($"JSON пошкоджено: {ex.Message}");
            return new ImportResult<ProductDto>(items, errors);
        }

        for (int i = 0; i < raw.Count; i++)
        {
            ProductDto? item = raw[i];

            string? problem = item switch
            {
                null => "порожній елемент",
                { Id: null or "" } or { Name: null or "" } => "id або назва порожні",
                { Price: < 0 } => "ціна від'ємна",
                _ => null
            };

            if (problem is null)
                items.Add(item!);
            else
                errors.Add($"елемент {i + 1}: {problem}");
        }

        return new ImportResult<ProductDto>(items, errors);
    }
}
