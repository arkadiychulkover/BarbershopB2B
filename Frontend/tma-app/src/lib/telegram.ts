export const tg = window.Telegram?.WebApp;

export function initTelegram() {
    if (tg) {
        tg.ready();
        tg.expand();
        if (tg.MainButton) {
            try { tg.MainButton.hide(); } catch (e) { }
        }
    }
}

export function getInitData() {
    return tg?.initData || '';
}

export function showMainButton(text, onClick) {
    if (!tg || !tg.MainButton) return;
    tg.MainButton.setText(text);
    tg.MainButton.onClick(onClick);
    tg.MainButton.show();
}

export function hideMainButton(onClick?: any) {
    if (!tg || !tg.MainButton) return;
    try {
        if (onClick) {
            tg.MainButton.offClick(onClick);
        }
        tg.MainButton.hide();
    } catch (e) { }
}

export function showBackButton(onClick) {
    if (!tg) return;
    tg.BackButton.onClick(onClick);
    tg.BackButton.show();
}

export function hideBackButton(onClick) {
    if (!tg) return;
    if (onClick) {
        tg.BackButton.offClick(onClick);
    }
    tg.BackButton.hide();
}

export function hapticSuccess() {
    tg?.HapticFeedback?.notificationOccurred('success');
}

export function hapticError() {
    tg?.HapticFeedback?.notificationOccurred('error');
}

export function hapticWarning() {
    tg?.HapticFeedback?.notificationOccurred('warning');
}

export function showAlert(message) {
    if (tg) {
        tg.showAlert(message);
    } else {
        alert(message);
    }
}

export function showConfirm(message, callback) {
    if (tg) {
        tg.showConfirm(message, callback);
    } else {
        const result = confirm(message);
        callback(result);
    }
}
