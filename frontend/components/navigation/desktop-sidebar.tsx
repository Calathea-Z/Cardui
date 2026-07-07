import { NavLink } from "./nav-link";
import { navItems } from "./nav-items";

export function DesktopSidebar() {
  return (
    <aside className="hidden w-64 border-r border-border bg-sidebar px-4 py-6 md:block">
      <div className="mb-8">
        <p className="text-sm text-muted-foreground">Cardui</p>
        <h1 className="text-xl font-semibold text-violet-100">Finance</h1>
      </div>

      <nav className="space-y-1">
        {navItems.map((item) => (
          <NavLink
            key={item.href}
            item={item}
            showIcon
            className="flex cursor-pointer items-center gap-3 rounded-md px-3 py-2 text-sm transition"
            activeClassName="bg-accent text-primary"
            inactiveClassName="text-muted-foreground hover:bg-accent hover:text-foreground"
          />
        ))}
      </nav>
    </aside>
  );
}
