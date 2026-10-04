import type {
  AccountSummaryDto,
  CategoryDto,
  DashboardSummaryDto,
  GroupDto,
  SubGroupDto,
} from "@/lib/api/types";
import { DashboardAccountsSlider } from "./DashboardAccountsSlider";
import { DashboardMonthlyActivityWidget } from "./DashboardMonthlyActivityWidget";
import { DashboardRecentTransactionsWidget } from "./DashboardRecentTransactionsWidget";

/**
 * Page data passed into every dashboard widget.
 * Each widget reads the summaries, categories, and groups it displays from this same set.
 */
export type DashboardWidgetProps = {
  dashboardSummary: DashboardSummaryDto;
  accountsSummary: AccountSummaryDto;
  categories: CategoryDto[];
  groups: GroupDto[];
  subGroups: SubGroupDto[];
};

/**
 * One dashboard block and the function that renders it.
 * The render function receives the shared page data.
 */
export type DashboardWidgetDefinition = {
  id: string;
  render: (props: DashboardWidgetProps) => React.ReactNode;
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
