import { ApiUnavailableBanner } from "@/components/ApiUnavailableBanner";
import { DesktopSidebar } from "@/components/navigation/desktop-sidebar";
import { MobileShell } from "@/components/navigation/mobile-shell";
import { MobileHeaderActionsProvider } from "@/components/navigation/mobile-header-actions";

type AppShellProps = {
  children: React.ReactNode;
  apiError?: string | null;
};

export function AppShell({ children, apiError = null }: AppShellProps) {
  return (
    <div className="min-h-screen bg-background text-foreground">
      <MobileHeaderActionsProvider>
        <div className="flex min-h-screen">
          <DesktopSidebar />
          <MobileShell>
            {apiError ? (
              <div className="mx-auto w-full max-w-6xl px-6 pt-8">
                <ApiUnavailableBanner message={apiError} />
              </div>
            ) : null}
            {children}
          </MobileShell>
        </div>
      </MobileHeaderActionsProvider>
    </div>
  );
}
