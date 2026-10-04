"use client";

import { useEffect } from "react";
import { Button } from "@/components/ui/button";

type ErrorPageProps = {
  error: Error & { digest?: string };
  reset: () => void;
};

/**
 * Signed-in page failed.
 * The user can retry, and the error is written to the console.
 */
export default function Error({ error, reset }: ErrorPageProps) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <div className="app-panel flex flex-col items-start gap-4 border-destructive/40 bg-destructive/10 p-6">
        <div>
          <h1 className="text-xl font-semibold text-foreground">
            Something went wrong
          </h1>
          <p className="mt-2 text-sm text-muted-foreground">
            This page hit an unexpected error. The rest of the app should still
            work — try again, or head somewhere else from the nav.
          </p>
          {error.message ? (
            <p className="mt-3 text-sm text-destructive/90">{error.message}</p>
          ) : null}
        </div>

        <Button type="button" size="lg" onClick={reset}>
          Try again
        </Button>
      </div>
    </section>
  );
}
