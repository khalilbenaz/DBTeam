using DBTeam.Core.Events;
using DBTeam.Core.Infrastructure;
using DBTeam.Core.Models;

namespace DBTeam.Tests.Infrastructure;

public class EventBusTests
{
    [Fact]
    public void Subscribe_And_Publish_Delivers_Event()
    {
        var bus = new EventBus();
        var received = 0;
        using var sub = bus.Subscribe<ConnectionsChangedEvent>(_ => received++);

        bus.Publish(new ConnectionsChangedEvent());
        bus.Publish(new ConnectionsChangedEvent());

        Assert.Equal(2, received);
    }

    [Fact]
    public void Dispose_Subscription_Stops_Delivery()
    {
        var bus = new EventBus();
        var received = 0;
        var sub = bus.Subscribe<ConnectionsChangedEvent>(_ => received++);
        bus.Publish(new ConnectionsChangedEvent());
        sub.Dispose();
        bus.Publish(new ConnectionsChangedEvent());

        Assert.Equal(1, received);
    }

    [Fact]
    public void Events_Are_Type_Scoped()
    {
        var bus = new EventBus();
        var a = 0; var b = 0;
        using var s1 = bus.Subscribe<ConnectionsChangedEvent>(_ => a++);
        using var s2 = bus.Subscribe<ConnectionOpenedEvent>(_ => b++);

        bus.Publish(new ConnectionsChangedEvent());
        bus.Publish(new ConnectionOpenedEvent { Connection = new SqlConnectionInfo { Name = "x" } });

        Assert.Equal(1, a);
        Assert.Equal(1, b);
    }
}
