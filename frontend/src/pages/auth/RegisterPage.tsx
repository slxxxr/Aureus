import { useState, useEffect, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useMutation } from "@tanstack/react-query";
import { Eye, EyeOff } from "lucide-react";
import { AuthShell } from "@/components/auth/AuthShell";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  startRegistration,
  verifyEmailCode,
  completeRegistration,
} from "@/features/auth/authApi";
import { useAuth } from "@/features/auth/AuthContext";
import { resolveAuthError } from "@/features/auth/resolveAuthError";

type Step = "email" | "code" | "password";

const MIN_PASSWORD_LENGTH = 8;
const RESEND_COOLDOWN_SECONDS = 60;

export function RegisterPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { signIn } = useAuth();

  const [step, setStep] = useState<Step>("email");
  const [email, setEmail] = useState("");
  const [code, setCode] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [registrationToken, setRegistrationToken] = useState("");

  const [sendCount, setSendCount] = useState(0);
  const [resendCooldown, setResendCooldown] = useState(0);

  useEffect(() => {
    if (sendCount === 0) {
      return;
    }

    setResendCooldown(RESEND_COOLDOWN_SECONDS);

    const id = setInterval(() => {
      setResendCooldown((prev) => {
        if (prev <= 1) {
          clearInterval(id);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(id);
  }, [sendCount]);

  const footer = (
    <>
      {t("auth.register.haveAccount")}{" "}
      <Link to="/login" className="font-medium text-foreground underline-offset-4 hover:underline">
        {t("auth.register.loginLink")}
      </Link>
    </>
  );

  // ─── Step 1: send code ───────────────────────────────────────────────────────

  const startMutation = useMutation({
    mutationFn: startRegistration,
    onSuccess: () => {
      setSendCount((c) => c + 1);
      setStep("code");
    },
  });

  const handleEmailSubmit = (event: FormEvent) => {
    event.preventDefault();
    startMutation.mutate({ email: email.trim() });
  };

  // ─── Step 2: verify code ─────────────────────────────────────────────────────

  const verifyMutation = useMutation({
    mutationFn: verifyEmailCode,
    onSuccess: (data) => {
      setRegistrationToken(data.registrationToken);
      setStep("password");
    },
  });

  const handleCodeSubmit = (event: FormEvent) => {
    event.preventDefault();
    verifyMutation.mutate({ email: email.trim(), code: code.trim() });
  };

  const handleResend = () => {
    verifyMutation.reset();
    startMutation.mutate({ email: email.trim() });
  };

  // ─── Step 3: set password ────────────────────────────────────────────────────

  const completeMutation = useMutation({
    mutationFn: completeRegistration,
    onSuccess: (data) => {
      signIn(data.accessToken);
      navigate("/", { replace: true });
    },
  });

  const handlePasswordSubmit = (event: FormEvent) => {
    event.preventDefault();
    completeMutation.mutate({ registrationToken, password });
  };

  // ─── Render ──────────────────────────────────────────────────────────────────

  if (step === "email") {
    return (
      <AuthShell
        title={t("auth.register.step1.title")}
        subtitle={t("auth.register.step1.subtitle")}
        footer={footer}
      >
        <form onSubmit={handleEmailSubmit} className="space-y-4" noValidate>
          <div className="space-y-2">
            <Label htmlFor="email">{t("auth.fields.email")}</Label>
            <Input
              id="email"
              type="email"
              autoComplete="email"
              autoFocus
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder={t("auth.fields.emailPlaceholder")}
              disabled={startMutation.isPending}
            />
          </div>

          {startMutation.isError && (
            <p className="text-sm text-destructive" role="alert">
              {resolveAuthError(startMutation.error, t)}
            </p>
          )}

          <Button
            type="submit"
            className="w-full"
            disabled={startMutation.isPending || !email.trim()}
          >
            {startMutation.isPending
              ? t("auth.register.step1.submitting")
              : t("auth.register.step1.submit")}
          </Button>
        </form>
      </AuthShell>
    );
  }

  if (step === "code") {
    return (
      <AuthShell
        title={t("auth.register.step2.title")}
        subtitle={t("auth.register.step2.subtitle")}
        footer={footer}
      >
        <form onSubmit={handleCodeSubmit} className="space-y-4" noValidate>
          <p className="text-sm text-muted-foreground">
            {t("auth.register.step2.codeSentTo", { email: email.trim() })}
          </p>

          <div className="space-y-2">
            <Label htmlFor="code">{t("auth.register.step2.codeLabel")}</Label>
            <Input
              id="code"
              type="text"
              inputMode="numeric"
              autoComplete="one-time-code"
              autoFocus
              required
              maxLength={6}
              value={code}
              onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))}
              placeholder={t("auth.register.step2.codePlaceholder")}
              disabled={verifyMutation.isPending}
              className="text-center tracking-widest text-lg"
            />
          </div>

          {verifyMutation.isError && (
            <p className="text-sm text-destructive" role="alert">
              {resolveAuthError(verifyMutation.error, t)}
            </p>
          )}

          {startMutation.isError && (
            <p className="text-sm text-destructive" role="alert">
              {resolveAuthError(startMutation.error, t)}
            </p>
          )}

          <Button
            type="submit"
            className="w-full"
            disabled={verifyMutation.isPending || code.length < 6}
          >
            {verifyMutation.isPending
              ? t("auth.register.step2.submitting")
              : t("auth.register.step2.submit")}
          </Button>

          <Button
            type="button"
            variant="ghost"
            className="w-full"
            disabled={startMutation.isPending || resendCooldown > 0}
            onClick={handleResend}
          >
            {resendCooldown > 0
              ? t("auth.register.step2.resendCooldown", { seconds: resendCooldown })
              : t("auth.register.step2.resend")}
          </Button>
        </form>
      </AuthShell>
    );
  }

  return (
    <AuthShell
      title={t("auth.register.step3.title")}
      subtitle={t("auth.register.step3.subtitle")}
      footer={footer}
    >
      <form onSubmit={handlePasswordSubmit} className="space-y-4" noValidate>
        <div className="space-y-2">
          <Label htmlFor="password">{t("auth.fields.password")}</Label>
          <div className="relative">
            <Input
              id="password"
              type={showPassword ? "text" : "password"}
              autoComplete="new-password"
              autoFocus
              required
              minLength={MIN_PASSWORD_LENGTH}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder={t("auth.register.step3.passwordPlaceholder")}
              disabled={completeMutation.isPending}
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

        {completeMutation.isError && (
          <p className="text-sm text-destructive" role="alert">
            {resolveAuthError(completeMutation.error, t)}
          </p>
        )}

        <Button
          type="submit"
          className="w-full"
          disabled={completeMutation.isPending || password.length < MIN_PASSWORD_LENGTH}
        >
          {completeMutation.isPending
            ? t("auth.register.step3.submitting")
            : t("auth.register.step3.submit")}
        </Button>
      </form>
    </AuthShell>
  );
}
