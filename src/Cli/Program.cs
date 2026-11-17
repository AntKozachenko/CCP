using Cli;
using Core;
using Core.Configuration;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Core.Services;

// ===== Composition root: єдине місце з конкретними класами =====
var settings = AppSettings.Load(args);          // appsettings → env → user-secrets → CLI
var log = new Log(settings.Options.Logging.MinimumLevel);
log.Debug($"Середовище: {settings.Options.EnvironmentName}, рівень журналу: {settings.Options.Logging.MinimumLevel}");
log.Debug($"Каталог даних: {settings.DataDir}");
ReportPrinter.Currency = settings.Options.Orders.Currency;
log.Debug($"Валюта: {settings.Options.Orders.Currency}");

IOrderStore store = StoreFactory.Create(args, settings);   // --file, --cache
var service = new OrderService(store);

// Джерела звітів. --empty: порожнє джерело (самоперевірка 3), --shuffle: інший порядок у джерелі (самоперевірка 2).
bool empty = args.Contains("--empty");
IReadOnlyList<Order> orders = empty ? [] : SampleData.Orders(args.Contains("--shuffle") ? 7 : null);
IReadOnlyList<Product> catalog = empty ? [] : service.All();

// Прапорці (--file, --cache, --empty ...) не є назвою звіту: беремо перший аргумент без "--".
var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "report-top"; //DoD5

try
{
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
        EventsDemo.Run(store, service, settings);
        break;

    // ----- лабораторна 8 -----
    case "import":            // async-імпорт товарів: [шлях] --stream --timeout=мс --slow=мс --progress
        Environment.ExitCode = await ImportCommand.ProductsAsync(args, service, settings);
        break;

    case "import-orders":     // async-імпорт замовлень з рядками: [шлях]
        Environment.ExitCode = await ImportCommand.OrdersAsync(args, settings);
        break;

    // ----- лабораторна 9 -----
    case "config":            // показує шляхи й наявність секрету (значення не друкується)
        Console.WriteLine($"Середовище : {settings.Options.EnvironmentName}");
        Console.WriteLine($"Файл даних : {settings.DataFile}");
        Console.WriteLine($"Каталог логів: {settings.LogDir}");
        Console.WriteLine($"Валюта     : {settings.Options.Orders.Currency}");
        Console.WriteLine(settings.ApiKeyStatus());
        break;

    case "export":            // уся колекція → JSON (атомарно): [шлях]
        Environment.ExitCode = ExportCommand.Run(args, service, settings);
        Console.WriteLine(settings.ApiKeyStatus());
        break;

    default:
        Console.WriteLine("report-top | report-big | report-groups | report-sum");
        Console.WriteLine("report-products | report-unsold | report-orders | report-groups-q | report-defer | report-bench");
        Console.WriteLine("events | import | import-orders | export | config");
        Console.WriteLine("прапорці: --file --cache --empty --shuffle; import: --stream --timeout=мс --slow=мс --progress");
        break;
}
}
// Зіпсований файл даних чи некоректні дані: зрозуміле повідомлення, а не stack trace.
catch (StorageException ex)
{
    Console.Error.WriteLine($"Помилка сховища: {ex.Message}");
    Environment.ExitCode = 1;
}
catch (DomainException ex)
{
    Console.Error.WriteLine($"Дані відхилено: {ex.Message}");
    Environment.ExitCode = 1;
}
