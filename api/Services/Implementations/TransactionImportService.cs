using System.Text;
using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Dtos.TransactionImport;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class TransactionImportService : ITransactionImportService
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public TransactionImportService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<TransactionImportInspectDto> InspectAsync(
        Stream content,
        string? fileName,
        CancellationToken cancellationToken = default)
    {
        var safeName = SanitizeFileName(fileName);
        var table = await ReadTableAsync(content, cancellationToken);
        return new TransactionImportInspectDto
        {
            FileName = safeName,
            Headers = table.Headers,
            SampleRows = table.Rows
                .Take(5)
                .Select(row => (IReadOnlyList<string>)row.Cells.ToList())
                .ToList(),
            DataRowCount = table.Rows.Count,
            Suggested = ToSuggested(TransactionImportColumnGuesser.Guess(table.Headers))
        };
    }

    /// <inheritdoc />
    public async Task<TransactionImportPreviewDto> PreviewAsync(
        Stream content,
        string? fileName,
        TransactionImportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        SanitizeFileName(fileName);
        var table = await ReadTableAsync(content, cancellationToken);
        var account = await RequireAccountAsync(request.AccountId, asNoTracking: true, cancellationToken);
        var drafts = await BuildDraftsAsync(table, ToColumnMap(request), account, cancellationToken);
        return ToPreview(drafts);
    }

    /// <inheritdoc />
    public async Task<TransactionImportBatchDto> CommitAsync(
        Stream content,
        string? fileName,
        TransactionImportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var safeName = SanitizeFileName(fileName);
        var table = await ReadTableAsync(content, cancellationToken);
        var account = await RequireAccountAsync(request.AccountId, asNoTracking: false, cancellationToken);
        var lineNumbers = ParseLineNumbers(request.IncludedLineNumbers);
        var drafts = await BuildDraftsAsync(table, ToColumnMap(request), account, cancellationToken);
        var selected = SelectDrafts(drafts, lineNumbers);
        var import = await SaveImportAsync(account, safeName, selected, cancellationToken);
        return ToBatch(import, account.Name);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionImportBatchDto>> ListOpenAsync(
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        return await _dbContext.TransactionImports
            .AsNoTracking()
            .Where(import => import.HouseholdId == householdId && import.UndoneAt == null)
            .OrderByDescending(import => import.CreatedAt)
            .Take(TransactionImportLimits.MaxOpenImports)
            .Select(import => new TransactionImportBatchDto
            {
                Id = import.Id,
                AccountId = import.AccountId,
                AccountName = import.Account.Name,
                FileName = import.FileName,
                ImportedCount = import.ImportedCount,
                CreatedAt = import.CreatedAt,
                UndoneAt = import.UndoneAt
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TransactionImportBatchDto> UndoAsync(
        Guid importId,
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var import = await _dbContext.TransactionImports
            .FirstOrDefaultAsync(
                item => item.Id == importId && item.HouseholdId == householdId,
                cancellationToken);

        if (import is null)
        {
            throw new NotFoundException($"Import '{importId}' was not found.");
        }

        if (import.UndoneAt is not null)
        {
            throw new BadRequestException("This import was already undone.");
        }

        var account = await _dbContext.Accounts
            .InHousehold(_householdScope)
            .FirstOrDefaultAsync(item => item.Id == import.AccountId, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException($"Import '{importId}' was not found.");
        }

        var now = _timeProvider.GetUtcNow();
        var transactions = await _dbContext.Transactions
            .Where(transaction =>
                transaction.AccountId == import.AccountId
                && transaction.ImportId == importId
                && transaction.ArchivedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var transaction in transactions)
        {
            transaction.ArchivedAt = now;
            transaction.UpdatedAt = now;
        }

        import.UndoneAt = now;
        await RefreshBalanceAsync(account, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToBatch(import, account.Name);
    }

    #region Private Methods

    /// <summary>
    /// Reads the upload, enforces the size limit, and decodes UTF-8 text.
    /// </summary>
    private static async Task<string> ReadTextAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        var total = 0;
        while (true)
        {
            var read = await content.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > TransactionImportLimits.MaxFileBytes)
            {
                throw new BadRequestException("The CSV file must be 1 MB or smaller.");
            }

            buffer.Write(chunk, 0, read);
        }

        var bytes = buffer.ToArray();
        if (bytes.Any(value => value == 0))
        {
            throw new BadRequestException("The file is not a CSV.");
        }

        var offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF
            ? 3
            : 0;

        try
        {
            return Utf8.GetString(bytes.AsSpan(offset));
        }
        catch (DecoderFallbackException)
        {
            throw new BadRequestException("The file must be a UTF-8 CSV.");
        }
    }

    /// <summary>
    /// Parses the upload into a table and rejects a file with no data rows.
    /// </summary>
    private static async Task<CsvTable> ReadTableAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        var text = await ReadTextAsync(content, cancellationToken);
        var parsed = CsvTableParser.Parse(text);
        if (parsed.Table is null)
        {
            throw new BadRequestException(parsed.Error ?? "The CSV could not be read.");
        }

        if (parsed.Table.Rows.Count == 0)
        {
            throw new BadRequestException("The file has no transactions.");
        }

        return parsed.Table;
    }

    /// <summary>
    /// Keeps a .csv file name and rejects anything else.
    /// </summary>
    private static string SanitizeFileName(string? fileName)
    {
        var name = Path.GetFileName(fileName ?? "").Trim();
        if (name.Length == 0 || !name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Choose a .csv file.");
        }

        if (name.Length > TransactionImportLimits.MaxFileNameLength)
        {
            throw new BadRequestException("The file name is too long.");
        }

        return name;
    }

    /// <summary>
    /// Copies the request into the domain column map.
    /// A missing sign or date order uses the request default.
    /// </summary>
    private static TransactionImportColumnMap ToColumnMap(TransactionImportRequestDto request)
    {
        return new TransactionImportColumnMap(
            request.DateColumn,
            request.NameColumn,
            request.AmountColumn,
            request.DebitColumn,
            request.CreditColumn,
            request.CategoryColumn,
            request.NotesColumn,
            request.AmountSign,
            request.DateOrder);
    }

    /// <summary>
    /// Reads the selected line numbers and rejects a list that is not numbers.
    /// </summary>
    private static IReadOnlyList<int> ParseLineNumbers(string? text)
    {
        var error = TransactionImportRowSelection.TryParse(text, out var lineNumbers);
        if (error is not null)
        {
            throw new BadRequestException(error);
        }

        return lineNumbers;
    }

    /// <summary>
    /// Loads the account for this household. Preview does not track it.
    /// </summary>
    private async Task<Account> RequireAccountAsync(
        Guid accountId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty)
        {
            throw new BadRequestException("Choose an account.");
        }

        var accounts = _dbContext.Accounts.InHousehold(_householdScope);
        if (asNoTracking)
        {
            accounts = accounts.AsNoTracking();
        }

        var account = await accounts.FirstOrDefaultAsync(
            item => item.Id == accountId,
            cancellationToken);

        if (account is null)
        {
            throw new NotFoundException($"Account '{accountId}' was not found.");
        }

        if (account.ArchivedAt is not null)
        {
            throw new BadRequestException("Restore this account before importing transactions.");
        }

        return account;
    }

    /// <summary>
    /// Maps the file and compares it with transactions already on the account.
    /// </summary>
    private async Task<IReadOnlyList<TransactionImportDraft>> BuildDraftsAsync(
        CsvTable table,
        TransactionImportColumnMap map,
        Account account,
        CancellationToken cancellationToken)
    {
        var error = TransactionImportColumnMapRules.Validate(map, table.Headers.Count);
        if (error is not null)
        {
            throw new BadRequestException(error);
        }

        IReadOnlySet<TransactionImportDuplicateKey> existing = new HashSet<TransactionImportDuplicateKey>();
        if (TransactionImportDates.TryGetSpan(table, map.DateColumn, map.DateOrder, out var min, out var max))
        {
            existing = await LoadDuplicateKeysAsync(account.Id, min, max, cancellationToken);
        }

        var categories = await LoadCategoryCatalogAsync(cancellationToken);
        var today = FinancialDate.Today(_timeProvider, _householdScope.TimeZoneId);
        var opening = ManualAccountBalance.UsesLedger(account) ? account.OpeningBalanceDate : null;
        return TransactionImportPreviewBuilder.Build(
            table,
            map,
            existing,
            categories,
            today,
            opening);
    }

    /// <summary>
    /// Loads posted transaction keys for the account and date span.
    /// The account and date seek the transaction index.
    /// </summary>
    private async Task<HashSet<TransactionImportDuplicateKey>> LoadDuplicateKeysAsync(
        Guid accountId,
        DateOnly min,
        DateOnly max,
        CancellationToken cancellationToken)
    {
        var rows = await _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId == accountId && transaction.ArchivedAt == null)
            .Where(transaction => transaction.Date >= min && transaction.Date <= max)
            .Select(transaction => new
            {
                transaction.Date,
                transaction.Amount,
                transaction.Name
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => TransactionImportDuplicateKey.Create(row.Date, row.Amount, row.Name))
            .ToHashSet();
    }

    /// <summary>
    /// Loads category names the household can use. A household name replaces a system name.
    /// System keys stay available for description matching.
    /// </summary>
    private async Task<ImportCategoryCatalog> LoadCategoryCatalogAsync(
        CancellationToken cancellationToken)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .VisibleToHousehold(_householdScope)
            .Select(category => new
            {
                category.Id,
                category.Name,
                category.Key,
                category.IsSystem
            })
            .ToListAsync(cancellationToken);

        var byName = new Dictionary<string, ImportCategoryMatch>();
        var byKey = new Dictionary<string, ImportCategoryMatch>();
        foreach (var category in categories.OrderByDescending(category => category.IsSystem))
        {
            var match = new ImportCategoryMatch(category.Id, category.Name);
            byName[ImportCategoryCatalog.NormalizeName(category.Name)] = match;
            if (category.IsSystem && !byKey.ContainsKey(category.Key))
            {
                byKey[category.Key] = match;
            }
        }

        return new ImportCategoryCatalog(byName, byKey);
    }

    /// <summary>
    /// Keeps the chosen lines that are ready or marked duplicate.
    /// An error line or a missing line stops the import.
    /// </summary>
    private static IReadOnlyList<TransactionImportDraft> SelectDrafts(
        IReadOnlyList<TransactionImportDraft> drafts,
        IReadOnlyList<int> lineNumbers)
    {
        if (lineNumbers.Count == 0)
        {
            throw new BadRequestException("Choose at least one row to import.");
        }

        var byLine = drafts.ToDictionary(draft => draft.LineNumber);
        var selected = new List<TransactionImportDraft>();
        foreach (var lineNumber in lineNumbers.Order())
        {
            if (!byLine.TryGetValue(lineNumber, out var draft))
            {
                throw new BadRequestException($"Line {lineNumber} is not in this file.");
            }

            if (draft.Status == TransactionImportRowStatus.Error)
            {
                throw new BadRequestException(
                    $"Line {lineNumber} cannot be imported. {draft.Message}");
            }

            selected.Add(draft);
        }

        return selected;
    }

    /// <summary>
    /// Stores the batch and its transactions, then recalculates a manual balance.
    /// </summary>
    private async Task<TransactionImport> SaveImportAsync(
        Account account,
        string fileName,
        IReadOnlyList<TransactionImportDraft> selected,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var import = new TransactionImport
        {
            Id = Guid.NewGuid(),
            HouseholdId = _householdScope.RequireHouseholdId(),
            AccountId = account.Id,
            FileName = fileName,
            ImportedCount = selected.Count,
            CreatedAt = now
        };

        foreach (var draft in selected)
        {
            _dbContext.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                ImportId = import.Id,
                PlaidTransactionId = null,
                Source = FinancialRecordSource.Csv,
                Provenance = FinancialRecordProvenance.CsvImport,
                Date = draft.Date!.Value,
                Name = draft.Name!,
                MerchantName = draft.Name,
                Amount = draft.Amount!.Value,
                IsoCurrencyCode = account.IsoCurrencyCode,
                Pending = false,
                CategoryId = draft.CategoryId,
                IsCategoryUserEdited = draft.CategoryFromFile && draft.CategoryId.HasValue,
                Notes = draft.Notes,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        _dbContext.TransactionImports.Add(import);
        await RefreshBalanceAsync(account, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return import;
    }

    /// <summary>
    /// Recalculates today's balance when the account uses the manual ledger.
    /// </summary>
    private async Task RefreshBalanceAsync(
        Account account,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!ManualAccountBalance.UsesLedger(account))
        {
            return;
        }

        await ManualAccountBalance.RefreshAsync(
            _dbContext,
            account,
            FinancialDate.Today(_timeProvider, _householdScope.TimeZoneId),
            now,
            cancellationToken);
    }

    /// <summary>
    /// Copies a suggested column map and the default sign and date order.
    /// </summary>
    private static TransactionImportSuggestedMapDto ToSuggested(
        TransactionImportSuggestedColumns suggested)
    {
        return new TransactionImportSuggestedMapDto
        {
            DateColumn = suggested.DateColumn,
            NameColumn = suggested.NameColumn,
            AmountColumn = suggested.AmountColumn,
            DebitColumn = suggested.DebitColumn,
            CreditColumn = suggested.CreditColumn,
            CategoryColumn = suggested.CategoryColumn,
            NotesColumn = suggested.NotesColumn,
            AmountSign = CsvAmountSign.PositiveOut,
            DateOrder = CsvDateOrder.MonthFirst
        };
    }

    /// <summary>
    /// Copies drafts into the preview response, including the status counts.
    /// </summary>
    private static TransactionImportPreviewDto ToPreview(
        IReadOnlyList<TransactionImportDraft> drafts)
    {
        return new TransactionImportPreviewDto
        {
            ReadyCount = drafts.Count(draft => draft.Status == TransactionImportRowStatus.Ready),
            DuplicateCount = drafts.Count(draft => draft.Status == TransactionImportRowStatus.Duplicate),
            ErrorCount = drafts.Count(draft => draft.Status == TransactionImportRowStatus.Error),
            Rows = drafts.Select(ToPreviewRow).ToList()
        };
    }

    /// <summary>
    /// Copies one draft into the preview row.
    /// </summary>
    private static TransactionImportPreviewRowDto ToPreviewRow(TransactionImportDraft draft)
    {
        return new TransactionImportPreviewRowDto
        {
            LineNumber = draft.LineNumber,
            Date = draft.Date,
            Name = draft.Name,
            Amount = draft.Amount,
            CategoryName = draft.CategoryName,
            Status = draft.Status,
            Message = draft.Message
        };
    }

    /// <summary>
    /// Copies a stored batch into the API shape.
    /// </summary>
    private static TransactionImportBatchDto ToBatch(TransactionImport import, string accountName)
    {
        return new TransactionImportBatchDto
        {
            Id = import.Id,
            AccountId = import.AccountId,
            AccountName = accountName,
            FileName = import.FileName,
            ImportedCount = import.ImportedCount,
            CreatedAt = import.CreatedAt,
            UndoneAt = import.UndoneAt
        };
    }

    #endregion
}
