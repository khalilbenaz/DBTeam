using DBTeam.Core.Abstractions;

namespace DBTeam.Core.Tests.Stores;

/// <summary>Protège en préfixant "enc:" ; lève SecretUnreadableException pour tout autre format (profil/machine différent).</summary>
internal sealed class FakeProtector : ISecretProtector
{
    public string Protect(string plain) => "enc:" + plain;
    public string Unprotect(string protectedValue) =>
        protectedValue.StartsWith("enc:", StringComparison.Ordinal)
            ? protectedValue[4..]
            : throw new SecretUnreadableException("illisible");
}
