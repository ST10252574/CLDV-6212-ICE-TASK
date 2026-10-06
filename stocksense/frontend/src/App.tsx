import { useState } from 'react';
import { loadAuth, saveAuth } from './api';
import type { AuthInfo } from './types';
import AuthPage from './components/AuthPage';
import Dashboard from './components/Dashboard';

export default function App() {
  const [auth, setAuth] = useState<AuthInfo | null>(() => loadAuth());

  const handleAuth = (info: AuthInfo) => {
    saveAuth(info);
    setAuth(info);
  };

  const handleLogout = () => {
    saveAuth(null);
    setAuth(null);
  };

  return auth ? (
    <Dashboard shopName={auth.shopName} onLogout={handleLogout} />
  ) : (
    <AuthPage onAuth={handleAuth} />
  );
}
