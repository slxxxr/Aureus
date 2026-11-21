import {useQuery} from "@tanstack/react-query";
import {useTranslation} from "react-i18next";
import {getFinancialAccountsSummary} from "@/features/financial-accounts/financialAccountsApi";
import {formatDate} from "@/lib/formatDate";
import {formatMoney} from "@/lib/formatMoney";

export function AccountBalanceSummary({workspaceId}: { workspaceId: string }) {
    const {t, i18n} = useTranslation();
    const {data, isPending, isError} = useQuery({
        queryKey: ["financial-accounts", workspaceId, "summary"],
        queryFn: () => getFinancialAccountsSummary(workspaceId),
        gcTime: 0,
        retry: false,
    });

    return (
        <section
            className="flex min-w-0 flex-wrap items-baseline gap-x-3 gap-y-1"
            aria-labelledby="account-balance-summary"
        >
            <h2 id="account-balance-summary" className="text-sm font-medium text-foreground">
                {t("financialAccounts.summary.title")}
            </h2>

            <div className="flex min-w-0 flex-wrap items-baseline gap-x-3 gap-y-1" aria-live="polite">
                {isPending && (
                    <p className="text-sm text-muted-foreground">{t("financialAccounts.summary.loading")}</p>
                )}

                {isError && (
                    <p className="text-sm text-muted-foreground">{t("financialAccounts.summary.unavailable")}</p>
                )}

                {data && !isError && (
                    <>
                        {data.totals.map(({currency, amountMinor}) => (
                            <p key={currency} className="whitespace-nowrap text-sm font-semibold tabular-nums">
                                {formatMoney(amountMinor, currency)}
                            </p>
                        ))}
                        <p className="text-sm text-muted-foreground">
                            {t("financialAccounts.summary.rateDate", {
                                date: formatDate(`${data.rateDate}T00:00:00`, i18n.language),
                            })}
                        </p>
                    </>
                )}
            </div>
        </section>
    );
}
