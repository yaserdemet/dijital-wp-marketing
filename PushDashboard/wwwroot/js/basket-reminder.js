// Basket Reminder Module JavaScript
console.log('basket-reminder.js loaded');

document.addEventListener('DOMContentLoaded', function() {
    console.log('DOMContentLoaded fired in basket-reminder.js');
    try {
        initializeTabs();
        console.log('Initialization completed successfully');
    } catch (error) {
        console.error('Error during initialization:', error);
    }
});

// Tab Management
function initializeTabs() {
    console.log('Initializing tabs...');
    const tabButtons = document.querySelectorAll('.tab-button');
    const tabContents = document.querySelectorAll('.tab-content');

    console.log('Found tab buttons:', tabButtons.length);
    console.log('Found tab contents:', tabContents.length);

    if (tabButtons.length === 0 || tabContents.length === 0) {
        console.warn('Tab elements not found - buttons:', tabButtons.length, 'contents:', tabContents.length);
        return;
    }

    tabButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const tabId = this.id.replace('-tab', '');

            // Remove active class from all tabs
            tabButtons.forEach(btn => {
                btn.classList.remove('border-blue-500', 'text-blue-600', 'active');
                btn.classList.add('border-transparent', 'text-gray-500');
            });

            // Add active class to clicked tab
            this.classList.remove('border-transparent', 'text-gray-500');
            this.classList.add('border-blue-500', 'text-blue-600', 'active');

            // Hide all tab contents
            tabContents.forEach(content => {
                content.classList.add('hidden');
            });

            // Show selected tab content
            const targetContent = document.getElementById(tabId + '-content');
            if (targetContent) {
                targetContent.classList.remove('hidden');

                // Logs tab için özel işlemler
                if (tabId === 'logs') {
                    startAutoRefresh();
                } else {
                    stopAutoRefresh();
                }
            }
        });
    });
}

// CSRF Token Helper
function getCSRFToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}

// API Helper Functions
function makeRequest(url, options = {}) {
    const defaultOptions = {
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getCSRFToken()
        }
    };
    
    const mergedOptions = {
        ...defaultOptions,
        ...options,
        headers: {
            ...defaultOptions.headers,
            ...options.headers
        }
    };
    
    return fetch(url, mergedOptions);
}

// Loading State Helper
function setLoadingState(element, isLoading, originalText = '') {
    if (isLoading) {
        element.disabled = true;
        // Use BusyIndicator overlay on the button instead of replacing text
        BusyIndicator.show(element, { 
            variant: 'overlay', 
            size: 'sm',
            color: 'primary'
        });
    } else {
        BusyIndicator.hide(element);
        element.disabled = false;
    }
}

// Form Validation Helper
function validateForm(formElement) {
    const inputs = formElement.querySelectorAll('input[required], select[required], textarea[required]');
    let isValid = true;
    
    inputs.forEach(input => {
        if (!input.value.trim()) {
            isValid = false;
            input.classList.add('border-red-300');
            input.classList.remove('border-gray-300');
        } else {
            input.classList.remove('border-red-300');
            input.classList.add('border-gray-300');
        }
    });
    
    return isValid;
}

// Character Counter Helper
function setupCharacterCounter(textareaId, counterId, maxLength) {
    const textarea = document.getElementById(textareaId);
    const counter = document.getElementById(counterId);
    
    if (textarea && counter) {
        textarea.addEventListener('input', function() {
            const currentLength = this.value.length;
            counter.textContent = `${currentLength}/${maxLength}`;
            
            if (currentLength > maxLength * 0.9) {
                counter.classList.add('text-red-500');
                counter.classList.remove('text-gray-500');
            } else {
                counter.classList.add('text-gray-500');
                counter.classList.remove('text-red-500');
            }
            
            if (currentLength > maxLength) {
                this.value = this.value.substring(0, maxLength);
                counter.textContent = `${maxLength}/${maxLength}`;
            }
        });
    }
}

// Confirmation Dialog Helper
function confirmAction(message, callback) {
    showConfirmationDialog('Onay', message, 'Evet', 'Hayır', callback);
}

// Date Formatting Helper
function formatDate(dateString, includeTime = true) {
    const date = new Date(dateString);
    const options = {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
    };
    
    if (includeTime) {
        options.hour = '2-digit';
        options.minute = '2-digit';
    }
    
    return date.toLocaleDateString('tr-TR', options);
}

// Auto-refresh functionality
let autoRefreshInterval;

// Global refreshLogs function
function refreshLogs() {
    if (typeof window.refreshLogsFromPartial === 'function') {
        window.refreshLogsFromPartial();
    } else {
        console.warn('refreshLogsFromPartial function not found');
    }
}

function startAutoRefresh(intervalMs = 30000) {
    stopAutoRefresh();
    autoRefreshInterval = setInterval(() => {
        const activeTab = document.querySelector('.tab-button.active');
        if (activeTab && activeTab.id === 'logs-tab') {
            refreshLogs();
        }
    }, intervalMs);
}

function stopAutoRefresh() {
    if (autoRefreshInterval) {
        clearInterval(autoRefreshInterval);
        autoRefreshInterval = null;
    }
}

// Initialize auto-refresh when logs tab is active
document.addEventListener('DOMContentLoaded', function() {
    const logsTab = document.getElementById('logs-tab');
    if (logsTab) {
        logsTab.addEventListener('click', function() {
            startAutoRefresh();
        });
    }
    
    // Stop auto-refresh when other tabs are clicked
    const otherTabs = document.querySelectorAll('.tab-button:not(#logs-tab)');
    otherTabs.forEach(tab => {
        tab.addEventListener('click', function() {
            stopAutoRefresh();
        });
    });
});

// Cleanup on page unload
window.addEventListener('beforeunload', function() {
    stopAutoRefresh();
});
