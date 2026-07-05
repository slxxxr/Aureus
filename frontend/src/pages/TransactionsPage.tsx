import { InputLimits } from "@/lib/inputLimits";
import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowDown, ArrowLeftRight, ArrowUp, Download, Pencil, Plus, Upload } from "lucide-react";
import { cn } from "@/lib/utils";
import { DAY_MS } from "@/lib/constants";
import { formatMoney } from "@/lib/formatMoney";
import { useWorkspace } from "@/features/workspaces/WorkspaceContext";
import {
  getTransactions,
  createTransaction,
  updateTransaction,
  deleteTransaction,
  exportTransactions,
  type Transaction,
  type TransactionType,
} from "@/features/transactions/transactionsApi";
import {
  getTransfers,
  createTransfer,
  updateTransfer,
  deleteTransfer,
  type Transfer,
} from "@/features/transfers/transfersApi";
import { useNameIndex, type NameEntry } from "@/features/transactions/useNameIndex";
import { resolveTransactionError } from "@/features/transactions/resolveTransactionError";
import { resolveTransferError } from "@/features/transfers/resolveTransferError";
import {
  getFinancialAccounts,
  type FinancialAccount,
} from "@/features/financial-accounts/financialAccountsApi";
import { getCategories, type Category } from "@/features/categories/categoriesApi";
import { getProfile } from "@/features/profile/profileApi";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Modal } from "@/components/ui/modal";
import { ImportTransactionsModal } from "@/features/transactions/ImportTransactionsModal";
import { CustomSelect } from "@/components/ui/custom-select";
import { MultiSelect } from "@/components/ui/custom-select";
import { DatePicker } from "@/components/ui/date-picker";
import { useHeaderAction } from "@/app/HeaderActionContext";
import { useTapToEdit } from "@/lib/useTapToEdit";

const MAX_AMOUNT = 1_000_000_000;

// ─── helpers ──────────────────────────────────────────────────────────────────

