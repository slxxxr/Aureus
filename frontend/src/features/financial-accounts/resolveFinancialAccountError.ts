import type { TFunction } from "i18next";
import { ApiError } from "@/lib/apiClient";

const codeToTranslationKey: Record<string, string> = {
  NameTaken: "financialAccounts.errors.nameTaken",
  NotFound: "financialAccounts.errors.notFound",
  ValidationFailed: "common.validation.invalidInput",
};

export function resolveFinancialAccountError(error: unknown, t: TFunction): string {
  if (error instanceof ApiError && error.code !== undefined) {
    const key = codeToTranslationKey[error.code];
    if (key !== undefined) {
      return t(key);
    }
  }

  return t("financialAccounts.errors.generic");
}
