import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { cn } from "@/lib/utils";
import { formatMoney } from "@/lib/formatMoney";
import {
  getCategoryTimeSeries,
  type AnalyticsFilter,
  type CategoryTimeSeriesPoint,
  type TimeInterval,
} from "@/features/analytics/analyticsApi";
import { colorForIndex, DELETED_CATEGORY_COLOR } from "@/features/analytics/categoryColors";
import {
  AXIS_COLOR,
  GRID_COLOR,
  groupByCurrency,
  formatAxisDate,
  formatBucketRange,
  bucketKey,
  enumerateBuckets,
  formatAxisNumber,
} from "@/features/analytics/dashboardUtils";
import { Section, DashboardSkeleton } from "./shared";

// ─── color meta ───────────────────────────────────────────────────────────────

const DELETED_KEY = "__deleted__";

type CategoryMeta = { label: string; color: string };
type DynamicsSeries = { key: string; label: string; color: string };
type DynamicsRow = Record<string, number | string>;

// One color per category across all currencies (by global total), so a category keeps its color
// in every per-currency chart; soft-deleted categories collapse into a single neutral aggregate.
function buildColorMeta(points: CategoryTimeSeriesPoint[], deletedLabel: string): Map<string, CategoryMeta> {
  const totals = new Map<string, { label: string; total: number }>();
  for (const point of points) {
    const key = point.label === null ? DELETED_KEY : point.categoryId;
    const entry = totals.get(key) ?? { label: point.label ?? deletedLabel, total: 0 };
    entry.total += point.amountMinor;
    totals.set(key, entry);
  }

  const meta = new Map<string, CategoryMeta>();
  [...totals.entries()]
    .filter(([key]) => key !== DELETED_KEY)
    .sort((a, b) => b[1].total - a[1].total)
    .forEach(([key, entry], index) => meta.set(key, { label: entry.label, color: colorForIndex(index) }));

  if (totals.has(DELETED_KEY)) {
    meta.set(DELETED_KEY, { label: totals.get(DELETED_KEY)!.label, color: DELETED_CATEGORY_COLOR });
  }
  return meta;
}

function buildDynamics(
  points: CategoryTimeSeriesPoint[],
  colorMeta: Map<string, CategoryMeta>,
  buckets: string[],
): { series: DynamicsSeries[]; rows: DynamicsRow[] } {
  const totals = new Map<string, number>();
  const byBucket = new Map<string, Map<string, number>>();

  for (const point of points) {
    const key = point.label === null ? DELETED_KEY : point.categoryId;
    totals.set(key, (totals.get(key) ?? 0) + point.amountMinor);

    const bk = bucketKey(point.periodStart);
    let amounts = byBucket.get(bk);
    if (!amounts) {
      amounts = new Map();
      byBucket.set(bk, amounts);
    }
    amounts.set(key, (amounts.get(key) ?? 0) + point.amountMinor);
  }

  const series: DynamicsSeries[] = [...totals.keys()]
    .sort((a, b) => {
      if (a === DELETED_KEY) return 1;
      if (b === DELETED_KEY) return -1;
      return (totals.get(b) ?? 0) - (totals.get(a) ?? 0);
    })
    .map((key) => {
      const meta = colorMeta.get(key);
      return { key, label: meta?.label ?? key, color: meta?.color ?? DELETED_CATEGORY_COLOR };
    });

  const rows: DynamicsRow[] = buckets.map((period) => {
    const amounts = byBucket.get(bucketKey(period));
    const row: DynamicsRow = { period };
    for (const item of series) row[item.key] = amounts?.get(item.key) ?? 0;
    return row;
  });

  return { series, rows };
}

// ─── small multiple tooltip ───────────────────────────────────────────────────

type SmallMultipleTooltipData = {
  active?: boolean;
  payload?: readonly { value?: number; payload?: { period?: string } }[];
};

function SmallMultipleTooltip({
  active,
  payload,
  currency,
  interval,
  color,
  from,
  to,
}: SmallMultipleTooltipData & { currency: string; interval: string; color: string; from?: string; to?: string }) {
  if (!active || !payload?.length) return null;
  const period = payload[0]?.payload?.period;
  const value = Number(payload[0]?.value ?? 0);

  return (
    <div className="rounded-lg border bg-background p-2 text-xs shadow-md" style={{ borderColor: GRID_COLOR }}>
      <p className="mb-1 text-muted-foreground">{period ? formatBucketRange(period, interval, from, to) : ""}</p>
      <span className="flex items-center gap-1.5 font-medium tabular-nums">
        <span className="h-2 w-2 shrink-0 rounded-full" style={{ background: color }} aria-hidden="true" />
        {formatMoney(value, currency)}
      </span>
    </div>
  );
}

