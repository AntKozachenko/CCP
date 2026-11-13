namespace Core.Reports;

// DTO рядка "клієнт: скільки замовлень і на яку суму".
public sealed record CustomerSummary(string CustomerId, int Orders, decimal TotalAmount);
