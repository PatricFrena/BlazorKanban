// StudyBoard — JavaScript helpers

// Drag & drop visual feedback
document.addEventListener('dragover', function (e) {
    e.preventDefault();
    const col = e.target.closest('.col-cards');
    if (col) col.classList.add('drag-over');
});

document.addEventListener('dragleave', function (e) {
    const col = e.target.closest('.col-cards');
    if (col && !col.contains(e.relatedTarget)) col.classList.remove('drag-over');
});

document.addEventListener('drop', function (e) {
    document.querySelectorAll('.col-cards').forEach(c => c.classList.remove('drag-over'));
});

// localStorage helpers (chamados pelo Blazor via JSInterop)
window.localStorageHelper = {
    get: (key) => localStorage.getItem(key),
    set: (key, value) => localStorage.setItem(key, value),
    remove: (key) => localStorage.removeItem(key)
};

// Notificação de browser para alertas de prazo
window.requestNotificationPermission = async () => {
    if ('Notification' in window) {
        const result = await Notification.requestPermission();
        return result;
    }
    return 'denied';
};

window.showNotification = (title, body) => {
    if (Notification.permission === 'granted') {
        new Notification(title, { body, icon: '/favicon.ico' });
    }
};

// Scroll suave para um elemento
window.scrollToElement = (id) => {
    const el = document.getElementById(id);
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
};

// Copiar link para clipboard
window.copyToClipboard = async (text) => {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        return false;
    }
};
