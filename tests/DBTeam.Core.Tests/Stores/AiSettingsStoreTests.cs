using DBTeam.Modules.AiAssistant.Engine;

namespace DBTeam.Core.Tests.Stores;

public sealed class AiSettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "dbteam-tests-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_dir, "ai.json");
    public AiSettingsStoreTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }

    private AiSettingsStore NewStore() => new(new FakeProtector(), FilePath);

    [Fact]
    public void SaveEtLoad_CleApi_EstChiffreeSurDisquePuisRestituee()
    {
        NewStore().Save(new AiSettings { ApiKey = "sk-123" });
        Assert.DoesNotContain("sk-123", File.ReadAllText(FilePath).Replace("enc:sk-123", ""));
        Assert.Equal("sk-123", NewStore().Load().ApiKey);
        Assert.Single(Directory.GetFiles(_dir));
    }

    [Fact]
    public void Load_CleIllisible_EstSignalee()
    {
        File.WriteAllText(FilePath, "{\"ApiKey\":\"blob-autre-profil\",\"MaxTokens\":10}");
        var store = NewStore();
        var s = store.Load();
        Assert.Equal("", s.ApiKey);
        Assert.NotNull(store.LastWarning);
    }

    [Fact]
    public void Save_ApresCleIllisible_NEcrasePasLeSecretOriginal()
    {
        File.WriteAllText(FilePath, "{\"ApiKey\":\"blob-autre-profil\",\"MaxTokens\":10}");
        var store = NewStore();
        var s = store.Load();
        store.Save(s);
        Assert.Contains("blob-autre-profil", File.ReadAllText(FilePath));
    }

    [Fact]
    public void Save_ErreurDEcriture_EstPropagee()
    {
        var store = new AiSettingsStore(new FakeProtector(), Path.Combine(_dir, "ai.json", "impossible", "x.json"));
        File.WriteAllText(FilePath, "x");
        Assert.ThrowsAny<IOException>(() => store.Save(new AiSettings()));
    }
}
