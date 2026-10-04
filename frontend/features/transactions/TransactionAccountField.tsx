type TransactionAccountFieldProps = {
  accountName: string;
};

/**
 * Shows which account holds the transaction.
 * The name is read-only.
 */
export function TransactionAccountField({
  accountName,
}: TransactionAccountFieldProps) {
  return (
    <div className="flex min-h-12 items-center gap-3">
      <span className="shrink-0 text-sm font-medium text-foreground">
        Account
      </span>
      <span className="min-w-0 flex-1 truncate text-right text-sm text-muted-foreground">
        {accountName}
      </span>
    </div>
  );
}
