using System.Windows;

namespace DBTeam.Modules.Debugger.Views;

public partial class BreakpointConditionWindow : Window
{
    public string Condition { get; private set; } = "";

    public BreakpointConditionWindow(int line, string? current)
    {
        InitializeComponent();
        LineLabel.Text = $"Line {line}";
        ConditionBox.Text = current ?? "";
        Loaded += (_, _) => { ConditionBox.Focus(); ConditionBox.SelectAll(); };
    }

    private void Ok_Click(object sender, RoutedEventArgs e) { Condition = ConditionBox.Text?.Trim() ?? ""; DialogResult = true; }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; }
}
