/**
 * Pushonica Social Proof Module
 * Sosyal kanıt bildirimleri modülü
 * R2'den config çeker ve popup gösterir
 */

(function () {
    'use strict';

    class SocialProofModule {
        constructor(options = {}) {
            this.options = options;
            this.companyId = options.companyId || this.getCompanyId();
            this.r2BaseUrl = "https://assets.pushonica.com";
            this.config = options.config || null;
            this.isInitialized = false;
            this.notificationQueue = [];
            this.isShowingNotification = false;
            this.intervalId = null;
            this.targetElement = options.targetElement || document.body;
            this.styleId = options.styleId || 'pushonica-social-proof-styles';
            this.containerId = options.containerId || 'pushonica-social-proof';

            if (this.companyId || this.config) {
                if (!options.manualInit) {
                    this.init();
                }
            } else {
                console.warn('[SocialProof] ❌ Company ID bulunamadı');
            }
        }

        async init() {
            if (this.isInitialized) return;

            try {
                if (!this.config) {
                    await this.loadConfig();
                }

                if (!this.config || !this.config.isActive) {
                    console.error('[SocialProof] ❌ Config bulunamadı veya modül aktif değil');
                    return;
                }

                this.injectStyles();
                this.startNotifications();
                this.isInitialized = true;
            } catch (error) {
                console.error('[SocialProof] ❌ Başlatma hatası:', error);
            }
        }

        async loadConfig() {
            try {
                const configUrl = `${this.r2BaseUrl}/${this.companyId}/socialProof.json`;
                console.log('[SocialProof] 📡 Config URL:', configUrl);

                const response = await fetch(configUrl);
                if (!response.ok) throw new Error(`HTTP ${response.status}`);

                this.config = await response.json();
                this.config = this.config.config || this.config;
            } catch (error) {
                console.warn('[SocialProof] ❌ Config yükleme hatası:', error.message);
                this.config = null;
            }
        }

        updateConfig(config) {
            this.config = { ...this.config, ...config };
            this.injectStyles();
            
            // If we want to update the current notification text immediately:
            if (this.isShowingNotification && this.options.livePreview) {
                 // Remove current and show new one immediately
                 const notification = this.targetElement.querySelector(`#${this.containerId}`);
                 if (notification) notification.remove();
                 this.isShowingNotification = false;
                 this.showRandomNotification();
            }
        }

        startNotifications() {
            // İlk bildirimi göster
            setTimeout(() => this.showRandomNotification(), 3000);

            // Periyodik bildirimler
            const interval = this.config.updateInterval || 60000;
            this.intervalId = setInterval(() => this.showRandomNotification(), interval);
        }

        showRandomNotification() {
            if (this.isShowingNotification && !this.options.livePreview) return;

            const types = [];
            if (this.config.isViewersActive !== false) types.push('viewers');
            if (this.config.isFollowersActive !== false) types.push('followers');
            if (this.config.isBuyersActive !== false) types.push('buyers');

            if (types.length === 0) return;

            const type = types[Math.floor(Math.random() * types.length)];

            this.showNotification(type);
        }

        showNotification(type) {
            this.isShowingNotification = true;

            const count = this.getRandomCount(type);
            const message = this.getMessage(type, count);
            const icon = this.getIcon(type);

            const existingNotification = this.targetElement.querySelector(`#${this.containerId}`);
            if (existingNotification) existingNotification.remove();

            const notification = document.createElement('div');
            notification.id = this.containerId;
            notification.innerHTML = `
                <div class="social-proof-content">
                    <span class="social-proof-icon">${icon}</span>
                    <span class="social-proof-message">${message}</span>
                </div>
            `;

            this.targetElement.appendChild(notification);

            setTimeout(() => notification.classList.add('show'), 10);

            if (!this.config.stayVisible) {
                const duration = this.config.displayDuration || 5000;
                setTimeout(() => {
                    notification.classList.remove('show');
                    setTimeout(() => {
                        notification.remove();
                        this.isShowingNotification = false;
                    }, 300);
                }, duration);
            }
        }

        getRandomCount(type) {
            let min, max;
            switch (type) {
                case 'viewers':
                    min = this.config.viewersMin || 15;
                    max = this.config.viewersMax || 45;
                    break;
                case 'followers':
                    min = this.config.followersMin || 5;
                    max = this.config.followersMax || 20;
                    break;
                case 'buyers':
                    min = this.config.buyersMin || 2;
                    max = this.config.buyersMax || 8;
                    break;
                default:
                    min = 5; max = 20;
            }
            return Math.floor(Math.random() * (max - min + 1)) + min;
        }

        getMessage(type, count) {
            const templates = this.config.textTemplates || {};
            let template;
            switch (type) {
                case 'viewers':
                    template = templates.viewersTemplate || '{count} kişi şu anda bu ürünü inceliyor';
                    break;
                case 'followers':
                    template = templates.followersTemplate || '{count} kişi bu ürünü takip ediyor';
                    break;
                case 'buyers':
                    template = templates.buyersTemplate || '{count} kişi satın almaya hazırlanıyor';
                    break;
            }
            return template.replace('{count}', count);
        }

        getIcon(type) {
            switch (type) {
                case 'viewers':
                    return '<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path><circle cx="12" cy="12" r="3"></circle></svg>';
                case 'followers':
                    return '<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path></svg>';
                case 'buyers':
                    return '<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="9" cy="21" r="1"></circle><circle cx="20" cy="21" r="1"></circle><path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path></svg>';
                default:
                    return '<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="20" x2="18" y2="10"></line><line x1="12" y1="20" x2="12" y2="4"></line><line x1="6" y1="20" x2="6" y2="14"></line></svg>';
            }
        }

        // Utility methods
        getCompanyId() {
            const scripts = document.querySelectorAll('script[data-module-id]');
            for (let script of scripts) {
                const id = script.getAttribute('data-module-id');
                if (id) return id;
            }

            return null;
        }

        getR2BaseUrl() {
            return window.PUSHONICA_R2_URL || 'https://pub-99389ce4adeb4856bc9e14750f2fa16f.r2.dev';
        }

        injectStyles() {
            if (document.getElementById(this.styleId)) {
                document.getElementById(this.styleId).remove();
            }

            const ds = this.config.displaySettings || {};
            const pos = ds.position || 'bottom-left';
            const [vertical, horizontal] = pos.split('-');

            const primaryColor = ds.primaryColor || '#007bff';
            const textColor = ds.textColor || '#ffffff';
            const backgroundColor = ds.backgroundColor || '#333333';
            const borderRadius = ds.borderRadius || 8;
            const fontSize = ds.fontSize || 14;
            const enableShadow = ds.enableShadow !== false;
            const enableAnimation = ds.enableAnimation !== false;

            const positionStrategy = this.targetElement === document.body ? 'fixed' : 'absolute';

            const styles = document.createElement('style');
            styles.id = this.styleId;
            styles.textContent = `
                #${this.containerId} {
                    position: ${positionStrategy};
                    ${vertical === 'top' ? 'top: 20px;' : 'bottom: 20px;'}
                    ${horizontal === 'right' ? 'right: 20px;' : 'left: 20px;'}
                    /* Background is handled by ::after mask */
                    color: ${textColor};
                    border-radius: ${borderRadius}px;
                    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
                    font-size: ${fontSize}px;
                    z-index: 99998;
                    transform: translateY(${vertical === 'top' ? '-20px' : '20px'});
                    opacity: 0;
                    transition: opacity 0.4s ease, transform 0.4s ease;
                    ${enableShadow ? 'box-shadow: 0 8px 30px rgba(0,0,0,0.3);' : ''}
                    display: flex;
                    align-items: center;
                    max-width: 350px;
                    pointer-events: none;
                    overflow: hidden;
                    box-sizing: border-box;
                }
                #${this.containerId}.show {
                    transform: translateY(0);
                    opacity: 1;
                    pointer-events: auto;
                }
                
                /* Rotating Border Gradient */
                ${enableAnimation ? `
                #${this.containerId}::before {
                    content: '';
                    position: absolute;
                    z-index: -2;
                    left: 50%;
                    top: 50%;
                    width: 1000px;
                    height: 1000px;
                    background-repeat: no-repeat;
                    background-position: 0 0;
                    background-image: conic-gradient(transparent, ${primaryColor}, transparent 50%);
                    animation: rotate 3.5s linear infinite;
                }
                ` : ''}

                /* Inner Background Mask */
                #${this.containerId}::after {
                    content: '';
                    position: absolute;
                    z-index: -1;
                    left: 3px;
                    top: 3px;
                    width: calc(100% - 6px);
                    height: calc(100% - 6px);
                    background: ${backgroundColor};
                    border-radius: calc(${borderRadius}px - 3px);
                }

                .social-proof-content {
                    position: relative;
                    z-index: 1;
                    display: flex;
                    align-items: center;
                    gap: 12px;
                    width: 100%;
                    padding: 16px 22px;
                }
                .social-proof-icon {
                    font-size: 24px;
                    color: ${primaryColor};
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    min-width: 24px;
                    ${enableAnimation ? 'animation: icon-pulse 2s infinite ease-in-out;' : ''}
                }
                .social-proof-message {
                    font-weight: 600;
                    line-height: 1.4;
                    flex: 1;
                }
                
                @keyframes rotate {
                    0% {
                        transform: translate(-50%, -50%) rotate(0deg);
                    }
                    100% {
                        transform: translate(-50%, -50%) rotate(360deg);
                    }
                }

                @keyframes icon-pulse {
                    0%, 100% { transform: scale(1); opacity: 1; }
                    50% { transform: scale(1.1); opacity: 0.8; }
                }
            `;
            document.head.appendChild(styles);
        }

        destroy() {
            if (this.intervalId) {
                clearInterval(this.intervalId);
                this.intervalId = null;
            }
            const notification = this.targetElement.querySelector(`#${this.containerId}`);
            if (notification) notification.remove();
            
            const styles = document.getElementById(this.styleId);
            if (styles) styles.remove();

            this.isInitialized = false;
        }
    }

    // Global instance
    window.SocialProofModule = SocialProofModule;
    window.pushonicaSocialProof = new SocialProofModule();
})();
