"use client";

import Link from "next/link";
import { useEffect, useRef, type RefObject } from "react";
import { TriangleAlert } from "lucide-react";
import { Button, buttonVariants } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { Input } from "@/components/ui/input";
import { Select, type SelectOption } from "@/components/ui/select";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { formatApr, formatCalendarDate } from "@/features/debts/debtDisplay";
import type {
  PlanDebtFactDto,
  RestructuringComparisonDto,
  RestructuringFeeFunding,
  RestructuringOfferKind,
} from "@/lib/api/types";
import { cn } from "@/lib/utils";
import {
  breakEvenSentence,
  comparisonAssumptions,
  costSentence,
  debtRateLabel,
  feeText,
  offerKindHelp,
  offerNotApproved,
  offerNotRanked,
  offerRateHelp,
  offerScope,
  opportunitySentence,
  opportunityWarns,
  paymentSentence,
  payoffText,
  termNote,
  unknownCostSentence,
} from "./restructuringCopy";
import {
  debtSelectionBlock,
  type RestructuringFieldErrors,
  type RestructuringFormState,
} from "./restructuringRules";

const offerOptions: SelectOption[] = [
  { value: "ConsolidationLoan", label: "Consolidation loan" },
  { value: "BalanceTransfer", label: "Balance transfer" },
  { value: "ReplacementLoan", label: "Replacement loan" },
];

const feeOptions: SelectOption[] = [
  { value: "Financed", label: "Added to the balance" },
  { value: "Cash", label: "Paid in cash" },
];

type PlanOfferComparisonProps = {
  debts: PlanDebtFactDto[];
  currency: string;
  form: RestructuringFormState;
  comparison: RestructuringComparisonDto | null;
  error: string | null;
  fieldErrors: RestructuringFieldErrors;
  isComparing: boolean;
  stale: boolean;
  onField: <Key extends keyof RestructuringFormState>(
    field: Key,
    value: RestructuringFormState[Key],
  ) => void;
  onToggleDebt: (debtId: string) => void;
  onCompare: () => void;
};

/**
 * Compares one offer with the selected debts.
 * The result leads once it exists. The form stays available to try different terms. Nothing is saved.
 */
export function PlanOfferComparison({
  debts,
  currency,
  form,
  comparison,
  error,
  fieldErrors,
  isComparing,
  stale,
  onField,
  onToggleDebt,
  onCompare,
}: PlanOfferComparisonProps) {
  const money = (amount: number) => formatCurrency(amount, currency);
  const resultRef = useRef<HTMLDivElement>(null);
  const announced = useRef<RestructuringComparisonDto | null>(null);

  useEffect(() => {
    if (!comparison || isComparing || announced.current === comparison) {
      return;
    }

    announced.current = comparison;
    resultRef.current?.scrollIntoView({ block: "nearest" });
    resultRef.current?.focus();
  }, [comparison, isComparing]);
  const selectable = debts.some(
    (debt) => debtSelectionBlock(debt, currency) === null,
  );

  return (
    <section className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <h2 className="app-section-title">Compare an offer</h2>
        <p className="max-w-xl text-sm text-foreground">{offerScope}</p>
        <p className="max-w-xl text-sm text-muted-foreground">
          {offerNotApproved}
        </p>
      </div>

      {!selectable ? (
        <div className="rounded-lg border border-border bg-card">
          <EmptyState
            title="A balance is required"
            description="Add a balance in the planning currency. The comparison uses that full balance."
            action={
              <Link
                href="/debts"
                className={cn(buttonVariants(), "min-h-11 px-4")}
              >
                Go to Debts
              </Link>
            }
          />
        </div>
      ) : null}

      {comparison ? (
        <OfferResult
          resultRef={resultRef}
          comparison={comparison}
          money={money}
          stale={stale}
          isComparing={isComparing}
        />
      ) : null}

      {selectable ? (
        <OfferForm
          debts={debts}
          currency={currency}
          form={form}
          error={error}
          fieldErrors={fieldErrors}
          isComparing={isComparing}
          onField={onField}
          onToggleDebt={onToggleDebt}
          onCompare={onCompare}
        />
      ) : null}

      <details className="rounded-lg border border-border bg-card">
        <summary className="min-h-11 cursor-pointer px-4 py-3 text-sm font-medium text-foreground focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none focus-visible:ring-inset">
          How this comparison works
        </summary>
        <ul className="flex flex-col gap-2 px-4 pb-4 text-sm text-muted-foreground">
          {comparisonAssumptions.map((assumption) => (
            <li key={assumption}>{assumption}</li>
          ))}
        </ul>
      </details>
    </section>
  );
}

/**
 * The comparison result. It states cost, payment, break-even, and cash without ranking the offer.
 */
