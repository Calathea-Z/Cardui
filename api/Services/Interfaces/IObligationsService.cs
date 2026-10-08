using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Obligations;

namespace Cardui.Api.Services.Interfaces;

public interface IObligationsService
{
    /// <summary>
    /// Lists the signed-in household's bills, ordered by name.
    /// Each stored amount is one payment. A monthly equivalent is not returned.
    /// </summary>
    Task<IReadOnlyList<ObligationDto>> GetObligationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the household's bills for Plan's cash outlook: amount, cadence,
    /// and next due date. Each amount is one payment.
    /// </summary>
    Task<IReadOnlyList<DatedBill>> GetOutlookBillsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists recurring payments noticed in activity that are not bills yet.
    /// A suggestion is not an obligation. It stays off the bill list until the person adds it.
    /// </summary>
    Task<IReadOnlyList<ObligationSuggestionDto>> GetSuggestionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remembers that a suggested payment is not a bill.
    /// The same pattern is not suggested again. No bill is created.
    /// </summary>
    Task DismissSuggestionAsync(
        string? key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a bill in the household planning currency.
    /// The amount is one payment. The source account is optional and must belong to this household.
    /// The currency is copied from the household and is not chosen on the request.
    /// An optional suggestion key ties the bill to a pattern so that pattern is not suggested again.
    /// Saving the bill is the confirmation. The key alone does not make it a bill.
    /// </summary>
    Task<ObligationDto> CreateAsync(
        UpsertObligationDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a bill's amount, schedule, source account, and essential or flexible classification.
    /// The currency stored at creation stays, so a later planning-currency change does not relabel the amount.
    /// </summary>
    Task<ObligationDto> UpdateAsync(
        Guid obligationId,
        UpsertObligationDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a bill.
    /// The source account and its balance stay as they are.
    /// </summary>
    Task DeleteAsync(
        Guid obligationId,
        CancellationToken cancellationToken = default);
}
