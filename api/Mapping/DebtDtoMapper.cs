using System.Linq.Expressions;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Models;

namespace Cardui.Api.Mapping;

public static class DebtDtoMapper
{
    public static readonly Expression<Func<Debt, DebtDto>> Projection =
        debt => new DebtDto
        {
            Id = debt.Id,
            Name = debt.Name,
            Kind = debt.Kind,
            AccountId = debt.AccountId,
            AccountName = debt.Account == null ? null : debt.Account.Name,
            Balance = debt.Balance,
            BalanceAsOf = debt.BalanceAsOf,
            Currency = debt.Currency,
            Apr = debt.Apr,
            MinimumPayment = debt.MinimumPayment,
            NextDueDate = debt.NextDueDate,
            CreditLimit = debt.CreditLimit,
            RemainingTermMonths = debt.RemainingTermMonths,
            PromotionalApr = debt.PromotionalApr,
            PromotionalEndsOn = debt.PromotionalEndsOn
        };
}
