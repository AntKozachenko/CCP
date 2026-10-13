namespace Core.Domain;

public static class OrderPlacement
{
    public const int MaxOpenDrafts = 3;

    public static Order StartOrder(string id, string customerId, IEnumerable<Order> existing)
    {
        Order order = Order.Create(id, customerId); // спершу перевіряємо й нормалізуємо вхід

        int drafts = existing.Count(o => o.CustomerId == order.CustomerId && o.Status == OrderStatus.Draft);
        if (drafts >= MaxOpenDrafts)
            throw new InvalidOperationException(
                $"Клієнт {order.CustomerId} уже має {drafts} незавершених замовлень (ліміт {MaxOpenDrafts})");

        return order;
    }
}
