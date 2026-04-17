using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.Diagram.ViewModels;

namespace DBTeam.Modules.Diagram.Views;

public partial class DiagramView : UserControl
{
    public DiagramView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<DiagramViewModel>();
    }
}
