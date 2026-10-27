using Core.Dto;

namespace Core.Domain;

public sealed class Order
{
    // DEMO4-1 приватне поле, get без set, приватний конструктор
    private readonly List<OrderLine> _lines = [];

    public string Id { get; }
    public string CustomerId { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(l => l.LineTotal);

    private Order(string id, string customerId)
    {
        Id = id;
        CustomerId = customerId;
    }

    public static Order Create(string id, string customerId)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор замовлення обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Ідентифікатор клієнта не може бути порожнім", nameof(customerId));

        return new Order(id.Trim(), customerId.Trim());
    }
    // DEMO4-4 спершу перевірки, потім зміна стану
    public void AddLine(string productId, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException(
                $"Замовлення {Id} має статус {Status}: рядки додавати не можна");

        _lines.Add(OrderLine.Create(productId, quantity, unitPrice)); // Create кидає ДО Add
    }

    public void Confirm()
    {
        if (Status == OrderStatus.Draft && _lines.Count == 0)
            throw new InvalidOperationException($"Замовлення {Id} порожнє: підтвердити неможливо");

        MoveTo(OrderStatus.Confirmed);
    }

    public void Cancel() => MoveTo(OrderStatus.Cancelled);

    // DEMO4 додаткове enum-статуси і дозволені переходи
    private void MoveTo(OrderStatus next)
    {
        bool allowed = (Status, next) switch
        {
            (OrderStatus.Draft, OrderStatus.Confirmed) => true,
            (OrderStatus.Draft, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException(
                $"Замовлення {Id}: перехід {Status} → {next} неможливий");

        Status = next;
    }
    // DEMO4-5 мапінг DTO <-> сутність
    public OrderDto ToDto() => new(Id, CustomerId, Status.ToString(),
        _lines.Select(l => new OrderLineDto(Id, l.ProductId, l.Quantity, l.UnitPrice)).ToList());

    // Відновлення проходить через ТІ САМІ публічні методи, тож інваріанти діють і на файл.
    public static Order FromDto(OrderDto dto)
    {
        if (!Enum.TryParse(dto.Status, ignoreCase: true, out OrderStatus status) || !Enum.IsDefined(status))
            throw new ArgumentException($"Невідомий статус '{dto.Status}'", nameof(dto));

        Order order = Create(dto.Id, dto.CustomerId);

        foreach (OrderLineDto l in dto.Lines)
        {
            if (l.OrderId.Trim() != order.Id)
                throw new ArgumentException(
                    $"Рядок належить замовленню {l.OrderId}, а не {order.Id}", nameof(dto));
            order.AddLine(l.ProductId, l.Quantity, l.UnitPrice);
        }

        switch (status)
        {
            case OrderStatus.Confirmed: order.Confirm(); break;
            case OrderStatus.Cancelled: order.Cancel(); break;
        }

        return order;
    }

    public override string ToString() => FormattableString.Invariant(
        $"{Id} [{Status}] клієнт {CustomerId}: рядків {_lines.Count}, разом {Total:F2}");
}
