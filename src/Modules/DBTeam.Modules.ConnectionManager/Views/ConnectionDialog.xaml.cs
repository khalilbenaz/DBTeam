using System.Windows;
using DBTeam.Modules.ConnectionManager.ViewModels;

namespace DBTeam.Modules.ConnectionManager.Views;

public partial class ConnectionDialog : Window
{
    private readonly ConnectionDialogViewModel _vm;
    public ConnectionDialog(ConnectionDialogViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.CloseAction = ok => { DialogResult = ok; Close(); };
        if (!string.IsNullOrEmpty(vm.Model.Password)) PwdBox.Password = vm.Model.Password;
    }

    private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _vm.Model.Password = PwdBox.Password;
    }
}
