const CURRENCY_LOCALES: Record<string, string> = {
  RUB: "ru-RU",
  USD: "en-US",
  EUR: "de-DE",
};

export function formatMoney(amountMinor: number, currency: string): string {
  const locale = CURRENCY_LOCALES[currency] ?? "en-US";
  const amount = amountMinor / 100;

  return new Intl.NumberFormat(locale, {
    style: "currency",
    currency,
    minimumFractionDigits: 2,
  }).format(amount);
}