function localDateKey(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function formatDateLabel(dateKey: string, t: TFunction): string {
  const todayKey = localDateKey(new Date());
  const yesterdayKey = localDateKey(new Date(Date.now() - DAY_MS));
  if (dateKey === todayKey) return t("transactions.date.today");
  if (dateKey === yesterdayKey) return t("transactions.date.yesterday");
  const [y, m, d] = dateKey.split("-");
  return `${d}.${m}.${y}`;
}

function getDailyNet(items: Transaction[]): string | null {
  if (items.length === 0) return null;
  const byCurrency = new Map<string, number>();
  for (const tx of items) {
    const sign = tx.type === "Income" ? 1 : -1;
    byCurrency.set(tx.currency, (byCurrency.get(tx.currency) ?? 0) + sign * tx.amountMinor);
  }
  return Array.from(byCurrency.entries())
    .map(([currency, net]) => (net > 0 ? "+" : "") + formatMoney(net, currency))
    .join(" · ");
}

// ─── create modal ─────────────────────────────────────────────────────────────

type EntryType = TransactionType | "Transfer";

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
  const [entryType, setEntryType] = useState<EntryType>("Expense");
  const [accountId, setAccountId] = useState(accounts[0]?.id ?? "");
  const [categoryId, setCategoryId] = useState("");
  const [fromAccountId, setFromAccountId] = useState(accounts[0]?.id ?? "");
  const [toAccountId, setToAccountId] = useState("");
  const [amount, setAmount] = useState("");
  const [date, setDate] = useState(() => localDateKey(new Date()));
  const [note, setNote] = useState("");
  const [showSuggestions, setShowSuggestions] = useState(false);

  const { search } = useNameIndex(workspaceId);

  const suggestions = useMemo(
    () => (showSuggestions && name ? search(name) : []),
    [name, showSuggestions, search],
  );

  const applyEntry = (entry: NameEntry) => {
    setName(entry.name);
    setEntryType(entry.type);
    setAccountId(entry.accountId);
    setCategoryId(entry.categoryId);
    setAmount((entry.amountMinor / 100).toFixed(2));
    setShowSuggestions(false);
  };

  const isTransfer = entryType === "Transfer";
  const filteredCategories = categories.filter((c) => c.type === entryType);

  const handleTypeChange = (newType: EntryType) => {
    setEntryType(newType);
    if (newType !== "Transfer" && !categories.some((c) => c.id === categoryId && c.type === newType)) {
      setCategoryId("");
    }
  };

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["transactions", workspaceId] });
    void queryClient.invalidateQueries({ queryKey: ["transfers", workspaceId] });
    void queryClient.invalidateQueries({ queryKey: ["financial-accounts", workspaceId] });
  };

  const transactionMutation = useMutation({
    mutationFn: () => {
      return createTransaction(workspaceId, {
        financialAccountId: accountId,
        categoryId,
        name: name.trim(),
        type: entryType as TransactionType,
        amountMinor: Math.round(parseFloat(amount) * 100),
        occurredAt: date,
        note: note.trim() || null,
      });
    },
    onSuccess: () => { invalidate(); onClose(); },
  });

  const transferMutation = useMutation({
    mutationFn: () => {
      return createTransfer(workspaceId, {
        fromAccountId,
        toAccountId,
        amountMinor: Math.round(parseFloat(amount) * 100),
        occurredAt: date,
        note: note.trim() || null,
      });
    },
    onSuccess: () => { invalidate(); onClose(); },
  });

  const mutation = isTransfer ? transferMutation : transactionMutation;

  const toAccountOptions = accounts.filter((a) => a.id !== fromAccountId);

  const missingAccounts = accounts.length === 0;
  const missingCategoryForType = filteredCategories.length === 0;
  const amountNum = parseFloat(amount);
  const amountValid = amount !== "" && !isNaN(amountNum) && amountNum > 0;
  const amountOverMax = amountValid && amountNum > MAX_AMOUNT;
  const canSubmit = isTransfer
    ? !missingAccounts && fromAccountId && toAccountId && amountValid && !amountOverMax
    : !missingAccounts &&
      !missingCategoryForType &&
      name.trim() &&
      accountId &&
      categoryId &&
      amountValid &&
      !amountOverMax;

  return (
    <Modal onBackdropClick={onClose}>
      <h2 className="mb-5 text-base font-semibold">
        {isTransfer ? t("transfers.createModal.title") : t("transactions.createModal.title")}
      </h2>
      <form
        onSubmit={(e: FormEvent) => { e.preventDefault(); if (canSubmit) mutation.mutate(); }}
        className="space-y-4"
      >
        {/* type toggle */}
        <div className="flex rounded-md border border-input">
          {(["Expense", "Income", "Transfer"] as EntryType[]).map((opt) => (
            <button
              key={opt}
              type="button"
              onClick={() => handleTypeChange(opt)}
              disabled={mutation.isPending}
              className={cn(
                "flex-1 py-1.5 text-sm font-medium transition-colors first:rounded-l-[5px] last:rounded-r-[5px]",
                entryType === opt
                  ? "bg-accent text-foreground"
                  : "text-muted-foreground hover:bg-accent/60",
              )}
            >
              {t(
                opt === "Income"
                  ? "categories.typeIncome"
                  : opt === "Expense"
                    ? "categories.typeExpense"
                    : "transfers.type",
              )}
            </button>
          ))}
        </div>

        {!isTransfer && (
          <div className="space-y-1.5">
            <Label htmlFor="tx-name">{t("transactions.createModal.nameLabel")}</Label>
            <div className="relative z-10">
              <Input
                id="tx-name"
                value={name}
                onChange={(e) => { setName(e.target.value); setShowSuggestions(true); }}
                onFocus={() => setShowSuggestions(true)}
                onBlur={() => setShowSuggestions(false)}
                placeholder={t("transactions.createModal.namePlaceholder")}
                required
                autoFocus
                autoComplete="off"
                maxLength={InputLimits.transactionNameMaxLength}
                disabled={mutation.isPending}
              />
              {suggestions.length > 0 && (
                <div className="absolute left-0 right-0 top-full z-20 mt-1 overflow-hidden rounded-md border border-border bg-background shadow-md">
                  {suggestions.map((s) => {
                    const catLabel = categories.find((c) => c.id === s.categoryId)?.name;
                    return (
                      <button
                        key={s.name}
                        type="button"
                        onMouseDown={(e) => { e.preventDefault(); applyEntry(s); }}
                        className="flex w-full items-center justify-between gap-3 px-3 py-2 text-sm hover:bg-accent"
                      >
                        <span className="truncate font-medium">{s.name}</span>
                        {catLabel && (
                          <span className="shrink-0 text-xs text-muted-foreground">{catLabel}</span>
                        )}
                      </button>
                    );
                  })}
                </div>
              )}
            </div>
          </div>
        )}

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
            onBlur={() => {
              const n = parseFloat(amount);
              if (!isNaN(n) && n > 0) setAmount(n.toFixed(2));
              else if (amount !== "") setAmount("");
            }}
            placeholder="0.00"
            required
            autoComplete="off"
            disabled={mutation.isPending}
          />
          {amountOverMax && (
            <p className="text-xs text-destructive">{t("common.validation.amountTooLarge")}</p>
          )}
        </div>

        {isTransfer ? (
          <>
            {/* from account */}
            <div className="space-y-1.5">
              <Label>{t("transfers.createModal.fromAccountLabel")}</Label>
              {missingAccounts ? (
                <p className="rounded-md border border-border bg-muted/40 px-3 py-2 text-xs text-muted-foreground">
                  {t("transfers.createModal.noAccounts")}
                </p>
              ) : (
                <CustomSelect
                  value={fromAccountId}
                  onChange={(v) => {
                    setFromAccountId(v);
                    if (v === toAccountId) setToAccountId("");
                  }}
                  options={accounts.map((a) => ({ value: a.id, label: a.name }))}
                  placeholder={t("transfers.createModal.selectFromAccount")}
                  disabled={mutation.isPending}
                />
              )}
            </div>

            {/* to account */}
            <div className="space-y-1.5">
              <Label>{t("transfers.createModal.toAccountLabel")}</Label>
              <CustomSelect
                value={toAccountId}
                onChange={setToAccountId}
                options={toAccountOptions.map((a) => ({ value: a.id, label: a.name }))}
                placeholder={t("transfers.createModal.selectToAccount")}
                disabled={mutation.isPending || missingAccounts}
              />
            </div>
          </>
        ) : (
          <>
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
          </>
        )}

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
            maxLength={InputLimits.transactionNoteMaxLength}
            disabled={mutation.isPending}
          />
        </div>

        {mutation.isError && (
          <p className="text-sm text-destructive" role="alert">
            {isTransfer
              ? resolveTransferError(mutation.error, t as TFunction)
              : resolveTransactionError(mutation.error, t as TFunction)}
          </p>
        )}

        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            {t("common.cancel")}
          </Button>
          <Button type="submit" disabled={mutation.isPending || !canSubmit}>
            {mutation.isPending
              ? (isTransfer ? t("transfers.createModal.submitting") : t("transactions.createModal.submitting"))
              : (isTransfer ? t("transfers.createModal.submit") : t("transactions.createModal.submit"))}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

// ─── edit modal ───────────────────────────────────────────────────────────────

function EditTransactionModal({
  tx,
  workspaceId,
  categories,
  accounts,
  onClose,
}: {
  tx: Transaction;
  workspaceId: string;
  categories: Category[];
  accounts: FinancialAccount[];
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const [type, setType] = useState<TransactionType>(tx.type);
  const [name, setName] = useState(tx.name);
  const [amount, setAmount] = useState((tx.amountMinor / 100).toFixed(2));
  const [accountId, setAccountId] = useState(tx.financialAccountId);
  const [categoryId, setCategoryId] = useState(tx.categoryId);
  const [date, setDate] = useState(tx.occurredAt);
  const [note, setNote] = useState(tx.note ?? "");
  const [confirmingDelete, setConfirmingDelete] = useState(false);

  const filteredCategories = categories.filter((c) => c.type === type);

  const handleTypeChange = (newType: TransactionType) => {
    setType(newType);
    if (!categories.some((c) => c.id === categoryId && c.type === newType)) {
      setCategoryId("");
    }
  };

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["transactions", workspaceId] });
    void queryClient.invalidateQueries({ queryKey: ["financial-accounts", workspaceId] });
  };

  const updateMutation = useMutation({
    mutationFn: () => {
      const newAmountMinor = Math.round(parseFloat(amount) * 100);
      return updateTransaction(workspaceId, tx.id, {
        name: name.trim() !== tx.name ? name.trim() : undefined,
        amountMinor: newAmountMinor !== tx.amountMinor ? newAmountMinor : undefined,
        categoryId: categoryId !== tx.categoryId ? categoryId : undefined,
        financialAccountId: accountId !== tx.financialAccountId ? accountId : undefined,
        type: type !== tx.type ? type : undefined,
        occurredAt: date !== tx.occurredAt ? date : undefined,
        note: note.trim() !== (tx.note ?? "") ? (note.trim() || null) : undefined,
      });
    },
    onSuccess: () => { invalidate(); onClose(); },
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteTransaction(workspaceId, tx.id),
    onSuccess: () => { invalidate(); onClose(); },
  });

  const isPending = updateMutation.isPending || deleteMutation.isPending;
  const amountNum = parseFloat(amount);
  const amountValid = amount !== "" && !isNaN(amountNum) && amountNum > 0;
  const amountOverMax = amountValid && amountNum > MAX_AMOUNT;
  const missingCategoryForType = filteredCategories.length === 0;
  const canSave = name.trim() && categoryId && amountValid && !amountOverMax && !missingCategoryForType;

  if (confirmingDelete) {
    return (
      <Modal onBackdropClick={() => setConfirmingDelete(false)}>
        <h2 className="mb-2 text-base font-semibold">{t("transactions.deleteConfirm.title")}</h2>
        <p className="mb-5 text-sm text-muted-foreground">{t("transactions.deleteConfirm.description")}</p>
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setConfirmingDelete(false)} disabled={deleteMutation.isPending}>
            {t("common.cancel")}
          </Button>
          <Button
            disabled={deleteMutation.isPending}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            onClick={() => deleteMutation.mutate()}
          >
            {t("common.delete")}
          </Button>
        </div>
      </Modal>
    );
  }

  return (
    <Modal onBackdropClick={onClose}>
      <h2 className="mb-5 text-base font-semibold">{t("transactions.editModal.title")}</h2>
      <form
        onSubmit={(e: FormEvent) => { e.preventDefault(); if (canSave) updateMutation.mutate(); }}
        className="space-y-4"
      >
        {/* type toggle */}
        <div className="flex rounded-md border border-input">
          {(["Expense", "Income"] as TransactionType[]).map((opt) => (
            <button
              key={opt}
              type="button"
              onClick={() => handleTypeChange(opt)}
              disabled={isPending}
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
          <Label htmlFor="edit-tx-name">{t("transactions.editModal.nameLabel")}</Label>
          <Input
            id="edit-tx-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            autoFocus
            autoComplete="off"
            maxLength={InputLimits.transactionNameMaxLength}
            disabled={isPending}
          />
        </div>

        {/* amount */}
        <div className="space-y-1.5">
          <Label htmlFor="edit-tx-amount">{t("transactions.editModal.amountLabel")}</Label>
          <Input
            id="edit-tx-amount"
            type="text"
            inputMode="decimal"
            value={amount}
            onChange={(e) => {
              const val = e.target.value.replace(",", ".");
              if (val === "" || /^\d*\.?\d{0,2}$/.test(val)) setAmount(val);
            }}
            onBlur={() => {
              const n = parseFloat(amount);
              if (!isNaN(n) && n > 0) setAmount(n.toFixed(2));
              else if (amount !== "") setAmount("");
            }}
            required
            autoComplete="off"
            disabled={isPending}
          />
          {amountOverMax && (
            <p className="text-xs text-destructive">{t("common.validation.amountTooLarge")}</p>
          )}
        </div>

        {/* account */}
        <div className="space-y-1.5">
          <Label>{t("transactions.editModal.accountLabel")}</Label>
          <CustomSelect
            value={accountId}
            onChange={setAccountId}
            options={accounts.map((a) => ({ value: a.id, label: a.name }))}
            placeholder={t("transactions.createModal.selectAccount")}
            disabled={isPending}
          />
        </div>

        {/* category */}
        <div className="space-y-1.5">
          <Label>{t("transactions.editModal.categoryLabel")}</Label>
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
              disabled={isPending}
            />
          )}
        </div>

        {/* date */}
        <div className="space-y-1.5">
          <Label>{t("transactions.editModal.dateLabel")}</Label>
          <DatePicker value={date} onChange={setDate} disabled={isPending} />
        </div>

        {/* note */}
        <div className="space-y-1.5">
          <Label htmlFor="edit-tx-note">{t("transactions.editModal.noteLabel")}</Label>
          <Input
            id="edit-tx-note"
            value={note}
            onChange={(e) => setNote(e.target.value)}
            placeholder={t("transactions.editModal.notePlaceholder")}
            autoComplete="off"
            maxLength={InputLimits.transactionNoteMaxLength}
            disabled={isPending}
          />
        </div>

        {updateMutation.isError && (
          <p className="text-sm text-destructive" role="alert">
            {resolveTransactionError(updateMutation.error, t as TFunction)}
          </p>
        )}

        <div className="flex items-center justify-between pt-1">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => setConfirmingDelete(true)}
            disabled={isPending}
            className="text-destructive hover:bg-destructive/10 hover:text-destructive"
          >
            {t("transactions.editModal.deleteTransaction")}
          </Button>
          <div className="flex gap-2">
            <Button type="button" variant="secondary" onClick={onClose} disabled={isPending}>
              {t("common.cancel")}
            </Button>
            <Button type="submit" disabled={isPending || !canSave}>
              {updateMutation.isPending ? t("transactions.editModal.saving") : t("transactions.editModal.save")}
            </Button>
          </div>
        </div>
      </form>
    </Modal>
  );
}

