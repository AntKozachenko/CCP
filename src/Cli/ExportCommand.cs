using Core;
using Core.Services;
using Core.Storage;

namespace Cli;

// Лаб. 9. Експорт усієї колекції в JSON (атомарно) і зворотний імпорт.
// export [шлях]  — записує всі товари; без шляху пише у файл даних із конфігурації.
// Імпорт файлу експорту робить наявна команда `import <шлях>` (читає camelCase і PascalCase).
public static class ExportCommand
{
    public static int Run(string[] args, OrderService service, AppSettings settings)
    {
        var target = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).Skip(1).FirstOrDefault();
        var path = target is null ? settings.DataFile : Path.GetFullPath(target);   // відносний шлях від cwd, як просив користувач

        var items = service.All();
        JsonCatalogFile.Write(path, items);

        Console.WriteLine($"Файл даних : {path}");
        Console.WriteLine($"Експортовано {items.Count} записів.");
        return 0;
    }
}
