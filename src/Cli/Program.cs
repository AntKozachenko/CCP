using Core.Dto;
using Core.Import;

bool mixed = args.Contains("--mixed");
string? given = args.FirstOrDefault(a => !a.StartsWith("--"));
string path = given ?? Path.Combine("data", mixed ? "mixed.csv" : "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

if (mixed)
{
    MixedImportResult m = MixedCsvImporter.Load(path);
    Console.WriteLine($"Товарів: {m.Products.Count}, клієнтів: {m.Customers.Count}");
    PrintProducts(m.Products);
    foreach (CustomerDto c in m.Customers.Take(3))
        Console.WriteLine($"  {c.Id,-6} {c.Name,-26} {c.Email}");
    PrintErrors(m.Errors);
    Console.WriteLine(m.Summary());
    return 0;
}

string ext = Path.GetExtension(path).ToLowerInvariant();
ImportResult<ProductDto>? result = ext switch
{
    ".csv" => ProductCsvImporter.Load(path),
    ".json" => ProductJsonImporter.Load(path),
    _ => null
};

if (result is null)
{
    Console.WriteLine($"Непідтримуваний формат '{ext}': очікую .csv або .json");
    return 2;
}

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
PrintProducts(result.Items);
PrintErrors(result.Errors);
Console.WriteLine(result.Summary());
return 0;

static void PrintProducts(IReadOnlyList<ProductDto> items)
{
    foreach (ProductDto p in items.Take(5))
        Console.WriteLine($"  {p.Id,-6} {p.Name,-26} {p.Price,10:F2}");
}

static void PrintErrors(IReadOnlyList<string> errors)
{
    if (errors.Count == 0) return;
    Console.WriteLine($"Пропущено рядків: {errors.Count}");
    foreach (string e in errors)
        Console.WriteLine($"  ! {e}");
}
