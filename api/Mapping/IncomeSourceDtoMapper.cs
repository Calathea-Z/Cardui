using System.Linq.Expressions;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Models;

namespace Cardui.Api.Mapping;

public static class IncomeSourceDtoMapper
{
    public static readonly Expression<Func<IncomeSource, IncomeSourceDto>> Projection =
        source => new IncomeSourceDto
        {
            Id = source.Id,
            Name = source.Name,
            TakeHomeAmount = source.TakeHomeAmount,
            LowTakeHomeAmount = source.LowTakeHomeAmount,
            StrongTakeHomeAmount = source.StrongTakeHomeAmount,
            Currency = source.Currency,
            Cadence = source.Cadence,
            NextPaymentDate = source.NextPaymentDate,
            ContributorId = source.ContributorId,
            ContributorName = source.Contributor == null ? null : source.Contributor.Name,
            Reliability = source.Reliability,
            Raises = source.Raises
                .OrderBy(raise => raise.EffectiveDate)
                .Select(raise => new IncomeRaiseDto
                {
                    Id = raise.Id,
                    EffectiveDate = raise.EffectiveDate,
                    TakeHomeAmount = raise.TakeHomeAmount
                })
                .ToList()
        };
}
