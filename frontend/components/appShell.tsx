import Link from "next/link";

type AppShellProps = {
  children: React.ReactNode;
};

const navItems = [
  {
    label: "Dashboard",
    href: "/",
  },
  {
    label: "Transactions",
    href: "/transactions",
  },
  {
    label: "Accounts",
    href: "/accounts",
  },
  {
    label: "Budgets",
    href: "/budgets",
  },
  {
    label: "Categories",
    href: "/categories",
  },
];

export function AppShell({ children }: AppShellProps) {
  return (
    <div className="min-h-screen bg-slate-950 text-white">
      <div className="flex min-h-screen">
        <aside className="hidden w-64 border-r border-slate-800 bg-slate-950 px-4 py-6 md:block">
          <div className="mb-8">
            <p className="text-sm text-slate-400">Cardui</p>
            <h1 className="text-xl font-semibold">Finance</h1>
          </div>

          <nav className="space-y-1">
            {navItems.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className="block rounded-md px-3 py-2 text-sm text-slate-300 transition hover:bg-slate-900 hover:text-white"
              >
                {item.label}
              </Link>
            ))}
          </nav>
        </aside>

        <div className="flex min-w-0 flex-1 flex-col">
          <header className="border-b border-slate-800 bg-slate-950 px-4 py-4 md:hidden">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-xs text-slate-400">Cardui</p>
                <p className="font-semibold">Finance</p>
              </div>
            </div>

            <nav className="mt-4 flex gap-2 overflow-x-auto">
              {navItems.map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  className="whitespace-nowrap rounded-md border border-slate-800 px-3 py-2 text-sm text-slate-300"
                >
                  {item.label}
                </Link>
              ))}
            </nav>
          </header>

          <main className="min-w-0 flex-1">{children}</main>
        </div>
      </div>
    </div>
  );
}
