using Core.Domain;
using Core.Dto;
using Core.Import;

Console.WriteLine("=== Сценарій 1: успіх ===");
Order order = Order.Create("O-001", "C-001");
Console.WriteLine(order);
order.AddLine("P-001", 2, 24999.00m);
order.AddLine("P-002", 1, 449.50m);
foreach (OrderLine l in order.Lines)
    Console.WriteLine($"  {l}");
order.Confirm();
Console.WriteLine(order);

Console.WriteLine();
Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
Order draft = Order.Create("O-002", "C-001");
Order cancelled = Order.Create("O-003", "C-001");
cancelled.Cancel();

TryDo("порожній клієнт", () => Order.Create("O-900", " "));
TryDo("нульова кількість у рядку", () => draft.AddLine("P-003", 0, 100m));
TryDo("від'ємна ціна", () => draft.AddLine("P-003", 1, -5m));
TryDo("рядок у підтверджене замовлення", () => order.AddLine("P-003", 1, 100m));
TryDo("підтвердження порожнього", () => draft.Confirm());
TryDo("повторне підтвердження", () => order.Confirm());
TryDo("підтвердження скасованого", () => cancelled.Confirm());
TryDo("FromDto: підтверджене без рядків", () => Order.FromDto(new OrderDto("O-900", "C-001", "Confirmed", [])));
TryDo("FromDto: невідомий статус", () => Order.FromDto(new OrderDto("O-900", "C-001", "Shipped", [])));

Console.WriteLine();
Console.WriteLine("Після всіх відмов:");
Console.WriteLine($"  {order}");
Console.WriteLine($"  {draft}");

Console.WriteLine();
Console.WriteLine("=== Мапінг ToDto / FromDto ===");
Order restored = Order.FromDto(order.ToDto());
Console.WriteLine($"  {restored}");

Console.WriteLine();
Console.WriteLine("=== Додаткове 1: імпорт → сутності ===");
string csvPath = Path.Combine("data", "sample.csv");
if (File.Exists(csvPath))
{
    ImportResult<ProductDto> csv = ProductCsvImporter.Load(csvPath);
    var withZeroPrice = new ImportResult<ProductDto>(
        [.. csv.Items, new ProductDto("P-999", "Безкоштовний", 0m)], csv.Errors);

    ImportResult<Product> products = DomainImport.ToProducts(withZeroPrice);
    Console.WriteLine($"Сутностей: {products.Items.Count}, відхилено: {products.Errors.Count}");
    foreach (string e in products.Errors)
        Console.WriteLine($"  ! {e}");
}
else
{
    Console.WriteLine($"  пропущено: не знайдено {Path.GetFullPath(csvPath)}");
}

Console.WriteLine();
Console.WriteLine("=== Додаткове 2: ліміт чернеток клієнта ===");
var history = new List<Order>();
for (int i = 1; i <= OrderPlacement.MaxOpenDrafts; i++)
    history.Add(OrderPlacement.StartOrder($"D-{i}", "C-009", history));
Console.WriteLine($"  створено чернеток: {history.Count}");
TryDo("четверта чернетка", () => OrderPlacement.StartOrder("D-4", "C-009", history));

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}
