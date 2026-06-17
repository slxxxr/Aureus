import { useState, useEffect, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { UserMinus } from "lucide-react";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { useWorkspace } from "@/features/workspaces/WorkspaceContext";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { CustomSelect } from "@/components/ui/custom-select";
import { cn } from "@/lib/utils";
import { InputLimits } from "@/lib/inputLimits";
import {
  getWorkspaceMembers,
  removeWorkspaceMember,
  updateMemberRole,
  transferOwnership,
  leaveWorkspace,
  type WorkspaceMember,
} from "@/features/workspaces/workspaceMembersApi";
import {
  getWorkspaceInvitations,
  inviteMember,
  revokeInvitation,
} from "@/features/workspaces/invitationsApi";
import {
  updateWorkspace,
  deleteWorkspace,
  type Workspace,
  type WorkspaceRole,
} from "@/features/workspaces/workspacesApi";
import { resolveWorkspaceMemberError } from "@/features/workspaces/resolveWorkspaceMemberError";
import { resolveInvitationError } from "@/features/workspaces/resolveInvitationError";
import { resolveWorkspaceError } from "@/features/workspaces/resolveWorkspaceError";

type Tab = "general" | "members" | "invitations";

// ─── role badge ───────────────────────────────────────────────────────────────

function RoleBadge({ role, t }: { role: WorkspaceRole; t: TFunction }) {
  return (
    <span className="shrink-0 rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">
      {t(`workspaceMembers.roles.${role}`)}
    </span>
  );
}

// ─── member avatar ────────────────────────────────────────────────────────────

function MemberAvatar({ name }: { name: string }) {
  return (
    <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-medium text-muted-foreground">
      {name.charAt(0).toUpperCase()}
    </div>
  );
}

// ─── general tab ─────────────────────────────────────────────────────────────

function GeneralTab({ workspace, onClose }: { workspace: Workspace; onClose: () => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [name, setName] = useState(workspace.name);
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const invalidateWorkspaces = () =>
    queryClient.invalidateQueries({ queryKey: ["workspaces"] });

  const renameMutation = useMutation({
    mutationFn: () => updateWorkspace(workspace.id, { name: name.trim() }),
    onSuccess: () => { void invalidateWorkspaces(); setError(null); },
    onError: (err) => setError(resolveWorkspaceError(err, t)),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteWorkspace(workspace.id),
    onSuccess: async () => { await invalidateWorkspaces(); onClose(); },
    onError: (err) => setError(resolveWorkspaceError(err, t)),
  });

  if (confirmingDelete) {
    return (
      <div>
        <p className="mb-1 text-sm font-medium">{t("workspace.deleteConfirm.title")}</p>
        <p className="mb-4 text-sm text-muted-foreground">{t("workspace.deleteConfirm.description")}</p>
        {error !== null && (
          <p className="mb-3 text-sm text-destructive" role="alert">{error}</p>
        )}
        <div className="flex justify-end gap-2">
          <Button
            variant="secondary"
            size="sm"
            onClick={() => { setConfirmingDelete(false); setError(null); }}
            disabled={deleteMutation.isPending}
          >
            {t("common.cancel")}
          </Button>
          <Button
            size="sm"
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            disabled={deleteMutation.isPending}
            onClick={() => deleteMutation.mutate()}
          >
            {t("common.delete")}
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <form
        onSubmit={(e: FormEvent) => { e.preventDefault(); renameMutation.mutate(); }}
        className="space-y-3"
      >
        <div className="space-y-1.5">
          <Label htmlFor="settings-workspace-name">
            {t("workspaceSettings.general.nameLabel")}
          </Label>
          <div className="flex gap-2">
            <Input
              id="settings-workspace-name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              autoComplete="off"
              maxLength={InputLimits.workspaceNameMaxLength}
              disabled={renameMutation.isPending}
            />
            <Button
              type="submit"
              disabled={renameMutation.isPending || !name.trim() || name.trim() === workspace.name}
            >
              {renameMutation.isPending
                ? t("workspaceSettings.general.saving")
                : t("workspaceSettings.general.save")}
            </Button>
          </div>
        </div>
        {error !== null && renameMutation.isError && (
          <p className="text-sm text-destructive" role="alert">{error}</p>
        )}
      </form>

      {workspace.role === "Owner" && (
        <div className="pt-2">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => { setError(null); setConfirmingDelete(true); }}
            disabled={deleteMutation.isPending}
            className="text-destructive hover:bg-destructive/10 hover:text-destructive"
          >
            {t("workspaceSettings.general.deleteWorkspace")}
          </Button>
        </div>
      )}
    </div>
  );
}

// ─── members tab ──────────────────────────────────────────────────────────────

function MembersTab({
  workspaceId,
  currentRole,
  currentUserId,
  onLeft,
}: {
  workspaceId: string;
  currentRole: WorkspaceRole;
  currentUserId: string;
  onLeft: () => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [confirmRemoveId, setConfirmRemoveId] = useState<string | null>(null);
  const [removingId, setRemovingId] = useState<string | null>(null);
  const [updatingRoleId, setUpdatingRoleId] = useState<string | null>(null);
  const [transferring, setTransferring] = useState(false);
  const [transferTargetId, setTransferTargetId] = useState("");
  const [memberError, setMemberError] = useState<string | null>(null);

  const { data: members = [], isLoading } = useQuery({
    queryKey: ["workspace-members", workspaceId],
    queryFn: () => getWorkspaceMembers(workspaceId),
  });

  const invalidateMembers = () =>
    queryClient.invalidateQueries({ queryKey: ["workspace-members", workspaceId] });

  const removeMutation = useMutation({
    mutationFn: (userId: string) => removeWorkspaceMember(workspaceId, userId),
    onSuccess: async () => { await invalidateMembers(); setRemovingId(null); setMemberError(null); },
    onError: (err) => { setMemberError(resolveWorkspaceMemberError(err, t)); setRemovingId(null); },
  });

  const updateRoleMutation = useMutation({
    mutationFn: ({ userId, role }: { userId: string; role: WorkspaceRole }) =>
      updateMemberRole(workspaceId, userId, role),
    onSuccess: async () => { await invalidateMembers(); setUpdatingRoleId(null); setMemberError(null); },
    onError: (err) => { setMemberError(resolveWorkspaceMemberError(err, t)); setUpdatingRoleId(null); },
  });

  const transferMutation = useMutation({
    mutationFn: (toUserId: string) => transferOwnership(workspaceId, toUserId),
    onSuccess: async () => {
      await Promise.all([
        invalidateMembers(),
        queryClient.invalidateQueries({ queryKey: ["workspaces"] }),
      ]);
      setTransferring(false);
      setTransferTargetId("");
      setMemberError(null);
    },
    onError: (err) => setMemberError(resolveWorkspaceMemberError(err, t)),
  });

  const leaveMutation = useMutation({
    mutationFn: () => leaveWorkspace(workspaceId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["workspaces"] });
      onLeft();
    },
    onError: (err) => setMemberError(resolveWorkspaceMemberError(err, t)),
  });

  function canRemove(member: WorkspaceMember): boolean {
    if (member.userId === currentUserId) return false;
    if (member.role === "Owner") return false;
    if (currentRole === "Owner") return true;
    if (currentRole === "Manager" && member.role === "Member") return true;
    return false;
  }

  const roleOptions = [
    { value: "Member", label: t("workspaceMembers.roles.Member") },
    { value: "Manager", label: t("workspaceMembers.roles.Manager") },
  ];

  const transferEligible = members.filter(
    (m) => m.userId !== currentUserId && m.role !== "Owner",
  );

  const transferOptions = transferEligible.map((m) => ({ value: m.userId, label: m.name }));

  if (isLoading) {
    return <p className="py-4 text-center text-sm text-muted-foreground">…</p>;
  }

  return (
    <div className="space-y-3">
      {memberError !== null && (
        <p className="text-sm text-destructive" role="alert">{memberError}</p>
      )}

      <ul>
        {members.map((member) => {
          const isMe = member.userId === currentUserId;
          const isUpdatingRole = updatingRoleId === member.userId;
          const isConfirmingRemove = confirmRemoveId === member.userId;
          const isRemoving = removingId === member.userId;

          return (
            <li key={member.userId} className="flex items-center gap-3 py-2.5">
              <MemberAvatar name={member.name} />

              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-1.5">
                  <span className="truncate text-sm font-medium">{member.name}</span>
                  {isMe && (
                    <span className="shrink-0 rounded-full bg-muted px-1.5 py-0.5 text-[10px] text-muted-foreground">
                      {t("workspaceMembers.you")}
                    </span>
                  )}
                </div>
                <p className="truncate text-xs text-muted-foreground">{member.email}</p>
              </div>

              {isConfirmingRemove ? (
                <div className="flex shrink-0 items-center gap-1.5">
                  <Button
                    size="sm"
                    className="h-7 bg-destructive px-2 text-xs text-destructive-foreground hover:bg-destructive/90"
                    disabled={isRemoving}
                    onClick={() => {
                      setRemovingId(member.userId);
                      setMemberError(null);
                      setConfirmRemoveId(null);
                      removeMutation.mutate(member.userId);
                    }}
                  >
                    {t("common.delete")}
                  </Button>
                  <Button
                    size="sm"
                    variant="ghost"
                    className="h-7 px-2 text-xs"
                    disabled={isRemoving}
                    onClick={() => setConfirmRemoveId(null)}
                  >
                    {t("common.cancel")}
                  </Button>
                </div>
              ) : (
                <>
                  {currentRole === "Owner" && !isMe && member.role !== "Owner" ? (
                    <div className="w-36 shrink-0">
                      <CustomSelect
                        value={member.role}
                        disabled={isUpdatingRole}
                        options={roleOptions}
                        onChange={(role) => {
                          setUpdatingRoleId(member.userId);
                          setMemberError(null);
                          updateRoleMutation.mutate({ userId: member.userId, role: role as WorkspaceRole });
                        }}
                      />
                    </div>
                  ) : (
                    <RoleBadge role={member.role} t={t} />
                  )}

                  {canRemove(member) && (
                    <button
                      type="button"
                      disabled={isRemoving || removeMutation.isPending}
                      onClick={() => {
                        setMemberError(null);
                        setConfirmRemoveId(member.userId);
                      }}
                      aria-label={t("workspaceMembers.removeConfirm.title")}
                      className="shrink-0 rounded p-1 text-muted-foreground transition-colors hover:text-destructive disabled:opacity-50"
                    >
                      <UserMinus className="h-3.5 w-3.5" aria-hidden="true" />
                    </button>
                  )}
                </>
              )}
            </li>
          );
        })}
      </ul>

      <div className="flex flex-wrap gap-2 pt-1">
        {currentRole === "Owner" && (
          transferring ? (
            <div className="flex w-full flex-col gap-2">
              <p className="text-xs text-muted-foreground">
                {t("workspaceMembers.transferModal.description")}
              </p>
              <div className="flex gap-2">
                <CustomSelect
                  value={transferTargetId}
                  onChange={setTransferTargetId}
                  options={transferOptions}
                  placeholder={t("workspaceMembers.transferModal.selectPlaceholder")}
                  className="flex-1"
                />
                <Button
                  disabled={!transferTargetId || transferMutation.isPending}
                  onClick={() => { setMemberError(null); transferMutation.mutate(transferTargetId); }}
                >
                  {transferMutation.isPending
                    ? t("workspaceMembers.transferModal.submitting")
                    : t("workspaceMembers.transferModal.submit")}
                </Button>
                <Button
                  variant="secondary"
                  onClick={() => { setTransferring(false); setTransferTargetId(""); setMemberError(null); }}
                  disabled={transferMutation.isPending}
                >
                  {t("common.cancel")}
                </Button>
              </div>
            </div>
          ) : (
            <Button
              size="sm"
              variant="secondary"
              onClick={() => { setTransferring(true); setMemberError(null); }}
            >
              {t("workspaceMembers.transferModal.title")}
            </Button>
          )
        )}

        {currentRole !== "Owner" && (
          <Button
            size="sm"
            variant="ghost"
            disabled={leaveMutation.isPending}
            onClick={() => { setMemberError(null); leaveMutation.mutate(); }}
            className="text-destructive hover:bg-destructive/10 hover:text-destructive"
          >
            {t("workspaceMembers.leave")}
          </Button>
        )}
      </div>
    </div>
  );
}

// ─── invitations tab ──────────────────────────────────────────────────────────

function InvitationsTab({ workspaceId }: { workspaceId: string }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const [email, setEmail] = useState("");
  const [inviteError, setInviteError] = useState<string | null>(null);
  const [inviteSuccess, setInviteSuccess] = useState(false);
  const [revokingId, setRevokingId] = useState<string | null>(null);
  const [revokeError, setRevokeError] = useState<string | null>(null);

  const { data: invitations = [] } = useQuery({
    queryKey: ["workspace-invitations", workspaceId],
    queryFn: () => getWorkspaceInvitations(workspaceId),
  });

  const invalidateInvitations = () =>
    queryClient.invalidateQueries({ queryKey: ["workspace-invitations", workspaceId] });

  const inviteMutation = useMutation({
    mutationFn: () => inviteMember(workspaceId, email.trim(), i18n.language),
    onSuccess: async () => {
      await invalidateInvitations();
      setEmail("");
      setInviteError(null);
      setInviteSuccess(true);
      setTimeout(() => setInviteSuccess(false), 3000);
    },
    onError: (err) => { setInviteError(resolveInvitationError(err, t)); setInviteSuccess(false); },
  });

  const revokeMutation = useMutation({
    mutationFn: (invitationId: string) => revokeInvitation(workspaceId, invitationId),
    onSuccess: async () => { await invalidateInvitations(); setRevokingId(null); setRevokeError(null); },
    onError: (err) => { setRevokeError(resolveInvitationError(err, t)); setRevokingId(null); },
  });

  return (
    <div className="space-y-4">
      <form
        onSubmit={(e: FormEvent) => { e.preventDefault(); setInviteSuccess(false); inviteMutation.mutate(); }}
        className="space-y-2"
      >
        <Label htmlFor="invite-email">{t("invitations.inviteForm.label")}</Label>
        <div className="flex gap-2">
          <Input
            id="invite-email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder={t("invitations.inviteForm.placeholder")}
            autoComplete="off"
            maxLength={InputLimits.emailMaxLength}
            disabled={inviteMutation.isPending}
          />
          <Button
            type="submit"
            disabled={inviteMutation.isPending || !email.trim()}
          >
            {inviteMutation.isPending
              ? t("invitations.inviteForm.submitting")
              : t("invitations.inviteForm.submit")}
          </Button>
        </div>
        {inviteError !== null && (
          <p className="text-sm text-destructive" role="alert">{inviteError}</p>
        )}
        {inviteSuccess && (
          <p className="text-sm text-green-600">{t("invitations.inviteForm.success")}</p>
        )}
      </form>

      <div>
        <p className="mb-2 text-xs font-medium text-muted-foreground">
          {t("invitations.pendingLabel")}
        </p>
        {revokeError !== null && (
          <p className="mb-2 text-sm text-destructive" role="alert">{revokeError}</p>
        )}
        {invitations.length === 0 ? (
          <p className="text-sm text-muted-foreground">{t("invitations.noPending")}</p>
        ) : (
          <ul>
            {invitations.map((inv) => {
              const expiresDate = new Date(inv.expiresAt).toLocaleDateString(undefined, {
                day: "2-digit",
                month: "2-digit",
                year: "numeric",
              });

              return (
                <li key={inv.id} className="flex items-center gap-3 py-2">
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm">{inv.email}</p>
                    <p className="text-xs text-muted-foreground">
                      {t("invitations.expires", { date: expiresDate })}
                    </p>
                  </div>
                  <button
                    type="button"
                    disabled={revokingId === inv.id || revokeMutation.isPending}
                    onClick={() => {
                      setRevokingId(inv.id);
                      setRevokeError(null);
                      revokeMutation.mutate(inv.id);
                    }}
                    className="shrink-0 text-xs text-muted-foreground hover:text-destructive disabled:opacity-50"
                  >
                    {t("invitations.revoke")}
                  </button>
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </div>
  );
}

// ─── workspace settings modal ─────────────────────────────────────────────────

export function WorkspaceSettingsModal({
  workspace,
  onClose,
}: {
  workspace: Workspace;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const currentUser = useCurrentUser();
  const currentUserId = currentUser?.userId ?? "";

  const { workspaces } = useWorkspace();
  const liveWorkspace = workspaces.find((w) => w.id === workspace.id) ?? workspace;
  const currentRole = liveWorkspace.role;

  const isManagerOrAbove = currentRole === "Owner" || currentRole === "Manager";

  const visibleTabs: Tab[] = [
    ...(isManagerOrAbove ? (["general"] as Tab[]) : []),
    "members",
    ...(isManagerOrAbove ? (["invitations"] as Tab[]) : []),
  ];

  const defaultTab: Tab = currentRole === "Owner" ? "general" : "members";
  const [activeTab, setActiveTab] = useState<Tab>(defaultTab);

  useEffect(() => {
    if (!visibleTabs.includes(activeTab)) {
      setActiveTab(visibleTabs[0] ?? "members");
    }
  }, [currentRole]);

  const tabLabel: Record<Tab, string> = {
    general: t("workspaceSettings.tabs.general"),
    members: t("workspaceSettings.tabs.members"),
    invitations: t("workspaceSettings.tabs.invitations"),
  };

  return (
    <Modal onBackdropClick={onClose} size="md">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold">{t("workspaceSettings.title")}</h2>
        <p className="text-sm text-muted-foreground">{liveWorkspace.name}</p>
      </div>

      {visibleTabs.length > 1 && (
        <div className="mb-4 flex gap-1 border-b border-border">
          {visibleTabs.map((tab) => (
            <button
              key={tab}
              type="button"
              onClick={() => setActiveTab(tab)}
              className={cn(
                "px-3 pb-2 text-sm transition-colors",
                activeTab === tab
                  ? "border-b-2 border-foreground font-medium text-foreground"
                  : "text-muted-foreground hover:text-foreground",
              )}
            >
              {tabLabel[tab]}
            </button>
          ))}
        </div>
      )}

      {activeTab === "general" && (
        <GeneralTab workspace={liveWorkspace} onClose={onClose} />
      )}
      {activeTab === "members" && (
        <MembersTab
          workspaceId={workspace.id}
          currentRole={currentRole}
          currentUserId={currentUserId}
          onLeft={onClose}
        />
      )}
      {activeTab === "invitations" && (
        <InvitationsTab workspaceId={workspace.id} />
      )}
    </Modal>
  );
}
