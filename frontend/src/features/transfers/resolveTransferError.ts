import type { TFunction } from "i18next";
import { ApiError } from "@/lib/apiClient";

export function resolveTransferError(error: unknown, t: TFunction): string {
  if (error instanceof ApiError) {
    if (error.code === "NotFound") return t("transfers.errors.notFound");
    if (error.code === "AccountNotFound") return t("transfers.errors.accountNotFound");
    if (error.code === "CurrencyMismatch") return t("transfers.errors.currencyMismatch");
    if (error.code === "SameAccount") return t("transfers.errors.sameAccount");
    if (error.code === "ValidationFailed") return t("common.validation.invalidInput");
  }
  return t("transfers.errors.generic");
}
