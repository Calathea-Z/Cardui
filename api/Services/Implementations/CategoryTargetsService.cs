using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Dtos.CategoryTargets;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class CategoryTargetsService : ICategoryTargetsService
{
    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public CategoryTargetsService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<CategoryTargetMonthDto> GetAsync(
        int? year,
        int? month,
        CancellationToken cancellationToken = default)
    {
        var (resolvedYear, resolvedMonth) = ResolveMonth(year, month);
        return await BuildMonthAsync(resolvedYear, resolvedMonth, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CategoryTargetMonthDto> CopyForwardAsync(
        CategoryTargetMonthRequest request,
        CancellationToken cancellationToken = default)
    {
        RequireMonth(request.Year, request.Month);
        if (await FindSavedMonthAsync(request.Year, request.Month, cancellationToken) is null)
        {
            await EnsureMonthAsync(
                request.Year,
                request.Month,
                copyPriorTargets: true,
                cancellationToken);
        }

        return await BuildMonthAsync(request.Year, request.Month, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CategoryTargetMonthDto> StartFreshAsync(
        CategoryTargetMonthRequest request,
        CancellationToken cancellationToken = default)
    {
        RequireMonth(request.Year, request.Month);
        var existing = await FindSavedMonthAsync(
            request.Year,
            request.Month,
            cancellationToken);
        if (existing is { Assignments.Count: > 0 })
        {
            throw new BadRequestException("This month has already started.");
        }

        if (existing is null)
        {
            await EnsureMonthAsync(
                request.Year,
                request.Month,
                copyPriorTargets: false,
                cancellationToken);
        }

        return await BuildMonthAsync(request.Year, request.Month, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CategoryTargetMonthDto> SaveAsync(
        Guid categoryId,
        UpsertCategoryTargetDto dto,
        CancellationToken cancellationToken = default)
    {
        RequireMonth(dto.Year, dto.Month);
        if (!CategoryTargetRules.TryReadAmount(dto.Amount, out var amountError))
        {
            throw new BadRequestException(amountError);
        }

        await RequireExpenseCategoryAsync(categoryId, cancellationToken);
        var month = await EnsureMonthAsync(
            dto.Year,
            dto.Month,
            copyPriorTargets: true,
            cancellationToken);
        SaveTarget(month, categoryId, dto.Amount, dto.Rollover);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await BuildMonthAsync(dto.Year, dto.Month, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CategoryTargetMonthDto> ClearAsync(
        Guid categoryId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        RequireMonth(year, month);
        await RequireExpenseCategoryAsync(categoryId, cancellationToken);
        var existing = await FindSavedMonthAsync(year, month, cancellationToken);
        if (existing is null && !await PreviewHasTargetAsync(year, month, categoryId, cancellationToken))
        {
            throw new BadRequestException("That category has no target.");
        }

        var saved = await EnsureMonthAsync(
            year,
            month,
            copyPriorTargets: true,
            cancellationToken);
        var target = saved.Targets.FirstOrDefault(item => item.CategoryId == categoryId);
        if (target is null)
        {
            throw new BadRequestException("That category has no target.");
        }

        _dbContext.CategoryTargets.Remove(target);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await BuildMonthAsync(year, month, cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Uses the household's current month when the request omits both parts.
    /// A request that sends only the year or only the month is refused.
    /// </summary>
    private (int Year, int Month) ResolveMonth(int? year, int? month)
    {
        if (year is null && month is null)
        {
            var today = Today();
            return (today.Year, today.Month);
        }

        if (year is not int resolvedYear || month is not int resolvedMonth)
        {
            throw new BadRequestException("Enter both the year and the month.");
        }

        RequireMonth(resolvedYear, resolvedMonth);
        return (resolvedYear, resolvedMonth);
    }

    /// <summary>
    /// Refuses a month outside January 2000 through December 2100.
    /// </summary>
    private static void RequireMonth(int year, int month)
    {
        if (!CategoryTargetRules.TryReadMonth(year, month, out _, out var error))
        {
            throw new BadRequestException(error);
        }
    }

    /// <summary>
    /// Today's date in the household time zone.
    /// </summary>
    private DateOnly Today()
    {
        return FinancialDate.Today(_timeProvider, _householdScope.TimeZoneId);
    }

    /// <summary>
    /// Loads the signed-in household and returns its id.
    /// </summary>
    private async Task<Guid> RequireHouseholdAsync(CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var exists = await _dbContext.Households
            .AsNoTracking()
            .AnyAsync(household => household.Id == householdId, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException("No household exists for the signed-in owner.");
        }

        return householdId;
    }

    /// <summary>
    /// Refuses a missing category, and a category that is not spending.
    /// Income and transfers cannot hold a target.
    /// </summary>
    private async Task RequireExpenseCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .AsNoTracking()
            .VisibleToHousehold(_householdScope)
            .Where(item => item.Id == categoryId)
            .Select(item => new { item.Id, GroupKey = item.SubGroup.Group.Key })
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
        {
            throw new NotFoundException("That category was not found.");
        }

        if (!IsExpenseGroup(category.GroupKey))
        {
            throw new BadRequestException(
                "Set a target on a spending category. Income and transfers are not spending.");
        }
    }

    /// <summary>
    /// True when the category's group is Expenses.
    /// </summary>
    private static bool IsExpenseGroup(string? groupKey)
    {
        return string.Equals(groupKey, SystemGroupKeys.Expenses, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Loads spending categories in subgroup order, then by name.
    /// </summary>
    private async Task<IReadOnlyList<ExpenseCategoryRow>> LoadExpenseCategoriesAsync(
        CancellationToken cancellationToken)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .VisibleToHousehold(_householdScope)
            .Select(item => new ExpenseCategoryRow(
                item.Id,
                item.Name,
                item.Color,
                item.Icon,
                item.SubGroup.Name,
                item.SubGroup.SortOrder,
                item.SubGroup.Group.Key))
            .ToListAsync(cancellationToken);

        return categories
            .Where(item => IsExpenseGroup(item.GroupKey))
            .OrderBy(item => item.SubGroupSortOrder)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Loads every started month for the household, with its targets.
    /// </summary>
    private async Task<IReadOnlyList<SavedTargetMonth>> LoadSavedMonthsAsync(
        CancellationToken cancellationToken)
    {
        var householdId = await RequireHouseholdAsync(cancellationToken);
        var months = await _dbContext.CategoryTargetMonths
            .AsNoTracking()
            .Where(item => item.HouseholdId == householdId)
            .Select(item => new
            {
                item.Id,
                item.Year,
                item.Month,
                item.CopiedFromYear,
                item.CopiedFromMonth
            })
            .ToListAsync(cancellationToken);

        var monthIds = months.Select(item => item.Id).ToList();
        var targets = await _dbContext.CategoryTargets
            .AsNoTracking()
            .Where(item => monthIds.Contains(item.CategoryTargetMonthId))
            .Select(item => new
            {
                item.CategoryTargetMonthId,
                item.CategoryId,
                item.Amount,
                item.Rollover
            })
            .ToListAsync(cancellationToken);

        return months
            .Select(item => new SavedTargetMonth(
                item.Id,
                item.Year,
                item.Month,
                item.CopiedFromYear,
                item.CopiedFromMonth,
                targets
                    .Where(target => target.CategoryTargetMonthId == item.Id)
                    .Select(target => new CategoryTargetAssignment(
                        target.CategoryId,
                        target.Amount,
                        target.Rollover))
                    .ToList()))
            .ToList();
    }

    /// <summary>
    /// Finds one started month, or null when that month has not been started.
    /// </summary>
    private async Task<SavedTargetMonth?> FindSavedMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var months = await LoadSavedMonthsAsync(cancellationToken);
        return months.FirstOrDefault(item => item.Year == year && item.Month == month);
    }

    /// <summary>
    /// True when the nearest earlier month has a target that a preview would show.
    /// </summary>
    private async Task<bool> PreviewHasTargetAsync(
        int year,
        int month,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var months = await LoadSavedMonthsAsync(cancellationToken);
        var prior = NearestPrior(months, year, month);
        return prior is not null && prior.Assignments.Any(item => item.CategoryId == categoryId);
    }

    /// <summary>
    /// The latest started month before the requested one.
    /// </summary>
    private static SavedTargetMonth? NearestPrior(
        IReadOnlyList<SavedTargetMonth> months,
        int year,
        int month)
    {
        return months
            .Where(item => CategoryTargetRules.IsBefore(item.Year, item.Month, year, month))
            .OrderByDescending(item => item.Year)
            .ThenByDescending(item => item.Month)
            .FirstOrDefault();
    }

    /// <summary>
    /// Creates a month when it has not been started.
    /// Copying brings forward target amounts and rollover choices, not leftover money.
    /// </summary>
    private async Task<CategoryTargetMonth> EnsureMonthAsync(
        int year,
        int month,
        bool copyPriorTargets,
        CancellationToken cancellationToken)
    {
        var householdId = await RequireHouseholdAsync(cancellationToken);
        var existing = await _dbContext.CategoryTargetMonths
            .Include(item => item.Targets)
            .SingleOrDefaultAsync(
                item => item.HouseholdId == householdId
                    && item.Year == year
                    && item.Month == month,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var now = _timeProvider.GetUtcNow();
        var created = new CategoryTargetMonth
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Year = year,
            Month = month,
            CreatedAt = now
        };

        if (copyPriorTargets)
        {
            await CopyPriorTargetsAsync(created, now, cancellationToken);
        }

        _dbContext.CategoryTargetMonths.Add(created);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return created;
    }

    /// <summary>
    /// Copies the nearest earlier month's targets onto a new month.
    /// Categories that are no longer spending are left out.
    /// </summary>
    private async Task CopyPriorTargetsAsync(
        CategoryTargetMonth created,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var months = await LoadSavedMonthsAsync(cancellationToken);
        var prior = NearestPrior(months, created.Year, created.Month);
        if (prior is null)
        {
            return;
        }

        created.CopiedFromYear = prior.Year;
        created.CopiedFromMonth = prior.Month;
        var expenseIds = (await LoadExpenseCategoriesAsync(cancellationToken))
            .Select(item => item.Id)
            .ToHashSet();

        foreach (var assignment in CategoryTargetCalculator.CopyForward(prior.Assignments))
        {
            if (!expenseIds.Contains(assignment.CategoryId))
            {
                continue;
            }

            created.Targets.Add(NewTarget(created.Id, assignment, now));
        }
    }

    /// <summary>
    /// Inserts or updates one category target on a tracked month.
    /// </summary>
    private void SaveTarget(
        CategoryTargetMonth month,
        Guid categoryId,
        decimal amount,
        bool rollover)
    {
        var now = _timeProvider.GetUtcNow();
        var target = month.Targets.FirstOrDefault(item => item.CategoryId == categoryId);
        if (target is null)
        {
            var created = NewTarget(
                month.Id,
                new CategoryTargetAssignment(categoryId, amount, rollover),
                now);
            month.Targets.Add(created);

            // A preset id on a month that is already stored is tracked as an update
            // of a row that does not exist yet. Mark the insert explicitly.
            if (_dbContext.Entry(month).State != EntityState.Added)
            {
                _dbContext.Entry(created).State = EntityState.Added;
            }

            return;
        }

        target.Amount = amount;
        target.Rollover = rollover;
        target.UpdatedAt = now;
    }

    /// <summary>
    /// A new target row that has not been saved yet.
    /// </summary>
    private static CategoryTarget NewTarget(
        Guid monthId,
        CategoryTargetAssignment assignment,
        DateTimeOffset now)
    {
        return new CategoryTarget
        {
            Id = Guid.NewGuid(),
            CategoryTargetMonthId = monthId,
            CategoryId = assignment.CategoryId,
            Amount = assignment.Amount,
            Rollover = assignment.Rollover,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Builds the month the screen shows, including spent and rollover.
    /// Reading does not start the month.
    /// </summary>
    private async Task<CategoryTargetMonthDto> BuildMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var today = Today();
        var expenses = await LoadExpenseCategoriesAsync(cancellationToken);
        var saved = await LoadSavedMonthsAsync(cancellationToken);
        var rows = await LoadActivityAsync(saved, year, month, today, cancellationToken);
        return ToMonth(year, month, today, expenses, saved, rows);
    }

    /// <summary>
    /// Loads household transactions from the earliest started month through the spending end.
    /// A future month loads nothing when that end is still before the month begins.
    /// </summary>
    private async Task<IReadOnlyList<DatedActivityRow>> LoadActivityAsync(
        IReadOnlyList<SavedTargetMonth> saved,
        int year,
        int month,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var (start, end) = ActivityRange(saved, year, month, today);
        if (start > end)
        {
            return [];
        }

        return await _dbContext.Transactions
            .AsNoTracking()
            .InHousehold(_dbContext, _householdScope)
            .Where(item => item.ArchivedAt == null)
            .Where(item => item.Date >= start && item.Date <= end)
            .Select(item => new DatedActivityRow(
                item.Date,
                item.Amount,
                item.Pending,
                item.CategoryId,
                item.Category == null
                    ? SystemCategoryNames.Uncategorized
                    : item.Category.Name,
                item.Category == null ? null : item.Category.Color,
                item.Category == null ? null : item.Category.Key,
                item.Category == null ? null : item.Category.SubGroup.Group.Key,
                item.Provenance,
                item.IsoCurrencyCode))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// The inclusive date range of transactions this month view needs.
    /// Spending stops today, matching the household's posted activity.
    /// </summary>
    private static (DateOnly Start, DateOnly End) ActivityRange(
        IReadOnlyList<SavedTargetMonth> saved,
        int year,
        int month,
        DateOnly today)
    {
        var requestedStart = CategoryTargetCalculator.MonthBounds(year, month).Start;
        var earliest = saved
            .Select(item => CategoryTargetCalculator.MonthBounds(item.Year, item.Month).Start)
            .DefaultIfEmpty(requestedStart)
            .Min();
        var start = earliest < requestedStart ? earliest : requestedStart;
        var end = CategoryTargetCalculator.SpendingEnd(year, month, today);
        return (start, end);
    }

    /// <summary>
    /// Assembles the month from saved targets, the copy-forward preview, and posted spending.
    /// </summary>
    private CategoryTargetMonthDto ToMonth(
        int year,
        int month,
        DateOnly today,
        IReadOnlyList<ExpenseCategoryRow> expenses,
        IReadOnlyList<SavedTargetMonth> saved,
        IReadOnlyList<DatedActivityRow> rows)
    {
        var planningCurrency = _householdScope.PlanningCurrency;
        var savedMonth = saved.FirstOrDefault(item => item.Year == year && item.Month == month);
        var expenseIds = expenses.Select(item => item.Id).ToHashSet();
        var assignments = AssignmentsFor(savedMonth, saved, year, month, expenseIds);
        var history = HistoryBefore(saved, rows, year, month, today, planningCurrency);
        var current = Summarize(
            RowsInMonth(rows, year, month, today),
            planningCurrency);
        var progress = CategoryTargetCalculator.Calculate(
            assignments,
            history,
            current.Spent,
            expenses.Select(item => item.Id).ToList(),
            year,
            month);
        var lines = progress
            .Select(item => ToLine(item, expenses, current.Labels))
            .ToList();
        var targeted = lines.Where(item => item.Target is not null).ToList();
        var bounds = CategoryTargetCalculator.MonthBounds(year, month);
        var copiedFrom = savedMonth is null ? CopiedFrom(saved, year, month) : null;

        return new CategoryTargetMonthDto
        {
            Year = year,
            Month = month,
            Saved = savedMonth is not null,
            CopiedFromYear = savedMonth?.CopiedFromYear ?? copiedFrom?.Year,
            CopiedFromMonth = savedMonth?.CopiedFromMonth ?? copiedFrom?.Month,
            PlanningCurrency = planningCurrency,
            PeriodStart = bounds.Start,
            PeriodEnd = bounds.End,
            ThroughToday = bounds.Start.Year == today.Year && bounds.Start.Month == today.Month,
            TodayYear = today.Year,
            TodayMonth = today.Month,
            TargetTotal = targeted.Count == 0
                ? null
                : targeted.Sum(item => item.Target!.Value),
            MissingTargetCount = lines.Count(item =>
                item.CategoryId is Guid categoryId
                && expenseIds.Contains(categoryId)
                && item.Target is null),
            UnassignedRolloverCount = lines.Count(item =>
                item.Target is null && item.RolloverIn != 0),
            Spent = lines.Sum(item => item.Spent),
            OtherSpent = lines.Where(item => item.Target is null).Sum(item => item.Spent),
            Remaining = targeted.Count == 0
                ? null
                : targeted.Sum(item => item.Remaining!.Value),
            ExcludedTransactionCount = current.ExcludedTransactionCount,
            ExcludedCurrencies = current.ExcludedCurrencies,
            Categories = lines
        };
    }

    /// <summary>
    /// Saved targets when the month has started. Otherwise the nearest earlier month's targets.
    /// </summary>
    private static IReadOnlyList<CategoryTargetAssignment> AssignmentsFor(
        SavedTargetMonth? savedMonth,
        IReadOnlyList<SavedTargetMonth> saved,
        int year,
        int month,
        HashSet<Guid> expenseIds)
    {
        if (savedMonth is not null)
        {
            return savedMonth.Assignments;
        }

        var prior = NearestPrior(saved, year, month);
        if (prior is null)
        {
            return [];
        }

        return CategoryTargetCalculator.CopyForward(prior.Assignments)
            .Where(item => expenseIds.Contains(item.CategoryId))
            .ToList();
    }

    /// <summary>
    /// The month a preview was copied from, or null when nothing earlier has started.
    /// </summary>
    private static SavedTargetMonth? CopiedFrom(
        IReadOnlyList<SavedTargetMonth> saved,
        int year,
        int month)
    {
        return NearestPrior(saved, year, month);
    }

    /// <summary>
    /// Earlier started months, in any order, with the spending that rollover needs.
    /// </summary>
    private static IReadOnlyList<CategoryTargetHistoryMonth> HistoryBefore(
        IReadOnlyList<SavedTargetMonth> saved,
        IReadOnlyList<DatedActivityRow> rows,
        int year,
        int month,
        DateOnly today,
        string planningCurrency)
    {
        return saved
            .Where(item => CategoryTargetRules.IsBefore(item.Year, item.Month, year, month))
            .Select(item => new CategoryTargetHistoryMonth(
                item.Year,
                item.Month,
                item.Assignments,
                Summarize(RowsInMonth(rows, item.Year, item.Month, today), planningCurrency).Spent))
            .ToList();
    }

    /// <summary>
    /// Transactions whose spending counts for one calendar month.
    /// </summary>
    private static List<DatedActivityRow> RowsInMonth(
        IReadOnlyList<DatedActivityRow> rows,
        int year,
        int month,
        DateOnly today)
    {
        var (start, _) = CategoryTargetCalculator.MonthBounds(year, month);
        var end = CategoryTargetCalculator.SpendingEnd(year, month, today);
        if (start > end)
        {
            return [];
        }

        return rows
            .Where(row => row.Date >= start && row.Date <= end)
            .ToList();
    }

    /// <summary>
    /// Classifies one month of rows with the same rules as household activity.
    /// Another currency is left out of spending and reported for the month being viewed.
    /// </summary>
    private static CategoryMonthActivity Summarize(
        IReadOnlyList<DatedActivityRow> rows,
        string planningCurrency)
    {
        var included = new List<TransactionActivityValue>();
        var excludedCurrencies = new List<string?>();
        var excludedCount = 0;

        foreach (var row in rows)
        {
            var value = row.ToValue();
            if (PlanningCurrencyRules.IsIncluded(row.CurrencyCode, planningCurrency))
            {
                included.Add(value);
                continue;
            }

            if (TransactionActivityCalculator.AffectsIncomeOrSpending(value))
            {
                excludedCount++;
                excludedCurrencies.Add(row.CurrencyCode);
            }
        }

        var totals = TransactionActivityCalculator.Calculate(included);
        var labels = new Dictionary<Guid, ActivityCategoryLabel>();
        foreach (var category in totals.SpendingByCategory)
        {
            if (category.CategoryId is not Guid categoryId || labels.ContainsKey(categoryId))
            {
                continue;
            }

            labels[categoryId] = new ActivityCategoryLabel(category.CategoryName, category.CategoryColor);
        }

        return new CategoryMonthActivity(
            totals.SpendingByCategory
                .Select(item => new CategoryMonthSpent(item.CategoryId, item.Amount))
                .ToList(),
            excludedCount,
            PlanningCurrencyRules.ExcludedCodes(excludedCurrencies, planningCurrency),
            labels);
    }

    /// <summary>
    /// The screen row for one calculated category.
    /// A spending category can be edited. Uncategorized spending cannot.
    /// </summary>
    private static CategoryTargetLineDto ToLine(
        CategoryTargetProgress progress,
        IReadOnlyList<ExpenseCategoryRow> expenses,
        IReadOnlyDictionary<Guid, ActivityCategoryLabel> labels)
    {
        if (progress.CategoryId is Guid categoryId)
        {
            var expense = expenses.FirstOrDefault(item => item.Id == categoryId);
            if (expense is not null)
            {
                return LineFrom(progress, expense.Name, expense.Color, expense.Icon, expense.SubGroupName, true);
            }
        }

        var name = SystemCategoryNames.Uncategorized;
        string? color = null;
        if (progress.CategoryId is Guid orphanId && labels.TryGetValue(orphanId, out var label))
        {
            name = label.Name;
            color = label.Color;
        }

        return LineFrom(progress, name, color, null, "No category", false);
    }

    /// <summary>
    /// Copies calculated amounts onto the row the screen renders.
    /// </summary>
    private static CategoryTargetLineDto LineFrom(
        CategoryTargetProgress progress,
        string name,
        string? color,
        string? icon,
        string subGroupName,
        bool canSetTarget)
    {
        return new CategoryTargetLineDto
        {
            CategoryId = progress.CategoryId,
            Name = name,
            Color = color,
            Icon = icon,
            SubGroupName = subGroupName,
            Target = progress.Target,
            Rollover = progress.Rollover,
            RolloverIn = progress.RolloverIn,
            Spent = progress.Spent,
            Available = progress.Available,
            Remaining = progress.Remaining,
            CanSetTarget = canSetTarget
        };
    }

    #endregion
}
