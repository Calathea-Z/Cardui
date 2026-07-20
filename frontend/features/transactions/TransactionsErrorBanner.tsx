type TransactionsErrorBannerProps = {
  message: string;
};

export function TransactionsErrorBanner({
  message,
}: TransactionsErrorBannerProps) {
  return (
    <div className="app-panel border-destructive/40 p-4 text-sm text-destructive">
      {message}
    </div>
  );
}
