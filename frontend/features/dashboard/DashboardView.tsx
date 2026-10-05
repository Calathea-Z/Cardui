import { PageHeader } from "@/components/navigation/page-header";
import { CurrencyExclusionNotice } from "@/features/household/CurrencyExclusionNotice";
import { DashboardMonthlyActivityWidget } from "./DashboardMonthlyActivityWidget";
import { DashboardNetWorthCard } from "./DashboardNetWorthCard";
import { DashboardRecentTransactionsWidget } from "./DashboardRecentTransactionsWidget";
import type { DashboardPageData } from "./server/loadDashboardPage";

/**
 * Home: net worth and this month side by side from lg, recent activity under both.
 * The currency notice sits above them and stays hidden when nothing is excluded from the planning currency.
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
      <div className="grid gap-6 lg:grid-cols-2">
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
