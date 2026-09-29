using System.Globalization;

namespace Core.Dto;

public static class ImportStats
{
    public static string Summary(int accepted, int skipped)
    {
        int total = accepted + skipped;
        double errorPercent = total == 0 ? 0 : 100.0 * skipped / total;
        return string.Create(CultureInfo.InvariantCulture,
            $"Усього: {total}, прийнято: {accepted}, пропущено: {skipped} ({errorPercent:F1}% помилок)");
    }
}
