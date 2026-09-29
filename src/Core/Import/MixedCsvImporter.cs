using Core.Dto;

namespace Core.Import;

public static class MixedCsvImporter
{
    private const char Separator = ';';

    public static MixedImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var customers = new List<CustomerDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            switch (ParseLine(line))
            {
                case ParseProduct p:
                    products.Add(p.Value);
                    break;
                case ParseCustomer c:
                    customers.Add(c.Value);
                    break;
                case ParseFailed f:
                    errors.Add($"рядок {number}: {f.Reason}");
                    break;
            }
        }

        return new MixedImportResult(products, customers, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["P", "", _, _] or ["P", _, "", _]
                => new ParseFailed("товар: id або назва порожні"),
            ["P", _, _, var price] when !PriceParser.TryParse(price, out _)
                => new ParseFailed($"товар: ціна '{price}' некоректна"),
            ["P", var id, var name, var price]
                => new ParseProduct(new ProductDto(id, name, PriceParser.Parse(price))),
            ["P", ..]
                => new ParseFailed($"товар: очікую 4 колонки, отримав {parts.Length}"),

            ["C", "", ..] or ["C", _, "", ..]
                => new ParseFailed("клієнт: id або ім'я порожні"),
            ["C", _, _, var email, ..] when !email.Contains('@')
                => new ParseFailed($"клієнт: email '{email}' некоректний"),
            ["C", var id, var name, var email]
                => new ParseCustomer(new CustomerDto(id, name, email)),
            ["C", var id, var name, var email, var phone]
                => new ParseCustomer(new CustomerDto(id, name, email, phone == "" ? null : phone)),
            ["C", ..]
                => new ParseFailed($"клієнт: очікую 4-5 колонок, отримав {parts.Length}"),

            [var prefix, ..] => new ParseFailed($"невідомий префікс '{prefix}'"),
            _ => new ParseFailed("порожній рядок")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseProduct(ProductDto Value) : ParseOutcome;
    private sealed record ParseCustomer(CustomerDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}
