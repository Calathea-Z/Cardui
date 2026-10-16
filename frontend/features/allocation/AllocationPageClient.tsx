"use client";

import { PageHeader } from "@/components/navigation/page-header";
import { PlanReturn } from "@/components/navigation/plan-return";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type { AllocationPageDto } from "@/lib/api/types";
import {
  allocationEditSentence,
  allocationExampleSentence,
  leftOutSentence,
  staysAvailableSentence,
  variableSplitSentence,
  windfallSentence,
} from "./allocationCopy";
import {
  allocationDraftDiffers,
  allocationSplitError,
  cashPercent,
  parsePercent,
  splitAllocation,
} from "./allocationRules";
import { useAllocationPage } from "./useAllocationPage";

type AllocationPageClientProps = {
  page: AllocationPageDto;
};

/**
 * Allocation setting.
 * The household reviews how extra variable pay and a windfall should be treated. Saving does not move money.
 */
export function AllocationPageClient({
  page: initial,
}: AllocationPageClientProps) {
  const allocation = useAllocationPage(initial);
  const variableCash = pairCash(
    allocation.form.variableIncomeDebtPercent,
    allocation.form.variableIncomeSavingsPercent,
  );
  const windfallCash = pairCash(
    allocation.form.windfallDebtPercent,
    allocation.form.windfallSavingsPercent,
  );
  const dirty = allocationDraftDiffers(allocation.form, allocation.page);
  const variableError = allocationSplitError(
    allocation.form.variableIncomeDebtPercent,
    allocation.form.variableIncomeSavingsPercent,
    "Variable income",
  );
  const windfallError = allocationSplitError(
    allocation.form.windfallDebtPercent,
    allocation.form.windfallSavingsPercent,
    "Windfall",
  );
  const money = (amount: number) =>
    formatCurrency(amount, allocation.page.planningCurrency);

  return (
    <div className="flex flex-col gap-6">
      <PlanReturn />
      <PageHeader
        title="Allocation"
        description="Choose how extra variable pay and a windfall should be treated. Saving this does not move money."
        actions={
          <Button
            type="button"
            className="min-h-11 w-full sm:w-auto"
            disabled={allocation.isSaving}
            onClick={() => void allocation.save()}
          >
            {allocation.isSaving ? "Saving…" : "Save allocation"}
          </Button>
        }
      />

      <p className="text-sm text-foreground">
        {allocationEditSentence(allocation.page.isSaved, dirty)}
      </p>
      {allocation.saveError ? (
        <p role="alert" className="text-sm text-destructive">
          {allocation.saveError}
        </p>
      ) : null}
      <p className="max-w-xl text-sm text-muted-foreground">
        This is the extra-money preference. It is separate from a one-time
        windfall tried on Plan, which stays in cash and is not saved here.
      </p>

      <section className="flex flex-col gap-3">
        <h2 className="app-section-title">Variable income</h2>
        <p className="max-w-xl text-sm text-muted-foreground">
          One split of the amount above typical pay on a source marked Variable.
          Steady and uncertain pay stay whole.
        </p>
        <SplitFields
          debtId="variable-income-debt"
          savingsId="variable-income-savings"
          debtValue={allocation.form.variableIncomeDebtPercent}
          savingsValue={allocation.form.variableIncomeSavingsPercent}
          cash={variableCash}
          error={variableError}
          onDebt={(value) =>
            allocation.setField("variableIncomeDebtPercent", value)
          }
          onSavings={(value) =>
            allocation.setField("variableIncomeSavingsPercent", value)
          }
        />
        <ExampleLine
          debtField={allocation.form.variableIncomeDebtPercent}
          savingsField={allocation.form.variableIncomeSavingsPercent}
          money={money}
        />
        <VariableReviews
          reviews={allocation.page.variableIncomeReviews}
          debtPercent={allocation.form.variableIncomeDebtPercent}
          savingsPercent={allocation.form.variableIncomeSavingsPercent}
          planningCurrency={allocation.page.planningCurrency}
        />
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="app-section-title">Windfall</h2>
        <p className="max-w-xl text-sm text-muted-foreground">
          One split to review for a bonus, tax refund, or other one-time amount.
          It does not change the windfall tried on Plan.
        </p>
        <SplitFields
          debtId="windfall-debt"
          savingsId="windfall-savings"
          debtValue={allocation.form.windfallDebtPercent}
          savingsValue={allocation.form.windfallSavingsPercent}
          cash={windfallCash}
          error={windfallError}
          onDebt={(value) => allocation.setField("windfallDebtPercent", value)}
          onSavings={(value) =>
            allocation.setField("windfallSavingsPercent", value)
          }
        />
        <ExampleLine
          debtField={allocation.form.windfallDebtPercent}
          savingsField={allocation.form.windfallSavingsPercent}
          money={money}
        />
        <p className="text-sm text-foreground">
          {windfallReview(
            allocation.form.windfallDebtPercent,
            allocation.form.windfallSavingsPercent,
            windfallCash,
          )}
        </p>
      </section>
    </div>
  );
}

type SplitFieldsProps = {
  debtId: string;
  savingsId: string;
  debtValue: string;
  savingsValue: string;
  cash: number | null;
  error: string | null;
  onDebt: (value: string) => void;
  onSavings: (value: string) => void;
};

/**
 * Debt, savings, and what stays available as one split.
 * The remainder is calculated. It is not a third field to type.
 */
