import type { AccountSummaryDto } from "@/lib/api";
import { AccountsBalanceChart } from "./AccountsBalanceChart";
import { AccountsSectionList } from "./AccountsSectionList";
import { formatCurrency } from "./formatCurrency";

type AccountsViewProps = {
  summary: AccountSummaryDto;
  actions?: React.ReactNode;
};

export function AccountsView({ summary, actions }: AccountsViewProps) {
  return (
    <section className="flex flex-col gap-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="hidden text-sm text-muted-foreground md:block">Balances</p>
          <h1 className="hidden text-3xl font-semibold text-violet-50 md:block">
            Accounts
          </h1>
          <p className="text-sm text-muted-foreground md:mt-1 md:text-base">
            <span className="md:hidden">Net worth </span>
            <span className="text-lg font-semibold text-violet-100 md:text-sm md:font-normal md:text-muted-foreground">
              {formatCurrency(summary.netWorth)}
            </span>
          </p>
        </div>

        {actions ? (
          <div className="hidden shrink-0 items-center gap-1 md:flex">
            {actions}
          </div>
        ) : null}
      </div>

      <AccountsBalanceChart history={summary.history} />

      <AccountsSectionList groups={summary.groups} />
    </section>
  );
}
