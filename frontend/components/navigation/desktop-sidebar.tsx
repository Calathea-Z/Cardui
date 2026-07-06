import { NavLink } from "./nav-link";
import { navItems } from "./nav-items";

export function DesktopSidebar() {
  return (
    <aside className="hidden w-64 border-r border-slate-800 bg-slate-950 px-4 py-6 md:block">
      <div className="mb-8">
        <p className="text-sm text-slate-400">Cardui</p>
        <h1 className="text-xl font-semibold">Finance</h1>
      </div>

      <nav className="space-y-1">
        {navItems.map((item) => (
          <NavLink
            key={item.href}
            item={item}
            showIcon
            className="flex cursor-pointer items-center gap-3 rounded-md px-3 py-2 text-sm transition"
            activeClassName="bg-slate-900 text-emerald-400"
            inactiveClassName="text-slate-300 hover:bg-slate-900 hover:text-white"
          />
        ))}
      </nav>
    </aside>
  );
}
