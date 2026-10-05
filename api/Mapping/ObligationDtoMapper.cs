using System.Linq.Expressions;
using Cardui.Api.Dtos.Obligations;
using Cardui.Api.Models;

namespace Cardui.Api.Mapping;

public static class ObligationDtoMapper
{
    public static readonly Expression<Func<Obligation, ObligationDto>> Projection =
        obligation => new ObligationDto
        {
            Id = obligation.Id,
            Name = obligation.Name,
            Amount = obligation.Amount,
            Currency = obligation.Currency,
            Cadence = obligation.Cadence,
            NextDueDate = obligation.NextDueDate,
            AccountId = obligation.AccountId,
            AccountName = obligation.Account == null ? null : obligation.Account.Name,
            Flexibility = obligation.Flexibility
        };
}
