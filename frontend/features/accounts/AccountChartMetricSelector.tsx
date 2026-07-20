"use client";

import {
  ACCOUNT_CHART_METRICS,
  type AccountChartMetric,
} from "./accountChartMetric";
import { cn } from "@/lib/utils";

type AccountChartMetricSelectorProps = {
  value: AccountChartMetric;
  onChange: (metric: AccountChartMetric) => void;
  className?: string;
};

export function AccountChartMetricSelector({
  value,
  onChange,
  className,
}: AccountChartMetricSelectorProps) {
  return (
    <div
      className={cn("flex w-full gap-1.5", className)}
      role="group"
      aria-label="Account balance metric"
    >
      {ACCOUNT_CHART_METRICS.map((option) => (
        <button
          key={option.value}
          type="button"
          aria-pressed={value === option.value}
          onClick={() => onChange(option.value)}
          className={cn(
            "min-h-9 flex-1 rounded-md px-1.5 text-center text-[11px] font-medium transition sm:text-xs",
            value === option.value
              ? "bg-primary text-primary-foreground shadow-sm"
              : "bg-muted text-muted-foreground hover:text-foreground",
          )}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}
