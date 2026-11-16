using Core.Domain;

namespace Core.Tests;

public class OrderTests
{
    [Fact] // варіант "Замовлення": сума після додавання рядків
    public void Total_AfterAddingLines_IsSumOfLineTotals()
    {
        var order = Order.Create("O-1", "C-1");

        order.AddLine("P-1", 2, 10m);     // 20
        order.AddLine("P-2", 1, 5.5m);    // 5.5

        Assert.Equal(25.5m, order.Total);
        Assert.Equal(2, order.Lines.Count);
    }

    [Fact] // DomainException: повторне підтвердження
    public void Confirm_Twice_ThrowsDomainException_AndKeepsStatus()
    {
        var order = TestData.Make("O-1", "C-1", OrderStatus.Confirmed, ("P-1", 1, 10m));

        var ex = Assert.Throws<DomainException>(order.Confirm);

        Assert.Contains("вже підтверджено", ex.Message);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void AddLine_ToConfirmedOrder_ThrowsDomainException_AndKeepsTotal()
    {
        var order = TestData.Make("O-1", "C-1", OrderStatus.Confirmed, ("P-1", 1, 10m));

        Assert.Throws<DomainException>(() => order.AddLine("P-2", 1, 5m));
        Assert.Equal(10m, order.Total);
    }

    [Fact] // ArgumentException для аргументів, DomainException для правил: різні типи
    public void AddLine_WithZeroQuantity_ThrowsArgumentOutOfRange_NotDomain()
    {
        var order = Order.Create("O-1", "C-1");

        Assert.Throws<ArgumentOutOfRangeException>(() => order.AddLine("P-1", 0, 10m));
    }
}
