import type { AccountSummaryDto } from "@/lib/api";
import { AccountsBalanceChart } from "./AccountsBalanceChart";

type AccountsViewProps = {
  summary: AccountSummaryDto;
};

function formatCurrency(value: number | null) {
  if (value === null) {
    return "-";
  }

  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(value);
}

export function AccountsView({ summary }: AccountsViewProps) {
  const accounts =
    summary.groups.find((group) => group.key === "net-worth")?.accounts ?? [];
  const detailGroups = summary.groups.filter((group) => group.key !== "net-worth");

  return (
    <section className="flex flex-col gap-6">
      <div>
        <p className="text-sm text-slate-400">Balances</p>
        <h1 className="text-3xl font-semibold">Accounts</h1>
        <p className="mt-1 text-sm text-slate-500">
          Net worth {formatCurrency(summary.netWorth)}
        </p>
      </div>

      <AccountsBalanceChart history={summary.history} />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {detailGroups.map((group) => (
          <article
            key={group.key}
            className="rounded-lg border border-slate-800 bg-slate-900 p-4"
          >
            <p className="text-sm text-slate-400">{group.name}</p>
            <p className="mt-2 text-2xl font-semibold">
              {formatCurrency(group.total)}
            </p>
            <p className="mt-1 text-xs text-slate-500">
              {group.accounts.length}{" "}
              {group.accounts.length === 1 ? "account" : "accounts"}
            </p>
          </article>
        ))}
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        {accounts.map((account) => (
          <article
            key={account.id}
            className="rounded-lg border border-slate-800 bg-slate-900 p-5"
          >
            <div className="flex items-start justify-between gap-4">
              <div>
                <h2 className="font-semibold">{account.name}</h2>
                <p className="mt-1 text-sm text-slate-400">
                  {account.subtype ?? account.type}
                  {account.mask ? ` - ${account.mask}` : ""}
                </p>
              </div>

              <span className="rounded-full border border-slate-700 px-2 py-1 text-xs text-slate-300">
                {account.isActive ? "Active" : "Inactive"}
              </span>
            </div>

            <div className="mt-6 grid gap-4 sm:grid-cols-2">
              <div>
                <p className="text-sm text-slate-400">Current balance</p>
                <p className="mt-1 text-2xl font-semibold">
                  {formatCurrency(account.currentBalance)}
                </p>
              </div>

              <div>
                <p className="text-sm text-slate-400">Available</p>
                <p className="mt-1 text-2xl font-semibold">
                  {formatCurrency(account.availableBalance)}
                </p>
              </div>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
