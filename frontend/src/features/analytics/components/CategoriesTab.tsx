import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import { Cell, Pie, PieChart } from "recharts";
import { ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";
import { formatMoney } from "@/lib/formatMoney";
import {
  getBreakdown,
  type AnalyticsFilter,
  type BreakdownItem,
} from "@/features/analytics/analyticsApi";
import { colorForIndex } from "@/features/analytics/categoryColors";
import { INCOME_COLOR, EXPENSE_COLOR } from "@/features/analytics/dashboardUtils";
import type { TransactionType } from "@/features/transactions/transactionsApi";
import { Section, DashboardSkeleton } from "./shared";

// ─── donut detail ─────────────────────────────────────────────────────────────

function CategoryDonutDetail({ items, currency }: { items: BreakdownItem[]; currency: string }) {
  const rows = useMemo(() => [...items].sort((a, b) => b.amountMinor - a.amountMinor), [items]);
  const total = rows.reduce((s, r) => s + r.amountMinor, 0);
  const data = rows.map((row, i) => ({ ...row, color: colorForIndex(i) }));
  const [activeKey, setActiveKey] = useState<string | null>(null);

  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-start" onClick={() => setActiveKey(null)}>
      <div
        className="relative mx-auto shrink-0 sm:mx-0"
        style={{ width: 148, height: 148 }}
        onClick={(e) => e.stopPropagation()}
      >
        <PieChart width={148} height={148}>
          <Pie
            data={data}
            dataKey="amountMinor"
            innerRadius={44}
            outerRadius={66}
            paddingAngle={3}
            isAnimationActive={false}
            strokeWidth={0}
            startAngle={90}
            endAngle={-270}
            onClick={(entry) => {
              const key = (entry as { key: string }).key;
              setActiveKey((prev) => (prev === key ? null : key));
            }}
            style={{ cursor: "pointer" }}
          >
            {data.map((entry) => (
              <Cell
                key={entry.key}
                fill={entry.color}
                opacity={activeKey === null || activeKey === entry.key ? 0.9 : 0.25}
              />
            ))}
          </Pie>
        </PieChart>
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center">
          <span className="text-xs font-semibold tabular-nums">{formatMoney(total, currency)}</span>
        </div>
      </div>

      <div className="min-w-0 flex-1 space-y-2">
        {data.map((row) => {
          const share = total > 0 ? (row.amountMinor / total) * 100 : 0;
          const dimmed = activeKey !== null && activeKey !== row.key;
          return (
            <div
              key={row.key}
              className={cn("flex items-center gap-2 text-sm transition-opacity", dimmed && "opacity-30")}
            >
              <span className="h-2 w-2 shrink-0 rounded-full" style={{ background: row.color }} aria-hidden="true" />
              <span className="min-w-0 flex-1 truncate text-muted-foreground">{row.label ?? "—"}</span>
              <span className="shrink-0 tabular-nums text-muted-foreground">{Math.round(share)}%</span>
              <span className="w-24 shrink-0 text-right tabular-nums font-medium">
                {formatMoney(row.amountMinor, currency)}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ─── accordion row ────────────────────────────────────────────────────────────

function CategoryAccordionRow({
  item,
  total,
  currency,
  barColor,
  type,
  workspaceId,
  filter,
  isOpen,
  onToggle,
}: {
  item: BreakdownItem;
  total: number;
  currency: string;
  barColor: string;
  type: TransactionType;
  workspaceId: string;
  filter: AnalyticsFilter;
  isOpen: boolean;
  onToggle: () => void;
}) {
  const { t } = useTranslation();
  const share = total > 0 ? (item.amountMinor / total) * 100 : 0;

  const { data: names = [], isLoading } = useQuery({
    queryKey: ["analytics", "breakdown", "Name", workspaceId, type, item.key, currency, filter],
    queryFn: () => getBreakdown(workspaceId, "Name", { ...filter, categoryIds: [item.key], type }),
    enabled: isOpen,
  });

  const nameRows = useMemo(
    () => names.filter((n) => n.currency === currency).sort((a, b) => b.amountMinor - a.amountMinor),
    [names, currency],
  );

  return (
    <div>
      <button type="button" onClick={onToggle} className="flex w-full flex-col gap-1.5 py-2.5 text-left">
        <div className="flex items-baseline justify-between gap-2 text-sm">
          <span className="truncate font-medium">{item.label ?? t("dashboard.deletedCategories")}</span>
          <span className="flex shrink-0 items-center gap-2 tabular-nums text-muted-foreground">
            {Math.round(share)}%
            <span className="font-medium text-foreground">{formatMoney(item.amountMinor, currency)}</span>
            <ChevronRight
              className={cn(
                "h-3.5 w-3.5 shrink-0 text-muted-foreground/50 transition-transform duration-200",
                isOpen && "rotate-90",
              )}
            />
          </span>
        </div>
        <div className="h-1.5 overflow-hidden rounded-full bg-muted">
          <div
            className="h-full rounded-full"
            style={{ width: `${share}%`, background: barColor, opacity: 0.75 }}
          />
        </div>
      </button>

      {isOpen && (
        <div className="pb-4 pt-1">
          {isLoading ? (
            <div className="h-24 animate-pulse rounded-lg bg-muted/40" />
          ) : nameRows.length === 0 ? (
            <p className="py-3 text-center text-xs text-muted-foreground">
              {t("dashboard.breakdown.detailEmpty")}
            </p>
          ) : (
            <CategoryDonutDetail items={nameRows} currency={currency} />
          )}
        </div>
      )}
    </div>
  );
}

// ─── tab ──────────────────────────────────────────────────────────────────────

export function CategoriesTab({
  workspaceId,
  filter,
  currencies,
  multiCurrency,
  enabled,
}: {
  workspaceId: string;
  filter: AnalyticsFilter;
  currencies: string[];
  multiCurrency: boolean;
  enabled: boolean;
}) {
  const { t } = useTranslation();
  const [type, setType] = useState<TransactionType>("Expense");
  const [openKey, setOpenKey] = useState<string | null>(null);

  const { data: expenseBreakdown = [], isLoading: expenseLoading } = useQuery({
    queryKey: ["analytics", "breakdown", "Expense", workspaceId, filter],
    queryFn: () => getBreakdown(workspaceId, "Category", { ...filter, type: "Expense" }),
    enabled,
  });

  const { data: incomeBreakdown = [], isLoading: incomeLoading } = useQuery({
    queryKey: ["analytics", "breakdown", "Income", workspaceId, filter],
    queryFn: () => getBreakdown(workspaceId, "Category", { ...filter, type: "Income" }),
    enabled,
  });

  const expenseByCurrency = useMemo(() => groupByCurrencyAndSort(expenseBreakdown), [expenseBreakdown]);
  const incomeByCurrency = useMemo(() => groupByCurrencyAndSort(incomeBreakdown), [incomeBreakdown]);

  const currentByCurrency = type === "Expense" ? expenseByCurrency : incomeByCurrency;
  const barColor = type === "Expense" ? EXPENSE_COLOR : INCOME_COLOR;

  const toggles: { value: TransactionType; label: string }[] = [
    { value: "Expense", label: t("dashboard.breakdown.title") },
    { value: "Income", label: t("dashboard.breakdown.incomeTitle") },
  ];

  if (expenseLoading || incomeLoading) return <DashboardSkeleton />;

  return (
    <Section>
      <div className="mb-4 flex gap-0.5">
        {toggles.map((opt) => (
          <button
            key={opt.value}
            type="button"
            onClick={() => {
              setType(opt.value);
              setOpenKey(null);
            }}
            className={cn(
              "rounded px-2.5 py-1.5 text-sm transition-colors",
              type === opt.value
                ? "bg-accent font-medium text-foreground"
                : "text-muted-foreground hover:bg-accent/60 hover:text-foreground",
            )}
          >
            {opt.label}
          </button>
        ))}
      </div>

      {currencies.map((currency) => {
        const items = currentByCurrency.get(currency) ?? [];
        const total = items.reduce((s, r) => s + r.amountMinor, 0);

        return (
          <div key={currency}>
            {multiCurrency && (
              <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground">{currency}</p>
            )}
            {items.length === 0 ? (
              <p className="py-10 text-center text-sm text-muted-foreground">
                {type === "Expense" ? t("dashboard.breakdown.empty") : t("dashboard.breakdown.incomeEmpty")}
              </p>
            ) : (
              <div>
                {items.map((item) => (
                  <CategoryAccordionRow
                    key={item.key}
                    item={item}
                    total={total}
                    currency={currency}
                    barColor={barColor}
                    type={type}
                    workspaceId={workspaceId}
                    filter={filter}
                    isOpen={openKey === `${currency}:${item.key}`}
                    onToggle={() =>
                      setOpenKey((prev) =>
                        prev === `${currency}:${item.key}` ? null : `${currency}:${item.key}`,
                      )
                    }
                  />
                ))}
              </div>
            )}
          </div>
        );
      })}
    </Section>
  );
}

function groupByCurrencyAndSort(items: BreakdownItem[]): Map<string, BreakdownItem[]> {
  const map = new Map<string, BreakdownItem[]>();
  for (const item of items) {
    if (!map.has(item.currency)) map.set(item.currency, []);
    map.get(item.currency)!.push(item);
  }
  for (const [, rows] of map) rows.sort((a, b) => b.amountMinor - a.amountMinor);
  return map;
}
