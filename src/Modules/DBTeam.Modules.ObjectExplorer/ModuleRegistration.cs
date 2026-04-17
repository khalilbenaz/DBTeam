using DBTeam.Modules.ObjectExplorer.ViewModels;
using DBTeam.Modules.ObjectExplorer.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.ObjectExplorer;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddSingleton<ObjectExplorerViewModel>();
        s.AddTransient<ObjectExplorerView>();
    }
}