// One panel per category, each with its own y-scale so a small category's trend stays readable
// next to a dominant one; panels share the x-axis bucket sequence to line up in time.
function SmallMultiple({
  serie,
  rows,
  interval,
  currency,
  from,
  to,
}: {
  serie: DynamicsSeries;
  rows: DynamicsRow[];
  interval: string;
  currency: string;
  from?: string;
  to?: string;
}) {
  const data = useMemo(
    () =>
      rows.map((row) => ({
        period: row.period as string,
        label: formatAxisDate(row.period as string, interval, from),
        value: Number(row[serie.key] ?? 0),
      })),
    [rows, interval, serie.key, from],
  );
  const total = data.reduce((sum, row) => sum + row.value, 0);
  const gradientId = `sm-${serie.key}`;

  return (
    <div className="rounded-lg border border-border bg-card p-3">
      <div className="mb-2 flex items-baseline justify-between gap-2">
        <span className="flex min-w-0 items-center gap-1.5 text-sm">
          <span className="h-2 w-2 shrink-0 rounded-full" style={{ background: serie.color }} aria-hidden="true" />
          <span className="truncate">{serie.label}</span>
        </span>
        <span className="shrink-0 text-sm font-medium tabular-nums">{formatMoney(total, currency)}</span>
      </div>
      <ResponsiveContainer width="100%" height={120}>
        <AreaChart data={data} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
          <defs>
            <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={serie.color} stopOpacity={0.25} />
              <stop offset="100%" stopColor={serie.color} stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid vertical={false} stroke={GRID_COLOR} />
          <XAxis
            dataKey="label"
            tickLine={false}
            axisLine={false}
            tick={{ fill: AXIS_COLOR, fontSize: 10 }}
            minTickGap={16}
          />
          <YAxis
            width={44}
            tickLine={false}
            axisLine={false}
            tick={{ fill: AXIS_COLOR, fontSize: 10 }}
            tickFormatter={formatAxisNumber}
          />
          <Tooltip
            cursor={{ stroke: GRID_COLOR }}
            isAnimationActive={false}
            content={(props) => (
              <SmallMultipleTooltip
                {...(props as SmallMultipleTooltipData)}
                currency={currency}
                interval={interval}
                color={serie.color}
                from={from}
                to={to}
              />
            )}
          />
          <Area
            type="monotone"
            dataKey="value"
            stroke={serie.color}
            strokeWidth={2}
            fill={`url(#${gradientId})`}
            dot={false}
            isAnimationActive={false}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}

// ─── tab ──────────────────────────────────────────────────────────────────────

export function DynamicsTab({
  workspaceId,
  filter,
  interval,
  enabled,
}: {
  workspaceId: string;
  filter: AnalyticsFilter;
  interval: TimeInterval;
  enabled: boolean;
}) {
  const { t } = useTranslation();
  const [type, setType] = useState<"Expense" | "Income">("Expense");

  const typedFilter = useMemo<AnalyticsFilter>(() => ({ ...filter, type }), [filter, type]);

  const { data: points = [], isLoading } = useQuery({
    queryKey: ["analytics", "category-timeseries", workspaceId, typedFilter, interval],
    queryFn: () => getCategoryTimeSeries(workspaceId, interval, typedFilter),
    enabled,
  });

  const deletedLabel = t("dashboard.deletedCategories");
  const byCurrency = useMemo(() => groupByCurrency(points), [points]);
  const currencies = useMemo(() => [...byCurrency.keys()].sort(), [byCurrency]);
  const multiCurrency = currencies.length > 1;
  const colorMeta = useMemo(() => buildColorMeta(points, deletedLabel), [points, deletedLabel]);
  const buckets = useMemo(
    () => enumerateBuckets(filter.from, filter.to, interval, points.map((p) => p.periodStart)),
    [filter.from, filter.to, interval, points],
  );

  const toggles: { value: "Expense" | "Income"; label: string }[] = [
    { value: "Expense", label: t("dashboard.dynamics.expense") },
    { value: "Income", label: t("dashboard.dynamics.income") },
  ];

  return (
    <Section>
      <div className="mb-3 flex gap-0.5">
        {toggles.map((option) => (
          <button
            key={option.value}
            type="button"
            onClick={() => setType(option.value)}
            className={cn(
              "rounded px-2.5 py-1.5 text-sm transition-colors",
              type === option.value
                ? "bg-accent font-medium text-foreground"
                : "text-muted-foreground hover:bg-accent/60 hover:text-foreground",
            )}
          >
            {option.label}
          </button>
        ))}
      </div>

      {isLoading ? (
        <div className="h-[280px] animate-pulse rounded-lg bg-muted/40" />
      ) : currencies.length === 0 ? (
        <p className="py-10 text-center text-sm text-muted-foreground">{t("dashboard.dynamics.empty")}</p>
      ) : (
        currencies.map((currency) => {
          const { series, rows } = buildDynamics(byCurrency.get(currency) ?? [], colorMeta, buckets);
          return (
            <div key={currency} className="[&:not(:first-child)]:mt-4">
              {multiCurrency && (
                <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  {currency}
                </p>
              )}
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {series.map((item) => (
                  <SmallMultiple
                    key={item.key}
                    serie={item}
                    rows={rows}
                    interval={interval}
                    currency={currency}
                    from={filter.from}
                    to={filter.to}
                  />
                ))}
              </div>
            </div>
          );
        })
      )}
    </Section>
  );
}
