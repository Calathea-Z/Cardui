using Going.Plaid;

namespace Cardui.Api.Services.Plaid;

public interface IPlaidClientSource
{
    /// <summary>
    /// Returns the Plaid client when credentials are configured.
    /// </summary>
    PlaidClient GetClient();
}
