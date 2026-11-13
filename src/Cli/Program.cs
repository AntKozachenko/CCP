using Cli;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Core.Services;

// ===== Composition root: єдине місце з конкретними класами =====
IOrderStore store = StoreFactory.Create(args);   // --file, --cache
var service = new OrderService(store);

// Джерела звітів. --empty: порожнє джерело (самоперевірка 3), --shuffle: інший порядок у джерелі (самоперевірка 2).
bool empty = args.Contains("--empty");
IReadOnlyList<Order> orders = empty ? [] : SampleData.Orders(args.Contains("--shuffle") ? 7 : null);
IReadOnlyList<Product> catalog = empty ? [] : service.All();

// Прапорці (--file, --cache, --empty ...) не є назвою звіту: беремо перший аргумент без "--".
var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "report-top"; //DoD5

switch (command)
{
    case "report-top":
        ReportPrinter.Print("Топ-5 клієнтів за сумою підтверджених замовлень:", ReportService.TopCustomers(orders, 5));
        break;

    case "report-big":
        ReportPrinter.Print("Замовлення дорожчі за 5 000:", ReportService.ExpensiveOrders(orders, 5_000m));
        break;

    case "report-groups":
        ReportPrinter.Print("Замовлення за статусом:", ReportService.ByStatus(orders));
        break;

    case "report-sum":
        ReportPrinter.Print("Підсумки по підтверджених замовленнях:", ReportService.Totals(orders));
        break;

    // ----- додаткові завдання -----
    case "report-products":   // Join: рядки замовлень × каталог, топ-5 через власний TopBy
        ReportPrinter.Print("Топ-5 товарів за виторгом (Join):", ReportService.TopProducts(orders, catalog, 5));
        break;

    case "report-unsold":     // GroupJoin: товари без жодного продажу
        ReportPrinter.Print("Товари без підтверджених продажів (GroupJoin):", ReportService.UnsoldProducts(orders, catalog));
        break;

    case "report-orders":     // другий звіт на власному TopBy
        ReportPrinter.Print("Топ-3 найбільші підтверджені замовлення (TopBy):", ReportService.TopOrders(orders, 3));
        break;

    case "report-groups-q":   // той самий звіт у синтаксисі запитів
        var method = ReportService.ByStatus(orders);
        var query = ReportService.ByStatusQuerySyntax(orders);
        ReportPrinter.Print("Замовлення за статусом (синтаксис запитів):", query);
        Console.WriteLine($" Збігається з методним записом: {method.SequenceEqual(query)}");
        break;

    case "report-defer":      // демо відкладеного виконання
        ReportPrinter.Print(DeferredDemo.Run(orders, 5_000m));
        break;

    case "report-bench":      // Stopwatch на 100 000 згенерованих замовлень
        var big = SampleData.GenerateOrders(100_000);
        ReportPrinter.Print($"Час звітів на {big.Count:N0} замовленнях (медіана з 9 прогонів, запускайте з -c Release):", ReportBenchmark.Run(big));
        break;

    // ----- лабораторна 6 -----
    case "events":
        EventsDemo.Run(store, service);
        break;

    default:
        Console.WriteLine("report-top | report-big | report-groups | report-sum");
        Console.WriteLine("report-products | report-unsold | report-orders | report-groups-q | report-defer | report-bench");
        Console.WriteLine("events");
        Console.WriteLine("прапорці: --file --cache --empty --shuffle");
        break;
}
