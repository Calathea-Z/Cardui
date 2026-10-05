using Cardui.Api.Dtos.Household;

namespace Cardui.Api.Services.Interfaces;

public interface IFinancialProfileService
{
    Task<FinancialProfileDto> GetAsync(CancellationToken cancellationToken = default);

    Task<FinancialProfileDto> UpdateAsync(
        UpdateFinancialProfileDto dto,
        CancellationToken cancellationToken = default);

    Task<HouseholdContributorDto> AddContributorAsync(
        UpsertHouseholdContributorDto dto,
        CancellationToken cancellationToken = default);

    Task<HouseholdContributorDto> UpdateContributorAsync(
        Guid contributorId,
        UpsertHouseholdContributorDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a contributor from the household.
    /// Income sources that named this person stay, with no contributor.
    /// </summary>
    Task RemoveContributorAsync(
        Guid contributorId,
        CancellationToken cancellationToken = default);
}
