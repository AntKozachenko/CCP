namespace Core.Reports;

// DTO рядка звіту про замовлення. Це контракт звіту: Api (тиждень 10) і Blazor (тиждень 12)
// бачать лише ці поля й не залежать від того, як усередині побудований Order.
// Status це string, а не OrderStatus: клієнту звіту enum з домену не потрібен.
public sealed record OrderSummary(string OrderId, string CustomerId, string Status, int Lines, decimal Total);
