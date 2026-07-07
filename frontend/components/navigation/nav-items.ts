export type NavItem = {
  label: string;
  href: string;
};

export const navItems: NavItem[] = [
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

export function getActiveNavItem(pathname: string): NavItem | undefined {
  if (pathname === "/") {
    return navItems.find((item) => item.href === "/");
  }

  return navItems.find(
    (item) => item.href !== "/" && pathname.startsWith(item.href),
  );
}

export const bottomNavItems = navItems.filter(
  (item) => item.href !== "/categories",
);
