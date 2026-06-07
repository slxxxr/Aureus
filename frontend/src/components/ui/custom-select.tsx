import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { Check, ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";

export type SelectOption = {
  value: string;
  label: string;
};

type DropdownPos = { top: number; left: number; width: number };

function useDropdownPos(
  triggerRef: React.RefObject<HTMLElement | null>,
  open: boolean,
): DropdownPos {
  const [pos, setPos] = useState<DropdownPos>({ top: 0, left: 0, width: 0 });
  useEffect(() => {
    if (!open || !triggerRef.current) return;
    const rect = triggerRef.current.getBoundingClientRect();
    setPos({ top: rect.bottom + 4, left: rect.left, width: rect.width });
  }, [open, triggerRef]);
  return pos;
}

function useOutsideClose(
  triggerRef: React.RefObject<HTMLElement | null>,
  dropdownRef: React.RefObject<HTMLElement | null>,
  open: boolean,
  onClose: () => void,
) {
  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      const t = e.target as Node;
      if (triggerRef.current?.contains(t) || dropdownRef.current?.contains(t)) return;
      onClose();
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [open, triggerRef, dropdownRef, onClose]);
}

// ─── single select ────────────────────────────────────────────────────────────

export function CustomSelect({
  value,
  onChange,
  options,
  placeholder,
  disabled,
  className,
}: {
  value: string;
  onChange: (v: string) => void;
  options: SelectOption[];
  placeholder?: string;
  disabled?: boolean;
  className?: string;
}) {
  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const pos = useDropdownPos(triggerRef, open);

  useOutsideClose(triggerRef, dropdownRef, open, () => setOpen(false));

  const selected = options.find((o) => o.value === value);

  return (
    <div className={cn("relative", className)}>
      <button
        ref={triggerRef}
        type="button"
        onClick={() => !disabled && setOpen((v) => !v)}
        disabled={disabled}
        className="flex h-9 w-full items-center justify-between rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
      >
        <span className={cn("truncate text-left", !selected && "text-muted-foreground")}>
          {selected ? selected.label : (placeholder ?? "")}
        </span>
        <ChevronDown
          className={cn(
            "ml-2 h-4 w-4 shrink-0 text-muted-foreground transition-transform",
            open && "rotate-180",
          )}
          aria-hidden="true"
        />
      </button>

      {open &&
        createPortal(
          <div
            ref={dropdownRef}
            style={{ position: "fixed", top: pos.top, left: pos.left, width: pos.width, zIndex: 9999 }}
            className="max-h-56 overflow-auto rounded-md border border-border bg-background py-1 shadow-md"
          >
            {options.map((opt) => (
              <button
                key={opt.value}
                type="button"
                onClick={() => { onChange(opt.value); setOpen(false); }}
                className={cn(
                  "flex w-full items-center gap-2 px-3 py-2 text-sm transition-colors",
                  opt.value === value ? "bg-accent" : "hover:bg-accent/60",
                )}
              >
                <Check
                  className={cn(
                    "h-3.5 w-3.5 shrink-0 text-muted-foreground",
                    opt.value !== value && "invisible",
                  )}
                  aria-hidden="true"
                />
                <span className="flex-1 truncate text-left">{opt.label}</span>
              </button>
            ))}
          </div>,
          document.body,
        )}
    </div>
  );
}

// ─── multi select ─────────────────────────────────────────────────────────────

export function MultiSelect({
  values,
  onChange,
  options,
  allLabel,
  disabled,
  className,
}: {
  values: string[];
  onChange: (v: string[]) => void;
  options: SelectOption[];
  allLabel: string;
  disabled?: boolean;
  className?: string;
}) {
  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const pos = useDropdownPos(triggerRef, open);

  useOutsideClose(triggerRef, dropdownRef, open, () => setOpen(false));

  const isAll = values.length === 0;

  const triggerLabel = isAll
    ? allLabel
    : values.length === 1
      ? (options.find((o) => o.value === values[0])?.label ?? allLabel)
      : `${values.length} счёта`;

  const toggle = (v: string) => {
    if (values.includes(v)) {
      const next = values.filter((x) => x !== v);
      onChange(next);
    } else {
      onChange([...values, v]);
    }
  };

  return (
    <div className={cn("relative", className)}>
      <button
        ref={triggerRef}
        type="button"
        onClick={() => !disabled && setOpen((v) => !v)}
        disabled={disabled}
        className="flex h-8 w-full items-center justify-between rounded-md border border-input bg-transparent px-2 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
      >
        <span className="truncate text-left">{triggerLabel}</span>
        <ChevronDown
          className={cn(
            "ml-1 h-3.5 w-3.5 shrink-0 text-muted-foreground transition-transform",
            open && "rotate-180",
          )}
          aria-hidden="true"
        />
      </button>

      {open &&
        createPortal(
          <div
            ref={dropdownRef}
            style={{ position: "fixed", top: pos.top, left: pos.left, width: pos.width, zIndex: 9999 }}
            className="max-h-56 overflow-auto rounded-md border border-border bg-background py-1 shadow-md"
          >
            {/* "Все" option — clears selection */}
            <button
              type="button"
              onClick={() => { onChange([]); setOpen(false); }}
              className={cn(
                "flex w-full items-center gap-2 px-3 py-2 text-sm transition-colors",
                isAll ? "bg-accent" : "hover:bg-accent/60",
              )}
            >
              <Check
                className={cn("h-3.5 w-3.5 shrink-0 text-muted-foreground", !isAll && "invisible")}
                aria-hidden="true"
              />
              <span className="flex-1 truncate text-left">{allLabel}</span>
            </button>

            {options.map((opt) => {
              const checked = values.includes(opt.value);
              return (
                <button
                  key={opt.value}
                  type="button"
                  onClick={() => toggle(opt.value)}
                  className={cn(
                    "flex w-full items-center gap-2 px-3 py-2 text-sm transition-colors",
                    checked ? "bg-accent/60" : "hover:bg-accent/60",
                  )}
                >
                  <Check
                    className={cn("h-3.5 w-3.5 shrink-0 text-muted-foreground", !checked && "invisible")}
                    aria-hidden="true"
                  />
                  <span className="flex-1 truncate text-left">{opt.label}</span>
                </button>
              );
            })}
          </div>,
          document.body,
        )}
    </div>
  );
}