function OfferResult({
  resultRef,
  comparison,
  money,
  stale,
  isComparing,
}: {
  resultRef: RefObject<HTMLDivElement | null>;
  comparison: RestructuringComparisonDto;
  money: (amount: number) => string;
  stale: boolean;
  isComparing: boolean;
}) {
  const cost = costSentence(comparison, money);
  const unknown = unknownCostSentence(comparison);
  const note = termNote(comparison);
  const opportunity = opportunitySentence(comparison, money);
  const warn = opportunityWarns(comparison);
  const rows = comparisonRows(comparison, money);

  return (
    <div
      ref={resultRef}
      tabIndex={-1}
      aria-live="polite"
      className="flex flex-col gap-4 rounded-lg border border-border bg-card p-4 focus:outline-none"
    >
      {isComparing ? (
        <p className="text-sm text-foreground">
          Comparing these terms. The numbers are still the previous comparison.
        </p>
      ) : null}
      {stale && !isComparing ? (
        <p className="text-sm text-foreground">
          These fields have changed. The numbers are the last comparison.
          Compare offer again to update them.
        </p>
      ) : null}
      <p className="text-sm text-foreground">
        {paymentSentence(comparison, money)}
      </p>
      {cost ? <p className="text-sm text-foreground">{cost}</p> : null}
      {unknown ? (
        <p className="flex items-start gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2 text-sm text-warning">
          <TriangleAlert
            className="mt-0.5 size-4 shrink-0"
            aria-hidden="true"
          />
          <span>{unknown}</span>
        </p>
      ) : null}
      {warn ? (
        <p className="flex items-start gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2 text-sm text-warning">
          <TriangleAlert
            className="mt-0.5 size-4 shrink-0"
            aria-hidden="true"
          />
          <span>{opportunity}</span>
        </p>
      ) : (
        <p className="text-sm text-foreground">{opportunity}</p>
      )}
      <p className="text-sm font-medium text-foreground">{offerNotRanked}</p>
      <div className="flex flex-col gap-3">
        <div className="hidden gap-3 text-xs text-muted-foreground sm:grid sm:grid-cols-[9rem_1fr_1fr]">
          <span>Compared</span>
          <span>Keeping these debts</span>
          <span>This offer</span>
        </div>
        {rows.map((row) => (
          <div
            key={row.label}
            className="grid gap-1 border-b border-border pb-3 sm:grid-cols-[9rem_1fr_1fr] sm:gap-3"
          >
            <p className="text-sm font-medium text-foreground">{row.label}</p>
            <p className="text-sm text-foreground">
              <span className="text-muted-foreground sm:hidden">
                Keeping these debts:{" "}
              </span>
              <span className="tabular-nums">{row.keep}</span>
            </p>
            <p className="text-sm text-foreground">
              <span className="text-muted-foreground sm:hidden">
                This offer:{" "}
              </span>
              <span className="tabular-nums">{row.offer}</span>
            </p>
          </div>
        ))}
      </div>
      {note ? <p className="text-sm text-foreground">{note}</p> : null}
      <p className="text-sm text-foreground">
        {breakEvenSentence(comparison, formatCalendarDate)}
      </p>
      <p className="text-sm text-foreground">
        New balance {money(comparison.newPrincipal)}. Covered balance{" "}
        {money(comparison.coveredBalance)}.
      </p>
    </div>
  );
}

/**
 * The fields for one offer. Compare offer sends them. It does not save them.
 */
function OfferForm({
  debts,
  currency,
  form,
  error,
  fieldErrors,
  isComparing,
  onField,
  onToggleDebt,
  onCompare,
}: {
  debts: PlanDebtFactDto[];
  currency: string;
  form: RestructuringFormState;
  error: string | null;
  fieldErrors: RestructuringFieldErrors;
  isComparing: boolean;
  onField: PlanOfferComparisonProps["onField"];
  onToggleDebt: (debtId: string) => void;
  onCompare: () => void;
}) {
  return (
    <div className="flex max-w-xl flex-col gap-4">
      <div className="flex flex-col gap-1.5">
        <span className="text-sm font-medium" id="offer-kind-label">
          Offer
        </span>
        <Select
          title="Offer"
          value={form.kind}
          options={offerOptions}
          onChange={(value) => onField("kind", value as RestructuringOfferKind)}
          className="min-h-11"
        />
        <p className="text-sm text-muted-foreground">
          {offerKindHelp(form.kind)}
        </p>
      </div>

      <fieldset className="flex flex-col gap-2">
        <legend className="text-sm font-medium">Debts</legend>
        {debts.map((debt) => {
          const blocked = debtSelectionBlock(debt, currency);
          if (blocked) {
            return (
              <p key={debt.debtId} className="text-sm text-muted-foreground">
                {blocked}
              </p>
            );
          }

          const checked = form.debtIds.includes(debt.debtId);
          return (
            <label
              key={debt.debtId}
              className="flex min-h-11 items-center gap-3 rounded-lg border border-border bg-card px-3 text-sm"
            >
              <input
                type="checkbox"
                className="size-4 accent-primary"
                checked={checked}
                onChange={() => onToggleDebt(debt.debtId)}
              />
              <span className="min-w-0 flex-1">{debt.name}</span>
              <span className="tabular-nums text-muted-foreground">
                {formatCurrency(debt.balance ?? 0, currency)}
                {debt.apr !== null ? ` · ${formatApr(debt.apr)}` : ""}
              </span>
            </label>
          );
        })}
        {fieldErrors.debts ? (
          <p role="alert" className="text-sm text-destructive">
            {fieldErrors.debts}
          </p>
        ) : null}
      </fieldset>

      <div className="grid gap-3 sm:grid-cols-2">
        <TextField
          id="offer-apr"
          label="APR"
          value={form.apr}
          suffix="%"
          error={fieldErrors.apr}
          help={offerRateHelp(form.kind)}
          onChange={(value) => onField("apr", value)}
        />
        <TextField
          id="offer-term"
          label="Term in months"
          value={form.termMonths}
          error={fieldErrors.termMonths}
          onChange={(value) => onField("termMonths", value)}
        />
        <TextField
          id="offer-fee"
          label="Fee"
          value={form.fee}
          error={fieldErrors.fee}
          onChange={(value) => onField("fee", value)}
        />
        <div className="flex flex-col gap-1.5">
          <span className="text-sm font-medium">Fee payment</span>
          <Select
            title="Fee payment"
            value={form.feeFunding}
            options={feeOptions}
            onChange={(value) =>
              onField("feeFunding", value as RestructuringFeeFunding)
            }
            className="min-h-11"
          />
        </div>
        <TextField
          id="offer-upfront"
          label="Upfront cash"
          help="Paid from cash and applied to the new balance."
          value={form.upfrontCash}
          error={fieldErrors.upfrontCash}
          onChange={(value) => onField("upfrontCash", value)}
        />
      </div>

      {error &&
      !fieldErrors.debts &&
      !fieldErrors.apr &&
      !fieldErrors.termMonths &&
      !fieldErrors.fee &&
      !fieldErrors.upfrontCash ? (
        <p role="alert" className="text-sm text-destructive">
          {error}
        </p>
      ) : null}

      <Button
        type="button"
        className="min-h-11 w-full sm:w-auto"
        disabled={isComparing}
        onClick={onCompare}
      >
        {isComparing ? "Comparing…" : "Compare offer"}
      </Button>
    </div>
  );
}

