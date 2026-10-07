/**
 * PushDialog - A reusable, accessible, and customizable dialog component.
 * 
 * Usage:
 * const myDialog = new PushDialog({
 *   title: 'My Dialog',
 *   content: '<p>Hello World</p>',
 *   footer: '<button>OK</button>',
 *   onClose: () => console.log('Closed')
 * });
 * myDialog.open();
 */
class PushDialog {
    constructor(options = {}) {
        // 1. Temel Yapı ve Parametreler (Props)
        this.isOpen = options.isOpen || false;
        this.onClose = options.onClose || (() => {});
        this.title = options.title || null;
        this.content = options.content || ''; // String (HTML) or Node
        this.footer = options.footer || null; // String (HTML) or Node

        // 1.1 Form Odaklı Yeni Parametreler
        this.isLoading = options.isLoading || false; // isSubmitting
        this.stickyFooter = options.stickyFooter !== undefined ? options.stickyFooter : true; 

        // 2. Davranışsal Parametreler
        this.closeOnOverlayClick = options.closeOnOverlayClick !== undefined ? options.closeOnOverlayClick : true;
        this.closeOnEsc = options.closeOnEsc !== undefined ? options.closeOnEsc : true;
        this.showCloseIcon = options.showCloseIcon !== undefined ? options.showCloseIcon : true;
        this.disableScroll = options.disableScroll !== undefined ? options.disableScroll : true;
        this.preventFocus = options.preventFocus !== undefined ? options.preventFocus : false;
        
        // 2.1 Z-Index Override
        this.zIndex = options.zIndex || null;

        // 3. Görsel ve Stil Parametreleri
        this.size = options.size || 'md'; // xs, sm, md, lg, xl, full, 2xl, 3xl, 4xl, 5xl
        this.position = options.position || 'center'; // center, top, bottom
        
        // If stickyFooter is true, force 'inside' scroll behavior for the body
        this.scrollBehavior = this.stickyFooter ? 'inside' : (options.scrollBehavior || 'inside'); 
        this.backdropBlur = options.backdropBlur || false;

        // Internal State
        this.dialogElement = null;
        this.panelElement = null;
        this.focusableElements = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';
        this.previousActiveElement = null;

        // Binding methods
        this.handleKeyDown = this.handleKeyDown.bind(this);
        this.handleOverlayClick = this.handleOverlayClick.bind(this);
        this.close = this.close.bind(this);
        this.setLoading = this.setLoading.bind(this);

        // Auto-open if requested
        if (this.isOpen) {
            this.open();
        }
    }

