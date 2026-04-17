using DBTeam.Modules.ConnectionManager.ViewModels;
using DBTeam.Modules.ConnectionManager.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.ConnectionManager;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddTransient<ConnectionDialogViewModel>();
        s.AddTransient<ConnectionDialog>();
        s.AddSingleton<ConnectionsPanelViewModel>();
        s.AddTransient<ConnectionsPanel>();
    }
}
