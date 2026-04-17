using DBTeam.Modules.SchemaCompare.ViewModels;
using DBTeam.Modules.SchemaCompare.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.SchemaCompare;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddTransient<SchemaCompareViewModel>();
        s.AddTransient<SchemaCompareView>();
    }
}
