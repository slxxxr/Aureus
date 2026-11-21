import {apiFetch} from "@/lib/apiClient";

export type FinancialAccount = {
    id: string;
    name: string;
    currency: string;
    initialBalanceMinor: number;
    currentBalanceMinor: number;
    createdAt: string;
    updatedAt: string | null;
};

export type CreateFinancialAccountPayload = {
    name: string;
    currency: string;
    initialBalanceMinor: number;
};

export type UpdateFinancialAccountPayload = {
    name?: string;
    initialBalanceMinor?: number;
};

export type ReorderFinancialAccountsPayload = {
    accountIds: string[];
    expectedAccountIds: string[];
};

export function getFinancialAccounts(workspaceId: string): Promise<FinancialAccount[]> {
    return apiFetch<FinancialAccount[]>(`/workspaces/${workspaceId}/financial-accounts`);
}

export function reorderFinancialAccounts(
    workspaceId: string,
    payload: ReorderFinancialAccountsPayload,
): Promise<void> {
    return apiFetch<void>(`/workspaces/${workspaceId}/financial-accounts/order`, {
        method: "PUT",
        body: payload,
    });
}

export function createFinancialAccount(
    workspaceId: string,
    payload: CreateFinancialAccountPayload,
): Promise<FinancialAccount> {
    return apiFetch<FinancialAccount>(`/workspaces/${workspaceId}/financial-accounts`, {
        method: "POST",
        body: payload,
    });
}

export function updateFinancialAccount(
    workspaceId: string,
    accountId: string,
    payload: UpdateFinancialAccountPayload,
): Promise<FinancialAccount> {
    return apiFetch<FinancialAccount>(`/workspaces/${workspaceId}/financial-accounts/${accountId}`, {
        method: "PATCH",
        body: payload,
    });
}

export function deleteFinancialAccount(workspaceId: string, accountId: string): Promise<void> {
    return apiFetch<void>(`/workspaces/${workspaceId}/financial-accounts/${accountId}`, {
        method: "DELETE",
    });
}
