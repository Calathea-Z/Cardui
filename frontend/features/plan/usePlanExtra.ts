"use client";

import { useCallback, useRef, useState } from "react";
import { getPlanRecovery } from "@/lib/api/browser";
import type { PlanRecoveryDto } from "@/lib/api/types";

/**
 * Whether the tried extra matches the plan on screen.
 * Updating and error keep the previous plan visible.
 */
export type PlanExtraStatus = "ready" | "updating" | "error";

/**
 * Holds the plan on screen and applies a tried extra by reloading it.
 * Zero restores the plan loaded with the page. A positive amount requests that extra and does not save it.
 * A newer request replaces an older one still in flight.
 */
export function usePlanExtra(baseline: PlanRecoveryDto) {
  const [report, setReport] = useState(baseline);
  const [status, setStatus] = useState<PlanExtraStatus>("ready");
  const [requestedAmount, setRequestedAmount] = useState<number | null>(null);
  const request = useRef(0);

  const applyExtra = useCallback(
    async (amount: number) => {
      const id = ++request.current;
      setRequestedAmount(amount);
      if (amount <= 0) {
        setReport(baseline);
        setStatus("ready");
        setRequestedAmount(null);
        return;
      }

      setStatus("updating");
      try {
        const next = await getPlanRecovery(amount);
        if (request.current !== id) {
          return;
        }

        setReport(next);
        setStatus("ready");
        setRequestedAmount(null);
      } catch {
        if (request.current !== id) {
          return;
        }

        setStatus("error");
      }
    },
    [baseline],
  );

  return { report, status, requestedAmount, applyExtra };
}
