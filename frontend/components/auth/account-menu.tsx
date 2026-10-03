"use client";

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
      <div className="flex min-w-0 items-center gap-3 rounded-lg bg-sidebar-accent px-3 py-2.5">
        <AccountButton />
        <span className="min-w-0 truncate text-sm font-medium text-sidebar-foreground">
          {label}
        </span>
      </div>
    </div>
  );
}
