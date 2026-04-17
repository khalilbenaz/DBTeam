using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.DataCompare.ViewModels;

namespace DBTeam.Modules.DataCompare.Views;

public partial class DataCompareView : UserControl
{
    public DataCompareView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<DataCompareViewModel>();
    }
}
