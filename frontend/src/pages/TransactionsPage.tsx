import { useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowDown, ArrowUp, Plus } from "lucide-react";
import { cn } from "@/lib/utils";
import { formatMoney } from "@/lib/formatMoney";
import { useWorkspace } from "@/features/workspaces/WorkspaceContext";
import {
  getTransactions,
  createTransaction,
  type Transaction,
  type TransactionType,
} from "@/features/transactions/transactionsApi";
import { resolveTransactionError } from "@/features/transactions/resolveTransactionError";
import {
  getFinancialAccounts,
  type FinancialAccount,
} from "@/features/financial-accounts/financialAccountsApi";
import { getCategories, type Category } from "@/features/categories/categoriesApi";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Modal } from "@/components/ui/modal";
import { CustomSelect } from "@/components/ui/custom-select";
import { MultiSelect } from "@/components/ui/custom-select";
import { DatePicker } from "@/components/ui/date-picker";

// ─── helpers ──────────────────────────────────────────────────────────────────

function getLocalDateKey(dateString: string): string {
  const d = new Date(dateString);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function formatDateLabel(dateKey: string, t: TFunction): string {
  const todayKey = getLocalDateKey(new Date().toISOString());
  const yesterdayKey = getLocalDateKey(new Date(Date.now() - 86_400_000).toISOString());
  if (dateKey === todayKey) return t("transactions.date.today");
  if (dateKey === yesterdayKey) return t("transactions.date.yesterday");
  const [y, m, d] = dateKey.split("-");
  return `${d}.${m}.${y}`;
}

function getDailyNet(items: Transaction[]): string | null {
  const currencies = new Set(items.map((tx) => tx.currency));
  if (currencies.size !== 1) return null;
  const currency = [...currencies][0];
  const net = items.reduce(
    (sum, tx) => sum + (tx.type === "Income" ? tx.amountMinor : -tx.amountMinor),
    0,
  );
  return (net > 0 ? "+" : "") + formatMoney(net, currency);
}

// ─── create modal ─────────────────────────────────────────────────────────────

function CreateTransactionModal({
  workspaceId,
  accounts,
  categories,
  onClose,
}: {
  workspaceId: string;
  accounts: FinancialAccount[];
  categories: Category[];
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const [name, setName] = useState("");
  const [type, setType] = useState<TransactionType>("Expense");
  const [accountId, setAccountId] = useState(accounts[0]?.id ?? "");
  const [categoryId, setCategoryId] = useState("");
  const [amount, setAmount] = useState("");
  const [date, setDate] = useState(() => {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
  });
  const [note, setNote] = useState("");

  const filteredCategories = categories.filter((c) => c.type === type);

  const handleTypeChange = (newType: TransactionType) => {
    setType(newType);
    const stillValid = filteredCategories.some((c) => c.id === categoryId && c.type === newType);
    if (!stillValid) setCategoryId("");
  };

  const mutation = useMutation({
    mutationFn: () => {
      const occurredAt = new Date(date + "T00:00:00").toISOString();
      return createTransaction(workspaceId, {
        financialAccountId: accountId,
        categoryId,
        name: name.trim(),
        type,
        amountMinor: Math.round(parseFloat(amount) * 100),
        occurredAt,
        note: note.trim() || null,
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["transactions", workspaceId] });
      void queryClient.invalidateQueries({ queryKey: ["financial-accounts", workspaceId] });
      onClose();
    },
  });

  const missingAccounts = accounts.length === 0;
  const missingCategoryForType = filteredCategories.length === 0;
  const amountValid = amount !== "" && !isNaN(parseFloat(amount)) && parseFloat(amount) > 0;
  const canSubmit =
    !missingAccounts &&
    !missingCategoryForType &&
    name.trim() &&
    accountId &&
    categoryId &&
    amountValid;

  return (
    <Modal onBackdropClick={onClose}>
      <h2 className="mb-5 text-base font-semibold">{t("transactions.createModal.title")}</h2>
      <form
        onSubmit={(e: FormEvent) => { e.preventDefault(); if (canSubmit) mutation.mutate(); }}
        className="space-y-4"
      >
        {/* type toggle */}
        <div className="flex rounded-md border border-input">
          {(["Expense", "Income"] as TransactionType[]).map((opt) => (
            <button
              key={opt}
              type="button"
              onClick={() => handleTypeChange(opt)}
              disabled={mutation.isPending}
              className={cn(
                "flex-1 py-1.5 text-sm font-medium transition-colors first:rounded-l-[5px] last:rounded-r-[5px]",
                type === opt
                  ? "bg-accent text-foreground"
                  : "text-muted-foreground hover:bg-accent/60",
              )}
            >
              {t(opt === "Income" ? "categories.typeIncome" : "categories.typeExpense")}
            </button>
          ))}
        </div>

        {/* name */}
        <div className="space-y-1.5">
          <Label htmlFor="tx-name">{t("transactions.createModal.nameLabel")}</Label>
          <Input
            id="tx-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder={t("transactions.createModal.namePlaceholder")}
            required
            autoFocus
            autoComplete="off"
            disabled={mutation.isPending}
          />
        </div>

        {/* amount */}
        <div className="space-y-1.5">
          <Label htmlFor="tx-amount">{t("transactions.createModal.amountLabel")}</Label>
          <Input
            id="tx-amount"
            type="text"
            inputMode="decimal"
            value={amount}
            onChange={(e) => {
              const val = e.target.value.replace(",", ".");
              if (val === "" || /^\d*\.?\d{0,2}$/.test(val)) setAmount(val);
            }}
            placeholder="0.00"
            required
            autoComplete="off"
            disabled={mutation.isPending}
          />
        </div>

        {/* account */}
        <div className="space-y-1.5">
          <Label>{t("transactions.createModal.accountLabel")}</Label>
          {missingAccounts ? (
            <p className="rounded-md border border-border bg-muted/40 px-3 py-2 text-xs text-muted-foreground">
              {t("transactions.createModal.noAccounts")}
            </p>
          ) : (
            <CustomSelect
              value={accountId}
              onChange={setAccountId}
              options={accounts.map((a) => ({ value: a.id, label: a.name }))}
              placeholder={t("transactions.createModal.selectAccount")}
              disabled={mutation.isPending}
            />
          )}
        </div>

        {/* category */}
        <div className="space-y-1.5">
          <Label>{t("transactions.createModal.categoryLabel")}</Label>
          {missingCategoryForType ? (
            <p className="rounded-md border border-border bg-muted/40 px-3 py-2 text-xs text-muted-foreground">
              {t("transactions.createModal.noCategoriesForType")}
            </p>
          ) : (
            <CustomSelect
              value={categoryId}
              onChange={setCategoryId}
              options={filteredCategories.map((c) => ({ value: c.id, label: c.name }))}
              placeholder={t("transactions.createModal.selectCategory")}
              disabled={mutation.isPending}
            />
          )}
        </div>

        {/* date */}
        <div className="space-y-1.5">
          <Label>{t("transactions.createModal.dateLabel")}</Label>
          <DatePicker
            value={date}
            onChange={setDate}
            disabled={mutation.isPending}
          />
        </div>

        {/* note (optional) */}
        <div className="space-y-1.5">
          <Label htmlFor="tx-note">{t("transactions.createModal.noteLabel")}</Label>
          <Input
            id="tx-note"
            value={note}
            onChange={(e) => setNote(e.target.value)}
            placeholder={t("transactions.createModal.notePlaceholder")}
            autoComplete="off"
            disabled={mutation.isPending}
          />
        </div>

        {mutation.isError && (
          <p className="text-sm text-destructive" role="alert">
            {resolveTransactionError(mutation.error, t as TFunction)}
          </p>
        )}

        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            {t("common.cancel")}
          </Button>
          <Button type="submit" disabled={mutation.isPending || !canSubmit}>
            {mutation.isPending
              ? t("transactions.createModal.submitting")
              : t("transactions.createModal.submit")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

// ─── transaction row ──────────────────────────────────────────────────────────

function TransactionRow({
  tx,
  categoryMap,
  accountMap,
}: {
  tx: Transaction;
  categoryMap: Map<string, Category>;
  accountMap: Map<string, FinancialAccount>;
}) {
  const { t } = useTranslation();
  const isIncome = tx.type === "Income";
  const category = categoryMap.get(tx.categoryId);
  const account = accountMap.get(tx.financialAccountId);

  return (
    <div className="flex items-center gap-3 rounded-lg px-3 py-2.5 transition-colors hover:bg-accent/60">
      {/* type icon */}
      <div
        className={cn(
          "flex h-7 w-7 shrink-0 items-center justify-center rounded-full",
          isIncome
            ? "bg-green-500/10 text-green-600 dark:text-green-400"
            : "bg-destructive/10 text-destructive",
        )}
      >
        {isIncome ? (
          <ArrowUp className="h-3.5 w-3.5" />
        ) : (
          <ArrowDown className="h-3.5 w-3.5" />
        )}
      </div>

      {/* name + category */}
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium">{tx.name}</p>
        <p className="truncate text-xs text-muted-foreground">
          {category?.name ?? t("transactions.unknownCategory")}
        </p>
      </div>

      {/* account + amount */}
      <div className="shrink-0 text-right">
        <p
          className={cn(
            "text-sm font-semibold tabular-nums",
            isIncome ? "text-green-600 dark:text-green-400" : "text-destructive",
          )}
        >
          {isIncome ? "+" : "−"}
          {formatMoney(tx.amountMinor, tx.currency)}
        </p>
        <p className="text-xs text-muted-foreground">
          {account?.name ?? t("transactions.unknownAccount")}
        </p>
      </div>
    </div>
  );
}

// ─── date group ───────────────────────────────────────────────────────────────

function DateGroup({
  dateKey,
  items,
  categoryMap,
  accountMap,
}: {
  dateKey: string;
  items: Transaction[];
  categoryMap: Map<string, Category>;
  accountMap: Map<string, FinancialAccount>;
}) {
  const { t } = useTranslation();
  const net = getDailyNet(items);

  return (
    <div>
      <div className="mb-1 flex items-center justify-between px-3">
        <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          {formatDateLabel(dateKey, t as TFunction)}
        </span>
        {net && (
          <span className="text-xs font-medium tabular-nums text-muted-foreground">{net}</span>
        )}
      </div>
      <div className="space-y-0.5">
        {items.map((tx) => (
          <TransactionRow
            key={tx.id}
            tx={tx}
            categoryMap={categoryMap}
            accountMap={accountMap}
          />
        ))}
      </div>
    </div>
  );
}

// ─── skeleton ─────────────────────────────────────────────────────────────────

function TransactionsSkeleton() {
  return (
    <div className="animate-pulse space-y-6">
      {[3, 2, 4].map((count, i) => (
        <div key={i}>
          <div className="mb-1 px-3">
            <div className="h-3 w-20 rounded bg-muted" />
          </div>
          <div className="space-y-0.5">
            {Array.from({ length: count }).map((_, j) => (
              <div key={j} className="flex items-center gap-3 px-3 py-2.5">
                <div className="h-7 w-7 rounded-full bg-muted" />
                <div className="flex-1 space-y-1.5">
                  <div className="h-3.5 w-32 rounded bg-muted" />
                  <div className="h-3 w-20 rounded bg-muted" />
                </div>
                <div className="space-y-1.5 text-right">
                  <div className="h-3.5 w-20 rounded bg-muted" />
                  <div className="h-3 w-16 rounded bg-muted" />
                </div>
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

// ─── filter sidebar ───────────────────────────────────────────────────────────

function FilterSidebar({
  accounts,
  accountFilter,
  onAccountChange,
  typeFilter,
  onTypeChange,
}: {
  accounts: FinancialAccount[];
  accountFilter: string[];
  onAccountChange: (v: string[]) => void;
  typeFilter: "" | TransactionType;
  onTypeChange: (v: "" | TransactionType) => void;
}) {
  const { t } = useTranslation();

  const typeOptions: { value: "" | TransactionType; label: string }[] = [
    { value: "", label: t("transactions.filters.typeAll") },
    { value: "Income", label: t("transactions.filters.typeIncome") },
    { value: "Expense", label: t("transactions.filters.typeExpense") },
  ];

  return (
    <aside className="w-48 shrink-0">
      <div className="sticky top-20 space-y-5">
        {/* account filter */}
        <div>
          <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
            {t("transactions.filters.account")}
          </p>
          <MultiSelect
            values={accountFilter}
            onChange={onAccountChange}
            options={accounts.map((a) => ({ value: a.id, label: a.name }))}
            allLabel={t("transactions.filters.allAccounts")}
          />
        </div>

        {/* type filter */}
        <div>
          <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
            {t("transactions.filters.type")}
          </p>
          <div className="flex flex-col gap-0.5">
            {typeOptions.map((opt) => (
              <button
                key={opt.value}
                type="button"
                onClick={() => onTypeChange(opt.value)}
                className={cn(
                  "rounded px-2 py-1.5 text-left text-sm transition-colors",
                  typeFilter === opt.value
                    ? "bg-accent font-medium text-foreground"
                    : "text-muted-foreground hover:bg-accent/60 hover:text-foreground",
                )}
              >
                {opt.label}
              </button>
            ))}
          </div>
        </div>
      </div>
    </aside>
  );
}

// ─── page ─────────────────────────────────────────────────────────────────────

export function TransactionsPage() {
  const { t } = useTranslation();
  const { activeWorkspace } = useWorkspace();
  const [accountFilter, setAccountFilter] = useState<string[]>([]);
  const [typeFilter, setTypeFilter] = useState<"" | TransactionType>("");
  const [showCreate, setShowCreate] = useState(false);

  const { data: transactions = [], isLoading: txLoading } = useQuery({
    queryKey: ["transactions", activeWorkspace?.id],
    queryFn: () => getTransactions(activeWorkspace!.id),
    enabled: activeWorkspace !== null,
    staleTime: 30_000,
  });

  const { data: accounts = [], isLoading: accLoading } = useQuery({
    queryKey: ["financial-accounts", activeWorkspace?.id],
    queryFn: () => getFinancialAccounts(activeWorkspace!.id),
    enabled: activeWorkspace !== null,
    staleTime: 30_000,
  });

  const { data: categories = [], isLoading: catLoading } = useQuery({
    queryKey: ["categories", activeWorkspace?.id],
    queryFn: () => getCategories(activeWorkspace!.id),
    enabled: activeWorkspace !== null,
    staleTime: 30_000,
  });

  const isLoading = txLoading || accLoading || catLoading;

  const categoryMap = useMemo(
    () => new Map(categories.map((c) => [c.id, c])),
    [categories],
  );
  const accountMap = useMemo(
    () => new Map(accounts.map((a) => [a.id, a])),
    [accounts],
  );

  const filtered = useMemo(
    () =>
      transactions.filter((tx) => {
        if (accountFilter.length > 0 && !accountFilter.includes(tx.financialAccountId))
          return false;
        if (typeFilter && tx.type !== typeFilter) return false;
        return true;
      }),
    [transactions, accountFilter, typeFilter],
  );

  const groups = useMemo(() => {
    const map = new Map<string, Transaction[]>();
    for (const tx of filtered) {
      const key = getLocalDateKey(tx.occurredAt);
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(tx);
    }
    return Array.from(map.entries()).map(([dateKey, items]) => ({ dateKey, items }));
  }, [filtered]);

  const hasData = transactions.length > 0;
  const hasFiltered = filtered.length > 0;

  // Empty state message depends on what's missing
  const emptyTitleKey =
    accounts.length === 0
      ? "transactions.emptyNoAccounts"
      : categories.length === 0
        ? "transactions.emptyNoCategories"
        : "transactions.emptyTitle";
  const emptyDescKey =
    accounts.length === 0
      ? "transactions.emptyNoAccountsHint"
      : categories.length === 0
        ? "transactions.emptyNoCategoriesHint"
        : "transactions.emptyDescription";

  return (
    <div>
      {/* top bar */}
      <div className="mb-3 flex items-center justify-end">
        <Button
          size="sm"
          variant="ghost"
          onClick={() => setShowCreate(true)}
          className="h-6 gap-1 px-1.5 text-xs"
        >
          <Plus className="h-3 w-3" aria-hidden="true" />
          {t("transactions.addTransaction")}
        </Button>
      </div>

      <div className="flex gap-6">
        {/* filter panel */}
        {!isLoading && hasData && (
          <FilterSidebar
            accounts={accounts}
            accountFilter={accountFilter}
            onAccountChange={setAccountFilter}
            typeFilter={typeFilter}
            onTypeChange={setTypeFilter}
          />
        )}

        {/* main content */}
        <div className="min-w-0 flex-1">
          {isLoading && <TransactionsSkeleton />}

          {!isLoading && !hasData && (
            <div className="pt-8 text-center">
              <p className="text-sm font-medium">{t(emptyTitleKey)}</p>
              <p className="mt-1 text-sm text-muted-foreground">{t(emptyDescKey)}</p>
            </div>
          )}

          {!isLoading && hasData && !hasFiltered && (
            <p className="py-10 text-center text-sm text-muted-foreground">
              {t("transactions.emptyFiltered")}
            </p>
          )}

          {!isLoading && hasFiltered && (
            <div className="space-y-6">
              {groups.map(({ dateKey, items }) => (
                <DateGroup
                  key={dateKey}
                  dateKey={dateKey}
                  items={items}
                  categoryMap={categoryMap}
                  accountMap={accountMap}
                />
              ))}
            </div>
          )}
        </div>
      </div>

      {showCreate && activeWorkspace && (
        <CreateTransactionModal
          workspaceId={activeWorkspace.id}
          accounts={accounts}
          categories={categories}
          onClose={() => setShowCreate(false)}
        />
      )}
    </div>
  );
}
