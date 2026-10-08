import type { SavingsGoalDto, SavingsGoalKind } from "@/lib/api/types";
import {
  toSavingsPayload,
  type SavingsFormResult,
  type SavingsFormState,
} from "./savingsCopy";

export type { SavingsFormState };

/**
 * A blank goal of one kind.
 * The reserved amount starts blank, which is stored as zero.
 */
export function emptySavingsForm(kind: SavingsGoalKind): SavingsFormState {
  return {
    kind,
    name: "",
    targetAmount: "",
    targetDate: "",
    monthlyAmount: "",
    readyDay: "1",
    floorAmount: "",
    reservedAmount: "",
    accountId: "",
    useAccountBalance: false,
  };
}

/**
 * The form for a saved goal.
 * The reserved field shows the amount the plan uses. Following without an override keeps using the balance.
 */
export function goalToForm(goal: SavingsGoalDto): SavingsFormState {
  return {
    kind: goal.kind,
    name: goal.kind === "Sinking" ? goal.name : "",
    targetAmount: goal.targetAmount === null ? "" : String(goal.targetAmount),
    targetDate: goal.targetDate?.slice(0, 10) ?? "",
    monthlyAmount: goal.monthlyAmount === null ? "" : String(goal.monthlyAmount),
    readyDay: goal.readyDay === null ? "1" : String(goal.readyDay),
    floorAmount: goal.floorAmount === null ? "" : String(goal.floorAmount),
    reservedAmount: String(goal.amountInUse),
    accountId: goal.accountId ?? "",
    useAccountBalance: goal.following && !goal.reservedOverridden,
  };
}

/**
 * The API body for this form.
 * An account is offered when it is in the list the person can still choose.
 */
export function toSavingsUpsert(
  form: SavingsFormState,
  accountOffered: boolean,
): SavingsFormResult {
  return toSavingsPayload(form, accountOffered);
}
