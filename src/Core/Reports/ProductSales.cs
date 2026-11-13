namespace Core.Reports;

// Додаткове 1: результат Join/GroupJoin рядків замовлень з каталогом товарів.
public sealed record ProductSales(string ProductId, string Name, int Quantity, decimal Revenue);
