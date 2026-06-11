import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { cn } from "@/lib/utils";
import { formatMoney } from "@/lib/formatMoney";
import {
  getBreakdown,
  getTimeSeries,
  type AnalyticsFilter,
  type CurrencySummary,
  type BreakdownItem,
  type TimeSeriesPoint,
  type TimeInterval,
} from "@/features/analytics/analyticsApi";
import {
  INCOME_COLOR,
  EXPENSE_COLOR,
  AXIS_COLOR,
  GRID_COLOR,
  INCOME_TONE,
  EXPENSE_TONE,
  groupByCurrency,
  netTone,
  formatAxisDate,
  formatBucketRange,
  bucketKey,
  enumerateBuckets,
  formatAxisNumber,
} from "@/features/analytics/dashboardUtils";
import { Section, DashboardSkeleton } from "./shared";

// ─── summary cards ────────────────────────────────────────────────────────────

function SummaryCards({ summary, showCurrency }: { summary: CurrencySummary; showCurrency: boolean }) {
  const { t } = useTranslation();

  const cards = [
    { label: t("dashboard.summary.income"), value: summary.incomeMinor, tone: INCOME_TONE },
    { label: t("dashboard.summary.expenses"), value: summary.expensesMinor, tone: EXPENSE_TONE },
    { label: t("dashboard.summary.net"), value: summary.netMinor, tone: netTone(summary.netMinor) },
  ];

  return (
    <div>
      {showCurrency && (
        <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          {summary.currency}
        </p>
      )}
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        {cards.map((card) => (
          <div key={card.label} className="rounded-lg border border-border bg-card p-4">
            <p className="text-xs text-muted-foreground">{card.label}</p>
            <p className={cn("mt-1 text-lg font-semibold tabular-nums", card.tone)}>
              {formatMoney(card.value, summary.currency)}
            </p>
          </div>
        ))}
      </div>
    </div>
  );
}

// ─── chart tooltip ────────────────────────────────────────────────────────────

type TooltipPayloadEntry = { dataKey?: string | number; value?: number; payload?: { period?: string } };

