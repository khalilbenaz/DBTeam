using DBTeam.Modules.DataGenerator.ViewModels;
using DBTeam.Modules.DataGenerator.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.DataGenerator;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddTransient<DataGeneratorViewModel>();
        s.AddTransient<DataGeneratorView>();
    }
}
