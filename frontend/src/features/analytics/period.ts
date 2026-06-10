import type { TimeInterval } from "@/features/analytics/analyticsApi";

export type PeriodPreset = "month" | "threeMonths" | "year" | "all";

export type DateRange = {
  from?: string;
  to?: string;
};

const dateKey = (date: Date): string =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;

export function presetRange(preset: PeriodPreset, now: Date = new Date()): DateRange {
  const tomorrow = dateKey(new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1));

  switch (preset) {
    case "month":
      return { from: dateKey(new Date(now.getFullYear(), now.getMonth(), 1)), to: tomorrow };
    case "threeMonths":
      return {
        from: dateKey(new Date(now.getFullYear(), now.getMonth() - 2, 1)),
        to: tomorrow,
      };
    case "year":
      return {
        from: dateKey(new Date(now.getFullYear(), 0, 1)),
        to: tomorrow,
      };
    case "all":
      return {};
  }
}

export function customRange(fromDay: string, toDay: string): DateRange {
  const range: DateRange = {};
  if (fromDay) {
    range.from = fromDay;
  }
  if (toDay) {
    const next = new Date(`${toDay}T00:00:00`);
    next.setDate(next.getDate() + 1);
    range.to = dateKey(next);
  }
  return range;
}

const DAY_MS = 86_400_000;

export function pickInterval(range: DateRange): TimeInterval {
  if (!range.from) {
    return "Month";
  }
  const from = new Date(range.from).getTime();
  const to = range.to ? new Date(range.to).getTime() : Date.now();
  const days = (to - from) / DAY_MS;

  if (days <= 31) return "Day";
  if (days <= 92) return "Week";
  return "Month";
}
