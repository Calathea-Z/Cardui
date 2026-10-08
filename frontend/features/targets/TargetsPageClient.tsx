"use client";

import { useRef } from "react";
import { PageHeader } from "@/components/navigation/page-header";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { Select } from "@/components/ui/select";
import {
  copyForwardNote,
  monthChoices,
  monthKey,
  parseMonthKey,
} from "./categoryTargetCopy";
import { TargetForm } from "./TargetForm";
import { TargetList } from "./TargetList";
import { TargetSummary } from "./TargetSummary";
import type { TargetsPageData } from "./targetPageData";
import { useCategoryTargets } from "./useCategoryTargets";

type TargetsPageClientProps = TargetsPageData;

/**
 * Spending targets page.
 * One month is on screen. A preview from an earlier month is saved only when it is used, edited, or started fresh.
 */
export function TargetsPageClient({
  month: initialMonth,
}: TargetsPageClientProps) {
  const targets = useCategoryTargets(initialMonth);
  const openerRef = useRef<HTMLElement | null>(null);
  const month = targets.month;
  const note = month ? copyForwardNote(month) : null;

  /**
   * Remembers the control that opened the form.
   */
  function openForm(opener: HTMLElement, lineId: string) {
    const line = month?.categories.find((item) => item.categoryId === lineId);
    if (!line) {
      return;
    }

    openerRef.current = opener;
    targets.startEditing(line);
  }

  /**
   * Closes the form and returns focus to the control that opened it.
   */
  function closeForm() {
    targets.closeForm();
    const opener = openerRef.current;
    openerRef.current = null;
    if (opener?.isConnected) {
      opener.focus();
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Spending targets"
        description="Monthly targets compare categorized Activity with your intentions. They do not add another spending amount to Plan."
        actions={
          month ? (
            <Select
              title="Month"
              className="min-h-11 sm:min-w-44"
              value={monthKey(month.year, month.month)}
              options={monthChoices(month.todayYear, month.todayMonth)}
              disabled={targets.isUpdating || targets.isSaving}
              onChange={(value) => {
                const next = parseMonthKey(value);
                if (!next) {
                  return;
                }

                void targets.loadMonth(next.year, next.month);
              }}
            />
          ) : null
        }
      />

      {month ? (
        <>
          <TargetSummary month={month} updating={targets.isUpdating} />
          {note ? (
            <section className="app-panel flex flex-col gap-3 p-4">
              <p className="text-sm text-foreground">{note}</p>
              <div className="flex flex-col gap-2 sm:flex-row">
                <Button
                  type="button"
                  className="min-h-11"
                  disabled={targets.isUpdating}
                  onClick={() => void targets.useCopiedTargets()}
                >
                  Use these targets
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="min-h-11"
                  disabled={targets.isUpdating}
                  onClick={() => void targets.startFresh()}
                >
                  Start fresh
                </Button>
              </div>
            </section>
          ) : null}
          <TargetList
            year={month.year}
            month={month.month}
            currency={month.planningCurrency}
            lines={month.categories}
            busy={targets.isUpdating || targets.isSaving}
            onEdit={(line, opener) => {
              if (line.categoryId) {
                openForm(opener, line.categoryId);
              }
            }}
          />
        </>
      ) : (
        <EmptyState
          title="Targets could not be loaded"
          description="Refresh the page to try again."
        />
      )}

      <BottomSheet
        open={targets.isFormOpen}
        onClose={closeForm}
        title={
          targets.editing ? `${targets.editing.name} target` : "Category target"
        }
        presentation="panel"
        headerAction="panel"
        closeOnEscape
      >
        {targets.editing && month ? (
          <TargetForm
            key={targets.editing.categoryId ?? "category"}
            categoryName={targets.editing.name}
            currency={month.planningCurrency}
            monthSaved={month.saved}
            hasTarget={targets.editing.target !== null}
            form={targets.form}
            isSaving={targets.isSaving}
            error={targets.formError}
            copiesOthers={month.copiedFromYear !== null}
            onChange={targets.setForm}
            onSubmit={async (event) => {
              const saved = await targets.save(event);
              if (saved) {
                closeForm();
              }
            }}
            onClear={() => void targets.clear()}
          />
        ) : null}
      </BottomSheet>
    </div>
  );
}
