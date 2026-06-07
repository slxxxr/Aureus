import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/features/auth/AuthContext";
import { getWorkspaces, type Workspace } from "@/features/workspaces/workspacesApi";
import { ACTIVE_WORKSPACE_KEY } from "@/lib/constants";

type WorkspaceContextValue = {
  workspaces: Workspace[];
  activeWorkspace: Workspace | null;
  setActiveWorkspace: (workspace: Workspace) => void;
  isLoading: boolean;
};

const WorkspaceContext = createContext<WorkspaceContextValue | undefined>(undefined);

export function WorkspaceProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();

  const [activeWorkspaceId, setActiveWorkspaceId] = useState<string | null>(
    () => localStorage.getItem(ACTIVE_WORKSPACE_KEY),
  );

  const { data: workspaces = [], isLoading } = useQuery({
    queryKey: ["workspaces"],
    queryFn: getWorkspaces,
    enabled: isAuthenticated,
    staleTime: 5 * 60 * 1000,
  });

  useEffect(() => {
    if (workspaces.length === 0) return;
    const isValid = workspaces.some((w) => w.id === activeWorkspaceId);
    if (!isValid) {
      const fallbackId = workspaces[0].id;
      localStorage.setItem(ACTIVE_WORKSPACE_KEY, fallbackId);
      setActiveWorkspaceId(fallbackId);
    }
  }, [workspaces, activeWorkspaceId]);

  const value = useMemo<WorkspaceContextValue>(
    () => ({
      workspaces,
      activeWorkspace: workspaces.find((w) => w.id === activeWorkspaceId) ?? null,
      isLoading,
      setActiveWorkspace: (workspace: Workspace) => {
        localStorage.setItem(ACTIVE_WORKSPACE_KEY, workspace.id);
        setActiveWorkspaceId(workspace.id);
      },
    }),
    [workspaces, activeWorkspaceId, isLoading],
  );

  return <WorkspaceContext.Provider value={value}>{children}</WorkspaceContext.Provider>;
}

export function useWorkspace(): WorkspaceContextValue {
  const context = useContext(WorkspaceContext);

  if (context === undefined) {
    throw new Error("useWorkspace must be used within a WorkspaceProvider");
  }

  return context;
}
