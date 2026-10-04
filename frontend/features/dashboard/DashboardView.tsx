import { CurrencyExclusionNotice } from "@/features/household/CurrencyExclusionNotice";
import { dashboardWidgets } from "./dashboard-widgets";
import type { DashboardPageData } from "./server/loadDashboardPage";

/**
 * Renders the dashboard widgets in list order.
 * The currency notice sits above them and stays hidden when nothing is excluded from the planning currency.
 */
export function DashboardView({
  dashboardSummary,
  accountsSummary,
  categories,
  groups,
  subGroups,
}: DashboardPageData) {
  const widgetProps = {
    dashboardSummary,
    accountsSummary,
    categories,
    groups,
    subGroups,
  };

  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <CurrencyExclusionNotice
        exclusion={{
          planningCurrency: dashboardSummary.planningCurrency,
          excludedAccountCount: dashboardSummary.excludedAccountCount,
          excludedTransactionCount: dashboardSummary.excludedTransactionCount,
          excludedCurrencies: dashboardSummary.excludedCurrencies,
        }}
      />
      {dashboardWidgets.map((widget) => (
        <div key={widget.id}>{widget.render(widgetProps)}</div>
      ))}
    </section>
  );
}
