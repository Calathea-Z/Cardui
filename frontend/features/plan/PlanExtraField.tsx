"use client";

import { useState } from "react";
import { Input } from "@/components/ui/input";
import { planExtraHelp, planExtraInvalid, planExtraStatus } from "./planCopy";
import { moneyCommaError } from "@/features/accounts/formatCurrency";
import { parsePlanExtra } from "./planExtra";
import type { PlanExtraStatus } from "./usePlanExtra";

type PlanExtraFieldProps = {
  appliedAmount: number;
  status: PlanExtraStatus;
  onApply: (amount: number) => void;
};

/**
 * The extra-each-month field.
 * Blank or zero is the minimums-only plan. A positive amount is applied when the field is left or Enter is pressed.
 * An invalid amount stays in the field and is not sent. The previous plan stays on screen while a request runs or fails.
 */
export function PlanExtraField({
  appliedAmount,
  status,
  onApply,
}: PlanExtraFieldProps) {
  const [draft, setDraft] = useState("");
  const [invalid, setInvalid] = useState(false);
  const helpId = "plan-extra-help";
  const statusId = "plan-extra-status";
  const describedBy = invalid ? `${helpId} ${statusId}` : helpId;
  const statusMessage = invalid
    ? (moneyCommaError(draft) ?? planExtraInvalid())
    : status === "ready"
      ? null
      : planExtraStatus(status);

  /**
   * Applies a parsed amount, or marks the field invalid.
   * The same amount already on screen is not requested again.
   */
  function commit(value: string) {
    const parsed = parsePlanExtra(value);
    if (parsed === null) {
      setInvalid(true);
      return;
    }

    setInvalid(false);
    if (parsed !== appliedAmount) {
      onApply(parsed);
    }
  }

  return (
    <div className="flex flex-col gap-1.5">
      <label
        htmlFor="plan-extra"
        className="flex max-w-xs flex-col gap-1.5 text-sm font-medium text-foreground"
      >
        Extra each month
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
          className={
            invalid || status === "error"
              ? "text-sm text-destructive"
              : "text-sm text-muted-foreground"
          }
        >
          {statusMessage}
        </p>
      ) : null}
    </div>
  );
}
