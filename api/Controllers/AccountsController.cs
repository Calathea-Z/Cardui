using Cardui.Api.Dtos.Account;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountsService _accountsService;

    public AccountsController(IAccountsService accountsService)
    {
        _accountsService = accountsService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccountDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAccounts(
        CancellationToken cancellationToken)
    {
        var accounts = await _accountsService.GetAccountsAsync(cancellationToken);
        return Ok(accounts);
    }

    [HttpGet("summary")]
    [ProducesResponseType<AccountSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AccountSummaryDto>> GetAccountsSummary(
        CancellationToken cancellationToken)
    {
        var summary = await _accountsService.GetAccountsSummaryAsync(cancellationToken);
        return Ok(summary);
    }
}
