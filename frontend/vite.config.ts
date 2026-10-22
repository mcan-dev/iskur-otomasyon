import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Geliştirmede /api istekleri arka yüze yönlendirilir, böylece CORS ayarı gerekmez.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5231', changeOrigin: true },
    },
  },
});
