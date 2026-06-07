import { useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil, Plus } from "lucide-react";
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
import { Modal } from "@/components/ui/modal";

const CURRENCIES = ["RUB", "USD", "EUR"] as const;
const MAX_AMOUNT = 1_000_000_000;

// ─── currency select ──────────────────────────────────────────────────────────

function CurrencySelect({
  value,
  onChange,
  disabled,
}: {
  value: string;
  onChange: (v: string) => void;
  disabled?: boolean;
}) {
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
      className="flex h-9 w-full appearance-none rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
    >
      {CURRENCIES.map((c) => (
        <option key={c} value={c}>{c}</option>
      ))}
    </select>
  );
}

// ─── skeleton ─────────────────────────────────────────────────────────────────

function AccountSkeleton() {
  return (
    <div className="rounded-lg border border-border bg-card p-5 animate-pulse space-y-3">
      <div className="h-4 w-28 rounded bg-muted" />
      <div className="h-7 w-32 rounded bg-muted" />
      <div className="h-3 w-12 rounded bg-muted" />
    </div>
  );
}

// ─── create modal ─────────────────────────────────────────────────────────────

function CreateAccountModal({ workspaceId, onClose }: { workspaceId: string; onClose: () => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const [name, setName] = useState("");
  const [currency, setCurrency] = useState("RUB");
  const [initialBalance, setInitialBalance] = useState("");

  const balanceOverMax = initialBalance !== "" && parseFloat(initialBalance) > MAX_AMOUNT;

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
    if (balanceOverMax) return;
    mutation.mutate({
      name: name.trim(),
      currency,
      initialBalanceMinor: Math.round(parseFloat(initialBalance || "0") * 100),
    });
  };

  return (
    <Modal onBackdropClick={onClose}>
      <h2 className="mb-5 text-base font-semibold">{t("financialAccounts.createModal.title")}</h2>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="create-name">{t("financialAccounts.createModal.nameLabel")}</Label>
          <Input
            id="create-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder={t("financialAccounts.createModal.namePlaceholder")}
            required
            autoFocus
            autoComplete="off"
            maxLength={120}
            disabled={mutation.isPending}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="create-currency">{t("financialAccounts.createModal.currencyLabel")}</Label>
          <CurrencySelect value={currency} onChange={setCurrency} disabled={mutation.isPending} />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="create-balance">{t("financialAccounts.createModal.initialBalanceLabel")}</Label>
          <Input
            id="create-balance"
            type="text"
            inputMode="decimal"
            value={initialBalance}
            onChange={(e) => {
              const val = e.target.value.replace(",", ".");
              if (val === "" || /^\d*\.?\d{0,2}$/.test(val)) setInitialBalance(val);
            }}
            placeholder="0.00"
            autoComplete="off"
            disabled={mutation.isPending}
          />
          {balanceOverMax && (
            <p className="text-xs text-destructive">Максимум: 1 000 000 000</p>
          )}
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
            {mutation.isPending ? t("financialAccounts.createModal.submitting") : t("financialAccounts.createModal.submit")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

// ─── edit modal ───────────────────────────────────────────────────────────────

function EditAccountModal({
  account,
  workspaceId,
  onClose,
}: {
  account: FinancialAccount;
  workspaceId: string;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const [name, setName] = useState(account.name);
  const [initialBalance, setInitialBalance] = useState((account.initialBalanceMinor / 100).toFixed(2));
  const [confirmingDelete, setConfirmingDelete] = useState(false);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["financial-accounts", workspaceId] });
  };
  const invalidateAll = () => {
    invalidate();
    void queryClient.invalidateQueries({ queryKey: ["transactions", workspaceId] });
  };

  const balanceOverMax = initialBalance !== "" && parseFloat(initialBalance) > MAX_AMOUNT;
  const newInitialMinor = Math.round(parseFloat(initialBalance || "0") * 100);

  const updateMutation = useMutation({
    mutationFn: () =>
      updateFinancialAccount(workspaceId, account.id, {
        name: name.trim() !== account.name ? name.trim() : undefined,
        initialBalanceMinor: newInitialMinor !== account.initialBalanceMinor ? newInitialMinor : undefined,
      }),
    onSuccess: () => { invalidate(); onClose(); },
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteFinancialAccount(workspaceId, account.id),
    onSuccess: () => { invalidateAll(); onClose(); },
  });

  const isPending = updateMutation.isPending || deleteMutation.isPending;
  const canUpdate = name.trim() && !balanceOverMax;

  if (confirmingDelete) {
    return (
      <Modal onBackdropClick={() => setConfirmingDelete(false)}>
        <h2 className="mb-2 text-base font-semibold">{t("financialAccounts.deleteConfirm.title")}</h2>
        <p className="mb-5 text-sm text-muted-foreground">{t("financialAccounts.deleteConfirm.description")}</p>
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
      <h2 className="mb-5 text-base font-semibold">{t("financialAccounts.editModal.title")}</h2>
      <form onSubmit={(e) => { e.preventDefault(); updateMutation.mutate(); }} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="edit-name">{t("financialAccounts.editModal.nameLabel")}</Label>
          <Input
            id="edit-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            autoFocus
            autoComplete="off"
            maxLength={120}
            disabled={isPending}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="edit-balance">{t("financialAccounts.editModal.initialBalanceLabel")}</Label>
          <Input
            id="edit-balance"
            type="text"
            inputMode="decimal"
            value={initialBalance}
            onChange={(e) => {
              const val = e.target.value.replace(",", ".");
              if (val === "" || /^\d*\.?\d{0,2}$/.test(val)) setInitialBalance(val);
            }}
            autoComplete="off"
            disabled={isPending}
          />
          {balanceOverMax && (
            <p className="text-xs text-destructive">Максимум: 1 000 000 000</p>
          )}
        </div>

        {updateMutation.isError && (
          <p className="text-sm text-destructive" role="alert">
            {resolveFinancialAccountError(updateMutation.error, t as TFunction)}
          </p>
        )}

        <div className="flex items-center justify-between pt-1">
          <button
            type="button"
            onClick={() => setConfirmingDelete(true)}
            disabled={isPending}
            className="text-sm text-destructive hover:underline disabled:opacity-50"
          >
            {t("financialAccounts.editModal.deleteAccount")}
          </button>
          <div className="flex gap-2">
            <Button type="button" variant="secondary" onClick={onClose} disabled={isPending}>
              {t("common.cancel")}
            </Button>
            <Button type="submit" disabled={isPending || !canUpdate}>
              {updateMutation.isPending ? t("financialAccounts.editModal.saving") : t("financialAccounts.editModal.save")}
            </Button>
          </div>
        </div>
      </form>
    </Modal>
  );
}

// ─── account card ─────────────────────────────────────────────────────────────

function AccountCard({ account, workspaceId }: { account: FinancialAccount; workspaceId: string }) {
  const { t } = useTranslation();
  const [editing, setEditing] = useState(false);

  return (
    <>
      <div className="group rounded-lg border border-border bg-card p-5 transition-shadow hover:shadow-sm">
        <div className="mb-1 flex items-start justify-between gap-2">
          <p className="truncate text-sm font-medium text-foreground" title={account.name}>
            {account.name}
          </p>
          <button
            onClick={() => setEditing(true)}
            className="shrink-0 rounded p-0.5 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100 hover:text-foreground focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            aria-label={t("financialAccounts.editModal.title")}
          >
            <Pencil className="h-3.5 w-3.5" />
          </button>
        </div>

        <p className="text-2xl font-semibold tabular-nums">
          {formatMoney(account.currentBalanceMinor, account.currency)}
        </p>

        <span className="mt-2 inline-block rounded-sm bg-muted px-1.5 py-0.5 text-xs font-medium text-muted-foreground">
          {account.currency}
        </span>
      </div>

      {editing && (
        <EditAccountModal account={account} workspaceId={workspaceId} onClose={() => setEditing(false)} />
      )}
    </>
  );
}

// ─── page ─────────────────────────────────────────────────────────────────────

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
    <div>
      {/* always-visible ghost button */}
      <div className="mb-3 flex justify-end">
        <Button size="sm" variant="ghost" onClick={() => setShowCreate(true)} className="h-6 gap-1 px-1.5 text-xs">
          <Plus className="h-3 w-3" aria-hidden="true" />
          {t("financialAccounts.addAccount")}
        </Button>
      </div>

      {isLoading && (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <AccountSkeleton />
          <AccountSkeleton />
          <AccountSkeleton />
          <AccountSkeleton />
        </div>
      )}

      {!isLoading && accounts?.length === 0 && (
        <div className="pt-8 text-center">
          <p className="text-sm font-medium">{t("financialAccounts.emptyTitle")}</p>
          <p className="mt-1 text-sm text-muted-foreground">{t("financialAccounts.emptyDescription")}</p>
        </div>
      )}

      {!isLoading && accounts && accounts.length > 0 && (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {accounts.map((account) => (
            <AccountCard key={account.id} account={account} workspaceId={activeWorkspace!.id} />
          ))}
        </div>
      )}

      {showCreate && activeWorkspace && (
        <CreateAccountModal workspaceId={activeWorkspace.id} onClose={() => setShowCreate(false)} />
      )}
    </div>
  );
}
