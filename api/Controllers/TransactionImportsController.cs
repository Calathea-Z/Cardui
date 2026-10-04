using Cardui.Api.Dtos.TransactionImport;
using Cardui.Api.Exceptions;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/transaction-imports")]
public class TransactionImportsController : ControllerBase
{
    private readonly ITransactionImportService _transactionImportService;

    public TransactionImportsController(ITransactionImportService transactionImportService)
    {
        _transactionImportService = transactionImportService;
    }

    /// <summary>
    /// GET /api/transaction-imports
    /// Returns imports the household can still undo.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TransactionImportBatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionImportBatchDto>>> ListOpen(
        CancellationToken cancellationToken)
    {
        var imports = await _transactionImportService.ListOpenAsync(cancellationToken);
        return Ok(imports);
    }

    /// <summary>
    /// POST /api/transaction-imports/inspect
    /// Returns the CSV header, sample rows, and a suggested column map.
    /// </summary>
    [HttpPost("inspect")]
    [ProducesResponseType<TransactionImportInspectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionImportInspectDto>> Inspect(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        await using var stream = OpenCsv(file);
        var result = await _transactionImportService.InspectAsync(
            stream,
            file!.FileName,
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/transaction-imports/preview
    /// Returns parsed rows, duplicates, and rows that cannot be imported.
    /// </summary>
    [HttpPost("preview")]
    [ProducesResponseType<TransactionImportPreviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionImportPreviewDto>> Preview(
        IFormFile? file,
        [FromForm] TransactionImportRequestDto request,
        CancellationToken cancellationToken)
    {
        await using var stream = OpenCsv(file);
        var result = await _transactionImportService.PreviewAsync(
            stream,
            file!.FileName,
            request,
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/transaction-imports
    /// Imports the selected CSV lines and returns the batch.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<TransactionImportBatchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionImportBatchDto>> Commit(
        IFormFile? file,
        [FromForm] TransactionImportRequestDto request,
        CancellationToken cancellationToken)
    {
        await using var stream = OpenCsv(file);
        var result = await _transactionImportService.CommitAsync(
            stream,
            file!.FileName,
            request,
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/transaction-imports/{id}/undo
    /// Archives the transactions from that import.
    /// </summary>
    [HttpPost("{id:guid}/undo")]
    [ProducesResponseType<TransactionImportBatchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionImportBatchDto>> Undo(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _transactionImportService.UndoAsync(id, cancellationToken);
        return Ok(result);
    }

    #region Private Methods

    /// <summary>
    /// Opens the uploaded CSV. A missing file is rejected before the service runs.
    /// </summary>
    private static Stream OpenCsv(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            throw new BadRequestException("Choose a CSV file.");
        }

        return file.OpenReadStream();
    }

    #endregion
}
