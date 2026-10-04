using Cardui.Api.Dtos.Common;
using Cardui.Api.Dtos.Transaction;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionsService _transactionsService;

    public TransactionsController(ITransactionsService transactionsService)
    {
        _transactionsService = transactionsService;
    }

    /// <summary>
    /// GET /api/transactions
    /// Returns a page of household transactions using the query filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<TransactionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<TransactionDto>>> GetTransactions(
        [FromQuery] TransactionQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _transactionsService.GetTransactionsAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/transactions/{id}
    /// Returns one household transaction.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> GetTransactionById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionsService.GetTransactionByIdAsync(
            id,
            cancellationToken);
        return Ok(transaction);
    }

    /// <summary>
    /// GET /api/transactions/{id}/merchant-history
    /// Returns spending history for the merchant on this transaction.
    /// </summary>
    [HttpGet("{id:guid}/merchant-history")]
    [ProducesResponseType<MerchantHistoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MerchantHistoryDto>> GetMerchantHistory(
        Guid id,
        [FromQuery] string granularity = "monthly",
        CancellationToken cancellationToken = default)
    {
        var history = await _transactionsService.GetMerchantHistoryAsync(
            id,
            granularity,
            cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// PATCH /api/transactions/{id}
    /// Updates a transaction's date, category, notes, and, for a manual
    /// entry or CSV import, its name and amount.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> UpdateTransactionDetails(
        Guid id,
        [FromBody] UpdateTransactionDetailsDto dto,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionsService.UpdateTransactionDetailsAsync(
            id,
            dto,
            cancellationToken);
        return Ok(transaction);
    }

    /// <summary>
    /// PATCH /api/transactions/{id}/category
    /// Changes a transaction's category and keeps that choice across Plaid sync.
    /// </summary>
    [HttpPatch("{id:guid}/category")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> UpdateTransactionCategory(
        Guid id,
        [FromBody] UpdateTransactionCategoryDto dto,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionsService.UpdateTransactionCategoryAsync(
            id,
            dto,
            cancellationToken);
        return Ok(transaction);
    }

    /// <summary>
    /// POST /api/transactions
    /// Creates a manual transaction on a household account.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> CreateManualTransaction(
        [FromBody] CreateManualTransactionDto dto,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionsService.CreateManualTransactionAsync(
            dto,
            cancellationToken);
        return Ok(transaction);
    }

    /// <summary>
    /// POST /api/transactions/{id}/archive
    /// Archives a transaction so it leaves activity and a manual balance.
    /// </summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> ArchiveTransaction(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionsService.ArchiveTransactionAsync(
            id,
            cancellationToken);
        return Ok(transaction);
    }

    /// <summary>
    /// POST /api/transactions/{id}/restore
    /// Restores an archived transaction and recalculates a manual balance.
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> RestoreTransaction(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionsService.RestoreTransactionAsync(
            id,
            cancellationToken);
        return Ok(transaction);
    }
}
