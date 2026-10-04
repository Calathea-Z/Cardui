using Cardui.Api.Data;
using Cardui.Api.Exceptions;
using Cardui.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Middleware;

public sealed class HouseholdScopeMiddleware
{
    private readonly RequestDelegate _next;

    public HouseholdScopeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IHouseholdOwnerContext ownerContext,
        CarduiDBContext dbContext,
        HouseholdScope householdScope)
    {
        if (context.User.Identity?.IsAuthenticated == true && !IsCurrentHouseholdCreate(context))
        {
            var ownerId = ownerContext.ClerkUserId.Trim();
            var household = await dbContext.Households
                .AsNoTracking()
                .Where(x => x.OwnerClerkUserId == ownerId)
                .Select(x => new
                {
                    x.Id,
                    x.PlanningCurrency,
                    x.TimeZoneId
                })
                .SingleOrDefaultAsync(context.RequestAborted);

            if (household is null)
            {
                throw new NotFoundException("No household exists for the signed-in owner.");
            }

            householdScope.Bind(
                household.Id,
                household.PlanningCurrency,
                household.TimeZoneId);
        }

        await _next(context);
    }

    private static bool IsCurrentHouseholdCreate(HttpContext context)
    {
        return HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.Equals("/api/households/current", StringComparison.OrdinalIgnoreCase);
    }
}
