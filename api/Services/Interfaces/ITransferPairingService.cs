namespace Cardui.Api.Services.Interfaces;

public interface ITransferPairingService
{
    /// <summary>
    /// Finds opposite-signed, same-amount transactions across the bound
    /// household's cash, investment, and credit-card accounts and marks both legs as Transfers.
    /// A described card payment is also marked as a Transfer when its category is still open.
    /// Returns the number of pairs and promotions.
    /// </summary>
    Task<int> PairOwnedAccountTransfersAsync(CancellationToken cancellationToken = default);
}
