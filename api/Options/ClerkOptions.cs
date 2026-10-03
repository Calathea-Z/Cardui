namespace Cardui.Api.Options;

public class ClerkOptions
{
    public const string SectionName = "Clerk";

    public string? Issuer { get; set; }

    public string? JwtPublicKey { get; set; }

    public string[] AuthorizedParties { get; set; } = [];
}
