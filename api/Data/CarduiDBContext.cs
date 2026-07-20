using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Data;

public class CarduiDBContext : DbContext
{
    public CarduiDBContext(DbContextOptions<CarduiDBContext> options)
        : base(options)
    {
    }

    public DbSet<PlaidItem> PlaidItems => Set<PlaidItem>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<AccountBalanceSnapshot> AccountBalanceSnapshots =>
        Set<AccountBalanceSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CarduiDBContext).Assembly);
    }
}
