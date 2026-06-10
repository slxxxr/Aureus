// Categorical palette for category-keyed charts (dynamics, future pie). Colors are assigned
// by position in a caller-sorted list, so the largest categories get stable, distinct hues.
export const CATEGORY_PALETTE = [
  "#2563eb",
  "#dc2626",
  "#16a34a",
  "#d97706",
  "#7c3aed",
  "#0891b2",
  "#db2777",
  "#65a30d",
  "#ea580c",
  "#0d9488",
  "#9333ea",
  "#ca8a04",
  "#4f46e5",
  "#e11d48",
] as const;

// Aggregated soft-deleted categories render as a single neutral series.
export const DELETED_CATEGORY_COLOR = "#9ca3af";

export function colorForIndex(index: number): string {
  return CATEGORY_PALETTE[index % CATEGORY_PALETTE.length];
}
