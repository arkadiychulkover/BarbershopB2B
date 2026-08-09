import { writable } from 'svelte/store';
export const authStore = writable({
    status: 'loading',
    token: null
});
export function setAuthStatus(status, token = null) {
    authStore.set({ status, token });
}
