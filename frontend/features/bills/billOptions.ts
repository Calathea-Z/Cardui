import type { ObligationCadence, ObligationFlexibility } from "@/lib/api/types";

/**
 * Cadence choices in the bill form, in schedule order.
 * The value is the API member name. The label is the schedule in plain language.
 */
export const obligationCadenceOptions: Array<{
  value: ObligationCadence;
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
 * Essential and flexible choices in the bill form.
 * Essential has to be paid. Flexible can be reduced or skipped.
 */
export const obligationFlexibilityOptions: Array<{
  value: ObligationFlexibility;
  label: string;
}> = [
  { value: "Essential", label: "Essential" },
  { value: "Flexible", label: "Flexible" },
];

/**
 * Plain-language cadence for a stored member name.
 * An unrecognized name is shown as stored.
 */
export function obligationCadenceLabel(cadence: string) {
  return (
    obligationCadenceOptions.find((option) => option.value === cadence)
      ?.label ?? cadence
  );
}

/**
 * Essential or flexible label for a stored member name.
 * An unrecognized name is shown as stored.
 */
export function obligationFlexibilityLabel(flexibility: string) {
  return (
    obligationFlexibilityOptions.find((option) => option.value === flexibility)
      ?.label ?? flexibility
  );
}

/**
 * True when the value is a cadence the form can save.
 */
export function isObligationCadence(value: string): value is ObligationCadence {
  return obligationCadenceOptions.some((option) => option.value === value);
}

/**
 * True when the value is essential or flexible.
 */
export function isObligationFlexibility(
  value: string,
): value is ObligationFlexibility {
  return obligationFlexibilityOptions.some((option) => option.value === value);
}
