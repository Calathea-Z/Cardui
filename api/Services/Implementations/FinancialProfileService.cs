using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Dtos.Household;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class FinancialProfileService : IFinancialProfileService
{
    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public FinancialProfileService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    public async Task<FinancialProfileDto> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var household = await RequireHouseholdAsync(cancellationToken);
        var contributors = await ListContributorsAsync(household.Id, cancellationToken);
        return Map(household, contributors);
    }

    public async Task<FinancialProfileDto> UpdateAsync(
        UpdateFinancialProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var household = await RequireHouseholdAsync(cancellationToken);
        household.PlanningCurrency = RequireCurrency(dto.PlanningCurrency);
        household.TimeZoneId = RequireTimeZone(dto.TimeZoneId);
        household.UpdatedAt = _timeProvider.GetUtcNow();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _householdScope.Bind(
            household.Id,
            household.PlanningCurrency,
            household.TimeZoneId);

        var contributors = await ListContributorsAsync(household.Id, cancellationToken);
        return Map(household, contributors);
    }

    public async Task<HouseholdContributorDto> AddContributorAsync(
        UpsertHouseholdContributorDto dto,
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var name = RequireName(dto.Name);
        await RequireUniqueNameAsync(householdId, name, null, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var contributor = new HouseholdContributor
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Name = name,
            IsVisible = dto.IsVisible,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.HouseholdContributors.Add(contributor);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapContributor(contributor);
    }

    public async Task<HouseholdContributorDto> UpdateContributorAsync(
        Guid contributorId,
        UpsertHouseholdContributorDto dto,
        CancellationToken cancellationToken = default)
    {
        var contributor = await FindContributorAsync(contributorId, cancellationToken);
        var name = RequireName(dto.Name);
        await RequireUniqueNameAsync(
            contributor.HouseholdId,
            name,
            contributor.Id,
            cancellationToken);

        contributor.Name = name;
        contributor.IsVisible = dto.IsVisible;
        contributor.UpdatedAt = _timeProvider.GetUtcNow();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapContributor(contributor);
    }

    /// <inheritdoc />
    public async Task RemoveContributorAsync(
        Guid contributorId,
        CancellationToken cancellationToken = default)
    {
        var contributor = await FindContributorAsync(contributorId, cancellationToken);
        await DetachIncomeSourcesAsync(contributor, cancellationToken);
        _dbContext.HouseholdContributors.Remove(contributor);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Loads the signed-in household so profile changes update that row.
    /// </summary>
    private async Task<Household> RequireHouseholdAsync(CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var household = await _dbContext.Households
            .FirstOrDefaultAsync(x => x.Id == householdId, cancellationToken);

        if (household is null)
        {
            throw new NotFoundException("No household exists for the signed-in owner.");
        }

        return household;
    }

    /// <summary>
    /// Loads one contributor in the signed-in household.
    /// A contributor from another household is not found.
    /// </summary>
    private async Task<HouseholdContributor> FindContributorAsync(
        Guid contributorId,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var contributor = await _dbContext.HouseholdContributors
            .FirstOrDefaultAsync(
                x => x.Id == contributorId && x.HouseholdId == householdId,
                cancellationToken);

        if (contributor is null)
        {
            throw new NotFoundException("That contributor was not found.");
        }

        return contributor;
    }

    /// <summary>
    /// Rejects a contributor name the household already uses, ignoring letter case.
    /// </summary>
    private async Task RequireUniqueNameAsync(
        Guid householdId,
        string name,
        Guid? exceptContributorId,
        CancellationToken cancellationToken)
    {
        var normalized = name.ToLowerInvariant();
        var contributors = _dbContext.HouseholdContributors
            .Where(x => x.HouseholdId == householdId);
        if (exceptContributorId is Guid exceptId)
        {
            contributors = contributors.Where(x => x.Id != exceptId);
        }

        var exists = await contributors
            .AnyAsync(x => x.Name.ToLower() == normalized, cancellationToken);

        if (exists)
        {
            throw new BadRequestException("That contributor is already in the household.");
        }
    }

    /// <summary>
    /// Lists the household's contributors by name. The rows are not tracked.
    /// </summary>
    private Task<List<HouseholdContributor>> ListContributorsAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        return _dbContext.HouseholdContributors
            .AsNoTracking()
            .Where(x => x.HouseholdId == householdId)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Keeps a three-letter planning currency code.
    /// </summary>
    private static string RequireCurrency(string? value)
    {
        if (!PlanningCurrencyRules.TryNormalize(value, out var code))
        {
            throw new BadRequestException("Enter a three-letter currency code such as USD.");
        }

        return code;
    }

    /// <summary>
    /// Keeps a time zone the household can use for today's date.
    /// </summary>
    private static string RequireTimeZone(string? value)
    {
        if (!HouseholdTime.TryNormalizeTimeZoneId(value, out var timeZoneId))
        {
            throw new BadRequestException("Choose a time zone.");
        }

        return timeZoneId;
    }

    /// <summary>
    /// Trims a contributor name and rejects a blank or oversized one.
    /// </summary>
    private static string RequireName(string? name)
    {
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0 || trimmed.Length > HouseholdContributor.NameMaxLength)
        {
            throw new BadRequestException("A contributor name is required.");
        }

        return trimmed;
    }

    /// <summary>
    /// Clears this contributor from the household's income sources.
    /// The sources stay, with no contributor, after the person is removed.
    /// </summary>
    private async Task DetachIncomeSourcesAsync(
        HouseholdContributor contributor,
        CancellationToken cancellationToken)
    {
        var sources = await _dbContext.IncomeSources
            .Where(source =>
                source.HouseholdId == contributor.HouseholdId
                && source.ContributorId == contributor.Id)
            .ToListAsync(cancellationToken);

        if (sources.Count == 0)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();
        foreach (var source in sources)
        {
            source.ContributorId = null;
            source.UpdatedAt = now;
        }
    }

    /// <summary>
    /// Builds the profile response from the household and its contributors.
    /// </summary>
    private static FinancialProfileDto Map(
        Household household,
        IReadOnlyList<HouseholdContributor> contributors)
    {
        return new FinancialProfileDto
        {
            PlanningCurrency = household.PlanningCurrency,
            TimeZoneId = household.TimeZoneId,
            Contributors = contributors.Select(MapContributor).ToList()
        };
    }

    /// <summary>
    /// Builds the contributor response.
    /// </summary>
    private static HouseholdContributorDto MapContributor(HouseholdContributor contributor)
    {
        return new HouseholdContributorDto
        {
            Id = contributor.Id,
            Name = contributor.Name,
            IsVisible = contributor.IsVisible
        };
    }

    #endregion
}
