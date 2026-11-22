namespace OmniCore.Services.User.Application.Features.Profiles.Commands.UploadMyAvatar;

/// <summary>Recognises JPEG, PNG and WebP by their first bytes, so a renamed file cannot pass as an image (finding U5).</summary>
public static class ImageSignature
{
    /// <summary>Returns the real content type, or null when the stream is not one of the allowed images.</summary>
    public static string? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return "image/jpeg";

        if (header.Length >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
            && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            return "image/png";

        // RIFF....WEBP
        if (header.Length >= 12
            && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
            && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P')
            return "image/webp";

        return null;
    }

    public static string Extension(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        _ => ".webp"
    };
}
