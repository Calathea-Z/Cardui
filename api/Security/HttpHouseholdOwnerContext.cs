namespace Cardui.Api.Security;

public sealed class HttpHouseholdOwnerContext : IHouseholdOwnerContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpHouseholdOwnerContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string ClerkUserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?
                .User
                .FindFirst(ClerkAuthenticationDefaults.UserIdClaimType)?
                .Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ClerkSessionTokenException("A signed-in owner is required.");
            }

            return userId;
        }
    }
}
