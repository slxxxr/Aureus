import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { cn } from "@/lib/utils";
import { formatMoney } from "@/lib/formatMoney";
import { useWorkspace } from "@/features/workspaces/WorkspaceContext";
import {
  getSummary,
  getBreakdown,
  getTimeSeries,
  type AnalyticsFilter,
  type CurrencySummary,
  type BreakdownItem,
  type TimeSeriesPoint,
} from "@/features/analytics/analyticsApi";
import { pickInterval, type PeriodPreset } from "@/features/analytics/period";
import { useDashboardFilters } from "@/features/analytics/DashboardFiltersContext";
import { getFinancialAccounts } from "@/features/financial-accounts/financialAccountsApi";
import { getCategories } from "@/features/categories/categoriesApi";
import { DatePicker } from "@/components/ui/date-picker";
import { MultiSelect, type SelectGroup } from "@/components/ui/custom-select";

const INCOME_COLOR = "#16a34a";
const EXPENSE_COLOR = "hsl(var(--destructive))";
const AXIS_COLOR = "hsl(var(--muted-foreground))";
const GRID_COLOR = "hsl(var(--border))";

const INCOME_TONE = "text-green-600 dark:text-green-400";
const EXPENSE_TONE = "text-destructive";

// ─── helpers ──────────────────────────────────────────────────────────────────

function groupByCurrency<T extends { currency: string }>(items: T[]): Map<string, T[]> {
  const map = new Map<string, T[]>();
  for (const item of items) {
    if (!map.has(item.currency)) map.set(item.currency, []);
    map.get(item.currency)!.push(item);
  }
  return map;
}

function netTone(net: number): string {
  if (net > 0) return INCOME_TONE;
  if (net < 0) return EXPENSE_TONE;
  return "text-foreground";
}

function formatAxisDate(iso: string, interval: string): string {
  const d = new Date(iso);
  const dd = String(d.getUTCDate()).padStart(2, "0");
  const mm = String(d.getUTCMonth() + 1).padStart(2, "0");
  if (interval === "Month") return `${mm}.${String(d.getUTCFullYear()).slice(2)}`;
  return `${dd}.${mm}`;
}

function formatAxisNumber(valueMinor: number): string {
  return Math.round(valueMinor / 100)
    .toLocaleString("en-US")
    .replace(/,/g, " ");
}

// ─── period filter ──────────────────────────────────────────────────────────────

function PeriodFilter() {
  const { t } = useTranslation();
  const { preset, customFrom, customTo, selectPreset, changeCustom } = useDashboardFilters();

  const presets: { value: PeriodPreset; label: string }[] = [
    { value: "month", label: t("dashboard.period.month") },
    { value: "threeMonths", label: t("dashboard.period.threeMonths") },
    { value: "year", label: t("dashboard.period.year") },
    { value: "all", label: t("dashboard.period.all") },
  ];

  return (
    <div className="flex flex-wrap items-center gap-x-2 gap-y-2">
      <div className="flex gap-0.5">
        {presets.map((option) => (
          <button
            key={option.value}
            type="button"
            onClick={() => selectPreset(option.value)}
            className={cn(
              "rounded px-2.5 py-1.5 text-sm transition-colors",
              preset === option.value
                ? "bg-accent font-medium text-foreground"
                : "text-muted-foreground hover:bg-accent/60 hover:text-foreground",
            )}
          >
            {option.label}
          </button>
        ))}
      </div>

      <div className="flex items-center gap-2">
        <span className="text-sm text-muted-foreground">{t("dashboard.period.from")}</span>
        <DatePicker value={customFrom} onChange={(value) => changeCustom(value, customTo)} />
        <span className="text-sm text-muted-foreground">{t("dashboard.period.to")}</span>
        <DatePicker value={customTo} onChange={(value) => changeCustom(customFrom, value)} />
      </div>
    </div>
  );
}

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

// ─── category breakdown ─────────────────────────────────────────────────────────

