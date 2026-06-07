import { createPortal } from "react-dom";
import type { ReactNode } from "react";

export function Modal({ children, onBackdropClick }: { children: ReactNode; onBackdropClick: () => void }) {
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
