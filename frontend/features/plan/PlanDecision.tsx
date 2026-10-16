"use client";

import { ChevronRight, CircleAlert, TriangleAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { formatCalendarDate } from "@/features/debts/debtDisplay";
import type {
  DecisionPlanDto,
  PayoffRolloverKind,
  PlanScenarioQuery,
} from "@/lib/api/types";
import {
  decisionActionCopy,
  decisionAssumptionCopy,
  decisionFactLines,
  decisionMilestoneCopy,
  decisionMissingCopy,
  decisionPriorityLine,
  decisionReviewCopy,
  decisionRiskCopy,
  decisionScenarioLines,
  type DecisionLine,
} from "./decisionPlanCopy";
import { useSavedPlan } from "./useSavedPlan";

type PlanDecisionProps = {
  scenario: PlanScenarioQuery;
  kind: PayoffRolloverKind;
  currency: string;
};

/**
 * Your plan: the three actions, then the facts they rest on.
 * Save keeps a draft. Accept replaces the previous accepted plan. Print uses the browser.
 */
export function PlanDecision({ scenario, kind, currency }: PlanDecisionProps) {
  const saved = useSavedPlan(scenario, kind, true);
  const plan = saved.chrome.plan;
  const money = (amount: number) => formatCurrency(amount, currency);

  if (!plan && saved.loadStatus !== "error") {
    return (
      <p className="text-sm text-muted-foreground">Preparing this plan.</p>
    );
  }

  if (!plan) {
    return (
      <div className="rounded-lg border border-border bg-card px-4 py-6">
        <p className="text-sm text-foreground" role="alert">
          {saved.message ?? "This plan could not be prepared."}
        </p>
        <Button
          type="button"
          className="mt-4 min-h-11 px-4"
          onClick={() => void saved.retry()}
        >
          Try again
        </Button>
      </div>
    );
  }

  const asOf = plan.asOf ? formatCalendarDate(plan.asOf) : "";
  const versionLabel = saved.chrome.version
    ? `Version ${saved.chrome.version.number}`
    : "Preview";

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-4 print:hidden sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <h2 className="app-section-title">Your plan</h2>
          <p className="mt-2 text-sm text-foreground">
            <span className="font-medium">{saved.chrome.label}.</span>{" "}
            {versionLabel}
            {asOf ? ` · As of ${asOf}` : ""}
          </p>
          <p className="mt-2 max-w-xl text-sm text-muted-foreground">
            {saved.chrome.note} Cardui does not pay a bill, pay a debt, or move
            money.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <DecisionActions saved={saved} />
        </div>
      </div>

      {saved.message ? (
        <p
          className={
            saved.messageTone === "error"
              ? "text-sm text-destructive print:hidden"
              : "text-sm text-foreground print:hidden"
          }
          role={saved.messageTone === "error" ? "alert" : "status"}
        >
          {saved.message}
        </p>
      ) : null}

      <article className="plan-decision-print flex flex-col gap-6">
        <header className="hidden print:block">
          <p className="text-lg font-semibold text-foreground">Your plan</p>
          <p className="mt-2 text-sm text-foreground">
            {saved.chrome.label}. {versionLabel}
            {asOf ? ` · As of ${asOf}` : ""}
          </p>
          <p className="mt-2 text-sm text-foreground">
            Cardui does not pay a bill, pay a debt, or move money.
          </p>
        </header>

        {plan.actions.length > 0 ? (
          <ol className="flex flex-col gap-4">
            {plan.actions.map((action) => {
              const copy = decisionActionCopy(
                action,
                money,
                formatCalendarDate,
              );
              return (
                <li
                  key={action.order}
                  className="rounded-lg border border-border bg-card px-4 py-4"
                >
                  <p className="text-xs font-medium text-muted-foreground">
                    Action {action.order}
                  </p>
                  <p className="mt-1 text-base font-semibold text-foreground">
                    {copy.title}
                  </p>
                  <p className="mt-2 text-sm text-foreground">{copy.why}</p>
                  <p className="mt-2 text-sm text-muted-foreground">
                    {copy.effect}
                  </p>
                </li>
              );
            })}
          </ol>
        ) : (
          <p className="text-sm text-muted-foreground">
            This plan has no next action.
          </p>
        )}

        <div className="print:hidden">
          <DecisionSupport plan={plan} money={money} expanded={false} />
        </div>
        <div className="hidden print:block">
          <DecisionSupport plan={plan} money={money} expanded />
        </div>
      </article>

      {saved.versions.length > 0 ? (
        <details className="group rounded-lg border border-border bg-card print:hidden">
          <summary className="flex min-h-11 cursor-pointer items-center justify-between gap-3 px-4 py-3 text-sm font-medium focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none focus-visible:ring-inset">
            <span>
              {saved.versions.length}{" "}
              {saved.versions.length === 1 ? "saved version" : "saved versions"}
            </span>
            <ChevronRight
              aria-hidden
              className="size-4 shrink-0 transition-transform group-open:rotate-90 motion-reduce:transition-none"
            />
          </summary>
          <ul className="border-t border-border">
            {saved.versions.map((version) => {
              const showing = saved.chrome.version?.id === version.id;
              return (
                <li
                  key={version.id}
                  className="flex flex-wrap items-center justify-between gap-3 border-b border-border/70 px-4 py-3 last:border-b-0"
                >
                  <p className="text-sm text-foreground">
                    Version {version.number} · {version.status} · As of{" "}
                    {formatCalendarDate(version.plan.asOf)}
                  </p>
                  {showing ? (
                    <span className="text-sm text-muted-foreground">
                      Showing
                    </span>
                  ) : (
                    <Button
                      type="button"
                      variant="outline"
                      className="min-h-11 px-4"
                      onClick={() => saved.viewVersion(version.id)}
                    >
                      View
                    </Button>
                  )}
                </li>
              );
            })}
          </ul>
        </details>
      ) : null}
    </div>
  );
}

