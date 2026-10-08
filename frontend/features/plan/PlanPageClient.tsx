"use client";

import Link from "next/link";
import { PageHeader } from "@/components/navigation/page-header";
import { buttonVariants } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import type { CashFlowRecoveryReportDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { PlanRecoveryPath } from "./PlanRecoveryPath";
import { excludedCurrencyNote } from "./planRecoveryCopy";

type PlanPageClientProps = {
  report: CashFlowRecoveryReportDto;
  failed: boolean;
};

/**
 * Plan page.
 * Shows when each payoff removes a minimum, and the breathing room on rollover and on keeping every freed payment.
 * A failed load stays on this page with the error above it. No debts asks for one on Debts.
 */
export function PlanPageClient({ report, failed }: PlanPageClientProps) {
  const excluded = excludedCurrencyNote(
    report.excludedCurrencies,
    report.planningCurrency,
  );

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Plan"
        description="When each payoff removes a monthly obligation, and the breathing room that follows. Debts are paid highest interest first, with no extra payment."
      />
      {failed ? (
        <p className="text-sm text-muted-foreground">
          This plan could not be loaded.
        </p>
      ) : !report.hasDebts ? (
        <div className="rounded-lg border border-border bg-card">
          <EmptyState
            title="A debt is required"
            description="Add a card or loan. This page shows when each payoff removes its minimum, and the breathing room that follows."
            action={
              <Link
                href="/debts"
                className={cn(buttonVariants(), "min-h-11 px-4")}
              >
                Go to Debts
              </Link>
            }
          />
        </div>
      ) : (
        <>
          {excluded ? (
            <p className="text-sm text-foreground">{excluded}</p>
          ) : null}
          <PlanRecoveryPath
            title="Rollover"
            description="Freed payments stay on the next debt."
            path={report.rollover}
            currency={report.planningCurrency}
          />
          <PlanRecoveryPath
            title="Keeping every freed payment"
            description="Each freed payment is kept instead of paying the next debt."
            path={report.reclaimAll}
            currency={report.planningCurrency}
          />
          <details className="app-panel">
            <summary className="min-h-11 cursor-pointer px-4 py-3 text-sm font-medium text-foreground focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/50">
              Assumptions
            </summary>
            <ul className="flex flex-col gap-2 border-t border-border px-4 py-3">
              {report.assumptions.map((assumption) => (
                <li key={assumption} className="text-sm text-muted-foreground">
                  {assumption}
                </li>
              ))}
            </ul>
          </details>
        </>
      )}
    </div>
  );
}
