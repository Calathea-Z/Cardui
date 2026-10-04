import { DesktopSidebar } from "@/components/navigation/desktop-sidebar";
import { MobileShell } from "@/components/navigation/mobile-shell";
import { MobileHeaderActionsProvider } from "@/components/navigation/mobile-header-actions";

type AppShellProps = {
  children: React.ReactNode;
};

export function AppShell({ children }: AppShellProps) {
  return (
    <div className="min-h-screen bg-background text-foreground">
      <MobileHeaderActionsProvider>
        <div className="flex min-h-screen">
          <DesktopSidebar />
          <MobileShell>{children}</MobileShell>
        </div>
      </MobileHeaderActionsProvider>
    </div>
  );
}
