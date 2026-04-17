using DBTeam.Modules.DataCompare.ViewModels;
using DBTeam.Modules.DataCompare.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.DataCompare;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddTransient<DataCompareViewModel>();
        s.AddTransient<DataCompareView>();
    }
}
