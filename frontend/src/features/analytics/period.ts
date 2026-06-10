import type { TimeInterval } from "@/features/analytics/analyticsApi";

export type PeriodPreset = "month" | "threeMonths" | "year" | "all";

export type DateRange = {
  from?: string;
  to?: string;
};

const startOfMonth = (date: Date): Date => new Date(date.getFullYear(), date.getMonth(), 1);

export function presetRange(preset: PeriodPreset, now: Date = new Date()): DateRange {
  const tomorrow = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1).toISOString();

  switch (preset) {
    case "month":
      return { from: startOfMonth(now).toISOString(), to: tomorrow };
    case "threeMonths":
      return {
        from: new Date(now.getFullYear(), now.getMonth() - 2, 1).toISOString(),
        to: tomorrow,
      };
    case "year":
      return {
        from: new Date(now.getFullYear(), 0, 1).toISOString(),
        to: tomorrow,
      };
    case "all":
      return {};
  }
}

export function customRange(fromDay: string, toDay: string): DateRange {
  const range: DateRange = {};
  if (fromDay) {
    range.from = new Date(`${fromDay}T00:00:00`).toISOString();
  }
  if (toDay) {
    const next = new Date(`${toDay}T00:00:00`);
    next.setDate(next.getDate() + 1);
    range.to = next.toISOString();
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