/**
 * One labeled amount or count. The visible label is the accessible name.
 */
function TextField({
  id,
  label,
  help,
  value,
  suffix,
  error,
  onChange,
}: {
  id: string;
  label: string;
  help?: string;
  value: string;
  suffix?: string;
  error?: string;
  onChange: (value: string) => void;
}) {
  const helpId = help ? `${id}-help` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [helpId, errorId].filter(Boolean).join(" ") || undefined;

  return (
    <label className="flex flex-col gap-1.5 text-sm font-medium" htmlFor={id}>
      {label}
      <span className="flex items-center gap-2 font-normal">
        <Input
          id={id}
          inputMode="decimal"
          className="min-h-11"
          value={value}
          aria-invalid={error ? true : undefined}
          aria-describedby={describedBy}
          onChange={(event) => onChange(event.target.value)}
        />
        {suffix ? (
          <span className="text-muted-foreground">{suffix}</span>
        ) : null}
      </span>
      {help ? (
        <span id={helpId} className="text-sm font-normal text-muted-foreground">
          {help}
        </span>
      ) : null}
      {error ? (
        <span
          id={errorId}
          role="alert"
          className="text-sm font-normal text-destructive"
        >
          {error}
        </span>
      ) : null}
    </label>
  );
}

/**
 * The side-by-side facts. Payment and cost stay on their own rows so neither is a rank.
 */
function comparisonRows(
  comparison: RestructuringComparisonDto,
  money: (amount: number) => string,
): { label: string; keep: string; offer: string }[] {
  const rates = comparison.debts
    .map(
      (debt) =>
        `${debt.name} ${debtRateLabel(debt, formatApr, formatCalendarDate)}`,
    )
    .join(", ");

  return [
    {
      label: "APR",
      keep: rates || "None",
      offer: formatApr(comparison.offerApr),
    },
    {
      label: "Fee",
      keep: "None",
      offer: feeText(comparison.fee, comparison.feeFunding, money),
    },
    {
      label: "Term",
      keep: "Current minimums",
      offer: `${comparison.termMonths} months`,
    },
    {
      label: "Monthly payment",
      keep:
        comparison.keepMonthlyPayment === null
          ? "Unknown"
          : money(comparison.keepMonthlyPayment),
      offer: money(comparison.offerMonthlyPayment),
    },
    {
      label: "Total interest",
      keep:
        comparison.keepTotalInterest === null
          ? "Unknown"
          : money(comparison.keepTotalInterest),
      offer:
        comparison.offerTotalInterest === null
          ? "Unknown"
          : money(comparison.offerTotalInterest),
    },
    {
      label: "Interest and fees",
      keep:
        comparison.keepTotalCost === null
          ? "Unknown"
          : money(comparison.keepTotalCost),
      offer:
        comparison.offerTotalCost === null
          ? "Unknown"
          : money(comparison.offerTotalCost),
    },
    {
      label: "Payoff",
      keep: payoffText(comparison.keepPaidOffOn, formatCalendarDate),
      offer: payoffText(comparison.offerPaidOffOn, formatCalendarDate),
    },
  ];
}
