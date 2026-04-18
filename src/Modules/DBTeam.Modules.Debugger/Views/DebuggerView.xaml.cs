using System.Windows;
using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.Debugger.ViewModels;

namespace DBTeam.Modules.Debugger.Views;

public partial class DebuggerView : UserControl
{
    public DebuggerView()
    {
        InitializeComponent();
        var vm = ServiceLocator.TryGet<DebuggerViewModel>();
        DataContext = vm;
        if (vm is not null)
        {
            Editor.Text = vm.Sql ?? "";
            Editor.TextChanged += (_, _) => vm.Sql = Editor.Text;
            vm.PropertyChanged += (_, a) => { if (a.PropertyName == nameof(DebuggerViewModel.Sql) && Editor.Text != vm.Sql) Editor.Text = vm.Sql ?? ""; };
        }
    }
}
