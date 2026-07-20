import type { Metadata, Viewport } from "next";
import "./globals.css";
import { AppShell } from "@/components/appShell";

export const metadata: Metadata = {
  title: "Cardui",
  description: "Personal finance tracking",
};

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  viewportFit: "cover",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className="dark" suppressHydrationWarning>
      <body suppressHydrationWarning>
        <AppShell>{children}</AppShell>
      </body>
    </html>
  );
}