// ─── edit transfer modal ──────────────────────────────────────────────────────

function EditTransferModal({
  transfer,
  workspaceId,
  accounts,
  onClose,
}: {
  transfer: Transfer;
  workspaceId: string;
  accounts: FinancialAccount[];
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const [fromAccountId, setFromAccountId] = useState(transfer.fromAccountId);
  const [toAccountId, setToAccountId] = useState(transfer.toAccountId);
  const [amount, setAmount] = useState((transfer.amountMinor / 100).toFixed(2));
  const [date, setDate] = useState(transfer.occurredAt);
  const [note, setNote] = useState(transfer.note ?? "");
  const [confirmingDelete, setConfirmingDelete] = useState(false);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["transfers", workspaceId] });
    void queryClient.invalidateQueries({ queryKey: ["financial-accounts", workspaceId] });
  };

  const updateMutation = useMutation({
    mutationFn: () => {
      const newAmountMinor = Math.round(parseFloat(amount) * 100);
      return updateTransfer(workspaceId, transfer.id, {
        fromAccountId: fromAccountId !== transfer.fromAccountId ? fromAccountId : undefined,
        toAccountId: toAccountId !== transfer.toAccountId ? toAccountId : undefined,
        amountMinor: newAmountMinor !== transfer.amountMinor ? newAmountMinor : undefined,
        occurredAt: date !== transfer.occurredAt ? date : undefined,
        note: note.trim() !== (transfer.note ?? "") ? (note.trim() || null) : undefined,
      });
    },
    onSuccess: () => { invalidate(); onClose(); },
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteTransfer(workspaceId, transfer.id),
    onSuccess: () => { invalidate(); onClose(); },
  });

  const isPending = updateMutation.isPending || deleteMutation.isPending;
  const toAccountOptions = accounts.filter((a) => a.id !== fromAccountId);
  const amountNum = parseFloat(amount);
  const amountValid = amount !== "" && !isNaN(amountNum) && amountNum > 0;
  const amountOverMax = amountValid && amountNum > MAX_AMOUNT;
  const canSave = fromAccountId && toAccountId && amountValid && !amountOverMax;

  if (confirmingDelete) {
    return (
      <Modal onBackdropClick={() => setConfirmingDelete(false)}>
        <h2 className="mb-2 text-base font-semibold">{t("transfers.deleteConfirm.title")}</h2>
        <p className="mb-5 text-sm text-muted-foreground">{t("transfers.deleteConfirm.description")}</p>
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setConfirmingDelete(false)} disabled={deleteMutation.isPending}>
            {t("common.cancel")}
          </Button>
          <Button
            disabled={deleteMutation.isPending}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            onClick={() => deleteMutation.mutate()}
          >
            {t("common.delete")}
          </Button>
        </div>
      </Modal>
    );
  }

  return (
    <Modal onBackdropClick={onClose}>
      <h2 className="mb-5 text-base font-semibold">{t("transfers.editModal.title")}</h2>
      <form
        onSubmit={(e: FormEvent) => { e.preventDefault(); if (canSave) updateMutation.mutate(); }}
        className="space-y-4"
      >
        {/* amount */}
        <div className="space-y-1.5">
          <Label htmlFor="edit-transfer-amount">{t("transfers.editModal.amountLabel")}</Label>
          <Input
            id="edit-transfer-amount"
            type="text"
            inputMode="decimal"
            value={amount}
            onChange={(e) => {
              const val = e.target.value.replace(",", ".");
              if (val === "" || /^\d*\.?\d{0,2}$/.test(val)) setAmount(val);
            }}
            onBlur={() => {
              const n = parseFloat(amount);
              if (!isNaN(n) && n > 0) setAmount(n.toFixed(2));
              else if (amount !== "") setAmount("");
            }}
            required
            autoFocus
            autoComplete="off"
            disabled={isPending}
          />
          {amountOverMax && (
            <p className="text-xs text-destructive">{t("common.validation.amountTooLarge")}</p>
          )}
        </div>

        {/* from account */}
        <div className="space-y-1.5">
          <Label>{t("transfers.createModal.fromAccountLabel")}</Label>
          <CustomSelect
            value={fromAccountId}
            onChange={(v) => {
              setFromAccountId(v);
              if (v === toAccountId) setToAccountId("");
            }}
            options={accounts.map((a) => ({ value: a.id, label: a.name }))}
            placeholder={t("transfers.createModal.selectFromAccount")}
            disabled={isPending}
          />
        </div>

        {/* to account */}
        <div className="space-y-1.5">
          <Label>{t("transfers.createModal.toAccountLabel")}</Label>
          <CustomSelect
            value={toAccountId}
            onChange={setToAccountId}
            options={toAccountOptions.map((a) => ({ value: a.id, label: a.name }))}
            placeholder={t("transfers.createModal.selectToAccount")}
            disabled={isPending}
          />
        </div>

        {/* date */}
        <div className="space-y-1.5">
          <Label>{t("transfers.editModal.dateLabel")}</Label>
          <DatePicker value={date} onChange={setDate} disabled={isPending} />
        </div>

        {/* note */}
        <div className="space-y-1.5">
          <Label htmlFor="edit-transfer-note">{t("transfers.editModal.noteLabel")}</Label>
          <Input
            id="edit-transfer-note"
            value={note}
            onChange={(e) => setNote(e.target.value)}
            placeholder={t("transfers.editModal.notePlaceholder")}
            autoComplete="off"
            maxLength={InputLimits.transactionNoteMaxLength}
            disabled={isPending}
          />
        </div>

        {updateMutation.isError && (
          <p className="text-sm text-destructive" role="alert">
            {resolveTransferError(updateMutation.error, t as TFunction)}
          </p>
        )}

        <div className="flex items-center justify-between pt-1">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => setConfirmingDelete(true)}
            disabled={isPending}
            className="text-destructive hover:bg-destructive/10 hover:text-destructive"
          >
            {t("transfers.editModal.deleteTransfer")}
          </Button>
          <div className="flex gap-2">
            <Button type="button" variant="secondary" onClick={onClose} disabled={isPending}>
              {t("common.cancel")}
            </Button>
            <Button type="submit" disabled={isPending || !canSave}>
              {updateMutation.isPending ? t("transfers.editModal.saving") : t("transfers.editModal.save")}
            </Button>
          </div>
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
  onEdit,
  canEdit,
}: {
  tx: Transaction;
  categoryMap: Map<string, Category>;
  accountMap: Map<string, FinancialAccount>;
  onEdit: () => void;
  canEdit: boolean;
}) {
  const { t } = useTranslation();
  const tapHandlers = useTapToEdit(onEdit, canEdit);
  const isIncome = tx.type === "Income";
  const category = categoryMap.get(tx.categoryId);
  const account = accountMap.get(tx.financialAccountId);

  return (
    <div
      {...tapHandlers}
      className={cn(
        "group flex items-center gap-3 rounded-lg px-3 py-2.5 transition-colors",
        canEdit && "cursor-pointer active:bg-accent/60 [@media(hover:hover)]:hover:bg-accent/60",
      )}
    >
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

      {/* edit pencil — desktop hover only, hidden on mobile */}
      <span
        aria-hidden="true"
        className={cn(
          "hidden shrink-0 rounded p-0.5 text-muted-foreground opacity-0 transition-opacity sm:block",
          canEdit ? "group-hover:opacity-100" : "invisible",
        )}
      >
        <Pencil className="h-3.5 w-3.5" />
      </span>
    </div>
  );
}

