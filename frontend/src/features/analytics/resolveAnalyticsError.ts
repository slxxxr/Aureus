import type { TFunction } from "i18next";
import { ApiError } from "@/lib/apiClient";

const codeToKey: Record<string, string> = {
  DailyQuotaExceeded: "dashboard.insights.errorQuotaExceeded",
  LlmTemporarilyUnavailable: "dashboard.insights.errorLlmUnavailable",
};

export function resolveInsightsError(error: unknown, t: TFunction): string {
  if (error instanceof ApiError && error.code !== undefined) {
    const key = codeToKey[error.code];
    if (key !== undefined) {
      return t(key);
    }
  }

  return t("dashboard.insights.error");
}
