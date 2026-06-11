import { useMemo } from "react";
import { useSearchParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import { cn } from "@/lib/utils";
import { useWorkspace } from "@/features/workspaces/WorkspaceContext";
import { getSummary, type AnalyticsFilter } from "@/features/analytics/analyticsApi";
import { pickInterval, type PeriodPreset } from "@/features/analytics/period";
import { useDashboardFilters } from "@/features/analytics/DashboardFiltersContext";
import { getFinancialAccounts } from "@/features/financial-accounts/financialAccountsApi";
import { getCategories } from "@/features/categories/categoriesApi";
import { DashboardSkeleton } from "@/features/analytics/components/shared";
import { OverviewTab } from "@/features/analytics/components/OverviewTab";
import { CategoriesTab } from "@/features/analytics/components/CategoriesTab";
import { DynamicsTab } from "@/features/analytics/components/DynamicsTab";
import { DatePicker } from "@/components/ui/date-picker";
import { MultiSelect, type SelectGroup } from "@/components/ui/custom-select";

type DashboardTab = "overview" | "categories" | "dynamics";

// ─── period filter ────────────────────────────────────────────────────────────

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

// ─── page ─────────────────────────────────────────────────────────────────────

export function DashboardPage() {
  const { t } = useTranslation();
  const { activeWorkspace } = useWorkspace();
  const { range, accountIds, setAccountIds, categoryIds, setCategoryIds } = useDashboardFilters();

  const interval = pickInterval(range);
  const enabled = activeWorkspace !== null;

  const [searchParams] = useSearchParams();
  const tab = (searchParams.get("tab") ?? "overview") as DashboardTab;

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

  const { data: summary = [], isLoading: summaryLoading } = useQuery({
    queryKey: ["analytics", "summary", activeWorkspace?.id, filter],
    queryFn: () => getSummary(activeWorkspace!.id, filter),
    enabled,
  });

  const currencies = summary.map((row) => row.currency);
  const multiCurrency = currencies.length > 1;

  const categoryGroups = useMemo(() => {
    const toOptions = (type: "Expense" | "Income") =>
      categories.filter((c) => c.type === type).map((c) => ({ value: c.id, label: c.name }));
    const groups: SelectGroup[] = [];
    const expense = toOptions("Expense");
    const income = toOptions("Income");
    if (expense.length > 0) groups.push({ label: t("categories.expenseSection"), options: expense });
    if (income.length > 0) groups.push({ label: t("categories.incomeSection"), options: income });
    return groups;
  }, [categories, t]);

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
                options={accounts.map((a) => ({ value: a.id, label: a.name }))}
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

      {summaryLoading && <DashboardSkeleton />}

      {!summaryLoading && summary.length === 0 && (
        <div className="pt-8 text-center">
          <p className="text-sm font-medium">{t("dashboard.emptyTitle")}</p>
          <p className="mt-1 text-sm text-muted-foreground">{t("dashboard.emptyDescription")}</p>
        </div>
      )}

      {!summaryLoading && summary.length > 0 && (
        <div className="pr-8">
          {tab === "overview" && (
            <OverviewTab
              workspaceId={activeWorkspace!.id}
              filter={filter}
              interval={interval}
              summary={summary}
              enabled={enabled}
            />
          )}

          {tab === "categories" && (
            <CategoriesTab
              workspaceId={activeWorkspace!.id}
              filter={filter}
              currencies={currencies}
              multiCurrency={multiCurrency}
              enabled={enabled}
            />
          )}

          {tab === "dynamics" && (
            <DynamicsTab
              workspaceId={activeWorkspace!.id}
              filter={filter}
              interval={interval}
              enabled={enabled}
            />
          )}
        </div>
      )}
    </div>
  );
}
