using System.Diagnostics;
using Core.Domain;

namespace Core.Reports;

public sealed record BenchResult(string Variant, double MedianMs, int ResultCount);

// Додаткове 4: вимір Stopwatch. Порівнюємо ToList у середині ланцюжка і без нього,
// а також запит, що перебирається двічі, проти матеріалізованого одного разу.
// Щоб цифри не були шумом: два розігрівальні прогони (JIT), перед кожним виміром збирання сміття,
// у підсумку МЕДІАНА з repeats прогонів (на відміну від середнього, один "поганий" прогін її не псує).
// Запускайте у Release: dotnet run -c Release --project src/Cli -- report-bench
public static class ReportBenchmark
{
    public static IReadOnlyList<BenchResult> Run(IReadOnlyList<Order> orders, int repeats = 9)
    {
        var variants = new (string Name, Func<int> Body)[]
        {
            ("Топ-5 клієнтів: без проміжного ToList", () => Streaming(orders).Count),
            ("Топ-5 клієнтів: ToList після Where",    () => ToListAfterWhere(orders).Count),
            ("Топ-5 клієнтів: ToList після кожного кроку", () => ToListEverywhere(orders).Count),
            ("Дорожчі за 5000: запит двічі (Count() + ToList())", () => TwoPasses(orders)),
            ("Дорожчі за 5000: ToList один раз",       () => OnePass(orders)),
        };

        var results = new List<BenchResult>();
        foreach (var (name, body) in variants)
        {
            int count = 0;
            for (int i = 0; i < 2; i++)
                count = body();   // розігрів

            var times = new double[repeats];
            for (int i = 0; i < repeats; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var sw = Stopwatch.StartNew();
                count = body();
                sw.Stop();
                times[i] = sw.Elapsed.TotalMilliseconds;
            }

            Array.Sort(times);
            double median = times[repeats / 2];

            results.Add(new BenchResult(name, median, count));
        }

        return results;
    }

    // Потокове: Where передає елементи в GroupBy по одному, проміжних списків немає.
    private static List<CustomerSummary> Streaming(IEnumerable<Order> orders) =>
        orders.Where(o => o.Status == OrderStatus.Confirmed)
              .GroupBy(o => o.CustomerId, StringComparer.Ordinal)
              .Select(g => new CustomerSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
              .OrderByDescending(c => c.TotalAmount).ThenBy(c => c.CustomerId, StringComparer.Ordinal)
              .Take(5)
              .ToList();

    // ToList у середині: підтверджені замовлення копіюються в окремий список.
    private static List<CustomerSummary> ToListAfterWhere(IEnumerable<Order> orders)
    {
        var confirmed = orders.Where(o => o.Status == OrderStatus.Confirmed).ToList();

        return confirmed.GroupBy(o => o.CustomerId, StringComparer.Ordinal)
                        .Select(g => new CustomerSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
                        .OrderByDescending(c => c.TotalAmount).ThenBy(c => c.CustomerId, StringComparer.Ordinal)
                        .Take(5)
                        .ToList();
    }

    // Антипатерн: матеріалізація на кожному кроці. Результат той самий, зайві копії та виділення пам'яті.
    private static List<CustomerSummary> ToListEverywhere(IEnumerable<Order> orders)
    {
        var confirmed = orders.Where(o => o.Status == OrderStatus.Confirmed).ToList();
        var summaries = confirmed.GroupBy(o => o.CustomerId, StringComparer.Ordinal)
                                 .Select(g => new CustomerSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
                                 .ToList();
        var sorted = summaries.OrderByDescending(c => c.TotalAmount)
                              .ThenBy(c => c.CustomerId, StringComparer.Ordinal)
                              .ToList();
        return sorted.Take(5).ToList();
    }

    // Запит перебирається двічі: Total (сума рядків) для кожного замовлення рахується двічі.
    private static int TwoPasses(IEnumerable<Order> orders)
    {
        var query = orders.Where(o => o.Total > 5000m);
        int count = query.Count();
        var list = query.ToList();
        return count + list.Count;
    }

    private static int OnePass(IEnumerable<Order> orders)
    {
        var list = orders.Where(o => o.Total > 5000m).ToList();
        return list.Count + list.Count;
    }
}
