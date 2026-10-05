import type {
  IncomeCadence,
  IncomeReliability,
  IncomeSourceDto,
} from "@/lib/api/types";

/**
 * Fields the income form edits.
 * Cadence and reliability stay empty until the user chooses them.
 * `contributorId` is empty when the source is not assigned to a person.
 */
export type IncomeSourceFormState = {
  name: string;
  takeHomeAmount: string;
  cadence: IncomeCadence | "";
  nextPaymentDate: string;
  contributorId: string;
  reliability: IncomeReliability | "";
};

/**
 * Blank form for a new income source.
 * Cadence and reliability start unset so a save cannot silently pick Weekly or Steady.
 */
export function emptyIncomeSourceForm(): IncomeSourceFormState {
  return {
    name: "",
    takeHomeAmount: "",
    cadence: "",
    nextPaymentDate: "",
    contributorId: "",
    reliability: "",
  };
}

/**
 * Copies a stored source into the form.
 * The payment date keeps the calendar day and drops any time suffix.
 */
export function incomeSourceToForm(
  source: IncomeSourceDto,
): IncomeSourceFormState {
  return {
    name: source.name,
    takeHomeAmount: String(source.takeHomeAmount),
    cadence: source.cadence,
    nextPaymentDate: source.nextPaymentDate.slice(0, 10),
    contributorId: source.contributorId ?? "",
    reliability: source.reliability,
  };
}
