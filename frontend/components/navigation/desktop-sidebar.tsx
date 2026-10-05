import { AccountMenu } from "@/components/auth/account-menu";
import { BrandMark } from "./brand-mark";
import { NavLink } from "./nav-link";
import { navItems } from "./nav-items";

/**
 * Side navigation for wide screens.
 * Shows the brand, the three primary destinations, and the account menu, and stays hidden below the md breakpoint.
 */
export function DesktopSidebar() {
  return (
    <aside className="sticky top-0 hidden h-dvh w-64 shrink-0 flex-col overflow-hidden border-r border-sidebar-border bg-sidebar px-5 py-7 text-sidebar-foreground md:flex">
      <BrandMark />

      <nav className="min-h-0 flex-1 space-y-1 overflow-y-auto">
        {navItems.map((item) => (
          <NavLink
            key={item.href}
            item={item}
            showIcon
            className="flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition"
            activeClassName="bg-sidebar-accent text-primary"
            inactiveClassName="text-sidebar-foreground/70 hover:bg-sidebar-accent hover:text-sidebar-foreground"
          />
        ))}
      </nav>
      <AccountMenu />
    </aside>
  );
}
