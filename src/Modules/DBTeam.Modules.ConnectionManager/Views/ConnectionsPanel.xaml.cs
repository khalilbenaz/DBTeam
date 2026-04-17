using System.Windows.Controls;
using System.Windows.Input;
using DBTeam.Core.Infrastructure;
using DBTeam.Core.Models;
using DBTeam.Modules.ConnectionManager.ViewModels;

namespace DBTeam.Modules.ConnectionManager.Views;

public partial class ConnectionsPanel : UserControl
{
    public ConnectionsPanel()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<ConnectionsPanelViewModel>();
    }

    private void ConnectionsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ConnectionsPanelViewModel vm) return;
        if (ConnectionsList.SelectedItem is not SqlConnectionInfo info) return;
        if (vm.ConnectCommand.CanExecute(info))
            vm.ConnectCommand.Execute(info);
    }
}
