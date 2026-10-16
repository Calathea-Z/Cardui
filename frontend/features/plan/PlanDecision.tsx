"use client";

import Link from "next/link";
import { ChevronRight, CircleAlert, TriangleAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { formatCalendarDate } from "@/features/debts/debtDisplay";
import type { DecisionPlanDto } from "@/lib/api/types";
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
import {
  decisionActionTarget,
  planVersionPrintLines,
} from "./decisionPlanView";
import { useSavedPlan } from "./useSavedPlan";

type PlanDecisionProps = {
  saved: ReturnType<typeof useSavedPlan>;
  calculationReady: boolean;
  onOpenCash: () => void;
  onOpenDebt: () => void;
};

/**
 * Your plan: the version on screen, then at most three actions.
 * Save keeps a draft. Accept replaces the previous accepted plan and leaves it in history. Print uses the browser.
 */
export function PlanDecision({
  saved,
  calculationReady,
  onOpenCash,
  onOpenDebt,
}: PlanDecisionProps) {
  const plan = saved.chrome.plan;
  const money = (amount: number) =>
    formatCurrency(amount, plan?.planningCurrency ?? "USD");

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

  const printed = planVersionPrintLines(saved.chrome, formatCalendarDate);
  const readingStored = saved.viewedId !== null;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-4 print:hidden sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <h2 className="app-section-title">Your plan</h2>
          <VersionLines printed={printed} />
          {readingStored ? (
            <p className="mt-2 max-w-xl text-sm text-foreground">
              You are reading a saved version. The scenario above edits the
              working preview and does not change this version.
            </p>
          ) : null}
          <p className="mt-2 max-w-xl text-sm text-muted-foreground">
            {saved.chrome.note}
          </p>
        </div>
        <div className="flex flex-col items-start gap-2">
          <div className="flex flex-wrap gap-2">
            <DecisionActions
              saved={saved}
              calculationReady={calculationReady}
            />
          </div>
          {saved.chrome.action === "save" && !calculationReady ? (
            <p className="max-w-xs text-sm text-foreground">
              This working scenario has not been calculated, so it cannot be
              saved.
            </p>
          ) : null}
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
          <VersionLines printed={printed} />
          <p className="mt-2 text-sm text-foreground">
            Cardui does not schedule a payment or change an account.
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
                  <ActionLink
                    kind={action.kind}
                    onOpenCash={onOpenCash}
                    onOpenDebt={onOpenDebt}
                  />
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
            <span>History</span>
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
                    Version {version.number} ·{" "}
                    {versionStatusName(version.status)} · As of{" "}
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
function DecisionActions({
  saved,
  calculationReady,
}: {
  saved: SavedPlanController;
  calculationReady: boolean;
}) {
  const action = saved.chrome.action;
  const saving = saved.saving;

  return (
    <>
      {action === "save" ? (
        <Button
          type="button"
          className="min-h-11 px-4"
          disabled={saving || !calculationReady}
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
      {action === "back" || saved.viewedId ? (
        <Button
          type="button"
          variant={action === "back" ? "default" : "outline"}
          className="min-h-11 px-4"
          onClick={() => saved.viewVersion(null)}
        >
          Return to working preview
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
    </>
  );
}

/**
 * Status, version, and date for the plan on screen and on the printed page.
 */
function VersionLines({
  printed,
}: {
  printed: ReturnType<typeof planVersionPrintLines>;
}) {
  return (
    <div className="mt-2 text-sm text-foreground">
      <p>
        <span className="font-medium">Status:</span> {printed.status}
      </p>
      <p>
        <span className="font-medium">Version:</span> {printed.version}
      </p>
      <p>{printed.date}</p>
      {printed.recorded ? <p>{printed.recorded}</p> : null}
    </div>
  );
}

/**
 * The review or edit link for one action.
 * It stays off the printed page.
 */
function ActionLink({
  kind,
  onOpenCash,
  onOpenDebt,
}: {
  kind: DecisionPlanDto["actions"][number]["kind"];
  onOpenCash: () => void;
  onOpenDebt: () => void;
}) {
  const target = decisionActionTarget(kind);
  if (!target) {
    return null;
  }

  if (target.href) {
    return (
      <Link
        href={target.href}
        className="mt-3 inline-flex min-h-11 items-center text-sm font-medium text-primary underline print:hidden"
      >
        {target.label}
      </Link>
    );
  }

  return (
    <button
      type="button"
      className="mt-3 min-h-11 text-sm font-medium text-primary underline print:hidden"
      onClick={target.view === "cash" ? onOpenCash : onOpenDebt}
    >
      {target.label}
    </button>
  );
}

/**
 * The name shown for a stored version.
 * Older copies are History. The stored status itself is unchanged.
 */
function versionStatusName(status: string) {
  if (status === "Draft") {
    return "Saved draft";
  }

  if (status === "Accepted") {
    return "Accepted plan";
  }

  if (status === "Superseded") {
    return "History";
  }

  return status;
}

const invalidatingRisks = [
  "CashShortfall",
  "ReserveShortfall",
  "ExtraGuardrail",
  "PayoffConditional",
] as const;

/**
 * Risks that change the result stay visible. The rest sits in two disclosures.
 * Print opens both disclosures.
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
  const invalidating = plan.risks.filter((risk) =>
    invalidatingRisks.some((kind) => kind === risk.kind),
  );
  const otherRisks = plan.risks.filter(
    (risk) => !invalidatingRisks.some((kind) => kind === risk.kind),
  );
  const priorities =
    plan.debts.length > 0
      ? plan.debts.map((debt) => decisionPriorityLine(debt, money))
      : [{ term: "Debts", detail: "No debt is in this payoff order." }];
  const milestones =
    plan.milestones.length > 0
      ? plan.milestones.map((milestone) =>
          decisionMilestoneCopy(milestone, money, formatCalendarDate),
        )
      : [{ term: "Milestones", detail: "This plan has no dated milestone." }];

  return (
    <div className="flex flex-col gap-4">
      {invalidating.length > 0 ? (
        <section className="flex flex-col gap-2 rounded-lg border border-warning/40 bg-warning/10 px-4 py-3">
          <h3 className="flex items-center gap-2 text-sm font-semibold text-warning">
            <TriangleAlert aria-hidden className="size-4 shrink-0" />
            Risks
          </h3>
          <ul className="flex flex-col gap-2">
            {invalidating.map((detail, index) => {
              const line = decisionRiskCopy(detail, money, formatCalendarDate);
              return (
                <li
                  key={`${line.term}-${index}`}
                  className="text-sm text-foreground"
                >
                  <span className="font-medium">{line.term}. </span>
                  {line.detail}
                </li>
              );
            })}
          </ul>
        </section>
      ) : null}
      <DecisionSection
        title="Assumptions and scenario"
        lines={[
          ...decisionScenarioLines(plan, money, formatCalendarDate),
          ...decisionFactLines(plan, money, formatCalendarDate),
          ...priorities,
          ...plan.assumptions.map((detail) =>
            decisionAssumptionCopy(detail, plan, money, formatCalendarDate),
          ),
        ]}
        expanded={expanded}
      />
      <DecisionSection
        title="Milestones and review"
        warning={plan.missing.length > 0}
        lines={[
          ...milestones,
          ...(otherRisks.length > 0
            ? otherRisks.map((detail) =>
                decisionRiskCopy(detail, money, formatCalendarDate),
              )
            : [{ term: "Other risks", detail: "No other risk is recorded." }]),
          ...(plan.missing.length > 0
            ? plan.missing.map((detail) => decisionMissingCopy(detail))
            : [
                {
                  term: "Missing information",
                  detail: "Nothing else is listed as missing.",
                },
              ]),
          ...plan.reviewConditions.map((detail) =>
            decisionReviewCopy(detail, formatCalendarDate),
          ),
        ]}
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
            <CircleAlert aria-hidden className="size-4 shrink-0" />
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
