import { writable, readable } from 'svelte/store';
const storedToken = localStorage.getItem('jwt_token');

function getRoleFromToken(token) {
  if (!token) return null;
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const payload = JSON.parse(atob(base64));
    const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
    return payload[roleClaim] || payload['role'] || payload['Role'] || null;
  } catch (e) {
    return null;
  }
}

export const authStore = writable({
  token: storedToken || null,
  isAuthenticated: !!storedToken,
  role: getRoleFromToken(storedToken)
});

export const profileStore = writable({
  ownerId: null,
  status: null,
  email: null
});

export function setAuthToken(token, role = null) {
  if (token) {
    localStorage.setItem('jwt_token', token);
    const resolvedRole = role || getRoleFromToken(token);
    authStore.set({ token, isAuthenticated: true, role: resolvedRole });
  } else {
    localStorage.removeItem('jwt_token');
    authStore.set({ token: null, isAuthenticated: false, role: null });
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
