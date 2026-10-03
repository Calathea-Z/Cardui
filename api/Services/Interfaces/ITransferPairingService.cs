namespace Cardui.Api.Services.Interfaces;

public interface ITransferPairingService
{
    /// <summary>
    /// Finds opposite-signed, same-amount transactions across the bound
    /// household's owned asset accounts and marks both legs as Transfers.
    /// Returns the number of pairs linked.
    /// </summary>
    Task<int> PairOwnedAccountTransfersAsync(CancellationToken cancellationToken = default);
}
