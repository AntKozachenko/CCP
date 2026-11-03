using Core.Events;

namespace Cli;

// Як подія виглядає як текст. Це справа Cli, а не Core: домен лише каже "що сталося".
// Спільний для обробника в Program.cs, ConsoleSink і FileSink, щоб рядок скрізь був однаковий.
public static class ChangeFormatter
{
    public static string Format(CatalogChangedEventArgs e)
    {
        // InvariantCulture: "599.00", а не "599,00" залежно від локалі машини
        string detail = e.Kind switch
        {
            CatalogChangeKind.Added => FormattableString.Invariant($"ціна {e.NewPrice:F2}"),
            // формат "+0.00;-0.00": дві секції через ';', перша для додатних зі знаком "+", друга для від'ємних
            CatalogChangeKind.PriceChanged => FormattableString.Invariant(
                $"{e.OldPrice:F2} → {e.NewPrice:F2} ({e.NewPrice - e.OldPrice:+0.00;-0.00})"),
            CatalogChangeKind.Removed => FormattableString.Invariant($"була {e.OldPrice:F2}"),
            _ => string.Empty
        };

        return FormattableString.Invariant($"{e.At:HH:mm:ss} {e.Kind,-12} {e.Id,-11} {detail}");
    }
}
