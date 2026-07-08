"use client";

import { ArrowLeft } from "lucide-react";
import { useRouter } from "next/navigation";
import { useCallback } from "react";
import { cn } from "@/lib/utils";

type BackButtonProps = {
  fallbackHref?: string;
  ariaLabel?: string;
  className?: string;
};

export function BackButton({
  fallbackHref = "/",
  ariaLabel = "Go back",
  className,
}: BackButtonProps) {
  const router = useRouter();

  const handleBack = useCallback(() => {
    if (typeof window !== "undefined" && window.history.length > 1) {
      router.back();
      return;
    }

    router.push(fallbackHref);
  }, [fallbackHref, router]);

  return (
    <button
      type="button"
      aria-label={ariaLabel}
      onClick={handleBack}
      className={cn("app-icon-button shrink-0 cursor-pointer", className)}
    >
      <ArrowLeft className="size-5" />
    </button>
  );
}
