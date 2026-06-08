const CURRENCY_SYMBOLS: Record<string, string> = {
  RUB: "₽",
  USD: "$",
  EUR: "€",
};

// SI notation: decimal dot, non-breaking space as thousands separator, symbol after amount.
export function formatMoney(amountMinor: number, currency: string): string {
  const amount = amountMinor / 100;
  const symbol = CURRENCY_SYMBOLS[currency] ?? currency;

  const digits = new Intl.NumberFormat("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })
    .format(Math.abs(amount))
    .replace(/,/g, " "); // thousands comma → non-breaking space

  return `${amount < 0 ? "−" : ""}${digits} ${symbol}`;
}
