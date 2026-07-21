import type {
  AccountSummaryDto,
  CategoryDto,
  DashboardSummaryDto,
  GroupDto,
  SubGroupDto,
} from "@/lib/api/types";
import { DashboardAccountsSlider } from "./DashboardAccountsSlider";
import { DashboardRecentTransactionsWidget } from "./DashboardRecentTransactionsWidget";

export type DashboardWidgetProps = {
  dashboardSummary: DashboardSummaryDto;
  accountsSummary: AccountSummaryDto;
  categories: CategoryDto[];
  groups: GroupDto[];
  subGroups: SubGroupDto[];
};

export type DashboardWidgetDefinition = {
  id: string;
  render: (props: DashboardWidgetProps) => React.ReactNode;
};

export const dashboardWidgets: DashboardWidgetDefinition[] = [
  {
    id: "accounts-slider",
    render: ({ accountsSummary }) => (
      <DashboardAccountsSlider summary={accountsSummary} />
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
