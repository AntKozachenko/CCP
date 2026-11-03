using Cli;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

// ===== Composition root: єдине місце з конкретними класами =====
IOrderStore store = StoreFactory.Create(args);   // --file, --cache
var service = new OrderService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");
Console.WriteLine($"Товарів на старті: {service.All().Count}");

// ===== Лаб. 6: підписка =====
string logPath = RepoPaths.Combine("logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
File.Delete(logPath);   // демо: кожен запуск починає з чистого журналу

// Локальна функція з ім'ям, а не лямбда: саме її ми потім передамо в -=
void OnCatalogChanged(object? sender, CatalogChangedEventArgs e)
{
    string line = ChangeFormatter.Format(e);
    Console.WriteLine(line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}

service.Changed += OnCatalogChanged;   // підписник №1: консоль + файл

// Підписник №2: лямбда із замиканням над змінною changes. _ _ це відкинуті sender і e.
// Друкує ПІСЛЯ першого підписника: так видно, що порядок викликів = порядок підписки.
int changes = 0;
service.Changed += (_, _) =>
{
    changes++;
    Console.WriteLine($"   ↳ лічильник подій = {changes}");
};

// ===== Сценарій успіху: кожна успішна зміна дає рівно одну подію =====
Console.WriteLine("\n--- Додавання й зміна ціни (події) ---");
var created = service.Add("Кабель UTP cat6 (50 м)", 650.00m);
service.ChangePrice(created.Id, 599.00m);
service.ChangePrice(created.Id, 549.00m);
Console.WriteLine($"Подій отримано: {changes}");   // 3

Console.WriteLine("\n--- Усі товари ---");
foreach (var p in service.All())
    Console.WriteLine($" {p.Id,-11} {p.Name,-26} {p.Price,10:F2}");

Console.WriteLine("\n--- Пошук за id ---");
Console.WriteLine(service.Find(created.Id) is { } found ? $" {found.Id} {found.Name}" : " не знайдено");
Console.WriteLine(service.Find("P-NOPE") is null ? " P-NOPE: не знайдено (null, не виняток)" : " знайдено");

// ===== Сценарій відмови: виняток є, події НЕМАЄ =====
Console.WriteLine("\n--- Відмови (подій бути не повинно) ---");
int beforeFailures = changes;
// Дубль id кидає саме сховище; сховище нічого не піднімає: події належать сервісу.
TryDo("дубль id у сховищі", () => store.Add(Product.Create(created.Id, "Двійник", 10m)));
TryDo("ChangePrice неіснуючого id", () => service.ChangePrice("P-NOPE", 10m));
TryDo("Remove неіснуючого id", () => service.Remove("P-NOPE"));
TryDo("ціна нуль (інваріант Product)", () => service.ChangePrice(created.Id, 0m));
TryDo("порожня назва (інваріант Product)", () => service.Add("  ", 10m));
Console.WriteLine($"Подій за час відмов: {changes - beforeFailures}");   // 0

// ===== Відписка: операція виконується, повідомлення для консолі й файлу немає =====
Console.WriteLine("\n--- Після відписки -= ---");
service.Changed -= OnCatalogChanged;
service.ChangePrice(created.Id, 499.00m);   // консоль і файл мовчать; озветься лише лічильник
Console.WriteLine($"Подій отримано: {changes}");   // 4: +1 лише від лямбди
Console.WriteLine($" ціна змінилась попри відписку: {service.Find(created.Id)?.Price:F2}");

// Видалення тимчасового товару: теж без запису в app.log (обробник знято), але лічильник рахує
Console.WriteLine("\n--- Видалення ---");
var temp = service.Add("Тимчасовий товар", 1m);
service.Remove(temp.Id);
Console.WriteLine($" {temp.Id} видалено, пошук тепер: {(service.Find(temp.Id) is null ? "null" : "знайдено")}");

// ===== Func замість події: потрібна відповідь, а не сповіщення =====
Console.WriteLine("\n--- Func: Search(p => ...) ---");
Console.WriteLine(" дорожчі за 3000:");
foreach (var p in service.Search(p => p.Price > 3000m))
    Console.WriteLine($"   {p.Id,-11} {p.Name,-26} {p.Price,10:F2}");
Console.WriteLine(" дешевші за 150:");
foreach (var p in service.Search(p => p.Price < 150m))
    Console.WriteLine($"   {p.Id,-11} {p.Name,-26} {p.Price,10:F2}");

// ===== Додаткове 1: INotificationSink замість події (інший сервіс, щоб не дублювати вивід) =====
Console.WriteLine("\n--- Додаткове 1: приймачі через конструктор ---");
string sinkLogPath = RepoPaths.Combine("logs", "sinks.log");
File.Delete(sinkLogPath);
var sinkService = new OrderService(
    new InMemoryOrderStore(),
    [new ConsoleSink(), new FileSink(sinkLogPath)]);   // ручний DI: приймачів дає composition root
var sinkProduct = sinkService.Add("Тестовий товар", 100m);
sinkService.ChangePrice(sinkProduct.Id, 120m);
sinkService.Remove(sinkProduct.Id);
Console.WriteLine($" sinks.log містить рядків: {File.ReadAllLines(sinkLogPath).Length}");

// ===== Додаткове 2: Action<string> у методі імпорту =====
Console.WriteLine("\n--- Додаткове 2: Action<string> в імпорті ---");
int skipped = 0;
var import = ProductCsvImporter.Load(
    RepoPaths.Combine("data", "sample.csv"),
    msg =>   // викликається імпортером на кожен пропущений рядок
    {
        skipped++;
        Console.WriteLine($" ! {msg}");
    });
Console.WriteLine($" прийнято {import.Items.Count}, повідомлень через Action: {skipped}");
Console.WriteLine($" {import.Summary()}");

// Кеш видно лише в режимі --cache
if (store is CachingOrderStore cache)
{
    service.All(); service.All(); service.Find(created.Id); service.Find(created.Id);
    Console.WriteLine($"\nКеш: влучань {cache.Hits}, промахів {cache.Misses}");
}

// ===== Що потрапило у файл журналу (мають бути рівно 3 рядки, без операцій після відписки) =====
Console.WriteLine("\n--- logs/app.log ---");
foreach (string line in File.ReadAllLines(logPath))
    Console.WriteLine($" {line}");

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($" [{title}] неочікувано пройшло");
    }
    catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
    {
        Console.WriteLine($" [{title}] {ex.GetType().Name}: {ex.Message.ReplaceLineEndings(" ")}");
    }
}
