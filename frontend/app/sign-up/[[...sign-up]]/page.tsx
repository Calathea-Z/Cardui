import { SignUp } from "@clerk/nextjs";

/**
 * Sign-up route.
 * Shows the Clerk sign-up form on a full-screen page.
 */
export default function SignUpPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4 py-10">
      <SignUp />
    </div>
  );
}
