import type {
  DecisionPlanActionDto,
  DecisionPlanDetailDto,
  DecisionPlanDto,
  DecisionPlanMilestoneDto,
} from "@/lib/api/types";

/**
 * Formats a money amount in the planning currency.
 */
type FormatMoney = (amount: number) => string;

/**
 * Formats a `YYYY-MM-DD` date for on-screen text.
 */
type FormatDate = (date: string) => string;

/**
 * One plain-language line: a short label and the sentence under it.
 */
export type DecisionLine = {
  term: string;
  detail: string;
};

/**
 * The three parts of one ranked action.
 */
export type DecisionActionCopy = {
  title: string;
  why: string;
  effect: string;
};

/**
 * The title, reason, and cash-flow effect for one ranked action.
 * Amounts and dates come from the action. A missing date is left out rather than invented.
 */
export function decisionActionCopy(
  action: DecisionPlanActionDto,
  money: FormatMoney,
  date: FormatDate,
): DecisionActionCopy {
  switch (action.kind) {
    case "CoverCashShortfall":
      return {
        title: "Cover the cash shortfall",
        why: "Dated cash goes below zero on this plan, so a payoff date stays conditional until that gap is covered.",
        effect: action.on
          ? `Cash first runs short on ${date(action.on)}.`
          : "Cash runs short inside this plan.",
      };
    case "LowerExtra":
      return {
        title: "Lower the extra payment",
        why: lowerExtraWhy(action),
        effect: lowerExtraEffect(action, money, date),
      };
    case "AddStartingCash":
      return {
        title: "Add the cash this plan starts from",
        why: "Plan starts from active cash accounts in the household currency. None are included.",
        effect:
          "Until cash is included, this plan cannot show what is safe to use.",
      };
    case "AddDebt":
      return {
        title: "Add the debts to pay",
        why: "This plan has no card or loan to put in a payoff order.",
        effect: "There is no payment to redirect until a debt is added.",
      };
    case "AddIncome":
      return {
        title: "Add take-home pay",
        why: "Dated pay is what this plan uses to judge whether bills and debt payments fit.",
        effect:
          "Without included pay, a surplus would be invented. This plan does not invent one.",
      };
    case "FinishDebtTerms":
      return {
        title: "Fill in the missing debt terms",
        why: finishTermsWhy(action),
        effect:
          "This plan does not date a payoff for those debts until the terms are known.",
      };
    case "RefreshBalances":
      return {
        title: "Refresh the balances this plan uses",
        why: `${countNoun(action.count ?? 0, "balance", "balances")} used here ${action.count === 1 ? "is" : "are"} not current.`,
        effect:
          "This version keeps the amounts from its plan date. Save a new version after a refresh if the plan should use the new balances.",
      };
    case "HoldIncomeLoss":
      return {
        title: "Plan for pay that is left out",
        why: `This plan leaves out included pay for ${months(action.count ?? 0)}${action.on ? `, starting ${date(action.on)}` : ""}.`,
        effect:
          "Later pay stays on its dates. Bills and debt payments in the gap still come due.",
      };
    case "HoldWindfall":
      return {
        title: "Leave the windfall in cash",
        why: windfallWhy(action, money, date),
        effect:
          "The cash outlook includes it. It does not pay a debt, and it is not treated as pay you can count on.",
      };
    case "PayFirstDebt":
      return {
        title: action.name ? `Pay ${action.name} first` : "Pay the first debt",
        why: "It is first in payment priority. That is who receives extra and a rolled payment, not the date the debt is paid off.",
        effect: payFirstEffect(action, money, date),
      };
    case "UseFreedPayment":
      return {
        title:
          action.nextName &&
          (action.extra ?? 0) > 0 &&
          (action.extra ?? 0) < (action.amount ?? 0)
            ? "Keep some of the freed payment"
            : action.nextName
              ? `Send the freed payment to ${action.nextName}`
              : "Keep the freed payment",
        why: freedWhy(action, money),
        effect: freedEffect(action, money, date),
      };
    case "ConfirmBills":
      return {
        title: "Confirm recurring bills",
        why: "No non-debt bills are included. Debt payments stay on Debts.",
        effect:
          "If a bill is missing, cash will fall short of this plan when that bill comes due.",
      };
    case "SetFlexibleSpending":
      return {
        title: "Set monthly flexible spending",
        why: "This plan has no amount for ordinary spending besides bills and debt payments.",
        effect:
          "Spendable cash cannot reserve ordinary spending until that amount is set.",
      };
    case "ChooseCashToKeep":
      return {
        title: "Choose cash to keep",
        why: "This plan does not protect a cash floor before debt payments.",
        effect:
          "Available cash can fall to the last dollar. Set a floor on Savings if some cash should stay untouched.",
      };
    default:
      return {
        title: "Review this action",
        why: "This plan includes an action the page does not describe yet.",
        effect: "The stored plan is unchanged.",
      };
  }
}

