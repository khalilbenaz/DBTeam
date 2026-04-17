using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.Documenter.ViewModels;

namespace DBTeam.Modules.Documenter.Views;

public partial class DocumenterView : UserControl
{
    public DocumenterView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<DocumenterViewModel>();
    }
}
