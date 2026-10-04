export const clerkAppearance = {
  variables: {
    colorBackground: "var(--popover)",
    colorText: "var(--popover-foreground)",
    colorTextSecondary: "var(--muted-foreground)",
    colorNeutral: "var(--border)",
    colorPrimary: "var(--primary)",
    colorPrimaryForeground: "var(--primary-foreground)",
    colorDanger: "var(--destructive)",
    colorInput: "var(--input)",
    colorInputForeground: "var(--foreground)",
    borderRadius: "var(--radius)",
    fontFamily: "var(--font-sora), Segoe UI, sans-serif",
  },
  elements: {
    card: "border border-border bg-popover! text-popover-foreground! shadow-xl",
    headerTitle: "text-foreground!",
    headerSubtitle: "text-muted-foreground!",
    identityPreviewText: "text-popover-foreground!",
    identityPreviewEditButton: "text-primary!",
    identityPreviewEditButtonIcon: "text-primary!",
    socialButtonsBlockButton:
      "border border-border bg-card! text-foreground! hover:bg-accent",
    socialButtonsBlockButtonText: "text-foreground!",
    dividerText: "text-muted-foreground!",
    formFieldLabel: "text-foreground!",
    formFieldInput:
      "border-border bg-input! text-foreground! placeholder:text-muted-foreground!",
    formButtonPrimary: "bg-primary! text-primary-foreground!",
    footerActionText: "text-muted-foreground!",
    footerActionLink: "text-primary!",
    footer: "text-muted-foreground!",
    userButtonPopoverCard:
      "border border-border bg-popover! text-popover-foreground! shadow-xl",
    userButtonPopoverActionButton:
      "border-t border-border text-popover-foreground! hover:bg-accent!",
    userButtonPopoverActionButtonText: "text-popover-foreground!",
    userButtonPopoverActionButtonIcon: "text-muted-foreground!",
    userPreviewMainIdentifier: "text-popover-foreground!",
    userPreviewSecondaryIdentifier: "text-muted-foreground!",
    userButtonPopoverFooter:
      "border-t border-border bg-sidebar! text-muted-foreground!",
  },
};