/**
 * One assumption, in the order the plan stored it.
 */
export function decisionAssumptionCopy(
  detail: DecisionPlanDetailDto,
  plan: DecisionPlanDto,
  money: FormatMoney,
  date: FormatDate,
): DecisionLine {
  switch (detail.kind) {
    case "Order":
      return {
        term: "Payment priority",
        detail: savedPriorityDetail(plan, money, detail.amount ?? 0),
      };
    case "Payments":
      return {
        term: "Payments",
        detail: "Each debt pays its minimum on its monthly due date.",
      };
    case "Interest":
      return {
        term: "Interest",
        detail:
          "One month of interest at the debt's rate, or its promo rate until that ends.",
      };
    case "PayoffMonth":
      return {
        term: "Payoff month",
        detail:
          "The month a debt is paid off still pays it. Its payment is free the month after.",
      };
    case "DueDates":
      return {
        term: "Due dates",
        detail:
          "A due date that has passed moves ahead a month at a time to the first one on or after the plan date.",
      };
    case "FreedPayment":
      return savedFreedPayment(plan, money);
    case "CashOutlook":
      return {
        term: "Cash outlook",
        detail:
          "Starts with cash on Accounts, adds shared pay, and takes out bills, living spending, and debt payments. Set-aside money stays in cash.",
      };
    case "Spendable":
      return {
        term: "Spendable",
        detail:
          "The lowest cash left after set-aside money, dated bills, debt payments, and ordinary spending. Only steady pay counts.",
      };
    case "LowPay":
      return {
        term: "Low pay",
        detail:
          "A low-pay comparison uses each source's low amount where one is recorded. This saved plan uses typical pay.",
      };
    case "LeftOut":
      return {
        term: "Left out",
        detail:
          "A debt missing a balance, rate, minimum, or due date, or one whose payment does not cover its interest, is named under Missing information.",
      };
    case "Currency":
      return {
        term: "Currency",
        detail: `Only ${plan.planningCurrency} is counted.`,
      };
    case "Limit":
      return {
        term: "Limit",
        detail: "Projections stop at 50 years.",
      };
    case "IncomeLoss":
      return {
        term: "Income loss",
        detail: `The next ${months(detail.count ?? 0)} leave out included pay. Later pay stays on its dates.`,
      };
    case "Windfall":
      return {
        term: "Windfall",
        detail: `${money(detail.amount ?? 0)}${detail.on ? ` on ${date(detail.on)}` : ""} stays in cash and does not pay a debt.`,
      };
    case "SpendingChange":
      return {
        term: "Spending",
        detail: `This plan uses ${money(detail.amount ?? 0)} a month for flexible spending.`,
      };
    case "CashFloorChange":
      return {
        term: "Cash to keep",
        detail: `This plan protects ${money(detail.amount ?? 0)}. Other set-aside money stays in cash.`,
      };
    case "ScenarioOrder":
      return {
        term: "Payoff order",
        detail: "These cash changes do not change which debt is paid first.",
      };
    default:
      return {
        term: "Assumption",
        detail:
          "This plan records an assumption the page does not describe yet.",
      };
  }
}

/**
 * One risk. A shortfall or an unaffordable extra makes a payoff date conditional.
 */
