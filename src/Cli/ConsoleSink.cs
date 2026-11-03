using Core.Abstractions;
using Core.Events;

namespace Cli;

// Додаткове 1 (лаб. 6): приймач сповіщень, що друкує в консоль.
public sealed class ConsoleSink(string prefix = "sink") : INotificationSink
{
    public void Notify(CatalogChangedEventArgs e) =>
        Console.WriteLine($" [{prefix}] {ChangeFormatter.Format(e)}");
}