    /**
     * Creates the DOM structure for the dialog
     */
    _render() {
        // Ensure portal root exists
        let portalRoot = document.getElementById('modal-root');
        if (!portalRoot) {
            portalRoot = document.createElement('div');
            portalRoot.id = 'modal-root';
            document.body.appendChild(portalRoot);
        }

        // Wrapper (Overlay)
        const overlay = document.createElement('div');
        overlay.setAttribute('role', 'dialog');
        overlay.setAttribute('aria-modal', 'true');
        if (this.title) {
            overlay.setAttribute('aria-labelledby', 'push-dialog-title');
        }
        
        // CSS Classes for Overlay
        const overlayClasses = [
            'fixed', 'inset-0', 'z-50', 'flex', 
            'transition-opacity', 'duration-300', 'ease-out', // Animation base
            'opacity-0' // Start hidden for animation
        ];

        // Position logic
        if (this.position === 'center') {
            overlayClasses.push('items-center', 'justify-center');
        } else if (this.position === 'top') {
            overlayClasses.push('items-start', 'justify-center', 'pt-10');
        } else if (this.position === 'bottom') {
            overlayClasses.push('items-end', 'justify-center', 'pb-0', 'sm:pb-10', 'sm:items-center');
        }

        // Scroll behavior logic (Outside scroll)
        // If we want sticky footer, we MUST NOT scroll the overlay, we scroll the body.
        if (this.scrollBehavior === 'outside') {
            overlayClasses.push('overflow-y-auto');
        } else {
            overlayClasses.push('overflow-hidden');
        }

        // Backdrop visual
        const backdropClass = this.backdropBlur ? 'backdrop-blur-sm bg-gray-900/60' : 'bg-gray-900/50';
        overlay.className = `${overlayClasses.join(' ')} ${backdropClass}`;
        
        if (this.zIndex) {
            overlay.style.zIndex = this.zIndex;
        }

        // The Dialog Panel Container
        const panel = document.createElement('div');
        
        // Size logic (Tailwind max-width)
        const sizeClasses = {
            'xs': 'max-w-xs',
            'sm': 'max-w-sm',
            'md': 'max-w-md',
            'lg': 'max-w-lg',
            'xl': 'max-w-xl',
            '2xl': 'max-w-2xl',
            '3xl': 'max-w-3xl',
            '4xl': 'max-w-4xl',
            '5xl': 'max-w-5xl',
            'full': 'max-w-full m-4 h-[calc(100%-2rem)]'
        };
        const widthClass = sizeClasses[this.size] || sizeClasses['md'];

        const panelClasses = [
            'relative', 'w-full', 'bg-white', 'rounded-xl', 'shadow-[0_8px_30px_rgba(0,0,0,0.08)]', 'border', 'border-gray-200', 
            'transform', 'transition-all', 'duration-300', 'ease-out', // Animation base
            'scale-95', 'opacity-0', // Start state for animation
            'flex', 'flex-col', // Flex Column for sticky header/footer
            widthClass
        ];

        if (this.position === 'bottom' && window.innerWidth < 640) {
             panelClasses.push('rounded-b-none', 'sm:rounded-xl'); 
        }

        if (this.scrollBehavior === 'inside') {
            panelClasses.push('max-h-[90vh]'); // Limit height to allow internal scroll
        }

        panel.className = panelClasses.join(' ');
        
        // Prevent click bubbling from panel to overlay

        // Header (Sticky: flex-shrink-0)
        if (this.title || this.showCloseIcon) {
            const header = document.createElement('div');
            header.className = 'flex items-center justify-between px-6 py-4 border-b border-gray-200 shrink-0';
            
            if (this.title) {
                const titleEl = document.createElement('h3');
                titleEl.id = 'push-dialog-title';
                titleEl.className = 'text-lg font-semibold text-gray-900 leading-6';
                
                if (typeof this.title === 'string') {
                    // Try to detect if it's HTML string (contains tags) or plain text
                    if (this.title.trim().startsWith('<')) {
                        titleEl.innerHTML = this.title;
                    } else {
                        titleEl.innerText = this.title;
                    }
                } else {
                    titleEl.appendChild(this.title);
                }
                header.appendChild(titleEl);
            } else {
                // Even without a title, create the element so setTitle() works later if user sets it dynamically
                const titleEl = document.createElement('h3');
                titleEl.id = 'push-dialog-title';
                titleEl.className = 'text-lg font-semibold text-gray-900 leading-6';
                header.appendChild(titleEl);
            }

            if (this.showCloseIcon) {
                const closeBtn = document.createElement('button');
                closeBtn.type = 'button';
                closeBtn.className = 'text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary rounded-lg p-1 transition-colors';
                closeBtn.innerHTML = `
                    <span class="sr-only">Kapat</span>
                    <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                `;
                closeBtn.onclick = this.close;
                header.appendChild(closeBtn);
            }
            panel.appendChild(header);
        }

        // Body (Scrollable: flex-grow-1, overflow-y-auto)
        const body = document.createElement('div');
        // 'grow' allows it to take available space, 'overflow-y-auto' makes it scroll
        const bodyScrollClass = this.scrollBehavior === 'inside' ? 'overflow-y-auto grow min-h-0' : '';
        body.className = `px-6 py-4 text-left ${bodyScrollClass}`;
        
        if (typeof this.content === 'string') {
            body.innerHTML = this.content;
        } else if (this.content instanceof Node) {
            body.appendChild(this.content);
        }
        panel.appendChild(body);

        // Footer (Sticky: flex-shrink-0)
        if (this.footer) {
            const footer = document.createElement('div');
            footer.className = 'bg-gray-50 px-6 py-3 flex flex-col sm:flex-row-reverse gap-2 sm:gap-3 rounded-b-xl border-t border-gray-200 shrink-0';
            
            if (typeof this.footer === 'string') {
                footer.innerHTML = this.footer;
            } else if (this.footer instanceof Node) {
                footer.appendChild(this.footer);
            }
            panel.appendChild(footer);
        }

        overlay.appendChild(panel);
        
        // Event Listeners
        if (this.closeOnOverlayClick) {
            overlay.addEventListener('click', this.handleOverlayClick);
        }

        this.dialogElement = overlay;
        this.panelElement = panel;
        
        // Initial Loading State check
        if (this.isLoading) {
            this.setLoading(true);
        }

        // Append to Portal (usually body or #modal-root)
        portalRoot.appendChild(overlay);
    }