// ─── transfer row ─────────────────────────────────────────────────────────────

function TransferRow({
  transfer,
  accountMap,
  onEdit,
  canEdit,
}: {
  transfer: Transfer;
  accountMap: Map<string, FinancialAccount>;
  onEdit: () => void;
  canEdit: boolean;
}) {
  const { t } = useTranslation();
  const tapHandlers = useTapToEdit(onEdit, canEdit);
  const fromAccount = accountMap.get(transfer.fromAccountId);
  const toAccount = accountMap.get(transfer.toAccountId);

  return (
    <div
      {...tapHandlers}
      className={cn(
        "group flex items-center gap-3 rounded-lg px-3 py-2.5 transition-colors",
        canEdit && "cursor-pointer active:bg-accent/60 [@media(hover:hover)]:hover:bg-accent/60",
      )}
    >
      {/* type icon */}
      <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-muted text-muted-foreground">
        <ArrowLeftRight className="h-3.5 w-3.5" />
      </div>

      {/* from → to */}
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium">{t("transfers.type")}</p>
        <p className="truncate text-xs text-muted-foreground">
          {(fromAccount?.name ?? t("transfers.unknownAccount"))} → {(toAccount?.name ?? t("transfers.unknownAccount"))}
        </p>
      </div>

      {/* amount */}
      <div className="shrink-0 text-right">
        <p className="text-sm font-semibold tabular-nums text-muted-foreground">
          {formatMoney(transfer.amountMinor, transfer.currency)}
        </p>
      </div>

      {/* edit pencil — desktop hover only, hidden on mobile */}
      <span
        aria-hidden="true"
        className={cn(
          "hidden shrink-0 rounded p-0.5 text-muted-foreground opacity-0 transition-opacity sm:block",
          canEdit ? "group-hover:opacity-100" : "invisible",
        )}
      >
        <Pencil className="h-3.5 w-3.5" />
      </span>
    </div>
  );
}

