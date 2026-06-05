import { useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { Check, ChevronsUpDown, Landmark } from "lucide-react";
import { cn } from "@/lib/utils";
import { useWorkspace } from "@/features/workspaces/WorkspaceContext";
import { useClickOutside } from "@/hooks/useClickOutside";

export function WorkspaceSwitcher() {
  const { t } = useTranslation();
  const { workspaces, activeWorkspace, setActiveWorkspace } = useWorkspace();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useClickOutside(ref, () => setOpen(false));

  const hasMultiple = workspaces.length > 1;

  return (
    <div ref={ref} className="relative mb-6 px-2">
      <button
        type="button"
        onClick={() => hasMultiple && setOpen((prev) => !prev)}
        className={cn(
          "flex w-full items-center gap-3 rounded-md px-2 py-1.5 text-left transition-colors",
          hasMultiple && "hover:bg-accent",
        )}
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
        {hasMultiple && (
          <ChevronsUpDown className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden="true" />
        )}
      </button>

      {open && (
        <div className="absolute left-2 right-2 top-full z-50 mt-1 rounded-md border border-border bg-background shadow-md">
          <ul role="menu" aria-label={t("workspace.listLabel")}>
            {workspaces.map((workspace) => (
              <li key={workspace.id} role="none">
                <button
                  type="button"
                  onClick={() => {
                    setActiveWorkspace(workspace);
                    setOpen(false);
                  }}
                  role="menuitem"
                  className="flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-accent"
                >
                  <span className="flex-1 truncate">{workspace.name}</span>
                  {workspace.id === activeWorkspace?.id && (
                    <Check className="h-4 w-4 shrink-0" aria-hidden="true" />
                  )}
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
