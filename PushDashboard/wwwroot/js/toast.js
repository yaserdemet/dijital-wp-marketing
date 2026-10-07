const Toast = {
    containerId: 'toast-container',

    // Minimal pastel styles per type
    types: {
        success: {
            title: 'Başarılı',
            box: 'bg-emerald-50 border-emerald-100',
            iconBox: 'bg-white border-emerald-100 text-emerald-600',
            titleText: 'text-emerald-800',
            bar: 'bg-emerald-300',
            icon: '<path d="M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Z"></path><path d="m9 12 2 2 4-4"></path>'
        },
        error: {
            title: 'Hata',
            box: 'bg-rose-50 border-rose-100',
            iconBox: 'bg-white border-rose-100 text-rose-600',
            titleText: 'text-rose-800',
            bar: 'bg-rose-300',
            icon: '<path d="M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Z"></path><path d="m15 9-6 6"></path><path d="m9 9 6 6"></path>'
        },
        warning: {
            title: 'Uyarı',
            box: 'bg-amber-50 border-amber-100',
            iconBox: 'bg-white border-amber-100 text-amber-600',
            titleText: 'text-amber-800',
            bar: 'bg-amber-300',
            icon: '<path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3"></path><path d="M12 9v4"></path><path d="M12 17h.01"></path>'
        },
        info: {
            title: 'Bilgi',
            box: 'bg-sky-50 border-sky-100',
            iconBox: 'bg-white border-sky-100 text-sky-600',
            titleText: 'text-sky-800',
            bar: 'bg-sky-300',
            icon: '<path d="M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Z"></path><path d="M12 16v-4"></path><path d="M12 8h.01"></path>'
        }
    },

    init() {
        if (!document.getElementById(this.containerId)) {
            const container = document.createElement('div');
            container.id = this.containerId;
            // Fixed position, top right
            container.className = 'fixed top-5 right-5 z-[99999] flex flex-col gap-3 pointer-events-none';
            container.style.setProperty('z-index', '99999', 'important');
            document.body.appendChild(container);
        }
    },

    show(message, type = 'info', duration = 5000) {
        this.init();

        const cfg = this.types[type] || this.types.info;
        const container = document.getElementById(this.containerId);
        const toast = document.createElement('div');

        toast.className = `pointer-events-auto relative overflow-hidden transform transition-all duration-300 ease-out translate-x-4 opacity-0 flex items-start w-full max-w-sm px-4 py-3 rounded-xl border shadow-[0_2px_8px_rgba(0,0,0,0.05)] ${cfg.box}`;
        toast.setAttribute('role', type === 'error' ? 'alert' : 'status');

        toast.innerHTML = `
            <div class="inline-flex items-center justify-center flex-shrink-0 w-8 h-8 rounded-lg border ${cfg.iconBox}">
                <svg class="w-[18px] h-[18px]" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">${cfg.icon}</svg>
            </div>
            <div class="ml-3 flex-1 min-w-0">
                <div class="text-xs font-medium ${cfg.titleText}">${cfg.title}</div>
                <div class="text-sm text-gray-700 break-words mt-0.5">${message}</div>
            </div>
            <button type="button" class="ml-3 -mr-1 -mt-0.5 p-1 rounded-md text-gray-400 hover:text-gray-700 hover:bg-white/70 inline-flex items-center justify-center transition-colors" aria-label="Kapat">
                <svg class="w-4 h-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M18 6 6 18"></path><path d="m6 6 12 12"></path></svg>
            </button>
            ${duration > 0 ? `<div class="toast-progress absolute left-0 bottom-0 h-0.5 w-full ${cfg.bar}" style="transform-origin:left;transition:transform ${duration}ms linear;"></div>` : ''}
        `;

        // Close button functionality
        toast.querySelector('button').addEventListener('click', () => {
            this.dismiss(toast);
        });

        container.appendChild(toast);

        // Animate in
        // Small delay to ensure DOM is updated and transition works
        setTimeout(() => {
            toast.classList.remove('translate-x-4', 'opacity-0');
            const bar = toast.querySelector('.toast-progress');
            if (bar) bar.style.transform = 'scaleX(0)';
        }, 10);

        // Auto dismiss
        if (duration > 0) {
            setTimeout(() => {
                this.dismiss(toast);
            }, duration);
        }
    },

    dismiss(toast) {
        toast.classList.add('translate-x-4', 'opacity-0');
        setTimeout(() => {
            if(toast.parentElement) {
                toast.parentElement.removeChild(toast);
            }
        }, 300);
    },

    success(message, duration) { this.show(message, 'success', duration); },
    error(message, duration) { this.show(message, 'error', duration); },
    warning(message, duration) { this.show(message, 'warning', duration); },
    info(message, duration) { this.show(message, 'info', duration); }
};

// Backward compatibility shim for existing code
window.showNotification = function(arg1, arg2, arg3) {
    // Heuristic to determine which is message and which is type
    // Existing usage seems to be (message, type) in some files, but definition was (type, message)
    let message, type, duration;

    const types = ['success', 'error', 'warning', 'info'];

    if (types.includes(arg1) && !types.includes(arg2)) {
        // (type, message, duration) - matching old _Layout.cshtml definition
        type = arg1;
        message = arg2;
        duration = arg3;
    } else {
        // (message, type, duration) - matching product-slider-edit.js usage
        message = arg1;
        type = arg2 || 'info';
        duration = arg3;
    }

    Toast.show(message, type, duration);
};
