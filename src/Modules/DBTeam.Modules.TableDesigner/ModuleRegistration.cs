using DBTeam.Modules.TableDesigner.ViewModels;
using DBTeam.Modules.TableDesigner.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.TableDesigner;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddTransient<TableDesignerViewModel>();
        s.AddTransient<TableDesignerView>();
    }
}
