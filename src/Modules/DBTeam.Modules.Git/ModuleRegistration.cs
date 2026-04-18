using DBTeam.Modules.Git.ViewModels;
using DBTeam.Modules.Git.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.Git;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddSingleton<GitPanelViewModel>();
        s.AddTransient<GitPanel>();
    }
}
