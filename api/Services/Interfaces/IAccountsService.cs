using Cardui.Api.Dtos.Account;

namespace Cardui.Api.Services.Interfaces;

public interface IAccountsService
{
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(
        CancellationToken cancellationToken = default);

    Task<AccountSummaryDto> GetAccountsSummaryAsync(
        CancellationToken cancellationToken = default);
}
