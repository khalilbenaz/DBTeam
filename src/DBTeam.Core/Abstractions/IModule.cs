using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Core.Abstractions;

public interface IModule
{
    string Id { get; }
    string DisplayName { get; }
    void Register(IServiceCollection services);
}
