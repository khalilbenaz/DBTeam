using DBTeam.Modules.Monitoring.ViewModels;
using DBTeam.Modules.Monitoring.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.Monitoring;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddSingleton<MonitoringViewModel>();
        s.AddTransient<MonitoringView>();
    }
}
