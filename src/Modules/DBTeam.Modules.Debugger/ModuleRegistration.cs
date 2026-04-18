using DBTeam.Modules.Debugger.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.Debugger;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddTransient<DebuggerView>();
    }
}
