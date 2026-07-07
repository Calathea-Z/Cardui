"use client";

import { MoreHorizontal, Plus } from "lucide-react";
import { useRef } from "react";
import { ActionMenu } from "@/components/ui/action-menu";
import { cn } from "@/lib/utils";

type AccountsActionButtonsProps = {
  onAdd: () => void;
  onRefreshAll: () => void;
  onManageInstitutions: () => void;
  isMenuOpen: boolean;
  onMenuOpenChange: (open: boolean) => void;
  isRefreshing: boolean;
};

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
    <div className="flex items-center gap-1">
      <button
        type="button"
        aria-label="Add account"
        onClick={onAdd}
        className="app-icon-button"
      >
        <Plus className="size-5" />
      </button>

      <div ref={menuContainerRef} className="relative">
        <button
          type="button"
          aria-label="More options"
          aria-expanded={isMenuOpen}
          aria-haspopup="menu"
          onClick={() => onMenuOpenChange(!isMenuOpen)}
          className={cn(
            "app-icon-button",
            isMenuOpen && "bg-accent text-foreground",
          )}
        >
          <MoreHorizontal className="size-5" />
        </button>

        <ActionMenu
          open={isMenuOpen}
          onClose={() => onMenuOpenChange(false)}
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
