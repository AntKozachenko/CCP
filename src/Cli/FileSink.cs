using Core.Abstractions;
using Core.Events;

namespace Cli;

// Додаткове 1 (лаб. 6): приймач сповіщень, що дописує рядок у файл.
public sealed class FileSink(string path) : INotificationSink
{
    private readonly string _path = Path.GetFullPath(path);

    public void Notify(CatalogChangedEventArgs e)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.AppendAllText(_path, ChangeFormatter.Format(e) + Environment.NewLine);
    }
}
