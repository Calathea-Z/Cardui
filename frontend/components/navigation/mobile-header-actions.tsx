"use client";

import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";

type MobileHeaderActionsContextValue = {
  actions: ReactNode;
  setActions: (actions: ReactNode) => void;
};

const MobileHeaderActionsContext =
  createContext<MobileHeaderActionsContextValue | null>(null);

export function MobileHeaderActionsProvider({
  children,
}: {
  children: ReactNode;
}) {
  const [actions, setActions] = useState<ReactNode>(null);

  return (
    <MobileHeaderActionsContext.Provider value={{ actions, setActions }}>
      {children}
    </MobileHeaderActionsContext.Provider>
  );
}

export function useMobileHeaderActionsContext() {
  const context = useContext(MobileHeaderActionsContext);

  if (!context) {
    throw new Error(
      "useMobileHeaderActionsContext must be used within MobileHeaderActionsProvider",
    );
  }

  return context;
}

export function useSetMobileHeaderActions(actions: ReactNode) {
  const { setActions } = useMobileHeaderActionsContext();

  useEffect(() => {
    setActions(actions);

    return () => {
      setActions(null);
    };
  }, [actions, setActions]);
}

export function MobileHeaderActionsSlot() {
  const context = useContext(MobileHeaderActionsContext);

  if (!context?.actions) {
    return null;
  }

  return (
    <div className="flex shrink-0 items-center gap-1">
      {context.actions}
    </div>
  );
}
