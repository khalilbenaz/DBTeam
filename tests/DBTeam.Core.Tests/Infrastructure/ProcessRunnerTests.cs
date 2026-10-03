using DBTeam.Core.Infrastructure;

namespace DBTeam.Core.Tests.Infrastructure;

public sealed class ProcessRunnerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "dbteam-tests-" + Guid.NewGuid().ToString("N"));
    public ProcessRunnerTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }

    // Les tests qui pilotent `sh` sont réservés à Unix ; ceux qui pilotent git tournent partout où git est installé.
    private static bool Unix => !OperatingSystem.IsWindows();

    [Fact]
    public async Task RunAsync_ArgumentsAvecCaracteresSpeciaux_SontTransmisTelsQuels()
    {
        if (!Unix) return;
        const string arg = "a \"b\" $HOME `x` \\ ; | & 'c'\nligne2";
        var r = await ProcessRunner.RunAsync("sh", new[] { "-c", "printf %s \"$1\"", "sh", arg });
        Assert.Equal(0, r.ExitCode);
        Assert.Equal(arg, r.StdOut);
    }

    [Fact]
    public async Task RunAsync_GrosStdoutEtStderr_NeBloquePas()
    {
        if (!Unix) return;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var r = await ProcessRunner.RunAsync("sh", new[] { "-c",
            "head -c 300000 /dev/zero | tr '\\0' a; head -c 300000 /dev/zero | tr '\\0' b >&2" }, ct: cts.Token);
        Assert.Equal(300000, r.StdOut.Length);
        Assert.Equal(300000, r.StdErr.Length);
    }

    [Fact]
    public async Task RunAsync_CodeDeSortieNonNul_EstRenvoye()
    {
        if (!Unix) return;
        var r = await ProcessRunner.RunAsync("sh", new[] { "-c", "echo boom >&2; exit 3" });
        Assert.Equal(3, r.ExitCode);
        Assert.Equal("boom", r.StdErr.Trim());
    }

    [Fact]
    public async Task RunAsync_Annulation_TueLeProcessus()
    {
        if (!Unix) return;
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ProcessRunner.RunAsync("sh", new[] { "-c", "sleep 30" }, ct: cts.Token));
    }

    [Fact]
    public async Task RunAsync_ExecutableIntrouvable_Leve() =>
        await Assert.ThrowsAnyAsync<Exception>(() => ProcessRunner.RunAsync("dbteam-programme-inexistant", Array.Empty<string>()));

    [Fact]
    public async Task RunAsync_MessageDeCommitHostile_EstConserveParGit()
    {
        var init = await ProcessRunner.RunAsync("git", new[] { "init", "-q" }, _dir);
        Assert.Equal(0, init.ExitCode);
        File.WriteAllText(Path.Combine(_dir, "a.sql"), "SELECT 1;");
        await ProcessRunner.RunAsync("git", new[] { "add", "-A" }, _dir);
        const string message = "fix: \"guillemets\" $HOME `cmd` \\ n'apostrophe\n\nCorps sur plusieurs lignes";

        var commit = await ProcessRunner.RunAsync("git",
            new[] { "-c", "user.name=t", "-c", "user.email=t@t", "-c", "commit.gpgsign=false", "commit", "-m", message }, _dir);
        Assert.Equal(0, commit.ExitCode);

        var log = await ProcessRunner.RunAsync("git", new[] { "log", "-1", "--format=%B" }, _dir);
        Assert.Equal(message, log.StdOut.TrimEnd());
    }
}
