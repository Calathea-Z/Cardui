import { ChevronRight, CircleAlert, LoaderCircle } from "lucide-react";
import { SegmentedControl } from "@/components/ui/segmented-control";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type { PayoffRolloverKind } from "@/lib/api/types";
import { PlanExtraField } from "./PlanExtraField";
import {
  planExtraStatus,
  planPathDescription,
  planPathOptions,
} from "./planCopy";
import type { PlanExtraStatus } from "./usePlanExtra";

type PlanScenarioControlsProps = {
  kind: PayoffRolloverKind;
  hasPayoff: boolean;
  appliedAmount: number;
  requestedAmount: number | null;
  status: PlanExtraStatus;
  currency: string;
  onKindChange: (kind: PayoffRolloverKind) => void;
  onApplyExtra: (amount: number) => void;
};

/**
 * Groups the temporary payoff behavior and extra payment in one disclosure.
 * Its summary always describes the applied forecast, while pending and failed requests name the older amount still on screen.
 */
export function PlanScenarioControls({
  kind,
  hasPayoff,
  appliedAmount,
  requestedAmount,
  status,
  currency,
  onKindChange,
  onApplyExtra,
}: PlanScenarioControlsProps) {
  const money = (amount: number) => formatCurrency(amount, currency);
  const strategy =
    kind === "Rollover" ? "Roll payments forward" : "Free up cash";
  const statusMessage =
    status !== "ready" && requestedAmount !== null
      ? planExtraStatus(status, requestedAmount, appliedAmount, money)
      : null;

  return (
    <section className="app-panel">
      <details className="group">
        <summary className="flex min-h-11 cursor-pointer items-center justify-between gap-3 px-4 py-3 focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none focus-visible:ring-inset">
          <span>
            <span className="block text-sm font-semibold text-foreground">
              Try a scenario
            </span>
            <span className="block text-xs text-muted-foreground">
              {strategy} · {money(appliedAmount)} extra
            </span>
          </span>
          <ChevronRight
            aria-hidden
            className="size-4 shrink-0 transition-transform group-open:rotate-90 motion-reduce:transition-none"
          />
        </summary>
        <div className="grid gap-5 border-t border-border p-4 md:grid-cols-[minmax(0,1.3fr)_minmax(14rem,0.7fr)]">
          <div>
            <p className="text-sm font-medium text-foreground">
              When a payment ends
            </p>
            {hasPayoff ? (
              <>
                <SegmentedControl
                  label="Payment strategy"
                  options={planPathOptions}
                  value={kind}
                  onChange={onKindChange}
                  className="mt-2"
                />
                <p className="mt-2 text-sm text-muted-foreground">
                  {planPathDescription(kind)}
                </p>
              </>
            ) : (
              <p className="mt-1 text-sm text-muted-foreground">
                Payment strategy becomes available after at least one debt has a
                projected payoff.
              </p>
            )}
          </div>
          <PlanExtraField
            appliedAmount={appliedAmount}
            retryCurrentAmount={status !== "ready"}
            onApply={onApplyExtra}
          />
          <p className="text-xs text-muted-foreground md:col-span-2">
            Temporary preview. Nothing is saved, and the scenario resets when
            you leave Plan.
          </p>
        </div>
      </details>
      {statusMessage ? (
        <p
          aria-live="polite"
          className={`flex items-start gap-2 border-t border-border px-4 py-3 text-sm ${
            status === "error" ? "text-destructive" : "text-muted-foreground"
          }`}
        >
          {status === "updating" ? (
            <LoaderCircle
              aria-hidden
              className="mt-0.5 size-4 shrink-0 animate-spin motion-reduce:animate-none"
            />
          ) : (
            <CircleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
          )}
          {statusMessage}
        </p>
      ) : null}
    </section>
  );
}
