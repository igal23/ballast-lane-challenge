using TaskBoard.Infrastructure.Security;

namespace TaskBoard.UnitTests.Infrastructure;

public sealed class AspNetPasswordHasherTests
{
    [Fact]
    public void HashAndVerify_UsesOneWayAdaptiveHash()
    {
        var hasher = new AspNetPasswordHasher();
        const string password = "correct horse battery staple";

        var hash = hasher.Hash(password);

        Assert.NotEqual(password, hash);
        Assert.True(hasher.Verify(hash, password));
        Assert.False(hasher.Verify(hash, "wrong password"));
    }

    [Fact]
    public void Verify_NullStoredHash_ReturnsFalse()
    {
        var hasher = new AspNetPasswordHasher();

        Assert.False(hasher.Verify(null, "supplied password"));
    }
}
