using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.SchemaCompare.ViewModels;

namespace DBTeam.Modules.SchemaCompare.Views;

public partial class SchemaCompareView : UserControl
{
    public SchemaCompareView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<SchemaCompareViewModel>();
    }
}
