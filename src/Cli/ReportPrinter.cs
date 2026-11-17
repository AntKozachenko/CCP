using Core.Reports;

namespace Cli;

// Лише вивід: форматування й вирівнювання. Жодного запиту тут немає, усе рахує ReportService.
// {x,-8} вирівнює вліво, {x,12:N2} вправо з розділювачами тисяч і двома знаками після коми.
public static class ReportPrinter
{
    // Orders:Currency з конфігурації; виставляється в Program.cs
    public static string Currency { get; set; } = "UAH";

    public static void Print(string title, IReadOnlyList<OrderSummary> rows)
    {
        Header(title, rows.Count);
        foreach (var r in rows)
            Console.WriteLine($" {r.OrderId,-8} {r.CustomerId,-8} {r.Status,-10} рядків: {r.Lines,2} {r.Total,12:N2}");
    }

    public static void Print(string title, IReadOnlyList<CustomerSummary> rows)
    {
        Header(title, rows.Count);
        foreach (var r in rows)
            Console.WriteLine($" {r.CustomerId,-8} замовлень: {r.Orders,3}  сума: {r.TotalAmount,12:N2}");
    }

    public static void Print(string title, IReadOnlyList<GroupSummary> rows)
    {
        Header(title, rows.Count);
        foreach (var g in rows)
            Console.WriteLine($" {g.Key,-10} замовлень: {g.Count,3}  сумарно: {g.Total,12:N2}");
    }

    public static void Print(string title, IReadOnlyList<ProductSales> rows)
    {
        Header(title, rows.Count);
        foreach (var r in rows)
            Console.WriteLine($" {r.ProductId,-8} {r.Name,-24} шт: {r.Quantity,3}  виторг: {r.Revenue,12:N2}");
    }

    public static void Print(string title, (int Orders, decimal Total, decimal Average, decimal Max) t)
    {
        Console.WriteLine(title);
        Console.WriteLine($" замовлень {t.Orders}, всього {t.Total:N2}, середній чек {t.Average:N2}, макс {t.Max:N2} ({Currency})");
    }

    public static void Print(DeferredDemoResult d)
    {
        Console.WriteLine("Відкладене виконання (запит: замовлення дорожчі за поріг):");
        Console.WriteLine($" Дорожчих до змін:        {d.Before}");
        Console.WriteLine($" Дорожчих після змін:     {d.After}   ← той самий запит, але джерело встигло змінитись");
        Console.WriteLine($" У знімку лишилось:       {d.SnapshotAfterClear}   ← ToList() зафіксував результат до Clear()");
        Console.WriteLine($" Сам запит після Clear(): {d.LiveAfterClear}   ← запит живий, джерело порожнє");
        Console.WriteLine($" Викликів предиката при двох Count():     {d.PredicateCallsTwoPasses}  (джерело {d.SourceSize} × 2)");
        Console.WriteLine($" Викликів предиката після ToList():       {d.PredicateCallsMaterialized}  (джерело {d.SourceSize} × 1)");
    }

    public static void Print(string title, IReadOnlyList<BenchResult> rows)
    {
        Header(title, rows.Count);
        foreach (var r in rows)
            Console.WriteLine($" {r.Variant,-52} {r.MedianMs,8:F2} мс  (елементів: {r.ResultCount})");
    }

    // Порожній звіт це нормальний результат, а не помилка: показуємо це явно.
    private static void Header(string title, int count)
    {
        Console.WriteLine(title);
        if (count == 0)
            Console.WriteLine(" (немає даних)");
    }
}
