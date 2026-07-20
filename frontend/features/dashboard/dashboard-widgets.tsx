import type { AccountSummaryDto, DashboardSummaryDto } from "@/lib/api/types";
import { DashboardAccountsSlider } from "./DashboardAccountsSlider";
import { DashboardRecentTransactionsWidget } from "./DashboardRecentTransactionsWidget";

export type DashboardWidgetProps = {
  dashboardSummary: DashboardSummaryDto;
  accountsSummary: AccountSummaryDto;
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
    render: ({ dashboardSummary }) => (
      <DashboardRecentTransactionsWidget
        transactions={dashboardSummary.recentTransactions}
      />
    ),
  },
];
