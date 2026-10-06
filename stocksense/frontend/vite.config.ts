import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

// During `npm run dev` the API runs on :8080 (docker compose up api db, or dotnet run).
// In Docker/production, nginx does this same proxying (see nginx.conf.template).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:8080',
    },
  },
  test: {
    environment: 'node',
  },
});
