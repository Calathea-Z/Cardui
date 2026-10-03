import type { AccountDto } from "@/lib/api/types";
import { formatCurrency } from "./formatCurrency";

type AccountListRowProps = {
  account: AccountDto;
  compact?: boolean;
  showBalance?: boolean;
  onSelect?: (account: AccountDto) => void;
};

export function AccountListRow({
  account,
  compact = false,
  showBalance = true,
  onSelect,
}: AccountListRowProps) {
  const subtitle = [
    account.subtype ?? account.type,
    account.mask ? `···${account.mask}` : null,
  ]
    .filter(Boolean)
    .join(" · ");

  const className = compact
    ? "flex w-full items-center justify-between gap-3 px-3 py-2.5 text-left transition hover:bg-accent/30"
    : "flex w-full items-center justify-between gap-4 px-4 py-3.5 text-left transition hover:bg-accent/30";

  const content = (
    <>
      <div className="min-w-0">
        <p className="truncate font-medium text-foreground">{account.name}</p>
        <p className="mt-0.5 truncate text-sm text-muted-foreground">
          {subtitle}
        </p>
        {showBalance && account.availableBalance !== null ? (
          <p className="mt-1 hidden text-xs text-muted-foreground/80 sm:block">
            Available {formatCurrency(account.availableBalance)}
          </p>
        ) : null}
      </div>

      {showBalance ? (
        <div className="shrink-0 text-right">
          <p className="ledger-amount text-foreground">
            {formatCurrency(account.currentBalance)}
          </p>
          {!account.isActive ? (
            <p className="mt-1 text-xs text-muted-foreground">Inactive</p>
          ) : null}
        </div>
      ) : !account.isActive ? (
        <p className="shrink-0 text-xs text-muted-foreground">Inactive</p>
      ) : null}
    </>
  );

  if (!onSelect) {
    return <div className={className}>{content}</div>;
  }

  return (
    <button
      type="button"
      className={`${className} cursor-pointer`}
      onClick={() => onSelect(account)}
    >
      {content}
    </button>
  );
}
