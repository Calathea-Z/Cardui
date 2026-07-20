namespace Cardui.Api.Domain;

public static class CategoryKeys
{
    public static string CreateFromName(string name)
    {
        return string.Join(
            "-",
            name.Trim()
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