export function decisionRiskCopy(
  detail: DecisionPlanDetailDto,
  money: FormatMoney,
  date: FormatDate,
): DecisionLine {
  switch (detail.kind) {
    case "CashShortfall":
      return {
        term: "Cash shortfall",
        detail: detail.on
          ? `Cash goes below zero on ${date(detail.on)}. Payoff dates in this plan are conditional until that is covered.`
          : "Cash goes below zero. Payoff dates in this plan are conditional until that is covered.",
      };
    case "ReserveShortfall":
      return {
        term: "Protected savings",
        detail: detail.on
          ? `Cash left after protected savings goes below zero on ${date(detail.on)}.`
          : "Cash left after protected savings goes below zero.",
      };
    case "ExtraGuardrail":
      return {
        term: "Extra payment",
        detail: extraGuardrailDetail(detail, money, date),
      };
    case "PayoffConditional":
      return {
        term: "Payoff date",
        detail:
          "A payoff date in this plan depends on cash staying available and on any missing terms being filled in.",
      };
    case "DoesNotPayDown":
      return {
        term: detail.name ?? "Debt",
        detail:
          "Its payment does not bring the balance down. The plan stops that debt there.",
      };
    case "HorizonReached":
      return {
        term: detail.name ?? "Debt",
        detail: "It is still open when the projection stops.",
      };
    case "ExcludedCurrency":
      return {
        term: detail.name ?? "Currency",
        detail: "It is left out. This plan counts only the planning currency.",
      };
    case "StaleBalance":
      return {
        term: "Balances",
        detail: `${countNoun(detail.count ?? 0, "balance was", "balances were")} not current on the plan date. This version keeps the amounts from that date.`,
      };
    case "IncomeLossRisk":
      return {
        term: "Income loss",
        detail: `Included pay is left out for ${months(detail.count ?? 0)}. The result is for that case, not a normal month.`,
      };
    case "WindfallNotApplied":
      return {
        term: "Windfall",
        detail: "The windfall stays in cash. It is not a payment toward debt.",
      };
    case "WindfallLeftOut":
      return {
        term: "Windfall",
        detail: "A windfall in the outlook is not counted as spendable pay.",
      };
    case "NoReliableIncome":
      return {
        term: "Steady pay",
        detail:
          "No steady pay is included, so spendable cash does not count uncertain or variable pay as money you can rely on.",
      };
    default:
      return {
        term: "Risk",
        detail: "This plan records a risk the page does not describe yet.",
      };
  }
}

/**
 * One piece of missing information. A blank is missing, not a stored zero.
 */
export function decisionMissingCopy(
  detail: DecisionPlanDetailDto,
): DecisionLine {
  const name = detail.name ?? "A debt";
  switch (detail.kind) {
    case "MissingBalance":
      return {
        term: name,
        detail: "It has no balance, so it is not in the payoff order.",
      };
    case "UnknownRate":
      return { term: name, detail: "It has no interest rate." };
    case "UnknownMinimum":
      return { term: name, detail: "It has no minimum payment." };
    case "UnknownDueDate":
      return { term: name, detail: "It has no due date." };
    case "DoesNotPayDown":
      return {
        term: name,
        detail:
          "Its payment does not cover interest, so a payoff date would be invented.",
      };
    case "NoIncome":
      return { term: "Income", detail: "Take-home pay is not included." };
    case "NoBills":
      return {
        term: "Bills",
        detail: "No recurring non-debt bills are included.",
      };
    case "NoFlexibleSpending":
      return {
        term: "Flexible spending",
        detail: "Monthly flexible spending is not set.",
      };
    case "NoCashFloor":
      return { term: "Cash to keep", detail: "Cash to keep is not set." };
    case "PaymentReview":
      return {
        term: "Zero balances",
        detail: `${countNoun(detail.count ?? 0, "zero-balance debt still has", "zero-balance debts still have")} a saved minimum. They are not active payments.`,
      };
    case "VariableIncomeLeftOut":
      return {
        term: name,
        detail: "Its variable pay is not counted as spendable income.",
      };
    case "UncertainIncomeLeftOut":
      return {
        term: name,
        detail: "Its uncertain pay is not counted as spendable income.",
      };
    case "IncomeLeftOut":
      return {
        term: "Pay",
        detail: "Included pay is left out of part of this plan.",
      };
    case "DebtLeftOut":
      return { term: name, detail: "It is left out of spendable cash." };
    default:
      return {
        term: "Missing",
        detail:
          "This plan is missing something the page does not describe yet.",
      };
  }
}

/**
 * One reason to read this version again. A later edit does not change the version.
 */
