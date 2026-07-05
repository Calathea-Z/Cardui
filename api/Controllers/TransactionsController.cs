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

    [HttpGet]
    [ProducesResponseType<PagedResultDto<TransactionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions([FromQuery] TransactionQueryDto query)
    {
        var result = await _transactionsService.GetTransactionsAsync(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionById(Guid id)
    {
        var transaction = await _transactionsService.GetTransactionByIdAsync(id);
        return Ok(transaction);
    }

    [HttpPatch("{id:guid}/category")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTransactionCategory(
        Guid id,
        [FromBody] UpdateTransactionCategoryDto dto)
    {
        var transaction = await _transactionsService.UpdateTransactionCategoryAsync(id, dto);
        return Ok(transaction);
    }
}
