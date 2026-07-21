type TransactionOriginalStatementFieldProps = {
  statement: string;
};

export function TransactionOriginalStatementField({
  statement,
}: TransactionOriginalStatementFieldProps) {
  return (
    <div className="flex min-h-12 items-start gap-3 py-3">
      <span className="shrink-0 pt-0.5 text-sm font-medium text-foreground">
        Original Statement
      </span>
      <span className="min-w-0 flex-1 wrap-break-word text-right text-sm leading-5 text-muted-foreground whitespace-pre-wrap">
        {statement}
      </span>
    </div>
  );
}
