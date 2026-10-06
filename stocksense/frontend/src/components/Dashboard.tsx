import { useCallback, useEffect, useState } from 'react';
import { api } from '../api';
import { formatCurrency, marginPercent, stockStatus } from '../lib/format';
import type { DashboardSummary, LowStockItem, Product } from '../types';
import AdjustModal from './AdjustModal';
import ProductModal from './ProductModal';

interface Props {
  shopName: string;
  onLogout: () => void;
}

export default function Dashboard({ shopName, onLogout }: Props) {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [lowStock, setLowStock] = useState<LowStockItem[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [editing, setEditing] = useState<Product | 'new' | null>(null);
  const [adjusting, setAdjusting] = useState<Product | null>(null);

  const load = useCallback(async () => {
    try {
      const [s, l, p] = await Promise.all([api.summary(), api.lowStock(), api.products(search)]);
      setSummary(s);
      setLowStock(l);
      setProducts(p);
      setError('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load your stock.');
    } finally {
      setLoading(false);
    }
  }, [search]);

  // Debounce searching so we do not hit the API on every keystroke.
  useEffect(() => {
    const timer = setTimeout(load, 250);
    return () => clearTimeout(timer);
  }, [load]);

  const afterSave = () => {
    setEditing(null);
    setAdjusting(null);
    load();
  };

  const remove = async (p: Product) => {
    if (!window.confirm(`Delete "${p.name}"? Its stock history will be deleted too.`)) return;
    try {
      await api.deleteProduct(p.id);
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not delete the product.');
    }
  };

  return (
    <div className="page">
      <header className="topbar">
        <div className="brand">
          <span className="brand-mark" aria-hidden="true">
            S
          </span>
          <div>
            <h1>StockSense</h1>
            <span className="muted">{shopName}</span>
          </div>
        </div>
        <button className="btn" onClick={onLogout}>
          Sign out
        </button>
      </header>

      {error && (
        <p className="error banner" role="alert">
          {error}
        </p>
      )}

      <section className="stats" aria-label="Summary">
        <Stat label="Products" value={summary ? String(summary.totalProducts) : '–'} />
        <Stat label="Units in stock" value={summary ? String(summary.totalUnits) : '–'} />
        <Stat label="Stock value (cost)" value={summary ? formatCurrency(summary.inventoryCostValue) : '–'} />
        <Stat
          label="Needs reordering"
          value={summary ? String(summary.lowStockCount) : '–'}
          tone={summary && summary.lowStockCount > 0 ? 'warn' : undefined}
          hint={summary && summary.outOfStockCount > 0 ? `${summary.outOfStockCount} sold out` : undefined}
        />
      </section>

      {lowStock.length > 0 && (
        <section className="panel alert" aria-label="Reorder list">
          <h2>Reorder list</h2>
          <p className="muted">These items are at or below their reorder level.</p>
          <table>
            <thead>
              <tr>
                <th>Product</th>
                <th className="num">In stock</th>
                <th className="num">Reorder level</th>
                <th className="num">Suggested order</th>
              </tr>
            </thead>
            <tbody>
              {lowStock.map((item) => (
                <tr key={item.id}>
                  <td>{item.name}</td>
                  <td className="num">{item.quantity}</td>
                  <td className="num">{item.reorderLevel}</td>
                  <td className="num">
                    <strong>{item.suggestedReorderQuantity}</strong>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}

      <section className="panel" aria-label="Products">
        <div className="panel-head">
          <h2>Products</h2>
          <div className="toolbar">
            <input
              type="search"
              placeholder="Search name or SKU"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              aria-label="Search products"
            />
            <button className="btn primary" onClick={() => setEditing('new')}>
              + Add product
            </button>
          </div>
        </div>

        {loading ? (
          <p className="muted">Loading…</p>
        ) : products.length === 0 ? (
          <p className="muted empty">
            {search ? 'No products match your search.' : 'No products yet. Add your first one to get started.'}
          </p>
        ) : (
          <div className="table-scroll">
            <table>
              <thead>
                <tr>
                  <th>Product</th>
                  <th>Category</th>
                  <th className="num">Cost</th>
                  <th className="num">Price</th>
                  <th className="num">Margin</th>
                  <th className="num">Stock</th>
                  <th>Status</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {products.map((p) => {
                  const status = stockStatus(p.quantity, p.reorderLevel);
                  return (
                    <tr key={p.id}>
                      <td>
                        <strong>{p.name}</strong>
                        <div className="muted small">{p.sku}</div>
                      </td>
                      <td>{p.category ?? '–'}</td>
                      <td className="num">{formatCurrency(p.costPrice)}</td>
                      <td className="num">{formatCurrency(p.sellPrice)}</td>
                      <td className="num">{marginPercent(p.costPrice, p.sellPrice)}%</td>
                      <td className="num">{p.quantity}</td>
                      <td>
                        <span className={`badge ${status}`}>
                          {status === 'out' ? 'Sold out' : status === 'low' ? 'Low' : 'OK'}
                        </span>
                      </td>
                      <td className="actions">
                        <button className="btn small" onClick={() => setAdjusting(p)}>
                          Adjust
                        </button>
                        <button className="btn small" onClick={() => setEditing(p)}>
                          Edit
                        </button>
                        <button className="btn small danger" onClick={() => remove(p)}>
                          Delete
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {editing && (
        <ProductModal
          product={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={afterSave}
        />
      )}
      {adjusting && <AdjustModal product={adjusting} onClose={() => setAdjusting(null)} onSaved={afterSave} />}
    </div>
  );
}

function Stat({ label, value, tone, hint }: { label: string; value: string; tone?: 'warn'; hint?: string }) {
  return (
    <div className={tone === 'warn' ? 'stat warn' : 'stat'}>
      <span className="stat-label">{label}</span>
      <span className="stat-value">{value}</span>
      {hint && <span className="stat-hint">{hint}</span>}
    </div>
  );
}
