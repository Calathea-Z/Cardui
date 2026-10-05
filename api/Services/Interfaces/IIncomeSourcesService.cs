using Cardui.Api.Dtos.Income;

namespace Cardui.Api.Services.Interfaces;

public interface IIncomeSourcesService
{
    /// <summary>
    /// Lists the signed-in household's income sources, ordered by name.
    /// Each amount is one payment, not a monthly equivalent.
    /// </summary>
    Task<IReadOnlyList<IncomeSourceDto>> GetIncomeSourcesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an income source in the household planning currency.
    /// The currency is copied from the household and is not chosen on the request.
    /// </summary>
    Task<IncomeSourceDto> CreateAsync(
        UpsertIncomeSourceDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an income source's payment facts.
    /// The currency stored at creation stays, so a later planning-currency change does not relabel the amount.
    /// </summary>
    Task<IncomeSourceDto> UpdateAsync(
        Guid incomeSourceId,
        UpsertIncomeSourceDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an income source.
    /// The row is removed. Balances and other sources stay as they are.
    /// </summary>
    Task DeleteAsync(
        Guid incomeSourceId,
        CancellationToken cancellationToken = default);
}
