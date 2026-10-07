/**
 * Pushonica Newsletter Popup Module — Premium Edition
 * E-Bülten abonelik popup widget'ı.
 * Admin live-preview (livePreview: true) ve production R2 deploy desteği.
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

    const uid = () => 'np_' + Date.now() + '_' + Math.random().toString(36).slice(2, 9);

    const STYLE_ID = 'pushonica-newsletter-popup-styles';

    /* ═══════════════════════════════════════════════════
       CLASS
    ═══════════════════════════════════════════════════ */
    class PushonicaNewsletterPopup {
        constructor(opts = {}) {
            this.options = opts;
            this.companyId = opts.companyId || this.getCompanyId();
            this.apiBaseUrl = opts.apiBaseUrl || 'https://pushadmin-production.up.railway.app';
            this.r2BaseUrl = opts.r2BaseUrl || 'https://assets.pushonica.com';
            this.livePreview = opts.livePreview || false;
            this.targetElement = opts.targetElement || document.body;
            this.config = null;
            this.containerId = 'newsletterPopup_' + uid();
            this.sessionId = uid();
            this.isInitialized = false;
            this.isVisible = false;

            // Storage keys (company-specific)
            this.storageKeySubmitted = `pushonica_newsletter_submitted_${this.companyId}`;
            this.storageKeyDismissed = `pushonica_newsletter_dismissed_${this.companyId}`;

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
                const configUrl = `${this.r2BaseUrl}/${this.companyId}/newsletterPopup.json`;
                const response = await fetch(configUrl);
                if (!response.ok) throw new Error(`HTTP ${response.status}`);
                const data = await response.json();
                return data;
            } catch (error) {
                console.warn('[NewsletterPopup] Config yüklenemedi:', error.message);
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

        /* ── config parser ── */
        _parseConfig(raw) {
            if (!raw) return;

            const c = raw.config || raw.Config || raw;

            this.config = {
                companyId: raw.companyId || c.companyId || this.companyId || '',
                isActive: c.isActive !== false,
                title: c.title || 'Bültenimize Abone Olun!',
                description: c.description || '',
                submitButtonText: c.submitButtonText || 'Kaydet',
                privacyText: c.privacyText || '',
                privacyUrl: c.privacyUrl || '#',
                kvkkLink: c.kvkkLink || '',
                etkLink: c.etkLink || '',
                namePlaceholder: c.namePlaceholder || 'Adınız',
                surnamePlaceholder: c.surnamePlaceholder || 'Soyadınız',
                phonePlaceholder: c.phonePlaceholder || '5XX XXX XX XX',
                backgroundColor: c.backgroundColor || '#ffffff',
                textColor: c.textColor || '#000000',
                buttonColor: c.buttonColor || '#e53935',
                buttonTextColor: c.buttonTextColor || '#ffffff',
                borderRadius: c.borderRadius || '12px',
                imageUrl: c.imageUrl || '',
                imagePosition: c.imagePosition || 'left',
                hideImageOnMobile: c.hideImageOnMobile === true,
                mobileImageUrl: c.mobileImageUrl || '',
                sloganEnabled: c.sloganEnabled === true,
                sloganBadgeText: c.sloganBadgeText || '',
                sloganText: c.sloganText || '',
                sloganHighlightColor: c.sloganHighlightColor || '#c8a96e',
                sloganTextColor: c.sloganTextColor || '#ffffff',
                showNameFields: c.showNameFields !== false,
                showPhoneField: c.showPhoneField === true,
                showConsentCheckbox: c.showConsentCheckbox !== false,
                delaySeconds: typeof c.delaySeconds === 'number' ? c.delaySeconds : 5,
                showFrequencyDays: typeof c.showFrequencyDays === 'number' ? c.showFrequencyDays : 30,
                showOnMobile: c.showOnMobile !== false,
                showOnDesktop: c.showOnDesktop !== false,
                enableAnimation: c.enableAnimation !== false,
                trackUrl: raw.trackUrl || `${this.apiBaseUrl}/api/NewsletterPopupApi/track`,
                submitUrl: raw.submitUrl || `${this.apiBaseUrl}/api/NewsletterPopupApi/submit`
            };
        }

        /* ── helpers ── */
        _isMobile() {

            return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);

        }
        _formatTRPhone(raw) {
            let digits = (raw || '').replace(/\D/g, '');

            // +90 prefix zaten gösterildiği için baştaki 0'ı kaldır
            if (digits.startsWith('90') && digits.length > 2) digits = digits.slice(2);
            if (digits.startsWith('0')) digits = digits.slice(1);

            // Sadece 5 ile başlayan numaralar kabul edilsin
            if (digits.length >= 1 && digits[0] !== '5') {
                digits = '';
            }

            digits = digits.slice(0, 10);

            const p1 = digits.slice(0, 3);
            const p2 = digits.slice(3, 6);
            const p3 = digits.slice(6, 8);
            const p4 = digits.slice(8, 10);

            let out = p1;
            if (p2) out += ' ' + p2;
            if (p3) out += ' ' + p3;
            if (p4) out += ' ' + p4;
            return out.trim();
        }

        _getTRPhoneDigits(formatted) {
            let d = (formatted || '').replace(/\D/g, '');

            // +90 prefix zaten gösterildiği için baştaki 0 veya 90'ı kaldır
            if (d.startsWith('90') && d.length > 2) d = d.slice(2);
            if (d.startsWith('0')) d = d.slice(1);

            d = d.slice(0, 10);

            if (d.length >= 1 && d[0] !== '5') return '';

            return d;
        }

        /* ── init ── */
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

                // Device check
                const isMobile = this._isMobile();
                if (isMobile && !this.config.showOnMobile) return;
                if (!isMobile && !this.config.showOnDesktop) return;

                // Already submitted → never show again
                if (this._getStorageItem(this.storageKeySubmitted) === 'true') return;

                // Frequency check
                const dismissedAt = this._getStorageItem(this.storageKeyDismissed);
                if (dismissedAt) {
                    const diffDays = (Date.now() - parseInt(dismissedAt)) / (1000 * 60 * 60 * 24);
                    if (diffDays < this.config.showFrequencyDays) return;
                }

                // Show with delay
                const delay = (this.config.delaySeconds || 0) * 1000;
                setTimeout(() => this._showPopup(), delay);

            } catch (error) {
                console.error('[NewsletterPopup] Başlatma hatası:', error);
            }
        }

        /* ── storage helpers ── */
        _getStorageItem(key) {
            try { return localStorage.getItem(key); } catch { return null; }
        }

        _setStorageItem(key, value) {
            try { localStorage.setItem(key, value); } catch { }
        }

        /* ═══════════════════════════════════════════
           INLINE PREVIEW (admin panel)
        ═══════════════════════════════════════════ */
        _renderInline() {
            if (!this.targetElement) return;
            this.targetElement.innerHTML = '';
            this._injectStyles();

            const wrap = document.createElement('div');
            wrap.className = 'pnp-inline-wrap';
            wrap.innerHTML = this._buildPopupHTML();
            this.targetElement.appendChild(wrap);
            this._bindEvents(wrap);
        }

        /* ═══════════════════════════════════════════
           POPUP (production)
        ═══════════════════════════════════════════ */
        _showPopup() {
            if (this.isVisible) return;
            this.isVisible = true;

            this._injectStyles();

            const overlay = document.createElement('div');
            overlay.id = this.containerId;
            overlay.className = 'pnp-overlay' + (this.config.enableAnimation ? ' pnp-animate' : '');

            overlay.innerHTML = `<div class="pnp-dialog">${this._buildPopupHTML()}</div>`;
            document.body.appendChild(overlay);

            // Show with animation
            if (this.config.enableAnimation) {
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => overlay.classList.add('pnp-visible'));
                });
            } else {
                overlay.classList.add('pnp-visible');
            }

            // Backdrop click to dismiss
            overlay.addEventListener('click', (e) => {
                if (e.target === overlay) {
                    this._dismiss();
                }
            });

            this._bindEvents(overlay);
            this._sendEvent('impression');
        }

        /* ═══════════════════════════════════════════
           HTML BUILDER
        ═══════════════════════════════════════════ */
        _renderSloganHtml(text, highlightColor) {
            if (!text) return '';
            // Escape HTML first
            let s = esc(text);
            // Replace [hl]...[/hl] with styled spans
            s = s.replace(/\[hl\](.*?)\[\/hl\]/g, `<span style="color:${highlightColor};font-style:italic;font-weight:700;">$1</span>`);
            // Convert newlines to <br>
            s = s.replace(/\n/g, '<br>');
            return s;
        }

        _buildPopupHTML() {
            const c = this.config;
            const bgColor = c.backgroundColor;
            const txtColor = c.textColor;
            const btnColor = c.buttonColor;
            const btnTxtColor = c.buttonTextColor;
            const radius = c.borderRadius;
            const isMobile = window.innerWidth <= 480;
            const effectiveImageUrl = isMobile
                ? (c.hideImageOnMobile ? '' : (c.mobileImageUrl || c.imageUrl))
                : c.imageUrl;
            const hasImage = !!effectiveImageUrl;
            const imgPos = c.imagePosition || 'left';
            const posClass = hasImage ? `pnp-card-with-image pnp-img-${imgPos}` : '';
            const showSlogan = c.sloganEnabled && hasImage && c.sloganText;

            let html = `<div class="pnp-card ${posClass}" style="background:${bgColor};color:${txtColor};border-radius:${radius};">`;

            // Build slogan overlay HTML
            let sloganOverlay = '';
            if (showSlogan) {
                const sloganTxtColor = c.sloganTextColor || '#ffffff';
                const sloganHlColor = c.sloganHighlightColor || '#c8a96e';
                sloganOverlay = `<div class="pnp-slogan-overlay" style="color:${sloganTxtColor};">`;
                if (c.sloganBadgeText) {
                    sloganOverlay += `<div class="pnp-slogan-badge" style="color:${sloganHlColor};">${esc(c.sloganBadgeText)}</div>`;
                }
                sloganOverlay += `<div class="pnp-slogan-text">${this._renderSloganHtml(c.sloganText, sloganHlColor)}</div>`;
                sloganOverlay += `</div>`;
            }

            // Image section (left or top - render before content)
            if (hasImage && (imgPos === 'left' || imgPos === 'top')) {
                html += `<div class="pnp-image-panel" style="border-radius: calc(${radius});">
                    <img src="${effectiveImageUrl}" alt="" class="pnp-image">
                    ${sloganOverlay}
                </div>`;
            }

            if (hasImage) {
                html += `<div class="pnp-content-panel">`;
            }

            // Close button
            html += `<button type="button" class="pnp-close" aria-label="Kapat">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="${txtColor}" stroke-width="2">
                    <path d="M18 6L6 18M6 6l12 12"/>
                </svg>
            </button>`;

            // Icon (only show when no image)
            if (!hasImage) {
                html += `<div class="pnp-icon" style="background:${btnColor};">
                    <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="${btnTxtColor}" stroke-width="2">
                        <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/>
                        <polyline points="22,6 12,13 2,6"/>
                    </svg>
                </div>`;
            }

            // Title & description
            html += `<h3 class="pnp-title" style="color:${txtColor};">${esc(c.title)}</h3>`;
            if (c.description) {
                html += `<p class="pnp-desc" style="color:${txtColor};">${esc(c.description)}</p>`;
            }

            // Form
            html += `<form class="pnp-form" onsubmit="return false;">`;

            if (c.showNameFields) {
                html += `<div class="pnp-field-group">
                    <div class="pnp-row pnp-name-row">
                        <input type="text" class="pnp-input" data-field="name" placeholder="${esc(c.namePlaceholder)}" style="color:${txtColor};">
                        <input type="text" class="pnp-input" data-field="surname" placeholder="${esc(c.surnamePlaceholder)}" style="color:${txtColor};">
                    </div>
                    <div class="pnp-error pnp-name-error" style="display:none; color: #ef4444; font-size: 12px; margin-top: 5px; text-align: left;">
                        Lütfen ad ve soyad alanlarını doldurunuz.
                    </div>
                </div>`;
            }

            if (c.showPhoneField) {
                html += `<div class="pnp-field-group">
                    <div class="pnp-phone-wrapper">
                        <div class="pnp-phone-prefix">
                            <span class="pnp-country-code">+90</span>
                        </div>
                        <input type="tel" class="pnp-input pnp-phone-input" data-field="phone" placeholder="${esc(c.phonePlaceholder)}">
                    </div>
                    <div class="pnp-error pnp-phone-error" style="display:none;">
                        Telefon numarası <b>5</b> ile başlamalı ve <b>10 hane</b> olmalıdır.
                    </div>
                </div>`;
            }

            if (c.showConsentCheckbox) {
                let privacyHtml = esc(c.privacyText);
                // Add legal text links
                const legalLinks = [];
                if (c.kvkkLink) {
                    legalLinks.push(`<a href="${esc(c.kvkkLink)}" target="_blank" style="color:${txtColor};text-decoration:underline;">KVKK Aydınlatma Metni</a>`);
                }
                if (c.etkLink) {
                    legalLinks.push(`<a href="${esc(c.etkLink)}" target="_blank" style="color:${txtColor};text-decoration:underline;">Ticari Elektronik İleti İzni</a>`);
                }
                if (legalLinks.length === 0 && c.privacyUrl && c.privacyUrl !== '#') {
                    legalLinks.push(`<a href="${esc(c.privacyUrl)}" target="_blank" style="color:${txtColor};text-decoration:underline;">Gizlilik Politikası</a>`);
                }
                if (legalLinks.length > 0) {
                    privacyHtml += ' ' + legalLinks.join(' ve ') + "'ni okudum, kabul ediyorum.";
                }
                html += `<div class="pnp-consent">
                    <input type="checkbox" class="pnp-consent-checkbox">
                    <span class="pnp-consent-text" style="color:${txtColor};">${privacyHtml}</span>
                </div>`;
            }

            html += `<button type="submit" class="pnp-btn pnp-btn-submit" style="background:${btnColor};color:${btnTxtColor};">
                ${esc(c.submitButtonText)}
            </button>`;

            html += `<p class="pnp-disclaimer" style="color:${txtColor};">İletişim bilgilerinizi (ad, soyad, e-posta adresi ve telefon numarası) paylaşarak tarafınıza pazarlama, kampanya, tanıtım ve bilgilendirme amaçlı ileti gönderilmesini kabul etmiş olursunuz. Kişisel verilerinizin işlenmesine ilişkin detaylı bilgiye Gizlilik Politikamızdan ulaşabilirsiniz.</p>`;

            html += `</form>`;

            // Success state (hidden initially)
            html += `<div class="pnp-success" style="display:none;">
                <div class="pnp-success-icon">
                    <svg viewBox="0 0 24 24" fill="none" stroke="${btnColor}" stroke-width="2">
                        <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                        <polyline points="22 4 12 14.01 9 11.01"/>
                    </svg>
                </div>
                <h4 class="pnp-success-title" style="color:${txtColor};">Teşekkür Ederiz!</h4>
                <p class="pnp-success-desc" style="color:${txtColor};">Kaydınız başarıyla alınmıştır.</p>
            </div>`;

            // Branding
            html += `<div class="pnp-branding">
                <a href="https://dijitaldesin.com" target="_blank" style="text-decoration:none;display:flex;align-items:center;">
                    <img src="https://dijitaldesin.com/wp-content/uploads/2023/02/dijitaldesin.svg" alt="Dijitaldesin" style="height:24px;">
                </a>
            </div>`;

            // Close content panel if image layout
            if (hasImage) {
                html += `</div>`;
            }

            // Image section (right - render after content)
            if (hasImage && imgPos === 'right') {
                html += `<div class="pnp-image-panel" style="border-radius: calc(${radius});">
                    <img src="${effectiveImageUrl}" alt="" class="pnp-image">
                    ${sloganOverlay}
                </div>`;
            }

            html += `</div>`;
            return html;
        }

        /* ═══════════════════════════════════════════
           EVENT BINDING
        ═══════════════════════════════════════════ */
        _bindEvents(root) {
            root.querySelectorAll('.pnp-close').forEach(b =>
                b.addEventListener('click', () => this._dismiss())
            );

            const nameEl = root.querySelector('[data-field="name"]');
            if (nameEl) {
                nameEl.addEventListener('input', () => {
                    nameEl.style.borderColor = '';
                    const errorEl = root.querySelector('.pnp-name-error');
                    if (errorEl) errorEl.style.display = 'none';
                });
            }

            const surnameEl = root.querySelector('[data-field="surname"]');
            if (surnameEl) {
                surnameEl.addEventListener('input', () => {
                    surnameEl.style.borderColor = '';
                    const errorEl = root.querySelector('.pnp-name-error');
                    if (errorEl) errorEl.style.display = 'none';
                });
            }

            const phoneEl = root.querySelector('[data-field="phone"]');
            if (phoneEl) {
                phoneEl.setAttribute('inputmode', 'numeric');
                phoneEl.setAttribute('autocomplete', 'tel');
                phoneEl.setAttribute('maxlength', '15');

                phoneEl.addEventListener('input', () => {
                    phoneEl.value = this._formatTRPhone(phoneEl.value);
                    const errorEl = root.querySelector('.pnp-phone-error');
                    if (errorEl) errorEl.style.display = 'none';
                    phoneEl.style.borderColor = '';
                });

                phoneEl.addEventListener('paste', (e) => {
                    e.preventDefault();
                    const text = (e.clipboardData || window.clipboardData).getData('text') || '';
                    phoneEl.value = this._formatTRPhone(text);
                });
            }

            // Form submit
            const form = root.querySelector('.pnp-form');
            if (form) {
                form.addEventListener('submit', (e) => {
                    e.preventDefault();
                    this._handleSubmit(root);
                });
            }
        }


        _handleSubmit(root) {
            const c = this.config;

            // Consent validation
            if (c.showConsentCheckbox) {
                const checkbox = root.querySelector('.pnp-consent-checkbox');
                if (checkbox && !checkbox.checked) {
                    checkbox.style.outline = '2px solid #ef4444';
                    setTimeout(() => { checkbox.style.outline = ''; }, 2000);
                    return;
                }
            }

            // Collect form data
            const formData = {};
            if (c.showNameFields) {
                const nameInput = root.querySelector('[data-field="name"]');
                const surnameInput = root.querySelector('[data-field="surname"]');
                const errorEl = root.querySelector('.pnp-name-error');
                
                formData.name = nameInput ? nameInput.value.trim() : '';
                formData.surname = surnameInput ? surnameInput.value.trim() : '';

                let hasNameError = false;

                if (!formData.name) {
                    if (nameInput) nameInput.style.borderColor = '#ef4444';
                    hasNameError = true;
                }

                if (!formData.surname) {
                    if (surnameInput) surnameInput.style.borderColor = '#ef4444';
                    hasNameError = true;
                }

                if (hasNameError) {
                    if (errorEl) errorEl.style.display = 'block';
                    return;
                } else {
                    if (errorEl) errorEl.style.display = 'none';
                }
            }
            if (c.showPhoneField) {
                const inputEl = root.querySelector('[data-field="phone"]');
                const errorEl = root.querySelector('.pnp-phone-error');
                const rawPhone = (inputEl || {}).value || '';
                formData.phone = this._getTRPhoneDigits(rawPhone);

                const isValid =
                    formData.phone &&
                    formData.phone.length === 10 &&
                    formData.phone.startsWith('5');

                if (!isValid) {
                    if (inputEl) {
                        inputEl.style.borderColor = '#ef4444';
                    }
                    if (errorEl) {
                        errorEl.style.display = 'block';
                    }

                    const btn = root.querySelector('.pnp-btn-submit');
                    if (btn) {
                        btn.disabled = false;
                        btn.textContent = c.submitButtonText;
                    }
                    return;
                } else {
                    // Hata varsa temizle
                    if (inputEl) {
                        inputEl.style.borderColor = '';
                    }
                    if (errorEl) {
                        errorEl.style.display = 'none';
                    }
                }
            }

            // UI feedback
            const btn = root.querySelector('.pnp-btn-submit');
            if (btn) {
                btn.disabled = true;
                btn.textContent = 'Gönderiliyor...';
            }

            if (this.livePreview) {
                // Preview mode: show success immediately
                this._showSuccess(root);
                return;
            }

            // Submit via API
            fetch(this.config.submitUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'omit',
                body: JSON.stringify({
                    companyId: this.config.companyId,
                    eventType: 'submit',
                    pageUrl: location.href,
                    sessionId: this.sessionId,
                    customerName: formData.name || '',
                    customerSurname: formData.surname || '',
                    customerPhone: formData.phone ? '0' + formData.phone : ''
                })
            })
                .then(r => { if (!r.ok) throw new Error('fail'); return r.json(); })
                .then(() => {
                    this._setStorageItem(this.storageKeySubmitted, 'true');
                    this._showSuccess(root);
                })
                .catch(() => {
                    // Show success anyway (don't block UX)
                    this._setStorageItem(this.storageKeySubmitted, 'true');
                    this._showSuccess(root);
                });
        }

        _showSuccess(root) {
            const form = root.querySelector('.pnp-form');
            const success = root.querySelector('.pnp-success');
            if (form) form.style.display = 'none';
            if (success) success.style.display = 'block';

            if (!this.livePreview) {
                setTimeout(() => this._dismiss(), 3000);
            } else {
                // In preview, reset after 3 seconds
                setTimeout(() => this._renderInline(), 3000);
            }
        }

        /* ═══════════════════════════════════════════
           DISMISS / CLOSE
        ═══════════════════════════════════════════ */
        _dismiss() {
            if (!this.livePreview) {
                this._sendEvent('dismiss');
                this._setStorageItem(this.storageKeyDismissed, Date.now().toString());
            }

            const overlay = document.getElementById(this.containerId);
            if (overlay) {
                overlay.classList.remove('pnp-visible');
                setTimeout(() => overlay.remove(), 350);
            }
            this.isVisible = false;
        }

        /* ═══════════════════════════════════════════
           EVENT TRACKING
        ═══════════════════════════════════════════ */
        _sendEvent(eventType) {
            if (this.livePreview) return;
            try {
                const payload = {
                    companyId: this.config.companyId,
                    eventType: eventType,
                    pageUrl: location.href,
                    sessionId: this.sessionId
                };

                fetch(this.config.trackUrl, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload),
                    credentials: 'omit',
                    keepalive: true
                }).catch(() => { });
            } catch { }
        }

        /* ═══════════════════════════════════════════
           STYLES
        ═══════════════════════════════════════════ */
        _injectStyles() {
            const old = document.getElementById(STYLE_ID);
            if (old) old.remove();

            const c = this.config;
            const accent = c.buttonColor;
            const accentRGB = this._hexToRgb(accent);
            const tag = document.createElement('style');
            tag.id = STYLE_ID;
            tag.textContent = `
/* ─── Google Font ─── */
@import url('https://fonts.googleapis.com/css2?family=Cormorant+Garamond:ital,wght@0,400;0,600;0,700;1,400;1,600;1,700&display=swap');

/* ─── PNP Reset ─── */
.pnp-overlay,.pnp-overlay *,.pnp-inline-wrap,.pnp-inline-wrap *{box-sizing:border-box;margin:0;padding:0;font-family:Inter,system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;}

/* ─── Overlay ─── */
.pnp-overlay{position:fixed;inset:0;z-index:999999;display:flex;align-items:center;justify-content:center;opacity:0;visibility:hidden;transition:opacity .35s ease,visibility .35s ease;}
.pnp-overlay.pnp-visible{opacity:1;visibility:visible;}
.pnp-overlay::before{content:'';position:absolute;inset:0;background:rgba(0,0,0,0.5);backdrop-filter:blur(4px);-webkit-backdrop-filter:blur(4px);}

/* ─── Dialog ─── */
.pnp-dialog{position:relative;z-index:1;width:92%;max-width:420px;max-height:95vh;max-height:95svh;overflow-y:auto;transform:scale(.92) translateY(20px);opacity:0;transition:transform .4s cubic-bezier(.22,1,.36,1),opacity .35s ease;}
.pnp-dialog:has(.pnp-card-with-image){max-width:860px;}
.pnp-dialog:has(.pnp-card-with-image.pnp-img-top){max-width:450px;}
.pnp-visible .pnp-dialog{transform:scale(1) translateY(0);opacity:1;}

/* ─── Inline wrap ─── */
.pnp-inline-wrap{width:100%;max-width:420px;margin:0 auto;}
.pnp-inline-wrap:has(.pnp-card-with-image){max-width:860px;}
.pnp-inline-wrap:has(.pnp-card-with-image.pnp-img-top){max-width:450px;}

/* ─── Card ─── */
.pnp-card{padding:2.5rem 1.75rem 2rem;position:relative;box-shadow:0 25px 60px -12px rgba(0,0,0,.25);}
.pnp-card-with-image{display:flex;padding:0px;gap:24px;overflow:hidden;}
.pnp-card-with-image .pnp-content-panel{flex:1;padding:2rem 2rem 1.5rem 0;overflow-y:auto;position:relative;min-width:0;display:flex;flex-direction:column;justify-content:center;}
.pnp-image-panel{flex:0 0 50%;max-width:50%;overflow:hidden;}
.pnp-image-panel .pnp-image{width:100%;height:100%;object-fit:cover;display:block;border-radius:inherit;}

/* ─── Image Position: Top ─── */
.pnp-card.pnp-img-top{flex-direction:column;gap:0;}
.pnp-card.pnp-img-top .pnp-image-panel{flex:none;max-width:100%;width:100%;max-height:220px;}
.pnp-card.pnp-img-top .pnp-image-panel .pnp-image{width:100%;height:220px;object-fit:cover;display:block;}
.pnp-card.pnp-img-top .pnp-content-panel{padding:1.25rem 1.75rem 1.25rem;}
.pnp-card.pnp-img-top .pnp-title{font-size:1.6rem;margin-bottom:6px;}
.pnp-card.pnp-img-top .pnp-desc{font-size:.875rem;margin-bottom:14px;}
.pnp-card.pnp-img-top .pnp-row{margin-bottom:10px;}
.pnp-card.pnp-img-top .pnp-field-group{margin-bottom:10px;}
.pnp-card.pnp-img-top .pnp-consent{margin-bottom:14px;gap:8px;}
.pnp-card.pnp-img-top .pnp-consent-text{font-size:11px;}
.pnp-card.pnp-img-top .pnp-btn{padding:11px;font-size:14px;}
.pnp-card.pnp-img-top .pnp-disclaimer{margin-top:8px;font-size:8px;}
.pnp-card.pnp-img-top .pnp-branding{margin-top:.75rem;padding-top:.5rem;}

/* ─── Image Position: Right ─── */
.pnp-card.pnp-img-right{flex-direction:row;}
.pnp-card.pnp-img-right .pnp-content-panel{padding:2rem 0 1.5rem 2rem;}

/* ─── Image Position: Left (default) ─── */
.pnp-card.pnp-img-left{flex-direction:row;}

/* ─── Slogan Overlay ─── */
.pnp-image-panel{position:relative;}
.pnp-slogan-overlay{position:absolute;bottom:0;left:0;right:0;padding:24px 28px;background:linear-gradient(0deg,rgba(0,0,0,0.75) 0%,rgba(0,0,0,0.35) 60%,transparent 100%);display:flex;flex-direction:column;justify-content:flex-end;z-index:1;}
.pnp-slogan-badge{display:inline-block;font-size:11px;font-weight:600;letter-spacing:2px;text-transform:uppercase;margin-bottom:12px;padding:5px 14px;border:1px solid currentColor;border-radius:50px;width:fit-content;font-family:Inter,system-ui,sans-serif !important;}
.pnp-slogan-text,.pnp-slogan-text *{font-size:2.2rem;font-weight:400;line-height:1.2;letter-spacing:-0.3px;font-family:'Cormorant Garamond',Georgia,'Times New Roman',serif !important;}

/* ─── Close ─── */
.pnp-close{position:absolute;top:14px;right:14px;background:transparent;border:none;cursor:pointer;width:32px;height:32px;display:flex;align-items:center;justify-content:center;border-radius:0;transition:opacity .2s;z-index:2;opacity:.55;}
.pnp-close:hover{opacity:1;}
.pnp-close svg{width:18px;height:18px;}

/* ─── Icon ─── */
.pnp-icon{width:64px;height:64px;border-radius:50%;display:flex;align-items:center;justify-content:center;margin:0 auto 22px;}

/* ─── Typography ─── */
.pnp-title{font-size:2.4rem;font-weight:800;margin:0 0 12px;line-height:1.15;text-align:left;letter-spacing:-0.5px;}
.pnp-desc{font-size:1rem;margin:0 0 24px;line-height:1.5;text-align:left;opacity:0.9;}

/* ─── Form ─── */
.pnp-form{display:flex;flex-direction:column;gap:0;}
.pnp-field-group{margin-bottom:16px;}
.pnp-row{display:flex;flex-direction:column;gap:16px;margin-bottom:16px;}
.pnp-name-row{flex-direction:row;gap:12px;}
.pnp-name-row .pnp-input{width:50%;margin-bottom:0;}
.pnp-input{width:100%;padding:10px 0;border:none;border-bottom:1px solid rgba(0,0,0,.3);border-radius:0;font-size:15px;outline:none;background:transparent;transition:all .2s ease;box-sizing:border-box;margin-bottom:0;font-family:inherit;color:inherit;}
.pnp-input:focus{border-bottom-color:${accent};box-shadow:none;}
.pnp-input::placeholder{color:inherit;opacity:.8;font-weight:400;}

/* ─── Phone field with prefix ─── */
.pnp-phone-wrapper{display:flex;align-items:stretch;border:none;border-bottom:1px solid rgba(0,0,0,.3);border-radius:0;overflow:hidden;background:transparent;transition:all .2s ease;padding:6px 0;}
.pnp-phone-wrapper:focus-within{border-bottom-color:${accent};box-shadow:none;}
.pnp-phone-prefix{display:flex;align-items:center;gap:4px;padding:4px 12px;background:rgba(0,0,0,.04);border:1px solid rgba(0,0,0,.15);border-radius:6px;flex-shrink:0;margin-right:12px;cursor:default;}
.pnp-country-code{font-size:14px;font-weight:500;opacity:.8;white-space:nowrap;}
.pnp-phone-input{border:none !important;border-radius:0 !important;box-shadow:none !important;margin:0;padding:6px 0;}
.pnp-phone-input:focus{box-shadow:none !important;}

/* ─── Consent ─── */
.pnp-consent{display:flex;align-items:flex-start;gap:10px;margin-bottom:24px;}
.pnp-consent-checkbox{flex-shrink:0;width:18px;height:18px;cursor:pointer;accent-color:${accent};margin-top:2px;}
.pnp-consent-text{font-size:12px;opacity:.8;line-height:1.45;text-align:left;}
.pnp-consent-text a{color:inherit;text-decoration:underline;}

/* ─── Submit Button ─── */
.pnp-btn{width:100%;padding:14px;border:none;border-radius:50px;font-size:16px;font-weight:600;cursor:pointer;transition:all .25s ease;font-family:inherit;letter-spacing:.5px;}
.pnp-btn:hover:not(:disabled){transform:translateY(-1px);box-shadow:0 6px 20px rgba(${accentRGB},.35);}
.pnp-btn:active:not(:disabled){transform:scale(.98);}
.pnp-btn:disabled{opacity:.6;cursor:not-allowed;}

/* ─── Success ─── */
.pnp-success{text-align:center;padding:32px 0;}
.pnp-success-icon{display:flex;justify-content:center;margin-bottom:20px;}
.pnp-success-icon svg{width:64px;height:64px;}
.pnp-success-title{font-size:18px;font-weight:700;margin:0 0 12px;}
.pnp-success-desc{font-size:14px;opacity:.7;margin:0;line-height:1.6;}

/* ─── Branding ─── */
.pnp-disclaimer{font-size:8.5px;line-height:1.4;opacity:.6;margin-top:12px;text-align:left;}
.pnp-branding{display:flex;justify-content:center;align-items:center;margin-top:1rem;padding-top:0.5rem;}
.pnp-branding a{opacity:.6;transition:opacity .2s;}
.pnp-branding a:hover{opacity:1;}

/* ─── Mobile ─── */
@media(max-width:480px){
    .pnp-overlay{align-items:flex-end;}
    .pnp-dialog{width:100%;max-width:100%;max-height:95vh;max-height:95svh;transform:translateY(100%);opacity:1;}
    .pnp-dialog:has(.pnp-card-with-image){max-width:100%;}
    .pnp-visible .pnp-dialog{transform:translateY(0);}
    .pnp-card{border-radius:24px 24px 0 0 !important;padding:1.5rem 1.5rem 0.5rem;}
    .pnp-card-with-image{flex-direction:column !important;border-radius:24px 24px 0 0 !important;padding:8px !important;}
    .pnp-card-with-image .pnp-image-panel{flex:none;max-width:100% !important;width:100%;max-height:200px;border-radius:16px;}
    .pnp-card-with-image .pnp-image-panel .pnp-image{width:100%;height:200px;object-fit:cover;display:block;}
    .pnp-card-with-image .pnp-content-panel{padding:0 1rem 0.5rem !important;}
    .pnp-slogan-overlay{padding:16px 18px !important;}
    .pnp-slogan-text{font-size:1.3rem !important;}
    .pnp-slogan-badge{font-size:9px !important;padding:3px 10px !important;margin-bottom:8px !important;}
}

/* ─── Error─── */
.pnp-error{font-size:12px;color:#ef4444;margin:6px 0 0;line-height:1.4;}

/* ─── No Animation ─── */
.pnp-overlay:not(.pnp-animate){transition:none;}
.pnp-overlay:not(.pnp-animate) .pnp-dialog{transition:none;}
.pnp-overlay:not(.pnp-animate).pnp-visible{opacity:1;visibility:visible;}
.pnp-overlay:not(.pnp-animate).pnp-visible .pnp-dialog{transform:none;opacity:1;}
            `;
            document.head.appendChild(tag);
        }

        _hexToRgb(hex) {
            if (!hex) return '0,0,0';
            hex = hex.replace('#', '');
            if (hex.length === 3) hex = hex[0] + hex[0] + hex[1] + hex[1] + hex[2] + hex[2];
            const n = parseInt(hex, 16);
            return `${(n >> 16) & 255},${(n >> 8) & 255},${n & 255}`;
        }

        /* ── public API ── */
        destroy() {
            const overlay = document.getElementById(this.containerId);
            if (overlay) overlay.remove();
            const style = document.getElementById(STYLE_ID);
            if (style) style.remove();
            this.isInitialized = false;
        }
    }

    /* ═══════════════════════════════════════════
       GLOBAL EXPORTS
    ═══════════════════════════════════════════ */
    window.PushonicaNewsletterPopup = PushonicaNewsletterPopup;

    // Auto-init: if loaded on production site (not admin)
    if (!window.__PUSHONICA_ADMIN__) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => {
                window.pushonicaNewsletterPopup = new PushonicaNewsletterPopup();
            });
        } else {
            window.pushonicaNewsletterPopup = new PushonicaNewsletterPopup();
        }
    }
})();
