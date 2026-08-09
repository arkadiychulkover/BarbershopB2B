import { writable, readable } from 'svelte/store';
const storedToken = localStorage.getItem('jwt_token');

export const authStore = writable({
  token: storedToken || null,
  isAuthenticated: !!storedToken
});

export const profileStore = writable({
  ownerId: null,
  status: null,
  email: null
});
export function setAuthToken(token) {
  if (token) {
    localStorage.setItem('jwt_token', token);
    authStore.set({ token, isAuthenticated: true });
  } else {
    localStorage.removeItem('jwt_token');
    authStore.set({ token: null, isAuthenticated: false });
    profileStore.set({ ownerId: null, status: null, email: null });
  }
}

export const currentLocation = readable(
  window.location.hash.replace(/^#/, '') || '/',
  (set) => {
    const update = () => set(window.location.hash.replace(/^#/, '') || '/');
    window.addEventListener('hashchange', update);
    return () => window.removeEventListener('hashchange', update);
  }
);
