using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.Admin.ViewModels;

namespace DBTeam.Modules.Admin.Views;

public partial class AdminView : UserControl
{
    public AdminView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<AdminViewModel>();
    }
}
