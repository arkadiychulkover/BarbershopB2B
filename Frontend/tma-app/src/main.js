import { mount } from 'svelte'
import './app.css'
import App from './App.svelte'
import { initTmaTheme } from './lib/stores/theme'

// ARCH SYSTEM Telegram Mini App Entrypoint
// CI/CD trigger update
initTmaTheme();

const app = mount(App, {
  target: document.getElementById('app'),
})

export default app
