using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;

namespace DBTeam.Modules.ObjectExplorer.ViewModels;

public partial class TreeNodeViewModel : ObservableObject
{
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private bool isExpanded;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isSelected;

    public DbObjectKind Kind { get; set; }
    public SqlConnectionInfo? Connection { get; set; }
    public string? Database { get; set; }
    public string? Schema { get; set; }
    public string? ObjectName { get; set; }
    public Func<TreeNodeViewModel, Task>? Loader { get; set; }
    public bool IsLoaded { get; set; }

    public ObservableCollection<TreeNodeViewModel> Children { get; } = new();

    partial void OnIsExpandedChanged(bool value)
    {
        if (value && !IsLoaded && Loader is not null)
        {
            _ = LoadChildrenAsync();
        }
    }

    public async Task LoadChildrenAsync()
    {
        if (IsLoaded || Loader is null) return;
        IsLoading = true;
        try { await Loader(this); IsLoaded = true; }
        finally { IsLoading = false; }
    }
}
