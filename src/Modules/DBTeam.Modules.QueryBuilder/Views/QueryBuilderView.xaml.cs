using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.QueryBuilder.ViewModels;

namespace DBTeam.Modules.QueryBuilder.Views;

public partial class QueryBuilderView : UserControl
{
    public QueryBuilderView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<QueryBuilderViewModel>();
    }
}
