import type { TFunction } from "i18next";
import { ApiError } from "@/lib/apiClient";

const codeToTranslationKey: Record<string, string> = {
  InvalidCredentials: "auth.errors.invalidCredentials",
  EmailNotConfirmed: "auth.errors.emailNotConfirmed",
  InvalidEmail: "auth.errors.invalidEmail",
  InvalidPassword: "auth.errors.invalidPassword",
  EmailAlreadyConfirmed: "auth.errors.emailAlreadyExists",
  RateLimited: "auth.errors.rateLimited",
  CodeNotFound: "auth.errors.codeNotFound",
  CodeExpired: "auth.errors.codeExpired",
  InvalidCode: "auth.errors.invalidCode",
  TooManyAttempts: "auth.errors.tooManyAttempts",
  RegistrationTokenInvalid: "auth.errors.registrationTokenInvalid",
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
