// ArdraKinetix - Global UI Utilities & Component Helpers

window.TaskBoard = {
    // Toast Notification System (Light Theme)
    showToast: function (message, type = 'success') {
        const container = document.getElementById('toast-container');
        if (!container) return;

        const toast = document.createElement('div');
        const isSuccess = type === 'success';
        const isError = type === 'error' || type === 'danger';
        const isWarning = type === 'warning';

        const bgClass = isSuccess 
            ? 'bg-white border-emerald-200 text-emerald-900 shadow-emerald-500/10' 
            : isError 
                ? 'bg-white border-rose-200 text-rose-900 shadow-rose-500/10' 
                : isWarning 
                    ? 'bg-white border-amber-200 text-amber-900 shadow-amber-500/10' 
                    : 'bg-white border-indigo-200 text-indigo-900 shadow-indigo-500/10';

        const iconSvg = isSuccess
            ? '<div class="w-8 h-8 rounded-lg bg-emerald-50 text-emerald-600 flex items-center justify-center shrink-0"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"></path></svg></div>'
            : isError
                ? '<div class="w-8 h-8 rounded-lg bg-rose-50 text-rose-600 flex items-center justify-center shrink-0"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12"></path></svg></div>'
                : '<div class="w-8 h-8 rounded-lg bg-amber-50 text-amber-600 flex items-center justify-center shrink-0"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg></div>';

        toast.className = `flex items-center gap-3 p-3.5 rounded-2xl border shadow-xl toast-enter transition-all duration-300 pointer-events-auto max-w-md ${bgClass}`;
        toast.innerHTML = `
            ${iconSvg}
            <div class="text-xs font-semibold flex-1 leading-snug">${message}</div>
            <button onclick="this.parentElement.remove()" class="text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
            </button>
        `;

        container.appendChild(toast);

        setTimeout(() => {
            toast.classList.add('toast-exit');
            setTimeout(() => toast.remove(), 300);
        }, 4500);
    },

    // Modal Manager
    openModal: function (modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.remove('hidden');
            modal.classList.add('flex');
            document.body.classList.add('overflow-hidden');
        }
    },

    closeModal: function (modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.add('hidden');
            modal.classList.remove('flex');
            document.body.classList.remove('overflow-hidden');
        }
    },

    // Tab Switcher for Task Details
    switchTab: function (tabName) {
        document.querySelectorAll('[data-tab-content]').forEach(el => el.classList.add('hidden'));
        document.querySelectorAll('[data-tab-button]').forEach(el => {
            el.classList.remove('text-indigo-600', 'border-indigo-600', 'bg-indigo-50/80');
            el.classList.add('text-slate-500', 'border-transparent');
        });

        const targetContent = document.querySelector(`[data-tab-content="${tabName}"]`);
        const targetBtn = document.querySelector(`[data-tab-button="${tabName}"]`);

        if (targetContent) targetContent.classList.remove('hidden');
        if (targetBtn) {
            targetBtn.classList.add('text-indigo-600', 'border-indigo-600', 'bg-indigo-50/80');
            targetBtn.classList.remove('text-slate-500', 'border-transparent');
        }

        const url = new URL(window.location);
        url.searchParams.set('tab', tabName);
        window.history.replaceState({}, '', url);
    },

    // Antiforgery Token Helper
    getAntiForgeryToken: function () {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }
};

// Global click-outside listener for vanilla dropdown fallbacks
document.addEventListener('click', function (e) {
    document.querySelectorAll('[data-dropdown-menu]').forEach(menu => {
        const toggle = document.querySelector(`[data-dropdown-toggle="${menu.id}"]`);
        if (toggle && !toggle.contains(e.target) && !menu.contains(e.target)) {
            menu.classList.add('hidden');
        }
    });
});

function toggleDropdown(id) {
    const el = document.getElementById(id);
    if (el) el.classList.toggle('hidden');
}
