using Cardui.Api.Dtos.Income;

namespace Cardui.Api.Services.Interfaces;

public interface IIncomeSourcesService
{
    /// <summary>
    /// Lists the signed-in household's income sources, ordered by name.
    /// Each amount is one payment, not a monthly equivalent.
    /// Raises are ordered by the date they start.
    /// </summary>
    Task<IReadOnlyList<IncomeSourceDto>> GetIncomeSourcesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an income source in the household planning currency.
    /// Typical net pay is one payment. Low and strong are optional.
    /// An expected raise is a later typical amount and does not replace the current one.
    /// The currency is copied from the household and is not chosen on the request.
    /// </summary>
    Task<IncomeSourceDto> CreateAsync(
        UpsertIncomeSourceDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an income source's payment facts, scenarios, and expected raises.
    /// The currency stored at creation stays, so a later planning-currency change does not relabel the amounts.
    /// Raises omitted from the request are removed.
    /// </summary>
    Task<IncomeSourceDto> UpdateAsync(
        Guid incomeSourceId,
        UpsertIncomeSourceDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an income source and its expected raises.
    /// Balances and other sources stay as they are.
    /// </summary>
    Task DeleteAsync(
        Guid incomeSourceId,
        CancellationToken cancellationToken = default);
}
