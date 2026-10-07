/**
 * BusyIndicator Component
 * Handles Inline, Overlay, and Fullscreen loading states.
 */
class BusyIndicator {
    /**
     * @param {HTMLElement|String} element - The target element (parent for overlay/inline). Pass null/undefined for fullscreen.
     * @param {Object} options - Configuration options.
     */
    constructor(element, options = {}) {
        this.element = typeof element === 'string' ? document.querySelector(element) : element;
        
        // Default Options
        const defaults = {
            active: false,
            variant: 'spinner', // 'spinner' (inline), 'overlay', 'fullscreen'
            text: null,
            size: 'md',   // 'sm', 'md', 'lg'
            color: 'primary', // 'primary', 'white', 'gray'
            delay: 200    // ms
        };

        this.options = { ...defaults, ...options };
        
        // Internal state
        this.timer = null;
        this.isActive = false;
        this.indicatorEl = null;

        // If fullscreen, forcing element to body if not provided
        if (this.options.variant === 'fullscreen') {
            this.element = document.body;
        }

        if (this.options.active) {
            this.show();
        }
    }

    /**
     * Shows the busy indicator.
     */
    show() {
        if (this.isActive) return;

        if (this.options.delay > 0) {
            this.timer = setTimeout(() => {
                this._render();
            }, this.options.delay);
        } else {
            this._render();
        }
        
        this.isActive = true;
    }

    /**
     * Hides the busy indicator.
     */
    hide() {
        if (!this.isActive) return;

        if (this.timer) {
            clearTimeout(this.timer);
            this.timer = null;
        }

        this._remove();
        this.isActive = false;
    }

    /**
     * Renders the DOM elements.
     * @private
     */
    _render() {
        if (this.indicatorEl) return; // Already rendered

        // Create Container
        const container = document.createElement('div');
        
        // Accessibility
        container.setAttribute('role', 'status'); // or progressbar
        container.setAttribute('aria-busy', 'true');
        container.setAttribute('aria-live', 'polite');

        // Classes based on variant
        if (this.options.variant === 'fullscreen') {
            container.classList.add('busy-indicator-fullscreen');
            document.body.style.overflow = 'hidden'; // Lock scroll
        } else if (this.options.variant === 'overlay') {
            container.classList.add('busy-indicator-overlay');
            if (this.element) {
                // Ensure parent position is relative or absolute or fixed so overlay covers it
                const computedStyle = window.getComputedStyle(this.element);
                if (computedStyle.position === 'static') {
                    this.element.classList.add('busy-relative');
                }
            }
        } else {
            // Inline / Spinner
            container.classList.add('busy-indicator-inline');
        }

        // Create Spinner
        const spinner = document.createElement('div');
        spinner.classList.add('busy-spinner');
        spinner.classList.add(`busy-spinner-${this.options.size}`);
        spinner.classList.add(`busy-spinner-${this.options.color}`);
        
        // Accessibility hidden text for spinner
        const screenReaderText = document.createElement('span');
        screenReaderText.classList.add('busy-visually-hidden');
        screenReaderText.textContent = this.options.text || 'Loading...';
        container.appendChild(screenReaderText);

        container.appendChild(spinner);

        // Visible Text
        if (this.options.text) {
            const textEl = document.createElement('div');
            textEl.classList.add('busy-text');
            textEl.textContent = this.options.text;
            container.appendChild(textEl);
        }

        this.indicatorEl = container;

        // Append to DOM
        if (this.options.variant === 'fullscreen') {
            document.body.appendChild(this.indicatorEl);
        } else if (this.element) {
            this.element.appendChild(this.indicatorEl);
            // If inline and we want to disable the button/input
            if (this.options.variant === 'spinner' && (this.element.tagName === 'BUTTON' || this.element.tagName === 'A')) {
                 this.element.classList.add('disabled');
                 this.element.setAttribute('disabled', 'disabled');
            }
        } else {
            console.warn('BusyIndicator: No element provided for inline/overlay mode.');
        }
    }

    /**
     * Removes DOM elements.
     * @private
     */
    _remove() {
        if (!this.indicatorEl) return;

        if (this.indicatorEl.parentNode) {
            this.indicatorEl.parentNode.removeChild(this.indicatorEl);
        }

        if (this.options.variant === 'fullscreen') {
            document.body.style.overflow = ''; // Restore scroll
        } else if (this.options.variant === 'overlay' && this.element) {
             this.element.classList.remove('busy-relative');
        } else if (this.options.variant === 'spinner' && this.element) {
             if (this.element.tagName === 'BUTTON' || this.element.tagName === 'A') {
                 this.element.classList.remove('disabled');
                 this.element.removeAttribute('disabled');
            }
        }

        this.indicatorEl = null;
    }
    
    /**
     * Updates text or other options dynamically.
     */
    update(options = {}) {
        const wasActive = this.isActive;
        if(wasActive) this.hide();
        this.options = { ...this.options, ...options };
        if(wasActive) this.show();
    }

    // --- Static Helper Methods for Global Usage ---

    static _globalInstance = null;

    /**
     * Shows a busy indicator.
     * Use param `element` to target a specific container (overlay/inline).
     * Pass `null` or omit `element` to show the global Fullscreen loader.
     * 
     * @param {HTMLElement|String|null} element - Target element.
     * @param {Object} options - Options (text, color, size, etc.)
     * @returns {BusyIndicator} The instance
     */
    static show(element, options = {}) {
        // Global / Fullscreen
        if (!element) {
            if (!this._globalInstance) {
                this._globalInstance = new BusyIndicator(null, { 
                    variant: 'fullscreen', 
                    text: 'Lütfen bekleyin...',
                    ...options 
                });
            } else {
                this._globalInstance.update(options);
            }
            this._globalInstance.show();
            return this._globalInstance;
        }

        // Element-bound (Overlay/Inline)
        const el = typeof element === 'string' ? document.querySelector(element) : element;
        if (!el) {
            console.warn('BusyIndicator: Target element not found:', element);
            return null;
        }

        // Check if instance already exists on element
        let instance = el.busyIndicatorInstance;
        if (!instance) {
            instance = new BusyIndicator(el, options);
            el.busyIndicatorInstance = instance;
        } else {
            instance.update(options);
        }
        
        instance.show();
        return instance;
    }

    /**
     * Hides the busy indicator for the given element (or global if null).
     * @param {HTMLElement|String|null} element 
     */
    static hide(element) {
        if (!element) {
            if (this._globalInstance) this._globalInstance.hide();
            return;
        }

        const el = typeof element === 'string' ? document.querySelector(element) : element;
        if (el && el.busyIndicatorInstance) {
            el.busyIndicatorInstance.hide();
        }
    }
}

// Global Helper for ease of use
window.BusyIndicator = BusyIndicator;
