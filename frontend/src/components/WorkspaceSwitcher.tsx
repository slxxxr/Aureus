import { InputLimits } from "@/lib/inputLimits";
import { useRef, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Check, ChevronDown, Landmark, Pencil, Plus } from "lucide-react";
import { cn } from "@/lib/utils";
import { useWorkspace } from "@/features/workspaces/WorkspaceContext";
import { useClickOutside } from "@/hooks/useClickOutside";
import {
  createWorkspace,
  deleteWorkspace,
  updateWorkspace,
  type Workspace,
} from "@/features/workspaces/workspacesApi";
import { resolveWorkspaceError } from "@/features/workspaces/resolveWorkspaceError";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Modal } from "@/components/ui/modal";

const MAX_NAME_CHARS = 24;
const truncateName = (name: string) =>
  name.length > MAX_NAME_CHARS ? `${name.slice(0, MAX_NAME_CHARS)}…` : name;

// ─── create modal ─────────────────────────────────────────────────────────────

function CreateWorkspaceModal({ onClose }: { onClose: () => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { setActiveWorkspace } = useWorkspace();
  const [name, setName] = useState("");

  const mutation = useMutation({
    mutationFn: (payload: { name: string }) => createWorkspace(payload),
    onSuccess: async (workspace) => {
      await queryClient.invalidateQueries({ queryKey: ["workspaces"] });
      setActiveWorkspace(workspace);
      onClose();
    },
  });

  return (
    <Modal onBackdropClick={onClose}>
      <h2 className="mb-5 text-base font-semibold">{t("workspace.createModal.title")}</h2>
      <form onSubmit={(e: FormEvent) => { e.preventDefault(); mutation.mutate({ name: name.trim() }); }} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="create-workspace-name">{t("workspace.createModal.nameLabel")}</Label>
          <Input
            id="create-workspace-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder={t("workspace.createModal.namePlaceholder")}
            required
            autoFocus
            autoComplete="off"
            maxLength={InputLimits.workspaceNameMaxLength}
            disabled={mutation.isPending}
          />
        </div>

        {mutation.isError && (
          <p className="text-sm text-destructive" role="alert">
            {resolveWorkspaceError(mutation.error, t as TFunction)}
          </p>
        )}

        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            {t("common.cancel")}
          </Button>
          <Button type="submit" disabled={mutation.isPending || !name.trim()}>
            {mutation.isPending ? t("workspace.createModal.submitting") : t("workspace.createModal.submit")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

// ─── edit modal ───────────────────────────────────────────────────────────────

function EditWorkspaceModal({ workspace, onClose }: { workspace: Workspace; onClose: () => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [name, setName] = useState(workspace.name);
  const [confirmingDelete, setConfirmingDelete] = useState(false);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["workspaces"] });

  const updateMutation = useMutation({
    mutationFn: () => updateWorkspace(workspace.id, { name: name.trim() }),
    onSuccess: () => { void invalidate(); onClose(); },
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteWorkspace(workspace.id),
    onSuccess: () => { void invalidate(); onClose(); },
  });

  const isPending = updateMutation.isPending || deleteMutation.isPending;

  if (confirmingDelete) {
    return (
      <Modal onBackdropClick={() => setConfirmingDelete(false)}>
        <h2 className="mb-2 text-base font-semibold">{t("workspace.deleteConfirm.title")}</h2>
        <p className="mb-5 text-sm text-muted-foreground">{t("workspace.deleteConfirm.description")}</p>
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setConfirmingDelete(false)} disabled={deleteMutation.isPending}>
            {t("common.cancel")}
          </Button>
          <Button
            disabled={deleteMutation.isPending}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            onClick={() => deleteMutation.mutate()}
          >
            {t("common.delete")}
          </Button>
        </div>
      </Modal>
    );
  }

  return (
    <Modal onBackdropClick={onClose}>
      <h2 className="mb-5 text-base font-semibold">{t("workspace.editModal.title")}</h2>
      <form onSubmit={(e: FormEvent) => { e.preventDefault(); updateMutation.mutate(); }} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="edit-workspace-name">{t("workspace.editModal.nameLabel")}</Label>
          <Input
            id="edit-workspace-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            autoFocus
            autoComplete="off"
            maxLength={InputLimits.workspaceNameMaxLength}
            disabled={isPending}
          />
        </div>

        {updateMutation.isError && (
          <p className="text-sm text-destructive" role="alert">
            {resolveWorkspaceError(updateMutation.error, t as TFunction)}
          </p>
        )}

        <div className="flex items-center justify-between pt-1">
          <button
            type="button"
            onClick={() => setConfirmingDelete(true)}
            disabled={isPending}
            className="text-sm text-destructive hover:underline disabled:opacity-50"
          >
            {t("workspace.editModal.deleteWorkspace")}
          </button>
          <div className="flex gap-2">
            <Button type="button" variant="secondary" onClick={onClose} disabled={isPending}>
              {t("common.cancel")}
            </Button>
            <Button
              type="submit"
              disabled={isPending || !name.trim() || name.trim() === workspace.name}
            >
              {updateMutation.isPending ? t("workspace.editModal.saving") : t("workspace.editModal.save")}
            </Button>
          </div>
        </div>
      </form>
    </Modal>
  );
}

// ─── workspace switcher ───────────────────────────────────────────────────────

export function WorkspaceSwitcher({ collapsed = false }: { collapsed?: boolean }) {
  const { t } = useTranslation();
  const { workspaces, activeWorkspace, setActiveWorkspace } = useWorkspace();
  const [open, setOpen] = useState(false);
  const [showCreate, setShowCreate] = useState(false);
  const [editingWorkspace, setEditingWorkspace] = useState<Workspace | null>(null);
  const ref = useRef<HTMLDivElement>(null);

  useClickOutside(ref, () => setOpen(false));

  const openEdit = (workspace: Workspace) => {
    setEditingWorkspace(workspace);
    setOpen(false);
  };

  const dropdown = open && (
    <div
      className={cn(
        "absolute left-0 top-full z-50 mt-1 rounded-md border border-border bg-background shadow-md",
        collapsed ? "min-w-[12rem]" : "w-full",
      )}
    >
      <ul role="menu" aria-label={t("workspace.listLabel")}>
        {workspaces.map((workspace) => (
          <li key={workspace.id} role="none">
            {/* Order: [name] → [pencil on hover] → [checkmark] */}
            <div className="group flex items-center hover:bg-accent">
              <button
                type="button"
                onClick={() => { setActiveWorkspace(workspace); setOpen(false); }}
                role="menuitem"
                title={workspace.name}
                className="flex min-w-0 flex-1 items-center py-2 pl-3 pr-1 text-sm"
              >
                <span className="flex-1 truncate text-left">{truncateName(workspace.name)}</span>
              </button>
              {workspace.role === "Owner" && (
                <button
                  type="button"
                  onClick={() => openEdit(workspace)}
                  className="shrink-0 rounded p-1 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100 hover:text-foreground focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                  aria-label={t("workspace.editModal.title")}
                >
                  <Pencil className="h-3.5 w-3.5" />
                </button>
              )}
              <div className="px-2">
                <Check
                  className={cn("h-4 w-4 shrink-0 text-muted-foreground", workspace.id !== activeWorkspace?.id && "invisible")}
                  aria-hidden="true"
                />
              </div>
            </div>
          </li>
        ))}
      </ul>
      <div className="border-t border-border p-1">
        <button
          type="button"
          onClick={() => { setShowCreate(true); setOpen(false); }}
          className="flex w-full items-center gap-2 rounded px-2 py-1.5 text-sm text-muted-foreground hover:bg-accent hover:text-foreground"
        >
          <Plus className="h-4 w-4" aria-hidden="true" />
          {t("workspace.newWorkspace")}
        </button>
      </div>
    </div>
  );

  const modals = (
    <>
      {showCreate && <CreateWorkspaceModal onClose={() => setShowCreate(false)} />}
      {editingWorkspace && (
        <EditWorkspaceModal workspace={editingWorkspace} onClose={() => setEditingWorkspace(null)} />
      )}
    </>
  );

  if (collapsed) {
    return (
      <>
        <div ref={ref} className="relative">
          <button
            type="button"
            onClick={() => setOpen((prev) => !prev)}
            className="flex h-9 w-9 items-center justify-center rounded-md border border-border bg-background transition-colors hover:bg-accent"
            aria-label={activeWorkspace?.name ?? t("common.appName")}
            aria-expanded={open}
            title={activeWorkspace?.name ?? t("common.appName")}
          >
            <Landmark className="h-4 w-4" aria-hidden="true" />
          </button>
          {dropdown}
        </div>
        {modals}
      </>
    );
  }

  return (
    <>
      <div ref={ref} className="relative w-full min-w-0">
        {/*
          Outer div is the hover target (group) for the pencil opacity transition.
          Main trigger button covers icon + name + chevron area.
          Pencil button is a sibling — before the chevron — no nested <button> in <button>.
        */}
        <div className="group flex w-full items-center rounded-md transition-colors hover:bg-accent">
          <button
            type="button"
            onClick={() => setOpen((prev) => !prev)}
            className="flex min-w-0 flex-1 items-center gap-3 px-2 py-1.5 text-left"
            aria-label={t("workspace.switcherLabel")}
            aria-expanded={open}
          >
            <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md border border-border bg-background">
              <Landmark className="h-4 w-4" aria-hidden="true" />
            </div>
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-semibold leading-5" title={activeWorkspace?.name}>
                {activeWorkspace ? truncateName(activeWorkspace.name) : t("common.appName")}
              </p>
              <p className="text-xs text-muted-foreground">{t("common.appName")}</p>
            </div>
          </button>

          {/* Pencil left of chevron — sibling button, not nested */}
          {activeWorkspace?.role === "Owner" && (
            <button
              type="button"
              onClick={() => openEdit(activeWorkspace)}
              className="shrink-0 rounded p-1 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100 hover:text-foreground focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              aria-label={t("workspace.editModal.title")}
            >
              <Pencil className="h-3.5 w-3.5" />
            </button>
          )}

          {/* Chevron is visual-only, toggling happens on the main button */}
          <button
            type="button"
            onClick={() => setOpen((prev) => !prev)}
            className="mr-1 shrink-0 rounded p-1 text-muted-foreground hover:text-foreground"
            tabIndex={-1}
            aria-hidden="true"
          >
            <ChevronDown
              className={cn("h-4 w-4 transition-transform", open && "rotate-180")}
            />
          </button>
        </div>
        {dropdown}
      </div>
      {modals}
    </>
  );
}
