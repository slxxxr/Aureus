import type { TFunction } from "i18next";
import { ApiError } from "@/lib/apiClient";

const codeToKey: Record<string, string> = {
  NotFound: "invitations.errors.notFound",
  AlreadyMember: "invitations.errors.alreadyMember",
  WorkspaceFull: "invitations.errors.workspaceFull",
  DailyQuotaExceeded: "invitations.errors.dailyQuotaExceeded",
  Forbidden: "invitations.errors.forbidden",
  ValidationFailed: "common.validation.invalidInput",
};

export function resolveInvitationError(error: unknown, t: TFunction): string {
  if (error instanceof ApiError && error.code !== undefined) {
    const key = codeToKey[error.code];
    if (key !== undefined) {
      return t(key);
    }
  }

  return t("invitations.errors.generic");
}
