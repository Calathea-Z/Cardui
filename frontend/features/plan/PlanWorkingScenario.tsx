"use client";

import { useState } from "react";
import { CircleAlert, LoaderCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DateField } from "@/components/ui/date-field";
import { Input } from "@/components/ui/input";
import { SegmentedControl } from "@/components/ui/segmented-control";
import {
  formatCurrency,
  moneyCommaError,
} from "@/features/accounts/formatCurrency";
import { formatCalendarDate } from "@/features/debts/debtDisplay";
import type { PayoffRolloverKind, PlanRecoveryDto } from "@/lib/api/types";
import {
  planExtraHelp,
  planExtraInvalid,
  planScenarioStatus,
  planScenarioSummary,
} from "./planCopy";
import { PlanPayoffChoices } from "./PlanPayoffChoices";
import {
  planScenarioComparison,
  scenarioComparisonNote,
  scenarioSafetyWindow,
} from "./planScenarioComparison";
import {
  incomeLossInvalid,
  scenarioAmountInvalid,
  windfallInvalid,
  type ScenarioAmountChoice,
  type ScenarioDraft,
  type ScenarioDraftErrors,
} from "./planScenario";
import type { PlanScenarioStatus } from "./usePlanScenario";

const tasks = [
  "extra",
  "income",
  "windfall",
  "living",
  "protect",
  "payoff",
] as const;

type ScenarioTask = (typeof tasks)[number];

const taskLabels: Record<ScenarioTask, string> = {
  extra: "Pay extra",
  income: "Change income",
  windfall: "Add a windfall",
  living: "Change living costs",
  protect: "Protect more cash",
  payoff: "Change what happens after payoff",
};

const amountChoices = [
  { value: "saved" as const, label: "Use saved amount" },
  { value: "amount" as const, label: "Enter an amount" },
];

type PlanWorkingScenarioProps = {
  draft: ScenarioDraft;
  errors: ScenarioDraftErrors;
  appliedKind: PayoffRolloverKind;
  hasPayoff: boolean;
  baseline: PlanRecoveryDto;
  report: PlanRecoveryDto;
  status: PlanScenarioStatus;
  label: string;
  uncommitted: boolean;
  unsafe: boolean;
  showReview: boolean;
  onDraft: (draft: ScenarioDraft) => void;
  onUpdate: () => void;
  onReset: () => void;
  onReview: () => void;
};

/**
 * The one working scenario above Plan's views.
 * Fields stay in the draft until Update preview. Reset returns to current facts. A failed update leaves the draft as entered.
 */
