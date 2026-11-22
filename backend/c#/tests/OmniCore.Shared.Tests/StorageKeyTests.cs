using OmniCore.Shared.Infrastructure.Services.Storage;

namespace OmniCore.Shared.Tests;

public class StorageKeyTests
{
    [Theory]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("C:\\temp\\cv final.pdf", "cv_final.pdf")]
    [InlineData("..hidden", "hidden")]
    [InlineData("a..b..c.png", "a.b.c.png")]
    [InlineData("", "file")]
    [InlineData("???", "___")]
    public void File_names_are_reduced_to_a_safe_last_segment(string input, string expected)
    {
        Assert.Equal(expected, MinioStorageService.SanitizeFileName(input));
    }

    [Fact]
    public void Long_file_names_are_cut()
    {
        Assert.True(MinioStorageService.SanitizeFileName(new string('a', 500) + ".png").Length <= 100);
    }

    [Theory]
    [InlineData("avatars", "avatars")]
    [InlineData("/avatars/", "avatars")]
    [InlineData("../other-user", "other-user")]
    [InlineData("a/../b", "a/b")]
    [InlineData("we ird?/x", "weird/x")]
    [InlineData(null, "")]
    [InlineData("  ", "")]
    public void Folders_cannot_climb_or_carry_odd_characters(string? input, string expected)
    {
        Assert.Equal(expected, MinioStorageService.SanitizeFolder(input));
    }

    [Fact]
    public void An_object_key_is_unique_per_call_and_stays_inside_its_folder()
    {
        var a = MinioStorageService.GenerateObjectKey("pic.png", "../avatars");
        var b = MinioStorageService.GenerateObjectKey("pic.png", "../avatars");

        Assert.NotEqual(a, b);
        Assert.StartsWith("avatars/", a);
        Assert.DoesNotContain("..", a);
        Assert.EndsWith("_pic.png", a);
    }
}
