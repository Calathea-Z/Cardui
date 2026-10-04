using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Cardui.Api.Security;

public sealed class ClerkAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ClerkSessionTokenValidator _validator;

    public ClerkAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ClerkSessionTokenValidator validator)
        : base(options, logger, encoder)
    {
        _validator = validator;
    }

    /// <summary>
    /// Authenticates a Bearer Clerk session token. A missing header is left
    /// for a later challenge. An invalid token fails authentication.
    /// </summary>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = header[prefix.Length..].Trim();
        if (token.Length == 0)
        {
            return AuthenticateResult.Fail("A session token is required.");
        }

        try
        {
            var principal = await _validator.ValidateAsync(token, Context.RequestAborted);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
        catch (ClerkSessionTokenException exception)
        {
            Logger.LogInformation(
                exception,
                "Rejected a Clerk session token.");
            return AuthenticateResult.Fail("The session token is invalid.");
        }
    }
}
