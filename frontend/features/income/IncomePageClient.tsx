"use client";

import { PageHeader } from "@/components/navigation/page-header";
import { Alert } from "@/components/ui/alert";
import type { IncomePageData } from "./incomePageData";
import { IncomeSourceForm } from "./IncomeSourceForm";
import { IncomeSourceList } from "./IncomeSourceList";
import { useIncomeSources } from "./useIncomeSources";

type IncomePageClientProps = IncomePageData;

/**
 * Income page.
 * The household records each source as one take-home payment, a cadence, the next date, a contributor, and reliability.
 */
export function IncomePageClient({
  sources,
  contributors,
  planningCurrency,
}: IncomePageClientProps) {
  const income = useIncomeSources(sources, contributors);
  const editingSource = income.sources.find(
    (source) => source.id === income.editingId,
  );

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        eyebrow="Income"
        title="Income sources"
        description="Record each place money comes in. Enter net pay for one payment, after taxes and deductions, plus how often it arrives, the next date, who it belongs to, and how reliable it is."
      />

      <IncomeSourceForm
        form={income.form}
        contributors={income.contributors}
        planningCurrency={planningCurrency}
        storedCurrency={editingSource?.currency ?? null}
        isEditing={income.editingId !== null}
        isSaving={income.isSaving}
        onChange={income.setForm}
        onSubmit={(event) => void income.handleSubmit(event)}
        onCancel={income.cancelEditing}
      />

      {income.error ? (
        <Alert variant="destructive">{income.error}</Alert>
      ) : null}

      <IncomeSourceList
        sources={income.sources}
        busyId={income.busyId}
        onEdit={income.startEditing}
        onRemove={(source) => void income.remove(source)}
      />
    </div>
  );
}
