using System.Windows.Controls;
using System.Windows.Input;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.Terminal.ViewModels;

namespace DBTeam.Modules.Terminal.Views;

public partial class TerminalView : UserControl
{
    public TerminalView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<TerminalViewModel>();
    }

    private void Output_TextChanged(object sender, TextChangedEventArgs e)
    {
        OutputBox.ScrollToEnd();
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is TerminalViewModel vm)
        {
            vm.SendCommand.Execute(null);
            e.Handled = true;
        }
    }
}
