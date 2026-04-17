using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.TableDesigner.ViewModels;

namespace DBTeam.Modules.TableDesigner.Views;

public partial class TableDesignerView : UserControl
{
    public TableDesignerView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<TableDesignerViewModel>();
    }
}
