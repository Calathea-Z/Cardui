"use client";

import { PageHeader } from "@/components/navigation/page-header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type { AllocationPageDto } from "@/lib/api/types";
import {
  allocationStatusSentence,
  leftOutSentence,
  staysAvailableSentence,
  variableSplitSentence,
  windfallSentence,
} from "./allocationCopy";
import { cashPercent, parsePercent, splitAllocation } from "./allocationRules";
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

  return (
    <div className="flex flex-col gap-6">
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
        {allocationStatusSentence(allocation.page.isSaved)}
      </p>

      <section className="flex flex-col gap-3">
        <h2 className="app-section-title">Variable income</h2>
        <p className="max-w-xl text-sm text-muted-foreground">
          This is the amount above typical pay on a source marked Variable, when
          a strong payment is recorded. Steady and uncertain pay stay whole.
        </p>
        <div className="grid gap-3 sm:grid-cols-2">
          <PercentField
            id="variable-income-debt"
            label="Extra debt percent"
            help="Whole percent of the amount above typical pay."
            value={allocation.form.variableIncomeDebtPercent}
            onChange={(value) =>
              allocation.setField("variableIncomeDebtPercent", value)
            }
          />
          <PercentField
            id="variable-income-savings"
            label="Savings percent"
            help="Whole percent set aside. It does not change a savings balance."
            value={allocation.form.variableIncomeSavingsPercent}
            onChange={(value) =>
              allocation.setField("variableIncomeSavingsPercent", value)
            }
          />
        </div>
        <p className="text-sm text-foreground">
          {variableCash === null
            ? "Enter debt and savings percents that add up to 100 or less."
            : staysAvailableSentence(variableCash)}
        </p>
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
          This is the split to review for a bonus, tax refund, or other one-time
          amount. It does not change the windfall tried on Plan.
        </p>
        <div className="grid gap-3 sm:grid-cols-2">
          <PercentField
            id="windfall-debt"
            label="Extra debt percent"
            help="Whole percent that would be extra debt. It does not pay a debt."
            value={allocation.form.windfallDebtPercent}
            onChange={(value) =>
              allocation.setField("windfallDebtPercent", value)
            }
          />
          <PercentField
            id="windfall-savings"
            label="Savings percent"
            help="Whole percent that would be set aside. It does not move money."
            value={allocation.form.windfallSavingsPercent}
            onChange={(value) =>
              allocation.setField("windfallSavingsPercent", value)
            }
          />
        </div>
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

type PercentFieldProps = {
  id: string;
  label: string;
  help: string;
  value: string;
  onChange: (value: string) => void;
};

/**
 * One percent field.
 * The label is the accessible name. The help text says what the percent applies to.
 */
function PercentField({ id, label, help, value, onChange }: PercentFieldProps) {
  return (
    <label className="flex flex-col gap-1.5 text-sm font-medium" htmlFor={id}>
      {label}
      <Input
        id={id}
        inputMode="numeric"
        value={value}
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
