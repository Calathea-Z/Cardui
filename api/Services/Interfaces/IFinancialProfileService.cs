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

    Task RemoveContributorAsync(
        Guid contributorId,
        CancellationToken cancellationToken = default);
}
