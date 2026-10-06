import { FormEvent, useState } from 'react';
import { api } from '../api';
import type { Product, ProductInput } from '../types';

interface Props {
  product: Product | null; // null = create
  onClose: () => void;
  onSaved: () => void;
}

export default function ProductModal({ product, onClose, onSaved }: Props) {
  const [form, setForm] = useState<ProductInput>({
    name: product?.name ?? '',
    sku: product?.sku ?? '',
    category: product?.category ?? '',
    costPrice: product?.costPrice ?? 0,
    sellPrice: product?.sellPrice ?? 0,
    quantity: product?.quantity ?? 0,
    reorderLevel: product?.reorderLevel ?? 5,
  });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const set = <K extends keyof ProductInput>(key: K, value: ProductInput[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      if (product) await api.updateProduct(product.id, form);
      else await api.createProduct(form);
      onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not save the product.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="overlay" role="dialog" aria-modal="true" aria-label={product ? 'Edit product' : 'Add product'}>
      <form className="modal" onSubmit={submit}>
        <h2>{product ? 'Edit product' : 'Add product'}</h2>

        <div className="grid-2">
          <label className="span-2">
            Name
            <input value={form.name} onChange={(e) => set('name', e.target.value)} required maxLength={120} />
          </label>
          <label>
            SKU / code
            <input value={form.sku} onChange={(e) => set('sku', e.target.value)} required maxLength={40} />
          </label>
          <label>
            Category
            <input value={form.category} onChange={(e) => set('category', e.target.value)} maxLength={60} />
          </label>
          <label>
            Cost price (R)
            <input
              type="number"
              min={0}
              step="0.01"
              value={form.costPrice}
              onChange={(e) => set('costPrice', Number(e.target.value))}
              required
            />
          </label>
          <label>
            Selling price (R)
            <input
              type="number"
              min={0}
              step="0.01"
              value={form.sellPrice}
              onChange={(e) => set('sellPrice', Number(e.target.value))}
              required
            />
          </label>
          <label>
            Quantity in stock
            <input
              type="number"
              min={0}
              step="1"
              value={form.quantity}
              onChange={(e) => set('quantity', Math.trunc(Number(e.target.value)))}
              required
            />
          </label>
          <label>
            Reorder level
            <input
              type="number"
              min={0}
              step="1"
              value={form.reorderLevel}
              onChange={(e) => set('reorderLevel', Math.trunc(Number(e.target.value)))}
              required
            />
            <small className="muted">You are warned when stock reaches this number.</small>
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
            {busy ? 'Saving…' : 'Save'}
          </button>
        </div>
      </form>
    </div>
  );
}
