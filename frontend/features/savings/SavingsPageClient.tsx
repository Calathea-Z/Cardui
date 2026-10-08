"use client";

import { useRef, useState } from "react";
import Link from "next/link";
import { TriangleAlert } from "lucide-react";
import { PageHeader } from "@/components/navigation/page-header";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { SavingsGoalForm } from "./SavingsGoalForm";
import { SavingsGoalSections } from "./SavingsGoalSections";
import type { SavingsPageData } from "./savingsPageData";
import { useSavingsGoals } from "./useSavingsGoals";

/**
 * Savings page.
 * Cash to keep and the emergency goal are single cards. Saving for is the named list.
 * Flexible monthly spending is on Plan budget.
 * Saving a goal does not move money.
 */
export function SavingsPageClient({
  goals,
  accounts,
  planningCurrency,
  planBudgetShortfall,
}: SavingsPageData) {
  const savings = useSavingsGoals(goals, accounts);
  const addButtonRef = useRef<HTMLButtonElement>(null);
  const openerRef = useRef<HTMLElement | null>(null);
  const [openPickerCount, setOpenPickerCount] = useState(0);
  const editing = savings.goals.find((goal) => goal.id === savings.editingId);
  const hasFloor = savings.goals.some((goal) => goal.kind === "Floor");
  const hasEmergency = savings.goals.some((goal) => goal.kind === "Emergency");
  const primaryKind = !hasFloor
    ? "Floor"
    : !hasEmergency
      ? "Emergency"
      : "Sinking";
  const primaryLabel =
    primaryKind === "Floor"
      ? "Set cash to keep"
      : primaryKind === "Emergency"
        ? "Set emergency goal"
        : "Save for something";

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
   * A control that has left the page falls back to the current foundation action.
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
    : savings.form.kind === "Floor"
      ? "Set cash to keep"
      : savings.form.kind === "Emergency"
        ? "Set emergency goal"
        : "Save for something";

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Savings"
        description="Protect a cash floor, then add emergency or named goals. These amounts stay in cash; saving a goal does not move money."
        actions={
          <Button
            ref={addButtonRef}
            type="button"
            className="min-h-11 w-full sm:w-auto"
            onClick={(event) =>
              openForm(event.currentTarget, () =>
                savings.startAdding(primaryKind),
              )
            }
          >
            {primaryLabel}
          </Button>
        }
      />
      <div className="app-panel p-4 text-sm text-muted-foreground">
        <p>
          Cash to keep is the always-available floor. Emergency and named goals
          are added to that floor; together they reduce what Plan treats as
          available without spending or moving the cash.
        </p>
        <p className="mt-2">
          Flexible monthly spending belongs on{" "}
          <Link href="/living" className="text-primary underline">
            Plan budget
          </Link>
          .{" "}
          <Link href="/targets" className="text-primary underline">
            Spending targets
          </Link>{" "}
          track posted Activity and do not add another amount to Plan.
        </p>
      </div>
      {planBudgetShortfall !== null && planBudgetShortfall > 0 ? (
        <p className="flex items-start gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2 text-sm font-medium text-warning">
          <TriangleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
          <span>
            <span className="sr-only">Warning: </span>
            Plan budget is already short{" "}
            {formatCurrency(planBudgetShortfall, planningCurrency)} a month.
            Savings protections add to that constraint.{" "}
            <Link href="/plan" className="underline">
              Review dated cash on Plan
            </Link>
            .
          </span>
        </p>
      ) : null}
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
