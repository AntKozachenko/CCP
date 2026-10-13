namespace Core.Domain;

public sealed class OrderLine
{
    public string ProductId { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }
    public decimal LineTotal => Quantity * UnitPrice;

    private OrderLine(string productId, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    internal static OrderLine Create(string productId, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Ідентифікатор товару в рядку обов'язковий", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity,
                "Кількість у рядку має бути більшою за нуль");
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice,
                "Ціна не може бути від'ємною");

        return new OrderLine(productId.Trim(), quantity, unitPrice);
    }

    public override string ToString() =>
        FormattableString.Invariant($"{ProductId} × {Quantity} по {UnitPrice:F2} = {LineTotal:F2}");
}
