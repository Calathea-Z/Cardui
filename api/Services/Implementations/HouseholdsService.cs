using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Dtos.Household;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class HouseholdsService : IHouseholdsService
{
    public const string DefaultDisplayName = "My household";

    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public HouseholdsService(CarduiDBContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<HouseholdDto> GetOrCreateForOwnerAsync(
        string ownerClerkUserId,
        CancellationToken cancellationToken = default)
    {
        var ownerId = ownerClerkUserId.Trim();
        if (ownerId.Length == 0 || ownerId.Length > Household.OwnerClerkUserIdMaxLength)
        {
            throw new BadRequestException("A signed-in owner is required.");
        }

        var existing = await FindByOwnerAsync(ownerId, cancellationToken);
        if (existing is not null)
        {
            return Map(existing);
        }

        var now = _timeProvider.GetUtcNow();
        var household = new Household
        {
            Id = Guid.NewGuid(),
            OwnerClerkUserId = ownerId,
            DisplayName = DefaultDisplayName,
            PlanningCurrency = PlanningCurrencyRules.DefaultCode,
            TimeZoneId = HouseholdTime.DefaultTimeZoneId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Households.Add(household);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Map(household);
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(household).State = EntityState.Detached;
            var raced = await FindByOwnerAsync(ownerId, cancellationToken);
            if (raced is null)
            {
                throw;
            }

            return Map(raced);
        }
    }

    private Task<Household?> FindByOwnerAsync(
        string ownerClerkUserId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Households
            .SingleOrDefaultAsync(x => x.OwnerClerkUserId == ownerClerkUserId, cancellationToken);
    }

    private static HouseholdDto Map(Household household)
    {
        return new HouseholdDto
        {
            Id = household.Id,
            DisplayName = household.DisplayName,
            CreatedAt = household.CreatedAt
        };
    }
}
