import { SignIn } from "@clerk/nextjs";

/**
 * Sign-in route.
 * Shows the Clerk sign-in form on a full-screen page.
 */
export default function SignInPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4 py-10">
      <SignIn />
    </div>
  );
}
