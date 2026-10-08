"use client";

import { Button } from "@/components/ui/button";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { formatCalendarDate } from "@/features/debts/debtDisplay";
import type { SavingsGoalDto, SavingsGoalKind } from "@/lib/api/types";
import { followNote, goalProgress, readyByShort } from "./savingsCopy";

type SavingsGoalSectionsProps = {
  goals: SavingsGoalDto[];
  busyId: string | null;
  onEdit: (goal: SavingsGoalDto, opener: HTMLElement) => void;
  onAdd: (kind: SavingsGoalKind, opener: HTMLElement) => void;
  onRemove: (goal: SavingsGoalDto) => void;
};

/**
 * Everyday spending, cash to keep, the emergency goal, and the named goals under Saving for.
 * The single cards stay on the page even when they have not been set.
 */
export function SavingsGoalSections({
  goals,
  busyId,
  onEdit,
  onAdd,
  onRemove,
}: SavingsGoalSectionsProps) {
  const operating = goals.find((goal) => goal.kind === "Operating") ?? null;
  const floor = goals.find((goal) => goal.kind === "Floor") ?? null;
  const emergency = goals.find((goal) => goal.kind === "Emergency") ?? null;
  const funds = goals
    .filter((goal) => goal.kind === "Sinking")
    .sort((left, right) => left.name.localeCompare(right.name));

  return (
    <div className="flex flex-col gap-6">
      <GoalSlot
        title="Everyday spending"
        empty="Nothing set yet. Choose how much you spend on everyday needs each month."
        goal={operating}
        busy={operating ? busyId === operating.id : false}
        onAdd={(opener) => onAdd("Operating", opener)}
        onEdit={onEdit}
        onRemove={onRemove}
      />
      <GoalSlot
        title="Cash to keep"
        empty="Nothing set yet. Choose how much to always keep available."
        goal={floor}
        busy={floor ? busyId === floor.id : false}
        onAdd={(opener) => onAdd("Floor", opener)}
        onEdit={onEdit}
        onRemove={onRemove}
      />
      <GoalSlot
        title="Emergency"
        empty="No emergency goal yet. Set a target and a date for the fund."
        goal={emergency}
        busy={emergency ? busyId === emergency.id : false}
        onAdd={(opener) => onAdd("Emergency", opener)}
        onEdit={onEdit}
        onRemove={onRemove}
      />
      <section className="flex flex-col gap-3">
        <h2 className="app-section-title">Saving for</h2>
        {funds.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            Nothing yet. Save for a cost that has a date, such as insurance or
            a trip.
          </p>
        ) : (
          <ul className="flex flex-col gap-3">
            {funds.map((goal) => (
              <GoalRow
                key={goal.id}
                goal={goal}
                busy={busyId === goal.id}
                onEdit={(opener) => onEdit(goal, opener)}
                onRemove={() => onRemove(goal)}
              />
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

type GoalSlotProps = {
  title: string;
  empty: string;
  goal: SavingsGoalDto | null;
  busy: boolean;
  onAdd: (opener: HTMLElement) => void;
  onEdit: (goal: SavingsGoalDto, opener: HTMLElement) => void;
  onRemove: (goal: SavingsGoalDto) => void;
};

/**
 * One household-level goal.
 * An empty slot offers Set. A saved goal can be edited or removed.
 */
function GoalSlot({
  title,
  empty,
  goal,
  busy,
  onAdd,
  onEdit,
  onRemove,
}: GoalSlotProps) {
  return (
    <section className="flex flex-col gap-3">
      <h2 className="app-section-title">{title}</h2>
      {goal ? (
        <ul className="flex flex-col gap-3">
          <GoalRow
            goal={goal}
            busy={busy}
            onEdit={(opener) => onEdit(goal, opener)}
            onRemove={() => onRemove(goal)}
          />
        </ul>
      ) : (
        <div className="flex flex-col gap-3 rounded-lg border border-border/70 p-3 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-muted-foreground">{empty}</p>
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            onClick={(event) => onAdd(event.currentTarget)}
          >
            Set
          </Button>
        </div>
      )}
    </section>
  );
}

type GoalRowProps = {
  goal: SavingsGoalDto;
  busy: boolean;
  onEdit: (opener: HTMLElement) => void;
  onRemove: () => void;
};

/**
 * One saved goal.
 * The amount shown is the amount the plan protects, and the sentence is the calculated monthly amount.
 */
function GoalRow({ goal, busy, onEdit, onRemove }: GoalRowProps) {
  const note = followNote(goal);
  return (
    <li className="flex flex-col gap-3 rounded-lg border border-border/70 p-3">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate font-medium">{goal.name}</p>
          <p className="mt-0.5 text-sm text-muted-foreground">
            {rowDetail(goal)}
          </p>
        </div>
        <div className="shrink-0 text-right">
          <p className="text-foreground tabular-nums">
            {formatCurrency(goal.amountInUse, goal.currency)}
          </p>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {goal.kind === "Operating" || goal.kind === "Floor"
              ? "available now"
              : "set aside"}
          </p>
        </div>
      </div>
      <p className="text-sm text-foreground">{rowSentence(goal)}</p>
      {note ? <p className="text-sm text-muted-foreground">{note}</p> : null}
      <div className="flex gap-2">
        <Button
          type="button"
          variant="outline"
          className="min-h-11"
          onClick={(event) => onEdit(event.currentTarget)}
        >
          Edit
        </Button>
        <Button
          type="button"
          variant="ghost"
          className="min-h-11"
          disabled={busy}
          onClick={onRemove}
        >
          Remove
        </Button>
      </div>
    </li>
  );
}

/**
 * The line under a row's name.
 * Everyday spending names the monthly amount and the day it counts. Cash to keep names the floor. A finishing goal names its target and date.
 */
function rowDetail(goal: SavingsGoalDto): string {
  if (goal.kind === "Operating" && goal.monthlyAmount !== null && goal.readyDay !== null) {
    return `${formatCurrency(goal.monthlyAmount, goal.currency)} a month, ready by ${readyByShort(goal.readyDay)}.`;
  }

  if (goal.kind === "Floor" && goal.floorAmount !== null) {
    return `Keep ${formatCurrency(goal.floorAmount, goal.currency)} available.`;
  }

  if (goal.targetAmount === null || goal.targetDate === null) {
    return "";
  }

  return `Target ${formatCurrency(goal.targetAmount, goal.currency)} by ${formatCalendarDate(goal.targetDate)}`;
}

/**
 * The sentence under a saved row.
 * Cash to keep names the gap. A finishing goal uses its calculated monthly amount. Everyday spending has already said its plan.
 */
function rowSentence(goal: SavingsGoalDto): string {
  if (goal.kind === "Operating") {
    return "This leaves cash on that day. It is not money set aside.";
  }

  if (goal.kind === "Floor") {
    return goal.alreadyMet
      ? "This amount is available."
      : `${formatCurrency(goal.remaining, goal.currency)} still to set aside.`;
  }

  return goalProgress(goal, (amount) => formatCurrency(amount, goal.currency));
}
