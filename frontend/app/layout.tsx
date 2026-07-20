import type { Metadata, Viewport } from "next";
import "./globals.css";
import { AppShell } from "@/components/appShell";
import { checkApiHealth, safeApiCall } from "@/lib/api";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "Cardui",
  description: "Personal finance tracking",
};

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  viewportFit: "cover",
};

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const health = await safeApiCall(checkApiHealth, { status: "unavailable" });

  return (
    <html lang="en" className="dark" suppressHydrationWarning>
      <body suppressHydrationWarning>
        <AppShell apiError={health.error}>{children}</AppShell>
      </body>
    </html>
  );
}
