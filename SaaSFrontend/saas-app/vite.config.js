import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'

// https://vite.dev/config/
export default defineConfig({
  plugins: [svelte()],
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:7177',
        changeOrigin: true,
        secure: false, // For self-signed certs during development
      }
    }
  }
})