// ─── date group ───────────────────────────────────────────────────────────────

type EntryItem =
  | { kind: "tx"; tx: Transaction }
  | { kind: "transfer"; transfer: Transfer };

function DateGroup({
  dateKey,
  items,
  categoryMap,
  accountMap,
  onEditTx,
  canEditTx,
  onEditTransfer,
  canEditTransfer,
}: {
  dateKey: string;
  items: EntryItem[];
  categoryMap: Map<string, Category>;
  accountMap: Map<string, FinancialAccount>;
  onEditTx: (tx: Transaction) => void;
  canEditTx: (tx: Transaction) => boolean;
  onEditTransfer: (transfer: Transfer) => void;
  canEditTransfer: (transfer: Transfer) => boolean;
}) {
  const { t } = useTranslation();
  const net = getDailyNet(items.filter((i): i is { kind: "tx"; tx: Transaction } => i.kind === "tx").map((i) => i.tx));

  return (
    <div>
      <div className="mb-1 flex items-center gap-3 px-3">
        <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          {formatDateLabel(dateKey, t as TFunction)}
        </span>
        {net && (
          <span className="ml-auto text-xs font-medium tabular-nums text-muted-foreground">{net}</span>
        )}
        <span className="hidden w-[18px] shrink-0 sm:block" aria-hidden="true" />
      </div>
      <div className="space-y-0.5">
        {items.map((item) =>
          item.kind === "tx" ? (
            <TransactionRow
              key={item.tx.id}
              tx={item.tx}
              categoryMap={categoryMap}
              accountMap={accountMap}
              onEdit={() => onEditTx(item.tx)}
              canEdit={canEditTx(item.tx)}
            />
          ) : (
            <TransferRow
              key={item.transfer.id}
              transfer={item.transfer}
              accountMap={accountMap}
              onEdit={() => onEditTransfer(item.transfer)}
              canEdit={canEditTransfer(item.transfer)}
            />
          ),
        )}
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
  isExporting,
  onExport,
  canImport,
  onImport,
}: {
  accounts: FinancialAccount[];
  accountFilter: string[];
  onAccountChange: (v: string[]) => void;
  typeFilter: "" | TransactionType;
  onTypeChange: (v: "" | TransactionType) => void;
  isExporting: boolean;
  onExport: () => void;
  canImport: boolean;
  onImport: () => void;
}) {
  const { t } = useTranslation();

  const typeOptions: { value: "" | TransactionType; label: string }[] = [
    { value: "", label: t("transactions.filters.typeAll") },
    { value: "Income", label: t("transactions.filters.typeIncome") },
    { value: "Expense", label: t("transactions.filters.typeExpense") },
  ];

  return (
    <aside className="hidden w-48 shrink-0 md:block">
      <div className="sticky top-0 space-y-5 pt-10">
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

        <div className="border-t border-border pt-4 space-y-0.5">
          <button
            type="button"
            disabled={isExporting}
            onClick={onExport}
            className="flex w-full items-center gap-1.5 rounded px-2 py-1.5 text-left text-sm text-muted-foreground transition-colors hover:bg-accent/60 hover:text-foreground disabled:pointer-events-none disabled:opacity-50"
          >
            <Download className="h-3.5 w-3.5" aria-hidden="true" />
            {t("transactions.export")}
          </button>
          {canImport && (
            <button
              type="button"
              onClick={onImport}
              className="flex w-full items-center gap-1.5 rounded px-2 py-1.5 text-left text-sm text-muted-foreground transition-colors hover:bg-accent/60 hover:text-foreground"
            >
              <Upload className="h-3.5 w-3.5" aria-hidden="true" />
              {t("transactions.import")}
            </button>
          )}
        </div>
      </div>
    </aside>
  );
}

