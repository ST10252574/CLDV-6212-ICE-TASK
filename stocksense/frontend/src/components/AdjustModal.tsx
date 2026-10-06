import { FormEvent, useEffect, useState } from 'react';
import { api } from '../api';
import { formatDateTime } from '../lib/format';
import type { Product, StockMovement } from '../types';

interface Props {
  product: Product;
  onClose: () => void;
  onSaved: () => void;
}

export default function AdjustModal({ product, onClose, onSaved }: Props) {
  const [direction, setDirection] = useState<'in' | 'out'>('in');
  const [amount, setAmount] = useState(1);
  const [reason, setReason] = useState('Delivery received');
  const [history, setHistory] = useState<StockMovement[]>([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    api.movements(product.id).then(setHistory).catch(() => setHistory([]));
  }, [product.id]);

  const chooseDirection = (d: 'in' | 'out') => {
    setDirection(d);
    setReason(d === 'in' ? 'Delivery received' : 'Sold');
  };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      await api.adjustStock(product.id, direction === 'in' ? amount : -amount, reason);
      onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not update stock.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="overlay" role="dialog" aria-modal="true" aria-label="Adjust stock">
      <form className="modal" onSubmit={submit}>
        <h2>Adjust stock</h2>
        <p className="muted">
          {product.name} · currently <strong>{product.quantity}</strong> in stock
        </p>

        <div className="tabs">
          <button type="button" className={direction === 'in' ? 'tab active' : 'tab'} onClick={() => chooseDirection('in')}>
            Stock in
          </button>
          <button type="button" className={direction === 'out' ? 'tab active' : 'tab'} onClick={() => chooseDirection('out')}>
            Stock out
          </button>
        </div>

        <div className="grid-2">
          <label>
            Units
            <input
              type="number"
              min={1}
              step={1}
              value={amount}
              onChange={(e) => setAmount(Math.max(1, Math.trunc(Number(e.target.value))))}
              required
            />
          </label>
          <label>
            Reason
            <input value={reason} onChange={(e) => setReason(e.target.value)} required maxLength={200} />
          </label>
        </div>

        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}

        <div className="modal-actions">
          <button type="button" className="btn" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" className="btn primary" disabled={busy}>
            {busy ? 'Saving…' : 'Record'}
          </button>
        </div>

        {history.length > 0 && (
          <>
            <h3>Recent movements</h3>
            <ul className="history">
              {history.slice(0, 8).map((m) => (
                <li key={m.id}>
                  <span className={m.change > 0 ? 'pos' : 'neg'}>
                    {m.change > 0 ? '+' : ''}
                    {m.change}
                  </span>
                  <span>{m.reason}</span>
                  <span className="muted">{formatDateTime(m.createdAt)}</span>
                </li>
              ))}
            </ul>
          </>
        )}
      </form>
    </div>
  );
}
