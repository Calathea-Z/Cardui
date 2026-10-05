"use client";

import { PageHeader } from "@/components/navigation/page-header";
import type { IncomePageData } from "./incomePageData";
import { IncomeSourceForm } from "./IncomeSourceForm";
import { IncomeSourceList } from "./IncomeSourceList";
import { useIncomeSources } from "./useIncomeSources";

type IncomePageClientProps = IncomePageData;

/**
 * Income page.
 * The household records each source as a typical take-home payment, optional low and strong amounts, and any expected raises.
 */
export function IncomePageClient({
  sources,
  contributors,
  planningCurrency,
  timeZoneId,
}: IncomePageClientProps) {
  const income = useIncomeSources(sources, contributors, timeZoneId);
  const editingSource = income.sources.find(
    (source) => source.id === income.editingId,
  );

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Income sources"
        description="Record each place money comes in. Typical net pay is one payment, after taxes and deductions. Low and strong are optional. An expected raise names the new typical amount from a later date. When that date arrives, confirm the pay or remove the raise."
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

      <IncomeSourceList
        sources={income.sources}
        today={income.today}
        busyId={income.busyId}
        onEdit={income.startEditing}
        onRemove={(source) => void income.remove(source)}
        onConfirmRaise={(source, raise) =>
          void income.confirmRaise(source, raise)
        }
        onRemoveRaise={(source, raise) =>
          void income.removeRaise(source, raise)
        }
      />
    </div>
  );
}
