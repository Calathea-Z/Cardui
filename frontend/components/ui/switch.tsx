"use client";

import { useId } from "react";
import { cn } from "@/lib/utils";

type SwitchProps = {
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  label: string;
  description?: string;
  disabled?: boolean;
};

/**
 * A two-state switch.
 * The label is the accessible name. On and Off sit beside the track so the state is not color alone.
 */
export function Switch({
  checked,
  onCheckedChange,
  label,
  description,
  disabled = false,
}: SwitchProps) {
  const descriptionId = useId();

  return (
    <div className="flex flex-col gap-1.5">
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        aria-describedby={description ? descriptionId : undefined}
        disabled={disabled}
        onClick={() => onCheckedChange(!checked)}
        className={cn(
          "flex min-h-11 w-full items-center justify-between gap-3 rounded-lg border border-border bg-card px-3 text-left",
          "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none",
          "disabled:cursor-not-allowed disabled:opacity-50",
        )}
      >
        <span className="text-sm font-medium text-foreground">{label}</span>
        <span className="flex items-center gap-2">
          <span className="text-sm text-foreground">
            {checked ? "On" : "Off"}
          </span>
          <span
            aria-hidden
            className={cn(
              "relative h-7 w-12 shrink-0 rounded-full border transition-colors motion-reduce:transition-none",
              checked ? "border-primary bg-primary" : "border-border bg-muted",
            )}
          >
            <span
              className={cn(
                "absolute top-0.5 left-0 size-5 rounded-full border border-border bg-white transition-transform motion-reduce:transition-none",
                checked ? "translate-x-6" : "translate-x-0.5",
              )}
            />
          </span>
        </span>
      </button>
      {description ? (
        <p id={descriptionId} className="text-sm text-muted-foreground">
          {description}
        </p>
      ) : null}
    </div>
  );
}
