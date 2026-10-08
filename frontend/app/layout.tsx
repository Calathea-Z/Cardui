import { ClerkProvider } from "@clerk/nextjs";
import { clerkAppearance } from "@/components/auth/clerk-appearance";
import { ConfirmProvider } from "@/components/ui/confirm-dialog";
import { Toaster } from "@/components/ui/sonner";
import type { Metadata, Viewport } from "next";
import { Inter } from "next/font/google";
import "./globals.css";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
  display: "swap",
});

/**
 * Browser title and description for Tortoise.
 */
export const metadata: Metadata = {
  title: "Tortoise",
  description: "Build a clear, realistic financial recovery plan",
};

/**
 * Sizes the page to the device width and covers the screen safe area.
 */
export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  viewportFit: "cover",
};

/**
 * Root layout for Tortoise.
 * Applies the light theme, Inter, and Clerk sign-in and sign-up routes.
 */
export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className={inter.variable} suppressHydrationWarning>
      <body className="font-sans antialiased" suppressHydrationWarning>
        <ClerkProvider
          signInUrl="/sign-in"
          signUpUrl="/sign-up"
          appearance={clerkAppearance}
        >
          <ConfirmProvider>
            {children}
            <Toaster />
          </ConfirmProvider>
        </ClerkProvider>
      </body>
    </html>
  );
}
