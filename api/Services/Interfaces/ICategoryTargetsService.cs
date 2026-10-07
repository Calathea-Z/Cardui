using Cardui.Api.Dtos.CategoryTargets;

namespace Cardui.Api.Services.Interfaces;

public interface ICategoryTargetsService
{
    /// <summary>
    /// Returns category targets and spent amounts for one month.
    /// When the year and month are omitted, the month is the household's current month.
    /// A month that has not been started shows the nearest earlier month's targets as a preview and does not save them.
    /// </summary>
    Task<CategoryTargetMonthDto> GetAsync(
        int? year,
        int? month,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a month by copying the nearest earlier month's targets and rollover choices.
    /// Leftover money is not copied. A month that already started is left as it is.
    /// </summary>
    Task<CategoryTargetMonthDto> CopyForwardAsync(
        CategoryTargetMonthRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a month with no targets, so a later visit does not copy an earlier month.
    /// A month that already has targets is left unchanged and refused.
    /// </summary>
    Task<CategoryTargetMonthDto> StartFreshAsync(
        CategoryTargetMonthRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves one category's target and rollover choice.
    /// The first save in a month also copies the other categories from the nearest earlier month.
    /// </summary>
    Task<CategoryTargetMonthDto> SaveAsync(
        Guid categoryId,
        UpsertCategoryTargetDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes one category's target for a month.
    /// The month stays started, so the removed target is not copied back on the next visit.
    /// </summary>
    Task<CategoryTargetMonthDto> ClearAsync(
        Guid categoryId,
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
