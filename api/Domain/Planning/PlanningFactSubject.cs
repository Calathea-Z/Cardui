using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// One household input before it is labeled.
/// RecordSource is set for a cash balance. FieldSource is set for a debt balance or credit limit.
/// Detail is a note, a suggestion key, or a suggested account name. It is not parsed as an amount.
/// </summary>
public sealed record PlanningFactSubject(
    PlanningFactKind Kind,
    Guid? SubjectId,
    Guid? RelatedId,
    string Name,
    string? Detail,
    decimal? Amount,
    string? Currency,
    DateOnly? AsOf,
    FinancialRecordSource? RecordSource,
    DebtFieldSource? FieldSource);
