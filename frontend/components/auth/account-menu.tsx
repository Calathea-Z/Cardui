"use client";

import Link from "next/link";
import { UserButton, useUser } from "@clerk/nextjs";

/**
 * Clerk user button for the signed-in account.
 * It renders UserButton with no extra props.
 */
export function AccountButton() {
  return <UserButton />;
}

/**
 * Account block at the bottom of the side navigation.
 * Links to Household and shows the Clerk button with the user's email, then full name, then Account.
 */
export function AccountMenu() {
  const { user } = useUser();
  const label =
    user?.primaryEmailAddress?.emailAddress ?? user?.fullName ?? "Account";

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
