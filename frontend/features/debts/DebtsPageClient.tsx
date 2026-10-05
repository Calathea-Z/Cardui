"use client";

import { useRef, useState } from "react";
import { PageHeader } from "@/components/navigation/page-header";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { DebtForm } from "./DebtForm";
import { DebtList } from "./DebtList";
import type { DebtsPageData } from "./debtPageData";
import { useDebts } from "./useDebts";

type DebtsPageClientProps = DebtsPageData;

/**
 * Debts page.
 * Saved debts stay on the page. Adding or editing one opens a panel.
 * A blank term stays unknown.
 */
export function DebtsPageClient({
  debts: initialDebts,
  accounts,
  planningCurrency,
}: DebtsPageClientProps) {
  const debts = useDebts(initialDebts);
  const addButtonRef = useRef<HTMLButtonElement>(null);
  const openerRef = useRef<HTMLElement | null>(null);
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

      <DebtList
        debts={debts.debts}
        busyId={debts.busyId}
        onAdd={(opener) => openForm(opener, debts.startAdding)}
        onEdit={(debt, opener) =>
          openForm(opener, () => debts.startEditing(debt))
        }
        onRemove={(debt) => void debts.remove(debt)}
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
    </div>
  );
}
