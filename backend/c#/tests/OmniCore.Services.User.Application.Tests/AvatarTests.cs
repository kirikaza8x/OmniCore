using Microsoft.Extensions.Logging.Abstractions;
using OmniCore.Services.User.Application.Features.Profiles.Commands.UploadMyAvatar;
using OmniCore.Shared.Application.Abstractions.Storage;
using OmniCore.Shared.Domain.Repositories;

namespace OmniCore.Services.User.Application.Tests;

public class AvatarTests
{
    private static readonly Guid Me = Guid.NewGuid();

    [Theory]
    [InlineData("jpeg")]
    [InlineData("png")]
    [InlineData("webp")]
    public void Real_images_are_recognised_by_their_first_bytes(string kind)
    {
        var (bytes, expected) = kind switch
        {
            "jpeg" => (TestSupport.JpegBytes, "image/jpeg"),
            "png" => (TestSupport.PngBytes, "image/png"),
            _ => (TestSupport.WebpBytes, "image/webp")
        };

        Assert.Equal(expected, ImageSignature.Detect(bytes));
    }

    [Fact]
    public void Other_files_and_short_files_are_not_images()
    {
        Assert.Null(ImageSignature.Detect(TestSupport.ExeBytes));
        Assert.Null(ImageSignature.Detect([0xFF, 0xD8]));
        Assert.Null(ImageSignature.Detect([]));
    }

    private static (UploadMyAvatarCommandHandler Handler, IStorageService Storage, IUnitOfWork Uow) Create(Domain.Entities.UserProfile? profile)
    {
        var storage = Substitute.For<IStorageService>();
        storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new UploadResult("avatars/new.png", "https://cdn.example.com/avatars/new.png", "new.png", "image/png", 10));
        var uow = Substitute.For<IUnitOfWork>();
        var handler = new UploadMyAvatarCommandHandler(
            TestSupport.Repository(profile), TestSupport.Caller(Me), storage, uow, NullLogger<UploadMyAvatarCommandHandler>.Instance);
        return (handler, storage, uow);
    }

    [Fact]
    public async Task A_renamed_executable_is_rejected_even_when_the_client_says_image_png()
    {
        var (handler, storage, _) = Create(TestSupport.NewProfile(Me));

        var result = await handler.Handle(
            new UploadMyAvatarCommand(new TestSupport.FakeUpload(TestSupport.ExeBytes, "evil.png", "image/png")), default);

        Assert.True(result.IsFailure);
        await storage.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_png_is_uploaded_with_its_real_content_type_saved_and_the_old_file_deleted_afterwards()
    {
        var profile = TestSupport.NewProfile(Me);
        profile.UpdateAvatar("https://cdn.example.com/avatars/old.png");
        var (handler, storage, uow) = Create(profile);
        var order = new List<string>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ => { order.Add("save"); return Task.FromResult(1); });
        storage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => { order.Add("delete"); return Task.CompletedTask; });

        var result = await handler.Handle(new UploadMyAvatarCommand(new TestSupport.FakeUpload(TestSupport.PngBytes)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://cdn.example.com/avatars/new.png", profile.AvatarUrl);
        await storage.Received(1).UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), "image/png", "avatars", Arg.Any<CancellationToken>());
        Assert.Equal(["save", "delete"], order);
    }

    [Fact]
    public async Task A_failing_delete_of_the_old_file_does_not_fail_the_upload()
    {
        var profile = TestSupport.NewProfile(Me);
        profile.UpdateAvatar("https://cdn.example.com/avatars/old.png");
        var (handler, storage, _) = Create(profile);
        storage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new IOException("gone")));

        var result = await handler.Handle(new UploadMyAvatarCommand(new TestSupport.FakeUpload(TestSupport.PngBytes)), default);

        Assert.True(result.IsSuccess);
    }
}
