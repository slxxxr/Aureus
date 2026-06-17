import { useEffect } from "react";
import { createPortal } from "react-dom";
import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

const sizeClass = {
  sm: "max-w-sm",
  md: "max-w-md",
} as const;

export function Modal({
  children,
  onBackdropClick,
  size = "sm",
}: {
  children: ReactNode;
  onBackdropClick: () => void;
  size?: keyof typeof sizeClass;
}) {
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape") onBackdropClick();
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [onBackdropClick]);

  return createPortal(
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onClick={(e) => { if (e.target === e.currentTarget) onBackdropClick(); }}
    >
      <div
        role="dialog"
        aria-modal="true"
        className={cn("w-full rounded-lg border border-border bg-background p-6 shadow-lg", sizeClass[size])}
      >
        {children}
      </div>
    </div>,
    document.body,
  );
}
