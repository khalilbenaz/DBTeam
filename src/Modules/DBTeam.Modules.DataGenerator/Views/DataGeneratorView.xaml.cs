using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.DataGenerator.ViewModels;

namespace DBTeam.Modules.DataGenerator.Views;

public partial class DataGeneratorView : UserControl
{
    public DataGeneratorView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<DataGeneratorViewModel>();
    }
}
