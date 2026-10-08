import { formatCurrency } from "@/features/accounts/formatCurrency";
import type { CashFlowRecoveryPathDto } from "@/lib/api/types";
import { minimumLeavesOn } from "./planRecoveryCopy";

type PlanRecoveryPathProps = {
  title: string;
  description: string;
  path: CashFlowRecoveryPathDto;
  currency: string;
};

/**
 * One recovery path.
 * Recurring breathing room is the monthly amount after the last change. Each row is one payoff.
 */
export function PlanRecoveryPath({
  title,
  description,
  path,
  currency,
}: PlanRecoveryPathProps) {
  const titleId = `${path.kind}-title`;

  return (
    <section className="app-panel" aria-labelledby={titleId}>
      <div className="app-panel-header px-4 py-3">
        <h2 id={titleId} className="app-section-title">
          {title}
        </h2>
        <p className="app-section-meta">{description}</p>
        <p className="mt-3 text-xs text-muted-foreground">
          Recurring breathing room
        </p>
        <p className="text-xl font-semibold tracking-tight text-foreground tabular-nums">
          {formatCurrency(path.recurringRoom, currency)} a month
        </p>
      </div>
      {path.steps.length > 0 ? (
        <ul className="divide-y divide-border/70">
          {path.steps.map((step) => (
            <li
              key={step.debtId}
              className="flex flex-col gap-0.5 px-4 py-3 sm:flex-row sm:items-baseline sm:justify-between sm:gap-4"
            >
              <div className="min-w-0">
                <p className="text-sm text-foreground">{step.name}</p>
                <p className="text-xs text-muted-foreground">
                  {minimumLeavesOn(
                    step,
                    formatCurrency(step.minimum, currency),
                  )}
                </p>
              </div>
              <p className="text-sm text-foreground tabular-nums sm:shrink-0 sm:text-right">
                <span className="text-muted-foreground">Breathing room </span>
                {formatCurrency(step.breathingRoom, currency)}
              </p>
            </li>
          ))}
        </ul>
      ) : null}
      <p className="border-t border-border/70 px-4 py-3 text-sm text-muted-foreground">
        {path.explanation}
      </p>
    </section>
  );
}
