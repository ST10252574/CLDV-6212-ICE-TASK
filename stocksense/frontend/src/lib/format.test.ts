import { describe, expect, it } from 'vitest';
import { formatDateTime, marginPercent, stockStatus } from './format';

describe('stockStatus', () => {
  it('flags zero stock as out', () => {
    expect(stockStatus(0, 5)).toBe('out');
  });

  it('flags stock at the reorder level as low', () => {
    expect(stockStatus(5, 5)).toBe('low');
    expect(stockStatus(3, 5)).toBe('low');
  });

  it('treats stock above the reorder level as ok', () => {
    expect(stockStatus(6, 5)).toBe('ok');
  });

  it('still reports out when the reorder level is zero', () => {
    expect(stockStatus(0, 0)).toBe('out');
  });
});

describe('marginPercent', () => {
  it('calculates margin on the selling price', () => {
    expect(marginPercent(50, 100)).toBe(50);
    expect(marginPercent(48, 62)).toBe(23);
  });

  it('returns 0 when there is no selling price', () => {
    expect(marginPercent(10, 0)).toBe(0);
  });
});

describe('formatDateTime', () => {
  it('treats timestamps without a zone as UTC', () => {
    const a = formatDateTime('2026-10-05T10:00:00');
    const b = formatDateTime('2026-10-05T10:00:00Z');
    expect(a).toBe(b);
  });
});
