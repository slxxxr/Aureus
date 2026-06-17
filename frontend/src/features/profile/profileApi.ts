import { apiFetch } from "@/lib/apiClient";

export type UserProfile = {
  id: string;
  email: string;
  name: string;
};

export type UpdateProfileRequest = {
  name: string;
};

export function getProfile(): Promise<UserProfile> {
  return apiFetch<UserProfile>("/users/me");
}

export function updateProfile(request: UpdateProfileRequest): Promise<void> {
  return apiFetch<void>("/users/me", { method: "PATCH", body: request });
}
