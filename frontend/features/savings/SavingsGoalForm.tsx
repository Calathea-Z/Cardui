"use client";

import { Button } from "@/components/ui/button";
import { DateField } from "@/components/ui/date-field";
import { Form } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type { SavingsAccountDto } from "@/lib/api/types";
import { readyByLabel } from "./savingsCopy";
import type { SavingsFormState } from "./savingsFormState";

type SavingsGoalFormProps = {
  form: SavingsFormState;
  accounts: SavingsAccountDto[];
  savedAccountName: string | null;
  planningCurrency: string;
  isEditing: boolean;
  isSaving: boolean;
  onChange: (form: SavingsFormState) => void;
  onPickerOpenChange: (open: boolean) => void;
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
};

/**
 * Form for one savings goal.
 * Everyday spending asks for a monthly amount and a day. Cash to keep asks for a floor. A finishing goal asks for a target and a date.
 * Choosing an account does not move money. Use account balance clears a typed amount.
 */
export function SavingsGoalForm({
  form,
  accounts,
  savedAccountName,
  planningCurrency,
  isEditing,
  isSaving,
  onChange,
  onPickerOpenChange,
  onSubmit,
}: SavingsGoalFormProps) {
  const selected = accounts.find((account) => account.id === form.accountId);
  const balanceLabel =
    selected && form.reservedAmount !== String(selected.balance)
      ? formatCurrency(selected.balance, selected.currency ?? planningCurrency)
      : null;

  /**
   * Fills the amount with the selected account's balance.
   * A negative balance is stored as zero. The person can still type a different amount after.
   */
  function useAccountBalance() {
    if (!selected) {
      return;
    }

    const amount = selected.balance > 0 ? selected.balance : 0;
    onChange({
      ...form,
      reservedAmount: String(amount),
      useAccountBalance: true,
    });
  }

  return (
    <Form className="flex flex-col gap-4" onSubmit={onSubmit}>
      <p className="text-sm text-muted-foreground">
        The amount uses {planningCurrency}. Saving this does not move money or
        create a transaction.
      </p>
      {form.kind === "Sinking" ? (
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Name
          <Input
            value={form.name}
            onChange={(event) =>
              onChange({ ...form, name: event.target.value })
            }
            autoComplete="off"
          />
        </label>
      ) : null}
      {form.kind === "Operating" ? (
        <>
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Monthly amount
            <Input
              inputMode="decimal"
              value={form.monthlyAmount}
              onChange={(event) =>
                onChange({ ...form, monthlyAmount: event.target.value })
              }
              autoComplete="off"
            />
            <span className="font-normal text-muted-foreground">
              How much you plan to spend on everyday needs each month.
            </span>
          </label>
          <div className="flex flex-col gap-1.5 text-sm font-medium">
            Ready by
            <Select
              title="Ready by"
              value={form.readyDay}
              onChange={(readyDay) => onChange({ ...form, readyDay })}
              options={readyByOptions()}
              onOpenChange={onPickerOpenChange}
            />
            <span className="font-normal text-muted-foreground">
              The day this spending counts. A short month uses its last day.
            </span>
          </div>
        </>
      ) : null}
      {form.kind === "Floor" ? (
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Amount to keep
          <Input
            inputMode="decimal"
            value={form.floorAmount}
            onChange={(event) =>
              onChange({ ...form, floorAmount: event.target.value })
            }
            autoComplete="off"
          />
          <span className="font-normal text-muted-foreground">
            How much you always want available. This does not leave cash.
          </span>
        </label>
      ) : null}
      {form.kind === "Emergency" || form.kind === "Sinking" ? (
        <>
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Target
            <Input
              inputMode="decimal"
              value={form.targetAmount}
              onChange={(event) =>
                onChange({ ...form, targetAmount: event.target.value })
              }
              autoComplete="off"
            />
            <span className="font-normal text-muted-foreground">
              The amount to have set aside.
            </span>
          </label>
          <div className="flex flex-col gap-1.5 text-sm font-medium">
            Target date
            <DateField
              title="Target date"
              value={form.targetDate}
              onChange={(targetDate) => onChange({ ...form, targetDate })}
              min="2000-01-01"
              max="2100-12-31"
              onOpenChange={onPickerOpenChange}
            />
          </div>
        </>
      ) : null}
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        {form.kind === "Operating" || form.kind === "Floor"
          ? "Available now"
          : "Already set aside"}
        <Input
          inputMode="decimal"
          value={form.reservedAmount}
          onChange={(event) =>
            onChange({
              ...form,
              reservedAmount: event.target.value,
              useAccountBalance: false,
            })
          }
          autoComplete="off"
        />
        <span className="font-normal text-muted-foreground">
          {form.kind === "Operating"
            ? "What is left of this month. Blank means zero."
            : form.kind === "Floor"
              ? "How much of that amount you already have. Blank means zero."
              : "Blank means zero. This is not a second expense."}
        </span>
      </label>
      <div className="flex flex-col gap-1.5 text-sm font-medium">
        Account
        <Select
          title="Account"
          value={form.accountId}
          onChange={(accountId) =>
            onChange({
              ...form,
              accountId,
              useAccountBalance: false,
            })
          }
          options={accountChoices(
            accounts,
            form.accountId,
            savedAccountName,
            planningCurrency,
          )}
          onOpenChange={onPickerOpenChange}
        />
        <span className="font-normal text-muted-foreground">
          {form.kind === "Operating" || form.kind === "Floor"
            ? "Optional. Available now follows this balance until you type your own."
            : "Optional. The plan uses this cash account's balance until you type your own."}
        </span>
      </div>
      {balanceLabel ? (
        <Button
          type="button"
          variant="outline"
          className="min-h-11"
          onClick={useAccountBalance}
        >
          Use account balance ({balanceLabel})
        </Button>
      ) : null}
      <Button type="submit" className="min-h-11" disabled={isSaving}>
        {isSaving
          ? "Saving…"
          : form.kind === "Operating" || form.kind === "Floor"
            ? "Save"
            : isEditing
              ? "Save changes"
              : "Save goal"}
      </Button>
    </Form>
  );
}

/**
 * The 31 days Ready by can use.
 * The label names the day of each month.
 */
function readyByOptions() {
  return Array.from({ length: 31 }, (_, index) => {
    const day = String(index + 1);
    return { value: day, label: readyByLabel(index + 1) };
  });
}

/**
 * Cash accounts this goal may follow, plus a saved account that is no longer eligible.
 * An account followed by another goal is left out.
 */
function accountChoices(
  accounts: SavingsAccountDto[],
  accountId: string,
  savedAccountName: string | null,
  planningCurrency: string,
) {
  const open = accounts
    .filter(
      (account) =>
        account.followedByGoalId === null || account.id === accountId,
    )
    .map((account) => ({
      value: account.id,
      label: accountLabel(account, planningCurrency),
    }));
  const choices = [{ value: "", label: "No account" }, ...open];
  if (accountId && !open.some((option) => option.value === accountId)) {
    choices.push({
      value: accountId,
      label: savedAccountName
        ? `${savedAccountName} (unavailable)`
        : "Unavailable account",
    });
  }

  return choices;
}

/**
 * The account name, its mask, and its balance, so the list says which cash would be followed.
 */
function accountLabel(account: SavingsAccountDto, planningCurrency: string) {
  const mask = account.mask ? ` · ${account.mask}` : "";
  const balance = formatCurrency(
    account.balance,
    account.currency ?? planningCurrency,
  );
  return `${account.name}${mask} · ${balance}`;
}
