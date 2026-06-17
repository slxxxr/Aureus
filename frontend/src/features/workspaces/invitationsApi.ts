import { apiFetch } from "@/lib/apiClient";

export type WorkspaceInvitation = {
  id: string;
  email: string;
  invitedByUserId: string;
  expiresAt: string;
};

export type MyInvitation = {
  id: string;
  workspaceId: string;
  workspaceName: string;
  expiresAt: string;
};

export function inviteMember(workspaceId: string, email: string, language: string): Promise<void> {
  return apiFetch<void>(`/workspaces/${workspaceId}/invitations`, {
    method: "POST",
    body: { email, language },
  });
}

export function getWorkspaceInvitations(workspaceId: string): Promise<WorkspaceInvitation[]> {
  return apiFetch<WorkspaceInvitation[]>(`/workspaces/${workspaceId}/invitations`);
}

export function revokeInvitation(workspaceId: string, invitationId: string): Promise<void> {
  return apiFetch<void>(`/workspaces/${workspaceId}/invitations/${invitationId}`, {
    method: "DELETE",
  });
}

export function getMyInvitations(): Promise<MyInvitation[]> {
  return apiFetch<MyInvitation[]>("/users/me/invitations");
}

export function acceptInvitation(invitationId: string): Promise<void> {
  return apiFetch<void>(`/users/me/invitations/${invitationId}/accept`, { method: "POST" });
}

export function declineInvitation(invitationId: string): Promise<void> {
  return apiFetch<void>(`/users/me/invitations/${invitationId}/decline`, { method: "POST" });
}