    /**
     * Opens the dialog with animations and focus trap
     */
    open() {
        if (document.getElementById('modal-root')?.contains(this.dialogElement)) {
            return; // Already open
        }

        // Store active element to restore focus later
        this.previousActiveElement = document.activeElement;

        this._render();

        // Prevent body scroll
        if (this.disableScroll) {
            document.body.classList.add('overflow-hidden');
        }

        // Animation: Wait a tick for DOM paint, then apply active classes
        requestAnimationFrame(() => {
            this.dialogElement.classList.remove('opacity-0');
            this.panelElement.classList.remove('scale-95', 'opacity-0');
            this.panelElement.classList.add('scale-100', 'opacity-100');
        });

        // Event Listeners
        if (this.closeOnEsc) {
            window.addEventListener('keydown', this.handleKeyDown);
        }

        // Focus Trap
        if (!this.preventFocus) {
            // Find first focusable element or focus the panel itself
            const focusable = this.panelElement.querySelectorAll(this.focusableElements);
            if (focusable.length > 0) {
                focusable[0].focus();
            } else {
                this.panelElement.setAttribute('tabindex', '-1');
                this.panelElement.focus();
            }
            
            // Trap loop listener
            this.dialogElement.addEventListener('keydown', this._trapFocus.bind(this));
        }
        
        this.isOpen = true;
    }

    /**
     * Closes the dialog
     */
    close(e) {
        if (e) e.preventDefault(); // If triggered by button click
        
        // Prevent closing if loading/submitting
        if (this.isLoading) return;

        if (!this.dialogElement) return;

        // Animation Out
        this.dialogElement.classList.add('opacity-0');
        this.panelElement.classList.remove('scale-100', 'opacity-100');
        this.panelElement.classList.add('scale-95', 'opacity-0');

        // Cleanup after animation
        setTimeout(() => {
             // Remove from DOM
             if (this.dialogElement && this.dialogElement.parentNode) {
                 this.dialogElement.parentNode.removeChild(this.dialogElement);
             }
             this.dialogElement = null;
             this.panelElement = null;

             // Restore body scroll
             if (this.disableScroll) {
                 document.body.classList.remove('overflow-hidden');
             }

             // Restore Focus
             if (this.previousActiveElement) {
                 this.previousActiveElement.focus();
             }

             // Cleanup Events
             window.removeEventListener('keydown', this.handleKeyDown);
             
             this.isOpen = false;
             if (this.onClose) this.onClose();

        }, 300); // Matches duration-300
    }

    handleKeyDown(e) {
        if (this.isLoading && e.key === 'Escape') return; // Prevent Escape if loading
        if (this.closeOnEsc && e.key === 'Escape') {
            this.close();
        }
    }

    handleOverlayClick(e) {
        if (this.isLoading) return; // Prevent closing while loading
        if (this.closeOnOverlayClick && e.target === this.dialogElement) {
            this.close();
        }
    }

