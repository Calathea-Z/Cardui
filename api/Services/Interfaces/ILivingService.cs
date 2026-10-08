using Cardui.Api.Domain.Living;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Living;

namespace Cardui.Api.Services.Interfaces;

public interface ILivingService
{
    /// <summary>
    /// Returns the signed-in household's contribution shares, monthly living spending, and affordability gap.
    /// Paychecks keep their dates. Saving here moves no money.
    /// </summary>
    Task<LivingPageDto> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores one person's monthly contribution and returns the page.
    /// A null amount clears it so their full recorded pay stays shared. The paycheck dates stay.
    /// </summary>
    Task<LivingPageDto> SetContributionAsync(
        Guid contributorId,
        UpsertLivingContributionDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scales the household's dated paychecks by each contributor's current share.
    /// A future raise and low pay keep the same share; unassigned and irregular pay stay whole.
    /// </summary>
    Task<IReadOnlyList<HouseholdIncome>> GetSharedIncomesAsync(
        IReadOnlyList<HouseholdIncome> incomes,
        CancellationToken cancellationToken = default);
}
