import type { TFunction } from "i18next";
import { ApiError } from "@/lib/apiClient";

const codeToTranslationKey: Record<string, string> = {
  InvalidCredentials: "auth.errors.invalidCredentials",
  InvalidEmail: "auth.errors.invalidEmail",
  InvalidPassword: "auth.errors.invalidPassword",
  EmailAlreadyExists: "auth.errors.emailAlreadyExists",
};

export function resolveAuthError(error: unknown, t: TFunction): string {
  if (error instanceof ApiError && error.code !== undefined) {
    const key = codeToTranslationKey[error.code];
    if (key !== undefined) {
      return t(key);
    }
  }

  return t("auth.errors.generic");
}
