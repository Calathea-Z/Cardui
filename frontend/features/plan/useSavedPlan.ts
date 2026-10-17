"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  acceptPlanVersion,
  getPlanDecision,
  getPlanVersions,
  savePlanVersion,
} from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type {
  DecisionPlanDto,
  PayoffRolloverKind,
  PlanScenarioQuery,
  PlanVersionDto,
} from "@/lib/api/types";
import {
  decisionPlanChrome,
  decisionPreviewIdentity,
  isCalculatedDecisionPreview,
} from "./decisionPlanView";

/**
 * Loads the decision preview and saved versions for Your plan.
 * `preview` can still be the previous scenario until `previewReady` is true. Save waits for the preview that matches this scenario, including after a failed refresh. Saving and accepting do not move money.
 */
export function useSavedPlan(
  scenario: PlanScenarioQuery,
  kind: PayoffRolloverKind,
  enabled: boolean,
  matchesCurrentFacts: boolean,
  allowSave: boolean,
) {
  const [preview, setPreview] = useState<DecisionPlanDto | null>(null);
  const [versions, setVersions] = useState<PlanVersionDto[]>([]);
  const [viewedId, setViewedId] = useState<string | null>(null);
  const [loadStatus, setLoadStatus] = useState<
    "idle" | "loading" | "ready" | "error"
  >("idle");
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [messageTone, setMessageTone] = useState<"success" | "error" | null>(
    null,
  );
  const request = useRef(0);
  const scenarioKey = JSON.stringify(scenario);
  const identity = decisionPreviewIdentity(kind, scenarioKey);
  const [readyIdentity, setReadyIdentity] = useState<string | null>(null);
  // A new scenario or path drops the previous calculation before paint, so save cannot use it.
  if (readyIdentity !== null && readyIdentity !== identity) {
    setReadyIdentity(null);
  }

  const load = useCallback(
    async (clearError = false) => {
      const id = ++request.current;
      const current = JSON.parse(scenarioKey) as PlanScenarioQuery;
      setLoadStatus((status) => (status === "ready" ? "ready" : "loading"));
      try {
        const [nextPreview, nextVersions] = await Promise.all([
          getPlanDecision(current, kind),
          getPlanVersions(),
        ]);
        if (request.current !== id) {
          return;
        }

        setPreview(nextPreview);
        setVersions(nextVersions);
        setReadyIdentity(identity);
        setLoadStatus("ready");
        if (clearError) {
          setMessage(null);
          setMessageTone(null);
        }
      } catch (error) {
        if (request.current !== id) {
          return;
        }

        setReadyIdentity(null);
        setLoadStatus("error");
        setMessage(
          getApiErrorMessage(error, "This plan could not be prepared."),
        );
        setMessageTone("error");
      }
    },
    [identity, kind, scenarioKey],
  );

  useEffect(() => {
    if (!enabled) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      void load(true);
    }, 0);

    return () => window.clearTimeout(timeoutId);
  }, [enabled, load]);

  const chrome = useMemo(
    () => decisionPlanChrome(preview, versions, viewedId, matchesCurrentFacts),
    [matchesCurrentFacts, preview, versions, viewedId],
  );

  const previewReady = isCalculatedDecisionPreview(readyIdentity, identity);

  /**
   * Stores the current preview as the open draft.
   * A preview that has not loaded, or whose refresh failed, is not saved. A stale draft asks the person to review the plan again.
   */
  async function save() {
    if (!allowSave || !previewReady) {
      return;
    }

    setSaving(true);
    setMessage(null);
    try {
      const current = JSON.parse(scenarioKey) as PlanScenarioQuery;
      await savePlanVersion({
        kind,
        monthlyExtra: current.monthlyExtra,
        incomeLossMonths: current.incomeLossMonths,
        windfallAmount: current.windfallAmount,
        windfallOn: current.windfallOn,
        livingSpendingMonthly: current.livingSpendingMonthly,
        cashFloor: current.cashFloor,
        priority: current.priority,
        constraintDebtId: current.constraintDebtId,
        constraintKind: current.constraintKind,
        reclaimAmount: current.reclaimAmount,
        replacesDraftId: chrome.replacesDraftId,
      });
      setViewedId(null);
      setMessage("Draft saved. Cardui did not move money.");
      setMessageTone("success");
      await load(false);
    } catch (error) {
      setMessage(getApiErrorMessage(error, "This plan could not be saved."));
      setMessageTone("error");
      await load(false);
    } finally {
      setSaving(false);
    }
  }

  /**
   * Accepts the open draft and leaves the previous accepted plan in history.
   */
  async function accept() {
    if (!chrome.acceptId) {
      return;
    }

    setSaving(true);
    setMessage(null);
    try {
      await acceptPlanVersion(chrome.acceptId);
      setViewedId(null);
      setMessage("Plan accepted. Cardui did not move money.");
      setMessageTone("success");
      await load(false);
    } catch (error) {
      setMessage(getApiErrorMessage(error, "This plan could not be accepted."));
      setMessageTone("error");
      await load(false);
    } finally {
      setSaving(false);
    }
  }

  return {
    chrome,
    preview,
    previewReady,
    loadStatus,
    saving,
    message,
    messageTone,
    viewedId,
    versions,
    save,
    accept,
    retry: () => load(true),
    viewVersion: setViewedId,
  };
}
