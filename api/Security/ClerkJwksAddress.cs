namespace Cardui.Api.Security;

public static class ClerkJwksAddress
{
    /// <summary>
    /// Builds the Clerk JWKS address from an https issuer origin.
    /// The issuer must have no user info and no path.
    /// </summary>
    public static Uri Create(string issuer)
    {
        if (!Uri.TryCreate(issuer.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.UserInfo)
            || uri.PathAndQuery != "/")
        {
            throw new ClerkSessionTokenException(
                "Clerk:Issuer must be the https origin of the Clerk Frontend API.");
        }

        return new Uri(uri, "/.well-known/jwks.json");
    }
}
