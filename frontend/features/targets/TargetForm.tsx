"use client";

import { Button } from "@/components/ui/button";
import { Form } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import type { CategoryTargetFormState } from "./categoryTargetForm";

type TargetFormProps = {
  categoryName: string;
  currency: string;
  monthSaved: boolean;
  hasTarget: boolean;
  form: CategoryTargetFormState;
  isSaving: boolean;
  error: string | null;
  copiesOthers: boolean;
  onChange: (form: CategoryTargetFormState) => void;
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
  onClear: () => void;
};

/**
 * Form for one category's monthly target.
 * Zero is a known target. Rollover carries leftover or overspend into the next month only when it is on.
 */
export function TargetForm({
  categoryName,
  currency,
  monthSaved,
  hasTarget,
  form,
  isSaving,
  error,
  copiesOthers,
  onChange,
  onSubmit,
  onClear,
}: TargetFormProps) {
  return (
    <Form className="flex flex-col gap-4" onSubmit={onSubmit}>
      <p className="text-sm text-muted-foreground">
        {categoryName} uses {currency}. A blank amount is not saved as zero.
        {!monthSaved
          ? copiesOthers
            ? " Saving starts this month. The other categories keep last month's targets."
            : " Saving starts this month."
          : ""}
      </p>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Target
        <Input
          inputMode="decimal"
          autoComplete="off"
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? "target-amount-error" : undefined}
          value={form.amount}
          onChange={(event) =>
            onChange({ ...form, amount: event.target.value })
          }
        />
      </label>
      {error ? (
        <p id="target-amount-error" className="text-sm text-destructive">
          {error}
        </p>
      ) : null}
      <Switch
        checked={form.rollover}
        onCheckedChange={(rollover) => onChange({ ...form, rollover })}
        label="Rollover"
        description="Leftover and overspend move into the next month only when this is on."
      />
      <Button type="submit" className="min-h-11" disabled={isSaving}>
        {isSaving ? "Saving…" : hasTarget ? "Save changes" : "Save target"}
      </Button>
      {hasTarget ? (
        <Button
          type="button"
          variant="destructive"
          className="min-h-11"
          disabled={isSaving}
          onClick={onClear}
        >
          Remove target
        </Button>
      ) : null}
    </Form>
  );
}
