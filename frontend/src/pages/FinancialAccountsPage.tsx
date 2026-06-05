import { useEffect, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useWorkspace } from "@/features/workspaces/WorkspaceContext";
import {
  createFinancialAccount,
  deleteFinancialAccount,
  getFinancialAccounts,
  updateFinancialAccount,
  type FinancialAccount,
} from "@/features/financial-accounts/financialAccountsApi";
import { resolveFinancialAccountError } from "@/features/financial-accounts/resolveFinancialAccountError";
import { formatMoney } from "@/lib/formatMoney";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const CURRENCIES = ["RUB", "USD", "EUR"] as const;

// ─── skeleton ───────────────────────────────────────────────────────────────

function AccountSkeleton() {
  return (
    <div className="rounded-lg border border-border bg-card p-4 animate-pulse">
      <div className="h-4 w-32 rounded bg-muted mb-3" />
      <div className="h-6 w-24 rounded bg-muted mb-1" />
      <div className="h-3 w-20 rounded bg-muted" />
    </div>
  );
}

// ─── modal ───────────────────────────────────────────────────────────────────

type ModalProps = {
  children: React.ReactNode;
  onBackdropClick: () => void;
};

function Modal({ children, onBackdropClick }: ModalProps) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
      onClick={(e) => { if (e.target === e.currentTarget) onBackdropClick(); }}
    >
      <div className="w-full max-w-sm rounded-lg border border-border bg-background p-6 shadow-lg">
        {children}
      </div>
    </div>
  );
}

// ─── create modal ─────────────────────────────────────────────────────────────

type CreateModalProps = {
  workspaceId: string;
  onClose: () => void;
};

