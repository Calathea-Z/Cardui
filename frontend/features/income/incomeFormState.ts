import { parseMoney } from "@/features/accounts/manualAccount";
import type {
  IncomeCadence,
  IncomeReliability,
  IncomeSourceDto,
  UpsertIncomeRaiseDto,
  UpsertIncomeSourceDto,
} from "@/lib/api/types";

/**
 * One expected raise while it is being edited.
 * `key` identifies the row on the form and is not saved.
 * The date and amount stay blank until the user fills them. A fully blank row is not saved.
 */
export type IncomeRaiseFormState = {
  key: string;
  effectiveDate: string;
  takeHomeAmount: string;
};

/**
 * Fields the income form edits.
 * Cadence and reliability stay empty until the user chooses them.
 * `contributorId` is empty when the source is not assigned to a person.
 * Low and strong stay blank when that scenario is not recorded.
 */
export type IncomeSourceFormState = {
  name: string;
  takeHomeAmount: string;
  lowTakeHomeAmount: string;
  strongTakeHomeAmount: string;
  cadence: IncomeCadence | "";
  nextPaymentDate: string;
  contributorId: string;
  reliability: IncomeReliability | "";
  raises: IncomeRaiseFormState[];
};

/**
 * The save payload, or the reason the form is not ready.
 * `ok` is false when a required fact is missing or a scenario or raise breaks a payment rule.
 */
export type IncomeSourceUpsertResult =
  { ok: true; dto: UpsertIncomeSourceDto } | { ok: false; error: string };

/**
 * Blank form for a new income source.
 * Cadence and reliability start unset so a save cannot silently pick Weekly or Steady.
 */
export function emptyIncomeSourceForm(): IncomeSourceFormState {
  return {
    name: "",
    takeHomeAmount: "",
    lowTakeHomeAmount: "",
    strongTakeHomeAmount: "",
    cadence: "",
    nextPaymentDate: "",
    contributorId: "",
    reliability: "",
    raises: [],
  };
}

/**
 * A raise row with no date and no amount.
 * The form ignores it on save.
 */
export function emptyIncomeRaiseForm(): IncomeRaiseFormState {
  return {
    key: crypto.randomUUID(),
    effectiveDate: "",
    takeHomeAmount: "",
  };
}

/**
 * Copies a stored source into the form.
 * The payment date keeps the calendar day and drops any time suffix.
 * A missing low or strong amount becomes a blank field.
 */
export function incomeSourceToForm(
  source: IncomeSourceDto,
): IncomeSourceFormState {
  return {
    name: source.name,
    takeHomeAmount: String(source.takeHomeAmount),
    lowTakeHomeAmount:
      source.lowTakeHomeAmount === null ? "" : String(source.lowTakeHomeAmount),
    strongTakeHomeAmount:
      source.strongTakeHomeAmount === null
        ? ""
        : String(source.strongTakeHomeAmount),
    cadence: source.cadence,
    nextPaymentDate: source.nextPaymentDate.slice(0, 10),
    contributorId: source.contributorId ?? "",
    reliability: source.reliability,
    raises: source.raises.map((raise) => ({
      key: raise.id,
      effectiveDate: raise.effectiveDate.slice(0, 10),
      takeHomeAmount: String(raise.takeHomeAmount),
    })),
  };
}

/**
 * Builds the save payload from the form.
 * Typical pay is required. Low and strong are omitted when blank.
 * A fully blank raise row is dropped. A partial row, a date before the next payment, or a repeated date is rejected.
 */
export function toIncomeSourceUpsert(
  form: IncomeSourceFormState,
): IncomeSourceUpsertResult {
  const takeHomeAmount = parseMoney(form.takeHomeAmount);
  if (
    !form.name.trim() ||
    takeHomeAmount === null ||
    takeHomeAmount <= 0 ||
    !form.cadence ||
    !form.nextPaymentDate ||
    !form.reliability
  ) {
    return {
      ok: false,
      error:
        "Enter a name, the typical net pay for one payment, how often it is paid, the next date, and how reliable it is.",
    };
  }

  const low = readOptionalPayment(form.lowTakeHomeAmount, "low net pay");
  if (!low.ok) {
    return low;
  }

  const strong = readOptionalPayment(
    form.strongTakeHomeAmount,
    "strong net pay",
  );
  if (!strong.ok) {
    return strong;
  }

  if (low.amount !== null && low.amount > takeHomeAmount) {
    return {
      ok: false,
      error: "Low net pay cannot be higher than the typical amount.",
    };
  }

  if (strong.amount !== null && strong.amount < takeHomeAmount) {
    return {
      ok: false,
      error: "Strong net pay cannot be lower than the typical amount.",
    };
  }

  const raises = readRaises(form.raises, form.nextPaymentDate);
  if (!raises.ok) {
    return raises;
  }

  return {
    ok: true,
    dto: {
      name: form.name.trim(),
      takeHomeAmount,
      lowTakeHomeAmount: low.amount,
      strongTakeHomeAmount: strong.amount,
      cadence: form.cadence,
      nextPaymentDate: form.nextPaymentDate,
      contributorId: form.contributorId || null,
      reliability: form.reliability,
      raises: raises.raises,
    },
  };
}

/**
 * Reads an optional scenario amount.
 * Blank means that scenario is not recorded. Zero, negative, or unreadable text is rejected.
 */
function readOptionalPayment(
  value: string,
  label: string,
): { ok: true; amount: number | null } | { ok: false; error: string } {
  if (!value.trim()) {
    return { ok: true, amount: null };
  }

  const amount = parseMoney(value);
  if (amount === null || amount <= 0) {
    return { ok: false, error: `Enter the ${label} for one payment.` };
  }

  return { ok: true, amount };
}

/**
 * Reads the raise rows that should be saved.
 * A row with neither a date nor an amount is ignored. Dates are sent in calendar order.
 */
function readRaises(
  rows: IncomeRaiseFormState[],
  nextPaymentDate: string,
): { ok: true; raises: UpsertIncomeRaiseDto[] } | { ok: false; error: string } {
  const raises: UpsertIncomeRaiseDto[] = [];
  const seen = new Set<string>();

  for (const row of rows) {
    const date = row.effectiveDate.trim();
    const amountText = row.takeHomeAmount.trim();
    if (!date && !amountText) {
      continue;
    }

    const amount = parseMoney(amountText);
    if (!date || amount === null || amount <= 0) {
      return {
        ok: false,
        error: "Enter the raise date and the new typical net pay.",
      };
    }

    if (date < nextPaymentDate) {
      return {
        ok: false,
        error: "Enter a raise date on or after the next payment.",
      };
    }

    if (seen.has(date)) {
      return { ok: false, error: "Each expected raise needs its own date." };
    }

    seen.add(date);
    raises.push({ effectiveDate: date, takeHomeAmount: amount });
  }

  raises.sort((left, right) =>
    left.effectiveDate.localeCompare(right.effectiveDate),
  );
  return { ok: true, raises };
}
