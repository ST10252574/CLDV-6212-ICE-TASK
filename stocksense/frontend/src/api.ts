import type {
  AuthInfo,
  DashboardSummary,
  LowStockItem,
  Product,
  ProductInput,
  StockMovement,
} from './types';

// All calls go to the same origin under /api. nginx (Docker) or the Vite dev server proxies them
// to the .NET backend, so there is no CORS configuration for the browser to worry about.
const BASE = '/api';
const STORAGE_KEY = 'stocksense.auth';

export class ApiError extends Error {
  status: number;
  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

export function loadAuth(): AuthInfo | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as AuthInfo) : null;
  } catch {
    return null;
  }
}

export function saveAuth(auth: AuthInfo | null): void {
  try {
    if (auth) sessionStorage.setItem(STORAGE_KEY, JSON.stringify(auth));
    else sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    /* storage unavailable: the session simply will not survive a refresh */
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const auth = loadAuth();
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  if (auth) headers.Authorization = `Bearer ${auth.token}`;

  let response: Response;
  try {
    response = await fetch(`${BASE}${path}`, { ...options, headers });
  } catch {
    throw new ApiError(
      'Cannot reach the server. If it was idle it may be waking up, so try again in a minute.',
      0,
    );
  }

  if (response.status === 401 && auth) {
    saveAuth(null);
    window.location.reload();
  }

  if (!response.ok) {
    let message = `Request failed (${response.status})`;
    try {
      const body = await response.json();
      if (body?.error) message = body.error;
      else if (body?.errors) message = Object.values(body.errors).flat().join(' ');
      else if (body?.title) message = body.title;
    } catch {
      /* body was not JSON */
    }
    throw new ApiError(message, response.status);
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

const json = (body: unknown) => JSON.stringify(body);

export const api = {
  login: (email: string, password: string) =>
    request<AuthInfo>('/auth/login', { method: 'POST', body: json({ email, password }) }),

  register: (email: string, password: string, shopName: string) =>
    request<AuthInfo>('/auth/register', {
      method: 'POST',
      body: json({ email, password, shopName }),
    }),

  summary: () => request<DashboardSummary>('/dashboard/summary'),
  lowStock: () => request<LowStockItem[]>('/dashboard/low-stock'),

  products: (search: string) =>
    request<Product[]>(`/products${search ? `?search=${encodeURIComponent(search)}` : ''}`),
  createProduct: (input: ProductInput) =>
    request<Product>('/products', { method: 'POST', body: json(input) }),
  updateProduct: (id: number, input: ProductInput) =>
    request<Product>(`/products/${id}`, { method: 'PUT', body: json(input) }),
  deleteProduct: (id: number) => request<void>(`/products/${id}`, { method: 'DELETE' }),

  adjustStock: (id: number, change: number, reason: string) =>
    request<Product>(`/products/${id}/adjust`, { method: 'POST', body: json({ change, reason }) }),
  movements: (id: number) => request<StockMovement[]>(`/products/${id}/movements`),
};
