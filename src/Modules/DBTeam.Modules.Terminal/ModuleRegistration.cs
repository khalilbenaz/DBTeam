using DBTeam.Modules.Terminal.ViewModels;
using DBTeam.Modules.Terminal.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.Terminal;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddSingleton<TerminalViewModel>();
        s.AddTransient<TerminalView>();
    }
}
