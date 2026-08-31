import { authStore } from './stores/auth';

let currentToken = null;
authStore.subscribe(state => {
    currentToken = state.token;
});

const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://backendbarbershopdomen.online';

export async function apiFetch(endpoint, options = {}) {
    const url = `${BASE_URL}${endpoint}`;
    
    const headers = new Headers(options.headers || {});
    if (currentToken && !options.skipAuth) {
        headers.set('Authorization', `Bearer ${currentToken}`);
    }
    if (!headers.has('Content-Type') && options.body && typeof options.body !== 'string' && !(options.body instanceof FormData)) {
        headers.set('Content-Type', 'application/json');
        options.body = JSON.stringify(options.body);
    }
    
    const config = {
        ...options,
        headers
    };
    
    const response = await fetch(url, config);
    if (response.status === 401) {
        authStore.set({ status: 'error', token: null });
        throw new Error('Unauthorized');
    }
    if (response.status === 409) {
        const errorData = await response.json();
        throw { status: 409, message: errorData.message || 'Конфликт данных' };
    }
    
    if (!response.ok) {
        let message = 'Network response was not ok';
        const errorText = await response.text();
        try {
            const errorData = JSON.parse(errorText);
            message = errorData.message || message;
        } catch(e) {
            message = errorText || message;
        }
        
        throw { status: response.status, message };
    }
    const text = await response.text();
    try {
        return text ? JSON.parse(text) : null;
    } catch(e) {
        return text;
    }
}

/**
 * Fetches an image path as a Blob and returns a local object URL.
 * Needed when the backend is behind ngrok or similar tunnels that
 * block unauthenticated static-file requests.
 * IMPORTANT: caller must call URL.revokeObjectURL() when done.
 */
export async function fetchImageBlob(path: string): Promise<string> {
    const headers = new Headers();
    if (currentToken) {
        headers.set('Authorization', `Bearer ${currentToken}`);
    }
    // ngrok requires this header to skip the browser warning page
    headers.set('ngrok-skip-browser-warning', 'true');

    const cleanBase = (BASE_URL || '').replace(/\/+$/, '');
    const cleanPath = path.startsWith('http') ? path : `${cleanBase}/${path.replace(/^\/+/, '')}`;

    const response = await fetch(cleanPath, { headers });
    if (!response.ok) throw new Error(`Image fetch failed: ${response.status}`);
    const blob = await response.blob();
    return URL.createObjectURL(blob);
}
