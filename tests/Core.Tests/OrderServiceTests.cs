using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class OrderServiceTests
{
    [Fact] // інваріант: відмова і незмінний стан
    public void ChangePrice_WithZeroPrice_Throws_AndKeepsState()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);
        var p = service.Add("Кабель", 100m);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.ChangePrice(p.Id, 0m));
        Assert.Equal(100m, store.GetById(p.Id)!.Price);
    }

    [Fact] // подія як спосіб перевірки: без консолі й диска
    public void Add_RaisesChangedExactlyOnce()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.Add("Гвинт", 7m);

        Assert.Single(events);
        Assert.Equal(CatalogChangeKind.Added, events[0].Kind);
    }

    [Fact] // відмова не піднімає подію
    public void FailedOperation_RaisesNoEvent()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var events = 0;
        service.Changed += (_, _) => events++;

        Assert.Throws<ArgumentException>(() => service.Add("  ", 10m));
        Assert.Throws<InvalidOperationException>(() => service.Remove("P-NOPE"));

        Assert.Equal(0, events);
    }
}