function CategoryBreakdown({
  items,
  currency,
  barClass,
}: {
  items: BreakdownItem[];
  currency: string;
  barClass: string;
}) {
  const { t } = useTranslation();

  const rows = useMemo(
    () => [...items].sort((a, b) => b.amountMinor - a.amountMinor),
    [items],
  );

  const total = rows.reduce((sum, row) => sum + row.amountMinor, 0);

  return (
    <div className="space-y-2">
      {rows.map((row) => {
        const share = total > 0 ? (row.amountMinor / total) * 100 : 0;
        return (
          <div key={row.key} className="space-y-1">
            <div className="flex items-baseline justify-between gap-2 text-sm">
              <span className="truncate">{row.label ?? t("dashboard.breakdown.uncategorized")}</span>
              <span className="shrink-0 tabular-nums text-muted-foreground">
                {Math.round(share)}% · {formatMoney(row.amountMinor, currency)}
              </span>
            </div>
            <div className="h-1.5 overflow-hidden rounded-full bg-muted">
              <div
                className={cn("h-full rounded-full", barClass)}
                style={{ width: `${share}%` }}
              />
            </div>
          </div>
        );
      })}
    </div>
  );
}

// ─── chart tooltip ──────────────────────────────────────────────────────────────

type TooltipData = {
  active?: boolean;
  label?: string | number;
  payload?: readonly { dataKey?: string | number; value?: number }[];
};

function ChartTooltip({ active, payload, label, currency }: TooltipData & { currency: string }) {
  const { t } = useTranslation();
  if (!active || !payload?.length) return null;

  const valueOf = (key: string) => Number(payload.find((entry) => entry.dataKey === key)?.value ?? 0);
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
      <p className="mb-1.5 font-medium text-foreground">{label}</p>
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

// ─── income / expense chart ─────────────────────────────────────────────────────

function IncomeExpenseChart({
  points,
  periods,
  currency,
  interval,
}: {
  points: TimeSeriesPoint[];
  periods: string[];
  currency: string;
  interval: string;
}) {
  const data = useMemo(() => {
    const byPeriod = new Map(points.map((point) => [point.periodStart, point]));
    return periods.map((period) => {
      const point = byPeriod.get(period);
      return {
        label: formatAxisDate(period, interval),
        income: point?.incomeMinor ?? 0,
        expenses: point?.expensesMinor ?? 0,
      };
    });
  }, [points, periods, interval]);

  return (
    <ResponsiveContainer width="100%" height={260}>
      <BarChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: 8 }}>
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
          content={(props) => <ChartTooltip {...(props as TooltipData)} currency={currency} />}
        />
        <Bar dataKey="income" fill={INCOME_COLOR} radius={[3, 3, 0, 0]} isAnimationActive={false} />
        <Bar dataKey="expenses" fill={EXPENSE_COLOR} radius={[3, 3, 0, 0]} isAnimationActive={false} />
      </BarChart>
    </ResponsiveContainer>
  );
}

// ─── section wrappers ─────────────────────────────────────────────────────────

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-lg border border-border bg-card p-4">
      <h2 className="mb-3 text-sm font-semibold">{title}</h2>
      {children}
    </div>
  );
}

function BreakdownSection({
  title,
  emptyLabel,
  byCurrency,
  currencies,
  multiCurrency,
  barClass,
}: {
  title: string;
  emptyLabel: string;
  byCurrency: Map<string, BreakdownItem[]>;
  currencies: string[];
  multiCurrency: boolean;
  barClass: string;
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
            {items.length > 0 ? (
              <CategoryBreakdown items={items} currency={currency} barClass={barClass} />
            ) : (
              <p className="py-6 text-center text-sm text-muted-foreground">{emptyLabel}</p>
            )}
          </div>
        );
      })}
    </Section>
  );
}

// ─── page ─────────────────────────────────────────────────────────────────────

