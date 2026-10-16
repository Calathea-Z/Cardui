"use client";

import { AccountButton } from "@/components/auth/account-menu";
import { useUser } from "@clerk/nextjs";
import { BrandMark } from "./brand-mark";
import { NavLink } from "./nav-link";
import { navItems, secondaryNavItems } from "./nav-items";

/**
 * Side navigation for wide screens.
 * Primary destinations stay fully visible. Plan setup and Settings are two compact links, not an expanded editor list.
 * A short window scrolls the whole sidebar instead of clipping Home, Accounts, Activity, and Plan.
 */
export function DesktopSidebar() {
  return (
    <aside className="sticky top-0 hidden h-dvh w-64 shrink-0 overflow-y-auto border-r border-sidebar-border bg-sidebar text-sidebar-foreground print:hidden! md:block">
      <div className="flex min-h-full flex-col px-5 py-7">
        <BrandMark />

        <nav className="mt-6 shrink-0 space-y-1" aria-label="Main">
          {navItems.map((item) => (
            <NavLink
              key={item.href}
              item={item}
              exact={item.href === "/plan"}
              showIcon
              className="flex min-h-11 cursor-pointer items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition"
              activeClassName="bg-sidebar-accent text-primary"
              inactiveClassName="text-sidebar-foreground/70 hover:bg-sidebar-accent hover:text-sidebar-foreground"
            />
          ))}
        </nav>

        <div className="mt-auto shrink-0 border-t border-sidebar-border pt-4">
          <nav className="space-y-1" aria-label="Setup and settings">
            {secondaryNavItems.map((item) => (
              <NavLink
                key={item.href}
                item={item}
                className="flex min-h-11 cursor-pointer items-center rounded-lg px-3 py-2 text-sm font-medium transition"
                activeClassName="bg-sidebar-accent text-primary"
                inactiveClassName="text-sidebar-foreground/80 hover:bg-sidebar-accent hover:text-sidebar-foreground"
              />
            ))}
          </nav>
          <SidebarAccount />
        </div>
      </div>
    </aside>
  );
}

/**
 * Signed-in identity under the compact secondary links.
 * Settings destinations open from the Settings page, so this row does not list editors.
 */
function SidebarAccount() {
  const { user } = useUser();
  const label =
    user?.primaryEmailAddress?.emailAddress ?? user?.fullName ?? "Account";

  return (
    <div className="mt-2 flex min-w-0 items-center gap-3 rounded-lg bg-sidebar-accent px-3 py-2.5">
      <AccountButton />
      <span className="min-w-0 truncate text-sm font-medium text-sidebar-foreground">
        {label}
      </span>
    </div>
  );
}
