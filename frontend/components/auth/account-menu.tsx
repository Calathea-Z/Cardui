"use client";

import { useState } from "react";
import Link from "next/link";
import { UserButton, useUser } from "@clerk/nextjs";
import { settingsItems } from "@/components/navigation/nav-items";
import { cn } from "@/lib/utils";

type AccountMenuProps = {
  variant?: "sidebar" | "header";
};

/**
 * Clerk user button for the signed-in account.
 * It renders UserButton with no extra props.
 */
export function AccountButton() {
  return <UserButton />;
}

/**
 * Account block with settings destinations and the Clerk button.
 * The sidebar lists those links. The header opens the same list from the account control.
 */
export function AccountMenu({ variant = "sidebar" }: AccountMenuProps) {
  const { user } = useUser();
  const [isOpen, setIsOpen] = useState(false);
  const label =
    user?.primaryEmailAddress?.emailAddress ?? user?.fullName ?? "Account";

  if (variant === "header") {
    return (
      <div className="relative">
        <button
          type="button"
          aria-expanded={isOpen}
          aria-haspopup="menu"
          onClick={() => setIsOpen((open) => !open)}
          className="max-w-40 truncate rounded-lg px-2 py-1.5 text-sm font-medium text-foreground"
        >
          {label}
        </button>
        {isOpen ? (
          <div
            role="menu"
            className="absolute top-full right-0 z-50 mt-2 min-w-52 rounded-lg border border-border bg-popover py-2"
          >
            <SettingsLinks onNavigate={() => setIsOpen(false)} />
            <div className="mt-2 flex items-center gap-3 border-t border-border px-3 pt-2">
              <AccountButton />
              <span className="min-w-0 truncate text-sm font-medium">
                {label}
              </span>
            </div>
          </div>
        ) : null}
      </div>
    );
  }

  return (
    <div className="mt-auto shrink-0 border-t border-sidebar-border pt-4">
      <SettingsLinks />
      <div className="mt-2 flex min-w-0 items-center gap-3 rounded-lg bg-sidebar-accent px-3 py-2.5">
        <AccountButton />
        <span className="min-w-0 truncate text-sm font-medium text-sidebar-foreground">
          {label}
        </span>
      </div>
    </div>
  );
}

type SettingsLinksProps = {
  onNavigate?: () => void;
};

/**
 * Lists settings destinations in the account block.
 * Income, Categories, Connections, and Household stay off the primary nav.
 */
function SettingsLinks({ onNavigate }: SettingsLinksProps) {
  return (
    <nav className="space-y-1" aria-label="Settings">
      {settingsItems.map((item) => (
        <Link
          key={item.href}
          href={item.href}
          onClick={onNavigate}
          className={cn(
            "block rounded-lg px-3 py-2 text-sm font-medium text-sidebar-foreground/80 transition hover:bg-sidebar-accent hover:text-sidebar-foreground",
          )}
        >
          {item.label}
        </Link>
      ))}
    </nav>
  );
}
