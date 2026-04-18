using System;
using System.Threading.Tasks;

namespace DBTeam.App.Services;

/// <summary>
/// Lightweight auto-update skeleton. When Velopack is wired in (NuGet: Velopack),
/// replace the body of <see cref="CheckAsync"/> with:
///
/// <code>
/// using Velopack;
/// using Velopack.Sources;
/// var mgr = new UpdateManager(new GithubSource("https://github.com/khalilbenaz/DBTeam", null, prerelease: false));
/// var info = await mgr.CheckForUpdatesAsync();
/// if (info is null) return (null, null);
/// await mgr.DownloadUpdatesAsync(info);
/// return (info.TargetFullRelease.Version.ToString(), () => mgr.ApplyUpdatesAndRestart(info));
/// </code>
///
/// Also call <c>VelopackApp.Build().Run();</c> at the very top of <c>Main()</c>.
/// Velopack also requires publishing via <c>vpk pack</c> instead of plain
/// <c>dotnet publish</c> — see docs/bmad/DESIGN-NOTES.md#e16.
/// </summary>
public sealed class UpdateService
{
    public Task<(string? newVersion, Action? applyAndRestart)> CheckAsync()
        => Task.FromResult<(string?, Action?)>((null, null));

    public bool IsEnabled => false;
}
