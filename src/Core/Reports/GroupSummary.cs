namespace Core.Reports;

// DTO групи: ключ групи, скільки в ній елементів і яка їхня сумарна вартість.
public sealed record GroupSummary(string Key, int Count, decimal Total);
