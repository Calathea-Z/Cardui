"use client";

import { useRef, useState } from "react";
import Link from "next/link";
import { TriangleAlert } from "lucide-react";
import { PageHeader } from "@/components/navigation/page-header";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { SavingsGoalForm } from "@/features/savings/SavingsGoalForm";
import { readyByShort } from "@/features/savings/savingsCopy";
import type { LivingPageDto } from "@/lib/api/types";
import { contributionNote, gapSentence } from "./livingCopy";
import { useLivingPage } from "./useLivingPage";

type LivingPageClientProps = {
  page: LivingPageDto;
};

/**
 * Living page.
 * Contribution shares limit the pay Plan may use, while one monthly amount covers flexible living spending.
 */
export function LivingPageClient({ page: initial }: LivingPageClientProps) {
  const living = useLivingPage(initial);
  const openerRef = useRef<HTMLElement | null>(null);
  const [openPickerCount, setOpenPickerCount] = useState(0);
  const page = living.page;
  const money = (amount: number) =>
    formatCurrency(amount, page.planningCurrency);
  const gap = gapSentence(page.gap, money);

  /**
   * Opens the monthly-spending panel and remembers where focus should return.
   */
  function openLivingSpending(opener: HTMLElement) {
    openerRef.current = opener;
    living.openLivingSpending();
  }

  /**
   * Closes the monthly-spending panel and returns focus to its opener.
   */
  function closeLivingSpending() {
    living.closeLivingSpending();
    setOpenPickerCount(0);
    const opener = openerRef.current;
    openerRef.current = null;
    if (opener?.isConnected) {
      opener.focus();
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Living"
        description="Choose how much pay enters the shared plan and one monthly amount for flexible living spending."
      />

      <section className="flex flex-col gap-3">
        <h2 className="app-section-title">Pay available to the plan</h2>
        <p className="text-sm text-muted-foreground">
          Enter the monthly amount at today&apos;s pay. The plan applies that
          same share to low pay and future raises. Blank uses all recorded pay;
          zero shares none. Paychecks stay on their dates.
        </p>
        {page.contributions.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No one is listed yet, so the plan uses all recorded pay.{" "}
            <Link href="/household" className="text-primary underline">
              Add a person on Household
            </Link>{" "}
            when people share different portions.
          </p>
        ) : (
          <ul className="flex flex-col gap-3">
            {page.contributions.map((person) => (
              <li
                key={person.contributorId}
                className="flex flex-col gap-3 rounded-lg border border-border/70 p-3"
              >
                <label className="flex flex-col gap-1.5 text-sm font-medium">
                  {person.name}
                  <Input
                    inputMode="decimal"
                    value={living.amounts[person.contributorId] ?? ""}
                    onChange={(event) =>
                      living.setAmount(person.contributorId, event.target.value)
                    }
                    autoComplete="off"
                  />
                  <span className="font-normal text-muted-foreground">
                    Current monthly amount available to the shared plan.
                    {person.recordedMonthly !== null
                      ? ` Recorded pay averages ${money(person.recordedMonthly)} a month.`
                      : ""}
                  </span>
                </label>
                <p className="text-sm text-foreground">
                  {contributionNote(person, money)}
                </p>
                <Button
                  type="button"
                  variant="outline"
                  className="min-h-11 w-full sm:w-28"
                  disabled={living.busyId === person.contributorId}
                  onClick={() =>
                    void living.saveContribution(person.contributorId)
                  }
                >
                  {living.busyId === person.contributorId ? "Saving…" : "Save"}
                </Button>
              </li>
            ))}
          </ul>
        )}
        {page.unassignedIncome.length > 0 ? (
          <p className="text-sm text-muted-foreground">
            Pay with no person stays fully shared:{" "}
            {page.unassignedIncome.join(", ")}.
          </p>
        ) : null}
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="app-section-title">Monthly living spending</h2>
        <p className="text-sm text-muted-foreground">
          One amount for groceries, gas, hobbies, and other flexible spending
          not already entered on Bills. A dated trip or other goal stays on{" "}
          <Link href="/savings" className="text-primary underline">
            Savings
          </Link>
          . Category{" "}
          <Link href="/targets" className="text-primary underline">
            Targets
          </Link>{" "}
          track actual spending and do not add another amount to Plan.
        </p>
        {page.livingSpending ? (
          <div className="flex flex-col gap-3 rounded-lg border border-border/70 p-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <p className="text-sm text-foreground">
                {money(page.livingSpending.monthlyAmount ?? 0)} a month, ready
                by {readyByShort(page.livingSpending.readyDay ?? 1)}.
              </p>
              <p className="mt-0.5 text-sm text-muted-foreground">
                This leaves forecast cash on that day. Saving it does not move
                real money.
              </p>
            </div>
            <Button
              type="button"
              variant="outline"
              className="min-h-11"
              onClick={(event) => openLivingSpending(event.currentTarget)}
            >
              Edit
            </Button>
          </div>
        ) : (
          <div className="flex flex-col gap-3 rounded-lg border border-border/70 p-3 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm text-muted-foreground">
              No monthly living-spending amount is included in Plan yet.
            </p>
            <Button
              type="button"
              className="min-h-11"
              onClick={(event) => openLivingSpending(event.currentTarget)}
            >
              Set monthly amount
            </Button>
          </div>
        )}
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="app-section-title">Monthly affordability</h2>
        {gap?.warning ? (
          <p className="flex items-start gap-2 rounded-md bg-warning/10 px-3 py-2 text-sm font-medium text-warning">
            <TriangleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
            <span>
              <span className="sr-only">Warning: </span>
              {gap.text}
            </span>
          </p>
        ) : gap ? (
          <p className="text-sm text-foreground">{gap.text}</p>
        ) : (
          <p className="text-sm text-muted-foreground">
            Add income, bills, debts, or living spending to see the monthly
            average.
          </p>
        )}
        {page.gap.leftOut.length > 0 ? (
          <ul className="flex flex-col gap-1 text-sm text-muted-foreground">
            {page.gap.leftOut.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        ) : null}
        <p className="text-sm text-muted-foreground">
          This is an average, not cash on a date.{" "}
          <Link href="/plan" className="text-primary underline">
            Review the dated cash outlook on Plan.
          </Link>
        </p>
      </section>

      <BottomSheet
        open={living.livingSpendingOpen}
        onClose={closeLivingSpending}
        title={
          page.livingSpending
            ? "Edit monthly living spending"
            : "Set monthly living spending"
        }
        presentation="panel"
        headerAction="panel"
        closeOnEscape={openPickerCount === 0}
      >
        <SavingsGoalForm
          key={page.livingSpending?.id ?? "living-spending"}
          form={living.livingSpendingForm}
          accounts={page.accounts}
          savedAccountName={page.livingSpending?.accountName ?? null}
          planningCurrency={page.planningCurrency}
          isEditing={page.livingSpending !== null}
          isSaving={living.isSaving}
          onChange={living.setLivingSpendingForm}
          onPickerOpenChange={(open) =>
            setOpenPickerCount((count) => Math.max(0, count + (open ? 1 : -1)))
          }
          onSubmit={async (event) => {
            const saved = await living.saveLivingSpending(event);
            if (saved) {
              closeLivingSpending();
            }
          }}
        />
      </BottomSheet>
    </div>
  );
}
