using DBTeam.Modules.Diagram.ViewModels;
using DBTeam.Modules.Diagram.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.Diagram;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddTransient<DiagramViewModel>();
        s.AddTransient<DiagramView>();
    }
}
