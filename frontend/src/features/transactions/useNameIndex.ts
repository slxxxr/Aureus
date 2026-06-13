import { useCallback, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { getTransactions, type Transaction, type TransactionType } from "./transactionsApi";

export type NameEntry = {
  name: string;
  categoryId: string;
  type: TransactionType;
  accountId: string;
  amountMinor: number;
  currency: string;
  count: number;
};

function normalize(s: string): string {
  return s.trim().toLowerCase().replace(/\s+/g, " ");
}

export function useNameIndex(workspaceId: string) {
  const { data: transactions = [] } = useQuery<Transaction[]>({
    queryKey: ["transactions", workspaceId],
    queryFn: () => getTransactions(workspaceId),
    staleTime: 30_000,
  });

  // transactions arrive OccurredAt DESC — first match per key = most recent
  const index = useMemo(() => {
    const map = new Map<string, NameEntry>();
    for (const tx of transactions) {
      const key = normalize(tx.name);
      if (!map.has(key)) {
        map.set(key, {
          name: tx.name,
          categoryId: tx.categoryId,
          type: tx.type,
          accountId: tx.financialAccountId,
          amountMinor: tx.amountMinor,
          currency: tx.currency,
          count: 1,
        });
      } else {
        map.get(key)!.count++;
      }
    }
    return map;
  }, [transactions]);

  const search = useCallback(
    (query: string): NameEntry[] => {
      if (!query) return [];
      const q = normalize(query);
      const prefix: NameEntry[] = [];
      const substring: NameEntry[] = [];
      for (const [key, entry] of index) {
        if (key.startsWith(q)) prefix.push(entry);
        else if (key.includes(q)) substring.push(entry);
      }
      prefix.sort((a, b) => b.count - a.count);
      substring.sort((a, b) => b.count - a.count);
      return [...prefix, ...substring].slice(0, 5);
    },
    [index],
  );

  return { search };
}
