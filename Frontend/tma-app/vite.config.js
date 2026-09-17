// ARCH SYSTEM Telegram Mini App Build Configuration
// Trigger CI/CD pipeline {21:36 17/09/26}
import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'
import { paraglideVitePlugin } from '@inlang/paraglide-js'

export default defineConfig({
  plugins: [
    svelte(),
    paraglideVitePlugin({
      project: './project.inlang',
      outdir: './src/lib/paraglide'
    })
  ],
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