type SavedPlanController = ReturnType<typeof useSavedPlan>;

/**
 * The leading action for the plan on screen, plus print.
 * Print stays available beside save or accept.
 */
function DecisionActions({ saved }: { saved: SavedPlanController }) {
  const action = saved.chrome.action;
  const saving = saved.saving;

  return (
    <>
      {action === "save" ? (
        <Button
          type="button"
          className="min-h-11 px-4"
          disabled={saving}
          onClick={() => void saved.save()}
        >
          {saved.chrome.replacesDraftId ? "Save a new draft" : "Save draft"}
        </Button>
      ) : null}
      {action === "accept" ? (
        <Button
          type="button"
          className="min-h-11 px-4"
          disabled={saving || !saved.chrome.acceptId}
          onClick={() => void saved.accept()}
        >
          Accept plan
        </Button>
      ) : null}
      {action === "back" ? (
        <Button
          type="button"
          className="min-h-11 px-4"
          onClick={() => saved.viewVersion(null)}
        >
          Show the current preview
        </Button>
      ) : null}
      <Button
        type="button"
        variant={action === "print" ? "default" : "outline"}
        className="min-h-11 px-4"
        onClick={() => window.print()}
      >
        Print
      </Button>
      {saved.viewedId && action !== "back" ? (
        <Button
          type="button"
          variant="outline"
          className="min-h-11 px-4"
          onClick={() => saved.viewVersion(null)}
        >
          Show the current preview
        </Button>
      ) : null}
    </>
  );
}

/**
 * Assumptions, risks, gaps, milestones, and the captured facts.
 * The screen keeps them closed. Print shows them open.
 */
