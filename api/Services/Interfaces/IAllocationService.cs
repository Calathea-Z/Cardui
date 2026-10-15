using Cardui.Api.Dtos.Allocation;

namespace Cardui.Api.Services.Interfaces;

public interface IAllocationService
{
    /// <summary>
    /// Returns the signed-in household's variable-income and windfall allocation preference.
    /// A missing row stays unsaved at zero. Nothing is moved.
    /// </summary>
    Task<AllocationPageDto> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores both allocation rules and returns the page.
    /// The forecast and account balances stay unchanged.
    /// </summary>
    Task<AllocationPageDto> SaveAsync(
        UpsertAllocationPreferenceDto dto,
        CancellationToken cancellationToken = default);
}
