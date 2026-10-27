using Core;
using Core.Abstractions;
using Core.Storage;

namespace Cli;

// Додаткове 3: вибір реалізації винесено з Program.cs.
// Це все ще composition root (конкретні класи знає лише точка входу), просто розбитий на два файли.
public static class OrderFactory
{
    public static IOrderStore Create(string[] args)
    {
        IOrderStore store = args.Contains("--file")
            ? new FileOrderStore(Path.Combine(AppContext.BaseDirectory, "data", "catalog.json"))
            : new InMemoryOrderStore(SampleData.Products());

        return args.Contains("--cache") ? new CachingOrderStore(store) : store;
    }
}
