using System.Windows;
using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.Profiler.ViewModels;

namespace DBTeam.Modules.Profiler.Views;

public partial class ProfilerView : UserControl
{
    public ProfilerView()
    {
        InitializeComponent();
        var vm = ServiceLocator.TryGet<ProfilerViewModel>();
        DataContext = vm;
        if (vm is not null)
        {
            Editor.Text = vm.Sql;
            Editor.TextChanged += (_, _) => vm.Sql = Editor.Text;
            vm.PropertyChanged += (_, a) => { if (a.PropertyName == nameof(ProfilerViewModel.Sql) && Editor.Text != vm.Sql) Editor.Text = vm.Sql ?? ""; };
        }
    }
}
