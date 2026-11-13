using Core.Domain;

namespace Core.Reports;

// Результат демо відкладеного виконання: лише числа, друкує їх Cli.
// Демо лежить у Core, щоб у Program.cs не було жодного Where (самоперевірка 1).
public sealed record DeferredDemoResult(
    int SourceSize,
    int Before,                 // Count() запиту до зміни джерела
    int After,                  // Count() того самого запиту після Add у джерело
    int SnapshotAfterClear,     // ToList() зроблено ДО Clear: число не змінюється
    int LiveAfterClear,         // сам запит після Clear: 0
    int PredicateCallsTwoPasses,    // скільки разів викликався Where-предикат при двох Count()
    int PredicateCallsMaterialized);   // ... при одному ToList і двох читаннях списку

public static class DeferredDemo
{
    public static DeferredDemoResult Run(IEnumerable<Order> orders, decimal threshold)
    {
        var source = orders.ToList();   // локальна копія: її можна змінювати, не чіпаючи вхід

        // 1. Запит ПОБУДОВАНО, але не виконано: жодного Total ще не обчислено.
        var query = source.Where(o => o.Total > threshold);

        int before = query.Count();     // виконання №1 (Count() проходить послідовність)

        var extra = Order.Create("O-TMP", "C-TMP");
        extra.AddLine("P-001", 1, threshold + 1);
        source.Add(extra);              // змінюємо джерело ПІСЛЯ побудови запиту

        int after = query.Count();      // виконання №2: бачить новий елемент, тому число інше

        var snapshot = query.ToList();  // результат зафіксовано: далі це звичайний список
        source.Clear();
        int liveAfterClear = query.Count();   // запит живий, джерело порожнє, отже 0
                                              // snapshot.Count це властивість, а не повторне виконання

        // 2. Скільки разів насправді виконується предикат: ЛІЧИЛЬНИК у замиканні (лаба 6).
        var copy = orders.ToList();
        int calls = 0;
        var counted = copy.Where(o => { calls++; return o.Total > threshold; });

        _ = counted.Count();
        _ = counted.Count();
        int twoPasses = calls;          // 2 × розмір джерела

        calls = 0;
        var list = counted.ToList();    // одне виконання
        _ = list.Count;
        _ = list.Count;                 // властивість: предикат не викликається
        int materialized = calls;       // 1 × розмір джерела

        return new DeferredDemoResult(
            copy.Count, before, after, snapshot.Count, liveAfterClear, twoPasses, materialized);
    }
}
