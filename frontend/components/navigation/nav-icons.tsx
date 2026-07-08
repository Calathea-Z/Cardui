import {
  ArrowLeftRight,
  Building2,
  LayoutDashboard,
  PieChart,
  Tags,
  Wallet,
} from "lucide-react";

type NavIconProps = {
  href: string;
};

export function NavIcon({ href }: NavIconProps) {
  const className = "size-5 shrink-0";

  switch (href) {
    case "/transactions":
      return <ArrowLeftRight className={className} aria-hidden />;
    case "/accounts":
      return <Wallet className={className} aria-hidden />;
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
