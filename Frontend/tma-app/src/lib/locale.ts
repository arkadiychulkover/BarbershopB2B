import { writable } from 'svelte/store';
import { getLocale, setLocale, isLocale, locales } from './paraglide/runtime.js';

const STORAGE_KEY = 'PARAGLIDE_LOCALE';

function getInitialLocale(): string {
  try {
    const saved = typeof window !== 'undefined' ? localStorage.getItem(STORAGE_KEY) : null;
    if (saved && isLocale(saved)) {
      return saved;
    }
    return 'en';
  } catch (e) {
    return 'en';
  }
}

const initial = getInitialLocale();
try {
  setLocale(initial as any);
  if (typeof document !== 'undefined') {
    document.documentElement.lang = initial;
  }
} catch (e) {}

export const currentLocale = writable<string>(initial);

export function switchLocale(newLocale: string) {
  if (!isLocale(newLocale)) return;
  setLocale(newLocale as any);
  try {
    if (typeof window !== 'undefined') {
      localStorage.setItem(STORAGE_KEY, newLocale);
      document.documentElement.lang = newLocale;
    }
  } catch (e) {}
  currentLocale.set(newLocale);
}

export { getLocale, locales };
