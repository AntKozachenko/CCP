using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

// ===== Composition root: єдине місце з конкретними класами сховищ =====
bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

IOrderStore store = useFile
    ? new FileOrderStore(dataPath)
    : new InMemoryOrderStore(SampleData.Products());

if (args.Contains("--cache"))
    store = new CachingOrderStore(store);   // декоратор (додаткове 1)

// Варіант із фабрикою (додаткове 3): розкоментувати рядок нижче й прибрати блок вище.
//IOrderStore store = Cli.OrderFactory.Create(args);

var service = new OrderService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");
Console.WriteLine($"Товарів на старті: {service.All().Count}");

// ===== Сценарій успіху: додати, змінити, показати, знайти =====
Console.WriteLine("\n--- Додавання й зміна ціни ---");
var created = service.Add("Кабель UTP cat6 (50 м)", 650.00m);
Console.WriteLine($" додано: {created.Id} {created.Name} {created.Price:F2}");
var changed = service.ChangePrice(created.Id, 599.00m);
Console.WriteLine($" нова ціна: {changed.Id} {changed.Price:F2}");

Console.WriteLine("\n--- Усі товари ---");
foreach (var p in service.All())
    Console.WriteLine($" {p.Id,-11} {p.Name,-26} {p.Price,10:F2}");

Console.WriteLine("\n--- Пошук за id ---");
Console.WriteLine(service.Find(created.Id) is { } found ? $" {found.Id} {found.Name}" : " не знайдено");
Console.WriteLine(service.Find("P-NOPE") is null ? " P-NOPE: не знайдено (null, не виняток)" : " знайдено");

// ===== Додаткове 2: пошук за предикатом =====
Console.WriteLine("\n--- Товари дорожчі за 3000 ---");
foreach (var p in service.Search(p => p.Price > 3000m))
    Console.WriteLine($" {p.Id,-11} {p.Name,-26} {p.Price,10:F2}");

// ===== Видалення: тимчасовий товар додаємо й одразу прибираємо =====
Console.WriteLine("\n--- Видалення ---");
var temp = service.Add("Тимчасовий товар", 1m);
service.Remove(temp.Id);
Console.WriteLine($" {temp.Id} видалено, пошук тепер: {(service.Find(temp.Id) is null ? "null" : "знайдено")}");

// ===== Сценарій відмови: помилки з людськими повідомленнями =====
Console.WriteLine("\n--- Відмови ---");
TryDo("дубль id у сховищі", () => store.Add(Product.Create(created.Id, "Двійник", 10m)));
TryDo("ChangePrice неіснуючого id", () => service.ChangePrice("P-NOPE", 10m));
TryDo("Remove неіснуючого id", () => service.Remove("P-NOPE"));
TryDo("ціна нуль (інваріант Product)", () => service.Add("Безкоштовний", 0m));
TryDo("порожня назва (інваріант Product)", () => service.Add("  ", 10m));

// Кеш видно лише в режимі --cache
if (store is CachingOrderStore cache)
{
    service.All(); service.All(); service.Find(created.Id); service.Find(created.Id);
    Console.WriteLine($"\nКеш: влучань {cache.Hits}, промахів {cache.Misses}");
}

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
