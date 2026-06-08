import { apiFetch } from "@/lib/apiClient";

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

export function deleteTransaction(
  workspaceId: string,
  transactionId: string,
): Promise<void> {
  return apiFetch<void>(
    `/workspaces/${workspaceId}/transactions/${transactionId}`,
    { method: "DELETE" },
  );
}
