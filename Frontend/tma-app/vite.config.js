// ARCH SYSTEM Telegram Mini App Build Configuration
// Trigger CI/CD pipeline
import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'
export default defineConfig({
  plugins: [svelte()],
  server: {
    allowedHosts: ['goldmine-unloved-capsule.ngrok-free.dev'],
    proxy: {
      '/api': {
        target: 'https://backendbarbershopdomen.online',
        changeOrigin: true,
        secure: false,
      },
      '/results': {
        target: 'https://backendbarbershopdomen.online',
        changeOrigin: true,
        secure: false,
      },
      '/barbers_photo': {
        target: 'https://backendbarbershopdomen.online',
        changeOrigin: true,
        secure: false,
      }
    }
  }
})