export function decisionReviewCopy(
  detail: DecisionPlanDetailDto,
  date: FormatDate,
): DecisionLine {
  switch (detail.kind) {
    case "FactsChange":
      return {
        term: "When facts change",
        detail:
          "Review this plan when take-home pay, a bill, flexible spending, or a debt term changes. Those edits do not change this version.",
      };
    case "ShortfallDate":
      return {
        term: "Shortfall",
        detail: detail.on
          ? `Look again on ${date(detail.on)}, when cash first runs short.`
          : "Look again when cash first runs short.",
      };
    case "IncomeLossEnds":
      return {
        term: "Income returns",
        detail: detail.on
          ? `Look again on ${date(detail.on)}, when the months without included pay end.`
          : "Look again when the months without included pay end.",
      };
    case "RefreshBalances":
      return {
        term: "Balances",
        detail:
          "Refresh the stale balances, then save a new version if the plan should use them.",
      };
    case "ZeroBalanceMinimum":
      return {
        term: "Zero balances",
        detail:
          "Review each zero-balance debt that still has a minimum before treating this plan as complete.",
      };
    case "WindfallArrives":
      return {
        term: "Windfall",
        detail: detail.on
          ? `On ${date(detail.on)}, confirm the windfall stayed in cash unless you choose a different plan.`
          : "When the windfall arrives, confirm it stayed in cash unless you choose a different plan.",
      };
    case "EmergencyChoice":
      return {
        term: "Emergency",
        detail:
          "Decide whether this plan needs an emergency target. Cardui does not choose an amount.",
      };
    default:
      return {
        term: "Review",
        detail:
          "This plan has a review condition the page does not describe yet.",
      };
  }
}

/**
 * One milestone the calculation produced.
 */
export function decisionMilestoneCopy(
  milestone: DecisionPlanMilestoneDto,
  money: FormatMoney,
  date: FormatDate,
): DecisionLine {
  switch (milestone.kind) {
    case "CashShortfall":
      return {
        term: "Cash shortfall",
        detail: milestone.on
          ? `Cash goes below zero on ${date(milestone.on)}.`
          : "Cash goes below zero.",
      };
    case "CashRecovered":
      return {
        term: "Cash recovers",
        detail: milestone.on
          ? `Cash is back to zero or above on ${date(milestone.on)}.`
          : "Cash recovers inside this plan.",
      };
    case "ReserveShortfall":
      return {
        term: "Protected savings",
        detail: milestone.on
          ? `Cash after protected savings goes below zero on ${date(milestone.on)}.`
          : "Cash after protected savings goes below zero.",
      };
    case "FirstPayoff":
      return {
        term: milestone.name ?? "First payoff",
        detail: `${milestone.on ? `Paid off on ${date(milestone.on)}. ` : ""}${milestone.amount !== null ? `Its ${money(milestone.amount)} payment is still paid that month.` : "Its payment is still paid that month."}`,
      };
    case "DebtFree":
      return {
        term: "Debt-free",
        detail: milestone.on
          ? `The last modeled debt is paid off on ${date(milestone.on)}.`
          : "The last modeled debt is paid off.",
      };
    case "BreathingRoom":
      return {
        term: "Breathing room",
        detail: `${money(milestone.amount ?? 0)} a month is free after the last payoff in this plan.`,
      };
    default:
      return {
        term: "Milestone",
        detail: "This plan has a milestone the page does not describe yet.",
      };
  }
}

/**
 * The cash and budget figures to read beside the actions.
 */
export function decisionFactLines(
  plan: DecisionPlanDto,
  money: FormatMoney,
  date: FormatDate,
): DecisionLine[] {
  const facts = plan.facts;
  return [
    { term: "Starting cash", detail: money(facts.startingCash) },
    { term: "Set aside", detail: money(facts.startingReserve) },
    { term: "Available after that", detail: money(facts.startingAvailable) },
    {
      term: "Flexible spending",
      detail:
        facts.livingSpendingMonthly > 0
          ? `${money(facts.livingSpendingMonthly)} a month`
          : "Not set",
    },
    {
      term: "Cash to keep",
      detail: facts.cashFloor > 0 ? money(facts.cashFloor) : "Not set",
    },
    {
      term: "Spendable",
      detail: `${money(facts.spendable)} over the next 30 days`,
    },
    {
      term: "Debt-free",
      detail: facts.paidOffOn ? date(facts.paidOffOn) : "Not dated",
    },
  ];
}

