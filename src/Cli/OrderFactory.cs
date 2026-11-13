using Core;
using Core.Abstractions;
using Core.Storage;

namespace Cli;

// Додаткове 3 (лаб. 5): вибір реалізації винесено з Program.cs.

public static class StoreFactory
{
    public static IOrderStore Create(string[] args)
    {
        IOrderStore store = args.Contains("--file")
            ? new FileOrderStore(RepoPaths.Combine("data", "catalog.json"))
            : new InMemoryOrderStore(SampleData.Products());

        return args.Contains("--cache") ? new CachingOrderStore(store) : store;
    }
}
