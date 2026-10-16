"use client";

import { useCallback, useRef, useState } from "react";
import { getPlanRecovery } from "@/lib/api/browser";
import type { PlanRecoveryDto, PlanScenarioQuery } from "@/lib/api/types";
import { isSavedPlanScenario } from "./planScenario";

/**
 * Whether the tried preview matches the plan on screen.
 * Updating and error keep the previous plan visible.
 */
export type PlanScenarioStatus = "ready" | "updating" | "error";

/**
 * Holds the plan on screen and reloads it for a temporary preview.
 * The saved plan is the page load. A newer request replaces an older one still in flight. Nothing is saved.
 * `onApplied` runs in the same update as the report, so the path and the figures change together. A failed or replaced request does not call it.
 */
export function usePlanScenario(baseline: PlanRecoveryDto) {
  const [report, setReport] = useState(baseline);
  const [status, setStatus] = useState<PlanScenarioStatus>("ready");
  const [requested, setRequested] = useState<PlanScenarioQuery | null>(null);
  const request = useRef(0);

  const applyScenario = useCallback(
    async (scenario: PlanScenarioQuery, onApplied?: () => void) => {
      const id = ++request.current;
      setRequested(scenario);
      if (isSavedPlanScenario(scenario)) {
        setReport(baseline);
        setStatus("ready");
        setRequested(null);
        onApplied?.();
        return true;
      }

      setStatus("updating");
      try {
        const next = await getPlanRecovery(scenario);
        if (request.current !== id) {
          return false;
        }

        setReport(next);
        setStatus("ready");
        setRequested(null);
        onApplied?.();
        return true;
      } catch {
        if (request.current !== id) {
          return false;
        }

        setStatus("error");
        return false;
      }
    },
    [baseline],
  );

  return { report, status, requested, applyScenario };
}

/**
 * The preview currently represented by a plan response.
 * Null spending and floor amounts mean the saved figures are still in use.
 */
export function scenarioFromReport(report: PlanRecoveryDto): PlanScenarioQuery {
  return {
    monthlyExtra: report.monthlyExtra,
    incomeLossMonths: report.incomeLossMonths,
    windfallAmount: report.windfallAmount,
    windfallOn: report.windfallOn,
    livingSpendingMonthly: report.scenarioLivingSpendingMonthly,
    cashFloor: report.scenarioCashFloor,
    priority: report.priority,
    constraintDebtId: report.constraintDebtId,
    constraintKind: report.constraintKind,
    reclaimAmount: report.reclaimAmount,
  };
}
