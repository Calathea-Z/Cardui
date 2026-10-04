"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import {
  ACCOUNT_TYPE_OPTIONS,
  openingBalanceHelp,
} from "./manualAccount";

export type ManualAccountFormValues = {
  name: string;
  type: string;
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
    <form
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
          required
          maxLength={200}
          autoComplete="off"
        />
      </label>

      <div className="flex flex-col gap-1.5 text-sm font-medium">
        Type
        <Select
          title="Account type"
          value={values.type}
          onChange={(type) => update({ type })}
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
          required
          autoComplete="off"
        />
        <span className="text-xs font-normal text-muted-foreground">
          {openingBalanceHelp(values.type)}
        </span>
      </label>

      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Opening date
        <Input
          type="date"
          value={values.openingBalanceDate}
          onChange={(event) =>
            update({ openingBalanceDate: event.target.value })
          }
          required
        />
      </label>

      {errorMessage ? (
        <p className="text-sm text-destructive" role="alert">
          {errorMessage}
        </p>
      ) : null}

      <Button type="submit" size="lg" disabled={isSaving} className="py-3">
        {isSaving ? "Saving" : submitLabel}
      </Button>
    </form>
  );
}
