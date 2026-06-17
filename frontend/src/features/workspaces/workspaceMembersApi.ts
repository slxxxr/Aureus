import { apiFetch } from "@/lib/apiClient";
import type { WorkspaceRole } from "@/features/workspaces/workspacesApi";

export type WorkspaceMember = {
  userId: string;
  name: string;
  email: string;
  role: WorkspaceRole;
  joinedAt: string;
};

export function getWorkspaceMembers(workspaceId: string): Promise<WorkspaceMember[]> {
  return apiFetch<WorkspaceMember[]>(`/workspaces/${workspaceId}/members`);
}

export function removeWorkspaceMember(workspaceId: string, userId: string): Promise<void> {
  return apiFetch<void>(`/workspaces/${workspaceId}/members/${userId}`, { method: "DELETE" });
}

export function updateMemberRole(workspaceId: string, userId: string, role: WorkspaceRole): Promise<void> {
  return apiFetch<void>(`/workspaces/${workspaceId}/members/${userId}/role`, {
    method: "PATCH",
    body: { role },
  });
}

export function transferOwnership(workspaceId: string, userId: string): Promise<void> {
  return apiFetch<void>(`/workspaces/${workspaceId}/transfer-ownership`, {
    method: "POST",
    body: { userId },
  });
}

export function leaveWorkspace(workspaceId: string): Promise<void> {
  return apiFetch<void>(`/workspaces/${workspaceId}/leave`, { method: "POST" });
}
