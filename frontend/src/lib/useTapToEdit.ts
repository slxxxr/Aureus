import { useRef } from "react";

export function useTapToEdit(onEdit: () => void, enabled = true) {
  const pressStart = useRef(0);
  if (!enabled) {
    return {};
  }
  return {
    onPointerDown: () => { pressStart.current = Date.now(); },
    onClick: () => { if (Date.now() - pressStart.current < 400) onEdit(); },
  };
}
