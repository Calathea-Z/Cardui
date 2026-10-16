"use client";

import {
  useCallback,
  useEffect,
  useRef,
  useState,
  useSyncExternalStore,
} from "react";
import type { PayoffRolloverKind, PlanRecoveryDto } from "@/lib/api/types";
import { isSavedPlanScenario } from "./planScenario";
import {
  clearPlanWork,
  readPlanWork,
  writePlanWork,
  type PlanWorkState,
  type PlanWorkStore,
} from "./planWorkState";
import { scenarioFromReport, usePlanScenario } from "./usePlanScenario";

/**
 * Holds the working payoff path and temporary preview for the Plan page.
 * Switching local views keeps both. Opening an editor and returning restores the last applied preview from this browser session.
 */
export function usePlanWork(baseline: PlanRecoveryDto) {
  const { report, status, applyScenario } = usePlanScenario(baseline);
  const mounted = useSyncExternalStore(
    subscribeToNothing,
    () => true,
    () => false,
  );
  const [kindOverride, setKindOverride] = useState<PayoffRolloverKind | null>(
    null,
  );
  const [settled, setSettled] = useState(false);
  const phase = useRef<"idle" | "restoring" | "waiting" | "ready">("idle");
  const stored = mounted ? clientWork() : null;
  const kind = kindOverride ?? stored?.kind ?? "Rollover";

  useEffect(() => {
    if (!mounted || phase.current !== "idle") {
      return;
    }

    const work = clientWork();
    if (work && !isSavedPlanScenario(work.scenario)) {
      phase.current = "restoring";
      void applyScenario(work.scenario);
      return;
    }

    phase.current = "ready";
  }, [applyScenario, mounted]);

  useEffect(() => {
    if (!mounted || phase.current === "idle") {
      return;
    }

    if (phase.current === "restoring") {
      if (status === "updating") {
        phase.current = "waiting";
      }

      return;
    }

    if (phase.current === "waiting") {
      if (status === "updating") {
        return;
      }

      phase.current = "ready";
    }

    if (
      phase.current === "ready" &&
      (status === "ready" || status === "error")
    ) {
      setSettled(true);
    }

    if (phase.current !== "ready" || status !== "ready") {
      return;
    }

    const store = browserStore();
    const scenario = scenarioFromReport(report);
    if (isSavedPlanScenario(scenario) && kind === "Rollover") {
      clearPlanWork(store);
      rememberPlanWork(null);
      return;
    }

    const work = { scenario, kind };
    writePlanWork(store, work);
    rememberPlanWork(work);
  }, [kind, mounted, report, status]);

  const setKind = useCallback((next: PayoffRolloverKind) => {
    setKindOverride(next);
  }, []);

  return {
    report,
    status,
    applyScenario,
    kind,
    setKind,
    settled,
  };
}

/**
 * Subscribes to nothing.
 * The mounted flag only distinguishes the server render from the browser.
 */
function subscribeToNothing() {
  return () => {};
}

/**
 * The browser session store, or null while the page is rendering on the server.
 */
function browserStore(): PlanWorkStore | null {
  if (typeof window === "undefined") {
    return null;
  }

  return window.sessionStorage;
}

let remembered: PlanWorkState | null | undefined;

/**
 * The last applied preview read once from this browser session.
 * Later writes update the same value so a return to Plan restores what was just applied.
 */
function clientWork(): PlanWorkState | null {
  if (remembered === undefined) {
    remembered = readPlanWork(browserStore());
  }

  return remembered;
}

/**
 * Remembers the preview that the next Plan visit should restore.
 */
function rememberPlanWork(work: PlanWorkState | null) {
  remembered = work;
}
