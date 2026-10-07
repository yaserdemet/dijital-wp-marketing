/**
 * Pushonica Cookie Management Module — 

 */
(function () {
    'use strict';

    /* ─────────── helpers ─────────── */
    const esc = (s) => {
        if (!s) return '';
        const d = document.createElement('div');
        d.textContent = s;
        return d.innerHTML;
    };

    const uid = () => 'cm_' + Date.now() + '_' + Math.random().toString(36).slice(2, 9);

    const STYLE_ID = 'pushonica-cookie-mgmt-styles';

    class PushonicaCookieManagement {
        constructor(opts = {}) {
            this.options = opts;
            this.companyId = opts.companyId || this.getCompanyId();
            this.apiBaseUrl = opts.apiBaseUrl || 'https://pushadmin-production.up.railway.app';
            this.r2BaseUrl = opts.r2BaseUrl || 'https://assets.pushonica.com';
            this.livePreview = opts.livePreview || false;
            this.targetElement = opts.targetElement || document.body;
            this.config = null;
            this.categories = [];
            this.containerId = 'cookieBanner_' + uid();
            this.modalId = 'cookieModal_' + uid();
            this.cookieKey = `pushonica_cookie_consent_${this.companyId}`;
            this.isInitialized = false;
            this.isBannerVisible = false;
            this.isModalOpen = false;
            this.sessionId = uid();
            this.trackUrl = '';

            if (opts.config) {
                this._parseConfig(opts.config);
            }

            if (this.companyId || this.config) {
                if (!opts.manualInit) {
                    this.init();
                }
            }
        }

        getCompanyId() {
            const scripts = document.querySelectorAll('script[data-module-id]');
            for (let script of scripts) {
                const id = script.getAttribute('data-module-id');
                if (id) return id;
            }
            const meta = document.querySelector('meta[name="pushonica-company-id"]');
            if (meta) return meta.getAttribute('content');
            return window.PUSHONICA_COMPANY_ID || null;
        }

        async loadConfigFromR2() {
            try {
                const configUrl = `${this.r2BaseUrl}/${this.companyId}/cookieManagement.json`;
                const response = await fetch(configUrl);
                if (!response.ok) throw new Error(`HTTP ${response.status}`);
                const data = await response.json();
                return data;
            } catch (error) {
                console.warn('[CookieManagement] Config yüklenemedi:', error.message);
                return null;
            }
        }

        updateConfig(config) {
            this._parseConfig(config);
            this._injectStyles();
            if (this.livePreview) {
                this._renderInline();
            }
        }

        _parseConfig(raw) {
            if (!raw) return;

            const c = raw.config || raw.Config || raw;
            const gen = c.general || {};

            this.config = {
                isActive: (c.isActive ?? gen.isActive) !== false,
                bannerTitle: c.bannerTitle || gen.bannerTitle || 'Bu site çerezleri kullanır',
                bannerDescription: c.bannerDescription || gen.bannerDescription || '',
                acceptButtonText: c.acceptButtonText || 'Tümünü Kabul Et',
                rejectButtonText: c.rejectButtonText || 'Tümünü Reddet',
                settingsButtonText: c.settingsButtonText || 'Ayarları Yönet',
                saveButtonText: c.saveButtonText || 'Seçimleri Kaydet',
                bannerPosition: c.bannerPosition || 'bottom',
                bannerBackgroundColor: c.bannerBackgroundColor || '#ffffff',
                bannerTextColor: c.bannerTextColor || '#333333',
                acceptButtonColor: c.acceptButtonColor || '#4CAF50',
                rejectButtonColor: c.rejectButtonColor || '#f44336',
                settingsButtonColor: c.settingsButtonColor || '#2196F3',
                borderRadius: c.borderRadius || '8px',
                showSettingsButton: c.showSettingsButton !== false,
                enableAnimation: c.enableAnimation !== false,
                cookieExpiryDays: c.cookieExpiryDays || 365
            };

            this.trackUrl = this.config.trackUrl || `${this.apiBaseUrl}/api/cookie-management/track`;

            this.categories = (c.categories || []).map(cat => ({
                id: cat.id || cat.Id,
                name: cat.name || cat.Name,
                description: cat.description || cat.Description || '',
                isRequired: cat.isRequired ?? cat.IsRequired ?? false,
                isEnabled: cat.isEnabled ?? cat.IsEnabled ?? false
            }));
        }

        async init() {
            if (this.isInitialized) return;

            try {
                if (!this.config && this.companyId) {
                    const r2Data = await this.loadConfigFromR2();
                    if (r2Data) {
                        this._parseConfig(r2Data);
                    }
                }

                if (!this.config || !this.config.isActive) return;

                this.isInitialized = true;

                if (this.livePreview) {
                    this._renderInline();
                    return;
                }

                // Check existing consent
                if (this._hasConsent()) return;

                this._injectStyles();
                this._showBanner();
                this._sendEvent('banner_view');

            } catch (error) {
                console.error('[CookieManagement] Başlatma hatası:', error);
            }
        }

        /* ─────────── consent storage ─────────── */

        _hasConsent() {
            return this._getCookie(this.cookieKey) !== null;
        }

        _getPreferences() {
            try {
                const data = this._getCookie(this.cookieKey);
                return data ? JSON.parse(decodeURIComponent(data)) : {};
            } catch { return {}; }
        }

        _saveConsent(preferences) {
            const data = encodeURIComponent(JSON.stringify(preferences));
            const expires = new Date(Date.now() + this.config.cookieExpiryDays * 864e5).toUTCString();
            document.cookie = `${this.cookieKey}=${data};expires=${expires};path=/;SameSite=Lax`;

            // Dispatch custom event
            try {
                window.dispatchEvent(new CustomEvent('pushonicaCookieConsent', {
                    detail: { preferences }
                }));
            } catch (e) { /* IE fallback */ }
        }

        _getCookie(name) {
            const value = `; ${document.cookie}`;
            const parts = value.split(`; ${name}=`);
            if (parts.length === 2) return parts.pop().split(';').shift();
            return null;
        }

        /* ─────────── inline preview (admin) ─────────── */

        _renderInline() {
            if (!this.targetElement) return;
            this.targetElement.innerHTML = '';
            this._injectStyles();

            const wrap = document.createElement('div');
            wrap.className = 'pcm-inline-wrap';
            wrap.innerHTML = this._buildBannerHTML();
            this.targetElement.appendChild(wrap);
            this._bindBanner(wrap);
        }

        /* ─────────── banner ─────────── */

        _showBanner() {
            if (this.isBannerVisible) return;
            this.isBannerVisible = true;

            const banner = document.createElement('div');
            banner.id = this.containerId;
            banner.className = 'pcm-banner pcm-pos-' + (this.config.bannerPosition || 'bottom');

            if (this.config.enableAnimation) {
                banner.classList.add('pcm-animate');
            }

            banner.innerHTML = this._buildBannerHTML();
            document.body.appendChild(banner);

            if (this.config.enableAnimation) {
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => banner.classList.add('pcm-visible'));
                });
            } else {
                banner.classList.add('pcm-visible');
            }

            this._bindBanner(banner);
        }

        _buildBannerHTML() {
            const c = this.config;
            return `
                <div class="pcm-banner-inner">
                    <div class="pcm-banner-content">
                        <div class="pcm-banner-text">
                            <h3 class="pcm-banner-title">${esc(c.bannerTitle)}</h3>
                            <p class="pcm-banner-desc">${esc(c.bannerDescription)}</p>
                        </div>
                        <div class="pcm-banner-actions">
                            <button type="button" class="pcm-btn pcm-btn-accept">${esc(c.acceptButtonText)}</button>
                            <button type="button" class="pcm-btn pcm-btn-reject">${esc(c.rejectButtonText)}</button>
                            ${c.showSettingsButton ? `<button type="button" class="pcm-btn pcm-btn-settings">${esc(c.settingsButtonText)}</button>` : ''}
                        </div>
                    </div>
                </div>
            `;
        }

        _bindBanner(root) {
            root.querySelectorAll('.pcm-btn-accept').forEach(b => b.addEventListener('click', () => this._acceptAll()));
            root.querySelectorAll('.pcm-btn-reject').forEach(b => b.addEventListener('click', () => this._rejectAll()));
            root.querySelectorAll('.pcm-btn-settings').forEach(b => b.addEventListener('click', () => this._showSettingsModal()));
        }

        _closeBanner() {
            const banner = document.getElementById(this.containerId);
            if (banner) {
                banner.classList.remove('pcm-visible');
                setTimeout(() => banner.remove(), 350);
            }
            this.isBannerVisible = false;
        }

        /* ─────────── actions ─────────── */

        _acceptAll() {
            const prefs = {};
            this.categories.forEach(cat => { prefs[cat.id] = true; });
            this._saveConsent(prefs);
            this._sendEvent('accept_all', Object.keys(prefs));
            this._closeBanner();
            this._closeSettingsModal();
        }

        _rejectAll() {
            const prefs = {};
            this.categories.forEach(cat => { prefs[cat.id] = cat.isRequired; });
            this._saveConsent(prefs);
            const accepted = Object.entries(prefs).filter(([, v]) => v).map(([k]) => k);
            this._sendEvent('reject_all', accepted);
            this._closeBanner();
            this._closeSettingsModal();
        }

        /* ─────────── settings modal ─────────── */

        _showSettingsModal() {
            if (this.isModalOpen) return;
            this.isModalOpen = true;
            this._sendEvent('open_preferences');

            const modal = document.createElement('div');
            modal.id = this.modalId;
            modal.className = 'pcm-modal';

            modal.innerHTML = `
                <div class="pcm-modal-overlay"></div>
                <div class="pcm-modal-dialog">
                    <button type="button" class="pcm-modal-close" aria-label="Kapat">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                    </button>
                    <h3 class="pcm-modal-title">Çerez Ayarları</h3>
                    <div class="pcm-categories">
                        ${this._buildCategoriesHTML()}
                    </div>
                    <div class="pcm-modal-footer">
                        <button type="button" class="pcm-btn pcm-btn-save">${esc(this.config.saveButtonText)}</button>
                    </div>
                </div>
            `;

            document.body.appendChild(modal);

            requestAnimationFrame(() => {
                requestAnimationFrame(() => modal.classList.add('pcm-modal-visible'));
            });

            this._bindModal(modal);
        }

        _buildCategoriesHTML() {
            const prefs = this._getPreferences();
            let html = '';

            this.categories.forEach(cat => {
                const isEnabled = cat.isRequired || prefs[cat.id] || cat.isEnabled;
                html += `
                    <div class="pcm-category">
                        <div class="pcm-category-header">
                            <div class="pcm-category-info">
                                <span class="pcm-category-name">${esc(cat.name)}</span>
                                ${cat.isRequired ? '<span class="pcm-category-badge">Zorunlu</span>' : ''}
                            </div>
                            <label class="pcm-switch">
                                <input type="checkbox" data-cat-id="${esc(cat.id)}" ${isEnabled ? 'checked' : ''} ${cat.isRequired ? 'disabled' : ''}>
                                <span class="pcm-switch-slider"></span>
                            </label>
                        </div>
                        <p class="pcm-category-desc">${esc(cat.description)}</p>
                    </div>
                `;
            });

            return html;
        }

        _bindModal(modal) {
            modal.querySelector('.pcm-modal-close')?.addEventListener('click', () => this._closeSettingsModal());
            modal.querySelector('.pcm-modal-overlay')?.addEventListener('click', () => this._closeSettingsModal());
            modal.querySelector('.pcm-btn-save')?.addEventListener('click', () => this._saveFromModal(modal));
        }

        _saveFromModal(modal) {
            const prefs = {};
            this.categories.forEach(cat => {
                const checkbox = modal.querySelector(`input[data-cat-id="${cat.id}"]`);
                prefs[cat.id] = checkbox ? checkbox.checked : cat.isRequired;
            });
            this._saveConsent(prefs);
            const accepted = Object.entries(prefs).filter(([, v]) => v).map(([k]) => k);
            this._sendEvent('save_preferences', accepted);
            this._closeSettingsModal();
            this._closeBanner();
        }

        _closeSettingsModal() {
            const modal = document.getElementById(this.modalId);
            if (modal) {
                modal.classList.remove('pcm-modal-visible');
                setTimeout(() => modal.remove(), 350);
            }
            this.isModalOpen = false;
        }

        /* ─────────── styles ─────────── */

        _injectStyles() {
            let existing = document.getElementById(STYLE_ID);
            if (existing) existing.remove();

            const c = this.config;
            const style = document.createElement('style');
            style.id = STYLE_ID;
            style.textContent = `
                /* ───── Banner ───── */
                .pcm-banner {
                    position: fixed;
                    z-index: 999999;
                    font-family: Inter, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                    font-size: 14px;
                    line-height: 1.5;
                }
                .pcm-banner.pcm-animate {
                    transition: transform 0.35s cubic-bezier(0.4, 0, 0.2, 1), opacity 0.35s ease;
                }
                /* Positions */
                .pcm-pos-bottom { bottom: 0; left: 0; right: 0; }
                .pcm-pos-top { top: 0; left: 0; right: 0; }
                .pcm-pos-left { bottom: 20px; left: 20px; max-width: 380px; }
                .pcm-pos-right { bottom: 20px; right: 20px; max-width: 380px; }

                .pcm-pos-bottom.pcm-animate { transform: translateY(100%); opacity: 0; }
                .pcm-pos-top.pcm-animate { transform: translateY(-100%); opacity: 0; }
                .pcm-pos-left.pcm-animate { transform: translateX(-100%); opacity: 0; }
                .pcm-pos-right.pcm-animate { transform: translateX(100%); opacity: 0; }

                .pcm-banner.pcm-visible { transform: translateY(0) translateX(0) !important; opacity: 1 !important; }

                .pcm-banner-inner {
                    background: ${c.bannerBackgroundColor};
                    color: ${c.bannerTextColor};
                    padding: 18px 24px;
                    box-shadow: 0 -2px 20px rgba(0,0,0,0.08), 0 0 0 1px rgba(0,0,0,0.04);
                }
                .pcm-pos-left .pcm-banner-inner,
                .pcm-pos-right .pcm-banner-inner {
                    border-radius: ${c.borderRadius};
                    box-shadow: 0 4px 24px rgba(0,0,0,0.12), 0 0 0 1px rgba(0,0,0,0.04);
                }

                .pcm-banner-content {
                    max-width: 1200px;
                    margin: 0 auto;
                    display: flex;
                    align-items: center;
                    justify-content: space-between;
                    gap: 20px;
                    flex-wrap: wrap;
                }
                .pcm-pos-left .pcm-banner-content,
                .pcm-pos-right .pcm-banner-content {
                    flex-direction: column;
                    align-items: stretch;
                }

                .pcm-banner-title {
                    margin: 0 0 4px;
                    font-size: 15px;
                    font-weight: 700;
                    line-height: 1.3;
                }
                .pcm-banner-desc {
                    margin: 0;
                    font-size: 13px;
                    opacity: 0.85;
                    line-height: 1.5;
                }
                .pcm-banner-actions {
                    display: flex;
                    gap: 8px;
                    flex-wrap: wrap;
                    flex-shrink: 0;
                }
                .pcm-pos-left .pcm-banner-actions,
                .pcm-pos-right .pcm-banner-actions {
                    flex-direction: column;
                }

                /* ───── Buttons ───── */
                .pcm-btn {
                    padding: 9px 18px;
                    border: none;
                    border-radius: ${c.borderRadius};
                    cursor: pointer;
                    font-size: 13px;
                    font-weight: 600;
                    transition: all 0.2s ease;
                    white-space: nowrap;
                    font-family: inherit;
                }
                .pcm-btn:hover {
                    transform: translateY(-1px);
                    box-shadow: 0 3px 10px rgba(0,0,0,0.12);
                }
                .pcm-btn:active { transform: translateY(0); }
                .pcm-btn-accept { background: ${c.acceptButtonColor}; color: #fff; }
                .pcm-btn-reject { background: ${c.rejectButtonColor}; color: #fff; }
                .pcm-btn-settings { background: ${c.settingsButtonColor}; color: #fff; }
                .pcm-btn-save { background: ${c.acceptButtonColor}; color: #fff; }

                /* ───── Modal ───── */
                .pcm-modal {
                    position: fixed;
                    inset: 0;
                    z-index: 1000000;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    padding: 20px;
                    opacity: 0;
                    visibility: hidden;
                    transition: opacity 0.3s ease, visibility 0.3s ease;
                    font-family: Inter, system-ui, -apple-system, sans-serif;
                }
                .pcm-modal-visible { opacity: 1; visibility: visible; }

                .pcm-modal-overlay {
                    position: absolute;
                    inset: 0;
                    background: rgba(0, 0, 0, 0.5);
                    backdrop-filter: blur(2px);
                }
                .pcm-modal-dialog {
                    position: relative;
                    background: #fff;
                    border-radius: 16px;
                    padding: 28px;
                    max-width: 480px;
                    width: 100%;
                    max-height: 80vh;
                    overflow-y: auto;
                    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.15);
                    transform: scale(0.95);
                    transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
                }
                .pcm-modal-visible .pcm-modal-dialog { transform: scale(1); }

                .pcm-modal-close {
                    position: absolute;
                    top: 16px;
                    right: 16px;
                    background: #f3f4f6;
                    border: none;
                    width: 32px;
                    height: 32px;
                    border-radius: 8px;
                    cursor: pointer;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    color: #6b7280;
                    transition: all 0.15s ease;
                }
                .pcm-modal-close:hover {
                    background: #e5e7eb;
                    color: #374151;
                }

                .pcm-modal-title {
                    margin: 0 0 20px;
                    font-size: 18px;
                    font-weight: 700;
                    color: #111827;
                }

                /* ───── Categories ───── */
                .pcm-category {
                    padding: 14px;
                    border: 1px solid #e5e7eb;
                    border-radius: 12px;
                    margin-bottom: 10px;
                    transition: border-color 0.15s ease;
                }
                .pcm-category:hover { border-color: #d1d5db; }
                .pcm-category:last-child { margin-bottom: 0; }

                .pcm-category-header {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    margin-bottom: 6px;
                }
                .pcm-category-info {
                    display: flex;
                    align-items: center;
                    gap: 8px;
                }
                .pcm-category-name {
                    font-size: 14px;
                    font-weight: 600;
                    color: #111827;
                }
                .pcm-category-badge {
                    font-size: 10px;
                    padding: 2px 6px;
                    border-radius: 4px;
                    background: #fef2f2;
                    color: #dc2626;
                    font-weight: 500;
                }
                .pcm-category-desc {
                    margin: 0;
                    font-size: 12px;
                    color: #6b7280;
                    line-height: 1.5;
                }

                /* ───── Toggle Switch ───── */
                .pcm-switch {
                    position: relative;
                    display: inline-block;
                    width: 40px;
                    height: 22px;
                    flex-shrink: 0;
                }
                .pcm-switch input {
                    opacity: 0;
                    width: 0;
                    height: 0;
                }
                .pcm-switch-slider {
                    position: absolute;
                    inset: 0;
                    background: #d1d5db;
                    border-radius: 22px;
                    cursor: pointer;
                    transition: background 0.25s ease;
                }
                .pcm-switch-slider::before {
                    content: '';
                    position: absolute;
                    top: 2px;
                    left: 2px;
                    width: 18px;
                    height: 18px;
                    background: #fff;
                    border-radius: 50%;
                    transition: transform 0.25s ease;
                    box-shadow: 0 1px 3px rgba(0,0,0,0.15);
                }
                .pcm-switch input:checked + .pcm-switch-slider {
                    background: ${c.acceptButtonColor};
                }
                .pcm-switch input:checked + .pcm-switch-slider::before {
                    transform: translateX(18px);
                }
                .pcm-switch input:disabled + .pcm-switch-slider {
                    opacity: 0.6;
                    cursor: not-allowed;
                }

                /* ───── Modal Footer ───── */
                .pcm-modal-footer {
                    display: flex;
                    justify-content: flex-end;
                    gap: 10px;
                    margin-top: 20px;
                    padding-top: 18px;
                    border-top: 1px solid #e5e7eb;
                }

                /* ───── Inline Preview ───── */
                .pcm-inline-wrap {
                    border-radius: ${c.borderRadius};
                    overflow: hidden;
                }
                .pcm-inline-wrap .pcm-banner-inner {
                    border-radius: ${c.borderRadius};
                }

                /* ───── Mobile ───── */
                @media (max-width: 640px) {
                    .pcm-banner-content {
                        flex-direction: column;
                        text-align: center;
                    }
                    .pcm-banner-actions {
                        width: 100%;
                        flex-direction: column;
                    }
                    .pcm-btn { width: 100%; text-align: center; }
                    .pcm-pos-left, .pcm-pos-right {
                        left: 10px !important;
                        right: 10px !important;
                        bottom: 10px !important;
                        max-width: none !important;
                    }
                    .pcm-modal-dialog {
                        padding: 20px;
                        max-height: 90vh;
                    }
                }
            `;
            document.head.appendChild(style);
        }

        /* ─────────── event tracking ─────────── */

        _sendEvent(eventType, acceptedCategories) {
            if (this.livePreview) return;
            const payload = {
                companyId: this.companyId,
                eventType: eventType,
                pageUrl: location.href,
                sessionId: this.sessionId
            };
            if (acceptedCategories) {
                payload.acceptedCategories = acceptedCategories;
            }
            fetch(this.trackUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            }).catch(() => { });
        }

        /* ─────────── public API ─────────── */

        hasConsent(categoryId) {
            const prefs = this._getPreferences();
            return prefs[categoryId] === true;
        }

        getPreferences() {
            return this._getPreferences();
        }

        openSettings() {
            this._showSettingsModal();
        }

        destroy() {
            this._closeBanner();
            this._closeSettingsModal();
            const style = document.getElementById(STYLE_ID);
            if (style) style.remove();
        }
    }

    /* ─────────── auto-init for end-user sites ─────────── */
    if (typeof window !== 'undefined') {
        window.PushonicaCookieManagement = PushonicaCookieManagement;

        // Auto-init if not in admin context
        if (!window.__PUSHONICA_ADMIN__) {
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', () => {
                    window.pushonicaCookieManagement = new PushonicaCookieManagement();
                });
            } else {
                window.pushonicaCookieManagement = new PushonicaCookieManagement();
            }
        }
    }

})();

