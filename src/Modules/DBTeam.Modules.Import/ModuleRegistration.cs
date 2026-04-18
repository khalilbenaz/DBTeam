using DBTeam.Modules.Import.ViewModels;
using DBTeam.Modules.Import.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.Import;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddTransient<ImportViewModel>();
        s.AddTransient<ImportView>();
    }
}
