namespace OmniCore.Shared.Application.Helpers;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static partial class SlugHelper
{
    public static string Generate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        string cleaned = NonAlphanumericRegex()
            .Replace(sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant(), "-");

        return MultipleHyphensRegex()
            .Replace(cleaned, "-")
            .Trim('-');
    }

    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"-+")]
    private static partial Regex MultipleHyphensRegex();
}