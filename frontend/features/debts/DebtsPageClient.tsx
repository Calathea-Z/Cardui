"use client";

import { useRef, useState } from "react";
import { PageHeader } from "@/components/navigation/page-header";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { DebtFollowSheet } from "./DebtFollowSheet";
import { DebtForm } from "./DebtForm";
import { DebtSummary } from "./DebtSummary";
import { DebtList } from "./DebtList";
import type { DebtDto } from "@/lib/api/types";
import type { DebtsPageData } from "./debtPageData";
import { useDebts } from "./useDebts";

type DebtsPageClientProps = DebtsPageData;

/**
 * Debts page.
 * Saved debts stay on the page. Summary sits above them when there is something to inventory.
 * Adding or editing one opens a panel. A blank term stays unknown.
 */
export function DebtsPageClient({
  debts: initialDebts,
  summary: initialSummary,
  accounts,
  planningCurrency,
}: DebtsPageClientProps) {
  const debts = useDebts(initialDebts, initialSummary);
  const addButtonRef = useRef<HTMLButtonElement>(null);
  const openerRef = useRef<HTMLElement | null>(null);
  const followOpenerRef = useRef<HTMLElement | null>(null);
  const [openPickerCount, setOpenPickerCount] = useState(0);
  const editing = debts.debts.find((debt) => debt.id === debts.editingId);

  /**
   * Remembers the control that opened the form, then runs that open action.
   * Closing the panel returns focus there when the control is still on the page.
   */
  function openForm(opener: HTMLElement, open: () => void) {
    openerRef.current = opener;
    open();
  }

  /**
   * Closes the form and returns focus to the control that opened it.
   * A control that has left the page falls back to Add debt.
   */
  function closeForm() {
    debts.closeForm();
    const opener = openerRef.current;
    openerRef.current = null;
    setOpenPickerCount(0);
    if (opener?.isConnected) {
      opener.focus();
      return;
    }

    addButtonRef.current?.focus();
  }

  /**
   * Remembers the follow button, then opens the follow steps.
   * Closing those steps returns focus to that button.
   */
  function openFollow(debt: DebtDto, opener: HTMLElement) {
    followOpenerRef.current = opener;
    debts.startFollowing(debt);
  }

  /**
   * Closes the follow steps and returns focus to the button that opened them.
   */
  function closeFollow() {
    debts.closeFollowing();
    const opener = followOpenerRef.current;
    followOpenerRef.current = null;
    if (opener?.isConnected) {
      opener.focus();
    }
  }

  /**
   * Refreshes the bank behind a stale followed balance.
   * An account with no bank link has no refresh.
   */
  function refreshFollowed(debt: DebtDto) {
    const account = accounts.find((item) => item.id === debt.accountId);
    if (account?.plaidItemId) {
      void debts.refreshConnection(debt, account.plaidItemId);
    }
  }

  /**
   * Tracks how many choice lists or calendars are open inside the form.
   * Escape closes the panel only when none of those are open.
   */
  function handlePickerOpenChange(open: boolean) {
    setOpenPickerCount((count) => Math.max(0, count + (open ? 1 : -1)));
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Debts"
        actions={
          <Button
            ref={addButtonRef}
            type="button"
            className="min-h-11 w-full sm:w-auto"
            onClick={(event) =>
              openForm(event.currentTarget, debts.startAdding)
            }
          >
            Add debt
          </Button>
        }
      />

      {debts.debts.length > 0 ? (
        <DebtSummary
          report={debts.summary}
          debts={debts.debts}
          updating={debts.summaryUpdating}
        />
      ) : null}

      <DebtList
        debts={debts.debts}
        accounts={accounts}
        summary={debts.summary}
        busyId={debts.busyId}
        onAdd={(opener) => openForm(opener, debts.startAdding)}
        onEdit={(debt, opener) =>
          openForm(opener, () => debts.startEditing(debt))
        }
        onRemove={(debt) => void debts.remove(debt)}
        onUseAccountBalance={(debt, startsFollow) =>
          void debts.chooseBalance(debt, startsFollow)
        }
        onFollow={openFollow}
        onStopFollowing={(debt) => void debts.stopFollowing(debt)}
        onRefresh={refreshFollowed}
      />

      <BottomSheet
        open={debts.isFormOpen}
        onClose={closeForm}
        title={debts.editingId ? "Edit debt" : "Add debt"}
        presentation="panel"
        headerAction="panel"
        closeOnEscape={openPickerCount === 0}
      >
        <DebtForm
          key={debts.editingId ?? "new"}
          form={debts.form}
          accounts={accounts}
          savedAccountName={editing?.accountName ?? null}
          planningCurrency={planningCurrency}
          storedCurrency={editing?.currency ?? null}
          isEditing={debts.editingId !== null}
          isSaving={debts.isSaving}
          following={editing?.following ?? false}
          onChange={debts.setForm}
          onPickerOpenChange={handlePickerOpenChange}
          onSubmit={async (event) => {
            const saved = await debts.handleSubmit(event);
            if (saved) {
              closeForm();
            }
          }}
        />
      </BottomSheet>

      <DebtFollowSheet
        debt={
          debts.debts.find((debt) => debt.id === debts.followDebtId) ?? null
        }
        accounts={debts.followAccounts}
        loading={debts.followLoading}
        error={debts.followError}
        busy={debts.busyId !== null}
        onClose={closeFollow}
        onRetry={debts.retryFollowing}
        onFollow={(accountId, keepOwnBalance) =>
          void debts.follow(accountId, keepOwnBalance)
        }
      />
    </div>
  );
}
