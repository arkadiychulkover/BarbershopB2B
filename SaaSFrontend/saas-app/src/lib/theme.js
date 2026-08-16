import { writable } from 'svelte/store';

// Check saved theme or system preference
const storedTheme = typeof window !== 'undefined' ? localStorage.getItem('app-theme') : 'dark';
const initialTheme = storedTheme || (typeof window !== 'undefined' && window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark');

export const theme = writable(initialTheme);

export function initTheme() {
  if (typeof window === 'undefined') return;
  const current = localStorage.getItem('app-theme') || initialTheme;
  applyTheme(current);
}

export function toggleTheme() {
  theme.update(current => {
    const next = current === 'dark' ? 'light' : 'dark';
    if (typeof window !== 'undefined') {
      localStorage.setItem('app-theme', next);
      applyTheme(next);
    }
    return next;
  });
}

export function setTheme(newTheme) {
  theme.set(newTheme);
  if (typeof window !== 'undefined') {
    localStorage.setItem('app-theme', newTheme);
    applyTheme(newTheme);
  }
}

function applyTheme(themeValue) {
  if (typeof document !== 'undefined') {
    document.documentElement.setAttribute('data-theme', themeValue);
    if (themeValue === 'light') {
      document.documentElement.classList.add('light-theme');
      document.documentElement.classList.remove('dark-theme');
    } else {
      document.documentElement.classList.add('dark-theme');
      document.documentElement.classList.remove('light-theme');
    }
  }
}