export function PlanWorkingScenario({
  draft,
  errors,
  appliedKind,
  hasPayoff,
  baseline,
  report,
  status,
  label,
  uncommitted,
  unsafe,
  showReview,
  onDraft,
  onUpdate,
  onReset,
  onReview,
}: PlanWorkingScenarioProps) {
  const [task, setTask] = useState<ScenarioTask>("extra");
  const currency = report.planningCurrency;
  const money = (amount: number) => formatCurrency(amount, currency);
  const applied = {
    monthlyExtra: report.monthlyExtra,
    incomeLossMonths: report.incomeLossMonths,
    windfallAmount: report.windfallAmount,
    windfallOn: report.windfallOn,
    livingSpendingMonthly: report.scenarioLivingSpendingMonthly,
    cashFloor: report.scenarioCashFloor,
    priority: report.priority,
    constraintDebtId: report.constraintDebtId,
    constraintKind: report.constraintKind,
    reclaimAmount: report.reclaimAmount,
  };
  const comparison = planScenarioComparison(
    baseline,
    report,
    appliedKind,
    money,
    formatCalendarDate,
  );
  const statusMessage = status === "ready" ? null : planScenarioStatus(status);
  const windowLabel = scenarioSafetyWindow(
    baseline,
    appliedKind,
    formatCalendarDate,
  );

  return (
    <section
      className="app-panel print:hidden"
      aria-labelledby="plan-scenario-title"
    >
      <div className="border-b border-border px-4 py-3">
        <h2 id="plan-scenario-title" className="app-section-title">
          Working scenario
        </h2>
        <p className="mt-1 text-sm font-medium text-foreground">{label}</p>
        <p className="mt-1 text-sm text-muted-foreground">
          {planScenarioSummary(
            appliedKind,
            hasPayoff,
            applied,
            money,
            formatCalendarDate,
          )}
        </p>
      </div>

      <div className="flex flex-col gap-4 p-4">
        <div
          role="group"
          aria-label="Change to try"
          className="flex flex-wrap gap-2"
        >
          {tasks.map((item) => (
            <button
              key={item}
              type="button"
              aria-pressed={task === item}
              onClick={() => setTask(item)}
              className={`min-h-11 rounded-lg border px-3 text-sm font-medium focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none ${
                task === item
                  ? "border-border bg-card text-foreground"
                  : "border-transparent text-muted-foreground"
              }`}
            >
              {taskLabels[item]}
            </button>
          ))}
        </div>

        <div aria-live="polite">
          {task === "extra" ? (
            <DraftField
              id="plan-extra"
              label="Extra monthly payment"
              help={planExtraHelp()}
              value={draft.monthlyExtra}
              error={
                errors.monthlyExtra
                  ? (moneyCommaError(draft.monthlyExtra) ?? planExtraInvalid())
                  : null
              }
              onChange={(monthlyExtra) => onDraft({ ...draft, monthlyExtra })}
            />
          ) : null}
          {task === "income" ? (
            <DraftField
              id="plan-income-loss"
              label="Months without pay"
              help="0 keeps every paycheck. A number of months leaves out included pay from today, then pay resumes."
              value={draft.incomeLossMonths}
              error={errors.incomeLossMonths ? incomeLossInvalid() : null}
              inputMode="numeric"
              onChange={(incomeLossMonths) =>
                onDraft({ ...draft, incomeLossMonths })
              }
            />
          ) : null}
          {task === "windfall" ? (
            <WindfallDraft
              draft={draft}
              asOf={baseline.cashOutlook.asOf}
              error={errors.windfall ? windfallInvalid() : null}
              onDraft={onDraft}
            />
          ) : null}
          {task === "living" ? (
            <AmountChoiceDraft
              id="plan-spending"
              label="Flexible spending"
              savedLabel={`Saved amount is ${money(baseline.livingSpendingMonthly)} a month.`}
              help="Zero removes later full months. A smaller remainder this month stays."
              choice={draft.livingChoice}
              amount={draft.livingAmount}
              allowAmount={baseline.livingSpendingMonthly > 0}
              blocked="Set flexible monthly spending on the Plan budget before trying a different amount."
              error={livingError(errors.living)}
              onChoice={(livingChoice) => onDraft({ ...draft, livingChoice })}
              onAmount={(livingAmount) => onDraft({ ...draft, livingAmount })}
            />
          ) : null}
          {task === "protect" ? (
            <AmountChoiceDraft
              id="plan-cash-floor"
              label="Cash to keep"
              savedLabel={`Saved floor is ${money(baseline.cashFloor)}. Other money already set aside stays.`}
              help="An entered amount replaces only that floor."
              choice={draft.floorChoice}
              amount={draft.floorAmount}
              allowAmount
              error={floorError(errors.floor)}
              onChoice={(floorChoice) => onDraft({ ...draft, floorChoice })}
              onAmount={(floorAmount) => onDraft({ ...draft, floorAmount })}
            />
          ) : null}
          {task === "payoff" ? (
            report.hasDebts ? (
              <PlanPayoffChoices
                draft={draft}
                errors={errors}
                report={report}
                onDraft={onDraft}
              />
            ) : (
              <p className="text-sm text-muted-foreground">
                Add a debt before choosing who is paid first or what happens to
                a freed payment.
              </p>
            )
          ) : null}
        </div>

        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            className="min-h-11 px-4"
            disabled={status === "updating"}
            onClick={onUpdate}
          >
            {status === "updating"
              ? "Updating preview"
              : status === "error"
                ? "Retry"
                : "Update preview"}
          </Button>
          <Button
            type="button"
            variant="outline"
            className="min-h-11 bg-card px-4"
            disabled={status === "updating"}
            onClick={onReset}
          >
            Reset changes
          </Button>
        </div>

        {uncommitted && status === "ready" ? (
          <p className="text-sm text-foreground">
            These edits are not in the preview yet.
          </p>
        ) : null}
        {statusMessage ? (
          <p
            aria-live="polite"
            className={`flex items-start gap-2 text-sm ${
              status === "error" ? "text-destructive" : "text-muted-foreground"
            }`}
          >
            {status === "updating" ? (
              <LoaderCircle
                aria-hidden
                className="mt-0.5 size-4 shrink-0 animate-spin motion-reduce:animate-none"
              />
            ) : (
              <CircleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
            )}
            {statusMessage}
          </p>
        ) : null}
        <p className="text-xs text-muted-foreground">
          Nothing here is saved. It stays in this browser when you open a setup
          editor.
        </p>
      </div>

      <div className="border-t border-border px-4 py-3">
        <h3 className="text-sm font-semibold text-foreground">
          Compared with current facts
        </h3>
        <p className="mt-1 text-sm text-muted-foreground">
          {scenarioComparisonNote(comparison)}
          {comparison.sameSafetyWindow
            ? ` Steady pay from ${windowLabel}.`
            : ""}
        </p>
        {status !== "ready" ? (
          <p className="mt-1 text-sm text-foreground">
            These figures are the previous result.
          </p>
        ) : null}
        {comparison.windfallNote ? (
          <p className="mt-1 text-sm text-foreground">
            {comparison.windfallNote}
          </p>
        ) : null}
        <div className="mt-3 flex flex-col gap-3">
          {comparison.rows.map((row) => (
            <div
              key={row.label}
              className="grid gap-1 border-b border-border pb-3 sm:grid-cols-3 sm:gap-3"
            >
              <p className="text-sm font-medium text-foreground">{row.label}</p>
              <p className="text-sm text-foreground">
                <span className="text-muted-foreground sm:hidden">
                  Current facts:{" "}
                </span>
                <span className="tabular-nums">{row.current.text}</span>
              </p>
              <p className="text-sm text-foreground">
                <span className="text-muted-foreground sm:hidden">
                  Trying changes:{" "}
                </span>
                <span className="tabular-nums">{row.trying.text}</span>
              </p>
            </div>
          ))}
        </div>
        {showReview && status === "ready" ? (
          <div className="mt-3 flex flex-col items-start gap-2">
            {unsafe ? (
              <p className="text-sm text-foreground">
                This window is still short. Review this plan for what to do
                about that shortfall.
              </p>
            ) : null}
            <Button
              type="button"
              variant="outline"
              className="min-h-11 bg-card px-4"
              onClick={onReview}
            >
              Review this plan
            </Button>
          </div>
        ) : null}
      </div>
    </section>
  );
}

