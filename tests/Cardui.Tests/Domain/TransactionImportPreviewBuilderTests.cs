using Cardui.Api.Domain.Categories;
using Cardui.Api.Domain.TransactionImport;
using Xunit;

namespace Cardui.Tests.Domain;

public class TransactionImportPreviewBuilderTests
{
    private static readonly DateOnly Today = new(2026, 10, 4);
    private static readonly Guid FoodId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid GroceryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Build_FlipsPositiveAmountsWhenMoneyInIsPositive()
    {
        var drafts = Build(
            "Date,Name,Amount\n10/01/2026,Deposit,12.00\n10/02/2026,Refund,(4.00)\n",
            Map(amountSign: CsvAmountSign.PositiveIn));

        Assert.Equal(-12m, drafts[0].Amount);
        Assert.Equal(TransactionImportRowStatus.Ready, drafts[0].Status);
        Assert.Equal(4m, drafts[1].Amount);
    }

    [Fact]
    public void Build_KeepsACreditMarkerWhenTheColumnSignWouldFlipIt()
    {
        var drafts = Build(
            "Date,Name,Amount\n2026-10-01,Deposit,12.50 CR\n",
            Map(amountSign: CsvAmountSign.PositiveIn));

        var row = Assert.Single(drafts);
        Assert.Equal(-12.50m, row.Amount);
        Assert.Equal(TransactionImportRowStatus.Ready, row.Status);
    }

    [Fact]
    public void Build_ReadsDebitAsMoneyOutAndCreditAsMoneyIn()
    {
        var drafts = Build(
            "Date,Name,Debit,Credit\n2026-10-01,Store,8.00,\n2026-10-02,Payroll,,2000\n2026-10-03,Both,1,2\n",
            Map(amountColumn: null, debitColumn: 2, creditColumn: 3));

        Assert.Equal(8m, drafts[0].Amount);
        Assert.Equal(-2000m, drafts[1].Amount);
        Assert.Equal(TransactionImportRowStatus.Error, drafts[2].Status);
        Assert.Equal("A row can have a debit or a credit, not both.", drafts[2].Message);
    }

    [Fact]
    public void Build_MarksExistingAndRepeatedRowsAsDuplicates()
    {
        var existing = new HashSet<TransactionImportDuplicateKey>
        {
            TransactionImportDuplicateKey.Create(new DateOnly(2026, 10, 1), 100m, "Rent")
        };
        var drafts = Build(
            "Date,Name,Amount\n2026-10-01,Rent,100.00\n2026-10-02,Coffee,4.00\n2026-10-02,coffee,4.00\n",
            Map(),
            existing);

        Assert.Equal(TransactionImportRowStatus.Duplicate, drafts[0].Status);
        Assert.Contains("already on the account", drafts[0].Message);
        Assert.Equal(TransactionImportRowStatus.Ready, drafts[1].Status);
        Assert.Equal(TransactionImportRowStatus.Duplicate, drafts[2].Status);
        Assert.Contains("earlier row", drafts[2].Message);
    }

    [Fact]
    public void Build_RejectsFutureAndPreOpeningDates()
    {
        var drafts = Build(
            "Date,Name,Amount\n2026-10-05,Later,1.00\n2025-12-01,Earlier,1.00\n",
            Map(),
            openingDate: new DateOnly(2026, 1, 1));

        Assert.Equal(TransactionImportRowStatus.Error, drafts[0].Status);
        Assert.Contains("future", drafts[0].Message);
        Assert.Equal(TransactionImportRowStatus.Error, drafts[1].Status);
        Assert.Contains("opening date", drafts[1].Message);
    }

    [Fact]
    public void Build_MatchesACategoryNameAndSuggestsOneFromTheDescription()
    {
        var drafts = Build(
            "Date,Name,Amount,Category\n2026-10-01,Market,12.00,groceries\n2026-10-02,Starbucks,4.50,\n2026-10-03,Unknown,3.00,Missing\n",
            Map(categoryColumn: 3),
            categories: Catalog());

        Assert.Equal(GroceryId, drafts[0].CategoryId);
        Assert.True(drafts[0].CategoryFromFile);
        Assert.Equal("Groceries", drafts[0].CategoryName);

        Assert.Equal(FoodId, drafts[1].CategoryId);
        Assert.False(drafts[1].CategoryFromFile);
        Assert.Equal("Food & Dining", drafts[1].CategoryName);

        Assert.Null(drafts[2].CategoryId);
        Assert.Equal(TransactionImportRowStatus.Ready, drafts[2].Status);
        Assert.Contains("Missing", drafts[2].Message);
    }

    private static IReadOnlyList<TransactionImportDraft> Build(
        string csv,
        TransactionImportColumnMap map,
        IReadOnlySet<TransactionImportDuplicateKey>? existing = null,
        ImportCategoryCatalog? categories = null,
        DateOnly? openingDate = null)
    {
        var table = CsvTableParser.Parse(csv).Table!;
        return TransactionImportPreviewBuilder.Build(
            table,
            map,
            existing ?? new HashSet<TransactionImportDuplicateKey>(),
            categories ?? ImportCategoryCatalog.Empty,
            Today,
            openingDate);
    }

    private static TransactionImportColumnMap Map(
        int? amountColumn = 2,
        int? debitColumn = null,
        int? creditColumn = null,
        int? categoryColumn = null,
        CsvAmountSign amountSign = CsvAmountSign.PositiveOut)
    {
        return new TransactionImportColumnMap(
            DateColumn: 0,
            NameColumn: 1,
            amountColumn,
            debitColumn,
            creditColumn,
            categoryColumn,
            NotesColumn: null,
            amountSign,
            CsvDateOrder.MonthFirst);
    }

    private static ImportCategoryCatalog Catalog()
    {
        return new ImportCategoryCatalog(
            new Dictionary<string, ImportCategoryMatch>
            {
                ["groceries"] = new(GroceryId, "Groceries"),
                ["food & dining"] = new(FoodId, "Food & Dining")
            },
            new Dictionary<string, ImportCategoryMatch>
            {
                [SystemCategoryKeys.FoodDining] = new(FoodId, "Food & Dining")
            });
    }
}
