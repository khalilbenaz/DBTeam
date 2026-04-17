using DBTeam.Modules.Profiler.ViewModels;
using DBTeam.Modules.Profiler.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.Profiler;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddTransient<ProfilerViewModel>();
        s.AddTransient<ProfilerView>();
    }
}
