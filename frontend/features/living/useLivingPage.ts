"use client";

import { useState } from "react";
import { toast } from "sonner";
import { getLivingPage, setLivingContribution } from "@/lib/api/browser";
import {
  createSavingsGoal,
  updateSavingsGoal,
} from "@/lib/api/browser/savings";
import { describeApiError } from "@/lib/api/errors";
import type { LivingPageDto } from "@/lib/api/types";
import {
  emptySavingsForm,
  goalToForm,
  toSavingsUpsert,
  type SavingsFormState,
} from "@/features/savings/savingsFormState";
import { contributionAmount, contributionField } from "./livingCopy";

/**
 * Holds the Living page and its contribution and monthly-spending forms.
 * A contribution scales paychecks. Monthly living spending leaves forecast cash but does not move real money.
 */
export function useLivingPage(initial: LivingPageDto) {
  const [page, setPage] = useState(initial);
  const [amounts, setAmounts] = useState(() => contributionDrafts(initial));
  const [livingSpendingForm, setLivingSpendingForm] =
    useState<SavingsFormState>(emptySavingsForm("Operating"));
  const [livingSpendingOpen, setLivingSpendingOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);

  /**
   * Replaces the page after a save and refreshes the contribution fields.
   * A field the person has not saved yet is reset to the stored amount.
   */
  function replace(next: LivingPageDto) {
    setPage(next);
    setAmounts(contributionDrafts(next));
  }

  /**
   * Saves one person's current monthly contribution benchmark.
   * Blank clears it. The same derived share applies to low pay and future raises.
   */
  async function saveContribution(contributorId: string) {
    const parsed = contributionAmount(amounts[contributorId] ?? "");
    if (!parsed.ok) {
      showLivingError(parsed.error);
      return;
    }

    setBusyId(contributorId);
    try {
      replace(
        await setLivingContribution(contributorId, {
          monthlyAmount: parsed.amount,
        }),
      );
      toast.success("Contribution saved.", { id: livingToastId });
    } catch (error) {
      reportLivingFailure(error, "The contribution could not be saved.");
    } finally {
      setBusyId(null);
    }
  }

  /**
   * Opens monthly living spending.
   * An empty card starts the one Operating row; a saved card edits it.
   */
  function openLivingSpending() {
    setLivingSpendingForm(
      page.livingSpending
        ? goalToForm(page.livingSpending)
        : emptySavingsForm("Operating"),
    );
    setLivingSpendingOpen(true);
  }

  /**
   * Saves monthly living spending through the existing Operating-goal route, then reloads Living.
   * Returns true when it was stored.
   */
  async function saveLivingSpending(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const offered =
      !livingSpendingForm.accountId ||
      page.accounts.some(
        (account) =>
          account.id === livingSpendingForm.accountId &&
          (account.followedByGoalId === null ||
            account.followedByGoalId === page.livingSpending?.id),
      );
    const payload = toSavingsUpsert(livingSpendingForm, offered);
    if (!payload.ok) {
      showLivingError(payload.error);
      return false;
    }

    setIsSaving(true);
    try {
      const saved = page.livingSpending
        ? await updateSavingsGoal(page.livingSpending.id, payload.dto)
        : await createSavingsGoal(payload.dto);
      const reloaded = await getLivingPage().catch(() => null);
      replace(reloaded ?? { ...page, livingSpending: saved });
      toast.success("Monthly living spending saved.", { id: livingToastId });
      return true;
    } catch (error) {
      reportLivingFailure(error, "Monthly living spending could not be saved.");
      return false;
    } finally {
      setIsSaving(false);
    }
  }

  return {
    page,
    amounts,
    setAmount: (contributorId: string, amount: string) =>
      setAmounts((current) => ({ ...current, [contributorId]: amount })),
    busyId,
    isSaving,
    saveContribution,
    livingSpendingForm,
    setLivingSpendingForm,
    livingSpendingOpen,
    openLivingSpending,
    closeLivingSpending: () => setLivingSpendingOpen(false),
    saveLivingSpending,
  };
}

/**
 * One text field per contributor.
 * A null stored amount stays blank.
 */
function contributionDrafts(page: LivingPageDto) {
  return Object.fromEntries(
    page.contributions.map((person) => [
      person.contributorId,
      contributionField(person.monthlyAmount),
    ]),
  );
}

const livingToastId = "living-action";

/**
 * Shows a living failure as a toast.
 * A server failure uses the action sentence; validation text stays as returned.
 */
function reportLivingFailure(error: unknown, action: string) {
  const text = describeApiError(error, action);
  toast.error(text.message, {
    id: livingToastId,
    ...(text.detail ? { description: text.detail } : {}),
  });
}

/**
 * Shows one living validation error.
 * A later living action replaces this toast.
 */
function showLivingError(message: string) {
  toast.error(message, { id: livingToastId });
}
