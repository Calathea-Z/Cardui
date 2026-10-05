import type { IncomeCadence, IncomeReliability } from "@/lib/api/types";

/**
 * Cadence choices in the income form, in schedule order.
 * The value is the API member name. The label is the schedule in plain language.
 */
export const incomeCadenceOptions: Array<{
  value: IncomeCadence;
  label: string;
}> = [
  { value: "Weekly", label: "Every week" },
  { value: "Biweekly", label: "Every two weeks" },
  { value: "Semimonthly", label: "Twice a month" },
  { value: "Monthly", label: "Every month" },
  { value: "Quarterly", label: "Every quarter" },
  { value: "Yearly", label: "Every year" },
  { value: "Irregular", label: "No set schedule" },
];

/**
 * Reliability choices in the income form.
 * Steady is expected in full. Variable can change. Uncertain may not arrive.
 */
export const incomeReliabilityOptions: Array<{
  value: IncomeReliability;
  label: string;
}> = [
  { value: "Steady", label: "Steady" },
  { value: "Variable", label: "Variable" },
  { value: "Uncertain", label: "Uncertain" },
];

/**
 * Plain-language cadence for a stored member name.
 * An unrecognized name is shown as stored.
 */
export function incomeCadenceLabel(cadence: string) {
  return (
    incomeCadenceOptions.find((option) => option.value === cadence)?.label ??
    cadence
  );
}

/**
 * Short reliability label for a stored member name.
 * An unrecognized name is shown as stored.
 */
export function incomeReliabilityLabel(reliability: string) {
  return (
    incomeReliabilityOptions.find((option) => option.value === reliability)
      ?.label ?? reliability
  );
}

/**
 * True when the value is a cadence the form can save.
 */
export function isIncomeCadence(value: string): value is IncomeCadence {
  return incomeCadenceOptions.some((option) => option.value === value);
}

/**
 * True when the value is a reliability the form can save.
 */
export function isIncomeReliability(value: string): value is IncomeReliability {
  return incomeReliabilityOptions.some((option) => option.value === value);
}
