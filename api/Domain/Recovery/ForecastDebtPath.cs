namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One debt and the schedule used for a forecast.
/// The schedule starts on the forecast date. Payments before that date are not in it.
/// </summary>
internal sealed record ForecastDebtPath(
    DebtAmortizationInput Input,
    DebtSchedule Schedule);
