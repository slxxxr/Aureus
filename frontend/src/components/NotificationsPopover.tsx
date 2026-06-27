import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { formatDate } from "@/lib/formatDate";
import { Bell } from "lucide-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/features/auth/AuthContext";
import {
  acceptInvitation,
  declineInvitation,
  getMyInvitations,
  type MyInvitation,
} from "@/features/workspaces/invitationsApi";
import { resolveInvitationError } from "@/features/workspaces/resolveInvitationError";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

function formatExpiryDate(isoDate: string, language: string, t: TFunction): string {
  return t("invitations.expires", { date: formatDate(isoDate, language) });
}

// ─── single invitation row ─────────────────────────────────────────────────────

function InvitationRow({ invitation }: { invitation: MyInvitation }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["my-invitations"] });

  const acceptMutation = useMutation({
    mutationFn: () => acceptInvitation(invitation.id),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["workspaces"] }),
        invalidate(),
      ]);
    },
    onError: (err) => setError(resolveInvitationError(err, t)),
  });

  const declineMutation = useMutation({
    mutationFn: () => declineInvitation(invitation.id),
    onSuccess: () => void invalidate(),
    onError: (err) => setError(resolveInvitationError(err, t)),
  });

  const isPending = acceptMutation.isPending || declineMutation.isPending;

  return (
    <div className="px-3 py-2.5">
      <p className="truncate text-sm font-medium">{invitation.workspaceName}</p>
      <p className="text-xs text-muted-foreground">
        {formatExpiryDate(invitation.expiresAt, i18n.language, t)}
      </p>

      {error !== null && (
        <p className="mt-1 text-xs text-destructive" role="alert">
          {error}
        </p>
      )}

      <div className="mt-2 flex gap-2">
        <Button
          size="sm"
          className="px-3"
          disabled={isPending}
          onClick={() => { setError(null); acceptMutation.mutate(); }}
        >
          {t("invitations.accept")}
        </Button>
        <Button
          size="sm"
          variant="secondary"
          className="px-3"
          disabled={isPending}
          onClick={() => { setError(null); declineMutation.mutate(); }}
        >
          {t("invitations.decline")}
        </Button>
      </div>
    </div>
  );
}

// ─── popover ──────────────────────────────────────────────────────────────────

export function NotificationsPopover() {
  const { t } = useTranslation();
  const { isAuthenticated } = useAuth();
  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const [pos, setPos] = useState({ top: 0, right: 0 });

  const { data: invitations = [] } = useQuery({
    queryKey: ["my-invitations"],
    queryFn: getMyInvitations,
    enabled: isAuthenticated,
    refetchInterval: 30_000,
  });

  const count = invitations.length;

  useEffect(() => {
    if (!open || !triggerRef.current) return;
    const rect = triggerRef.current.getBoundingClientRect();
    setPos({
      top: rect.bottom + 4,
      right: window.innerWidth - rect.right,
    });
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      const target = e.target as Node;
      if (triggerRef.current?.contains(target) || dropdownRef.current?.contains(target)) return;
      setOpen(false);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [open]);

  return (
    <>
      <button
        ref={triggerRef}
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-label={t("notifications.title")}
        title={t("notifications.title")}
        className={cn(
          "relative flex h-9 w-9 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground",
          open && "bg-accent text-accent-foreground",
        )}
      >
        <Bell className="h-4 w-4" aria-hidden="true" />
        {count > 0 && (
          <span
            aria-label={String(count)}
            className="absolute right-1.5 top-1.5 h-2 w-2 rounded-full bg-blue-500"
          />
        )}
      </button>

      {open && createPortal(
        <div
          ref={dropdownRef}
          style={{ top: pos.top, right: pos.right }}
          className="fixed z-50 w-72 rounded-md border border-border bg-background shadow-md"
        >
          <p className="border-b border-border px-3 py-2 text-xs font-medium text-muted-foreground">
            {t("notifications.title")}
          </p>

          {count === 0 ? (
            <p className="px-3 py-4 text-center text-sm text-muted-foreground">
              {t("notifications.noItems")}
            </p>
          ) : (
            <ul>
              {invitations.map((inv, i) => (
                <li key={inv.id} className={cn(i > 0 && "border-t border-border")}>
                  <InvitationRow invitation={inv} />
                </li>
              ))}
            </ul>
          )}
        </div>,
        document.body,
      )}
    </>
  );
}
