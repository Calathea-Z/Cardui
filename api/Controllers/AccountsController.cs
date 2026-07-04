using Microsoft.AspNetCore.Mvc;
using Cardui.Api.Services.Interfaces;
using Cardui.Api.Dtos.Account;

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
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAccounts()
    {
        var accounts = await _accountsService.GetAccountsAsync();
        return Ok(accounts);
    }
}