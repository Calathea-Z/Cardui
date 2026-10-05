import {
  ArrowLeftRight,
  Banknote,
  Building2,
  CreditCard,
  LayoutDashboard,
  PieChart,
  Receipt,
  Tags,
  Wallet,
} from "lucide-react";

type NavIconProps = {
  href: string;
};

/**
 * Icon for a primary nav href.
 * The dashboard path and any href without its own case use the dashboard icon.
 */
export function NavIcon({ href }: NavIconProps) {
  const className = "size-5 shrink-0";

  switch (href) {
    case "/transactions":
      return <ArrowLeftRight className={className} aria-hidden />;
    case "/accounts":
      return <Wallet className={className} aria-hidden />;
    case "/income":
      return <Banknote className={className} aria-hidden />;
    case "/bills":
      return <Receipt className={className} aria-hidden />;
    case "/debts":
      return <CreditCard className={className} aria-hidden />;
    case "/institutions":
      return <Building2 className={className} aria-hidden />;
    case "/budgets":
      return <PieChart className={className} aria-hidden />;
    case "/categories":
      return <Tags className={className} aria-hidden />;
    default:
      return <LayoutDashboard className={className} aria-hidden />;
  }
}
