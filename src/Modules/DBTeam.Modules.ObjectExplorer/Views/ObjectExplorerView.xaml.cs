using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.ObjectExplorer.ViewModels;

namespace DBTeam.Modules.ObjectExplorer.Views;

public partial class ObjectExplorerView : UserControl
{
    public ObjectExplorerView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<ObjectExplorerViewModel>();
        Tree.PreviewMouseRightButtonDown += Tree_PreviewMouseRightButtonDown;
    }

    private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is ObjectExplorerViewModel vm && e.NewValue is ViewModels.TreeNodeViewModel node)
            vm.Selected = node;
    }

    private void Tree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var item = VisualUpwards<TreeViewItem>(e.OriginalSource as DependencyObject);
        if (item is not null) { item.IsSelected = true; item.Focus(); }
    }

    private static T? VisualUpwards<T>(DependencyObject? d) where T : DependencyObject
    {
        while (d is not null && d is not T) d = VisualTreeHelper.GetParent(d);
        return d as T;
    }
}
