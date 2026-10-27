using Core.Domain;

namespace Core;

public static class SampleData
{
    public static IReadOnlyList<Product> Products() =>
    [
        Product.Create("P-001", "Ноутбук 15\"", 24999.00m),
        Product.Create("P-002", "Бездротова миша", 449.50m),
        Product.Create("P-003", "Монітор 27\"", 8999.00m),
        Product.Create("P-004", "Клавіатура механічна", 2199.00m),
        Product.Create("P-005", "Флешка 64GB", 349.00m),
        Product.Create("P-006", "Кабель UTP cat6 (1 м)", 15.00m),
        Product.Create("P-007", "Блок живлення 12V", 289.90m),
        Product.Create("P-008", "Веб-камера Full HD", 1299.00m),
        Product.Create("P-009", "Навушники закриті", 1599.00m),
        Product.Create("P-010", "Роутер Wi-Fi 6", 2499.00m),
        Product.Create("P-011", "Зовнішній SSD 1TB", 3899.00m),
        Product.Create("P-012", "Підставка для ноутбука", 799.00m),
        Product.Create("P-013", "USB-хаб 4 порти", 399.00m),
        Product.Create("P-014", "Килимок для миші", 129.00m),
        Product.Create("P-015", "Мікрофон USB", 1899.00m),
        Product.Create("P-016", "Картридер SD", 99.90m)
    ];
}
