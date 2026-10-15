"use client";

import { useState, type ReactNode } from "react";
import { DateField } from "@/components/ui/date-field";
import { Input } from "@/components/ui/input";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type { PlanScenarioQuery } from "@/lib/api/types";
import {
  incomeLossInvalid,
  parseIncomeLossMonths,
  parseScenarioAmount,
  parseWindfall,
  planForecastEnd,
  scenarioAmountInvalid,
  windfallInvalid,
} from "./planScenario";

type PlanCashScenarioFieldsProps = {
  applied: PlanScenarioQuery;
  storedSpending: number;
  storedFloor: number;
  asOf: string;
  currency: string;
  retry: boolean;
  onApply: (scenario: PlanScenarioQuery) => void;
};

/**
 * The temporary income, windfall, spending, and cash-floor fields.
 * Blank spending or floor keeps the saved amount. A windfall needs both an amount and a date. Nothing is saved.
 */
export function PlanCashScenarioFields({
  applied,
  storedSpending,
  storedFloor,
  asOf,
  currency,
  retry,
  onApply,
}: PlanCashScenarioFieldsProps) {
  const forecastEnd = planForecastEnd(asOf);
  const money = (amount: number) => formatCurrency(amount, currency);

  return (
    <div className="grid gap-5 md:grid-cols-2">
      <MonthsField
        appliedMonths={applied.incomeLossMonths}
        retry={retry}
        onApply={(incomeLossMonths) =>
          onApply({ ...applied, incomeLossMonths })
        }
      />
      <WindfallField
        appliedAmount={applied.windfallAmount}
        appliedOn={applied.windfallOn}
        asOf={asOf}
        forecastEnd={forecastEnd}
        retry={retry}
        onApply={(windfallAmount, windfallOn) =>
          onApply({ ...applied, windfallAmount, windfallOn })
        }
      />
      <AmountField
        id="plan-spending"
        label="Flexible spending"
        help={
          storedSpending > 0
            ? `Blank keeps the saved ${money(storedSpending)} a month. Zero removes later full months. A smaller remainder this month stays.`
            : "Set flexible monthly spending on Plan budget before trying a different amount."
        }
        disabled={storedSpending <= 0}
        applied={applied.livingSpendingMonthly}
        retry={retry}
        onApply={(livingSpendingMonthly) =>
          onApply({ ...applied, livingSpendingMonthly })
        }
      />
      <AmountField
        id="plan-cash-floor"
        label="Cash to keep"
        help={`Saved floor is ${money(storedFloor)}. Blank keeps it. Other money already set aside stays.`}
        applied={applied.cashFloor}
        retry={retry}
        onApply={(cashFloor) => onApply({ ...applied, cashFloor })}
      />
    </div>
  );
}

type MonthsFieldProps = {
  appliedMonths: number;
  retry: boolean;
  onApply: (months: number) => void;
};

/**
 * Months of included pay to leave out, starting today.
 * Blank is zero. The number is sent when the field is left or Enter is pressed.
 */
function MonthsField({ appliedMonths, retry, onApply }: MonthsFieldProps) {
  const [draft, setDraft] = useState(
    appliedMonths > 0 ? String(appliedMonths) : "",
  );
  const [invalid, setInvalid] = useState(false);
  const helpId = "plan-income-loss-help";
  const statusId = "plan-income-loss-status";

  /**
   * Sends a whole number of months, or marks the field invalid.
   * The same applied number is sent only when it retries a pending or failed request.
   */
  function commit(value: string) {
    const parsed = parseIncomeLossMonths(value);
    if (parsed === null) {
      setInvalid(true);
      return;
    }

    setInvalid(false);
    setDraft(parsed > 0 ? String(parsed) : "");
    if (parsed !== appliedMonths || retry) {
      onApply(parsed);
    }
  }

  return (
    <FieldShell
      id="plan-income-loss"
      label="Months without pay"
      helpId={helpId}
      help="0 keeps every paycheck. A number of months leaves out included pay from today, then pay resumes."
      invalid={invalid}
      statusId={statusId}
      status={incomeLossInvalid()}
    >
      <Input
        id="plan-income-loss"
        value={draft}
        inputMode="numeric"
        autoComplete="off"
        aria-invalid={invalid || undefined}
        aria-describedby={invalid ? `${helpId} ${statusId}` : helpId}
        className="h-11 min-h-11"
        onChange={(event) => {
          setDraft(event.target.value);
          setInvalid(false);
        }}
        onBlur={() => commit(draft)}
        onKeyDown={(event) => {
          if (event.key === "Enter") {
            event.preventDefault();
            commit(draft);
          }
        }}
      />
    </FieldShell>
  );
}

type WindfallFieldProps = {
  appliedAmount: number;
  appliedOn: string | null;
  asOf: string;
  forecastEnd: string | null;
  retry: boolean;
  onApply: (amount: number, on: string | null) => void;
};

/**
 * A one-time amount on one date.
 * Both fields blank clears it. The preview is sent when either field is left, Enter is pressed, or a date is chosen.
 */
