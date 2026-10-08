"use client";

import { useState } from "react";
import { Input } from "@/components/ui/input";
import { planExtraHelp, planExtraInvalid } from "./planCopy";
import { moneyCommaError } from "@/features/accounts/formatCurrency";
import { parsePlanExtra } from "./planExtra";

type PlanExtraFieldProps = {
  appliedAmount: number;
  retryCurrentAmount: boolean;
  onApply: (amount: number) => void;
};

/**
 * The extra-each-month field.
 * Zero is shown explicitly for the minimums-only plan. A positive amount is applied when the field is left or Enter is pressed.
 * An invalid amount stays in the field and is not sent.
 */
export function PlanExtraField({
  appliedAmount,
  retryCurrentAmount,
  onApply,
}: PlanExtraFieldProps) {
  const [draft, setDraft] = useState(String(appliedAmount));
  const [invalid, setInvalid] = useState(false);
  const helpId = "plan-extra-help";
  const statusId = "plan-extra-status";
  const describedBy = invalid ? `${helpId} ${statusId}` : helpId;
  const statusMessage = invalid
    ? (moneyCommaError(draft) ?? planExtraInvalid())
    : null;

  /**
   * Applies a parsed amount, or marks the field invalid.
   * The same applied amount is sent only when it clears or retries a pending/failed request.
   */
  function commit(value: string) {
    const parsed = parsePlanExtra(value);
    if (parsed === null) {
      setInvalid(true);
      return;
    }

    setInvalid(false);
    setDraft(String(parsed));
    if (parsed !== appliedAmount || retryCurrentAmount) {
      onApply(parsed);
    }
  }

  return (
    <div className="flex flex-col gap-1.5">
      <label
        htmlFor="plan-extra"
        className="flex flex-col gap-1.5 text-sm font-medium text-foreground"
      >
        Extra monthly payment
        <Input
          id="plan-extra"
          value={draft}
          inputMode="decimal"
          autoComplete="off"
          aria-invalid={invalid || undefined}
          aria-describedby={describedBy}
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
      </label>
      <p id={helpId} className="text-sm text-muted-foreground">
        {planExtraHelp()}
      </p>
      {statusMessage ? (
        <p
          id={statusId}
          aria-live="polite"
          className="text-sm text-destructive"
        >
          {statusMessage}
        </p>
      ) : null}
    </div>
  );
}
