import { mount } from 'svelte'
import './app.css'
import App from './App.svelte'
import { initTheme } from './lib/theme.js'

// ARCH SYSTEM SaaS Web Panel Entrypoint
// CI/CD trigger update
initTheme();

const appElement = document.getElementById('app');
if (appElement) {
  appElement.innerHTML = '';
}

const app = mount(App, {
  target: appElement,
})

export default app
