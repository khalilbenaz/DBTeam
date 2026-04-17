using System;

namespace DBTeam.Core.Infrastructure;

public static class ServiceLocator
{
    public static IServiceProvider? Services { get; set; }

    public static T Get<T>() where T : notnull
        => (T)(Services?.GetService(typeof(T)) ?? throw new InvalidOperationException($"Service {typeof(T).Name} not registered"));

    public static T? TryGet<T>() where T : class
        => Services?.GetService(typeof(T)) as T;
}
