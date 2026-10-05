using Cardui.Api.Dtos.Debts;

namespace Cardui.Api.Services.Interfaces;

public interface IDebtsService
{
    /// <summary>
    /// Lists the signed-in household's debts, ordered by name.
    /// A null term is unknown. Utilization is calculated when the balance and credit limit are both known.
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
}
