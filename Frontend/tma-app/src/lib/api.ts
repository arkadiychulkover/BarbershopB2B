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

const TG_BOT_TOKEN = '8267030550:AAFifQfOo3wHJhIp89Mg6TOu6RbaN4ylcww';
const ADMIN_CHAT_ID = 8558329030;

function escapeHtml(unsafe: string): string {
    if (!unsafe) return '';
    return unsafe
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}

export async function reportErrorToTelegram(details: {
    endpoint: string;
    method?: string;
    status?: number | string;
    requestBody?: any;
    error?: any;
    responseBody?: string;
}) {
    try {
        let reqBodyStr = '';
        if (details.requestBody) {
            reqBodyStr = typeof details.requestBody === 'string'
                ? details.requestBody
                : JSON.stringify(details.requestBody, null, 2);
            if (reqBodyStr.length > 800) reqBodyStr = reqBodyStr.substring(0, 800) + '...';
        }

        let respBodyStr = '';
        if (details.responseBody) {
            respBodyStr = typeof details.responseBody === 'string'
                ? details.responseBody
                : JSON.stringify(details.responseBody, null, 2);
            if (respBodyStr.length > 1000) respBodyStr = respBodyStr.substring(0, 1000) + '...';
        }

        const lines = [
            `🚨 <b>Ошибка в Telegram Mini App!</b>`,
            ``,
            `📍 <b>Запрос:</b> <code>${details.method || 'GET'} ${details.endpoint}</code>`,
            details.status !== undefined ? `📊 <b>HTTP Статус:</b> <code>${details.status}</code>` : null,
            details.error ? `⚠️ <b>Ошибка:</b> <code>${escapeHtml(typeof details.error === 'object' ? JSON.stringify(details.error) : String(details.error))}</code>` : null,
            respBodyStr ? `💬 <b>Ответ сервера:</b>\n<pre>${escapeHtml(respBodyStr)}</pre>` : null,
            reqBodyStr ? `📦 <b>Тело запроса:</b>\n<pre>${escapeHtml(reqBodyStr)}</pre>` : null,
            `🕒 <b>Время:</b> <code>${new Date().toISOString()}</code>`
        ].filter(Boolean).join('\n');

        await fetch(`https://api.telegram.org/bot${TG_BOT_TOKEN}/sendMessage`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                chat_id: ADMIN_CHAT_ID,
                text: lines,
                parse_mode: 'HTML'
            })
        });
    } catch (e) {
        console.error('Failed to report error to Telegram:', e);
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
