import { apiFetch } from "@/lib/apiClient";

export type LoginRequest = {
  email: string;
  password: string;
};

export type LoginResponse = {
  accessToken: string;
};

export type StartRegistrationRequest = {
  email: string;
};

export type VerifyEmailCodeRequest = {
  email: string;
  code: string;
};

export type VerifyEmailCodeResponse = {
  registrationToken: string;
};

export type CompleteRegistrationRequest = {
  registrationToken: string;
  password: string;
};

export type CompleteRegistrationResponse = {
  accessToken: string;
};

export function login(request: LoginRequest): Promise<LoginResponse> {
  return apiFetch<LoginResponse>("/auth/login", { method: "POST", body: request, anonymous: true });
}

export function startRegistration(request: StartRegistrationRequest): Promise<void> {
  return apiFetch<void>("/auth/register/start", { method: "POST", body: request, anonymous: true });
}

export function verifyEmailCode(request: VerifyEmailCodeRequest): Promise<VerifyEmailCodeResponse> {
  return apiFetch<VerifyEmailCodeResponse>("/auth/register/verify", {
    method: "POST",
    body: request,
    anonymous: true,
  });
}

export function completeRegistration(
  request: CompleteRegistrationRequest,
): Promise<CompleteRegistrationResponse> {
  return apiFetch<CompleteRegistrationResponse>("/auth/register/complete", {
    method: "POST",
    body: request,
    anonymous: true,
  });
}
