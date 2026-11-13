namespace Core.Reports;

public static class EnumerableExtensions
{
    // Додаткове 2: власний метод-розширення. Топ-N за спаданням ключа з обов'язковим другим ключем,
    // щоб порядок не залежав від порядку у джерелі. Використано у TopProducts і TopOrders.
    //
    // Метод навмисно НЕ iterator (без yield): перевірки аргументів спрацюють одразу
    // при виклику, а не пізніше, коли хтось почне перебирати результат.
    // Повертає IEnumerable: запит лишається відкладеним, ToList викликає той, хто звертається.
    public static IEnumerable<T> TopBy<T, TKey>(
        this IEnumerable<T> source, Func<T, TKey> key, Func<T, string> tieBreaker, int n)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(tieBreaker);
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        return source.OrderByDescending(key)
                     .ThenBy(tieBreaker, StringComparer.Ordinal)
                     .Take(n);
    }
}
