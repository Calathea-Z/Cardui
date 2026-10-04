using Cardui.Api.Dtos.Household;

namespace Cardui.Api.Services.Interfaces;

public interface IHouseholdsService
{
    /// <summary>
    /// Returns the household owned by this Clerk user, creating one named
    /// "My household" when none exists. A concurrent create returns the row
    /// that won the unique owner constraint.
    /// </summary>
    Task<HouseholdDto> GetOrCreateForOwnerAsync(
        string ownerClerkUserId,
        CancellationToken cancellationToken = default);
}