function ChartTooltip({
  active,
  payload,
  currency,
  interval,
  from,
  to,
}: {
  active?: boolean;
  payload?: readonly TooltipPayloadEntry[];
  currency: string;
  interval: string;
  from?: string;
  to?: string;
}) {
  const { t } = useTranslation();
  if (!active || !payload?.length) return null;

  const period = payload[0]?.payload?.period;
  const valueOf = (key: string) => Number(payload.find((e) => e.dataKey === key)?.value ?? 0);
  const income = valueOf("income");
  const expenses = valueOf("expenses");
  const net = income - expenses;

  const rows = [
    { label: t("dashboard.summary.income"), value: income, tone: INCOME_TONE },
    { label: t("dashboard.summary.expenses"), value: expenses, tone: EXPENSE_TONE },
    { label: t("dashboard.summary.net"), value: net, tone: netTone(net) },
  ];

  return (
    <div className="rounded-lg border bg-background p-2.5 text-xs shadow-md" style={{ borderColor: GRID_COLOR }}>
      <p className="mb-1.5 font-medium text-foreground">
        {period ? formatBucketRange(period, interval, from, to) : ""}
      </p>
      <div className="space-y-1">
        {rows.map((row) => (
          <div key={row.label} className="flex items-center justify-between gap-4">
            <span className="text-muted-foreground">{row.label}</span>
            <span className={cn("font-medium tabular-nums", row.tone)}>{formatMoney(row.value, currency)}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

// ─── income / expense chart ───────────────────────────────────────────────────

function IncomeExpenseChart({
  points,
  periods,
  currency,
  interval,
  from,
  to,
}: {
  points: TimeSeriesPoint[];
  periods: string[];
  currency: string;
  interval: string;
  from?: string;
  to?: string;
}) {
  const data = useMemo(() => {
    const byKey = new Map(points.map((point) => [bucketKey(point.periodStart), point]));
    return periods.map((period) => {
      const point = byKey.get(bucketKey(period));
      return {
        period,
        label: formatAxisDate(period, interval, from),
        income: point?.incomeMinor ?? 0,
        expenses: point?.expensesMinor ?? 0,
      };
    });
  }, [points, periods, interval, from]);

  return (
    <ResponsiveContainer width="100%" height={260}>
      <BarChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: 8 }}>
        <defs>
          <linearGradient id="bar-income" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={INCOME_COLOR} stopOpacity={0.95} />
            <stop offset="100%" stopColor={INCOME_COLOR} stopOpacity={0.45} />
          </linearGradient>
          <linearGradient id="bar-expense" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={EXPENSE_COLOR} stopOpacity={0.95} />
            <stop offset="100%" stopColor={EXPENSE_COLOR} stopOpacity={0.45} />
          </linearGradient>
        </defs>
        <CartesianGrid vertical={false} stroke={GRID_COLOR} />
        <XAxis dataKey="label" tickLine={false} axisLine={false} tick={{ fill: AXIS_COLOR, fontSize: 12 }} />
        <YAxis
          width={48}
          tickLine={false}
          axisLine={false}
          tick={{ fill: AXIS_COLOR, fontSize: 12 }}
          tickFormatter={formatAxisNumber}
        />
        <Tooltip
          cursor={{ fill: GRID_COLOR, opacity: 0.3 }}
          isAnimationActive={false}
          content={(props) => (
            <ChartTooltip
              {...(props as { active?: boolean; payload?: readonly TooltipPayloadEntry[] })}
              currency={currency}
              interval={interval}
              from={from}
              to={to}
            />
          )}
        />
        <Bar dataKey="income" fill="url(#bar-income)" radius={[10, 10, 0, 0]} isAnimationActive={false} />
        <Bar dataKey="expenses" fill="url(#bar-expense)" radius={[10, 10, 0, 0]} isAnimationActive={false} />
      </BarChart>
    </ResponsiveContainer>
  );
}

// ─── breakdown bars / section ─────────────────────────────────────────────────

function BreakdownBars({
  items,
  currency,
  barColor,
  fallbackLabel,
}: {
  items: BreakdownItem[];
  currency: string;
  barColor: string;
  fallbackLabel: string;
}) {
  const rows = useMemo(() => [...items].sort((a, b) => b.amountMinor - a.amountMinor), [items]);
  const total = rows.reduce((sum, row) => sum + row.amountMinor, 0);

  return (
    <div className="space-y-2">
      {rows.map((row) => {
        const share = total > 0 ? (row.amountMinor / total) * 100 : 0;
        return (
          <div key={row.key} className="space-y-1">
            <div className="flex items-baseline justify-between gap-2 text-sm">
              <span className="truncate">{row.label ?? fallbackLabel}</span>
              <span className="shrink-0 tabular-nums text-muted-foreground">
                {Math.round(share)}% · {formatMoney(row.amountMinor, currency)}
              </span>
            </div>
            <div className="h-1.5 overflow-hidden rounded-full bg-muted">
              <div className="h-full rounded-full" style={{ width: `${share}%`, background: barColor, opacity: 0.75 }} />
            </div>
          </div>
        );
      })}
    </div>
  );
}

function BreakdownSection({
  title,
  emptyLabel,
  byCurrency,
  currencies,
  multiCurrency,
  barColor,
  fallbackLabel,
}: {
  title: string;
  emptyLabel: string;
  byCurrency: Map<string, BreakdownItem[]>;
  currencies: string[];
  multiCurrency: boolean;
  barColor: string;
  fallbackLabel: string;
}) {
  return (
    <Section title={title}>
      {currencies.map((currency) => {
        const items = byCurrency.get(currency) ?? [];
        return (
          <div key={currency} className="space-y-2 [&:not(:first-child)]:mt-4">
            {multiCurrency && (
              <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{currency}</p>
            )}
            {items.length === 0 ? (
              <p className="py-6 text-center text-sm text-muted-foreground">{emptyLabel}</p>
            ) : (
              <BreakdownBars items={items} currency={currency} barColor={barColor} fallbackLabel={fallbackLabel} />
            )}
          </div>
        );
      })}
    </Section>
  );
}

// ─── tab ──────────────────────────────────────────────────────────────────────

export function OverviewTab({
  workspaceId,
  filter,
  interval,
  summary,
  enabled,
}: {
  workspaceId: string;
  filter: AnalyticsFilter;
  interval: TimeInterval;
  summary: CurrencySummary[];
  enabled: boolean;
}) {
  const { t } = useTranslation();

  const currencies = summary.map((row) => row.currency);
  const multiCurrency = currencies.length > 1;

  const { data: expenseAccountBreakdown = [], isLoading: expenseAccountLoading } = useQuery({
    queryKey: ["analytics", "breakdown", "Account", "Expense", workspaceId, filter],
    queryFn: () => getBreakdown(workspaceId, "Account", { ...filter, type: "Expense" }),
    enabled,
  });

  const { data: incomeAccountBreakdown = [], isLoading: incomeAccountLoading } = useQuery({
    queryKey: ["analytics", "breakdown", "Account", "Income", workspaceId, filter],
    queryFn: () => getBreakdown(workspaceId, "Account", { ...filter, type: "Income" }),
    enabled,
  });

  const { data: series = [], isLoading: seriesLoading } = useQuery({
    queryKey: ["analytics", "timeseries", workspaceId, filter, interval],
    queryFn: () => getTimeSeries(workspaceId, interval, filter),
    enabled,
  });

  const isLoading = seriesLoading || expenseAccountLoading || incomeAccountLoading;

  const expenseAccountByCurrency = useMemo(() => groupByCurrency(expenseAccountBreakdown), [expenseAccountBreakdown]);
  const incomeAccountByCurrency = useMemo(() => groupByCurrency(incomeAccountBreakdown), [incomeAccountBreakdown]);
  const seriesByCurrency = useMemo(() => groupByCurrency(series), [series]);
  const periods = useMemo(
    () => enumerateBuckets(filter.from, filter.to, interval, series.map((p) => p.periodStart)),
    [filter.from, filter.to, interval, series],
  );

  if (isLoading) return <DashboardSkeleton />;

  return (
    <div className="space-y-6">
      {summary.map((row) => (
        <SummaryCards key={row.currency} summary={row} showCurrency={multiCurrency} />
      ))}

      <Section title={t("dashboard.chart.title")}>
        {currencies.map((currency) => {
          const points = seriesByCurrency.get(currency) ?? [];
          return (
            <div key={currency} className="space-y-1">
              {multiCurrency && (
                <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{currency}</p>
              )}
              {points.length > 0 ? (
                <IncomeExpenseChart
                  points={points}
                  periods={periods}
                  currency={currency}
                  interval={interval}
                  from={filter.from}
                  to={filter.to}
                />
              ) : (
                <p className="py-10 text-center text-sm text-muted-foreground">{t("dashboard.chart.empty")}</p>
              )}
            </div>
          );
        })}
      </Section>

      <BreakdownSection
        title={t("dashboard.breakdown.incomeByAccount")}
        emptyLabel={t("dashboard.breakdown.incomeByAccountEmpty")}
        byCurrency={incomeAccountByCurrency}
        currencies={currencies}
        multiCurrency={multiCurrency}
        barColor={INCOME_COLOR}
        fallbackLabel={t("transactions.unknownAccount")}
      />

      <BreakdownSection
        title={t("dashboard.breakdown.expenseByAccount")}
        emptyLabel={t("dashboard.breakdown.expenseByAccountEmpty")}
        byCurrency={expenseAccountByCurrency}
        currencies={currencies}
        multiCurrency={multiCurrency}
        barColor={EXPENSE_COLOR}
        fallbackLabel={t("transactions.unknownAccount")}
      />
    </div>
  );
}
