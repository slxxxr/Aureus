import type { TFunction } from "i18next";
import { ApiError } from "@/lib/apiClient";

const codeToTranslationKey: Record<string, string> = {
  NameTaken: "categories.errors.nameTaken",
  NotFound: "categories.errors.notFound",
};

export function resolveCategoryError(error: unknown, t: TFunction): string {
  if (error instanceof ApiError && error.code !== undefined) {
    const key = codeToTranslationKey[error.code];
    if (key !== undefined) return t(key);
  }
  return t("categories.errors.generic");
}
