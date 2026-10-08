/**
 * One destination: the visible label and its path.
 */
export type NavItem = {
  label: string;
  href: string;
};

/**
 * Primary destinations in sidebar and tab-bar order.
 * Plan is first. Home, Accounts, and Activity are the daily jobs. Income,
 * Bills, Debts, Living, Savings, and Targets stay routes under settings.
 */
export const navItems: NavItem[] = [
  {
    label: "Home",
    href: "/",
  },
  {
    label: "Accounts",
    href: "/accounts",
  },
  {
    label: "Activity",
    href: "/activity",
  },
  {
    label: "Plan",
    href: "/plan",
  },
];

/**
 * Destinations in the account block.
 * These stay off the primary nav, including Income, Bills, Debts, Living, Savings, and Targets.
 * Each href matches the screen name.
 */
export const settingsItems: NavItem[] = [
  {
    label: "Income",
    href: "/income",
  },
  {
    label: "Bills",
    href: "/bills",
  },
  {
    label: "Debts",
    href: "/debts",
  },
  {
    label: "Living",
    href: "/living",
  },
  {
    label: "Savings",
    href: "/savings",
  },
  {
    label: "Targets",
    href: "/targets",
  },
  {
    label: "Categories",
    href: "/categories",
  },
  {
    label: "Connections",
    href: "/connections",
  },
  {
    label: "Household",
    href: "/household",
  },
];

/**
 * Phone tab-bar destinations.
 * The list is the primary items, with Plan first.
 */
export const bottomNavItems = navItems;

/**
 * Nav item for the current path.
 * The path `/` matches the `/` item. Any other path matches the first primary
 * or settings item with a non-root href that pathname starts with.
 */
export function getActiveNavItem(pathname: string): NavItem | undefined {
  const items = [...navItems, ...settingsItems];

  if (pathname === "/") {
    return items.find((item) => item.href === "/");
  }

  return items.find(
    (item) => item.href !== "/" && pathname.startsWith(item.href),
  );
}
