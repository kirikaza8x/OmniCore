using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;

namespace OmniCore.Services.Auth.Domain.Tests;

// ADR 0003 step 1: the raw refresh token must never be stored.
public class RefreshTokenHashingTests
{
    private static RefreshToken NewToken(string raw) =>
        RefreshToken.Create(AccountId.New(), raw, TimeSpan.FromDays(7)).Value;

    [Fact]
    public void StoredValue_IsTheSha256HexDigest_NotTheRawToken()
    {
        var token = NewToken("my-raw-token");

        Assert.NotEqual("my-raw-token", token.TokenHash);
        Assert.Equal(64, token.TokenHash.Length);
        Assert.Matches("^[0-9a-f]{64}$", token.TokenHash);
        // Known SHA-256 of "abc": the same function the database migration computes for existing rows.
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", RefreshToken.HashToken("abc"));
    }

    [Fact]
    public void Matches_AcceptsOnlyTheOriginalToken()
    {
        var token = NewToken("my-raw-token");

        Assert.True(token.Matches("my-raw-token"));
        Assert.False(token.Matches("my-raw-token "));
        Assert.False(token.Matches("MY-RAW-TOKEN"));
        Assert.False(token.Matches(token.TokenHash));   // presenting the stored hash is not a way in
    }

    [Fact]
    public void Revoke_StoresTheHashOfTheReplacement()
    {
        var token = NewToken("old");

        token.Revoke("new");

        Assert.Equal(RefreshToken.HashToken("new"), token.ReplacedByTokenHash);
    }
}
