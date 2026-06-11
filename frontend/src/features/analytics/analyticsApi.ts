import { apiFetch } from "@/lib/apiClient";
import type { TransactionType } from "@/features/transactions/transactionsApi";

export type BreakdownDimension = "Category" | "Account" | "Name";
export type TimeInterval = "Day" | "Week" | "Month";

export type CurrencySummary = {
  currency: string;
  incomeMinor: number;
  expensesMinor: number;
  netMinor: number;
};

export type BreakdownItem = {
  key: string;
  label: string | null;
  currency: string;
  amountMinor: number;
};

export type TimeSeriesPoint = {
  periodStart: string;
  currency: string;
  incomeMinor: number;
  expensesMinor: number;
};

export type CategoryTimeSeriesPoint = {
  periodStart: string;
  currency: string;
  categoryId: string;
  label: string | null;
  amountMinor: number;
};

export type AnalyticsFilter = {
  from?: string;
  to?: string;
  accountIds?: string[];
  categoryIds?: string[];
  type?: TransactionType;
};

function buildQuery(filter: AnalyticsFilter, extra: Record<string, string> = {}): string {
  const params = new URLSearchParams(extra);
  if (filter.from) params.set("from", filter.from);
  if (filter.to) params.set("to", filter.to);
  if (filter.type) params.set("type", filter.type);
  for (const accountId of filter.accountIds ?? []) {
    params.append("accountIds", accountId);
  }
  for (const categoryId of filter.categoryIds ?? []) {
    params.append("categoryIds", categoryId);
  }
  const query = params.toString();
  return query ? `?${query}` : "";
}

export function getSummary(workspaceId: string, filter: AnalyticsFilter): Promise<CurrencySummary[]> {
  return apiFetch<CurrencySummary[]>(`/workspaces/${workspaceId}/analytics/summary${buildQuery(filter)}`);
}

export function getBreakdown(
  workspaceId: string,
  dimension: BreakdownDimension,
  filter: AnalyticsFilter,
): Promise<BreakdownItem[]> {
  return apiFetch<BreakdownItem[]>(
    `/workspaces/${workspaceId}/analytics/breakdown${buildQuery(filter, { dimension })}`,
  );
}

export function getTimeSeries(
  workspaceId: string,
  interval: TimeInterval,
  filter: AnalyticsFilter,
): Promise<TimeSeriesPoint[]> {
  return apiFetch<TimeSeriesPoint[]>(
    `/workspaces/${workspaceId}/analytics/timeseries${buildQuery(filter, { interval })}`,
  );
}

export function askInsights(
  workspaceId: string,
  question: string,
  from: string,
  to: string,
  language: string,
): Promise<{ answer: string }> {
  return apiFetch<{ answer: string }>(`/workspaces/${workspaceId}/analytics/insights`, {
    method: "POST",
    body: { question, from, to, language },
  });
}

export function getCategoryTimeSeries(
  workspaceId: string,
  interval: TimeInterval,
  filter: AnalyticsFilter,
): Promise<CategoryTimeSeriesPoint[]> {
  return apiFetch<CategoryTimeSeriesPoint[]>(
    `/workspaces/${workspaceId}/analytics/category-timeseries${buildQuery(filter, { interval })}`,
  );
}
