using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Modules.Product.Application.Services;

public static class SlugHelper
{
    public static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "product";

        var normalized = name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(char.IsLetterOrDigit(character) ? character : '-');
        }

        var slug = Regex.Replace(builder.ToString(), "-+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "product" : slug;
    }
}
