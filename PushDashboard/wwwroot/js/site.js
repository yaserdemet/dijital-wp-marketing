// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Global confirmation dialog function
function showConfirmationDialog(title, message, confirmText = 'Onayla', cancelText = 'İptal', onConfirm = null, onCancel = null, type = null) {
    let modal = document.getElementById('global-confirmation-modal');
    
    // Auto-detect type if not provided
    if (!type) {
        const lowerTitle = (title || '').toLowerCase();
        const lowerText = (confirmText || '').toLowerCase();
        if (lowerTitle.includes('sil') || lowerTitle.includes('delete') || lowerText.includes('sil') || lowerTitle.includes('kaldır')) {
            type = 'danger';
        } else if (lowerTitle.includes('uyarı') || lowerTitle.includes('warning') || lowerTitle.includes('dikkat') || lowerTitle.includes('reset') || lowerTitle.includes('sıfırla')) {
            type = 'warning';
        } else if (lowerTitle.includes('başarılı') || lowerTitle.includes('success')) {
            type = 'success';
        } else {
            type = 'info';
        }
    }

    // Fallback if partial is not loaded
    if (!modal) {
        const modalHtml = `
            <div class="relative top-20 mx-auto p-5 border max-w-lg shadow-xl rounded-xl bg-white transition-all transform duration-300">
                <div class="mt-3 text-center">
                    <div class="mx-auto flex items-center justify-center h-16 w-16 rounded-full mb-6 transition-colors duration-300 bg-blue-100" id="global-confirmation-icon-container">
                        <svg id="global-icon-info" class="h-10 w-10 text-blue-600 hidden" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        <svg id="global-icon-success" class="h-10 w-10 text-green-600 hidden" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        <svg id="global-icon-warning" class="h-10 w-10 text-amber-600 hidden" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                             <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
                        </svg>
                        <svg id="global-icon-danger" class="h-10 w-10 text-red-600 hidden" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                    </div>
                    
                    <h3 id="global-confirmation-title" class="text-xl leading-6 font-semibold text-gray-900 mb-2"></h3>
                    
                    <div class="mt-2 px-4">
                        <div id="global-confirmation-message" class="text-sm text-gray-500 break-words"></div>
                    </div>

                    <div class="mt-6 flex justify-center space-x-3">
                        <button id="global-confirmation-cancel" type="button" class="px-5 py-2.5 rounded-lg border border-gray-300 text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-200 transition-colors">
                            İptal
                        </button>
                        <button id="global-confirmation-confirm" type="button" class="px-5 py-2.5 rounded-lg shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors">
                            Onayla
                        </button>
                    </div>
                </div>
            </div>
        `;

        modal = document.createElement('div');
        modal.id = 'global-confirmation-modal';
        modal.className = 'fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50 hidden';
        modal.style.zIndex = '9999'; // Ensure it's on top of everything
        modal.style.display = 'none';
        modal.setAttribute('aria-labelledby', 'modal-title');
        modal.setAttribute('role', 'dialog');
        modal.setAttribute('aria-modal', 'true');
        modal.innerHTML = modalHtml;
        document.body.appendChild(modal);
    }

    const titleEl = document.getElementById('global-confirmation-title');
    const messageEl = document.getElementById('global-confirmation-message');
    const confirmBtn = document.getElementById('global-confirmation-confirm');
    const cancelBtn = document.getElementById('global-confirmation-cancel');
    const iconContainer = document.getElementById('global-confirmation-icon-container');

    // Reset icons
    ['info', 'success', 'warning', 'danger'].forEach(t => {
        const icon = document.getElementById(`global-icon-${t}`);
        if(icon) icon.classList.add('hidden');
    });

    // Determine colors and show icon
    let btnClass = 'bg-blue-600 hover:bg-blue-700 focus:ring-blue-500';
    let containerClass = 'bg-blue-100';
    
    // Default to info if type is invalid
    if (!['info', 'success', 'warning', 'danger'].includes(type)) type = 'info';

    const activeIcon = document.getElementById(`global-icon-${type}`);
    if (activeIcon) activeIcon.classList.remove('hidden');

    switch (type) {
        case 'success':
            btnClass = 'bg-green-600 hover:bg-green-700 focus:ring-green-500';
            containerClass = 'bg-green-100';
            break;
        case 'warning':
            btnClass = 'bg-amber-600 hover:bg-amber-700 focus:ring-amber-500';
            containerClass = 'bg-amber-100';
            break;
        case 'danger':
            btnClass = 'bg-red-600 hover:bg-red-700 focus:ring-red-500';
            containerClass = 'bg-red-100';
            break;
        case 'info':
        default:
            btnClass = 'bg-blue-600 hover:bg-blue-700 focus:ring-blue-500';
            containerClass = 'bg-blue-100';
            break;
    }

    // Apply classes
    confirmBtn.className = `px-5 py-2.5 rounded-lg shadow-sm text-sm font-medium text-white transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 ${btnClass}`;
    iconContainer.className = `mx-auto flex items-center justify-center h-16 w-16 rounded-full mb-6 transition-colors duration-300 ${containerClass}`;

    // Update content
    titleEl.textContent = title;
    messageEl.innerHTML = message;
    confirmBtn.textContent = confirmText;
    cancelBtn.textContent = cancelText;

    // Helper to close modal
    const closeModal = () => {
        modal.classList.add('hidden');
        modal.style.display = 'none';
        document.removeEventListener('keydown', handleEscape);
    };

    // Replace buttons to remove old event listeners
    const newConfirmBtn = confirmBtn.cloneNode(true);
    confirmBtn.parentNode.replaceChild(newConfirmBtn, confirmBtn);
    
    const newCancelBtn = cancelBtn.cloneNode(true);
    cancelBtn.parentNode.replaceChild(newCancelBtn, cancelBtn);

    // Add new listeners
    newConfirmBtn.addEventListener('click', () => {
        closeModal();
        if (onConfirm) onConfirm();
    });

    newCancelBtn.addEventListener('click', () => {
        closeModal();
        if (onCancel) onCancel();
    });

    // Handle backdrop click
    modal.onclick = (e) => {
         if (e.target === modal) {
            closeModal();
            if (onCancel) onCancel();
        }
    };

    // Handle Escape key
    const handleEscape = (e) => {
        if (e.key === 'Escape') {
            closeModal();
            if (onCancel) onCancel();
        }
    };

    // Show modal
    modal.style.zIndex = '9999'; // Ensure it stays on top
    modal.classList.remove('hidden');
    modal.style.display = 'block';
    
    document.addEventListener('keydown', handleEscape);
}