// ─── mobile filter bar ───────────────────────────────────────────────────────

function MobileFilterBar({
  accounts,
  accountFilter,
  onAccountChange,
  typeFilter,
  onTypeChange,
  isExporting,
  onExport,
  canImport,
  onImport,
}: {
  accounts: FinancialAccount[];
  accountFilter: string[];
  onAccountChange: (v: string[]) => void;
  typeFilter: "" | TransactionType;
  onTypeChange: (v: "" | TransactionType) => void;
  isExporting: boolean;
  onExport: () => void;
  canImport: boolean;
  onImport: () => void;
}) {
  const { t } = useTranslation();

  const typeOptions: { value: "" | TransactionType; label: string }[] = [
    { value: "", label: t("transactions.filters.typeAll") },
    { value: "Income", label: t("transactions.filters.typeIncome") },
    { value: "Expense", label: t("transactions.filters.typeExpense") },
  ];

  return (
    <div className="mb-4 flex flex-col gap-2 md:hidden">
      <MultiSelect
        values={accountFilter}
        onChange={onAccountChange}
        options={accounts.map((a) => ({ value: a.id, label: a.name }))}
        allLabel={t("transactions.filters.allAccounts")}
      />
      <div className="flex items-center gap-1">
        <div className="flex flex-1 gap-1">
          {typeOptions.map((opt) => (
            <button
              key={opt.value}
              type="button"
              onClick={() => onTypeChange(opt.value)}
              className={cn(
                "flex-1 rounded px-2 py-1.5 text-sm transition-colors",
                typeFilter === opt.value
                  ? "bg-accent font-medium text-foreground"
                  : "text-muted-foreground hover:bg-accent/60",
              )}
            >
              {opt.label}
            </button>
          ))}
        </div>
        <button
          type="button"
          disabled={isExporting}
          onClick={onExport}
          className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-accent/60 hover:text-foreground disabled:opacity-50"
          aria-label={t("transactions.export")}
          title={t("transactions.export")}
        >
          <Download className="h-4 w-4" aria-hidden="true" />
        </button>
        {canImport && (
          <button
            type="button"
            onClick={onImport}
            className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-accent/60 hover:text-foreground"
            aria-label={t("transactions.import")}
            title={t("transactions.import")}
          >
            <Upload className="h-4 w-4" aria-hidden="true" />
          </button>
        )}
      </div>
    </div>
  );
}

