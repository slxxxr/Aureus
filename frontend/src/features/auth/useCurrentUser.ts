import { useMemo } from "react";
import { useAuth } from "@/features/auth/AuthContext";

type JwtPayload = {
  sub: string;
  email: string;
  exp: number;
};

type CurrentUser = {
  userId: string;
  email: string;
};

function decodeJwtPayload(token: string): JwtPayload | null {
  try {
    const payloadBase64 = token.split(".")[1];
    if (payloadBase64 === undefined) return null;
    const json = atob(payloadBase64.replace(/-/g, "+").replace(/_/g, "/"));
    return JSON.parse(json) as JwtPayload;
  } catch {
    return null;
  }
}

export function useCurrentUser(): CurrentUser | null {
  const { token } = useAuth();

  return useMemo(() => {
    if (token === null) return null;
    const payload = decodeJwtPayload(token);
    if (payload === null) return null;
    return { userId: payload.sub, email: payload.email };
  }, [token]);
}
