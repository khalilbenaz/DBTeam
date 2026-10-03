using System;
using System.Security.Cryptography;
using System.Text;
using DBTeam.Core.Abstractions;

namespace DBTeam.Data.Security;

public sealed class DpapiProtector : ISecretProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DBTeam.v1");

    public string Protect(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return string.Empty;
        var data = Encoding.UTF8.GetBytes(plain);
        var enc = ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(enc);
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrEmpty(protectedValue)) return string.Empty;
        try
        {
            var data = Convert.FromBase64String(protectedValue);
            var dec = ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(dec);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            throw new SecretUnreadableException(
                "Secret illisible : il a été chiffré par un autre profil Windows ou une autre machine, ou est altéré.", ex);
        }
    }
}