function SplitFields({
  debtId,
  savingsId,
  debtValue,
  savingsValue,
  cash,
  error,
  onDebt,
  onSavings,
}: SplitFieldsProps) {
  return (
    <div className="flex flex-col gap-2">
      <div className="grid gap-3 sm:grid-cols-3">
        <PercentField
          id={debtId}
          label="Debt"
          help="Whole percent that would be extra debt."
          value={debtValue}
          invalid={error !== null}
          onChange={onDebt}
        />
        <PercentField
          id={savingsId}
          label="Savings"
          help="Whole percent that would be set aside."
          value={savingsValue}
          invalid={error !== null}
          onChange={onSavings}
        />
        <div className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium">Stays available</span>
          <p className="flex min-h-11 items-center tabular-nums text-foreground">
            {cash === null ? "—" : `${cash} percent`}
          </p>
          <span className="text-muted-foreground">
            {cash === null
              ? "Enter debt and savings percents that add up to 100 or less."
              : staysAvailableSentence(cash)}
          </span>
        </div>
      </div>
      {error ? (
        <p role="alert" className="text-sm text-destructive">
          {error}
        </p>
      ) : null}
    </div>
  );
}

type ExampleLineProps = {
  debtField: string;
  savingsField: string;
  money: (amount: number) => string;
};

/**
 * A $100 example for the percents currently in the fields.
 * An invalid pair does not invent a split.
 */
function ExampleLine({ debtField, savingsField, money }: ExampleLineProps) {
  const debt = parsePercent(debtField);
  const savings = parsePercent(savingsField);
  if (!debt.ok || !savings.ok) {
    return null;
  }

  const cash = cashPercent(debt.percent, savings.percent);
  if (cash === null) {
    return null;
  }

  const split = splitAllocation(100, debt.percent, savings.percent);
  return (
    <p className="text-sm text-muted-foreground">
      {allocationExampleSentence(
        money(100),
        money(split.debt),
        money(split.savings),
        money(split.cash),
      )}
    </p>
  );
}

type PercentFieldProps = {
  id: string;
  label: string;
  help: string;
  value: string;
  invalid?: boolean;
  onChange: (value: string) => void;
};

/**
 * One percent field.
 * The label is the accessible name. The help text says what the percent applies to.
 */
function PercentField({
  id,
  label,
  help,
  value,
  invalid = false,
  onChange,
}: PercentFieldProps) {
  return (
    <label className="flex flex-col gap-1.5 text-sm font-medium" htmlFor={id}>
      {label}
      <Input
        id={id}
        inputMode="numeric"
        value={value}
        aria-invalid={invalid || undefined}
        onChange={(event) => onChange(event.target.value)}
        autoComplete="off"
      />
      <span className="font-normal text-muted-foreground">{help}</span>
    </label>
  );
}

type VariableReviewsProps = {
  reviews: AllocationPageDto["variableIncomeReviews"];
  debtPercent: string;
  savingsPercent: string;
  planningCurrency: string;
};

/**
 * Reviews each variable source against the percents currently in the fields.
 * A source that is left out stays named. An invalid pair does not invent a split.
 */
function VariableReviews({
  reviews,
  debtPercent,
  savingsPercent,
  planningCurrency,
}: VariableReviewsProps) {
  if (reviews.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        No income source is marked Variable. This rule waits until one is.
      </p>
    );
  }

  const debt = parsePercent(debtPercent);
  const savings = parsePercent(savingsPercent);
  const cash =
    debt.ok && savings.ok ? cashPercent(debt.percent, savings.percent) : null;

  return (
    <ul className="flex flex-col gap-2">
      {reviews.map((review) => (
        <li key={review.name} className="text-sm text-foreground">
          {reviewSentence(review, debt, savings, cash, planningCurrency)}
        </li>
      ))}
    </ul>
  );
}

/**
 * One review line for a variable source.
 * A left-out source stays named. An invalid percent pair does not invent a split.
 */
function reviewSentence(
  review: AllocationPageDto["variableIncomeReviews"][number],
  debt: ReturnType<typeof parsePercent>,
  savings: ReturnType<typeof parsePercent>,
  cash: number | null,
  planningCurrency: string,
): string {
  if (review.surplus === null || review.leftOut) {
    return leftOutSentence(review.name, review.leftOut ?? "");
  }

  const currency = review.currency || planningCurrency;
  const surplus = formatCurrency(review.surplus, currency);
  if (cash === null || !debt.ok || !savings.ok) {
    return `${review.name} is ${surplus} above typical. Enter percents that add up to 100 or less to review the split.`;
  }

  const split = splitAllocation(review.surplus, debt.percent, savings.percent);
  return variableSplitSentence(
    review.name,
    surplus,
    formatCurrency(split.debt, currency),
    formatCurrency(split.savings, currency),
    formatCurrency(split.cash, currency),
  );
}

/**
 * The windfall review for the percents in the fields.
 * An invalid pair asks for percents instead of stating a split.
 */
function windfallReview(
  debtField: string,
  savingsField: string,
  cash: number | null,
): string {
  if (cash === null) {
    return "Enter debt and savings percents that add up to 100 or less.";
  }

  const debt = parsePercent(debtField);
  const savings = parsePercent(savingsField);
  if (!debt.ok || !savings.ok) {
    return "Enter debt and savings percents that add up to 100 or less.";
  }

  return windfallSentence(debt.percent, savings.percent, cash);
}

/**
 * The available share for two fields, or null when the pair cannot be stored.
 */
function pairCash(debt: string, savings: string): number | null {
  const debtPercent = parsePercent(debt);
  const savingsPercent = parsePercent(savings);
  if (!debtPercent.ok || !savingsPercent.ok) {
    return null;
  }

  return cashPercent(debtPercent.percent, savingsPercent.percent);
}
