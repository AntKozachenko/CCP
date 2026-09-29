namespace Core.Dto;

public sealed record ImportResult<T>(IReadOnlyList<T> Items, IReadOnlyList<string> Errors)
{
    public string Summary() => ImportStats.Summary(Items.Count, Errors.Count);
}
