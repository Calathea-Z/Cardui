import type { AccountSummaryDto, DashboardSummaryDto } from "@/lib/api/types";
import { dashboardWidgets } from "./dashboard-widgets";

type DashboardViewProps = {
  dashboardSummary: DashboardSummaryDto;
  accountsSummary: AccountSummaryDto;
};

export function DashboardView({
  dashboardSummary,
  accountsSummary,
}: DashboardViewProps) {
  const widgetProps = { dashboardSummary, accountsSummary };

  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      {dashboardWidgets.map((widget) => (
        <div key={widget.id}>{widget.render(widgetProps)}</div>
      ))}
    </section>
  );
}
