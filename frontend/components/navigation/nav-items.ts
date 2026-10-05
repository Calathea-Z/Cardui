/**
 * One primary destination: the visible label and its path.
 */
export type NavItem = {
  label: string;
  href: string;
};

/**
 * Primary destinations in sidebar and drawer order.
 */
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
    label: "Income",
    href: "/income",
  },
  {
    label: "Institutions",
    href: "/institutions",
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

/**
 * Nav item for the current path.
 * The path `/` matches the `/` item. Any other path matches the first item with a non-root href that pathname starts with.
 */
export function getActiveNavItem(pathname: string): NavItem | undefined {
  if (pathname === "/") {
    return navItems.find((item) => item.href === "/");
  }

  return navItems.find(
    (item) => item.href !== "/" && pathname.startsWith(item.href),
  );
}

/**
 * Phone tab-bar destinations, in navItems order.
 * The list is Dashboard, Transactions, Accounts, Income, and Budgets.
 */
export const bottomNavItems = navItems.filter(
  (item) => item.href !== "/categories" && item.href !== "/institutions",
);
