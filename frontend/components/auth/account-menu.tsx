"use client";

import Link from "next/link";
import { UserButton, useUser } from "@clerk/nextjs";

export function AccountButton() {
  return <UserButton />;
}

export function AccountMenu() {
  const { user } = useUser();
  const label =
    user?.primaryEmailAddress?.emailAddress ??
    user?.fullName ??
    "Account";

  return (
    <div className="mt-auto shrink-0 border-t border-sidebar-border pt-4">
      <Link
        href="/household"
        className="mb-2 block rounded-lg px-3 py-2 text-sm font-medium text-sidebar-foreground/80 transition hover:bg-sidebar-accent/70 hover:text-sidebar-foreground"
      >
        Household
      </Link>
      <div className="flex min-w-0 items-center gap-3 rounded-lg bg-sidebar-accent px-3 py-2.5">
        <AccountButton />
        <span className="min-w-0 truncate text-sm font-medium text-sidebar-foreground">
          {label}
        </span>
      </div>
    </div>
  );
}
