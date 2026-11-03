using Core;
using Core.Abstractions;
using Core.Storage;

namespace Cli;

// Додаткове 3: вибір реалізації винесено з Program.cs.
// Це все ще composition root (конкретні класи знає лише точка входу), просто розбитий на два файли.
public static class StoreFactory
{
    public static IOrderStore Create(string[] args)
    {
        IOrderStore store = args.Contains("--file")
            ? new FileOrderStore(FindDataPath())
            : new InMemoryOrderStore(SampleData.Products());

        return args.Contains("--cache") ? new CachingOrderStore(store) : store;
    }

    // Іде вгору від папки збірки до теки з файлом рішення; data/catalog.json лежить у корені репозиторію.
    private static string FindDataPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.EnumerateFiles("*.sln*").Any())
            dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? AppContext.BaseDirectory, "data", "catalog.json");
    }
}
