using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Storage;

namespace OmniCore.Services.User.Application.Tests;

internal static class TestSupport
{
    public static UserProfile NewProfile(Guid? id = null, string username = "alice_01")
    {
        var result = UserProfile.Create(id ?? Guid.NewGuid(), username, "alice@example.com");
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    /// <summary>A signed-in caller, or an anonymous one when no id is given.</summary>
    public static ICurrentUserService Caller(Guid? userId)
    {
        var caller = Substitute.For<ICurrentUserService>();
        caller.UserId.Returns(userId);
        return caller;
    }

    /// <summary>A repository that returns <paramref name="profile"/> for any id (null: no profile).</summary>
    public static IUserProfileRepository Repository(UserProfile? profile)
    {
        var repo = Substitute.For<IUserProfileRepository>();
        repo.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(profile);
        repo.GetReadOnlyAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(profile);
        return repo;
    }

    public sealed class FakeUpload(byte[] bytes, string fileName = "a.png", string contentType = "image/png") : IFileUpload
    {
        public string FileName => fileName;
        public string ContentType => contentType;
        public long Length => bytes.Length;
        public Stream OpenReadStream() => new MemoryStream(bytes);
    }

    public static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 1, 2, 3];
    public static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0, 0, 0, 0, 0, 0, 1];
    public static readonly byte[] WebpBytes = [(byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P', 1];
    public static readonly byte[] ExeBytes = [(byte)'M', (byte)'Z', 0x90, 0, 3, 0, 0, 0, 4, 0, 0, 0, 0xFF, 0xFF];
}
