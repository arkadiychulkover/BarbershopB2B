import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'
export default defineConfig({
  plugins: [svelte()],
  server: {
    allowedHosts: ['goldmine-unloved-capsule.ngrok-free.dev'],
    proxy: {
      '/api': {
        target: 'http://localhost:5111',
        changeOrigin: true,
        secure: false,
      }
    }
  }
})
