using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.Monitoring.ViewModels;

namespace DBTeam.Modules.Monitoring.Views;

public partial class MonitoringView : UserControl
{
    public MonitoringView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<MonitoringViewModel>();
    }
}
