using System;
using DBTeam.Core.Models;

namespace DBTeam.Core.Events;

public sealed class ConnectionOpenedEvent : EventArgs { public SqlConnectionInfo Connection { get; init; } = new(); }
public sealed class ConnectionClosedEvent : EventArgs { public Guid ConnectionId { get; init; } }
public sealed class OpenQueryEditorRequest : EventArgs { public SqlConnectionInfo Connection { get; init; } = new(); public string? InitialSql { get; init; } public string? Database { get; init; } }
public sealed class NodeActivatedEvent : EventArgs { public DbObjectNode Node { get; init; } = new(); public SqlConnectionInfo Connection { get; init; } = new(); public string? Database { get; init; } }
public sealed class OpenDocumentRequest : EventArgs { public string Title { get; init; } = "Document"; public object Content { get; init; } = new(); }
public sealed class ConnectionsChangedEvent : EventArgs { }
public sealed class ShowPaneRequest : EventArgs { public string PaneId { get; init; } = ""; }

public interface IEventBus
{
    void Publish<T>(T @event) where T : EventArgs;
    IDisposable Subscribe<T>(Action<T> handler) where T : EventArgs;
}
