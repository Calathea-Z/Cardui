"use client";

import { MoreHorizontal, Plus } from "lucide-react";
import { useRef } from "react";
import { ActionMenu } from "@/components/ui/action-menu";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

type AccountsActionButtonsProps = {
  onAdd: () => void;
  onRefreshAll: () => void;
  onManageInstitutions: () => void;
  isMenuOpen: boolean;
  onMenuOpenChange: (open: boolean) => void;
  isRefreshing: boolean;
};

/**
 * Adds an account or opens refresh and institution actions.
 * The add control is labeled on desktop and stays an icon on a phone. Refresh and institution management stay disabled while a refresh is running.
 */
export function AccountsActionButtons({
  onAdd,
  onRefreshAll,
  onManageInstitutions,
  isMenuOpen,
  onMenuOpenChange,
  isRefreshing,
}: AccountsActionButtonsProps) {
  const menuContainerRef = useRef<HTMLDivElement>(null);

  return (
    <div className="flex items-center gap-2">
      <Button type="button" onClick={onAdd} className="hidden md:inline-flex">
        Add account
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon-lg"
        aria-label="Add account"
        onClick={onAdd}
        className="md:hidden"
      >
        <Plus className="size-5" />
      </Button>

      <div ref={menuContainerRef}>
        <Button
          type="button"
          variant="ghost"
          size="icon-lg"
          aria-label="More options"
          aria-expanded={isMenuOpen}
          aria-haspopup="menu"
          onClick={() => onMenuOpenChange(!isMenuOpen)}
          className={cn(isMenuOpen && "bg-accent text-foreground")}
        >
          <MoreHorizontal className="size-5" />
        </Button>

        <ActionMenu
          open={isMenuOpen}
          onClose={() => onMenuOpenChange(false)}
          anchorRef={menuContainerRef}
          items={[
            {
              id: "refresh-all",
              label: isRefreshing ? "Refreshing..." : "Refresh all accounts",
              onSelect: onRefreshAll,
              disabled: isRefreshing,
            },
            {
              id: "manage-institutions",
              label: "Manage institutions",
              onSelect: onManageInstitutions,
              disabled: isRefreshing,
            },
          ]}
        />
      </div>
    </div>
  );
}
