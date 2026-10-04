import { DashboardAccountsSlider } from "./DashboardAccountsSlider";
import { DashboardMonthlyActivityWidget } from "./DashboardMonthlyActivityWidget";
import { DashboardRecentTransactionsWidget } from "./DashboardRecentTransactionsWidget";
import type { DashboardPageData } from "./server/loadDashboardPage";

/**
 * One dashboard block and the function that renders it.
 * The render function receives the same page data the route loaded.
 */
export type DashboardWidgetDefinition = {
  id: string;
  render: (props: DashboardPageData) => React.ReactNode;
};

/**
 * Dashboard blocks in on-screen order.
 * The accounts card is first, then monthly activity, then recent transactions.
 */
export const dashboardWidgets: DashboardWidgetDefinition[] = [
  {
    id: "accounts-slider",
    render: ({ accountsSummary }) => (
      <DashboardAccountsSlider summary={accountsSummary} />
    ),
  },
  {
    id: "monthly-activity",
    render: ({ dashboardSummary }) => (
      <DashboardMonthlyActivityWidget summary={dashboardSummary} />
    ),
  },
  {
    id: "recent-transactions",
    render: ({ dashboardSummary, categories, groups, subGroups }) => (
      <DashboardRecentTransactionsWidget
        transactions={dashboardSummary.recentTransactions}
        categories={categories}
        groups={groups}
        subGroups={subGroups}
      />
    ),
  },
];
