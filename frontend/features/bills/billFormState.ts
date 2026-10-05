import { parseMoney } from "@/features/accounts/manualAccount";
import type {
  ObligationCadence,
  ObligationDto,
  ObligationFlexibility,
  UpsertObligationDto,
} from "@/lib/api/types";

/**
 * Fields the bill form edits.
 * Cadence and flexibility stay empty until the user chooses them.
 * `accountId` is empty when the bill is not paid from a household account.
 */
export type BillFormState = {
  name: string;
  amount: string;
  cadence: ObligationCadence | "";
  nextDueDate: string;
  accountId: string;
  flexibility: ObligationFlexibility | "";
};

/**
 * The save payload, or the reason the form is not ready.
 * `ok` is false when a required fact is missing or the amount is not one payment.
 */
export type BillUpsertResult =
  { ok: true; dto: UpsertObligationDto } | { ok: false; error: string };

/**
 * Blank form for a new bill.
 * Cadence and flexibility start unset so a save cannot silently pick Monthly or Essential.
 */
export function emptyBillForm(): BillFormState {
  return {
    name: "",
    amount: "",
    cadence: "",
    nextDueDate: "",
    accountId: "",
    flexibility: "",
  };
}

/**
 * Copies a stored bill into the form.
 * The due date keeps the calendar day and drops any time suffix.
 * A missing account becomes a blank choice.
 */
export function obligationToForm(obligation: ObligationDto): BillFormState {
  return {
    name: obligation.name,
    amount: String(obligation.amount),
    cadence: obligation.cadence,
    nextDueDate: obligation.nextDueDate.slice(0, 10),
    accountId: obligation.accountId ?? "",
    flexibility: obligation.flexibility,
  };
}

/**
 * Builds the save payload from the form.
 * The amount is one payment. A blank account is omitted.
 */
export function toObligationUpsert(form: BillFormState): BillUpsertResult {
  const name = form.name.trim();
  if (!name) {
    return { ok: false, error: "A bill name is required." };
  }

  if (name.length > 80) {
    return { ok: false, error: "A bill name must be 80 characters or fewer." };
  }

  const amount = parseMoney(form.amount);
  if (amount === null || amount <= 0) {
    return { ok: false, error: "Enter the amount for one payment." };
  }

  if (amount > 100_000_000) {
    return { ok: false, error: "That amount is too large." };
  }

  if (!form.cadence) {
    return { ok: false, error: "Choose how often this bill is due." };
  }

  if (!form.nextDueDate) {
    return { ok: false, error: "Enter the next due date." };
  }

  if (!form.flexibility) {
    return {
      ok: false,
      error: "Choose whether this bill is essential or flexible.",
    };
  }

  return {
    ok: true,
    dto: {
      name,
      amount,
      cadence: form.cadence,
      nextDueDate: form.nextDueDate,
      accountId: form.accountId || null,
      flexibility: form.flexibility,
    },
  };
}
