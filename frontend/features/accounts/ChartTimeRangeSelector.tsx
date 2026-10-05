"use client";

import { CHART_TIME_RANGES, type ChartTimeRange } from "./chartTimeRange";
import { cn } from "@/lib/utils";

type ChartTimeRangeSelectorProps = {
  value: ChartTimeRange;
  onChange: (range: ChartTimeRange) => void;
  compact?: boolean;
  variant?: "pills" | "text";
  className?: string;
};

/**
 * Lets the household pick the balance chart's time window.
 * The pressed button is the range the chart filters to. The text variant
 * marks the active range in the primary color without a filled pill.
 */
export function ChartTimeRangeSelector({
  value,
  onChange,
  compact = false,
  variant = "pills",
  className,
}: ChartTimeRangeSelectorProps) {
  return (
    <div
      className={cn(
        "flex w-full gap-1",
        compact ? "gap-1" : "gap-1.5",
        className,
      )}
      role="group"
      aria-label="Chart time range"
    >
      {CHART_TIME_RANGES.map((option) => (
        <button
          key={option.value}
          type="button"
          aria-pressed={value === option.value}
          onClick={() => onChange(option.value)}
          className={cn(
            "flex-1 font-medium transition",
            compact
              ? "min-h-8 px-1 text-center text-[11px]"
              : "min-h-9 px-2 text-center text-xs",
            variant === "text"
              ? value === option.value
                ? "text-primary"
                : "text-muted-foreground hover:text-foreground"
              : cn(
                  "rounded-md",
                  value === option.value
                    ? "bg-primary text-primary-foreground"
                    : "bg-transparent text-muted-foreground hover:bg-muted/70 hover:text-foreground",
                ),
          )}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}
