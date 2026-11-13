using Core.Domain;

namespace Core.Reports;

// Усі запити тут. Вхід IEnumerable<...> (а не сховище): чисті функції легко перевірити тестом
// і викликати з будь-якого джерела. Вихід IReadOnlyList<DTO>: запит уже виконано, Count у Cli
// не запустить його вдруге. Клас нічого не друкує і не читає з диска.
public static class ReportService
{
    // Правило звітів: "клієнт", "чек", "товар" рахуються за ПІДТВЕРДЖЕНИМИ замовленнями.
    // Чернетка ще не продаж, скасоване не продаж. Правило одне місце, а не копія в кожному запиті.
    private static IEnumerable<Order> Confirmed(IEnumerable<Order> source) =>
        source.Where(o => o.Status == OrderStatus.Confirmed);

    private static OrderSummary ToSummary(Order o) =>
        new(o.Id, o.CustomerId, o.Status.ToString(), o.Lines.Count, o.Total);

    // 1) Питання: які замовлення дорожчі за поріг?  Оператори: Where, OrderByDescending, ThenBy, Select.
    // Where йде ПЕРШИМ: сортуємо вже відібране, а не все підряд.
    public static IReadOnlyList<OrderSummary> ExpensiveOrders(IEnumerable<Order> source, decimal threshold) => //DoD1
        source.Where(o => o.Total > threshold)
              .OrderByDescending(o => o.Total).ThenBy(o => o.Id, StringComparer.Ordinal)
              .Select(ToSummary)
              .ToList();

    // 2) Питання: які 5 клієнтів принесли найбільше?  Оператори: Where, GroupBy, Select, OrderByDescending, ThenBy, Take.
    // ThenBy за CustomerId робить топ-N детермінованим: у даних є клієнти з однаковою сумою.
    public static IReadOnlyList<CustomerSummary> TopCustomers(IEnumerable<Order> source, int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        return Confirmed(source)
            .GroupBy(o => o.CustomerId, StringComparer.Ordinal)
            .Select(g => new CustomerSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(c => c.TotalAmount).ThenBy(c => c.CustomerId, StringComparer.Ordinal) //DoD 3
            .Take(n)
            .ToList();
    }

    // 3) Питання: як замовлення розподілені за статусом?  Оператори: GroupBy, Select, OrderByDescending, ThenBy.
    // Ключ групи це enum, а не рядок: порівняння за значенням, без питання про регістр.
    public static IReadOnlyList<GroupSummary> ByStatus(IEnumerable<Order> source) =>
        source.GroupBy(o => o.Status)
              .Select(g => (Status: g.Key, Count: g.Count(), Total: g.Sum(o => o.Total)))
              .OrderByDescending(x => x.Total).ThenBy(x => x.Status)   // другий ключ: порядок enum
              .Select(x => new GroupSummary(x.Status.ToString(), x.Count, x.Total))
              .ToList();

    // 4) Питання: який середній чек і найбільше замовлення?  Оператори: Where, Select, Sum, Average, Max.
    // Порожнє джерело не кидає InvalidOperationException: Count == 0 перевіряємо ДО Average/Max.
    // Усі суми decimal (гроші), тому Average теж decimal, а не double.
    public static (int Orders, decimal Total, decimal Average, decimal Max) Totals(IEnumerable<Order> source)
    {
        var totals = Confirmed(source).Select(o => o.Total).ToList();   // Total рахується раз на замовлення

        return totals.Count == 0
            ? (0, 0m, 0m, 0m)    // суфікс m: decimal, як 0d у лабі робить double
            : (totals.Count, totals.Sum(), totals.Average(), totals.Max());
    }

    // ===== Додаткові завдання =====

    // Додаткове 1 (Join): з'єднуємо рядки замовлень з каталогом, щоб отримати назву товару.
    // SelectMany "розплющує" замовлення в єдиний потік рядків. Топ-5 через власний TopBy (додаткове 2).
    public static IReadOnlyList<ProductSales> TopProducts(
        IEnumerable<Order> orders, IEnumerable<Product> catalog, int n) =>
        Confirmed(orders)
            .SelectMany(o => o.Lines)
            .Join(catalog,
                  line => line.ProductId,
                  product => product.Id,
                  (line, product) => (product.Id, product.Name, line.Quantity, Amount: line.LineTotal),
                  StringComparer.Ordinal)
            .GroupBy(x => x.Id, StringComparer.Ordinal)
            .Select(g => new ProductSales(g.Key, g.First().Name, g.Sum(x => x.Quantity), g.Sum(x => x.Amount)))
            .TopBy(s => s.Revenue, s => s.ProductId, n)
            .ToList();

    // Додаткове 1 (GroupJoin): ліве з'єднання. Товар лишається у результаті, навіть якщо його ніхто
    // не купив (sold порожня). Саме це відрізняє GroupJoin від Join.
    // Внутрішня послідовність (рядки) буферизується в Lookup один раз, а не по разу на товар.
    public static IReadOnlyList<ProductSales> UnsoldProducts(IEnumerable<Order> orders, IEnumerable<Product> catalog) =>
        catalog.GroupJoin(Confirmed(orders).SelectMany(o => o.Lines),
                          product => product.Id,
                          line => line.ProductId,
                          (product, sold) => new ProductSales(
                              product.Id, product.Name, sold.Sum(l => l.Quantity), sold.Sum(l => l.LineTotal)),
                          StringComparer.Ordinal)
               .Where(s => s.Quantity == 0)
               .OrderBy(s => s.ProductId, StringComparer.Ordinal)
               .ToList();

    // Додаткове 2: другий звіт на власному TopBy: найбільші підтверджені замовлення.
    public static IReadOnlyList<OrderSummary> TopOrders(IEnumerable<Order> source, int n) =>
        Confirmed(source)
            .Select(ToSummary)
            .TopBy(s => s.Total, s => s.OrderId, n)
            .ToList();

    // Додаткове 3: той самий звіт, що ByStatus, у синтаксисі запитів.
    // Компілятор перекладає його на ті самі GroupBy/Select/OrderByDescending.
    // let зберігає суму групи, щоб не рахувати двічі (у методному записі для цього потрібен проміжний Select).
    // Мінус синтаксису запитів: у group by не передати StringComparer, тому для ключа-рядка довелося б
    // переходити на методний запис.
    public static IReadOnlyList<GroupSummary> ByStatusQuerySyntax(IEnumerable<Order> source) =>
        (from o in source
         group o by o.Status into g
         let total = g.Sum(o => o.Total)
         orderby total descending, g.Key
         select new GroupSummary(g.Key.ToString(), g.Count(), total))
        .ToList();
}