/**
 * The payoff order captured with this plan.
 * A missing rate or minimum stays missing.
 */
export function decisionPriorityLine(
  debt: DecisionPlanDto["debts"][number],
  money: FormatMoney,
): DecisionLine {
  const rate = debt.apr === null ? "Rate missing" : formatRate(debt.apr);
  const minimum =
    debt.minimum === null
      ? "Minimum missing"
      : `${money(debt.minimum)} minimum`;
  return {
    term: debt.name,
    detail: `${money(debt.balance)} · ${rate} · ${minimum}`,
  };
}

/**
 * The scenario this plan kept. An unused preview is omitted.
 */
export function decisionScenarioLines(
  plan: DecisionPlanDto,
  money: FormatMoney,
  date: FormatDate,
): DecisionLine[] {
  const scenario = plan.scenario;
  const lines: DecisionLine[] = [
    {
      term: "Payment priority",
      detail: savedPriorityDetail(plan, money, scenario.monthlyExtra),
    },
    {
      term: "Freed cash",
      detail: savedFreedPayment(plan, money).detail,
    },
    {
      term: "Extra",
      detail:
        scenario.monthlyExtra > 0
          ? `${money(scenario.monthlyExtra)} each month`
          : "Minimums only",
    },
  ];
  if (scenario.incomeLossMonths > 0) {
    lines.push({
      term: "Income loss",
      detail: months(scenario.incomeLossMonths),
    });
  }
  if (scenario.windfallAmount > 0) {
    lines.push({
      term: "Windfall",
      detail: `${money(scenario.windfallAmount)}${scenario.windfallOn ? ` on ${date(scenario.windfallOn)}` : ""}`,
    });
  }
  if (scenario.livingSpendingMonthly !== null) {
    lines.push({
      term: "Flexible spending",
      detail: `${money(scenario.livingSpendingMonthly)} a month`,
    });
  }
  if (scenario.cashFloor !== null) {
    lines.push({
      term: "Cash to keep",
      detail: money(scenario.cashFloor),
    });
  }
  return lines;
}

/**
 * Why the extra payment is in the plan, including when the horizon is already short without it.
 */
function lowerExtraWhy(action: DecisionPlanActionDto) {
  const which =
    action.count === 1
      ? "The extra would leave ordinary spending short."
      : action.count === 2
        ? "The extra would leave a near-term bill or debt payment short."
        : "The extra would leave ordinary spending and a near-term bill or debt payment short.";
  return action.alreadyShortWithoutExtra
    ? `${which} The horizon is already short without the extra.`
    : which;
}

/**
 * The dated gap the extra payment creates.
 */
function lowerExtraEffect(
  action: DecisionPlanActionDto,
  money: FormatMoney,
  date: FormatDate,
) {
  const when = action.on ? `On ${date(action.on)}, ` : "";
  const gap =
    action.amount !== null
      ? `the day is short by ${money(action.amount)}.`
      : "the day is short.";
  const extra =
    action.extra !== null
      ? ` This plan adds ${money(action.extra)} a month.`
      : "";
  return `${when}${gap}${extra}`;
}

/**
 * Names how many debts still need a term, and one of them.
 */
function finishTermsWhy(action: DecisionPlanActionDto) {
  const count = action.count ?? 0;
  const noun = count === 1 ? "debt still needs" : "debts still need";
  const named = action.name ? ` ${action.name} is one of them.` : "";
  return `${count} ${noun} a balance, rate, minimum, due date, or a payment that covers interest.${named}`;
}

/**
 * Says the windfall stays in cash on its date.
 */
function windfallWhy(
  action: DecisionPlanActionDto,
  money: FormatMoney,
  date: FormatDate,
) {
  const amount = action.amount !== null ? money(action.amount) : "The windfall";
  const when = action.on ? ` on ${date(action.on)}` : "";
  return `${amount}${when} is extra cash in this plan. It does not pay a debt.`;
}

/**
 * The payment, any extra, and the payoff date when the plan has one.
 */
