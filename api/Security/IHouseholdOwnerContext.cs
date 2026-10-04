namespace Cardui.Api.Security;

public interface IHouseholdOwnerContext
{
    /// <summary>
    /// Clerk user id from the authenticated request.
    /// Throws when the request has no signed-in owner.
    /// </summary>
    string ClerkUserId { get; }
}
