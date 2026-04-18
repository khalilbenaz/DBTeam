using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace DBTeam.Modules.Debugger.Views;

public partial class DebuggerView : UserControl
{
    public DebuggerView() { InitializeComponent(); }

    private void OpenIssue_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/khalilbenaz/DBTeam/issues/13") { UseShellExecute = true });
        }
        catch { }
    }
}
