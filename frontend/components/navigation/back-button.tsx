"use client";

import { ArrowLeft } from "lucide-react";
import { useRouter } from "next/navigation";
import { useCallback } from "react";
import { Button } from "@/components/ui/button";
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
    <Button
      type="button"
      variant="ghost"
      size="icon-lg"
      aria-label={ariaLabel}
      onClick={handleBack}
      className={cn("shrink-0", className)}
    >
      <ArrowLeft className="size-5" />
    </Button>
  );
}
