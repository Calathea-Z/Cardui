"use client";

import { useRef, useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@/components/ui/confirm-dialog";
import {
  chooseAccountBalance,
  clearDebtOverride,
  createDebt,
  deleteDebt,
  followDebtAccount,
  getDebtFollowAccounts,
  getDebtSummary,
  getDebts,
  setDebtOverride,
  stopFollowingDebt,
  syncPlaidItem,
  updateDebt,
} from "@/lib/api/browser";
import { describeApiError } from "@/lib/api/errors";
import type {
  DebtDto,
  DebtFollowAccountDto,
  DebtSummaryReportDto,
} from "@/lib/api/types";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { formatCalendarDate } from "./debtDisplay";
import {
  followedAccountLabel,
  ownBalanceToast,
  startedFollowingToast,
  stoppedFollowingToast,
  syncedBalanceToast,
} from "./debtFollowCopy";
import {
  debtToForm,
  emptyDebtForm,
  toBalanceOverride,
  toDebtUpsert,
  toTodayBalanceOverride,
  type DebtFormState,
} from "./debtFormState";

/**
 * Holds the debt list, its summary, and the create or edit form.
 * A blank term stays unknown. Choosing an eligible account balance starts following it.
 * A manual account is copied once. Stopping a follow copies that balance back onto the debt.
 */
export function useDebts(
  initialDebts: DebtDto[],
  initialSummary: DebtSummaryReportDto | null,
) {
  const [debts, setDebts] = useState(initialDebts);
  const [summary, setSummary] = useState(initialSummary);
  const [summaryUpdating, setSummaryUpdating] = useState(false);
  const [form, setForm] = useState<DebtFormState>(emptyDebtForm());
  const [editingId, setEditingId] = useState<string | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [followDebtId, setFollowDebtId] = useState<string | null>(null);
  const [followAccounts, setFollowAccounts] = useState<DebtFollowAccountDto[]>(
    [],
  );
  const [followLoading, setFollowLoading] = useState(false);
  const [followError, setFollowError] = useState<string | null>(null);
  const summaryRequest = useRef(0);
  const followRequest = useRef(0);
  const confirm = useConfirm();

  /**
   * Creates a debt or saves the one being edited.
   * Returns true when the debt was saved. The name and type are required. A blank term stays unknown.
   */
  async function handleSubmit(
    event: React.FormEvent<HTMLFormElement>,
  ): Promise<boolean> {
    event.preventDefault();
    const payload = toDebtUpsert(form);
    if (!payload.ok) {
      showDebtError(payload.error);
      return false;
    }

    toast.dismiss(debtToastId);
    setIsSaving(true);

    try {
      if (editingId) {
        const updated = await updateDebt(editingId, payload.dto);
        setDebts((current) =>
          current.map((debt) => (debt.id === updated.id ? updated : debt)),
        );
      } else {
        const created = await createDebt(payload.dto);
        setDebts((current) => [...current, created]);
      }

      void refreshSummary();
      return true;
    } catch (err) {
      reportDebtFailure(err, "That debt could not be saved.");
      return false;
    } finally {
      setIsSaving(false);
    }
  }

  /**
   * Opens a blank form for a new debt.
   */
  function startAdding() {
    toast.dismiss(debtToastId);
    setEditingId(null);
    setForm(emptyDebtForm());
    setIsFormOpen(true);
  }

  /**
   * Opens the form with one saved debt.
   */
  function startEditing(debt: DebtDto) {
    toast.dismiss(debtToastId);
    setEditingId(debt.id);
    setForm(debtToForm(debt));
    setIsFormOpen(true);
  }

  /**
   * Closes the form.
   * The next open replaces whatever was left in the fields.
   */
  function closeForm() {
    toast.dismiss(debtToastId);
    setIsFormOpen(false);
  }

  /**
   * Deletes a debt after the user confirms.
   * The linked account and its balance stay.
   */
  async function remove(debt: DebtDto) {
    const confirmed = await confirm({
      title: `Remove ${debt.name}?`,
      description:
        "This deletes the debt. The linked account and its balance stay.",
      confirmLabel: "Remove",
    });
    if (!confirmed) {
      return;
    }

    setBusyId(debt.id);
    try {
      await deleteDebt(debt.id);
      setDebts((current) => current.filter((item) => item.id !== debt.id));
      if (editingId === debt.id) {
        closeForm();
      }

      void refreshSummary();
    } catch (err) {
      reportDebtFailure(err, "That debt could not be removed.");
    } finally {
      setBusyId(null);
    }
  }

  /**
   * Uses the linked account balance after confirmation.
   * An eligible account starts following. Any other account is copied onto the debt once.
   */
  async function chooseBalance(debt: DebtDto, startsFollow: boolean) {
    const comparison = summary?.debts.find(
      (item) => item.debtId === debt.id,
    )?.balanceComparison;
    if (!comparison?.canUseAccountBalance) {
      return;
    }

    const accountName = debt.accountName ?? "this account";
    const confirmed = await confirm(
      startsFollow
        ? {
            title: `Follow ${accountName} for ${debt.name}?`,
            description:
              "The plan will use this account's balance from now on. APR, minimum, and due date stay yours. The account itself is not changed.",
            confirmLabel: "Follow",
            destructive: false,
          }
        : {
            title: `Use the account balance for ${debt.name}?`,
            description:
              debt.balance === null
                ? "The debt balance stays unknown until you do. This saves the account balance and its date on the debt. The account itself is not changed."
                : "The plan keeps the recorded balance until you do. This saves the account balance and its date on the debt. The account itself is not changed.",
            confirmLabel: "Use the account balance",
            destructive: false,
          },
    );
    if (!confirmed) {
      return;
    }

    setBusyId(debt.id);
    try {
      const updated = await chooseAccountBalance(debt.id);
      setDebts((current) =>
        current.map((item) => (item.id === updated.id ? updated : item)),
      );
      const refreshed = await refreshSummary();
      if (refreshed) {
        toast.success(
          updated.following
            ? startedFollowingToast(
                updated.name,
                followedAccountLabel(updated.accountName ?? accountName, null),
              )
            : "The recorded balance now matches the account.",
          { id: debtToastId },
        );
      }
    } catch (err) {
      reportDebtFailure(err, "That account balance could not be saved.");
    } finally {
      setBusyId(null);
    }
  }

  /**
   * Opens the follow steps for one debt and loads the accounts it may follow.
   */
  function startFollowing(debt: DebtDto) {
    setFollowDebtId(debt.id);
    setFollowAccounts([]);
    void loadFollowAccounts(debt.id);
  }

  /**
   * Closes the follow steps. A load that is still running is ignored.
   */
  function closeFollowing() {
    followRequest.current += 1;
    setFollowDebtId(null);
    setFollowLoading(false);
    setFollowError(null);
  }

  /**
   * Loads the accounts one debt may follow.
   * A later open or close drops this result.
   */
  async function loadFollowAccounts(debtId: string) {
    const request = followRequest.current + 1;
    followRequest.current = request;
    setFollowLoading(true);
    setFollowError(null);
    try {
      const accounts = await getDebtFollowAccounts(debtId);
      if (followRequest.current === request) {
        setFollowAccounts(accounts);
      }
    } catch (err) {
      if (followRequest.current === request) {
        const text = describeApiError(
          err,
          "Those accounts could not be loaded.",
        );
        setFollowError(text.message);
      }
    } finally {
      if (followRequest.current === request) {
        setFollowLoading(false);
      }
    }
  }

  /**
   * Makes the open debt follow the chosen account.
   * Keep-own records a different balance as the person's value. The toast names the account.
   */
  async function follow(accountId: string, keepOwnBalance: boolean) {
    const debt = debts.find((item) => item.id === followDebtId);
    if (!debt) {
      return;
    }

    const account = followAccounts.find((item) => item.accountId === accountId);
    setBusyId(debt.id);
    try {
      const updated = await followDebtAccount(debt.id, {
        accountId,
        keepOwnBalance,
      });
      setDebts((current) =>
        current.map((item) => (item.id === updated.id ? updated : item)),
      );
      closeFollowing();
      toast.success(
        startedFollowingToast(
          updated.name,
          followedAccountLabel(
            account?.name ?? updated.accountName ?? "the account",
            account?.mask ?? null,
          ),
        ),
        { id: debtToastId },
      );
      void refreshSummary();
    } catch (err) {
      reportDebtFailure(err, "That account could not be followed.");
    } finally {
      setBusyId(null);
    }
  }

  /**
   * Stops following and keeps the last balance.
   * The toast names the amount that was kept when there is one.
   */
  async function stopFollowing(debt: DebtDto) {
    setBusyId(debt.id);
    try {
      const updated = await stopFollowingDebt(debt.id);
      setDebts((current) =>
        current.map((item) => (item.id === updated.id ? updated : item)),
      );
      const balance =
        updated.balance === null
          ? null
          : formatCurrency(updated.balance, updated.currency);
      const asOf = updated.balanceAsOf
        ? formatCalendarDate(updated.balanceAsOf)
        : null;
      toast.success(stoppedFollowingToast(updated.name, balance, asOf), {
        id: debtToastId,
      });
      void refreshSummary();
    } catch (err) {
      reportDebtFailure(err, "Following could not be stopped.");
    } finally {
      setBusyId(null);
    }
  }

  /**
   * Keeps the open debt's balance as the person's value.
   * The other terms in the form stay. A blank amount or date is rejected here.
   */
  async function saveOwnBalance(balance: string, balanceAsOf: string) {
    if (!editingId) {
      return false;
    }

    const parsed = toBalanceOverride(balance, balanceAsOf);
    if (!parsed.ok) {
      showDebtError(parsed.error);
      return false;
    }

    setIsSaving(true);
    try {
      const updated = await setDebtOverride(editingId, "Balance", parsed.dto);
      rememberDebt(updated);
      toast.success(ownBalanceText(updated), { id: debtToastId });
      void refreshSummary();
      return true;
    } catch (err) {
      reportDebtFailure(err, "That balance could not be saved.");
      return false;
    } finally {
      setIsSaving(false);
    }
  }

  /**
   * Sets a balance override dated today on a card whose connection is not current.
   * A blank amount is rejected here. The date is chosen by the server.
   */
  async function updateBalance(debt: DebtDto, amount: string) {
    const parsed = toTodayBalanceOverride(amount);
    if (!parsed.ok) {
      showDebtError(parsed.error);
      return false;
    }

    setBusyId(debt.id);
    try {
      const updated = await setDebtOverride(debt.id, "Balance", parsed.dto);
      rememberDebt(updated);
      toast.success(ownBalanceText(updated), { id: debtToastId });
      void refreshSummary();
      return true;
    } catch (err) {
      reportDebtFailure(err, "That balance could not be saved.");
      return false;
    } finally {
      setBusyId(null);
    }
  }

  /**
   * Clears the person's balance so the debt uses the synced balance again.
   */
  async function useSyncedBalance(debt: DebtDto) {
    setBusyId(debt.id);
    try {
      const updated = await clearDebtOverride(debt.id, "Balance");
      rememberDebt(updated);
      toast.success(syncedBalanceToast(updated.name), { id: debtToastId });
      void refreshSummary();
    } catch (err) {
      reportDebtFailure(err, "The synced balance could not be used.");
    } finally {
      setBusyId(null);
    }
  }

  /**
   * Pulls the bank behind a stale followed balance, then reloads the debts.
   * A sync that is already running does not change the balance.
   */
  async function refreshConnection(debt: DebtDto, plaidItemId: string) {
    setBusyId(debt.id);
    try {
      const result = await syncPlaidItem(plaidItemId);
      if (result.alreadyRunning) {
        toast.warning("Already syncing", {
          id: debtToastId,
          description: "This refresh did nothing.",
        });
        return;
      }

      const next = await getDebts();
      setDebts(next);
      toast.success("Refresh finished", { id: debtToastId });
      void refreshSummary();
    } catch (err) {
      reportDebtFailure(err, "That refresh failed. Try again.");
    } finally {
      setBusyId(null);
    }
  }

  /**
   * Reloads summary after a debt change.
   * The previous summary stays visible until the new one arrives. A failed reload leaves it in place.
   */
  async function refreshSummary() {
    const request = summaryRequest.current + 1;
    summaryRequest.current = request;
    setSummaryUpdating(true);
    try {
      const next = await getDebtSummary();
      if (summaryRequest.current === request) {
        setSummary(next);
      }

      return summaryRequest.current === request;
    } catch (err) {
      if (summaryRequest.current === request) {
        reportDebtFailure(err, "Debt summary could not be updated.");
      }

      return false;
    } finally {
      if (summaryRequest.current === request) {
        setSummaryUpdating(false);
      }
    }
  }

  /**
   * Replaces one debt in the list and, when that debt is open, its balance fields.
   * The other terms the person has typed stay in the form.
   */
  function rememberDebt(updated: DebtDto) {
    setDebts((current) =>
      current.map((item) => (item.id === updated.id ? updated : item)),
    );
    setForm((current) => {
      if (editingId !== updated.id) {
        return current;
      }

      return {
        ...current,
        balance:
          updated.balanceInUse === null ? "" : String(updated.balanceInUse),
        balanceAsOf: updated.balanceInUseAsOf
          ? updated.balanceInUseAsOf.slice(0, 10)
          : "",
      };
    });
  }

  /**
   * The toast for a saved balance override.
   * The amount and date are the ones now in use.
   */
  function ownBalanceText(updated: DebtDto) {
    const balance =
      updated.balanceInUse === null
        ? null
        : formatCurrency(updated.balanceInUse, updated.currency);
    const asOf = updated.balanceInUseAsOf
      ? formatCalendarDate(updated.balanceInUseAsOf)
      : null;
    return ownBalanceToast(updated.name, balance, asOf);
  }

  return {
    form,
    setForm,
    editingId,
    isFormOpen,
    debts: sortDebts(debts),
    summary,
    summaryUpdating,
    isSaving,
    busyId,
    handleSubmit,
    startAdding,
    startEditing,
    closeForm,
    remove,
    chooseBalance,
    followDebtId,
    followAccounts,
    followLoading,
    followError,
    startFollowing,
    closeFollowing,
    retryFollowing: () => {
      if (followDebtId) {
        void loadFollowAccounts(followDebtId);
      }
    },
    follow,
    stopFollowing,
    refreshConnection,
    saveOwnBalance,
    updateBalance,
    useSyncedBalance,
  };
}

const debtToastId = "debt-action";

/**
 * Shows a debt failure as a toast.
 * A 500 uses the sentence about the action, and in development the exception is the second line.
 * Validation text stays as the toast message.
 */
function reportDebtFailure(error: unknown, action: string) {
  const text = describeApiError(error, action);
  showDebtError(text.message, text.detail);
}

/**
 * Shows one debt error toast.
 * A later debt action replaces this toast.
 */
function showDebtError(message: string, detail?: string) {
  toast.error(message, {
    id: debtToastId,
    ...(detail ? { description: detail } : {}),
  });
}

/**
 * Orders debts by name.
 * The page uses this after a save so a new debt does not stay at the bottom.
 */
function sortDebts(debts: DebtDto[]) {
  return [...debts].sort((left, right) => left.name.localeCompare(right.name));
}
