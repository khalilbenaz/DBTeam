using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.QueryEditor.ViewModels;

namespace DBTeam.Modules.QueryEditor.Views;

public partial class QueryHistoryPanel : UserControl
{
    public QueryHistoryPanel()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<QueryHistoryPanelViewModel>();
    }
}
