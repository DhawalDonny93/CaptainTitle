
// Simple toast notification system
type ToastType = 'success' | 'error' | 'warning' | 'info';

interface ToastOptions {
    duration?: number;
    position?: 'top-right' | 'top-center' | 'bottom-right' | 'bottom-center';
}

class ToastManager {
    private container: HTMLDivElement | null = null;
    private toasts: Map<string, HTMLDivElement> = new Map();

    private ensureContainer() {
        if (!this.container) {
            this.container = document.createElement('div');
            this.container.className = 'fixed top-4 right-4 z-50 flex flex-col gap-2';
            document.body.appendChild(this.container);
        }
        return this.container;
    }

    private getIcon(type: ToastType) {
        switch (type) {
            case 'success':
                return '<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>';
            case 'error':
                return '<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>';
            case 'warning':
                return '<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>';
            case 'info':
                return '<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>';
        }
    }

    private getColors(type: ToastType) {
        switch (type) {
            case 'success':
                return 'bg-green-50 border-green-200 text-green-800';
            case 'error':
                return 'bg-red-50 border-red-200 text-red-800';
            case 'warning':
                return 'bg-yellow-50 border-yellow-200 text-yellow-800';
            case 'info':
                return 'bg-blue-50 border-blue-200 text-blue-800';
        }
    }

    show(message: string, type: ToastType = 'info', options: ToastOptions = {}) {
        const container = this.ensureContainer();
        const id = `toast-${Date.now()}-${Math.random()}`;
        const duration = options.duration || 3000;

        const toast = document.createElement('div');
        toast.id = id;
        toast.className = `${this.getColors(type)} border rounded-lg px-4 py-3 shadow-lg flex items-center gap-3 min-w-[300px] max-w-md animate-slide-in`;
        toast.innerHTML = `
      <div class="flex-shrink-0">
        ${this.getIcon(type)}
      </div>
      <div class="flex-1 text-sm font-medium">
        ${message}
      </div>
      <button class="flex-shrink-0 text-gray-400 hover:text-gray-600 transition-colors" onclick="this.parentElement.remove()">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
        </svg>
      </button>
    `;

        container.appendChild(toast);
        this.toasts.set(id, toast);

        // Auto-remove after duration
        setTimeout(() => {
            toast.style.animation = 'slide-out 0.3s ease-out';
            setTimeout(() => {
                toast.remove();
                this.toasts.delete(id);
            }, 300);
        }, duration);

        return id;
    }

    success(message: string, options?: ToastOptions) {
        return this.show(message, 'success', options);
    }

    error(message: string, options?: ToastOptions) {
        return this.show(message, 'error', options);
    }

    warning(message: string, options?: ToastOptions) {
        return this.show(message, 'warning', options);
    }

    info(message: string, options?: ToastOptions) {
        return this.show(message, 'info', options);
    }

    remove(id: string) {
        const toast = this.toasts.get(id);
        if (toast) {
            toast.remove();
            this.toasts.delete(id);
        }
    }

    clear() {
        this.toasts.forEach(toast => toast.remove());
        this.toasts.clear();
    }
}

// Export singleton instance
export const toast = new ToastManager();

// Add CSS animations to document
if (typeof document !== 'undefined') {
    const style = document.createElement('style');
    style.textContent = `
    @keyframes slide-in {
      from {
        transform: translateX(100%);
        opacity: 0;
      }
      to {
        transform: translateX(0);
        opacity: 1;
      }
    }
    @keyframes slide-out {
      from {
        transform: translateX(0);
        opacity: 1;
      }
      to {
        transform: translateX(100%);
        opacity: 0;
      }
    }
    .animate-slide-in {
      animation: slide-in 0.3s ease-out;
    }
  `;
    document.head.appendChild(style);
}
