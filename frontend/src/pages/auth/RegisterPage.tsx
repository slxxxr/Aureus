import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useMutation } from "@tanstack/react-query";
import { AuthShell } from "@/components/auth/AuthShell";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { login, register, type RegisterRequest } from "@/features/auth/authApi";
import { useAuth } from "@/features/auth/AuthContext";
import { resolveAuthError } from "@/features/auth/resolveAuthError";

const MIN_PASSWORD_LENGTH = 8;

export function RegisterPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { signIn } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const mutation = useMutation({
    mutationFn: async (request: RegisterRequest) => {
      await register(request);
      return login(request);
    },
    onSuccess: (data) => {
      signIn(data.accessToken);
      navigate("/", { replace: true });
    },
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    mutation.mutate({ email: email.trim(), password });
  };

  return (
    <AuthShell
      title={t("auth.register.title")}
      subtitle={t("auth.register.subtitle")}
      footer={
        <>
          {t("auth.register.haveAccount")}{" "}
          <Link
            to="/login"
            className="font-medium text-foreground underline-offset-4 hover:underline"
          >
            {t("auth.register.loginLink")}
          </Link>
        </>
      }
    >
      <form onSubmit={handleSubmit} className="space-y-4" noValidate>
        <div className="space-y-2">
          <Label htmlFor="email">{t("auth.fields.email")}</Label>
          <Input
            id="email"
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            placeholder={t("auth.fields.emailPlaceholder")}
            disabled={mutation.isPending}
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="password">{t("auth.fields.password")}</Label>
          <Input
            id="password"
            type="password"
            autoComplete="new-password"
            required
            minLength={MIN_PASSWORD_LENGTH}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            placeholder={t("auth.register.passwordPlaceholder")}
            disabled={mutation.isPending}
          />
        </div>

        {mutation.isError && (
          <p className="text-sm text-destructive" role="alert">
            {resolveAuthError(mutation.error, t)}
          </p>
        )}

        <Button type="submit" className="w-full" disabled={mutation.isPending}>
          {mutation.isPending ? t("auth.register.submitting") : t("auth.register.submit")}
        </Button>
      </form>
    </AuthShell>
  );
}
