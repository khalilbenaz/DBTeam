using System.Collections.Generic;
using System.Windows;
using AvalonDock.Layout;
using DBTeam.Core.Infrastructure;

namespace DBTeam.App.Services;

/// <summary>
/// Binds AvalonDock LayoutDocument titles to resource keys so they retranslate
/// when the user changes language. AvalonDock's LayoutDocument.Title is a plain
/// CLR property, not a DP; we maintain a map and reapply on LanguageChanged.
/// </summary>
public static class LocalizedDockDocument
{
    private static readonly Dictionary<LayoutDocument, (string key, string fallback)> _bindings = new();
    private static bool _hooked;

    public static void Bind(LayoutDocument doc, string key, string fallback)
    {
        _bindings[doc] = (key, fallback);
        doc.Title = LocalizationService.T(key, fallback);
        doc.Closed += (_, _) => _bindings.Remove(doc);

        if (!_hooked)
        {
            var loc = ServiceLocator.TryGet<LocalizationService>();
            if (loc is not null)
            {
                loc.LanguageChanged += ApplyAll;
                _hooked = true;
            }
        }
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
