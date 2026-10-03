using Cardui.Api.Dtos.Household;

namespace Cardui.Api.Services.Interfaces;

public interface IHouseholdsService
{
    Task<HouseholdDto> GetOrCreateForOwnerAsync(
        string ownerClerkUserId,
        CancellationToken cancellationToken = default);
}