// Make function globally available
window.showConfirmationDialog = showConfirmationDialog;

// Force modal function - cannot be closed by user
function showForceModal(title, message, confirmText, onConfirm, type = 'warning') {
  // Create modal backdrop
  const backdrop = document.createElement('div');
  backdrop.className = 'fixed inset-0 bg-gray-600 bg-opacity-75 overflow-y-auto h-full w-full z-[9999]';
  backdrop.id = 'force-modal-backdrop';

  // Create modal content
  const modal = document.createElement('div');
  modal.className = 'relative top-20 mx-auto p-5 border w-96 shadow-2xl rounded-md bg-white';

  const colors = {
    danger: 'text-red-600',
    warning: 'text-yellow-600',
    info: 'text-blue-600'
  };

  modal.innerHTML = `
    <div class="mt-3 text-center">
      <div class="mx-auto flex items-center justify-center h-12 w-12 rounded-full ${type === 'danger' ? 'bg-red-100' : type === 'warning' ? 'bg-yellow-100' : 'bg-blue-100'}">
        <svg class="h-6 w-6 ${colors[type] || colors.info}" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
        </svg>
      </div>
      <h3 class="text-lg leading-6 font-medium text-gray-900 mt-2">${title}</h3>
      <div class="mt-2 px-7 py-3">
        <p class="text-sm text-gray-500">${message}</p>
      </div>
      <div class="items-center px-4 py-3">
        <button id="force-modal-confirm" class="px-4 py-2 bg-red-500 hover:bg-red-700 text-white text-base font-medium rounded-md w-full">
          ${confirmText}
        </button>
      </div>
    </div>
  `;

  backdrop.appendChild(modal);
  document.body.appendChild(backdrop);

  // Only allow confirm action - no cancel, no ESC, no backdrop click
  document.getElementById('force-modal-confirm').addEventListener('click', () => {
    onConfirm();
    document.body.removeChild(backdrop);
    // Remove ESC key listener
    document.removeEventListener('keydown', preventEscape);
  });

  // Prevent ESC key from closing
  const preventEscape = (e) => {
    if (e.key === 'Escape') {
      e.preventDefault();
      e.stopPropagation();
    }
  };

  document.addEventListener('keydown', preventEscape);
}

