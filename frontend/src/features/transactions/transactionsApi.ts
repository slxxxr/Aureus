import { apiFetch, ApiError } from "@/lib/apiClient";
import { TOKEN_STORAGE_KEY } from "@/lib/constants";

export type TransactionType = "Income" | "Expense";

export type Transaction = {
  id: string;
  financialAccountId: string;
  categoryId: string;
  createdByUserId: string;
  name: string;
  type: TransactionType;
  amountMinor: number;
  currency: string;
  occurredAt: string;
  note: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateTransactionPayload = {
  financialAccountId: string;
  categoryId: string;
  name: string;
  type: TransactionType;
  amountMinor: number;
  occurredAt: string;
  note?: string | null;
};

export type UpdateTransactionPayload = {
  name?: string;
  amountMinor?: number;
  categoryId?: string;
  financialAccountId?: string;
  type?: TransactionType;
  occurredAt?: string;
  note?: string | null;
};

export function getTransactions(workspaceId: string): Promise<Transaction[]> {
  return apiFetch<Transaction[]>(`/workspaces/${workspaceId}/transactions`);
}

export function createTransaction(
  workspaceId: string,
  payload: CreateTransactionPayload,
): Promise<Transaction> {
  return apiFetch<Transaction>(`/workspaces/${workspaceId}/transactions`, {
    method: "POST",
    body: payload,
  });
}

export function updateTransaction(
  workspaceId: string,
  transactionId: string,
  payload: UpdateTransactionPayload,
): Promise<Transaction> {
  return apiFetch<Transaction>(
    `/workspaces/${workspaceId}/transactions/${transactionId}`,
    { method: "PATCH", body: payload },
  );
}

export type ExportTransactionsFilter = {
  from?: string;
  to?: string;
  accountIds?: string[];
  categoryIds?: string[];
  type?: "Income" | "Expense";
};

export async function exportTransactions(
  workspaceId: string,
  filter: ExportTransactionsFilter = {},
): Promise<void> {
  const params = new URLSearchParams();
  if (filter.from) { params.set("from", filter.from); }
  if (filter.to) { params.set("to", filter.to); }
  if (filter.type) { params.set("type", filter.type); }
  filter.accountIds?.forEach((id) => params.append("accountIds", id));
  filter.categoryIds?.forEach((id) => params.append("categoryIds", id));

  const url = `/api/workspaces/${workspaceId}/transactions/export?${params.toString()}`;
  const token = localStorage.getItem(TOKEN_STORAGE_KEY);

  const response = await fetch(url, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!response.ok) {
    let problem: { title?: string; detail?: string } | undefined;
    try { problem = await response.json() as typeof problem; } catch { problem = undefined; }
    if (response.status === 401) { window.dispatchEvent(new CustomEvent("aureus:unauthorized")); }
    throw new ApiError(response.status, problem);
  }

  const blob = await response.blob();
  const objectUrl = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.style.display = "none";
  anchor.href = objectUrl;
  anchor.download = "transactions.csv";
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  setTimeout(() => URL.revokeObjectURL(objectUrl), 100);
}

export function deleteTransaction(
  workspaceId: string,
  transactionId: string,
): Promise<void> {
  return apiFetch<void>(
    `/workspaces/${workspaceId}/transactions/${transactionId}`,
    { method: "DELETE" },
  );
}

export type ImportRowPreview = {
  rowNumber: number;
  isValid: boolean;
  errorCode: string | null;
  errorSubject: string | null;
  date: string;
  type: string;
  amount: string;
  account: string;
  category: string;
  name: string;
  note: string;
};

export type ImportPreviewResult = {
  rows: ImportRowPreview[];
  validCount: number;
  errorCount: number;
};

async function importFetch<T>(path: string, body: FormData): Promise<T> {
  const token = localStorage.getItem(TOKEN_STORAGE_KEY);
  const response = await fetch(path, {
    method: "POST",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    body,
  });
  if (!response.ok) {
    let problem: { title?: string; detail?: string } | undefined;
    try { problem = await response.json() as typeof problem; } catch { problem = undefined; }
    if (response.status === 401) { window.dispatchEvent(new CustomEvent("aureus:unauthorized")); }
    throw new ApiError(response.status, problem);
  }
  return response.json() as Promise<T>;
}

export function previewImport(workspaceId: string, file: File): Promise<ImportPreviewResult> {
  const form = new FormData();
  form.append("file", file);
  return importFetch<ImportPreviewResult>(`/api/workspaces/${workspaceId}/transactions/import/preview`, form);
}

export function commitImport(workspaceId: string, file: File): Promise<number> {
  const form = new FormData();
  form.append("file", file);
  return importFetch<number>(`/api/workspaces/${workspaceId}/transactions/import/commit`, form);
}
