import { EmptyState } from "@/components/ui/empty-state";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type { DashboardSummaryDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import {
  calculateCategoryPercentage,
  formatDashboardPeriod,
} from "./dashboardMonthlyActivity";

type DashboardMonthlyActivityWidgetProps = {
  summary: DashboardSummaryDto;
};

type MonthlyMetricProps = {
  label: string;
  value: number;
  currency: string;
  tone?: "default" | "positive" | "negative";
};

/**
 * Shows income, spending, and their difference for the dashboard period.
 * Category amounts use formatCurrency, and each bar is that category's share of spending.
 */
export function DashboardMonthlyActivityWidget({
  summary,
}: DashboardMonthlyActivityWidgetProps) {
  const difference = summary.monthlyIncome - summary.monthlySpending;
  const periodLabel = formatDashboardPeriod(
    summary.periodStart,
    summary.periodEnd,
  );

  return (
    <section className="app-panel" aria-labelledby="monthly-activity-title">
      <div className="app-panel-header p-4">
        <h2 id="monthly-activity-title" className="app-section-title">
          Monthly Activity
        </h2>
        <p className="app-section-meta">{periodLabel}</p>
      </div>

      <div className="grid grid-cols-1 divide-y divide-border/70 sm:grid-cols-3 sm:divide-x sm:divide-y-0">
        <MonthlyMetric
          currency={summary.planningCurrency}
          label="Income"
          value={summary.monthlyIncome}
          tone="positive"
        />
        <MonthlyMetric
          currency={summary.planningCurrency}
          label="Spending"
          value={summary.monthlySpending}
          tone="negative"
        />
        <MonthlyMetric
          currency={summary.planningCurrency}
          label="Difference"
          value={difference}
          tone={difference >= 0 ? "positive" : "negative"}
        />
      </div>

      <div className="border-t border-border/70">
        <div className="px-4 pt-4">
          <h3 className="text-xs font-semibold tracking-wide text-foreground uppercase">
            Spending by category
          </h3>
        </div>

        {summary.spendingByCategory.length === 0 ? (
          <EmptyState
            title="No spending recorded"
            description={`Category spending for ${periodLabel.toLowerCase()} will appear here.`}
            className="py-8 [&_p]:text-sm [&_p]:font-normal"
          />
        ) : (
          <ul className="divide-y divide-border/70 px-4 py-2">
            {summary.spendingByCategory.map((category) => {
              const percentage = calculateCategoryPercentage(
                category.amount,
                summary.monthlySpending,
              );

              return (
                <li
                  key={category.categoryId ?? "uncategorized"}
                  className="py-3"
                >
                  <div className="flex items-baseline justify-between gap-4">
                    <span className="truncate text-sm text-foreground">
                      {category.categoryName}
                    </span>
                    <span className="font-mono text-sm font-medium tabular-nums">
                      {formatCurrency(
                        category.amount,
                        summary.planningCurrency,
                      )}
                    </span>
                  </div>
                  <div
                    className="mt-2 h-1.5 overflow-hidden rounded-full bg-muted"
                    role="meter"
                    aria-label={`${category.categoryName}: ${percentage.toFixed(1)}% of spending`}
                    aria-valuemin={0}
                    aria-valuemax={100}
                    aria-valuenow={Number(percentage.toFixed(1))}
                  >
                    <div
                      className="h-full rounded-full bg-primary"
                      style={{
                        width: `${percentage}%`,
                        backgroundColor: category.color ?? undefined,
                      }}
                    />
                  </div>
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </section>
  );
}

/**
 * Shows one income, spending, or difference amount.
 * The figure uses formatCurrency, with the success color for a positive tone and the destructive color for a negative tone.
 */
function MonthlyMetric({
  label,
  value,
  currency,
  tone = "default",
}: MonthlyMetricProps) {
  return (
    <div className="flex min-h-24 flex-col justify-center px-4 py-3">
      <span className="text-xs text-muted-foreground">{label}</span>
      <span
        className={cn(
          "mt-1 font-mono text-xl font-semibold tracking-tight tabular-nums",
          tone === "positive" && "text-success",
          tone === "negative" && "text-destructive",
        )}
      >
        {formatCurrency(value, currency)}
      </span>
    </div>
  );
}
