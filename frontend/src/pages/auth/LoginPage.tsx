import { InputLimits } from "@/lib/inputLimits";
import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useMutation } from "@tanstack/react-query";
import { Eye, EyeOff } from "lucide-react";
import { AuthShell } from "@/components/auth/AuthShell";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { login } from "@/features/auth/authApi";
import { useAuth } from "@/features/auth/AuthContext";
import { resolveAuthError } from "@/features/auth/resolveAuthError";
import { ApiError } from "@/lib/apiClient";

export function LoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { signIn } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const mutation = useMutation({
    mutationFn: login,
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
      title={t("auth.login.title")}
      subtitle={t("auth.login.subtitle")}
      footer={
        <>
          {t("auth.login.noAccount")}{" "}
          <Link
            to="/register"
            className="font-medium text-foreground underline-offset-4 hover:underline"
          >
            {t("auth.login.registerLink")}
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
            maxLength={InputLimits.emailMaxLength}
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            placeholder={t("auth.fields.emailPlaceholder")}
            disabled={mutation.isPending}
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="password">{t("auth.fields.password")}</Label>
          <div className="relative">
            <Input
              id="password"
              type={showPassword ? "text" : "password"}
              autoComplete="current-password"
              required
              maxLength={InputLimits.passwordMaxLength}
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              placeholder={t("auth.fields.passwordPlaceholder")}
              disabled={mutation.isPending}
              className="pr-10"
            />
            <button
              type="button"
              tabIndex={-1}
              onClick={() => setShowPassword((v) => !v)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
            >
              {showPassword ? (
                <EyeOff className="h-4 w-4" aria-hidden="true" />
              ) : (
                <Eye className="h-4 w-4" aria-hidden="true" />
              )}
            </button>
          </div>
        </div>

        {mutation.isError && (
          <p className="text-sm text-destructive" role="alert">
            {resolveAuthError(mutation.error, t)}
            {mutation.error instanceof ApiError &&
              mutation.error.code === "EmailNotConfirmed" && (
                <>
                  {" "}
                  <Link to="/register" className="underline underline-offset-4">
                    {t("auth.login.registerLink")}
                  </Link>
                </>
              )}
          </p>
        )}

        <Button type="submit" className="w-full" disabled={mutation.isPending}>
          {mutation.isPending ? t("auth.login.submitting") : t("auth.login.submit")}
        </Button>
      </form>
    </AuthShell>
  );
}
