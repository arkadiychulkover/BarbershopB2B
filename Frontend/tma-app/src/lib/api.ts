import { authStore } from './stores/auth';
import { m } from './paraglide/messages.js';

let currentToken = null;
authStore.subscribe(state => {
    currentToken = state.token;
});

export const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://backendbarbershopdomen.online';

export function getFullImageUrl(path: string | null | undefined): string {
    if (!path) return '';
    if (path.startsWith('http')) return path;
    const cleanBase = (BASE_URL || '').replace(/\/+$/, '');
    const cleanPath = path.replace(/^\/+/, '');
    return `${cleanBase}/${cleanPath}`;
}

export async function reportErrorToTelegram(details: {
    endpoint: string;
    method?: string;
    status?: number | string;
    requestBody?: any;
    error?: any;
    responseBody?: string;
}) {
    // Avoid recursion if error reporting itself fails
    if (details.endpoint?.includes('/api/Tracking/report-error')) return;

    try {
        await fetch(`${BASE_URL}/api/Tracking/report-error`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                source: 'TMA',
                endpoint: details.endpoint,
                method: details.method || 'GET',
                status: String(details.status ?? ''),
                error: typeof details.error === 'object' ? JSON.stringify(details.error) : String(details.error || ''),
                responseBody: typeof details.responseBody === 'string' ? details.responseBody : (details.responseBody ? JSON.stringify(details.responseBody) : null),
                requestBody: typeof details.requestBody === 'string' ? details.requestBody : (details.requestBody ? JSON.stringify(details.requestBody) : null)
            })
        });
    } catch (e) {
        console.error('Failed to report error to backend:', e);
    }
}

export async function apiFetch(endpoint: string, options: any = {}) {
    const url = `${BASE_URL}${endpoint}`;
    
    const headers = new Headers(options.headers || {});
    if (currentToken && !options.skipAuth) {
        headers.set('Authorization', `Bearer ${currentToken}`);
    }

    let body = options.body;
    if (body && !(body instanceof FormData)) {
        if (!headers.has('Content-Type')) {
            headers.set('Content-Type', 'application/json');
        }
        if (typeof body !== 'string') {
            body = JSON.stringify(body);
        }
    }
    
    const config: RequestInit = {
        ...options,
        headers,
        body
    };
    
    let response: Response;
    try {
        response = await fetch(url, config);
    } catch (networkErr: any) {
        console.error('apiFetch network error:', networkErr);
        await reportErrorToTelegram({
            endpoint,
            method: (config.method as string) || 'GET',
            status: 'NETWORK_FAILED',
            requestBody: body,
            error: networkErr?.message || String(networkErr)
        });
        throw {
            status: 0,
            message: networkErr?.message || 'Network response was not ok'
        };
    }

    if (response.status === 401) {
        authStore.set({ status: 'error', token: null });
        await reportErrorToTelegram({
            endpoint,
            method: (config.method as string) || 'GET',
            status: 401,
            requestBody: body,
            error: 'Unauthorized (401) - Token expired or invalid'
        });
        throw new Error('Unauthorized');
    }
    if (response.status === 409) {
        const errorData = await response.json();
        const msg = errorData.message || m.tma_conflict_error();
        await reportErrorToTelegram({
            endpoint,
            method: (config.method as string) || 'GET',
            status: 409,
            requestBody: body,
            error: msg,
            responseBody: JSON.stringify(errorData)
        });
        throw { status: 409, message: msg };
    }
    
    if (!response.ok) {
        let message = 'Network response was not ok';
        const errorText = await response.text();
        try {
            const errorData = JSON.parse(errorText);
            if (errorData) {
                if (typeof errorData === 'string') {
                    message = errorData;
                } else if (errorData.message) {
                    message = errorData.message;
                } else if (errorData.error) {
                    message = errorData.error;
                } else if (errorData.title) {
                    message = errorData.title;
                    if (errorData.errors && typeof errorData.errors === 'object') {
                        const fieldErrors = Object.values(errorData.errors).flat().join(', ');
                        if (fieldErrors) message += `: ${fieldErrors}`;
                    }
                }
            }
        } catch(e) {
            if (errorText) message = errorText;
        }

        await reportErrorToTelegram({
            endpoint,
            method: (config.method as string) || 'GET',
            status: response.status,
            requestBody: body,
            error: message,
            responseBody: errorText
        });
        
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
