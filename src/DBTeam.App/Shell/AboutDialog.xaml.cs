using System.Windows;

namespace DBTeam.App.Shell;

public partial class AboutDialog : Window
{
    public AboutDialog() { InitializeComponent(); }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