function CreateAccountModal({ workspaceId, onClose }: CreateModalProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const [name, setName] = useState("");
  const [currency, setCurrency] = useState<string>("RUB");
  const [initialBalance, setInitialBalance] = useState("");

  const mutation = useMutation({
    mutationFn: (payload: { name: string; currency: string; initialBalanceMinor: number }) =>
      createFinancialAccount(workspaceId, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["financial-accounts", workspaceId] });
      onClose();
    },
  });

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const minor = Math.round(parseFloat(initialBalance || "0") * 100);
    mutation.mutate({ name: name.trim(), currency, initialBalanceMinor: minor });
  };

  return (
    <Modal onBackdropClick={onClose}>
      <h2 className="mb-5 text-base font-semibold">{t("financialAccounts.createModal.title")}</h2>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="acc-name">{t("financialAccounts.createModal.nameLabel")}</Label>
          <Input
            id="acc-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder={t("financialAccounts.createModal.namePlaceholder")}
            required
            disabled={mutation.isPending}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="acc-currency">{t("financialAccounts.createModal.currencyLabel")}</Label>
          <select
            id="acc-currency"
            value={currency}
            onChange={(e) => setCurrency(e.target.value)}
            disabled={mutation.isPending}
            className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50"
          >
            {CURRENCIES.map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="acc-initial">{t("financialAccounts.createModal.initialBalanceLabel")}</Label>
          <Input
            id="acc-initial"
            type="number"
            step="0.01"
            value={initialBalance}
            onChange={(e) => setInitialBalance(e.target.value)}
            placeholder="0.00"
            disabled={mutation.isPending}
          />
        </div>

        {mutation.isError && (
          <p className="text-sm text-destructive" role="alert">
            {resolveFinancialAccountError(mutation.error, t as TFunction)}
          </p>
        )}

        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            {t("common.cancel")}
          </Button>
          <Button type="submit" disabled={mutation.isPending || !name.trim()}>
            {mutation.isPending
              ? t("financialAccounts.createModal.submitting")
              : t("financialAccounts.createModal.submit")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

// ─── delete confirm ───────────────────────────────────────────────────────────

type DeleteConfirmProps = {
  onConfirm: () => void;
  onCancel: () => void;
  isPending: boolean;
};

function DeleteConfirm({ onConfirm, onCancel, isPending }: DeleteConfirmProps) {
  const { t } = useTranslation();
  return (
    <Modal onBackdropClick={onCancel}>
      <h2 className="mb-2 text-base font-semibold">{t("financialAccounts.deleteConfirm.title")}</h2>
      <p className="mb-5 text-sm text-muted-foreground">{t("financialAccounts.deleteConfirm.description")}</p>
      <div className="flex justify-end gap-2">
        <Button variant="secondary" onClick={onCancel} disabled={isPending}>
          {t("common.cancel")}
        </Button>
        <Button
          onClick={onConfirm}
          disabled={isPending}
          className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
        >
          {t("common.delete")}
        </Button>
      </div>
    </Modal>
  );
}

// ─── account card ─────────────────────────────────────────────────────────────

type AccountCardProps = {
  account: FinancialAccount;
  workspaceId: string;
};

function AccountCard({ account, workspaceId }: AccountCardProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const [editingName, setEditingName] = useState(false);
  const [nameValue, setNameValue] = useState(account.name);

  const [editingBalance, setEditingBalance] = useState(false);
  const [balanceValue, setBalanceValue] = useState(
    (account.initialBalanceMinor / 100).toFixed(2),
  );

  const [confirmingDelete, setConfirmingDelete] = useState(false);

  useEffect(() => {
    if (!editingName) setNameValue(account.name);
  }, [account.name, editingName]);

  useEffect(() => {
    if (!editingBalance) setBalanceValue((account.initialBalanceMinor / 100).toFixed(2));
  }, [account.initialBalanceMinor, editingBalance]);

  const updateMutation = useMutation({
    mutationFn: (payload: { name?: string; initialBalanceMinor?: number }) =>
      updateFinancialAccount(workspaceId, account.id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["financial-accounts", workspaceId] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteFinancialAccount(workspaceId, account.id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["financial-accounts", workspaceId] });
    },
  });

  const commitName = () => {
    setEditingName(false);
    const trimmed = nameValue.trim();
    if (trimmed && trimmed !== account.name) {
      updateMutation.mutate({ name: trimmed });
    } else {
      setNameValue(account.name);
    }
  };

  const commitBalance = () => {
    setEditingBalance(false);
    const minor = Math.round(parseFloat(balanceValue || "0") * 100);
    if (minor !== account.initialBalanceMinor) {
      updateMutation.mutate({ initialBalanceMinor: minor });
    }
  };

  return (
    <>
      <div className="rounded-lg border border-border bg-card p-4">
        <div className="mb-3 flex items-start justify-between gap-2">
          {editingName ? (
            <Input
              autoFocus
              value={nameValue}
              onChange={(e) => setNameValue(e.target.value)}
              onBlur={commitName}
              onKeyDown={(e) => {
                if (e.key === "Enter") commitName();
                if (e.key === "Escape") { setNameValue(account.name); setEditingName(false); }
              }}
              className="h-7 text-sm font-medium"
            />
          ) : (
            <button
              onClick={() => setEditingName(true)}
              className="text-sm font-medium text-foreground hover:text-muted-foreground transition-colors truncate text-left"
              title={account.name}
            >
              {account.name}
            </button>
          )}

          <button
            onClick={() => setConfirmingDelete(true)}
            disabled={deleteMutation.isPending}
            className="shrink-0 text-muted-foreground hover:text-destructive transition-colors disabled:opacity-50"
            aria-label={t("common.delete")}
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="3 6 5 6 21 6" />
              <path d="M19 6l-1 14H6L5 6" />
              <path d="M10 11v6M14 11v6" />
              <path d="M9 6V4h6v2" />
            </svg>
          </button>
        </div>

        <div className="text-xl font-semibold tabular-nums">
          {formatMoney(account.currentBalanceMinor, account.currency)}
        </div>

        <div className="mt-1 flex items-center gap-1 text-xs text-muted-foreground">
          <span>{t("financialAccounts.initialBalance")}:</span>
          {editingBalance ? (
            <input
              autoFocus
              type="number"
              step="0.01"
              value={balanceValue}
              onChange={(e) => setBalanceValue(e.target.value)}
              onBlur={commitBalance}
              onKeyDown={(e) => {
                if (e.key === "Enter") commitBalance();
                if (e.key === "Escape") {
                  setBalanceValue((account.initialBalanceMinor / 100).toFixed(2));
                  setEditingBalance(false);
                }
              }}
              className="w-24 rounded border border-input bg-transparent px-1 py-0.5 text-xs focus:outline-none focus:ring-1 focus:ring-ring"
            />
          ) : (
            <button
              onClick={() => setEditingBalance(true)}
              className="hover:text-foreground transition-colors"
            >
              {formatMoney(account.initialBalanceMinor, account.currency)}
            </button>
          )}
          <span className="ml-1 opacity-60">{account.currency}</span>
        </div>

        {updateMutation.isError && (
          <p className="mt-1 text-xs text-destructive">
            {resolveFinancialAccountError(updateMutation.error, t as TFunction)}
          </p>
        )}
      </div>

      {confirmingDelete && (
        <DeleteConfirm
          onConfirm={() => { setConfirmingDelete(false); deleteMutation.mutate(); }}
          onCancel={() => setConfirmingDelete(false)}
          isPending={deleteMutation.isPending}
        />
      )}
    </>
  );
}

// ─── page ────────────────────────────────────────────────────────────────────

export function FinancialAccountsPage() {
  const { t } = useTranslation();
  const { activeWorkspace } = useWorkspace();
  const [showCreate, setShowCreate] = useState(false);

  const { data: accounts, isLoading } = useQuery({
    queryKey: ["financial-accounts", activeWorkspace?.id],
    queryFn: () => getFinancialAccounts(activeWorkspace!.id),
    enabled: activeWorkspace !== null,
    staleTime: 30_000,
  });

  return (
    <div className="p-6">
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-xl font-semibold">{t("financialAccounts.title")}</h1>
        <Button size="sm" onClick={() => setShowCreate(true)}>
          {t("financialAccounts.addAccount")}
        </Button>
      </div>

      {isLoading && (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <AccountSkeleton />
          <AccountSkeleton />
          <AccountSkeleton />
        </div>
      )}

      {!isLoading && accounts?.length === 0 && (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <p className="text-sm font-medium">{t("financialAccounts.emptyTitle")}</p>
          <p className="mt-1 text-sm text-muted-foreground">{t("financialAccounts.emptyDescription")}</p>
          <Button className="mt-4" size="sm" onClick={() => setShowCreate(true)}>
            {t("financialAccounts.addAccount")}
          </Button>
        </div>
      )}

      {!isLoading && accounts && accounts.length > 0 && (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {accounts.map((account) => (
            <AccountCard
              key={account.id}
              account={account}
              workspaceId={activeWorkspace!.id}
            />
          ))}
        </div>
      )}

      {showCreate && activeWorkspace && (
        <CreateAccountModal
          workspaceId={activeWorkspace.id}
          onClose={() => setShowCreate(false)}
        />
      )}
    </div>
  );
}
