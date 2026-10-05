"use client";

import { Button } from "@/components/ui/button";
import { Form } from "@/components/ui/form";
import { DateField } from "@/components/ui/date-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import type { AccountDto } from "@/lib/api/types";
import type { BillFormState } from "./billFormState";
import {
  isObligationCadence,
  isObligationFlexibility,
  obligationCadenceOptions,
  obligationFlexibilityOptions,
} from "./billOptions";

type BillFormProps = {
  form: BillFormState;
  accounts: AccountDto[];
  savedAccountName: string | null;
  planningCurrency: string;
  storedCurrency: string | null;
  isEditing: boolean;
  fromSuggestion: boolean;
  isSaving: boolean;
  onChange: (form: BillFormState) => void;
  onPickerOpenChange: (open: boolean) => void;
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
};

/**
 * Form for one bill.
 * The amount is one payment. Cadence and flexibility start unset on a new bill.
 * A suggestion fills the facts activity showed and still asks whether the bill is essential or flexible.
 * The source account can stay blank.
 */
export function BillForm({
  form,
  accounts,
  savedAccountName,
  planningCurrency,
  storedCurrency,
  isEditing,
  fromSuggestion,
  isSaving,
  onChange,
  onPickerOpenChange,
  onSubmit,
}: BillFormProps) {
  /**
   * Explains which currency the amount uses.
   * An edit keeps the currency from when the bill was created.
   */
  function currencyNote() {
    if (fromSuggestion) {
      return `This is a suggestion from activity. It becomes a bill when you add it. The amount uses ${planningCurrency}. It is one payment.`;
    }

    if (!isEditing) {
      return `The amount uses ${planningCurrency}. It is one payment.`;
    }

    if (storedCurrency && storedCurrency !== planningCurrency) {
      return `This amount stays in ${storedCurrency}. It is one payment.`;
    }

    return "This amount stays one payment.";
  }

  return (
    <Form className="flex flex-col gap-4" onSubmit={onSubmit}>
      <p className="text-sm text-muted-foreground">{currencyNote()}</p>
      <div className="flex flex-col gap-4">
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Name
          <Input
            value={form.name}
            onChange={(event) =>
              onChange({ ...form, name: event.target.value })
            }
            maxLength={80}
            autoComplete="off"
          />
        </label>

        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Amount
          <Input
            value={form.amount}
            onChange={(event) =>
              onChange({ ...form, amount: event.target.value })
            }
            inputMode="decimal"
            autoComplete="off"
          />
          <span className="font-normal text-muted-foreground">
            The amount of one payment.
          </span>
        </label>

        <div className="flex flex-col gap-1.5 text-sm font-medium">
          How often it is due
          <Select
            title="How often it is due"
            value={form.cadence}
            onChange={(value) => {
              if (isObligationCadence(value)) {
                onChange({ ...form, cadence: value });
              }
            }}
            options={obligationCadenceOptions.map((option) => ({
              value: option.value,
              label: option.label,
            }))}
            placeholder="Select"
            onOpenChange={onPickerOpenChange}
          />
          {form.cadence === "Biweekly" ? (
            <span className="font-normal text-muted-foreground">
              Every 14 days from the next due date. Some months include three
              payments.
            </span>
          ) : null}
          {form.cadence === "Semimonthly" ? (
            <span className="font-normal text-muted-foreground">
              Two days each month, about fifteen days apart, based on the next
              due date.
            </span>
          ) : null}
        </div>

        <div className="flex flex-col gap-1.5 text-sm font-medium">
          Next due date
          <DateField
            title="Next due date"
            value={form.nextDueDate}
            onChange={(nextDueDate) => onChange({ ...form, nextDueDate })}
            min="2000-01-01"
            max="2100-12-31"
            onOpenChange={onPickerOpenChange}
          />
        </div>

        <div className="flex flex-col gap-1.5 text-sm font-medium">
          Paid from
          <Select
            title="Paid from"
            value={form.accountId}
            onChange={(value) => onChange({ ...form, accountId: value })}
            options={accountChoices(accounts, form.accountId, savedAccountName)}
            onOpenChange={onPickerOpenChange}
          />
          <span className="font-normal text-muted-foreground">
            The account this payment leaves.
          </span>
        </div>

        <div className="flex flex-col gap-1.5 text-sm font-medium">
          Essential or flexible
          <Select
            title="Essential or flexible"
            value={form.flexibility}
            onChange={(value) => {
              if (isObligationFlexibility(value)) {
                onChange({ ...form, flexibility: value });
              }
            }}
            options={obligationFlexibilityOptions.map((option) => ({
              value: option.value,
              label: option.label,
            }))}
            placeholder="Select"
            onOpenChange={onPickerOpenChange}
          />
          <span className="font-normal text-muted-foreground">
            Essential has to be paid. Flexible can be reduced or skipped.
          </span>
        </div>
      </div>

      <Button type="submit" className="min-h-11" disabled={isSaving}>
        {isSaving ? "Saving…" : isEditing ? "Save changes" : "Add bill"}
      </Button>
    </Form>
  );
}

/**
 * Account choices for the bill form.
 * Open accounts are listed. A saved account that is no longer open stays selectable and is marked archived.
 */
function accountChoices(
  accounts: AccountDto[],
  accountId: string,
  savedAccountName: string | null,
) {
  const open = accounts
    .filter((account) => account.archivedAt === null)
    .map((account) => ({
      value: account.id,
      label: account.name,
    }));

  const choices = [{ value: "", label: "No account" }, ...open];
  if (accountId && !open.some((option) => option.value === accountId)) {
    choices.push({
      value: accountId,
      label: savedAccountName
        ? `${savedAccountName} (archived)`
        : "Archived account",
    });
  }

  return choices;
}