/**
 * The message for a flexible-spending field, or null when it can be sent.
 */
function livingError(code: ScenarioDraftErrors["living"]) {
  if (code === "living-unset") {
    return "Set flexible monthly spending on the Plan budget before trying a different amount.";
  }

  if (code === "living-empty") {
    return "Enter an amount, or use the saved amount.";
  }

  if (code === "living-invalid") {
    return scenarioAmountInvalid();
  }

  return null;
}

/**
 * The message for a cash-floor field, or null when it can be sent.
 */
function floorError(code: ScenarioDraftErrors["floor"]) {
  if (code === "floor-empty") {
    return "Enter an amount, or use the saved amount.";
  }

  if (code === "floor-invalid") {
    return scenarioAmountInvalid();
  }

  return null;
}

type DraftFieldProps = {
  id: string;
  label: string;
  help: string;
  value: string;
  error: string | null;
  inputMode?: "decimal" | "numeric";
  onChange: (value: string) => void;
};

/**
 * One text field in the working scenario.
 * Typing changes the draft only. Update preview is what sends it.
 */
function DraftField({
  id,
  label,
  help,
  value,
  error,
  inputMode = "decimal",
  onChange,
}: DraftFieldProps) {
  const helpId = `${id}-help`;
  const statusId = `${id}-status`;

  return (
    <div className="flex max-w-xl flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-foreground">
        {label}
      </label>
      <Input
        id={id}
        value={value}
        inputMode={inputMode}
        autoComplete="off"
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${helpId} ${statusId}` : helpId}
        className="h-11 min-h-11"
        onChange={(event) => onChange(event.target.value)}
      />
      <p id={helpId} className="text-sm text-muted-foreground">
        {help}
      </p>
      {error ? (
        <p id={statusId} className="text-sm text-destructive">
          {error}
        </p>
      ) : null}
    </div>
  );
}

type WindfallDraftProps = {
  draft: ScenarioDraft;
  asOf: string;
  error: string | null;
  onDraft: (draft: ScenarioDraft) => void;
};

/**
 * The windfall amount and date.
 * Zero with no date clears it. A positive amount needs a date. The amount stays in cash.
 */
function WindfallDraft({ draft, asOf, error, onDraft }: WindfallDraftProps) {
  const helpId = "plan-windfall-help";
  const statusId = "plan-windfall-status";

  return (
    <div className="flex max-w-xl flex-col gap-1.5">
      <label
        htmlFor="plan-windfall"
        className="text-sm font-medium text-foreground"
      >
        One-time windfall
      </label>
      <Input
        id="plan-windfall"
        value={draft.windfallAmount}
        inputMode="decimal"
        autoComplete="off"
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${helpId} ${statusId}` : helpId}
        className="h-11 min-h-11"
        onChange={(event) =>
          onDraft({ ...draft, windfallAmount: event.target.value })
        }
      />
      <span className="text-sm font-medium text-foreground">Windfall date</span>
      <DateField
        title="Windfall date"
        value={draft.windfallOn}
        min={asOf || undefined}
        className="h-11 min-h-11"
        onChange={(windfallOn) => onDraft({ ...draft, windfallOn })}
      />
      <p id={helpId} className="text-sm text-muted-foreground">
        0 means no windfall. An amount needs a date inside the 18-month
        forecast. It stays in cash and is not steady pay.
      </p>
      {error ? (
        <p id={statusId} className="text-sm text-destructive">
          {error}
        </p>
      ) : null}
    </div>
  );
}

