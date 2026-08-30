import { mount } from 'svelte'
import './app.css'
import App from './App.svelte'
import { initTheme } from './lib/theme.js'

// ARCH SYSTEM SaaS Web Panel Entrypoint
// CI/CD trigger update
initTheme();

const app = mount(App, {
  target: document.getElementById('app'),
})

export default app
