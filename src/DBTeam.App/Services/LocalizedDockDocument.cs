using System.Collections.Generic;
using System.Windows;
using AvalonDock.Layout;
using DBTeam.Core.Infrastructure;

namespace DBTeam.App.Services;

/// <summary>
/// Binds AvalonDock LayoutContent titles (LayoutDocument, LayoutAnchorable) to
/// resource keys so they retranslate when the user changes language. DynamicResource
/// in XAML does not follow ResourceDictionary swaps on non-visual LayoutContent, so
/// we maintain a map and reapply on LocalizationService.LanguageChanged.
/// </summary>
public static class LocalizedDockDocument
{
    private static readonly Dictionary<LayoutContent, (string key, string fallback)> _bindings = new();
    private static bool _hooked;

    public static void Bind(LayoutContent item, string key, string fallback)
    {
        _bindings[item] = (key, fallback);
        item.Title = LocalizationService.T(key, fallback);
        item.Closed += (_, _) => _bindings.Remove(item);
        EnsureHooked();
    }

    private static void EnsureHooked()
    {
        if (_hooked) return;
        var loc = ServiceLocator.TryGet<LocalizationService>();
        if (loc is null) return;
        loc.LanguageChanged += ApplyAll;
        _hooked = true;
    }

    private static void ApplyAll()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            foreach (var kv in _bindings)
                kv.Key.Title = LocalizationService.T(kv.Value.key, kv.Value.fallback);
        });
    }
}