function payFirstEffect(
  action: DecisionPlanActionDto,
  money: FormatMoney,
  date: FormatDate,
) {
  const parts = [
    action.amount !== null
      ? `Pay ${money(action.amount)} on its due date.`
      : "",
    action.extra !== null
      ? `Add ${money(action.extra)} a month to the first debt that can take it.`
      : "",
    action.on
      ? `This plan has it paid off on ${date(action.on)}.`
      : "This plan does not have a payoff date for it yet.",
  ].filter(Boolean);
  return parts.join(" ");
}

/**
 * The stored payment priority. An older plan with no priority is avalanche.
 */
function savedPriorityDetail(
  plan: DecisionPlanDto,
  money: FormatMoney,
  extra: number,
) {
  const priority = plan.scenario.priority ?? "Avalanche";
  const lead =
    priority === "MinimumRelease"
      ? "Remove a minimum first."
      : priority === "Utilization"
        ? "Lower utilization first."
        : priority === "Constraint"
          ? "A chosen debt is paid first or last."
          : "Highest interest rate first.";
  const payment =
    extra > 0
      ? ` ${money(extra)} extra each month goes to the first debt that can take it.`
      : " No extra payment in this plan.";
  return `${lead}${payment}`;
}

/**
 * Where the stored plan sends a freed payment.
 * The payoff month still pays the debt. The freed cash starts the month after.
 */
function savedFreedPayment(
  plan: DecisionPlanDto,
  money: FormatMoney,
): DecisionLine {
  if (plan.kind === "Reclaim") {
    const amount = plan.scenario.reclaimAmount ?? 0;
    return {
      term: "Keep some",
      detail:
        amount > 0
          ? `Keep ${money(amount)} of each freed payment the month after it ends. The rest rolls.`
          : "Keep part of each freed payment the month after it ends. The rest rolls.",
    };
  }

  if (plan.kind === "ReclaimAll") {
    return {
      term: "Free up cash",
      detail:
        "A freed payment becomes available the month after it ends. It is not sent to the next debt.",
    };
  }

  return {
    term: "Roll payments forward",
    detail:
      "A freed payment goes to the next debt the month after it ends. It becomes breathing room once no debt can take it.",
  };
}

/**
 * The payment that ends, which is still paid in the payoff month.
 */
function freedWhy(action: DecisionPlanActionDto, money: FormatMoney) {
  const name = action.name ?? "That debt";
  const amount =
    action.amount !== null
      ? `its ${money(action.amount)} payment`
      : "its payment";
  return `When ${name} is paid off, ${amount} is still paid that month and is free afterward.`;
}

/**
 * Where the freed payment goes, or that this plan does not show that later month.
 */
function freedEffect(
  action: DecisionPlanActionDto,
  money: FormatMoney,
  date: FormatDate,
) {
  if (!action.on) {
    return "This plan does not show the later month when that payment is free.";
  }

  const when = `Starting ${date(action.on)}, `;
  const kept = action.extra ?? 0;
  const minimum = action.amount ?? 0;
  if (action.nextName && kept > 0 && kept < minimum) {
    return `${when}keep ${money(kept)} and send the rest to ${action.nextName}.`;
  }

  if (action.nextName) {
    return `${when}that payment goes to ${action.nextName}.`;
  }

  const amount = action.amount !== null ? money(action.amount) : "that payment";
  return `${when}${amount} becomes available. It is not sent to another debt.`;
}

/**
 * The extra-payment risk, including a shortfall that exists without the extra.
 */
function extraGuardrailDetail(
  detail: DecisionPlanDetailDto,
  money: FormatMoney,
  date: FormatDate,
) {
  const amount = detail.amount !== null ? ` by ${money(detail.amount)}` : "";
  const when = detail.on ? ` on ${date(detail.on)}` : "";
  const already =
    detail.count === 1 ? " The shortfall is there without the extra too." : "";
  return `The extra payment leaves the horizon short${amount}${when}.${already}`;
}

/**
 * A count plus a singular or plural label.
 */
function countNoun(count: number, singular: string, plural: string) {
  return `${count} ${count === 1 ? singular : plural}`;
}

/**
 * A whole number of months, with the singular when there is one.
 */
function months(count: number) {
  return count === 1 ? "1 month" : `${count} months`;
}

/**
 * Formats an APR percent. 19.99 is 19.99%, and trailing zeros are dropped.
 */
function formatRate(value: number) {
  const text = value.toFixed(3).replace(/\.?0+$/, "");
  return `${text}%`;
}
