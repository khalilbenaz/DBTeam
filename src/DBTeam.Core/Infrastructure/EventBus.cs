using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DBTeam.Core.Events;

namespace DBTeam.Core.Infrastructure;

public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subs = new();

    public void Publish<T>(T @event) where T : EventArgs
    {
        if (!_subs.TryGetValue(typeof(T), out var list)) return;
        Delegate[] snapshot;
        lock (list) snapshot = list.ToArray();
        foreach (var d in snapshot) ((Action<T>)d).Invoke(@event);
    }

    public IDisposable Subscribe<T>(Action<T> handler) where T : EventArgs
    {
        var list = _subs.GetOrAdd(typeof(T), _ => new List<Delegate>());
        lock (list) list.Add(handler);
        return new Sub(() => { lock (list) list.Remove(handler); });
    }

    private sealed class Sub : IDisposable
    {
        private readonly Action _a; private bool _d;
        public Sub(Action a) { _a = a; }
        public void Dispose() { if (_d) return; _d = true; _a(); }
    }
}
