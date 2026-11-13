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

    // Лаб. 7: 18 замовлень, 6 клієнтів, 3 статуси. Ціни рядків беруться з каталогу Products().
    // Дані підібрано навмисно:
    //  - C-005 і C-006 мають однакову суму підтверджених замовлень (1886.90): без ThenBy топ-N залежав би від порядку;
    //  - є замовлення дорожчі за 5000 у різних статусах (у т. ч. скасоване);
    //  - P-006 і P-016 є лише в чернетках, тому "непродані" (GroupJoin).
    // shuffleSeed перемішує порядок у джерелі: доказ, що звіти не залежать від порядку вхідних даних.
    public static IReadOnlyList<Order> Orders(int? shuffleSeed = null)
    {
        var prices = Products().ToDictionary(p => p.Id, p => p.Price);

        Order Make(string id, string customerId, OrderStatus status, params (string ProductId, int Quantity)[] lines)
        {
            var order = Order.Create(id, customerId);
            foreach (var (productId, quantity) in lines)
                order.AddLine(productId, quantity, prices[productId]);

            switch (status)
            {
                case OrderStatus.Confirmed: order.Confirm(); break;
                case OrderStatus.Cancelled: order.Cancel(); break;
            }
            return order;
        }

        List<Order> orders =
        [
            Make("O-001", "C-001", OrderStatus.Confirmed, ("P-001", 1)),
            Make("O-002", "C-001", OrderStatus.Confirmed, ("P-002", 2), ("P-014", 1)),
            Make("O-003", "C-001", OrderStatus.Draft,     ("P-003", 1)),
            Make("O-004", "C-001", OrderStatus.Cancelled, ("P-010", 1)),

            Make("O-005", "C-002", OrderStatus.Confirmed, ("P-003", 2)),
            Make("O-006", "C-002", OrderStatus.Confirmed, ("P-004", 1), ("P-009", 1)),
            Make("O-007", "C-002", OrderStatus.Confirmed, ("P-005", 3)),
            Make("O-008", "C-002", OrderStatus.Draft,     ("P-006", 10)),

            Make("O-009", "C-003", OrderStatus.Confirmed, ("P-011", 1), ("P-005", 1)),
            Make("O-010", "C-003", OrderStatus.Confirmed, ("P-008", 1)),
            Make("O-011", "C-003", OrderStatus.Cancelled, ("P-001", 1)),

            Make("O-012", "C-004", OrderStatus.Confirmed, ("P-010", 2)),
            Make("O-013", "C-004", OrderStatus.Confirmed, ("P-015", 1), ("P-009", 1)),
            Make("O-014", "C-004", OrderStatus.Draft,     ("P-016", 4)),

            Make("O-015", "C-005", OrderStatus.Confirmed, ("P-012", 1), ("P-013", 2)),
            Make("O-016", "C-005", OrderStatus.Confirmed, ("P-007", 1)),

            Make("O-017", "C-006", OrderStatus.Confirmed, ("P-012", 1), ("P-013", 2), ("P-007", 1)),
            Make("O-018", "C-006", OrderStatus.Cancelled, ("P-002", 1)),
        ];

        if (shuffleSeed is int seed)
        {
            var rnd = new Random(seed);
            orders = orders.OrderBy(_ => rnd.Next()).ToList();
        }

        return orders;
    }

    // Лаб. 7, додаткове 4: детермінований генератор великого набору для вимірювання часу.
    // Фіксований seed: щоразу однакові дані, тож цифри двох запусків можна порівнювати.
    public static IReadOnlyList<Order> GenerateOrders(int count, int seed = 42)
    {
        var rnd = new Random(seed);
        var catalog = Products();
        var orders = new List<Order>(count);

        for (int i = 1; i <= count; i++)
        {
            var order = Order.Create($"G-{i:D6}", $"C-{rnd.Next(1, 501):D4}");

            int lines = rnd.Next(1, 5);
            for (int j = 0; j < lines; j++)
            {
                var product = catalog[rnd.Next(catalog.Count)];
                order.AddLine(product.Id, rnd.Next(1, 6), product.Price);
            }

            int roll = rnd.Next(100);
            if (roll < 70) order.Confirm();        // 70% підтверджені
            else if (roll < 80) order.Cancel();    // 10% скасовані, решта чернетки

            orders.Add(order);
        }

        return orders;
    }
}
