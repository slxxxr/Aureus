import { InputLimits } from "@/lib/inputLimits";
import { useState, useEffect, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getProfile, updateProfile } from "@/features/profile/profileApi";

export function SettingsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const profileQuery = useQuery({
    queryKey: ["profile"],
    queryFn: getProfile,
  });

  const [name, setName] = useState("");

  useEffect(() => {
    if (profileQuery.data) {
      setName(profileQuery.data.name);
    }
  }, [profileQuery.data]);

  const updateMutation = useMutation({
    mutationFn: updateProfile,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["profile"] });
    },
  });

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    updateMutation.mutate({ name: name.trim() });
  };

  const trimmedName = name.trim();
  const isValid = trimmedName.length > 0 && trimmedName.length <= InputLimits.nameMaxLength;
  const hasChanged = trimmedName !== (profileQuery.data?.name ?? "");
  const canSubmit = isValid && hasChanged && !updateMutation.isPending;

  return (
    <div className="max-w-lg px-6 pt-4 sm:pt-11">
      <section>
        <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          {t("settings.profile.title")}
        </p>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="email">{t("auth.fields.email")}</Label>
            <Input
              id="email"
              type="text"
              autoComplete="off"
              disabled
              value={profileQuery.data?.email ?? ""}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="name">{t("settings.profile.nameLabel")}</Label>
            <Input
              id="name"
              type="text"
              autoComplete="off"
              maxLength={InputLimits.nameMaxLength}
              value={name}
              onChange={(e) => setName(e.target.value)}
              disabled={profileQuery.isLoading || updateMutation.isPending}
            />
          </div>

          {updateMutation.isError && (
            <p className="text-sm text-destructive" role="alert">
              {t("settings.profile.errorGeneric")}
            </p>
          )}

          <div className="flex items-center gap-3">
            <Button type="submit" size="sm" disabled={!canSubmit}>
              {updateMutation.isPending
                ? t("settings.profile.saving")
                : t("settings.profile.save")}
            </Button>
            {updateMutation.isSuccess && !hasChanged && (
              <span className="text-sm text-muted-foreground">{t("settings.profile.saved")}</span>
            )}
          </div>
        </form>
      </section>
    </div>
  );
}
