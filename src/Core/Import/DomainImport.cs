using Core.Domain;
using Core.Dto;

namespace Core.Import;

public static class DomainImport
{
    // DEMO4-6a додаткове імпорт -> сутності
    public static ImportResult<Product> ToProducts(ImportResult<ProductDto> import)
    {
        var products = new List<Product>();
        var errors = new List<string>(import.Errors);

        foreach (ProductDto dto in import.Items)
        {
            try
            {
                products.Add(Product.FromDto(dto));
            }
            catch (ArgumentException ex) // ловить і ArgumentOutOfRangeException
            {
                errors.Add($"товар {dto.Id}: {ex.Message.ReplaceLineEndings(" ")}");
            }
        }

        return new ImportResult<Product>(products, errors);
    }
}
