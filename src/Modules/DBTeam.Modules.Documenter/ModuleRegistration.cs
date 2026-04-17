using DBTeam.Modules.Documenter.ViewModels;
using DBTeam.Modules.Documenter.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.Documenter;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddTransient<DocumenterViewModel>();
        s.AddTransient<DocumenterView>();
    }
}
