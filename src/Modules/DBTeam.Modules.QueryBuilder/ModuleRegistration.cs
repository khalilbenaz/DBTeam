using DBTeam.Modules.QueryBuilder.ViewModels;
using DBTeam.Modules.QueryBuilder.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.Modules.QueryBuilder;

public static class ModuleRegistration
{
    public static void Register(IServiceCollection s)
    {
        s.AddTransient<QueryBuilderViewModel>();
        s.AddTransient<QueryBuilderView>();
    }
}
