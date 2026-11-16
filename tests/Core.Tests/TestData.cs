using Core.Domain;

namespace Core.Tests;

// Маленький фіксований набір, незалежний від SampleData. Кожен виклик створює нові об'єкти: спільного стану немає.
internal static class TestData
{
    // C-B і C-C мають однакову суму 100: без другого ключа сортування топ-N залежав би від порядку.
    public static IReadOnlyList<Order> Orders() =>
    [
        Make("O-1", "C-A", OrderStatus.Confirmed, ("P-1", 1, 500m)),
        Make("O-2", "C-C", OrderStatus.Confirmed, ("P-1", 2, 50m)),
        Make("O-3", "C-B", OrderStatus.Confirmed, ("P-2", 1, 100m)),
        Make("O-4", "C-A", OrderStatus.Draft,     ("P-3", 10, 1000m)),
        Make("O-5", "C-B", OrderStatus.Cancelled, ("P-3", 1, 9000m)),
    ];

    public static Order Make(string id, string customer, OrderStatus status, params (string Product, int Qty, decimal Price)[] lines)
    {
        var order = Order.Create(id, customer);
        foreach (var (product, qty, price) in lines)
            order.AddLine(product, qty, price);
        if (status == OrderStatus.Confirmed) order.Confirm();
        if (status == OrderStatus.Cancelled) order.Cancel();
        return order;
    }

    public static IReadOnlyList<Product> Products(int count) =>
        Enumerable.Range(1, count).Select(i => Product.Create($"T-{i}", $"Товар {i}", 10m * i)).ToList();
}
