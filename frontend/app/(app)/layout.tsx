import { auth } from "@clerk/nextjs/server";
import { SessionTokenRegistration } from "@/components/auth/session-token-registration";
import { AppShell } from "@/components/app-shell";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { getApiErrorMessage } from "@/lib/api/errors";
import { ensureCurrentHousehold } from "@/lib/api/server/households";

export const dynamic = "force-dynamic";

export default async function SignedInLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  await auth.protect();

  let householdError: string | null = null;

  try {
    await ensureCurrentHousehold();
  } catch (error) {
    householdError = getApiErrorMessage(
      error,
      "Your household could not be saved.",
    );
  }

  return (
    <AppShell>
      <SessionTokenRegistration />
      {householdError ? <PageApiErrorBanner message={householdError} /> : null}
      {children}
    </AppShell>
  );
}
