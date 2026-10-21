// API Client with rate limiting and error handling

class ApiClient {
    constructor(baseUrl) {
        this.baseUrl = baseUrl || '';
        this.requestQueue = [];
        this.isProcessing = false;
        this.maxRetries = 3;
    }

    async request(url, options = {}) {
        const defaultOptions = {
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            credentials: 'include'
        };

        const mergedOptions = {
            ...defaultOptions,
            ...options,
            headers: {
                ...defaultOptions.headers,
                ...options.headers
            }
        };

        // Initialize retry count if not present
        if (!mergedOptions._retryCount) {
            mergedOptions._retryCount = 0;
        }

        try {
            showLoading();
            const response = await fetch(this.baseUrl + url, mergedOptions);
            
            // Handle rate limiting
            if (response.status === 429) {
                if (mergedOptions._retryCount >= this.maxRetries) {
                    throw new Error('Maximum retry attempts exceeded');
                }
                return await this.handleRateLimit(response, url, mergedOptions);
            }

            // Handle unauthorized
            if (response.status === 401) {
                this.handleUnauthorized();
                throw new Error('Unauthorized');
            }

            // Handle other errors
            if (!response.ok) {
                const error = await this.parseError(response);
                throw error;
            }

            // Parse successful response
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                return await response.json();
            }

            return await response.text();
        } catch (error) {
            this.handleError(error);
            throw error;
        } finally {
            hideLoading();
        }
    }

    async handleRateLimit(response, url, options) {
        const retryAfter = response.headers.get('Retry-After');
        const seconds = retryAfter ? parseInt(retryAfter) : 60;

        // Increment retry count
        options._retryCount = (options._retryCount || 0) + 1;

        // Show user-friendly message
        this.showNotification(
            'error',
            'Too Many Requests',
            `Please wait ${seconds} seconds before trying again. (Attempt ${options._retryCount}/${this.maxRetries})`
        );

        // Wait and retry
        await this.sleep(seconds * 1000);
        return await this.request(url, options);
    }

    async parseError(response) {
        try {
            const errorData = await response.json();
            return new Error(errorData.message || errorData.error || 'An error occurred');
        } catch {
            return new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
    }

    handleUnauthorized() {
        // Redirect to login
        window.location.href = '/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
    }

    handleError(error) {
        console.error('API Error:', error);
        
        // Show user-friendly error message
        let message = 'An unexpected error occurred. Please try again.';
        
        if (error.message) {
            message = error.message;
        }

        this.showNotification('error', 'Error', message);
    }

    showNotification(type, title, message) {
        // Use toast notification if available
        if (window.showToast) {
            window.showToast(type, title, message);
        } else {
            // Fallback to alert
            alert(`${title}: ${message}`);
        }
    }

    sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    // Convenience methods
    async get(url, options = {}) {
        return await this.request(url, { ...options, method: 'GET' });
    }

    async post(url, data, options = {}) {
        return await this.request(url, {
            ...options,
            method: 'POST',
            body: JSON.stringify(data)
        });
    }

    async put(url, data, options = {}) {
        return await this.request(url, {
            ...options,
            method: 'PUT',
            body: JSON.stringify(data)
        });
    }

    async delete(url, options = {}) {
        return await this.request(url, { ...options, method: 'DELETE' });
    }

    async patch(url, data, options = {}) {
        return await this.request(url, {
            ...options,
            method: 'PATCH',
            body: JSON.stringify(data)
        });
    }
}

// Create global instance
window.apiClient = new ApiClient();

// Helper functions to show/hide loading overlay
function showLoading() {
    // Check for external loading handler (not this function itself)
    if (window.showLoadingOverlay && typeof window.showLoadingOverlay === 'function') {
        window.showLoadingOverlay();
    } else {
        // Fallback: add a simple loading indicator
        const existingLoader = document.getElementById('api-loading-indicator');
        if (!existingLoader) {
            const loader = document.createElement('div');
            loader.id = 'api-loading-indicator';
            loader.style.cssText = 'position:fixed;top:0;left:0;right:0;height:3px;background:linear-gradient(90deg,#6366f1,#8b5cf6);z-index:9999;';
            document.body.appendChild(loader);
        }
    }
}

function hideLoading() {
    // Check for external loading handler (not this function itself)
    if (window.hideLoadingOverlay && typeof window.hideLoadingOverlay === 'function') {
        window.hideLoadingOverlay();
    } else {
        // Fallback: remove the loading indicator
        const loader = document.getElementById('api-loading-indicator');
        if (loader) {
            loader.remove();
        }
    }
}
