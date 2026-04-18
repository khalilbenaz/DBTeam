using System.Windows.Controls;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.AiAssistant.ViewModels;

namespace DBTeam.Modules.AiAssistant.Views;

public partial class AiAssistantView : UserControl
{
    public AiAssistantView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<AiAssistantViewModel>();
    }
}
