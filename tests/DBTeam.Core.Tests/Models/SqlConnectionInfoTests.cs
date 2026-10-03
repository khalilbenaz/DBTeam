using DBTeam.Core.Models;

namespace DBTeam.Core.Tests.Models;

public class SqlConnectionInfoTests
{
    [Fact]
    public void NouvelleConnexion_NeFaitPasConfianceAuCertificatParDefaut() =>
        Assert.False(new SqlConnectionInfo().TrustServerCertificate);

    [Fact]
    public void NouvelleConnexion_ChiffreParDefaut() =>
        Assert.True(new SqlConnectionInfo().Encrypt);
}
