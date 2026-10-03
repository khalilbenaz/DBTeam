using DBTeam.Core.Models;
using DBTeam.Data.Stores;

namespace DBTeam.Core.Tests.Stores;

public sealed class JsonConnectionStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "dbteam-tests-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_dir, "connections.json");
    public JsonConnectionStoreTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }

    private JsonConnectionStore NewStore() => new(new FakeProtector(), FilePath);

    [Fact]
    public async Task SaveEtLoad_MotDePasse_EstChiffreSurDisquePuRestitue()
    {
        var store = NewStore();
        await store.SaveAsync(new[] { new SqlConnectionInfo { Name = "n", Server = "s", User = "u", Password = "secret" } });

        var raw = File.ReadAllText(FilePath);
        Assert.DoesNotContain("\"secret\"", raw);
        var loaded = await NewStore().LoadAsync();
        Assert.Equal("secret", Assert.Single(loaded).Password);
        Assert.Single(Directory.GetFiles(_dir)); // aucun fichier temporaire résiduel
    }

    [Fact]
    public async Task Load_MotDePasseIllisible_EstSignaleEtNonVideSilencieusement()
    {
        File.WriteAllText(FilePath, "[{\"Id\":\"11111111-1111-1111-1111-111111111111\",\"Name\":\"prod\",\"Server\":\"s\",\"Password\":\"blob-dpapi-autre-profil\"}]");
        var store = NewStore();

        var loaded = await store.LoadAsync();

        var c = Assert.Single(loaded);
        Assert.True(c.PasswordUnreadable);
        Assert.Null(c.Password);
        var warning = Assert.Single(store.LastLoadWarnings);
        Assert.Contains("prod", warning);
    }

    [Fact]
    public async Task Save_ApresMotDePasseIllisible_NEcrasePasLeSecretOriginal()
    {
        File.WriteAllText(FilePath, "[{\"Id\":\"11111111-1111-1111-1111-111111111111\",\"Name\":\"prod\",\"Server\":\"s\",\"Password\":\"blob-dpapi-autre-profil\"}]");
        var store = NewStore();
        var loaded = await store.LoadAsync();

        await store.SaveAsync(loaded);

        Assert.Contains("blob-dpapi-autre-profil", File.ReadAllText(FilePath));
    }

    [Fact]
    public async Task Save_NouveauMotDePasseSaisi_RemplaceLeSecretIllisible()
    {
        File.WriteAllText(FilePath, "[{\"Id\":\"11111111-1111-1111-1111-111111111111\",\"Name\":\"prod\",\"Server\":\"s\",\"Password\":\"blob\"}]");
        var store = NewStore();
        var c = (await store.LoadAsync()).Single();
        c.Password = "neuf";

        await store.SaveAsync(new[] { c });

        var again = (await NewStore().LoadAsync()).Single();
        Assert.Equal("neuf", again.Password);
        Assert.False(again.PasswordUnreadable);
    }

    [Fact]
    public async Task Load_JsonCorrompu_PreserveUneCopieEtSignale()
    {
        File.WriteAllText(FilePath, "[{\"Id\": tronqué");
        var store = NewStore();

        var loaded = await store.LoadAsync();

        Assert.Empty(loaded);
        Assert.Single(store.LastLoadWarnings);
        Assert.Single(Directory.GetFiles(_dir, "connections.json.corrupt-*"));
    }
}
