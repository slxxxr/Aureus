import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { TOKEN_STORAGE_KEY, ACTIVE_WORKSPACE_KEY } from "@/lib/constants";

type AuthContextValue = {
  token: string | null;
  isAuthenticated: boolean;
  signIn: (token: string) => void;
  signOut: () => void;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(TOKEN_STORAGE_KEY));

  const resetSession = useCallback(() => {
    localStorage.removeItem(ACTIVE_WORKSPACE_KEY);
    queryClient.clear();
  }, [queryClient]);

  useEffect(() => {
    const handleUnauthorized = () => {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      resetSession();
      setToken(null);
    };

    window.addEventListener("aureus:unauthorized", handleUnauthorized);
    return () => window.removeEventListener("aureus:unauthorized", handleUnauthorized);
  }, [resetSession]);

  const value = useMemo<AuthContextValue>(
    () => ({
      token,
      isAuthenticated: token !== null,
      signIn: (newToken: string) => {
        resetSession();
        localStorage.setItem(TOKEN_STORAGE_KEY, newToken);
        setToken(newToken);
      },
      signOut: () => {
        localStorage.removeItem(TOKEN_STORAGE_KEY);
        resetSession();
        setToken(null);
      },
    }),
    [token, resetSession],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);

  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }

  return context;
}