function WindfallField({
  appliedAmount,
  appliedOn,
  asOf,
  forecastEnd,
  retry,
  onApply,
}: WindfallFieldProps) {
  const [amount, setAmount] = useState(
    appliedAmount > 0 ? String(appliedAmount) : "",
  );
  const [date, setDate] = useState(appliedOn ?? "");
  const [invalid, setInvalid] = useState(false);
  const helpId = "plan-windfall-help";
  const statusId = "plan-windfall-status";

  /**
   * Sends a complete windfall, or clears both fields, or marks them invalid.
   * The same applied windfall is sent only when it retries a pending or failed request.
   */
  function commit(nextAmount: string, nextDate: string) {
    if (!forecastEnd) {
      setInvalid(true);
      return;
    }

    const parsed = parseWindfall(nextAmount, nextDate, asOf, forecastEnd);
    if (!parsed) {
      setInvalid(true);
      return;
    }

    setInvalid(false);
    setAmount(parsed.amount > 0 ? String(parsed.amount) : "");
    setDate(parsed.on ?? "");
    const unchanged =
      parsed.amount === appliedAmount && parsed.on === appliedOn;
    if (!unchanged || retry) {
      onApply(parsed.amount, parsed.on);
    }
  }

  return (
    <FieldShell
      id="plan-windfall"
      label="One-time windfall"
      helpId={helpId}
      help="A one-time amount on one date. It stays in cash and does not pay a debt."
      invalid={invalid}
      statusId={statusId}
      status={windfallInvalid()}
    >
      <Input
        id="plan-windfall"
        value={amount}
        inputMode="decimal"
        autoComplete="off"
        aria-invalid={invalid || undefined}
        aria-describedby={invalid ? `${helpId} ${statusId}` : helpId}
        className="h-11 min-h-11"
        onChange={(event) => {
          setAmount(event.target.value);
          setInvalid(false);
        }}
        onBlur={() => commit(amount, date)}
        onKeyDown={(event) => {
          if (event.key === "Enter") {
            event.preventDefault();
            commit(amount, date);
          }
        }}
      />
      <span className="text-sm font-medium text-foreground">Windfall date</span>
      <DateField
        title="Windfall date"
        value={date}
        min={asOf || undefined}
        max={forecastEnd ?? undefined}
        className="h-11 min-h-11"
        onChange={(next) => {
          setDate(next);
          setInvalid(false);
          commit(amount, next);
        }}
      />
    </FieldShell>
  );
}

type AmountFieldProps = {
  id: string;
  label: string;
  help: string;
  disabled?: boolean;
  applied: number | null;
  retry: boolean;
  onApply: (amount: number | null) => void;
};

/**
 * An optional money override.
 * Blank keeps the saved amount. Zero is explicit. The amount is sent when the field is left or Enter is pressed.
 */
function AmountField({
  id,
  label,
  help,
  disabled = false,
  applied,
  retry,
  onApply,
}: AmountFieldProps) {
  const [draft, setDraft] = useState(applied === null ? "" : String(applied));
  const [invalid, setInvalid] = useState(false);
  const helpId = `${id}-help`;
  const statusId = `${id}-status`;

  /**
   * Sends a blank, zero, or positive amount, or marks the field invalid.
   * The same applied amount is sent only when it retries a pending or failed request.
   */
  function commit(value: string) {
    const parsed = parseScenarioAmount(value);
    if (parsed.state === "invalid") {
      setInvalid(true);
      return;
    }

    const next = parsed.state === "empty" ? null : parsed.amount;
    setInvalid(false);
    setDraft(next === null ? "" : String(next));
    if (next !== applied || retry) {
      onApply(next);
    }
  }

  return (
    <FieldShell
      id={id}
      label={label}
      helpId={helpId}
      help={help}
      invalid={invalid}
      statusId={statusId}
      status={scenarioAmountInvalid()}
    >
      <Input
        id={id}
        value={draft}
        disabled={disabled}
        inputMode="decimal"
        autoComplete="off"
        aria-invalid={invalid || undefined}
        aria-describedby={invalid ? `${helpId} ${statusId}` : helpId}
        className="h-11 min-h-11"
        onChange={(event) => {
          setDraft(event.target.value);
          setInvalid(false);
        }}
        onBlur={() => commit(draft)}
        onKeyDown={(event) => {
          if (event.key === "Enter") {
            event.preventDefault();
            commit(draft);
          }
        }}
      />
    </FieldShell>
  );
}

type FieldShellProps = {
  id: string;
  label: string;
  helpId: string;
  help: string;
  invalid: boolean;
  statusId: string;
  status: string;
  children: ReactNode;
};

/**
 * A labeled preview field with its help line and invalid message.
 */
function FieldShell({
  id,
  label,
  helpId,
  help,
  invalid,
  statusId,
  status,
  children,
}: FieldShellProps) {
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-foreground">
        {label}
      </label>
      <div className="flex flex-col gap-2">{children}</div>
      <p id={helpId} className="text-sm text-muted-foreground">
        {help}
      </p>
      {invalid ? (
        <p
          id={statusId}
          aria-live="polite"
          className="text-sm text-destructive"
        >
          {status}
        </p>
      ) : null}
    </div>
  );
}