type AmountChoiceDraftProps = {
  id: string;
  label: string;
  savedLabel: string;
  help: string;
  choice: ScenarioAmountChoice;
  amount: string;
  allowAmount: boolean;
  blocked?: string;
  error: string | null;
  onChoice: (choice: ScenarioAmountChoice) => void;
  onAmount: (amount: string) => void;
};

/**
 * Saved amount or an entered amount.
 * The choice is named so a blank field is not read as zero.
 */
function AmountChoiceDraft({
  id,
  label,
  savedLabel,
  help,
  choice,
  amount,
  allowAmount,
  blocked,
  error,
  onChoice,
  onAmount,
}: AmountChoiceDraftProps) {
  const helpId = `${id}-help`;
  const statusId = `${id}-status`;

  return (
    <div className="flex max-w-xl flex-col gap-2">
      <p className="text-sm font-medium text-foreground">{label}</p>
      <p className="text-sm text-muted-foreground">{savedLabel}</p>
      {allowAmount ? (
        <>
          <SegmentedControl
            label={label}
            options={amountChoices}
            value={choice}
            onChange={onChoice}
          />
          {choice === "amount" ? (
            <Input
              id={id}
              value={amount}
              inputMode="decimal"
              autoComplete="off"
              aria-label={label}
              aria-invalid={error ? true : undefined}
              aria-describedby={error ? `${helpId} ${statusId}` : helpId}
              className="h-11 min-h-11"
              onChange={(event) => onAmount(event.target.value)}
            />
          ) : null}
          <p id={helpId} className="text-sm text-muted-foreground">
            {help}
          </p>
          {error ? (
            <p id={statusId} className="text-sm text-destructive">
              {error}
            </p>
          ) : null}
        </>
      ) : (
        <p className="text-sm text-muted-foreground">{blocked}</p>
      )}
    </div>
  );
}
