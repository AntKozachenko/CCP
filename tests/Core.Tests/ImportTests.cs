using Core.Abstractions;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class ImportTests
{
    private static (OrderService Service, InMemoryOrderStore Store) NewService()
    {
        var store = new InMemoryOrderStore();
        return (new OrderService(store), store);
    }

    // ----- скасування -----

    [Fact] // заздалегідь скасований токен; файл "any.json" не існує, тож виняток має бути саме скасуванням
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var (service, _) = NewService();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonCatalogImporter(), "any.json", cts.Token));
    }

    [Fact] // скасування посеред циклу: додані записи лишаються, наступні не додаються
    public async Task ImportAsync_CancelledMidway_StopsLoop()
    {
        using var cts = new CancellationTokenSource();
        var (service, store) = NewService();
        service.Changed += (_, _) => { if (store.List().Count == 2) cts.Cancel(); };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new FakeImporter(TestData.Products(5)), "x", cts.Token));

        Assert.Equal(2, store.List().Count);
    }

    // ----- зв'язані токени й таймаут -----

    [Fact] // довів, що спрацював саме таймаут: токен користувача не скасований
    public async Task ImportWithTimeout_WhenTimerFires_ThrowsTimeoutException()
    {
        var (service, _) = NewService();

        await Assert.ThrowsAsync<TimeoutException>(
            () => service.ImportWithTimeoutAsync(new HangingImporter(), "x", TimeSpan.FromMilliseconds(50)));
    }

    [Fact] // користувач скасував раніше за таймер: це скасування, а не таймаут
    public async Task ImportWithTimeout_WhenUserCancels_ThrowsCancellation_NotTimeout()
    {
        using var userCts = new CancellationTokenSource();
        var (service, _) = NewService();

        var task = service.ImportWithTimeoutAsync(new HangingImporter(), "x", TimeSpan.FromMinutes(5), userCts.Token);
        await userCts.CancelAsync();

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.IsNotType<TimeoutException>(ex);
    }

    // ----- логіка імпорту без диска -----

    [Fact]
    public async Task ImportAsync_AddsItems_RaisesEventPerItem_AndIsIdempotent()
    {
        var (service, store) = NewService();
        var kinds = new List<CatalogChangeKind>();
        service.Changed += (_, e) => kinds.Add(e.Kind);
        var importer = new FakeImporter(TestData.Products(3));

        var first = await service.ImportAsync(importer, "x");
        var second = await service.ImportAsync(importer, "x");

        Assert.Equal(3, first);
        Assert.Equal(0, second);
        Assert.Equal(3, store.List().Count);
        Assert.Equal(3, kinds.Count);
        Assert.All(kinds, k => Assert.Equal(CatalogChangeKind.Added, k));
    }

    [Fact] // IProgress<int>: відсотки зростають і закінчуються на 100
    public async Task ImportAsync_ReportsIncreasingPercent_EndingAt100()
    {
        var (service, _) = NewService();
        var progress = new SyncProgress();

        await service.ImportAsync(new FakeImporter(TestData.Products(4)), "x", progress: progress);

        Assert.Equal(new[] { 25, 50, 75, 100 }, progress.Values);
    }

    [Fact] // потоковий імпорт через await foreach
    public async Task ImportStreamAsync_AddsAllItems()
    {
        var (service, store) = NewService();

        var added = await service.ImportStreamAsync(new FakeImporter(TestData.Products(3)), "x");

        Assert.Equal(3, added);
        Assert.Equal(3, store.List().Count);
    }

    // ----- справжній JSON-імпортер: лише тимчасові файли -----

    [Fact]
    public async Task LoadAsync_WithBadRecord_ThrowsDomainException_WithRecordNumber()
    {
        using var file = new TempFile("""[{"id":"A","name":"Ok","price":10},{"id":"B","name":"","price":5}]""");

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => new JsonCatalogImporter().LoadAsync(file.Path));

        Assert.Contains("Запис 2", ex.Message);
        Assert.IsAssignableFrom<ArgumentException>(ex.InnerException);
    }

    [Fact] // технічна помилка не маскується під доменну, а початковий виняток зберігається
    public async Task LoadAsync_WithBrokenJson_ThrowsStorageException_WithInner()
    {
        using var file = new TempFile("[{ це не json");

        var ex = await Assert.ThrowsAsync<StorageException>(
            () => new JsonCatalogImporter().LoadAsync(file.Path));

        Assert.IsAssignableFrom<System.Text.Json.JsonException>(ex.InnerException);
    }

    [Fact]
    public async Task LoadAsync_WithValidFile_ReturnsProducts()
    {
        using var file = new TempFile("""[{"id":"A","name":"Один","price":10},{"id":"B","name":"Два","price":20.5}]""");

        var items = await new JsonCatalogImporter().LoadAsync(file.Path);

        Assert.Equal(new[] { "A", "B" }, items.Select(p => p.Id));
        Assert.Equal(20.5m, items[1].Price);
    }

    [Fact] // потік: записи до поганого вже додані, поганий дає DomainException з номером
    public async Task ImportStreamAsync_WithBadRecord_ThrowsDomainException_AfterValidOnes()
    {
        using var file = new TempFile("""[{"id":"A","name":"Ok","price":10},{"id":"B","name":"Ok","price":-1}]""");
        var (service, store) = NewService();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.ImportStreamAsync(new JsonCatalogImporter(), file.Path));

        Assert.Contains("Запис 2", ex.Message);
        Assert.Single(store.List());
    }

    [Fact]
    public async Task StreamAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        ICatalogImporter importer = new JsonCatalogImporter();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in importer.StreamAsync("any.json", cts.Token)) { }
        });
    }

    [Fact] // варіант "Замовлення": рядки замовлень, сума з файлу
    public async Task OrderImporter_LoadsOrdersWithLines()
    {
        using var file = new TempFile("""
            [{"id":"O-1","customerId":"C-1","status":"Confirmed",
              "lines":[{"orderId":"O-1","productId":"P-1","quantity":2,"unitPrice":10},
                       {"orderId":"O-1","productId":"P-2","quantity":1,"unitPrice":5.5}]}]
            """);

        var orders = await new JsonOrderImporter().LoadAsync(file.Path);

        Assert.Single(orders);
        Assert.Equal(25.5m, orders[0].Total);
        Assert.Equal(Core.Domain.OrderStatus.Confirmed, orders[0].Status);
    }

    [Fact] // підтверджене замовлення без рядків порушує бізнес-правило
    public async Task OrderImporter_ConfirmedWithoutLines_ThrowsDomainException()
    {
        using var file = new TempFile("""[{"id":"O-1","customerId":"C-1","status":"Confirmed","lines":[]}]""");

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => new JsonOrderImporter().LoadAsync(file.Path));

        Assert.Contains("Замовлення 1", ex.Message);
    }
}
