"use client";

import { useRef, useState } from "react";
import { PageHeader } from "@/components/navigation/page-header";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { BillForm } from "./BillForm";
import { BillList } from "./BillList";
import type { BillsPageData } from "./billPageData";
import { useBills } from "./useBills";

type BillsPageClientProps = BillsPageData;

/**
 * Bills page.
 * Saved bills stay on the page. Adding or editing one opens a panel.
 */
export function BillsPageClient({
  obligations,
  accounts,
  planningCurrency,
}: BillsPageClientProps) {
  const bills = useBills(obligations);
  const addButtonRef = useRef<HTMLButtonElement>(null);
  const openerRef = useRef<HTMLElement | null>(null);
  const [openPickerCount, setOpenPickerCount] = useState(0);
  const editing = bills.obligations.find(
    (obligation) => obligation.id === bills.editingId,
  );

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
   * A control that has left the page falls back to Add bill.
   */
  function closeForm() {
    bills.closeForm();
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
        title="Bills"
        actions={
          <Button
            ref={addButtonRef}
            type="button"
            className="min-h-11 w-full sm:w-auto"
            onClick={(event) =>
              openForm(event.currentTarget, bills.startAdding)
            }
          >
            Add bill
          </Button>
        }
      />

      <BillList
        obligations={bills.obligations}
        busyId={bills.busyId}
        onAdd={(opener) => openForm(opener, bills.startAdding)}
        onEdit={(obligation, opener) =>
          openForm(opener, () => bills.startEditing(obligation))
        }
        onRemove={(obligation) => void bills.remove(obligation)}
      />

      <BottomSheet
        open={bills.isFormOpen}
        onClose={closeForm}
        title={bills.editingId ? "Edit bill" : "Add bill"}
        presentation="panel"
        headerAction="panel"
        closeOnEscape={openPickerCount === 0}
      >
        <BillForm
          key={bills.editingId ?? "new"}
          form={bills.form}
          accounts={accounts}
          savedAccountName={editing?.accountName ?? null}
          planningCurrency={planningCurrency}
          storedCurrency={editing?.currency ?? null}
          isEditing={bills.editingId !== null}
          isSaving={bills.isSaving}
          onChange={bills.setForm}
          onPickerOpenChange={handlePickerOpenChange}
          onSubmit={async (event) => {
            const saved = await bills.handleSubmit(event);
            if (saved) {
              closeForm();
            }
          }}
        />
      </BottomSheet>
    </div>
  );
}
