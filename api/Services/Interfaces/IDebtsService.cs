using Cardui.Api.Dtos.Debts;

namespace Cardui.Api.Services.Interfaces;

public interface IDebtsService
{
    /// <summary>
    /// Lists the signed-in household's debts, ordered by name.
    /// A null term is unknown. Utilization uses the balance in use and the credit limit when both are known.
    /// A followed debt's balance in use is the connected balance, except where the person kept their own.
    /// </summary>
    Task<IReadOnlyList<DebtDto>> GetDebtsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a debt in the household planning currency.
    /// The linked account is optional and must belong to this household.
    /// The currency is copied from the household and is not chosen on the request.
    /// A blank term is stored as unknown. The linked account balance is not changed.
    /// </summary>
    Task<DebtDto> CreateAsync(
        UpsertDebtDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a debt's type, balance, terms, and linked account.
    /// The currency stored at creation stays, so a later planning-currency change does not relabel the amounts.
    /// Clearing a term stores it as unknown. The linked account balance is not changed.
    /// While the debt is following, the balance and the linked account stay as they are.
    /// </summary>
    Task<DebtDto> UpdateAsync(
        Guid debtId,
        UpsertDebtDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a debt.
    /// The linked account and its balance stay as they are.
    /// </summary>
    Task DeleteAsync(
        Guid debtId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the summary of the household's debts.
    /// Interest and utilization use each debt's balance in use.
    /// A reference link shows the account balance when it differs, and it is not copied until chosen.
    /// A followed balance that is not current is still counted, and the summary names how many are not current.
    /// </summary>
    Task<DebtSummaryReportDto> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uses the linked account's balance for this debt.
    /// When that account can be followed, the debt starts following it and the stored balance stays.
    /// Otherwise the dated balance is copied onto the debt once. APR, minimum, and due date stay as they are.
    /// A debt that already follows the account is rejected.
    /// </summary>
    Task<DebtDto> UseAccountBalanceAsync(
        Guid debtId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the connected credit cards and loans this debt may follow.
    /// An account already followed by another debt is left out. A manual account is left out.
    /// </summary>
    Task<IReadOnlyList<DebtFollowAccountDto>> GetFollowAccountsAsync(
        Guid debtId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes the debt follow one eligible account.
    /// The debt's stored balance stays. KeepOwnBalance records it as the person's value when the amounts differ.
    /// </summary>
    Task<DebtDto> FollowAccountAsync(
        Guid debtId,
        FollowDebtAccountDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops following and keeps the last balance on the debt.
    /// A synced balance is copied once. An override stays as the person's value. The account link remains.
    /// </summary>
    Task<DebtDto> StopFollowingAsync(
        Guid debtId,
        CancellationToken cancellationToken = default);
}
