/**
 * Skeleton blocks while a page's data is loading.
 * The pulse uses the same content width as a loaded page.
 */
export function PageLoading() {
  return (
    <div className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <div className="animate-pulse space-y-6">
        <div className="space-y-2">
          <div className="h-4 w-32 rounded bg-muted" />
          <div className="h-9 w-56 rounded bg-muted" />
        </div>
        <div className="h-64 rounded-xl bg-muted/80" />
        <div className="h-48 rounded-xl bg-muted/80" />
      </div>
    </div>
  );
}
