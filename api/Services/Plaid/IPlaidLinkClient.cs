using Going.Plaid.Link;

namespace Cardui.Api.Services.Plaid;

public interface IPlaidLinkClient
{
    /// <summary>
    /// Asks Plaid for a Link token. The request already includes credentials.
    /// Returns the link token only.
    /// </summary>
    Task<string?> CreateAsync(
        LinkTokenCreateRequest request,
        CancellationToken cancellationToken = default);
}
