"use client";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Form } from "@/components/ui/form";
import { DateField } from "@/components/ui/date-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import type { ManualAccountType } from "@/lib/api/types";
import {
  ACCOUNT_TYPE_OPTIONS,
  isManualAccountType,
  openingBalanceHelp,
} from "./manualAccount";

/**
 * Editable fields for a manual account.
 * The opening balance stays text until the caller parses it into an amount.
 */
export type ManualAccountFormValues = {
  name: string;
  type: ManualAccountType;
  subtype: string;
  openingBalance: string;
  openingBalanceDate: string;
};

type ManualAccountFormProps = {
  values: ManualAccountFormValues;
  onChange: (values: ManualAccountFormValues) => void;
  onSubmit: () => void;
  submitLabel: string;
  isSaving: boolean;
  errorMessage: string | null;
  onPickerOpenChange?: (open: boolean) => void;
};

/**
 * Collects a manual account's name, type, subtype, opening balance, and opening date.
 * The balance hint follows the account type, and the submit button waits while a save is in progress.
 */
export function ManualAccountForm({
  values,
  onChange,
  onSubmit,
  submitLabel,
  isSaving,
  errorMessage,
  onPickerOpenChange,
}: ManualAccountFormProps) {
  function update(patch: Partial<ManualAccountFormValues>) {
    onChange({ ...values, ...patch });
  }

  return (
    <Form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit();
      }}
    >
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Name
        <Input
          value={values.name}
          onChange={(event) => update({ name: event.target.value })}
          maxLength={200}
          autoComplete="off"
        />
      </label>

      <div className="flex flex-col gap-1.5 text-sm font-medium">
        Type
        <Select
          title="Account type"
          value={values.type}
          onChange={(type) => {
            if (isManualAccountType(type)) {
              update({ type });
            }
          }}
          onOpenChange={onPickerOpenChange}
          className="h-9"
          options={ACCOUNT_TYPE_OPTIONS.map((option) => ({
            value: option.value,
            label: option.label,
          }))}
        />
      </div>

      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Subtype
        <Input
          value={values.subtype}
          onChange={(event) => update({ subtype: event.target.value })}
          maxLength={100}
          placeholder="Checking, savings, card"
          autoComplete="off"
        />
      </label>

      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Opening balance
        <Input
          value={values.openingBalance}
          onChange={(event) => update({ openingBalance: event.target.value })}
          inputMode="decimal"
          autoComplete="off"
        />
        <span className="text-xs font-normal text-muted-foreground">
          {openingBalanceHelp(values.type)}
        </span>
      </label>

      <div className="flex flex-col gap-1.5 text-sm font-medium">
        Opening date
        <DateField
          title="Opening date"
          value={values.openingBalanceDate}
          onChange={(openingBalanceDate) => update({ openingBalanceDate })}
          onOpenChange={onPickerOpenChange}
          className="h-9"
        />
      </div>

      {errorMessage ? (
        <Alert variant="destructive">{errorMessage}</Alert>
      ) : null}

      <Button type="submit" size="lg" disabled={isSaving} className="py-3">
        {isSaving ? "Saving" : submitLabel}
      </Button>
    </Form>
  );
}
