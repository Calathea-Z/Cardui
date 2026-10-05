import { PageHeader } from "@/components/navigation/page-header";
import { CurrencyExclusionNotice } from "@/features/household/CurrencyExclusionNotice";
import { DashboardMonthlyActivityWidget } from "./DashboardMonthlyActivityWidget";
import { DashboardNetWorthCard } from "./DashboardNetWorthCard";
import { DashboardRecentTransactionsWidget } from "./DashboardRecentTransactionsWidget";
import type { DashboardPageData } from "./server/loadDashboardPage";

/**
 * Home: net worth and this month stack until the page is wide enough for two cards.
 * Recent activity sits under both. The currency notice stays hidden when nothing is excluded from the planning currency.
 */
export function DashboardView({
  dashboardSummary,
  accountsSummary,
  categories,
  groups,
  subGroups,
}: DashboardPageData) {
  return (
    <section className="flex w-full flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
      <PageHeader title="Home" />
      <CurrencyExclusionNotice
        exclusion={{
          planningCurrency: dashboardSummary.planningCurrency,
          excludedAccountCount: dashboardSummary.excludedAccountCount,
          excludedTransactionCount: dashboardSummary.excludedTransactionCount,
          excludedCurrencies: dashboardSummary.excludedCurrencies,
        }}
      />
      <div className="grid gap-6 xl:grid-cols-2">
        <DashboardNetWorthCard summary={accountsSummary} />
        <DashboardMonthlyActivityWidget summary={dashboardSummary} />
      </div>
      <DashboardRecentTransactionsWidget
        transactions={dashboardSummary.recentTransactions}
        categories={categories}
        groups={groups}
        subGroups={subGroups}
      />
    </section>
  );
}
