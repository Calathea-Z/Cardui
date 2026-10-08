"use client";

import { useRef, useState } from "react";
import { PageHeader } from "@/components/navigation/page-header";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import type { SavingsAccountDto, SavingsGoalDto } from "@/lib/api/types";
import { SavingsGoalForm } from "./SavingsGoalForm";
import { SavingsGoalSections } from "./SavingsGoalSections";
import { useSavingsGoals } from "./useSavingsGoals";

type SavingsPageClientProps = {
  goals: SavingsGoalDto[];
  accounts: SavingsAccountDto[];
  planningCurrency: string;
};

/**
 * Savings page.
 * Everyday spending and cash to keep are single cards. Saving for is the named list.
 * Saving a goal does not move money.
 */
export function SavingsPageClient({
  goals,
  accounts,
  planningCurrency,
}: SavingsPageClientProps) {
  const savings = useSavingsGoals(goals, accounts);
  const addButtonRef = useRef<HTMLButtonElement>(null);
  const openerRef = useRef<HTMLElement | null>(null);
  const [openPickerCount, setOpenPickerCount] = useState(0);
  const editing = savings.goals.find((goal) => goal.id === savings.editingId);

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
   * A control that has left the page falls back to Save for something.
   */
  function closeForm() {
    savings.closeForm();
    const opener = openerRef.current;
    openerRef.current = null;
    setOpenPickerCount(0);
    if (opener?.isConnected) {
      opener.focus();
      return;
    }

    addButtonRef.current?.focus();
  }

  const title = savings.editingId
    ? `Edit ${editing?.name ?? "goal"}`
    : savings.form.kind === "Operating"
      ? "Set everyday spending"
      : savings.form.kind === "Floor"
        ? "Set cash to keep"
        : savings.form.kind === "Emergency"
        ? "Set emergency goal"
        : "Save for something";

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Savings"
        description="Everyday spending, cash to keep, and goals with a date. Saving does not move money."
        actions={
          <Button
            ref={addButtonRef}
            type="button"
            className="min-h-11 w-full sm:w-auto"
            onClick={(event) =>
              openForm(event.currentTarget, () =>
                savings.startAdding("Sinking"),
              )
            }
          >
            Save for something
          </Button>
        }
      />
      <SavingsGoalSections
        goals={savings.goals}
        busyId={savings.busyId}
        onAdd={(kind, opener) =>
          openForm(opener, () => savings.startAdding(kind))
        }
        onEdit={(goal, opener) =>
          openForm(opener, () => savings.startEditing(goal))
        }
        onRemove={(goal) => void savings.remove(goal)}
      />
      <BottomSheet
        open={savings.isFormOpen}
        onClose={closeForm}
        title={title}
        presentation="panel"
        headerAction="panel"
        closeOnEscape={openPickerCount === 0}
      >
        <SavingsGoalForm
          key={savings.editingId ?? savings.form.kind}
          form={savings.form}
          accounts={accounts}
          savedAccountName={editing?.accountName ?? null}
          planningCurrency={planningCurrency}
          isEditing={savings.editingId !== null}
          isSaving={savings.isSaving}
          onChange={savings.setForm}
          onPickerOpenChange={(open) =>
            setOpenPickerCount((count) => Math.max(0, count + (open ? 1 : -1)))
          }
          onSubmit={async (event) => {
            const saved = await savings.handleSubmit(event);
            if (saved) {
              closeForm();
            }
          }}
        />
      </BottomSheet>
    </div>
  );
}
