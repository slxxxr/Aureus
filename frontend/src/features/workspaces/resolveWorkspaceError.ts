import type { TFunction } from "i18next";
import { ApiError } from "@/lib/apiClient";

const codeToTranslationKey: Record<string, string> = {
  NameTaken: "workspace.errors.nameTaken",
  NotFound: "workspace.errors.notFound",
  Forbidden: "workspace.errors.forbidden",
};

export function resolveWorkspaceError(error: unknown, t: TFunction): string {
  if (error instanceof ApiError && error.code !== undefined) {
    const key = codeToTranslationKey[error.code];
    if (key !== undefined) {
      return t(key);
    }
  }

  return t("workspace.errors.generic");
}
