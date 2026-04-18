using DBTeam.Modules.AiAssistant.Engine;
using DBTeam.Modules.AiAssistant.ViewModels;
using DBTeam.Modules.AiAssistant.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.AiAssistant;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddSingleton<AiSettingsStore>();
        s.AddSingleton<AiAssistantViewModel>();
        s.AddTransient<AiAssistantView>();
    }
}
