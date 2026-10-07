using System.Text;

namespace Cardui.Api.Domain.Categories;

public static class CategoryKeys
{
    /// <summary>
    /// Turns a display name into a lowercase key, with words separated by hyphens.
    /// </summary>
    public static string CreateFromName(string name)
    {
        var builder = new StringBuilder(name.Length);
        foreach (var ch in name.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (char.IsWhiteSpace(ch) || ch is '-' or '&')
            {
                builder.Append(' ');
            }
        }

        return string.Join(
            "-",
            builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
