import { BrandMark } from "./brand-mark";
import { NavLink } from "./nav-link";
import { navItems } from "./nav-items";

export function DesktopSidebar() {
  return (
    <aside className="hidden w-64 border-r border-sidebar-border bg-sidebar px-5 py-7 text-sidebar-foreground md:block">
      <BrandMark />

      <nav className="space-y-1">
        {navItems.map((item) => (
          <NavLink
            key={item.href}
            item={item}
            showIcon
            className="flex cursor-pointer items-center gap-3 rounded-lg border-l-2 border-transparent px-3 py-2.5 text-sm font-medium transition"
            activeClassName="border-l-sidebar-primary bg-sidebar-accent text-sidebar-primary"
            inactiveClassName="text-sidebar-foreground/65 hover:bg-sidebar-accent/70 hover:text-sidebar-foreground"
          />
        ))}
      </nav>
    </aside>
  );
}
