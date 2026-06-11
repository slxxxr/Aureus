import { DAY_MS } from "@/lib/constants";
import type { TimeInterval } from "@/features/analytics/analyticsApi";

export const INCOME_COLOR = "#22c55e";
export const EXPENSE_COLOR = "#dc2626";
export const AXIS_COLOR = "hsl(var(--muted-foreground))";
export const GRID_COLOR = "hsl(var(--border))";

export const INCOME_TONE = "text-green-600 dark:text-green-400";
export const EXPENSE_TONE = "text-destructive";

export function groupByCurrency<T extends { currency: string }>(items: T[]): Map<string, T[]> {
  const map = new Map<string, T[]>();
  for (const item of items) {
    if (!map.has(item.currency)) map.set(item.currency, []);
    map.get(item.currency)!.push(item);
  }
  return map;
}

export function netTone(net: number): string {
  if (net > 0) return INCOME_TONE;
  if (net < 0) return EXPENSE_TONE;
  return "text-foreground";
}

export function clampStart(start: Date, from?: string): Date {
  if (!from) return start;
  const f = new Date(from);
  return start.getTime() < f.getTime() ? f : start;
}

export function clampEnd(end: Date, to?: string): Date {
  if (!to) return end;
  const last = new Date(new Date(to).getTime() - DAY_MS);
  return end.getTime() > last.getTime() ? last : end;
}

export function formatAxisDate(iso: string, interval: string, from?: string): string {
  const d = clampStart(new Date(iso), from);
  const dd = String(d.getUTCDate()).padStart(2, "0");
  const mm = String(d.getUTCMonth() + 1).padStart(2, "0");
  if (interval === "Month") return `${mm}.${String(d.getUTCFullYear()).slice(2)}`;
  return `${dd}.${mm}`;
}

export function formatFullDate(d: Date): string {
  const dd = String(d.getUTCDate()).padStart(2, "0");
  const mm = String(d.getUTCMonth() + 1).padStart(2, "0");
  return `${dd}.${mm}.${d.getUTCFullYear()}`;
}

export function formatBucketRange(iso: string, interval: string, from?: string, to?: string): string {
  const origin = new Date(iso);
  if (interval === "Day") return formatFullDate(origin);

  const end =
    interval === "Week"
      ? new Date(Date.UTC(origin.getUTCFullYear(), origin.getUTCMonth(), origin.getUTCDate() + 6))
      : new Date(Date.UTC(origin.getUTCFullYear(), origin.getUTCMonth() + 1, 0));

  return `${formatFullDate(clampStart(origin, from))} – ${formatFullDate(clampEnd(end, to))}`;
}

export function bucketKey(iso: string): string {
  const d = new Date(iso);
  return `${d.getUTCFullYear()}-${d.getUTCMonth()}-${d.getUTCDate()}`;
}

export function bucketStartOf(iso: string, interval: string): Date {
  const d = new Date(iso);
  if (interval === "Week") {
    const mondayOffset = (d.getUTCDay() + 6) % 7;
    return new Date(Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate() - mondayOffset));
  }
  if (interval === "Month") return new Date(Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), 1));
  return new Date(Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate()));
}

export function nextBucket(d: Date, interval: string): Date {
  if (interval === "Week") return new Date(Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate() + 7));
  if (interval === "Month") return new Date(Date.UTC(d.getUTCFullYear(), d.getUTCMonth() + 1, 1));
  return new Date(Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate() + 1));
}

// Continuous bucket sequence over the period (incl. empty buckets), shared by all currencies so
// axes and widths align. Falls back to the data extent when the period is open-ended.
export function enumerateBuckets(
  from: string | undefined,
  to: string | undefined,
  interval: TimeInterval,
  present: string[],
): string[] {
  const startSource = from ?? (present.length > 0 ? present.reduce((a, b) => (a < b ? a : b)) : undefined);
  if (startSource === undefined) return [];

  const endDate = to
    ? new Date(to)
    : present.length > 0
      ? nextBucket(bucketStartOf(present.reduce((a, b) => (a > b ? a : b)), interval), interval)
      : undefined;
  if (endDate === undefined) return [];

  const buckets: string[] = [];
  let cursor = bucketStartOf(startSource, interval);
  for (let guard = 0; cursor.getTime() < endDate.getTime() && guard < 5000; guard++) {
    buckets.push(cursor.toISOString());
    cursor = nextBucket(cursor, interval);
  }
  return buckets;
}

export function formatAxisNumber(valueMinor: number): string {
  return Math.round(valueMinor / 100)
    .toLocaleString("en-US")
    .replace(/,/g, " ");
}