// ─── page ─────────────────────────────────────────────────────────────────────

export function TransactionsPage() {
  const { t } = useTranslation();
  const { activeWorkspace } = useWorkspace();
  const { setAction } = useHeaderAction();
  const [accountFilter, setAccountFilter] = useState<string[]>([]);
  const [typeFilter, setTypeFilter] = useState<"" | TransactionType>("");
  const [showCreate, setShowCreate] = useState(false);
  const [showImport, setShowImport] = useState(false);
  const [isExporting, setIsExporting] = useState(false);
  const [editing, setEditing] = useState<Transaction | null>(null);
  const [editingTransfer, setEditingTransfer] = useState<Transfer | null>(null);

  const { data: transactions = [], isLoading: txLoading } = useQuery({
    queryKey: ["transactions", activeWorkspace?.id],
    queryFn: () => getTransactions(activeWorkspace!.id),
    enabled: activeWorkspace !== null,
    staleTime: 30_000,
  });

  const { data: transfers = [], isLoading: transfersLoading } = useQuery({
    queryKey: ["transfers", activeWorkspace?.id],
    queryFn: () => getTransfers(activeWorkspace!.id),
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

  const { data: profile } = useQuery({
    queryKey: ["profile"],
    queryFn: getProfile,
    staleTime: 5 * 60 * 1000,
  });

  const canEdit = (tx: Transaction) =>
    activeWorkspace?.role !== "Member" || tx.createdByUserId === profile?.id;

  const canEditTransfer = (transfer: Transfer) =>
    activeWorkspace?.role !== "Member" || transfer.createdByUserId === profile?.id;

  const canImport = activeWorkspace?.role === "Manager" || activeWorkspace?.role === "Owner";

  const isLoading = txLoading || transfersLoading || accLoading || catLoading;

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

  const filteredTransfers = useMemo(
    () =>
      typeFilter
        ? []
        : transfers.filter((transfer) => {
            if (accountFilter.length === 0) return true;
            return accountFilter.includes(transfer.fromAccountId) || accountFilter.includes(transfer.toAccountId);
          }),
    [transfers, accountFilter, typeFilter],
  );

  const groups = useMemo(() => {
    const map = new Map<string, EntryItem[]>();
    for (const tx of filtered) {
      const key = tx.occurredAt;
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push({ kind: "tx", tx });
    }
    for (const transfer of filteredTransfers) {
      const key = transfer.occurredAt;
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push({ kind: "transfer", transfer });
    }
    for (const items of map.values()) {
      items.sort((a, b) => {
        const aCreated = a.kind === "tx" ? a.tx.createdAt : a.transfer.createdAt;
        const bCreated = b.kind === "tx" ? b.tx.createdAt : b.transfer.createdAt;
        return bCreated.localeCompare(aCreated);
      });
    }
    return Array.from(map.entries())
      .sort((a, b) => b[0].localeCompare(a[0]))
      .map(([dateKey, items]) => ({ dateKey, items }));
  }, [filtered, filteredTransfers]);

  const hasData = transactions.length > 0 || transfers.length > 0;
  const hasFiltered = filtered.length > 0 || filteredTransfers.length > 0;

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

  useEffect(() => {
    setAction(
      <Button size="sm" variant="ghost" onClick={() => setShowCreate(true)} className="gap-1.5">
        <Plus className="h-3.5 w-3.5" aria-hidden="true" />
        {t("transactions.addTransaction")}
      </Button>,
    );
    return () => { setAction(null); };
  }, [setAction, setShowCreate, t]);

  const handleExport = async () => {
    setIsExporting(true);
    try {
      await exportTransactions(activeWorkspace!.id, {
        accountIds: accountFilter.length > 0 ? accountFilter : undefined,
        type: typeFilter || undefined,
      });
    } finally {
      setIsExporting(false);
    }
  };

  const filterProps = {
    accounts,
    accountFilter,
    onAccountChange: setAccountFilter,
    typeFilter,
    onTypeChange: setTypeFilter,
    isExporting,
    onExport: handleExport,
    canImport,
    onImport: () => setShowImport(true),
  };

  return (
    <div className="flex gap-6">
      {/* filter panel — desktop only */}
      {!isLoading && hasData && <FilterSidebar {...filterProps} />}

      {/* main content */}
      <div className="min-w-0 flex-1">
        {/* add button — sticky on desktop; on mobile it's rendered in the app header via context */}
        <div className="sticky top-0 z-10 hidden justify-end bg-background pb-3 pt-9 pr-8 md:flex">
          <Button
            size="sm"
            variant="ghost"
            onClick={() => setShowCreate(true)}
            className="gap-1.5"
          >
            <Plus className="h-3.5 w-3.5" aria-hidden="true" />
            {t("transactions.addTransaction")}
          </Button>
        </div>

        {/* mobile filter bar — shown instead of sidebar */}
        {!isLoading && hasData && <MobileFilterBar {...filterProps} />}

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
                onEditTx={setEditing}
                canEditTx={canEdit}
                onEditTransfer={setEditingTransfer}
                canEditTransfer={canEditTransfer}
              />
            ))}
          </div>
        )}
      </div>

      {showCreate && activeWorkspace && (
        <CreateTransactionModal
          workspaceId={activeWorkspace.id}
          accounts={accounts}
          categories={categories}
          onClose={() => setShowCreate(false)}
        />
      )}
      {editing && activeWorkspace && (
        <EditTransactionModal
          tx={editing}
          workspaceId={activeWorkspace.id}
          categories={categories}
          accounts={accounts}
          onClose={() => setEditing(null)}
        />
      )}
      {editingTransfer && activeWorkspace && (
        <EditTransferModal
          transfer={editingTransfer}
          workspaceId={activeWorkspace.id}
          accounts={accounts}
          onClose={() => setEditingTransfer(null)}
        />
      )}
      {showImport && activeWorkspace && (
        <ImportTransactionsModal
          workspaceId={activeWorkspace.id}
          onClose={() => setShowImport(false)}
        />
      )}
    </div>
  );
}
