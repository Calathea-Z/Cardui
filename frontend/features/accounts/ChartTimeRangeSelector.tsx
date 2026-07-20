"use client";

import { CHART_TIME_RANGES, type ChartTimeRange } from "./chartTimeRange";
import { cn } from "@/lib/utils";

type ChartTimeRangeSelectorProps = {
  value: ChartTimeRange;
  onChange: (range: ChartTimeRange) => void;
  compact?: boolean;
  className?: string;
};

export function ChartTimeRangeSelector({
  value,
  onChange,
  compact = false,
  className,
}: ChartTimeRangeSelectorProps) {
  return (
    <div
      className={cn(
        "scrollbar-none flex gap-1.5 overflow-x-auto",
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
            "shrink-0 rounded-md font-medium transition",
            compact
              ? "min-h-8 px-2.5 text-[11px]"
              : "min-h-9 px-3 text-xs",
            value === option.value
              ? "bg-primary text-primary-foreground shadow-sm"
              : "bg-transparent text-muted-foreground hover:bg-muted/70 hover:text-foreground",
          )}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}
