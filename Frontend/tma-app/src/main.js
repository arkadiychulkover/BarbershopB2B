import { mount } from 'svelte'
import './app.css'
import App from './App.svelte'
import { initTmaTheme } from './lib/stores/theme'
import { reportErrorToTelegram } from './lib/api'

// ARCH SYSTEM Telegram Mini App Entrypoint
initTmaTheme();

if (typeof window !== 'undefined') {
  window.addEventListener('unhandledrejection', (event) => {
    reportErrorToTelegram({
      endpoint: 'window.unhandledrejection',
      error: event.reason?.message || String(event.reason)
    });
  });

  window.addEventListener('error', (event) => {
    reportErrorToTelegram({
      endpoint: 'window.onerror',
      error: `${event.message} (${event.filename}:${event.lineno}:${event.colno})`
    });
  });
}

const app = mount(App, {
  target: document.getElementById('app'),
})

export default app
