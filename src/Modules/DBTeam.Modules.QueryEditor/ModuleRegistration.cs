using DBTeam.Modules.QueryEditor.Intellisense;
using DBTeam.Modules.QueryEditor.ViewModels;
using DBTeam.Modules.QueryEditor.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.QueryEditor;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddSingleton<SqlCompletionProvider>();
        s.AddTransient<QueryEditorViewModel>();
        s.AddTransient<QueryEditorView>();
        s.AddSingleton<QueryHistoryPanelViewModel>();
        s.AddTransient<QueryHistoryPanel>();
    }
}
