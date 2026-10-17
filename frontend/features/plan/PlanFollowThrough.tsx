import Link from "next/link";
import { CircleAlert, TriangleAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { formatCalendarDate } from "@/features/debts/debtDisplay";
import type { DecisionPlanActionKind } from "@/lib/api/types";
import {
  decisionActionCopy,
  decisionMilestoneCopy,
  decisionRiskCopy,
} from "./decisionPlanCopy";
import { decisionActionTarget } from "./decisionPlanView";
import type { PlanFollowThrough as PlanFollowThroughModel } from "./planHomeFollowThrough";

type PlanFollowThroughProps = {
  status: "loading" | "ready" | "error";
  followThrough: PlanFollowThroughModel;
  currency: string;
  onOpenShortfall: () => void;
  onOpenDebt: () => void;
  onTryChange: () => void;
  onRetry: () => void;
};

type FollowTarget = {
  label: string;
  href?: string;
  view?: "shortfall" | "debt" | "changes";
};

/**
 * Ranked actions, unset assumptions, and calculated milestones under the 30-day action.
 * The working preview supplies them. A saved version opened from History does not replace this column.
 */
export function PlanFollowThrough({
  status,
  followThrough,
  currency,
  onOpenShortfall,
  onOpenDebt,
  onTryChange,
  onRetry,
}: PlanFollowThroughProps) {
  if (
    status === "ready" &&
    followThrough.actions.length === 0 &&
    followThrough.confirmations.length === 0 &&
    followThrough.milestones.length === 0
  ) {
    return null;
  }

  const money = (amount: number) => formatCurrency(amount, currency);
  const conditional = followThrough.payoffDatesConditional
    ? decisionRiskCopy(
        {
          kind: "PayoffConditional",
          name: null,
          amount: null,
          on: null,
          count: null,
        },
        money,
        formatCalendarDate,
      ).detail
    : null;
  const caveatWithActions =
    conditional !== null &&
    followThrough.actions.some(
      (action) =>
        action.kind === "PayFirstDebt" || action.kind === "UseFreedPayment",
    );

  return (
    <div className="flex flex-col gap-4">
      {status === "loading" ? (
        <section className="app-panel p-4" aria-labelledby="plan-next-actions">
          <h2 id="plan-next-actions" className="app-section-title">
            Next actions
          </h2>
          <p className="mt-2 text-sm text-muted-foreground">
            Preparing next actions, milestones, and assumptions.
          </p>
        </section>
      ) : null}
      {status === "error" ? (
        <section className="app-panel p-4" aria-labelledby="plan-next-actions">
          <h2 id="plan-next-actions" className="app-section-title">
            Next actions
          </h2>
          <p className="mt-2 text-sm text-foreground" role="alert">
            Next actions could not be prepared. The 30-day figure is unchanged.
          </p>
          <Button
            type="button"
            variant="outline"
            className="mt-3 min-h-11 bg-card px-4"
            onClick={onRetry}
          >
            Try again
          </Button>
        </section>
      ) : null}
      {status === "ready" && followThrough.actions.length > 0 ? (
        <section className="app-panel p-4" aria-labelledby="plan-next-actions">
          <h2 id="plan-next-actions" className="app-section-title">
            Next actions
          </h2>
          <p className="app-section-meta">
            {followThrough.safetyLeads
              ? "The 30-day action above is step 1. These follow it."
              : "In the order this plan ranks them."}
          </p>
          <ul className="mt-3 flex flex-col gap-4">
            {followThrough.actions.map((action) => {
              const copy = decisionActionCopy(
                action,
                money,
                formatCalendarDate,
              );
              return (
                <li
                  key={action.order}
                  className="border-t border-border pt-3 first:border-t-0 first:pt-0"
                >
                  <p className="text-xs font-medium text-muted-foreground">
                    Action {action.order}
                  </p>
                  <p className="mt-1 text-sm font-medium text-foreground">
                    {copy.title}
                  </p>
                  <p className="mt-1 text-sm text-foreground">{copy.why}</p>
                  <p className="mt-1 text-sm text-muted-foreground">
                    {copy.effect}
                  </p>
                  <ActionControl
                    kind={action.kind}
                    onOpenShortfall={onOpenShortfall}
                    onOpenDebt={onOpenDebt}
                    onTryChange={onTryChange}
                  />
                </li>
              );
            })}
          </ul>
          {caveatWithActions ? <ConditionalNote text={conditional} /> : null}
        </section>
      ) : null}
      {status === "ready" && followThrough.confirmations.length > 0 ? (
        <section
          className="app-panel border-warning/40"
          aria-labelledby="plan-confirmations"
        >
          <div className="app-panel-header px-4 py-3">
            <h2
              id="plan-confirmations"
              className="app-section-title flex items-center gap-2 text-warning"
            >
              <TriangleAlert aria-hidden className="size-4 shrink-0" />
              Assumptions to confirm
            </h2>
            <p className="app-section-meta">
              The plan is going ahead without these.
            </p>
          </div>
          <ul>
            {followThrough.confirmations.map((item) => (
              <li
                key={item.kind}
                className="flex flex-col gap-2 border-b border-border/70 px-4 py-3 last:border-b-0 sm:flex-row sm:items-start sm:justify-between"
              >
                <div className="flex min-w-0 items-start gap-2">
                  <CircleAlert
                    aria-hidden
                    className="mt-0.5 size-4 shrink-0 text-warning"
                  />
                  <div>
                    <p className="text-sm font-medium text-foreground">
                      {item.term}
                    </p>
                    <p className="mt-0.5 text-sm text-muted-foreground">
                      {item.detail}
                    </p>
                  </div>
                </div>
                <Link
                  href={item.href}
                  className="inline-flex min-h-11 shrink-0 items-center text-sm font-medium text-primary underline focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
                >
                  {item.label}
                </Link>
              </li>
            ))}
          </ul>
        </section>
      ) : null}
      {status === "ready" && followThrough.milestones.length > 0 ? (
        <section className="app-panel p-4" aria-labelledby="plan-milestones">
          <h2 id="plan-milestones" className="app-section-title">
            Milestones
          </h2>
          <p className="app-section-meta">
            Dates and amounts this plan calculated.
          </p>
          <ul className="mt-3 flex flex-col gap-3">
            {followThrough.milestones.map((milestone, index) => {
              const copy = decisionMilestoneCopy(
                milestone,
                money,
                formatCalendarDate,
              );
              return (
                <li key={`${milestone.kind}-${milestone.name ?? ""}-${index}`}>
                  <p className="text-sm font-medium text-foreground">
                    {copy.term}
                  </p>
                  <p className="mt-0.5 text-sm text-muted-foreground">
                    {copy.detail}
                  </p>
                </li>
              );
            })}
          </ul>
          {conditional && !caveatWithActions ? (
            <ConditionalNote text={conditional} />
          ) : null}
        </section>
      ) : null}
    </div>
  );
}

/**
 * The payoff-date caveat beside the result that states the date.
 * It uses the same sentence as Your plan. Color is paired with the icon.
 */
function ConditionalNote({ text }: { text: string }) {
  return (
    <p className="mt-3 flex items-start gap-2 text-sm text-warning">
      <CircleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
      <span>{text}</span>
    </p>
  );
}

/**
 * Where a ranked action can be reviewed from Overview.
 * Lowering the extra and holding a windfall open Try changes, because those fields are not on this view.
 */
function followTarget(kind: DecisionPlanActionKind): FollowTarget | null {
  if (kind === "LowerExtra" || kind === "HoldWindfall") {
    return { label: "Try a change", view: "changes" };
  }

  return decisionActionTarget(kind);
}

/**
 * The review control for one ranked action.
 * An action with nowhere to go on this page has no control.
 */
function ActionControl({
  kind,
  onOpenShortfall,
  onOpenDebt,
  onTryChange,
}: {
  kind: DecisionPlanActionKind;
  onOpenShortfall: () => void;
  onOpenDebt: () => void;
  onTryChange: () => void;
}) {
  const target = followTarget(kind);
  if (!target) {
    return null;
  }

  if (target.href) {
    return (
      <Link
        href={target.href}
        className="mt-2 inline-flex min-h-11 items-center text-sm font-medium text-primary underline focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
      >
        {target.label}
      </Link>
    );
  }

  return (
    <button
      type="button"
      onClick={() =>
        openTarget(target, onOpenShortfall, onOpenDebt, onTryChange)
      }
      className="mt-2 min-h-11 text-sm font-medium text-primary underline focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
    >
      {target.label}
    </button>
  );
}

/**
 * Opens the Plan detail or Try changes named by the action.
 */
function openTarget(
  target: FollowTarget,
  onOpenShortfall: () => void,
  onOpenDebt: () => void,
  onTryChange: () => void,
) {
  if (target.view === "shortfall") {
    onOpenShortfall();
    return;
  }

  if (target.view === "debt") {
    onOpenDebt();
    return;
  }

  onTryChange();
}
