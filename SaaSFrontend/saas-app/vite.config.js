import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'
export default defineConfig({
  plugins: [svelte()],
  server: {
    allowedHosts: ['goldmine-unloved-capsule.ngrok-free.dev'],
    proxy: {
      '/api': {
        target: 'https://barbershop-backend-production-f891.up.railway.app',
        changeOrigin: true,
        secure: false,
      }
    }
  }
})
