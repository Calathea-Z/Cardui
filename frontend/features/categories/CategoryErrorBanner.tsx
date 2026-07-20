type CategoryErrorBannerProps = {
  message: string;
};

export function CategoryErrorBanner({ message }: CategoryErrorBannerProps) {
  return (
    <p className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
      {message}
    </p>
  );
}
