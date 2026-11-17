using Core.Configuration;
using Core.Domain;
using Core.Storage;

namespace Core.Tests;

public class ConfigurationTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "crossapp-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void ResolveFullPath_Relative_UsesBaseDir_AndCreatesDirectory()
    {
        var path = new StorageOptions { DataPath = "data", FileName = "x.json" }.ResolveFullPath(_dir);

        Assert.Equal(Path.GetFullPath(Path.Combine(_dir, "data", "x.json")), path);
        Assert.True(Directory.Exists(Path.Combine(_dir, "data")));
    }

    [Fact]
    public void ResolveFullPath_Absolute_IgnoresBaseDir()
    {
        var abs = Path.Combine(_dir, "abs");
        var path = new StorageOptions { DataPath = abs, FileName = "x.json" }.ResolveFullPath("/ignored");

        Assert.Equal(Path.Combine(abs, "x.json"), path);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void WithDataPath_EmptyOverride_KeepsOriginal(string? value)
    {
        var o = new StorageOptions { DataPath = "data" };
        Assert.Equal("data", o.WithDataPath(value).DataPath);
    }

    [Fact]
    public void WithDataPath_Override_ReplacesOnlyDataPath()
    {
        var o = new StorageOptions { DataPath = "data", FileName = "a.json" }.WithDataPath("tmp");
        Assert.Equal("tmp", o.DataPath);
        Assert.Equal("a.json", o.FileName);
    }

    [Fact]
    public void Write_ThenRead_RoundTrips_AndLeavesNoTmp()
    {
        var path = Path.Combine(_dir, "nested", "catalog.json");
        JsonCatalogFile.Write(path, [Product.Create("P-1", "Цемент М400", 120m)]);

        var items = JsonCatalogFile.Read(path);
        Assert.Single(items);
        Assert.Equal("Цемент М400", items[0].Name);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Write_UsesCamelCase_AndReadableCyrillic()
    {
        var path = Path.Combine(_dir, "c.json");
        JsonCatalogFile.Write(path, [Product.Create("P-1", "Цемент", 1m)]);

        var text = File.ReadAllText(path);
        Assert.Contains("\"name\": \"Цемент\"", text);
        Assert.DoesNotContain("\\u0426", text);
    }

    [Fact]
    public void Read_CorruptedJson_ThrowsStorageException()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "bad.json");
        File.WriteAllText(path, "[ { \"id\": ");

        var ex = Assert.Throws<StorageException>(() => JsonCatalogFile.Read(path));
        Assert.Contains("bad.json", ex.Message);
    }

    [Fact]
    public void Read_InvalidRecord_ThrowsStorageException_WithNumber()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "inv.json");
        File.WriteAllText(path, "[{\"id\":\"P-1\",\"name\":\"A\",\"price\":0}]");

        var ex = Assert.Throws<StorageException>(() => JsonCatalogFile.Read(path));
        Assert.Contains("запис 1", ex.Message);
    }

    [Fact]
    public void FileOrderStore_PersistsAcrossInstances_AndRecreatesDeletedDirectory()
    {
        var path = Path.Combine(_dir, "data", "catalog.json");
        new FileOrderStore(path).Add(Product.Create("P-1", "A", 5m));

        Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        new FileOrderStore(path).Add(Product.Create("P-2", "B", 6m));   // каталог створюється заново

        Assert.Single(new FileOrderStore(path).List());
    }

    [Fact]
    public void Read_OldPascalCaseFile_StillLoads()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "old.json");
        File.WriteAllText(path, "[{\"Id\":\"P-1\",\"Name\":\"A\",\"Price\":5.0}]");

        Assert.Equal("P-1", JsonCatalogFile.Read(path)[0].Id);
    }
}
