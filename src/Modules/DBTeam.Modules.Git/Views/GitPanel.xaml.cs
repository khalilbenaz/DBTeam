using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.Git.ViewModels;

namespace DBTeam.Modules.Git.Views;

public partial class GitPanel : UserControl
{
    public GitPanel()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<GitPanelViewModel>();
    }
}
