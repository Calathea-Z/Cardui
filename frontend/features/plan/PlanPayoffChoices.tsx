import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { formatCalendarDate } from "@/features/debts/debtDisplay";
import type { PayoffConstraintKind, PlanRecoveryDto } from "@/lib/api/types";
import {
  firstPaidName,
  freedCashLine,
  freedCashOptions,
  payoffPriorityOptions,
  priorityOrderLine,
  priorityTradeoff,
  recoveryPath,
} from "./planPayoffChoice";
import type { ScenarioDraft, ScenarioDraftErrors } from "./planScenario";

type PlanPayoffChoicesProps = {
  draft: ScenarioDraft;
  errors: ScenarioDraftErrors;
  report: PlanRecoveryDto;
  onDraft: (draft: ScenarioDraft) => void;
};

const constraintOptions: { value: PayoffConstraintKind; label: string }[] = [
  { value: "PayFirst", label: "Pay first" },
  { value: "PayLast", label: "Pay last" },
];

/**
 * Payment priority and how much freed cash to keep.
 * The tradeoff lines are the last calculated preview. Update preview applies the selected path.
 * Payment priority is who is paid first. Payoff dates stay on Debt payoff.
 */
export function PlanPayoffChoices({
  draft,
  errors,
  report,
  onDraft,
}: PlanPayoffChoicesProps) {
  const money = (amount: number) =>
    formatCurrency(amount, report.planningCurrency);
  const listed =
    report.rollover.debts.length > 0 ? report.rollover.debts : report.debtFacts;
  const debts = listed.map((debt) => ({
    value: debt.debtId,
    label: debt.name,
  }));
  const none = recoveryPath(report, "Rollover");
  const interestKnown =
    Boolean(none.paidOffOn) &&
    none.debts.every((debt) => debt.stop === "PaidOff");

  return (
    <div className="flex flex-col gap-4">
      <fieldset className="flex flex-col gap-2">
        <legend className="text-sm font-medium text-foreground">
          Payment priority
        </legend>
        <p className="text-sm text-muted-foreground">
          Who receives extra and a rolled payment. This is not the date each
          debt is paid off.
        </p>
        {payoffPriorityOptions.map((option) => {
          const alternative = report.priorityAlternatives.find(
            (item) => item.choice === option.value,
          );
          return (
            <label
              key={option.value}
              className="flex min-h-11 cursor-pointer items-start gap-3 rounded-md border border-border bg-card px-3 py-2"
            >
              <input
                type="radio"
                name="plan-payment-priority"
                className="mt-1"
                checked={draft.priority === option.value}
                onChange={() =>
                  onDraft({
                    ...draft,
                    priority: option.value,
                  })
                }
              />
              <span className="min-w-0">
                <span className="block text-sm font-medium text-foreground">
                  {option.label}
                </span>
                {alternative ? (
                  <>
                    <span className="mt-1 block text-xs text-muted-foreground">
                      {priorityOrderLine(report, alternative.debtIds)}
                    </span>
                    <span className="mt-1 block text-xs text-muted-foreground">
                      {priorityTradeoff(
                        alternative,
                        money,
                        firstPaidName(report, alternative.debtIds),
                      )}
                    </span>
                  </>
                ) : null}
              </span>
            </label>
          );
        })}
        {draft.priority === "Constraint" ? (
          <div className="grid gap-3 sm:grid-cols-2">
            <Select
              label="Debt"
              title="Debt to move"
              placeholder="Choose a debt"
              value={draft.constraintDebtId}
              options={debts}
              onChange={(constraintDebtId) =>
                onDraft({ ...draft, constraintDebtId })
              }
            />
            <Select
              label="Where it sits"
              title="Pay first or last"
              placeholder="Pay first or last"
              value={draft.constraintKind}
              options={constraintOptions}
              onChange={(value) =>
                onDraft({
                  ...draft,
                  constraintKind: value as PayoffConstraintKind,
                })
              }
            />
          </div>
        ) : null}
        {errors.constraint ? (
          <p className="text-sm text-destructive">
            Choose a debt and whether to pay it first or last.
          </p>
        ) : null}
      </fieldset>

      <fieldset className="flex flex-col gap-2">
        <legend className="text-sm font-medium text-foreground">
          Freed cash
        </legend>
        <p className="text-sm text-muted-foreground">
          A freed payment starts the month after the modeled payment ends. The
          payoff month still pays that debt.
        </p>
        {freedCashOptions.map((option) => (
          <label
            key={option.value}
            className="flex min-h-11 cursor-pointer items-start gap-3 rounded-md border border-border bg-card px-3 py-2"
          >
            <input
              type="radio"
              name="plan-freed-cash"
              className="mt-1"
              checked={draft.kind === option.value}
              onChange={() => onDraft({ ...draft, kind: option.value })}
            />
            <span className="min-w-0">
              <span className="block text-sm font-medium text-foreground">
                {option.label}
              </span>
              <span className="mt-1 block text-xs text-muted-foreground">
                {freedCashLine(
                  option.value,
                  recoveryPath(report, option.value),
                  interestKnown,
                  money,
                  formatCalendarDate,
                )}
              </span>
            </span>
          </label>
        ))}
        {draft.kind === "Reclaim" ? (
          <div className="flex flex-col gap-1">
            <label
              htmlFor="plan-reclaim-amount"
              className="text-sm font-medium text-foreground"
            >
              Amount to keep each month
            </label>
            <Input
              id="plan-reclaim-amount"
              inputMode="decimal"
              value={draft.reclaimAmount}
              aria-invalid={errors.reclaim ? true : undefined}
              onChange={(event) =>
                onDraft({ ...draft, reclaimAmount: event.target.value })
              }
            />
            <p className="text-xs text-muted-foreground">
              The rest still rolls to the next debt. An amount above that
              month&apos;s freed cash keeps all of it.
            </p>
            {errors.reclaim ? (
              <p className="text-sm text-destructive">
                Enter how much freed cash to keep each month.
              </p>
            ) : null}
          </div>
        ) : null}
      </fieldset>
    </div>
  );
}
