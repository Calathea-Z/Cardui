using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Data;

public class CarduiDBContext : DbContext
{
    public CarduiDBContext(DbContextOptions<CarduiDBContext> options)
        : base(options)
    {
    }

    public DbSet<Household> Households => Set<Household>();

    public DbSet<HouseholdContributor> HouseholdContributors => Set<HouseholdContributor>();

    public DbSet<IncomeSource> IncomeSources => Set<IncomeSource>();

    public DbSet<IncomeRaise> IncomeRaises => Set<IncomeRaise>();

    public DbSet<Obligation> Obligations => Set<Obligation>();

    public DbSet<ObligationSuggestionDismissal> ObligationSuggestionDismissals =>
        Set<ObligationSuggestionDismissal>();

    public DbSet<Debt> Debts => Set<Debt>();

    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();

    public DbSet<CategoryTargetMonth> CategoryTargetMonths => Set<CategoryTargetMonth>();

    public DbSet<CategoryTarget> CategoryTargets => Set<CategoryTarget>();

    public DbSet<PlaidItem> PlaidItems => Set<PlaidItem>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<TransactionImport> TransactionImports => Set<TransactionImport>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<SubGroup> SubGroups => Set<SubGroup>();
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<AccountBalanceSnapshot> AccountBalanceSnapshots =>
        Set<AccountBalanceSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CarduiDBContext).Assembly);
    }
}