// Prevent numeric input for fields with specific regex pattern
document.addEventListener('DOMContentLoaded', function() {
    // Function to check if an input should block numbers
    function shouldBlockNumbers(input) {
        // Check for data-val-regex-pattern="^[^0-9]*$"
        // Note: The attribute value might be HTML encoded or slightly different depending on how it's rendered.
        // We check if the pattern explicitly forbids numbers.
        const pattern = input.getAttribute('data-val-regex-pattern');
        return pattern && (pattern === '^[^0-9]*$' || pattern.indexOf('^[^0-9]*$') !== -1);
    }

    document.body.addEventListener('keydown', function(e) {
        if (e.target.tagName === 'INPUT' && shouldBlockNumbers(e.target)) {
            // Allow navigation keys, backspace, delete, tab, etc.
            // 46: Delete, 8: Backspace, 9: Tab, 27: Esc, 13: Enter
            // 35: End, 36: Home, 37: Left, 38: Up, 39: Right, 40: Down
            if ([46, 8, 9, 27, 13].indexOf(e.keyCode) !== -1 ||
                // Allow: Ctrl+A, Command+A, Ctrl+C, Ctrl+V, Ctrl+X
                (e.ctrlKey === true || e.metaKey === true) ||
                // Allow: home, end, left, right, down, up
                (e.keyCode >= 35 && e.keyCode <= 40)) {
                return;
            }
            
            // Check if it is a number key
            // Top row numbers: 48-57
            // Numpad numbers: 96-105
            var isNumber = (e.keyCode >= 48 && e.keyCode <= 57) || (e.keyCode >= 96 && e.keyCode <= 105);
            
            // If shift is pressed with top row numbers, it's a symbol (e.g. !@#$%), so allow it?
            // The regex ^[^0-9]*$ allows symbols. So we only block if it IS a number digit.
            // Shift + Top Row Number = Symbol.
            // No Shift + Top Row Number = Digit.
            // Numpad numbers are always digits (unless NumLock is off, but keyCode handles that differently usually).
            
            if (isNumber && !e.shiftKey) {
                e.preventDefault();
            }
        }
    });
    
    // Also handle paste events to strip numbers
    document.body.addEventListener('paste', function(e) {
        if (e.target.tagName === 'INPUT' && shouldBlockNumbers(e.target)) {
            // Get pasted data via clipboard API
            var clipboardData = e.clipboardData || window.clipboardData;
            var pastedData = clipboardData.getData('Text');
            
            // Check if pasted data contains numbers
            if (/[0-9]/.test(pastedData)) {
                e.preventDefault();
                // Strip numbers and insert the rest
                var cleanData = pastedData.replace(/[0-9]/g, '');
                document.execCommand('insertText', false, cleanData);
            }
        }
    });
});

document.addEventListener('DOMContentLoaded', () => {
    const INACTIVE_TITLE_MESSAGE = "Dijitaldesin ile Büyümeye Devam Et!";
    let originalTitle = document.title;
    let isTitleInActiveState = false;

    document.addEventListener('visibilitychange', () => {
        if (document.hidden) {
            if (document.title !== INACTIVE_TITLE_MESSAGE) {
                originalTitle = document.title;
            }
            document.title = INACTIVE_TITLE_MESSAGE;
            isTitleInActiveState = true;
        } else {
            if (originalTitle === INACTIVE_TITLE_MESSAGE) {
                 originalTitle = "PushDashboard | Dijitaldesin E-Ticaret Büyüme Platformu";
            }
            
            document.title = originalTitle;
            isTitleInActiveState = false;
        }
    });

    const titleElement = document.querySelector('title');
    if (titleElement) {
        const observer = new MutationObserver((mutations) => {
            if (document.title === INACTIVE_TITLE_MESSAGE && isTitleInActiveState) return;

            if (!isTitleInActiveState) {
                originalTitle = document.title;
            } else {
                if (document.title !== INACTIVE_TITLE_MESSAGE) {
                    originalTitle = document.title;
                    document.title = INACTIVE_TITLE_MESSAGE; 
                }
            }
        });

        observer.observe(titleElement, { childList: true, characterData: true, subtree: true });
    }
});
