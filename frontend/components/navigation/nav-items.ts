/**
 * One destination: the visible label and its path.
 */
export type NavItem = {
  label: string;
  href: string;
};

/**
 * Primary destinations in sidebar and tab-bar order.
 * Home, Accounts, and Activity come first; Plan is fourth. Income,
 * Bills, Debts, Plan budget, Savings, and Spending targets stay routes under settings.
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
 * These stay off the primary nav. Plan budget keeps the approved `/living`
 * route, and Spending targets keeps `/targets`.
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
    label: "Plan budget",
    href: "/living",
  },
  {
    label: "Savings",
    href: "/savings",
  },
  {
    label: "Spending targets",
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
 * Visual groups for the existing account-menu destinations.
 * They do not add routes or change the approved primary navigation.
 */
export const settingsGroups = [
  {
    label: "Plan inputs",
    items: settingsItems.slice(0, 5),
  },
  {
    label: "Tracking and data",
    items: settingsItems.slice(5),
  },
];

/**
 * Phone tab-bar destinations.
 * The list uses the same Home, Accounts, Activity, Plan order.
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