    _trapFocus(e) {
        if (e.key !== 'Tab') return;

        const focusableContent = this.panelElement.querySelectorAll(this.focusableElements);
        const firstElement = focusableContent[0];
        const lastElement = focusableContent[focusableContent.length - 1];

        if (e.shiftKey) { // Shift + Tab
            if (document.activeElement === firstElement) {
                lastElement.focus();
                e.preventDefault();
            }
        } else { // Tab
            if (document.activeElement === lastElement) {
                firstElement.focus();
                e.preventDefault();
            }
        }
    }

    /**
     * Controls the loading state of the dialog (e.g. during form submission)
     * @param {boolean} loadingState 
     */
    setLoading(loadingState) {
        this.isLoading = loadingState;
        
        if (!this.panelElement) return;

        if (loadingState) {
            BusyIndicator.show(this.panelElement, { 
                variant: 'overlay', 
                text: 'İşlem yapılıyor...', 
                color: 'primary',
                size: 'lg'
            });
        } else {
            BusyIndicator.hide(this.panelElement);
        }
    }

    /**
     * Helper to update content dynamically
     */
    setContent(newContent) {
        this.content = newContent;
        if (this.panelElement) {
             let bodyEl = null;
             for (let i = 0; i < this.panelElement.children.length; i++) {
                 const child = this.panelElement.children[i];
                 if (!child.classList.contains('border-b') && !child.classList.contains('border-t')) {
                     bodyEl = child;
                     break;
                 }
             }

             if (bodyEl) {
                 if (typeof newContent === 'string') {
                    bodyEl.innerHTML = newContent;
                } else {
                    bodyEl.innerHTML = '';
                    bodyEl.appendChild(newContent);
                }
             }
        }
    }

    /**
     * Helper to update title dynamically
     */
    setTitle(newTitle) {
        this.title = newTitle;
        if (this.panelElement) {
            const titleEl = this.panelElement.querySelector('#push-dialog-title');
            if (titleEl) {
                if (typeof newTitle === 'string') {
                    // Try to detect if it's HTML string (contains tags) or plain text
                    // Simplistic check: contains <
                    if (newTitle.trim().startsWith('<')) {
                        titleEl.innerHTML = newTitle;
                    } else {
                        titleEl.innerText = newTitle;
                    }
                } else if (newTitle instanceof Node) {
                    titleEl.innerHTML = '';
                    titleEl.appendChild(newTitle);
                }
            }
        }
    }

    /**
     * Helper to update footer dynamically
     */
    setFooter(newFooter) {
        this.footer = newFooter;
        if (!this.panelElement) return;

        // Try to find existing footer
        let footerEl = null;

        // Look for the element with the footer classes
        // Note: This relies on the class list being consistent with _render
        const children = this.panelElement.children;
        for (let i = 0; i < children.length; i++) {
           if (children[i].classList.contains('bg-gray-50') && children[i].classList.contains('rounded-b-xl')) {
               footerEl = children[i];
               break;
           }
        }

        if (newFooter) {
            // If footer doesn't exist, create it
            if (!footerEl) {
                footerEl = document.createElement('div');
                // Updated classes for better consistency: flex col on mobile, row-reverse on desktop, gap handling spacing
                footerEl.className = 'bg-gray-50 px-6 py-3 flex flex-col sm:flex-row-reverse gap-2 sm:gap-3 rounded-b-xl border-t border-gray-200 shrink-0';
                this.panelElement.appendChild(footerEl);
            }

            // Update content
            if (typeof newFooter === 'string') {
                footerEl.innerHTML = newFooter;
            } else if (newFooter instanceof Node) {
                footerEl.innerHTML = '';
                footerEl.appendChild(newFooter);
            }
            // Ensure visible (in case it was hidden or something, though not applicable here)
        } else {
            // Remove footer if passed null/empty
            if (footerEl) {
                footerEl.remove();
            }
        }
    }
}

// Make it globally available
window.PushDialog = PushDialog;
