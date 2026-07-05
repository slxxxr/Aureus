import { apiFetch } from "@/lib/apiClient";

export type Transfer = {
  id: string;
  fromAccountId: string;
  toAccountId: string;
  createdByUserId: string;
  amountMinor: number;
  currency: string;
  occurredAt: string;
  note: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateTransferPayload = {
  fromAccountId: string;
  toAccountId: string;
  amountMinor: number;
  occurredAt: string;
  note?: string | null;
};

export type UpdateTransferPayload = {
  fromAccountId?: string;
  toAccountId?: string;
  amountMinor?: number;
  occurredAt?: string;
  note?: string | null;
};

export function getTransfers(workspaceId: string): Promise<Transfer[]> {
  return apiFetch<Transfer[]>(`/workspaces/${workspaceId}/transfers`);
}

export function createTransfer(
  workspaceId: string,
  payload: CreateTransferPayload,
): Promise<Transfer> {
  return apiFetch<Transfer>(`/workspaces/${workspaceId}/transfers`, {
    method: "POST",
    body: payload,
  });
}

export function updateTransfer(
  workspaceId: string,
  transferId: string,
  payload: UpdateTransferPayload,
): Promise<Transfer> {
  return apiFetch<Transfer>(
    `/workspaces/${workspaceId}/transfers/${transferId}`,
    { method: "PATCH", body: payload },
  );
}

export function deleteTransfer(
  workspaceId: string,
  transferId: string,
): Promise<void> {
  return apiFetch<void>(
    `/workspaces/${workspaceId}/transfers/${transferId}`,
    { method: "DELETE" },
  );
}
