import { SignIn } from "@clerk/nextjs";

/**
 * Sign-in route.
 * Shows the Clerk sign-in form on a full-screen page.
 */
export default function SignInPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-6 bg-background px-4 py-10">
      <div className="max-w-sm text-center">
        <p className="text-2xl font-semibold tracking-tight text-foreground">
          Tortoise
        </p>
        <p className="mt-2 text-sm text-muted-foreground">
          Build a clear, realistic financial recovery plan from the facts you
          know today.
        </p>
      </div>
      <SignIn />
    </div>
  );
}
