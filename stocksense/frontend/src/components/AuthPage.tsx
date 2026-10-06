import { FormEvent, useState } from 'react';
import { api } from '../api';
import type { AuthInfo } from '../types';

interface Props {
  onAuth: (info: AuthInfo) => void;
}

export default function AuthPage({ onAuth }: Props) {
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [shopName, setShopName] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      const info =
        mode === 'login'
          ? await api.login(email, password)
          : await api.register(email, password, shopName);
      onAuth(info);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.');
    } finally {
      setBusy(false);
    }
  };

  const useDemo = () => {
    setMode('login');
    setEmail('demo@stocksense.app');
    setPassword('Demo1234!');
  };

  return (
    <main className="auth-wrap">
      <section className="auth-card">
        <div className="brand">
          <span className="brand-mark" aria-hidden="true">
            S
          </span>
          <h1>StockSense</h1>
        </div>
        <p className="muted">Know what is on your shelves, and what to reorder, before you run out.</p>

        <div className="tabs" role="tablist">
          <button
            role="tab"
            aria-selected={mode === 'login'}
            className={mode === 'login' ? 'tab active' : 'tab'}
            onClick={() => setMode('login')}
            type="button"
          >
            Sign in
          </button>
          <button
            role="tab"
            aria-selected={mode === 'register'}
            className={mode === 'register' ? 'tab active' : 'tab'}
            onClick={() => setMode('register')}
            type="button"
          >
            Create account
          </button>
        </div>

        <form onSubmit={submit} className="form">
          {mode === 'register' && (
            <label>
              Shop name
              <input
                value={shopName}
                onChange={(e) => setShopName(e.target.value)}
                required
                minLength={2}
                maxLength={120}
                placeholder="Thandi's Corner Shop"
              />
            </label>
          )}
          <label>
            Email
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
            />
          </label>
          <label>
            Password
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={mode === 'register' ? 8 : undefined}
              autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
            />
            {mode === 'register' && <small className="muted">At least 8 characters.</small>}
          </label>

          {error && (
            <p className="error" role="alert">
              {error}
            </p>
          )}

          <button className="btn primary" disabled={busy} type="submit">
            {busy ? 'Please wait…' : mode === 'login' ? 'Sign in' : 'Create account'}
          </button>
        </form>

        <button className="link" type="button" onClick={useDemo}>
          Fill in the demo account
        </button>
      </section>
    </main>
  );
}
