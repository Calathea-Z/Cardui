import type { AccountDto } from "@/lib/api";
import { formatCurrency } from "./formatCurrency";

type AccountListRowProps = {
  account: AccountDto;
};

export function AccountListRow({ account }: AccountListRowProps) {
  const subtitle = [
    account.subtype ?? account.type,
    account.mask ? `···${account.mask}` : null,
  ]
    .filter(Boolean)
    .join(" · ");

  return (
    <div className="flex items-center justify-between gap-4 px-4 py-3.5 transition hover:bg-accent/30">
      <div className="min-w-0">
        <p className="truncate font-medium text-foreground">{account.name}</p>
        <p className="mt-0.5 truncate text-sm text-muted-foreground">
          {subtitle}
        </p>
        {account.availableBalance !== null ? (
          <p className="mt-1 hidden text-xs text-muted-foreground/80 sm:block">
            Available {formatCurrency(account.availableBalance)}
          </p>
        ) : null}
      </div>

      <div className="shrink-0 text-right">
        <p className="font-semibold tabular-nums text-foreground">
          {formatCurrency(account.currentBalance)}
        </p>
        {!account.isActive ? (
          <p className="mt-1 text-xs text-muted-foreground">Inactive</p>
        ) : null}
      </div>
    </div>
  );
}
