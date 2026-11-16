using Core.Abstractions;
using Core.Domain;

namespace Core.Tests;

// Імпортер із готового списку: без диска.
internal sealed class FakeImporter(IReadOnlyList<Product> items) : ICatalogImporter
{
    public Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default) =>
        Task.FromResult(items);

    public async IAsyncEnumerable<Product> StreamAsync(string path,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var item in items) { await Task.Yield(); yield return item; }
    }
}

// Імпортер, що "висить", доки не скасують токен: імітує повільну мережу чи диск.
internal sealed class HangingImporter : ICatalogImporter
{
    public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        await Task.Delay(Timeout.Infinite, ct);
        return [];
    }

    public IAsyncEnumerable<Product> StreamAsync(string path, CancellationToken ct = default) =>
        throw new NotSupportedException();
}

// Синхронний IProgress: Progress<T> викликає обробник асинхронно, і тест міг би перевірити список зарано.
internal sealed class SyncProgress : IProgress<int>
{
    public List<int> Values { get; } = [];
    public void Report(int value) => Values.Add(value);
}

// Тимчасовий файл, що видаляється в Dispose (еквівалент try/finally). У data/ тести не заглядають.
internal sealed class TempFile : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"core-tests-{Guid.NewGuid():N}.json");

    public TempFile(string content) => File.WriteAllText(Path, content);

    public void Dispose() => File.Delete(Path);
}
