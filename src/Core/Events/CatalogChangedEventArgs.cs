namespace Core.Events;

// Дані події: ЩО сталося з каталогом. Усі властивості лише для читання,
// тож підписник не може "підправити" повідомлення для інших підписників.
// OldPrice/NewPrice nullable, бо при Added старої ціни ще немає, а при Removed нової вже немає.
public sealed class CatalogChangedEventArgs(
    string id, CatalogChangeKind kind, decimal? oldPrice, decimal? newPrice) : EventArgs
{
    public string Id { get; } = id;
    public CatalogChangeKind Kind { get; } = kind;
    public decimal? OldPrice { get; } = oldPrice;
    public decimal? NewPrice { get; } = newPrice;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}
