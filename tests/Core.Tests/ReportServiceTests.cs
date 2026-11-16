using Core.Reports;

namespace Core.Tests;

public class ReportServiceTests
{
    // Суми замовлень у TestData: 500, 100, 100, 10000 (чернетка), 9000 (скасоване). Поріг строгий (>).
    [Theory] // один тест на кількох наборах даних
    [InlineData(400, 3)]
    [InlineData(100, 3)]
    [InlineData(99, 5)]
    [InlineData(100_000, 0)]
    public void ExpensiveOrders_CountsOnlyAboveThreshold(int threshold, int expected) =>
        Assert.Equal(expected, ReportService.ExpensiveOrders(TestData.Orders(), threshold).Count);

    [Fact] // детермінований топ-N: рівні суми розрізняє CustomerId
    public void TopCustomers_IsDeterministic_OnTies()
    {
        var forward = ReportService.TopCustomers(TestData.Orders(), 3).Select(c => c.CustomerId);
        var reversed = ReportService.TopCustomers(TestData.Orders().Reverse().ToList(), 3).Select(c => c.CustomerId);

        Assert.Equal(new[] { "C-A", "C-B", "C-C" }, forward);
        Assert.Equal(forward, reversed);
    }

    [Fact]
    public void Totals_OnEmptySource_ReturnsZeros()
    {
        Assert.Equal((0, 0m, 0m, 0m), ReportService.Totals([]));
    }
}
