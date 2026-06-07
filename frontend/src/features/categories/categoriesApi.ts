import { apiFetch } from "@/lib/apiClient";

export type CategoryType = "Income" | "Expense";

export type Category = {
  id: string;
  name: string;
  type: CategoryType;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateCategoryPayload = {
  name: string;
  type: CategoryType;
};

export type UpdateCategoryPayload = {
  name?: string;
};

export function getCategories(workspaceId: string): Promise<Category[]> {
  return apiFetch<Category[]>(`/workspaces/${workspaceId}/categories`);
}

export function createCategory(workspaceId: string, payload: CreateCategoryPayload): Promise<Category> {
  return apiFetch<Category>(`/workspaces/${workspaceId}/categories`, { method: "POST", body: payload });
}

export function updateCategory(workspaceId: string, categoryId: string, payload: UpdateCategoryPayload): Promise<Category> {
  return apiFetch<Category>(`/workspaces/${workspaceId}/categories/${categoryId}`, { method: "PATCH", body: payload });
}

export function deleteCategory(workspaceId: string, categoryId: string): Promise<void> {
  return apiFetch<void>(`/workspaces/${workspaceId}/categories/${categoryId}`, { method: "DELETE" });
}
