import { auth } from "@clerk/nextjs/server";
import Link from "next/link";
import { PageHeader } from "@/components/navigation/page-header";
import { settingsDestinations } from "@/components/navigation/nav-items";

/**
 * Renders Settings on each request.
 */
export const dynamic = "force-dynamic";

const details: Record<string, string> = {
  "/categories": "Names and groups for activity.",
  "/connections": "Linked banks. Accounts also opens this.",
  "/household": "People, currency, and time zone.",
};

/**
 * Settings route.
 * Categories, Connections, and Household stay here. Plan editors stay on Plan setup.
 */
export default async function SettingsPage() {
  await auth.protect();

  return (
    <section className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
      <PageHeader
        title="Settings"
        description="Categories, connections, and the household."
      />
      <ul className="app-panel divide-y divide-border">
        {settingsDestinations.map((item) => (
          <li key={item.href}>
            <Link
              href={item.href}
              className="flex min-h-11 flex-col justify-center px-4 py-3 focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
            >
              <span className="text-sm font-medium text-foreground">
                {item.label}
              </span>
              <span className="text-xs text-muted-foreground">
                {details[item.href]}
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
