using Microsoft.AspNetCore.Mvc;
using Cardui.Api.Services.Interfaces;
using Cardui.Api.Dtos.Transaction;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionsService _transactionsService;

    public TransactionsController(ITransactionsService transactionsService){
        _transactionsService = transactionsService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> GetTransactions(
        [FromQuery] TransactionQueryDto query)
    {
        var transactions = await _transactionsService.GetTransactionsAsync(query);
        return Ok(transactions);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> GetTransactionById(Guid id)
    {
        var transaction = await _transactionsService.GetTransactionByIdAsync(id);

        if (transaction is null)
        {
            return NotFound();
        }

        return Ok(transaction);
    }
    
    [HttpPatch("{id:guid}/category")]
    public async Task<ActionResult<TransactionDto>> UpdateTransactionCategory(
        Guid id, [FromBody] UpdateTransactionCategoryDto dto)
    {
        var transaction = await _transactionsService.UpdateTransactionCategoryAsync(id, dto);

        if (transaction is null)
        {
            return NotFound();
        }

        return Ok(transaction);
    }
}