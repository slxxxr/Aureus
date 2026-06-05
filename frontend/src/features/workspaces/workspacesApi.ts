import { apiFetch } from "@/lib/apiClient";

export type Workspace = {
  id: string;
  name: string;
  role: string;
};

export function getWorkspaces(): Promise<Workspace[]> {
  return apiFetch<Workspace[]>("/workspaces");
}
