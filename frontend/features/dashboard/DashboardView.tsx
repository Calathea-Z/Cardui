import type {
  AccountSummaryDto,
  CategoryDto,
  DashboardSummaryDto,
  GroupDto,
  SubGroupDto,
} from "@/lib/api/types";
import { dashboardWidgets } from "./dashboard-widgets";

type DashboardViewProps = {
  dashboardSummary: DashboardSummaryDto;
  accountsSummary: AccountSummaryDto;
  categories: CategoryDto[];
  groups: GroupDto[];
  subGroups: SubGroupDto[];
};

export function DashboardView({
  dashboardSummary,
  accountsSummary,
  categories,
  groups,
  subGroups,
}: DashboardViewProps) {
  const widgetProps = {
    dashboardSummary,
    accountsSummary,
    categories,
    groups,
    subGroups,
  };

  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      {dashboardWidgets.map((widget) => (
        <div key={widget.id}>{widget.render(widgetProps)}</div>
      ))}
    </section>
  );
}
