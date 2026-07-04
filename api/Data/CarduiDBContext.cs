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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlaidItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.PlaidItemId)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.AccessToken)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(x => x.InstitutionId)
                .HasMaxLength(200);

            entity.Property(x => x.InstitutionName)
                .HasMaxLength(200);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .IsRequired();

            entity.HasIndex(x => x.PlaidItemId)
                .IsUnique();

            entity.HasMany(x => x.Accounts)
                .WithOne(x => x.PlaidItem)
                .HasForeignKey(x => x.PlaidItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.PlaidAccountId)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.OfficialName)
                .HasMaxLength(300);

            entity.Property(x => x.Type)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Subtype)
                .HasMaxLength(100);

            entity.Property(x => x.Mask)
                .HasMaxLength(20);

            entity.Property(x => x.CurrentBalance)
                .HasPrecision(18, 2);

            entity.Property(x => x.AvailableBalance)
                .HasPrecision(18, 2);

            entity.Property(x => x.IsoCurrencyCode)
                .HasMaxLength(10);

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .IsRequired();

            entity.HasIndex(x => x.PlaidAccountId)
                .IsUnique();

            entity.HasMany(x => x.Transactions)
                .WithOne(x => x.Account)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.PlaidTransactionId)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(x => x.MerchantName)
                .HasMaxLength(300);

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.Property(x => x.IsoCurrencyCode)
                .HasMaxLength(10);

            entity.Property(x => x.Pending)
                .IsRequired();

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .IsRequired();

            entity.HasIndex(x => x.PlaidTransactionId)
                .IsUnique();

            entity.HasIndex(x => x.Date);

            entity.HasIndex(x => x.AccountId);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Color)
                .HasMaxLength(20);

            entity.Property(x => x.Icon)
                .HasMaxLength(100);

            entity.Property(x => x.IsSystem)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .IsRequired();

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.HasOne(x => x.ParentCategory)
                .WithMany(x => x.ChildCategories)
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
