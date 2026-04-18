using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.Import.ViewModels;

namespace DBTeam.Modules.Import.Views;

public partial class ImportView : UserControl
{
    public ImportView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<ImportViewModel>();
    }
}
