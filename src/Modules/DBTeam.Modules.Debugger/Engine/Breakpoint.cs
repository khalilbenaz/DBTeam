using CommunityToolkit.Mvvm.ComponentModel;

namespace DBTeam.Modules.Debugger.Engine;

public partial class Breakpoint : ObservableObject
{
    [ObservableProperty] private int line;
    [ObservableProperty] private string? condition;
    [ObservableProperty] private int hitCount;

    public bool IsConditional => !string.IsNullOrWhiteSpace(Condition);
}
