"use client";

import { CHART_TIME_RANGES, type ChartTimeRange } from "./chartTimeRange";
import { cn } from "@/lib/utils";

type ChartTimeRangeSelectorProps = {
  value: ChartTimeRange;
  onChange: (range: ChartTimeRange) => void;
  compact?: boolean;
  className?: string;
};

/**
 * Lets the household pick the balance chart's time window.
 * The pressed label is the range the chart filters to, in the same green as a favorable change.
 */
export function ChartTimeRangeSelector({
  value,
  onChange,
  compact = false,
  className,
}: ChartTimeRangeSelectorProps) {
  return (
    <div
      className={cn("flex w-full", compact ? "gap-1" : "gap-1.5", className)}
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
            value === option.value
              ? "text-success"
              : "text-muted-foreground hover:text-foreground",
          )}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}
