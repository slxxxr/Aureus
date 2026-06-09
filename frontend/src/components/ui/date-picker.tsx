import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useTranslation } from "react-i18next";
import { Calendar, ChevronLeft, ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";

const DROPDOWN_WIDTH = 252;

function toDateKey(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function formatDisplay(dateKey: string): string {
  const [y, m, d] = dateKey.split("-");
  return `${d}.${m}.${y}`;
}

function buildCells(year: number, month: number): { date: Date; current: boolean }[] {
  const first = new Date(year, month, 1);
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const startOffset = (first.getDay() + 6) % 7; // 0=Mon, 6=Sun

  const cells: { date: Date; current: boolean }[] = [];

  for (let i = startOffset - 1; i >= 0; i--) {
    cells.push({ date: new Date(year, month, -i), current: false });
  }
  for (let d = 1; d <= daysInMonth; d++) {
    cells.push({ date: new Date(year, month, d), current: true });
  }
  let next = 1;
  while (cells.length < 42) {
    cells.push({ date: new Date(year, month + 1, next++), current: false });
  }
  return cells;
}

export function DatePicker({
  value,
  onChange,
  disabled,
  placeholder,
}: {
  value: string;
  onChange: (v: string) => void;
  disabled?: boolean;
  placeholder?: string;
}) {
  const { t } = useTranslation();
  const MONTHS = t("common.datePicker.months", { returnObjects: true }) as string[];
  const DAYS = t("common.datePicker.days", { returnObjects: true }) as string[];

  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const now = new Date();
  const [viewYear, setViewYear] = useState(
    () => (value ? parseInt(value.slice(0, 4)) : now.getFullYear()),
  );
  const [viewMonth, setViewMonth] = useState(
    () => (value ? parseInt(value.slice(5, 7)) - 1 : now.getMonth()),
  );

  const [pos, setPos] = useState({ top: 0, left: 0 });

  const todayKey = toDateKey(now);

  const handleOpen = () => {
    if (disabled) return;
    if (!open && triggerRef.current) {
      const rect = triggerRef.current.getBoundingClientRect();
      const dropdownHeight = 290;
      const top =
        window.innerHeight - rect.bottom > dropdownHeight
          ? rect.bottom + 4
          : rect.top - dropdownHeight - 4;
      const left = Math.max(8, rect.right - DROPDOWN_WIDTH);
      setPos({ top, left });
    }
    setOpen((v) => !v);
  };

  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      const t = e.target as Node;
      if (triggerRef.current?.contains(t) || dropdownRef.current?.contains(t)) return;
      setOpen(false);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [open]);

  const prevMonth = () => {
    if (viewMonth === 0) { setViewMonth(11); setViewYear((y) => y - 1); }
    else setViewMonth((m) => m - 1);
  };
  const nextMonth = () => {
    if (viewMonth === 11) { setViewMonth(0); setViewYear((y) => y + 1); }
    else setViewMonth((m) => m + 1);
  };

  const selectDay = (date: Date) => {
    const key = toDateKey(date);
    onChange(key);
    setViewYear(date.getFullYear());
    setViewMonth(date.getMonth());
    setOpen(false);
  };

  const cells = buildCells(viewYear, viewMonth);

  return (
    <div className="relative">
      <button
        ref={triggerRef}
        type="button"
        onClick={handleOpen}
        disabled={disabled}
        className="flex h-9 w-full items-center justify-between gap-2 rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
      >
        <span className={cn(!value && "text-muted-foreground")}>
          {value ? formatDisplay(value) : (placeholder ?? t("common.datePicker.placeholder"))}
        </span>
        <Calendar className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden="true" />
      </button>

      {open &&
        createPortal(
          <div
            ref={dropdownRef}
            style={{ position: "fixed", top: pos.top, left: pos.left, zIndex: 9999, width: DROPDOWN_WIDTH }}
            className="rounded-lg border border-border bg-background p-3 shadow-md"
          >
            <div className="mb-3 flex items-center justify-between">
              <button
                type="button"
                onClick={prevMonth}
                className="flex h-7 w-7 items-center justify-center rounded hover:bg-accent"
                aria-label={t("common.datePicker.prevMonth")}
              >
                <ChevronLeft className="h-4 w-4" aria-hidden="true" />
              </button>
              <span className="text-sm font-medium">
                {MONTHS[viewMonth]} {viewYear}
              </span>
              <button
                type="button"
                onClick={nextMonth}
                className="flex h-7 w-7 items-center justify-center rounded hover:bg-accent"
                aria-label={t("common.datePicker.nextMonth")}
              >
                <ChevronRight className="h-4 w-4" aria-hidden="true" />
              </button>
            </div>

            <div className="mb-1 grid grid-cols-7">
              {DAYS.map((d) => (
                <div
                  key={d}
                  className="py-1 text-center text-xs font-medium text-muted-foreground"
                >
                  {d}
                </div>
              ))}
            </div>

            <div className="grid grid-cols-7">
              {cells.map(({ date, current }, i) => {
                const key = toDateKey(date);
                const isSelected = key === value;
                const isToday = key === todayKey;
                return (
                  <button
                    key={i}
                    type="button"
                    onClick={() => selectDay(date)}
                    className={cn(
                      "h-8 w-full rounded text-sm transition-colors",
                      !current && "text-muted-foreground/40",
                      current && !isSelected && "hover:bg-accent",
                      isSelected && "bg-foreground text-background",
                      isToday && !isSelected && "font-semibold",
                    )}
                  >
                    {date.getDate()}
                  </button>
                );
              })}
            </div>
          </div>,
          document.body,
        )}
    </div>
  );
}
