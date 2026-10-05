"use client";

import { Button } from "@/components/ui/button";
import { DateField } from "@/components/ui/date-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import type { HouseholdContributorDto } from "@/lib/api/types";
import type { IncomeSourceFormState } from "./incomeFormState";
import {
  incomeCadenceOptions,
  incomeReliabilityOptions,
  isIncomeCadence,
  isIncomeReliability,
} from "./incomeSourceOptions";

type IncomeSourceFormProps = {
  form: IncomeSourceFormState;
  contributors: HouseholdContributorDto[];
  planningCurrency: string;
  storedCurrency: string | null;
  isEditing: boolean;
  isSaving: boolean;
  onChange: (form: IncomeSourceFormState) => void;
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
  onCancel: () => void;
};

/**
 * Form for one income source.
 * The amount is net pay for a single payment, after taxes and deductions.
 */
export function IncomeSourceForm({
  form,
  contributors,
  planningCurrency,
  storedCurrency,
  isEditing,
  isSaving,
  onChange,
  onSubmit,
  onCancel,
}: IncomeSourceFormProps) {
  /**
   * Explains which currency the amount uses.
   * An edit keeps the currency from when the source was created.
   */
  function currencyNote() {
    if (!isEditing) {
      return `New amounts use ${planningCurrency}. A biweekly paycheck stays on its own dates and is not turned into a monthly amount.`;
    }

    if (storedCurrency && storedCurrency !== planningCurrency) {
      return `This amount stays in ${storedCurrency}.`;
    }

    return "The amount stays one payment. It is not turned into a monthly average.";
  }

  return (
    <form className="app-panel flex flex-col gap-4 p-4" onSubmit={onSubmit}>
      <div>
        <h2 className="app-section-title">
          {isEditing ? "Edit income source" : "Add income source"}
        </h2>
        <p className="app-section-meta">{currencyNote()}</p>
      </div>

      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Name
        <Input
          value={form.name}
          onChange={(event) => onChange({ ...form, name: event.target.value })}
          maxLength={80}
          autoComplete="off"
          required
        />
      </label>

      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Net pay
        <Input
          value={form.takeHomeAmount}
          onChange={(event) =>
            onChange({ ...form, takeHomeAmount: event.target.value })
          }
          inputMode="decimal"
          autoComplete="off"
          required
        />
        <span className="font-normal text-muted-foreground">
          Enter net pay for one payment, after taxes and deductions.
        </span>
      </label>

      <div className="flex flex-col gap-1.5 text-sm font-medium">
        How often it is paid
        <Select
          title="How often it is paid"
          value={form.cadence}
          onChange={(value) => {
            if (isIncomeCadence(value)) {
              onChange({ ...form, cadence: value });
            }
          }}
          options={incomeCadenceOptions.map((option) => ({
            value: option.value,
            label: option.label,
          }))}
          placeholder="Select"
        />
      </div>

      <div className="flex flex-col gap-1.5 text-sm font-medium">
        Next payment date
        <DateField
          title="Next payment date"
          value={form.nextPaymentDate}
          onChange={(nextPaymentDate) => onChange({ ...form, nextPaymentDate })}
          min="2000-01-01"
          max="2100-12-31"
        />
      </div>

      <div className="flex flex-col gap-1.5 text-sm font-medium">
        Contributor
        <Select
          title="Contributor"
          value={form.contributorId}
          onChange={(value) => onChange({ ...form, contributorId: value })}
          options={[
            { value: "", label: "No contributor" },
            ...contributors.map((contributor) => ({
              value: contributor.id,
              label: contributor.isVisible
                ? contributor.name
                : `${contributor.name} (hidden)`,
            })),
          ]}
        />
      </div>

      <div className="flex flex-col gap-1.5 text-sm font-medium">
        Reliability
        <Select
          title="Reliability"
          value={form.reliability}
          onChange={(value) => {
            if (isIncomeReliability(value)) {
              onChange({ ...form, reliability: value });
            }
          }}
          options={incomeReliabilityOptions.map((option) => ({
            value: option.value,
            label: option.label,
          }))}
          placeholder="Select"
        />
        <span className="font-normal text-muted-foreground">
          Steady is expected in full. Variable can change. Uncertain may not
          arrive.
        </span>
      </div>

      <div className="flex gap-2">
        <Button type="submit" disabled={isSaving}>
          {isSaving ? "Saving…" : isEditing ? "Save changes" : "Add income"}
        </Button>
        {isEditing ? (
          <Button type="button" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
        ) : null}
      </div>
    </form>
  );
}
