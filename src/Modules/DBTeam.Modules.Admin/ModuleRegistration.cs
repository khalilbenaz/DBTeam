using DBTeam.Modules.Admin.ViewModels;
using DBTeam.Modules.Admin.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.Admin;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddTransient<AdminViewModel>();
        s.AddTransient<AdminView>();
    }
}
