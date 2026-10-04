"use client";

import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";

type MobileHeaderActionsContextValue = {
  leading: ReactNode;
  actions: ReactNode;
  setLeading: (leading: ReactNode) => void;
  setActions: (actions: ReactNode) => void;
};

const MobileHeaderActionsContext =
  createContext<MobileHeaderActionsContextValue | null>(null);

/**
 * Holds the mobile header's leading control and trailing actions.
 * A page sets those slots while it is mounted.
 */
export function MobileHeaderActionsProvider({
  children,
}: {
  children: ReactNode;
}) {
  const [leading, setLeading] = useState<ReactNode>(null);
  const [actions, setActions] = useState<ReactNode>(null);

  return (
    <MobileHeaderActionsContext.Provider
      value={{ leading, actions, setLeading, setActions }}
    >
      {children}
    </MobileHeaderActionsContext.Provider>
  );
}

/**
 * Reads the mobile header slots.
 * Throws when the caller sits outside MobileHeaderActionsProvider.
 */
export function useMobileHeaderActionsContext() {
  const context = useContext(MobileHeaderActionsContext);

  if (!context) {
    throw new Error(
      "useMobileHeaderActionsContext must be used within MobileHeaderActionsProvider",
    );
  }

  return context;
}

/**
 * Places trailing actions in the mobile header for the life of the caller.
 * Clears those actions when the caller unmounts.
 */
export function useSetMobileHeaderActions(actions: ReactNode) {
  const { setActions } = useMobileHeaderActionsContext();

  useEffect(() => {
    setActions(actions);

    return () => {
      setActions(null);
    };
  }, [actions, setActions]);
}

/**
 * Places a leading control in the mobile header for the life of the caller.
 * Clears that control when the caller unmounts.
 */
export function useSetMobileHeaderLeading(leading: ReactNode) {
  const { setLeading } = useMobileHeaderActionsContext();

  useEffect(() => {
    setLeading(leading);

    return () => {
      setLeading(null);
    };
  }, [leading, setLeading]);
}

/**
 * Shows the page's leading header control.
 * Uses fallback when the context leading slot is empty.
 */
export function MobileHeaderLeadingSlot({ fallback }: { fallback: ReactNode }) {
  const context = useContext(MobileHeaderActionsContext);

  if (context?.leading) {
    return <>{context.leading}</>;
  }

  return <>{fallback}</>;
}

/**
 * Shows the page's trailing header actions.
 * Returns null when the context actions slot is empty.
 */
export function MobileHeaderActionsSlot() {
  const context = useContext(MobileHeaderActionsContext);

  if (!context?.actions) {
    return null;
  }

  return (
    <div className="flex shrink-0 items-center gap-1">{context.actions}</div>
  );
}
