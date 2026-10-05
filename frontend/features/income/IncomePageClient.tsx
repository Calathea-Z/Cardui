"use client";

import { useRef, useState } from "react";
import { PageHeader } from "@/components/navigation/page-header";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import type { IncomePageData } from "./incomePageData";
import { IncomeSourceForm } from "./IncomeSourceForm";
import { IncomeSourceList } from "./IncomeSourceList";
import { useIncomeSources } from "./useIncomeSources";

type IncomePageClientProps = IncomePageData;

/**
 * Income page.
 * Saved sources stay on the page. Adding or editing one opens a panel.
 */
export function IncomePageClient({
  sources,
  contributors,
  planningCurrency,
  timeZoneId,
}: IncomePageClientProps) {
  const income = useIncomeSources(sources, contributors, timeZoneId);
  const addButtonRef = useRef<HTMLButtonElement>(null);
  const openerRef = useRef<HTMLElement | null>(null);
  const [openPickerCount, setOpenPickerCount] = useState(0);
  const editingSource = income.sources.find(
    (source) => source.id === income.editingId,
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
   * A control that has left the page falls back to Add income.
   */
  function closeForm() {
    income.closeForm();
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
        title="Income"
        actions={
          <Button
            ref={addButtonRef}
            type="button"
            className="min-h-11 w-full sm:w-auto"
            onClick={(event) =>
              openForm(event.currentTarget, income.startAdding)
            }
          >
            Add income
          </Button>
        }
      />

      <IncomeSourceList
        sources={income.sources}
        today={income.today}
        busyId={income.busyId}
        onAdd={(opener) => openForm(opener, income.startAdding)}
        onEdit={(source, opener) =>
          openForm(opener, () => income.startEditing(source))
        }
        onRemove={(source) => void income.remove(source)}
        onConfirmRaise={(source, raise) =>
          void income.confirmRaise(source, raise)
        }
        onRemoveRaise={(source, raise) =>
          void income.removeRaise(source, raise)
        }
      />

      <BottomSheet
        open={income.isFormOpen}
        onClose={closeForm}
        title={income.editingId ? "Edit income" : "Add income"}
        presentation="panel"
        headerAction="panel"
        closeOnEscape={openPickerCount === 0}
      >
        <IncomeSourceForm
          key={income.editingId ?? "new"}
          form={income.form}
          contributors={income.contributors}
          planningCurrency={planningCurrency}
          storedCurrency={editingSource?.currency ?? null}
          isEditing={income.editingId !== null}
          isSaving={income.isSaving}
          onChange={income.setForm}
          onPickerOpenChange={handlePickerOpenChange}
          onSubmit={async (event) => {
            const saved = await income.handleSubmit(event);
            if (saved) {
              closeForm();
            }
          }}
        />
      </BottomSheet>
    </div>
  );
}
