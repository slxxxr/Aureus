import { apiFetch } from "@/lib/apiClient";

export type WorkspaceRole = "Owner" | "Member";

export type Workspace = {
  id: string;
  name: string;
  role: WorkspaceRole;
};

export type CreateWorkspacePayload = {
  name: string;
};

export type UpdateWorkspacePayload = {
  name?: string;
};

export function getWorkspaces(): Promise<Workspace[]> {
  return apiFetch<Workspace[]>("/workspaces");
}

export function createWorkspace(payload: CreateWorkspacePayload): Promise<Workspace> {
  return apiFetch<Workspace>("/workspaces", { method: "POST", body: payload });
}

export function updateWorkspace(workspaceId: string, payload: UpdateWorkspacePayload): Promise<Workspace> {
  return apiFetch<Workspace>(`/workspaces/${workspaceId}`, { method: "PATCH", body: payload });
}

export function deleteWorkspace(workspaceId: string): Promise<void> {
  return apiFetch<void>(`/workspaces/${workspaceId}`, { method: "DELETE" });
}
