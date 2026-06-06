import { useRef, useState, type FormEvent } from "react";
import { createPortal } from "react-dom";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Check, ChevronDown, Landmark, Pencil, Plus } from "lucide-react";
import { cn } from "@/lib/utils";
import { useWorkspace } from "@/features/workspaces/WorkspaceContext";
import { useClickOutside } from "@/hooks/useClickOutside";
import {
  createWorkspace,
  updateWorkspace,
  type Workspace,
} from "@/features/workspaces/workspacesApi";
import { resolveWorkspaceError } from "@/features/workspaces/resolveWorkspaceError";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

// ─── modal wrapper ────────────────────────────────────────────────────────────

function Modal({ children, onBackdropClick }: { children: React.ReactNode; onBackdropClick: () => void }) {
  return createPortal(
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onClick={(e) => { if (e.target === e.currentTarget) onBackdropClick(); }}
    >
      <div className="w-full max-w-sm rounded-lg border border-border bg-background p-6 shadow-lg">
        {children}
      </div>
    </div>,
    document.body,
  );
}

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

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    mutation.mutate({ name: name.trim() });
  };

  return (
    <Modal onBackdropClick={onClose}>
      <h2 className="mb-5 text-base font-semibold">{t("workspace.createModal.title")}</h2>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="create-workspace-name">{t("workspace.createModal.nameLabel")}</Label>
          <Input
            id="create-workspace-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder={t("workspace.createModal.namePlaceholder")}
            required
            autoFocus
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

  const mutation = useMutation({
    mutationFn: () => updateWorkspace(workspace.id, { name: name.trim() }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["workspaces"] });
      onClose();
    },
  });

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    mutation.mutate();
  };

  return (
    <Modal onBackdropClick={onClose}>
      <h2 className="mb-5 text-base font-semibold">{t("workspace.editModal.title")}</h2>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="edit-workspace-name">{t("workspace.editModal.nameLabel")}</Label>
          <Input
            id="edit-workspace-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            autoFocus
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
          <Button
            type="submit"
            disabled={mutation.isPending || !name.trim() || name.trim() === workspace.name}
          >
            {mutation.isPending ? t("workspace.editModal.saving") : t("workspace.editModal.save")}
          </Button>
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

  const isOwner = activeWorkspace?.role === "Owner";

  const dropdown = open && (
    <div className="absolute left-0 top-full z-50 mt-1 w-52 rounded-md border border-border bg-background shadow-md">
      <ul role="menu" aria-label={t("workspace.listLabel")}>
        {workspaces.map((workspace) => (
          <li key={workspace.id} role="none">
            <button
              type="button"
              onClick={() => { setActiveWorkspace(workspace); setOpen(false); }}
              role="menuitem"
              className="flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-accent"
            >
              <span className="flex-1 truncate text-left">{workspace.name}</span>
              <Check
                className={cn("h-4 w-4 shrink-0", workspace.id !== activeWorkspace?.id && "invisible")}
                aria-hidden="true"
              />
            </button>
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
      <div ref={ref} className="relative">
        <button
          type="button"
          onClick={() => setOpen((prev) => !prev)}
          className="group flex w-full items-center gap-3 rounded-md px-2 py-1.5 text-left transition-colors hover:bg-accent"
          aria-label={t("workspace.switcherLabel")}
          aria-expanded={open}
        >
          <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md border border-border bg-background">
            <Landmark className="h-4 w-4" aria-hidden="true" />
          </div>
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-semibold leading-5">
              {activeWorkspace?.name ?? t("common.appName")}
            </p>
            <p className="text-xs text-muted-foreground">{t("common.appName")}</p>
          </div>
          {isOwner && activeWorkspace && (
            <span
              role="button"
              onClick={(e) => { e.stopPropagation(); setEditingWorkspace(activeWorkspace); }}
              className="shrink-0 rounded p-1 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100 hover:text-foreground focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              aria-label={t("workspace.editModal.title")}
            >
              <Pencil className="h-3.5 w-3.5" />
            </span>
          )}
          <ChevronDown
            className={cn("h-4 w-4 shrink-0 text-muted-foreground transition-transform", open && "rotate-180")}
            aria-hidden="true"
          />
        </button>
        {dropdown}
      </div>
      {modals}
    </>
  );
}
