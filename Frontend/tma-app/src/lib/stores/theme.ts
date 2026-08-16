import { writable } from 'svelte/store';

const storedTheme = typeof window !== 'undefined' ? localStorage.getItem('tma-theme') : null;
// If Telegram theme is available, or prefers dark, default to dark
const initialTheme = storedTheme || 'dark';

export const theme = writable<string>(initialTheme);

export function initTmaTheme() {
  if (typeof window === 'undefined') return;
  const current = localStorage.getItem('tma-theme') || initialTheme;
  applyTheme(current);
}

export function toggleTmaTheme() {
  theme.update(current => {
    const next = current === 'dark' ? 'light' : 'dark';
    if (typeof window !== 'undefined') {
      localStorage.setItem('tma-theme', next);
      applyTheme(next);
    }
    return next;
  });
}

export function setTmaTheme(newTheme: string) {
  theme.set(newTheme);
  if (typeof window !== 'undefined') {
    localStorage.setItem('tma-theme', newTheme);
    applyTheme(newTheme);
  }
}

function applyTheme(themeValue: string) {
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
