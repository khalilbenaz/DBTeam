using DBTeam.Core.Infrastructure;

namespace DBTeam.Core.Tests.Infrastructure;

public sealed class AtomicFileTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "dbteam-tests-" + Guid.NewGuid().ToString("N"));
    public AtomicFileTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }

    [Fact]
    public async Task WriteAllTextAsync_FichierAbsent_LeCree()
    {
        var p = Path.Combine(_dir, "a.json");
        await AtomicFile.WriteAllTextAsync(p, "{}");
        Assert.Equal("{}", File.ReadAllText(p));
        Assert.Single(Directory.GetFiles(_dir));
    }

    [Fact]
    public async Task WriteAllTextAsync_FichierExistant_LeRemplaceSansLaisserDeTemporaire()
    {
        var p = Path.Combine(_dir, "a.json");
        File.WriteAllText(p, "ancien");
        await AtomicFile.WriteAllTextAsync(p, "nouveau");
        Assert.Equal("nouveau", File.ReadAllText(p));
        Assert.Single(Directory.GetFiles(_dir));
    }

    [Fact]
    public async Task WriteAsync_EcritureInterrompue_ConserveLAncienContenu()
    {
        var p = Path.Combine(_dir, "a.json");
        File.WriteAllText(p, "ancien");
        await Assert.ThrowsAsync<InvalidOperationException>(() => AtomicFile.WriteAsync(p, async (s, ct) =>
        {
            await s.WriteAsync(new byte[] { 1, 2, 3 }, ct);
            throw new InvalidOperationException("crash en cours d'écriture");
        }));
        Assert.Equal("ancien", File.ReadAllText(p));
        Assert.Single(Directory.GetFiles(_dir));
    }

    [Fact]
    public void WriteAllText_Synchrone_RemplaceLeFichier()
    {
        var p = Path.Combine(_dir, "b.json");
        AtomicFile.WriteAllText(p, "1");
        AtomicFile.WriteAllText(p, "2");
        Assert.Equal("2", File.ReadAllText(p));
        Assert.Single(Directory.GetFiles(_dir));
    }
}
