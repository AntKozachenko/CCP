using System.Globalization;

namespace Core.Import;

internal static class PriceParser
{
    public static bool TryParse(string text, out decimal price) =>
        decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out price)
        && price >= 0;

    public static decimal Parse(string text) =>
        decimal.Parse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
}
