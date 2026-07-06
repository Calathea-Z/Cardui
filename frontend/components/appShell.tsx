import { DesktopSidebar } from "@/components/navigation/desktop-sidebar";
import { MobileShell } from "@/components/navigation/mobile-shell";

type AppShellProps = {
  children: React.ReactNode;
};

export function AppShell({ children }: AppShellProps) {
  return (
    <div className="min-h-screen bg-slate-950 text-white">
      <div className="flex min-h-screen">
        <DesktopSidebar />
        <MobileShell>{children}</MobileShell>
      </div>
    </div>
  );
}