export function DashboardPage() {
  const { t } = useTranslation();
  const { activeWorkspace } = useWorkspace();
  const { range, accountIds, setAccountIds, categoryIds, setCategoryIds } = useDashboardFilters();

  const interval = pickInterval(range);
  const enabled = activeWorkspace !== null;

  const filter = useMemo<AnalyticsFilter>(
    () => ({
      ...range,
      accountIds: accountIds.length > 0 ? accountIds : undefined,
      categoryIds: categoryIds.length > 0 ? categoryIds : undefined,
    }),
    [range, accountIds, categoryIds],
  );

  const { data: accounts = [] } = useQuery({
    queryKey: ["financial-accounts", activeWorkspace?.id],
    queryFn: () => getFinancialAccounts(activeWorkspace!.id),
    enabled,
  });

  const { data: categories = [] } = useQuery({
    queryKey: ["categories", activeWorkspace?.id],
    queryFn: () => getCategories(activeWorkspace!.id),
    enabled,
  });

  const categoryGroups = useMemo(() => {
    const toOptions = (type: "Expense" | "Income") =>
      categories.filter((category) => category.type === type).map((c) => ({ value: c.id, label: c.name }));
    const groups: SelectGroup[] = [];
    const expense = toOptions("Expense");
    const income = toOptions("Income");
    if (expense.length > 0) groups.push({ label: t("categories.expenseSection"), options: expense });
    if (income.length > 0) groups.push({ label: t("categories.incomeSection"), options: income });
    return groups;
  }, [categories, t]);

  const { data: summary = [], isLoading: summaryLoading } = useQuery({
    queryKey: ["analytics", "summary", activeWorkspace?.id, filter],
    queryFn: () => getSummary(activeWorkspace!.id, filter),
    enabled,
  });

  const { data: expenseBreakdown = [], isLoading: expenseLoading } = useQuery({
    queryKey: ["analytics", "breakdown", "Expense", activeWorkspace?.id, filter],
    queryFn: () => getBreakdown(activeWorkspace!.id, "Category", { ...filter, type: "Expense" }),
    enabled,
  });

  const { data: incomeBreakdown = [], isLoading: incomeLoading } = useQuery({
    queryKey: ["analytics", "breakdown", "Income", activeWorkspace?.id, filter],
    queryFn: () => getBreakdown(activeWorkspace!.id, "Category", { ...filter, type: "Income" }),
    enabled,
  });

  const { data: series = [], isLoading: seriesLoading } = useQuery({
    queryKey: ["analytics", "timeseries", activeWorkspace?.id, filter, interval],
    queryFn: () => getTimeSeries(activeWorkspace!.id, interval, filter),
    enabled,
  });

  const isLoading = summaryLoading || expenseLoading || incomeLoading || seriesLoading;

  const expenseByCurrency = useMemo(() => groupByCurrency(expenseBreakdown), [expenseBreakdown]);
  const incomeByCurrency = useMemo(() => groupByCurrency(incomeBreakdown), [incomeBreakdown]);
  const seriesByCurrency = useMemo(() => groupByCurrency(series), [series]);
  const periods = useMemo(
    () => Array.from(new Set(series.map((point) => point.periodStart))).sort(),
    [series],
  );
  const currencies = summary.map((row) => row.currency);
  const multiCurrency = currencies.length > 1;

  return (
    <div className="min-w-0">
      <div className="sticky top-0 z-10 bg-background pb-3 pr-8 pt-9">
        <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
          <PeriodFilter />
          {accounts.length > 0 && (
            <div className="w-48">
              <MultiSelect
                values={accountIds}
                onChange={setAccountIds}
                options={accounts.map((account) => ({ value: account.id, label: account.name }))}
                allLabel={t("dashboard.allAccounts")}
              />
            </div>
          )}
          {categoryGroups.length > 0 && (
            <div className="w-48">
              <MultiSelect
                values={categoryIds}
                onChange={setCategoryIds}
                groups={categoryGroups}
                allLabel={t("dashboard.allCategories")}
              />
            </div>
          )}
        </div>
      </div>

      {isLoading && (
        <div className="animate-pulse space-y-4">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            {[0, 1, 2].map((i) => (
              <div key={i} className="h-20 rounded-lg border border-border bg-muted/40" />
            ))}
          </div>
          <div className="h-72 rounded-lg border border-border bg-muted/40" />
        </div>
      )}

      {!isLoading && summary.length === 0 && (
        <div className="pt-8 text-center">
          <p className="text-sm font-medium">{t("dashboard.emptyTitle")}</p>
          <p className="mt-1 text-sm text-muted-foreground">{t("dashboard.emptyDescription")}</p>
        </div>
      )}

      {!isLoading && summary.length > 0 && (
        <div className="space-y-6 pr-8">
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
                    <IncomeExpenseChart points={points} periods={periods} currency={currency} interval={interval} />
                  ) : (
                    <p className="py-10 text-center text-sm text-muted-foreground">{t("dashboard.chart.empty")}</p>
                  )}
                </div>
              );
            })}
          </Section>

          <BreakdownSection
            title={t("dashboard.breakdown.title")}
            emptyLabel={t("dashboard.breakdown.empty")}
            byCurrency={expenseByCurrency}
            currencies={currencies}
            multiCurrency={multiCurrency}
            barClass="bg-destructive/70"
          />

          <BreakdownSection
            title={t("dashboard.breakdown.incomeTitle")}
            emptyLabel={t("dashboard.breakdown.incomeEmpty")}
            byCurrency={incomeByCurrency}
            currencies={currencies}
            multiCurrency={multiCurrency}
            barClass="bg-green-500/70"
          />
        </div>
      )}
    </div>
  );
}
