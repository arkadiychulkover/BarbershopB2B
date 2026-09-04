import { writable } from 'svelte/store';

/**
 * Normalizes a path, ensuring leading slash and removing trailing slashes (except for root '/')
 * Also strips legacy hash prefix if present.
 */
export function normalizePath(path) {
  if (!path) return '/';
  let clean = path;
  if (clean.startsWith('#/')) {
    clean = clean.slice(1);
  } else if (clean.startsWith('#')) {
    clean = clean.slice(1);
  }
  // Strip query and hash
  const queryIndex = clean.indexOf('?');
  if (queryIndex !== -1) {
    clean = clean.slice(0, queryIndex);
  }
  if (!clean.startsWith('/')) {
    clean = '/' + clean;
  }
  if (clean.length > 1 && clean.endsWith('/')) {
    clean = clean.slice(0, -1);
  }
  return clean;
}

/**
 * Returns current pathname, automatically converting legacy hash URLs to clean URLs.
 */
function getInitialPath() {
  if (typeof window === 'undefined') return '/';
  
  // Backward compatibility: If opened with /#/login or #/register, migrate to clean path
  const hash = window.location.hash;
  if (hash.startsWith('#/')) {
    const migratedPath = hash.slice(1);
    window.history.replaceState(null, '', migratedPath);
    return normalizePath(migratedPath);
  }

  return normalizePath(window.location.pathname);
}

export const currentPath = writable(getInitialPath());

// Synchronize store on browser popstate (back/forward navigation) or hashchange
if (typeof window !== 'undefined') {
  const syncLocation = () => {
    const hash = window.location.hash;
    if (hash.startsWith('#/')) {
      const migratedPath = hash.slice(1);
      window.history.replaceState(null, '', migratedPath);
      currentPath.set(normalizePath(migratedPath));
      return;
    }
    currentPath.set(normalizePath(window.location.pathname));
  };

  window.addEventListener('popstate', syncLocation);
  window.addEventListener('hashchange', syncLocation);
}

/**
 * Programmatic navigation using HTML5 History API.
 * Accepts '/path' or legacy '#/path'.
 */
export function push(url) {
  if (typeof window === 'undefined') return;
  const cleanUrl = url.startsWith('#/') ? url.slice(1) : url;
  window.history.pushState(null, '', cleanUrl);
  currentPath.set(normalizePath(cleanUrl));
  window.scrollTo({ top: 0, behavior: 'instant' });
}

/**
 * Replaces the current URL without creating a new history entry.
 */
export function replace(url) {
  if (typeof window === 'undefined') return;
  const cleanUrl = url.startsWith('#/') ? url.slice(1) : url;
  window.history.replaceState(null, '', cleanUrl);
  currentPath.set(normalizePath(cleanUrl));
}

/**
 * Navigates back in browser history.
 */
export function pop() {
  if (typeof window !== 'undefined') {
    window.history.back();
  }
}
