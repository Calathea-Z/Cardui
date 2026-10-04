"use client";

import { Select, type SelectOption } from "@/components/ui/select";

type ImportLabeledSelectProps = {
  label: string;
  title: string;
  value: string;
  onChange: (value: string) => void;
  onOpenChange: (open: boolean) => void;
  options: SelectOption[];
  placeholder?: string;
  hint?: string;
};

/**
 * Renders a labeled choice list on the import sheet.
 * An optional hint is shown under the list.
 */
export function ImportLabeledSelect({
  label,
  title,
  value,
  onChange,
  onOpenChange,
  options,
  placeholder,
  hint,
}: ImportLabeledSelectProps) {
  return (
    <div className="flex flex-col gap-1.5 text-sm font-medium">
      {label}
      <Select
        title={title}
        value={value}
        onChange={onChange}
        onOpenChange={onOpenChange}
        placeholder={placeholder}
        className="h-9"
        options={options}
      />
      {hint ? (
        <p className="text-xs font-normal text-muted-foreground">{hint}</p>
      ) : null}
    </div>
  );
}
