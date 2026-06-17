import type { TFunction } from "i18next";
import { ApiError } from "@/lib/apiClient";

const codeToKey: Record<string, string> = {
  MemberNotFound: "workspaceMembers.errors.memberNotFound",
  CannotRemoveOwner: "workspaceMembers.errors.cannotRemoveOwner",
  InsufficientRole: "workspaceMembers.errors.insufficientRole",
  CannotLeaveAsOwner: "workspaceMembers.errors.cannotLeaveAsOwner",
  CannotTargetSelf: "workspaceMembers.errors.cannotTargetSelf",
  CannotChangeOwnerRole: "workspaceMembers.errors.cannotChangeOwnerRole",
  ValidationFailed: "common.validation.invalidInput",
};

export function resolveWorkspaceMemberError(error: unknown, t: TFunction): string {
  if (error instanceof ApiError && error.code !== undefined) {
    const key = codeToKey[error.code];
    if (key !== undefined) {
      return t(key);
    }
  }

  return t("workspaceMembers.errors.generic");
}
