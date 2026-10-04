namespace Cardui.Api.Domain;

public static class HouseholdTime
{
    public const string DefaultTimeZoneId = "America/Denver";
    public const int TimeZoneIdMaxLength = 64;

    public static bool TryNormalizeTimeZoneId(string? value, out string timeZoneId)
    {
        timeZoneId = "";
        var candidate = value?.Trim() ?? "";
        if (candidate.Length == 0 || candidate.Length > TimeZoneIdMaxLength)
        {
            return false;
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(candidate, out _))
        {
            return false;
        }

        timeZoneId = candidate;
        return true;
    }
}
