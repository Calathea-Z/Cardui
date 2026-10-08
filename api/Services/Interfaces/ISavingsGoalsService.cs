using Cardui.Api.Domain.Savings;
using Cardui.Api.Dtos.Savings;

namespace Cardui.Api.Services.Interfaces;

public interface ISavingsGoalsService
{
    /// <summary>
    /// Lists the signed-in household's savings goals.
    /// Operating cash, then the emergency goal, then sinking funds by name.
    /// The amount in use is the followed balance or the typed amount. The monthly amount is calculated.
    /// </summary>
    Task<IReadOnlyList<SavingsGoalDto>> GetGoalsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists cash accounts the household can follow.
    /// An account already followed by a goal names that goal. Credit, loans, and investments are left out.
    /// </summary>
    Task<IReadOnlyList<SavingsAccountDto>> GetAccountsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The reserve and later contributions Plan's cash outlook reads.
    /// A contribution raises the reserve and does not reduce cash.
    /// </summary>
    Task<SavingsOutlook> GetOutlookAsync(
        DateOnly today,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a goal in the household planning currency.
    /// Saving it does not create a transaction or change an account balance.
    /// </summary>
    Task<SavingsGoalDto> CreateAsync(
        UpsertSavingsGoalDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a goal's target, date, typed amount, and followed account.
    /// The kind and the currency stored at creation stay.
    /// </summary>
    Task<SavingsGoalDto> UpdateAsync(
        Guid goalId,
        UpsertSavingsGoalDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a goal.
    /// The account and its balance stay as they are.
    /// </summary>
    Task DeleteAsync(
        Guid goalId,
        CancellationToken cancellationToken = default);
}
