import type { TFunction } from "i18next";
import { ApiError } from "@/lib/apiClient";

export function resolveTransactionError(error: unknown, t: TFunction): string {
  if (error instanceof ApiError) {
    if (error.code === "NotFound") return t("transactions.errors.notFound");
    if (error.code === "AccountNotFound") return t("transactions.errors.accountNotFound");
    if (error.code === "CategoryNotFound") return t("transactions.errors.categoryNotFound");
    if (error.code === "CategoryRequiredOnTypeChange") return t("transactions.errors.categoryRequired");
    if (error.code === "CategoryTypeMismatch") return t("transactions.errors.categoryTypeMismatch");
    if (error.code === "ValidationFailed") return t("common.validation.invalidInput");
  }
  return t("transactions.errors.generic");
}
