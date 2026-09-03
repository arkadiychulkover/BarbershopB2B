import { writable } from 'svelte/store';
import { getLocale, setLocale, isLocale, locales } from './paraglide/runtime.js';

const STORAGE_KEY = 'PARAGLIDE_LOCALE';

function getInitialLocale() {
  try {
    const saved = typeof window !== 'undefined' ? localStorage.getItem(STORAGE_KEY) : null;
    if (saved && isLocale(saved)) {
      return saved;
    }
  } catch (e) {}
  // Default is 'en' as requested
  return 'en';
}

const initial = getInitialLocale();
try {
  setLocale(initial);
  if (typeof document !== 'undefined') {
    document.documentElement.lang = initial;
  }
} catch (e) {}

export const currentLocale = writable(initial);

export function switchLocale(newLocale) {
  if (!isLocale(newLocale)) return;
  setLocale(newLocale);
  try {
    if (typeof window !== 'undefined') {
      localStorage.setItem(STORAGE_KEY, newLocale);
      document.documentElement.lang = newLocale;
    }
  } catch (e) {}
  currentLocale.set(newLocale);
}

export { getLocale, locales };