function DecisionSupport({
  plan,
  money,
  expanded,
}: {
  plan: DecisionPlanDto;
  money: (amount: number) => string;
  expanded: boolean;
}) {
  const warningRisk = plan.risks.some(
    (risk) =>
      risk.kind === "CashShortfall" ||
      risk.kind === "ReserveShortfall" ||
      risk.kind === "ExtraGuardrail",
  );

  return (
    <div className="flex flex-col gap-4">
      <DecisionSection
        title="Facts"
        lines={decisionFactLines(plan, money, formatCalendarDate)}
        expanded={expanded}
      />
      <DecisionSection
        title="Priorities"
        lines={
          plan.debts.length > 0
            ? plan.debts.map((debt) => decisionPriorityLine(debt, money))
            : [{ term: "Debts", detail: "No debt is in this payoff order." }]
        }
        expanded={expanded}
      />
      <DecisionSection
        title="Selected scenario"
        lines={decisionScenarioLines(plan, money, formatCalendarDate)}
        expanded={expanded}
      />
      <DecisionSection
        title="Milestones"
        lines={
          plan.milestones.length > 0
            ? plan.milestones.map((milestone) =>
                decisionMilestoneCopy(milestone, money, formatCalendarDate),
              )
            : [
                {
                  term: "Milestones",
                  detail: "This plan has no dated milestone.",
                },
              ]
        }
        expanded={expanded}
      />
      <DecisionSection
        title="Assumptions"
        lines={plan.assumptions.map((detail) =>
          decisionAssumptionCopy(detail, plan, money, formatCalendarDate),
        )}
        expanded={expanded}
      />
      <DecisionSection
        title="Risks"
        warning={warningRisk}
        lines={
          plan.risks.length > 0
            ? plan.risks.map((detail) =>
                decisionRiskCopy(detail, money, formatCalendarDate),
              )
            : [
                {
                  term: "Risks",
                  detail: "No extra risk is recorded for this plan.",
                },
              ]
        }
        expanded={expanded}
      />
      <DecisionSection
        title="Missing information"
        warning={plan.missing.length > 0}
        lines={
          plan.missing.length > 0
            ? plan.missing.map((detail) => decisionMissingCopy(detail))
            : [
                {
                  term: "Missing information",
                  detail: "Nothing else is listed as missing.",
                },
              ]
        }
        expanded={expanded}
      />
      <DecisionSection
        title="Review"
        lines={plan.reviewConditions.map((detail) =>
          decisionReviewCopy(detail, formatCalendarDate),
        )}
        expanded={expanded}
      />
    </div>
  );
}

/**
 * One supporting group. A warning summary uses the warning icon and color.
 */
function DecisionSection({
  title,
  lines,
  expanded,
  warning = false,
}: {
  title: string;
  lines: DecisionLine[];
  expanded: boolean;
  warning?: boolean;
}) {
  const body = (
    <ul className="flex flex-col gap-2">
      {lines.map((line, index) => (
        <li key={`${line.term}-${index}`} className="text-sm text-foreground">
          <span className="font-medium">{line.term}. </span>
          {line.detail}
        </li>
      ))}
    </ul>
  );

  if (expanded) {
    return (
      <section className="flex flex-col gap-2">
        <h3 className="text-sm font-semibold text-foreground">{title}</h3>
        {body}
      </section>
    );
  }

  return (
    <details
      className={
        warning
          ? "group rounded-lg border border-warning/40 bg-warning/10"
          : "group rounded-lg border border-border bg-card"
      }
    >
      <summary
        className={
          warning
            ? "flex min-h-11 cursor-pointer items-center justify-between gap-3 px-4 py-3 text-sm font-medium text-warning focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none focus-visible:ring-inset"
            : "flex min-h-11 cursor-pointer items-center justify-between gap-3 px-4 py-3 text-sm font-medium focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none focus-visible:ring-inset"
        }
      >
        <span className="flex items-center gap-2">
          {warning ? (
            title === "Risks" ? (
              <TriangleAlert aria-hidden className="size-4 shrink-0" />
            ) : (
              <CircleAlert aria-hidden className="size-4 shrink-0" />
            )
          ) : null}
          {title}
        </span>
        <ChevronRight
          aria-hidden
          className="size-4 shrink-0 transition-transform group-open:rotate-90 motion-reduce:transition-none"
        />
      </summary>
      <div className="border-t border-border px-4 py-3">{body}</div>
    </details>
  );
}
