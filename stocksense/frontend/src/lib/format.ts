export type StockStatus = 'out' | 'low' | 'ok';

const rand = new Intl.NumberFormat('en-ZA', {
  style: 'currency',
  currency: 'ZAR',
  minimumFractionDigits: 2,
});

export function formatCurrency(value: number): string {
  return rand.format(value);
}

/** Out of stock beats low stock; low stock means at or below the reorder level. */
export function stockStatus(quantity: number, reorderLevel: number): StockStatus {
  if (quantity <= 0) return 'out';
  if (quantity <= reorderLevel) return 'low';
  return 'ok';
}

/** Gross margin as a percentage of the selling price. Returns 0 when there is no selling price. */
export function marginPercent(costPrice: number, sellPrice: number): number {
  if (sellPrice <= 0) return 0;
  return Math.round(((sellPrice - costPrice) / sellPrice) * 100);
}

export function formatDateTime(iso: string): string {
  // The API returns UTC without a "Z" suffix; treat it as UTC.
  const value = /[zZ]|[+-]\d\d:\d\d$/.test(iso) ? iso : `${iso}Z`;
  return new Date(value).toLocaleString('en-ZA', {
    dateStyle: 'medium',
    timeStyle: 'short',
  });
}
