/**
 * Pushonica Exit Intent Survey Module — Premium CRO Edition
 * High-end exit-intent survey widget with multi-step logic, NPS,
 * selectable card inputs, smooth animations and mobile bottom-sheet.
 * Compatible with admin live-preview (livePreview: true).
 *
 * Question Types (ExitSurveyQuestionType):
 *   0 | SINGLE_CHOICE  → Radyo butonları veya buton grubu
 *   1 | TEXT_AREA      → Açık uçlu yorum alanı
 *   2 | BOOLEAN        → Evet/Hayır butonları
 *   3 | NPS_RATING     → 0-10 arası veya 1-5 yıldız puanlama
 *   4 | MULTI_CHOICE   → Checkbox (Birden fazla seçim)
 *   5 | EMOJI_RATING   → Üzgün, Nötr, Mutlu yüz ifadeleri
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

    const uid = () => 'es_' + Date.now() + '_' + Math.random().toString(36).slice(2, 9);

    const STYLE_ID = 'pushonica-exit-survey-styles';

    /* ─────────── normalise question type ─────────── */
    function normalizeType(t) {
        if (typeof t === 'number') {
            return t;
        }
        const map = {
            // Type 0: SINGLE_CHOICE
            'single_choice': 0, 'singlechoice': 0, 'multiplechoice': 0,
            // Type 1: TEXT_AREA
            'text_area': 1, 'textarea': 1, 'text': 1,
            // Type 2: BOOLEAN
            'boolean': 2, 'yes_no': 2, 'yesno': 2,
            // Type 3: NPS_RATING
            'nps_rating': 3, 'npsrating': 3, 'rating': 3,
            // Type 4: MULTI_CHOICE
            'multi_choice': 4, 'multichoice': 4, 'checkbox': 4,
            // Type 5: EMOJI_RATING
            'emoji_rating': 5, 'emojirating': 5, 'emoji': 5
        };
        const normalized = map[String(t).toLowerCase()] ?? 0;
        return normalized;
    }

    /* ═══════════════════════════════════════════════════
       CLASS
    ═══════════════════════════════════════════════════ */
    class PushonicaExitSurvey {
        constructor(opts = {}) {
            this.options = opts;
            this.companyId = opts.companyId || this.getCompanyId();
            this.apiBaseUrl = opts.apiBaseUrl || 'https://pushadmin-production.up.railway.app';
            this.r2BaseUrl = opts.r2BaseUrl || 'https://assets.pushonica.com';
            this.livePreview = opts.livePreview || false;
            this.targetElement = opts.targetElement || document.body;
            this.config = null;
            this.style = {};
            this.questions = [];
            this.currentStep = 0;
            this.answers = {};
            this.hasShown = false;
            this.hasSubmitted = false;
            this.sessionId = uid();
            this.containerId = 'exitSurveyModal_' + uid();
            this.cookieKey = `pushonica_exit_survey_${this.companyId}`;
            this._boundMouseLeave = null;
            this._boundBeforeUnload = null;
            this._boundVisibilityChange = null;
            this._mobileDelayTimer = null;
            this.isInitialized = false;

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
            return null;
        }

        async loadConfigFromR2() {
            try {
                const configUrl = `${this.r2BaseUrl}/${this.companyId}/exitSurvey.json`;
                const response = await fetch(configUrl);
                if (!response.ok) throw new Error(`HTTP ${response.status}`);
                const data = await response.json();
                return data;
            } catch (error) {
                console.warn('[ExitSurvey] Config yüklenemedi:', error.message);
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

        /* ── config parser (supports nested & flat JSON) ── */
        _parseConfig(raw) {
            if (!raw) return;

            if (raw.config || raw.general) {
                const c = raw.config || raw;
                const gen = c.general || {};
                const sty = c.style || {};
                const tar = c.targeting || {};
                const qs = c.questions || [];

                this.config = {
                    companyId: raw.companyId || gen.companyId || c.companyId || '',
                    isActive: (gen.isActive ?? c.isActive) !== false,
                    title: gen.title || c.title || 'Ayrılıyor musunuz?',
                    description: gen.description || c.description || '',
                    logoUrl: gen.logoUrl || c.logoUrl || '',
                    thankYouMessage: gen.thankYouMessage || c.thankYouMessage || 'Geri bildiriminiz için teşekkürler!',
                    closeOnOverlayClick: (gen.closeOnOverlayClick ?? c.closeOnOverlayClick) !== false,
                    submitButtonText: sty.submitButtonText || c.submitButtonText || 'Gönder',
                    cancelButtonText: sty.cancelButtonText || c.cancelButtonText || 'Kapat',
                    showFrequencyDays: tar.showFrequencyDays || c.showFrequencyDays || 30,
                    delayBeforeShow: tar.delayBeforeShow ?? c.delayBeforeShow ?? 0,
                    enableAnimation: (c.enableAnimation ?? sty.enableAnimation) !== false,
                    showOnPageExit: (c.showOnPageExit ?? tar.showOnPageExit) !== false,
                    showOnTabClose: (c.showOnTabClose ?? tar.showOnTabClose) !== false,
                    showOnMobile: (c.showOnMobile ?? tar.showOnMobile) !== false,
                    showOnDesktop: (c.showOnDesktop ?? tar.showOnDesktop) !== false,
                    mobileDelayMinutes: tar.mobileDelayMinutes ?? c.mobileDelayMinutes ?? 5,
                    submitUrl: raw.submitUrl || `${this.apiBaseUrl}/api/ExitSurveyApi/submit`,
                    trackUrl: raw.trackUrl || `${this.apiBaseUrl}/api/ExitSurveyApi/track`
                };

                this.style = {
                    backgroundColor: sty.backgroundColor || '#ffffff',
                    textColor: sty.textColor || '#3e3c3c',
                    submitButtonColor: sty.submitButtonColor || '#4caf50',
                    cancelButtonColor: sty.cancelButtonColor || '#6c757d',
                    borderRadius: sty.borderRadius || '16px',
                    overlayColor: sty.overlayColor || 'rgba(0,0,0,0.6)',
                    fontFamily: sty.fontFamily || "Inter, system-ui, -apple-system, sans-serif"
                };

                this.questions = qs.map(q => ({
                    id: q.id,
                    step: q.step ?? q.id,
                    questionText: q.questionText || '',
                    questionType: normalizeType(q.questionType),
                    isRequired: q.isRequired || false,
                    options: (q.options || []).map(o =>
                        typeof o === 'string' ? { id: o, label: o, value: o } : o
                    ),
                    logic: q.logic || null
                }));
            } else {
                // Flat format (admin panel backward compat)
                this.config = {
                    companyId: raw.companyId || '',
                    isActive: raw.isActive !== false,
                    title: raw.title || 'Anket',
                    description: raw.description || '',
                    logoUrl: raw.logoUrl || '',
                    thankYouMessage: raw.thankYouMessage || 'Geri bildiriminiz için teşekkürler!',
                    closeOnOverlayClick: true,
                    submitButtonText: raw.submitButtonText || 'Gönder',
                    cancelButtonText: raw.cancelButtonText || 'Kapat',
                    showFrequencyDays: raw.showFrequencyDays || 30,
                    delayBeforeShow: raw.delayBeforeShow ?? 0,
                    enableAnimation: raw.enableAnimation !== false,
                    showOnPageExit: raw.showOnPageExit !== false,
                    showOnTabClose: raw.showOnTabClose !== false,
                    showOnMobile: raw.showOnMobile !== false,
                    showOnDesktop: raw.showOnDesktop !== false,
                    mobileDelayMinutes: raw.mobileDelayMinutes ?? 5,
                    submitUrl: raw.submitUrl || `${this.apiBaseUrl}/api/ExitSurveyApi/submit`,
                    trackUrl: raw.trackUrl || `${this.apiBaseUrl}/api/ExitSurveyApi/track`
                };

                this.style = {
                    backgroundColor: raw.backgroundColor || '#ffffff',
                    textColor: raw.textColor || '#3e3c3c',
                    submitButtonColor: raw.submitButtonColor || '#4caf50',
                    cancelButtonColor: raw.cancelButtonColor || '#6c757d',
                    borderRadius: raw.borderRadius || '16px',
                    overlayColor: raw.overlayColor || 'rgba(0,0,0,0.6)',
                    fontFamily: raw.fontFamily || "Inter, system-ui, -apple-system, sans-serif"
                };

                this.questions = (raw.questions || []).map(q => ({
                    id: q.id,
                    step: q.step ?? q.id,
                    questionText: q.questionText || '',
                    questionType: normalizeType(q.questionType),
                    isRequired: q.isRequired || false,
                    options: (q.options || []).map(o =>
                        typeof o === 'string' ? { id: o, label: o, value: o } : o
                    ),
                    logic: q.logic || null
                }));
            }
        }

        /* ── helpers ── */
        _isMobile() {
            return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)
                || (window.innerWidth <= 768);
        }

        /* ── init ── */
        async init() {
            if (this.isInitialized) return;

            try {
                // Config yoksa R2'den yükle
                if (!this.config && this.companyId) {
                    const r2Data = await this.loadConfigFromR2();
                    if (r2Data) {
                        this._parseConfig(r2Data);
                    }
                }

                if (!this.config || !this.config.isActive || this.questions.length === 0) return;

                this.isInitialized = true;

                if (this.livePreview) {
                    this._renderInline();
                    return;
                }

                // Cihaz kontrolü (mobil/masaüstü)
                const isMobile = this._isMobile();
                if (isMobile && !this.config.showOnMobile) return;
                if (!isMobile && !this.config.showOnDesktop) return;

                // Frekans kontrolü (cookie/localStorage)
                const cookieData = this._getCookieData();
                if (cookieData && cookieData.lastShown) {
                    const days = (Date.now() - cookieData.lastShown) / 864e5;
                    if (days < this.config.showFrequencyDays) return;
                }

                // Desktop: fare imleci sayfadan çıkınca (exit intent)
                if (!isMobile && this.config.showOnPageExit) {
                    this._boundMouseLeave = (e) => {
                        if (this.hasShown) return;
                        if (e.clientY <= 0) {
                            if (this.config.delayBeforeShow > 0) {
                                setTimeout(() => {
                                    if (!this.hasShown) this._showModal();
                                }, this.config.delayBeforeShow * 1000);
                            } else {
                                this._showModal();
                            }
                        }
                    };
                    document.addEventListener('mouseleave', this._boundMouseLeave);
                }

                // Sekme/tarayıcı kapatma: beforeunload
                // Site içi navigasyonlarda (aynı origin linklere tıklama, form submit vb.)
                // beforeunload tetiklenir ama anket gösterilmemelidir.
                if (this.config.showOnTabClose) {
                    this._isInternalNavigation = false;

                    // Tüm link tıklamalarını dinle — aynı origin ise flag'le
                    this._boundLinkClick = (e) => {
                        const anchor = e.target.closest('a[href]');
                        if (!anchor) return;
                        const href = anchor.getAttribute('href');
                        if (!href) return;
                        // javascript:, # ve boş href'leri atla
                        if (href.startsWith('javascript:') || href === '#' || href.startsWith('#')) return;
                        try {
                            const url = new URL(href, window.location.href);
                            if (url.origin === window.location.origin) {
                                this._isInternalNavigation = true;
                            }
                        } catch (_) { /* geçersiz URL, harici kabul et */ }
                    };
                    document.addEventListener('click', this._boundLinkClick, true);

                    // Form submit'leri de site içi navigasyon sayılır
                    this._boundFormSubmit = () => {
                        this._isInternalNavigation = true;
                    };
                    document.addEventListener('submit', this._boundFormSubmit, true);

                    this._boundBeforeUnload = (e) => {
                        if (this.hasShown || this.hasSubmitted) return;
                        if (this._isInternalNavigation) {
                            this._isInternalNavigation = false;
                            return; // site içi geçiş, anketi gösterme
                        }
                        this._showModal();
                        e.preventDefault();
                        e.returnValue = '';
                    };
                    window.addEventListener('beforeunload', this._boundBeforeUnload);
                }

                // Mobil: belirli süre sonra otomatik göster (mobileDelayMinutes dakika)
                if (isMobile && this.config.showOnMobile && this.config.mobileDelayMinutes > 0) {
                    this._mobileDelayTimer = setTimeout(() => {
                        if (!this.hasShown) this._showModal();
                    }, this.config.mobileDelayMinutes * 60 * 1000);
                }

                // Mobil: sayfadan ayrılıp geri dönünce göster (exit intent alternatifi)
                if (isMobile && this.config.showOnPageExit) {
                    let _leftAt = 0;
                    this._boundVisibilityChange = () => {
                        if (this.hasShown || this.hasSubmitted) return;
                        if (document.visibilityState === 'hidden') {
                            _leftAt = Date.now();
                        } else if (document.visibilityState === 'visible' && _leftAt > 0) {
                            const awayMs = Date.now() - _leftAt;
                            if (awayMs >= 3000) {
                                this._showModal();
                            }
                            _leftAt = 0;
                        }
                    };
                    document.addEventListener('visibilitychange', this._boundVisibilityChange);
                }
            } catch (error) {
                console.error('[ExitSurvey] Başlatma hatası:', error);
            }
        }

        _getCookieData() {
            return
            try {
                const data = localStorage.getItem(this.cookieKey);
                return data ? JSON.parse(data) : null;
            } catch { return null; }
        }

        _setCookieData(data) {
            try {
                localStorage.setItem(this.cookieKey, JSON.stringify(data));
            } catch { }
        }

        /* ═══════════════════════════════════════════
           INLINE PREVIEW (admin panel)
        ═══════════════════════════════════════════ */
        _renderInline() {
            if (!this.targetElement) return;
            this.targetElement.innerHTML = '';
            this._injectStyles();
            this.currentStep = 0;

            const wrap = document.createElement('div');
            wrap.className = 'pes-inline-wrap';
            wrap.innerHTML = this._buildContentHTML();
            this.targetElement.appendChild(wrap);
            this._bind(wrap);
        }

        /* ═══════════════════════════════════════════
           MODAL (production)
        ═══════════════════════════════════════════ */
        _showModal() {
            if (this.hasShown) return;
            this.hasShown = true;
            this.currentStep = 0;
            this._sendEvent('impression');
            this._injectStyles();

            // Anket gösterildiğinde beforeunload'ı kaldır (kullanıcı kapatabilsin)
            if (this._boundBeforeUnload) {
                window.removeEventListener('beforeunload', this._boundBeforeUnload);
                this._boundBeforeUnload = null;
            }

            // Mobil zamanlayıcıyı temizle
            if (this._mobileDelayTimer) {
                clearTimeout(this._mobileDelayTimer);
                this._mobileDelayTimer = null;
            }

            const modal = document.createElement('div');
            modal.id = this.containerId;
            modal.className = 'pes-modal' + (this.config.enableAnimation === false ? ' pes-no-animation' : '');
            modal.innerHTML = `
                <div class="pes-overlay"></div>
                <div class="pes-dialog">
                    ${this._buildContentHTML()}
                </div>
            `;
            document.body.appendChild(modal);

            if (this.config.enableAnimation === false) {
                modal.classList.add('pes-visible');
            } else {
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => modal.classList.add('pes-visible'));
                });
            }

            this._bind(modal);

            if (this.config.closeOnOverlayClick) {
                modal.querySelector('.pes-overlay').addEventListener('click', () => this._close());
            }
        }

        /* ═══════════════════════════════════════════
           HTML BUILDERS
        ═══════════════════════════════════════════ */
        _buildContentHTML() {
            const q = this.questions[this.currentStep];
            const step = this.currentStep;
            const total = this.questions.length;
            const pct = total > 1 ? ((step) / total) * 100 : 0;

            return `
                <div class="pes-card">
                    <button type="button" class="pes-close" aria-label="Kapat">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                    </button>

                    ${this.config.logoUrl ? `<div class="pes-logo"><img src="${esc(this.config.logoUrl)}" alt="logo"></div>` : ''}

                    <div class="pes-header">
                        <h3 class="pes-title">${esc(this.config.title)}</h3>
                        ${this.config.description ? `<p class="pes-desc">${esc(this.config.description)}</p>` : ''}
                    </div>

                    ${total > 1 ? `
                    <div class="pes-progress">
                        <div class="pes-progress-bar" style="width:${pct}%"></div>
                    </div>
                    <div class="pes-step-label">Soru ${step + 1} / ${total}</div>
                    ` : ''}

                    <div class="pes-question-area">
                        ${q ? this._buildQuestion(q) : ''}
                    </div>

                    <div class="pes-footer">
                        ${step > 0 ? `<button type="button" class="pes-btn pes-btn-back">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="15 18 9 12 15 6"></polyline></svg>
                            Geri
                        </button>` : `<span></span>`}
                        <button type="button" class="pes-btn pes-btn-next" ${q && q.isRequired ? 'disabled' : ''}>
                            ${step < total - 1 ? 'Devam <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"></polyline></svg>' : (this.config.submitButtonText || 'Gönder')}
                        </button>
                    </div>
                    <div class="pes-branding">
                        <a href="https://dijitaldesin.com" target="_blank" style="text-decoration: none; display: flex; align-items: center;">
                            <img src="https://dijitaldesin.com/wp-content/uploads/2023/02/dijitaldesin.svg" alt="Dijitaldesin" style="height: 24px;">
                        </a>
                    </div>
                </div>
            `;
        }

        _buildQuestion(q) {
            const type = q.questionType;

            let html = `<p class="pes-q-text">${esc(q.questionText)} ${q.isRequired ? '<span class="pes-req">*</span>' : ''}</p>`;

            switch (type) {
                case 0: // SINGLE_CHOICE → Card buttons
                    html += `<div class="pes-options" data-qid="${q.id}">`;
                    (q.options || []).forEach(opt => {
                        const val = typeof opt === 'string' ? opt : (opt.value || opt.label);
                        const label = typeof opt === 'string' ? opt : opt.label;
                        const selected = this.answers[q.id] === val;
                        html += `<button type="button" class="pes-opt-card ${selected ? 'pes-selected' : ''}" data-value="${esc(val)}">${esc(label)}</button>`;
                    });
                    html += `</div>`;
                    break;

                case 1: // TEXT_AREA → Textarea
                    html += `<textarea class="pes-textarea" data-qid="${q.id}" placeholder="Cevabınızı buraya yazın…" rows="4">${esc(this.answers[q.id] || '')}</textarea>`;
                    break;

                case 2: // BOOLEAN → Yes/No cards
                    html += `<div class="pes-options pes-yn" data-qid="${q.id}">
                        <button type="button" class="pes-opt-card ${this.answers[q.id] === 'Evet' ? 'pes-selected' : ''}" data-value="Evet">
                            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>
                            Evet
                        </button>
                        <button type="button" class="pes-opt-card ${this.answers[q.id] === 'Hayır' ? 'pes-selected' : ''}" data-value="Hayır">
                            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                            Hayır
                        </button>
                    </div>`;
                    break;

                case 3: { // NPS_RATING → 0-10 bar or 1-5 stars (configured via ratingStyle)
                    const ratingStyle = q.ratingStyle || 'nps'; // 'nps' or 'stars'

                    if (ratingStyle === 'stars') {
                        // 1-5 Stars
                        const cur = parseInt(this.answers[q.id]) || 0;
                        html += `<div class="pes-stars" data-qid="${q.id}">`;
                        for (let i = 1; i <= 5; i++) {
                            html += `<span class="pes-star ${i <= cur ? 'pes-star-on' : ''}" data-val="${i}">
                                <svg width="32" height="32" viewBox="0 0 24 24" fill="${i <= cur ? 'currentColor' : 'none'}" stroke="currentColor" stroke-width="1.5"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
                            </span>`;
                        }
                        html += `</div>`;
                    } else {
                        // NPS 0-10 bar (default)
                        const nps = this.answers[q.id] !== undefined ? parseInt(this.answers[q.id]) : -1;
                        html += `<div class="pes-nps" data-qid="${q.id}">`;
                        for (let i = 0; i <= 10; i++) {
                            const cls = i === nps ? 'pes-nps-btn pes-nps-active' : 'pes-nps-btn';
                            let color = '';
                            if (i <= 6) color = 'pes-nps-red';
                            else if (i <= 8) color = 'pes-nps-yellow';
                            else color = 'pes-nps-green';
                            html += `<button type="button" class="${cls} ${color}" data-val="${i}">${i}</button>`;
                        }
                        html += `<div class="pes-nps-labels"><span>Hiç olası değil</span><span>Kesinlikle öneririm</span></div>`;
                        html += `</div>`;
                    }
                    break;
                }

                case 4: { // MULTI_CHOICE → Multiple checkboxes
                    const selected = Array.isArray(this.answers[q.id]) ? this.answers[q.id] : [];
                    html += `<div class="pes-checkboxes" data-qid="${q.id}">`;
                    (q.options || []).forEach(opt => {
                        const val = typeof opt === 'string' ? opt : (opt.value || opt.label);
                        const label = typeof opt === 'string' ? opt : opt.label;
                        const checked = selected.includes(val);
                        html += `<label class="pes-checkbox-item ${checked ? 'pes-checked' : ''}">
                            <input type="checkbox" value="${esc(val)}" ${checked ? 'checked' : ''}>
                            <span class="pes-checkbox-icon">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>
                            </span>
                            <span class="pes-checkbox-label">${esc(label)}</span>
                        </label>`;
                    });
                    html += `</div>`;
                    break;
                }

                case 5: { // EMOJI_RATING → Emoji faces (Üzgün, Nötr, Mutlu)
                    const emojis = [
                        { value: '1', label: 'Üzgün', emoji: '😞', color: '#ef4444' },
                        { value: '2', label: 'Nötr', emoji: '😐', color: '#f59e0b' },
                        { value: '3', label: 'Mutlu', emoji: '😊', color: '#22c55e' }
                    ];
                    const cur = this.answers[q.id];
                    html += `<div class="pes-emojis" data-qid="${q.id}">`;
                    emojis.forEach(em => {
                        const active = cur === em.value;
                        html += `<button type="button" class="pes-emoji-btn ${active ? 'pes-emoji-active' : ''}" data-val="${em.value}" data-color="${em.color}">
                            <span class="pes-emoji-icon">${em.emoji}</span>
                            <span class="pes-emoji-label">${em.label}</span>
                        </button>`;
                    });
                    html += `</div>`;
                    break;
                }
            }

            return html;
        }

        _buildThankYou() {
            return `
                <div class="pes-card pes-thankyou">
                    <div class="pes-ty-icon">
                        <svg width="56" height="56" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
                    </div>
                    <h3 class="pes-title">${esc(this.config.thankYouMessage)}</h3>
                </div>
            `;
        }

        /* ═══════════════════════════════════════════
           EVENT BINDING
        ═══════════════════════════════════════════ */
        _bind(root) {
            const q = this.questions[this.currentStep];
            if (!q) return;

            // Close
            root.querySelectorAll('.pes-close').forEach(b => b.addEventListener('click', () => this._close()));

            // Option cards
            root.querySelectorAll('.pes-opt-card').forEach(card => {
                card.addEventListener('click', () => {
                    const parent = card.closest('.pes-options');
                    parent.querySelectorAll('.pes-opt-card').forEach(c => c.classList.remove('pes-selected'));
                    card.classList.add('pes-selected');
                    this.answers[q.id] = card.dataset.value;
                    this._enableNext(root);
                });
            });

            // Textarea
            root.querySelectorAll('.pes-textarea').forEach(ta => {
                ta.addEventListener('input', () => {
                    this.answers[q.id] = ta.value;
                    this._enableNext(root);
                });
            });

            // Stars
            root.querySelectorAll('.pes-star').forEach(star => {
                star.addEventListener('click', () => {
                    const val = parseInt(star.dataset.val);
                    this.answers[q.id] = val;
                    this._refreshContent(root);
                });
                star.addEventListener('mouseenter', () => {
                    const val = parseInt(star.dataset.val);
                    root.querySelectorAll('.pes-star').forEach((s, i) => {
                        s.classList.toggle('pes-star-hover', i < val);
                    });
                });
                star.addEventListener('mouseleave', () => {
                    root.querySelectorAll('.pes-star').forEach(s => s.classList.remove('pes-star-hover'));
                });
            });

            // NPS
            root.querySelectorAll('.pes-nps-btn').forEach(btn => {
                btn.addEventListener('click', () => {
                    const val = parseInt(btn.dataset.val);
                    this.answers[q.id] = val;
                    this._refreshContent(root);
                });
            });

            // Checkboxes (MULTI_CHOICE)
            root.querySelectorAll('.pes-checkbox-item input').forEach(cb => {
                cb.addEventListener('change', () => {
                    const parent = cb.closest('.pes-checkboxes');
                    const qid = parent.dataset.qid;
                    const checked = Array.from(parent.querySelectorAll('input:checked')).map(c => c.value);
                    this.answers[qid] = checked;
                    this._refreshContent(root);
                });
            });

            // Emoji Rating
            root.querySelectorAll('.pes-emoji-btn').forEach(btn => {
                btn.addEventListener('click', () => {
                    this.answers[q.id] = btn.dataset.val;
                    this._refreshContent(root);
                });
            });

            // Back
            root.querySelectorAll('.pes-btn-back').forEach(b => {
                b.addEventListener('click', () => {
                    if (this.currentStep > 0) {
                        this.currentStep--;
                        this._refreshContent(root);
                    }
                });
            });

            // Next / Submit
            root.querySelectorAll('.pes-btn-next').forEach(b => {
                b.addEventListener('click', () => {
                    if (b.disabled) return;
                    this._handleNext(root);
                });
            });
        }

        _enableNext(root) {
            const q = this.questions[this.currentStep];
            const btn = root.querySelector('.pes-btn-next');
            if (!btn || !q) return;

            if (!q.isRequired) {
                btn.disabled = false;
                return;
            }

            const ans = this.answers[q.id];
            const filled = ans !== undefined && ans !== null && ans !== '';
            btn.disabled = !filled;
        }

        _refreshContent(root) {
            const container = this.livePreview ? root : root.querySelector('.pes-dialog');
            if (!container) return;
            container.innerHTML = this._buildContentHTML();
            this._bind(root);
            this._enableNext(root);
        }

        _handleNext(root) {
            const q = this.questions[this.currentStep];
            if (!q) return;

            let nextStep = this.currentStep + 1;

            // Logic / branching
            if (q.logic) {
                const score = parseInt(this.answers[q.id]);
                const cond = q.logic.condition || '';
                let shouldJump = false;

                const match = cond.match(/score\s*([<>=!]+)\s*(\d+)/);
                if (match && !isNaN(score)) {
                    const op = match[1];
                    const val = parseInt(match[2]);
                    switch (op) {
                        case '<': shouldJump = score < val; break;
                        case '>': shouldJump = score > val; break;
                        case '<=': shouldJump = score <= val; break;
                        case '>=': shouldJump = score >= val; break;
                        case '==': shouldJump = score === val; break;
                        case '!=': shouldJump = score !== val; break;
                    }
                }

                if (shouldJump && q.logic.jumpToQuestionId) {
                    const jumpIdx = this.questions.findIndex(qq => qq.id === q.logic.jumpToQuestionId);
                    if (jumpIdx !== -1) nextStep = jumpIdx;
                }
            }

            if (nextStep >= this.questions.length) {
                this._submit(root);
            } else {
                this.currentStep = nextStep;
                this._refreshContent(root);
            }
        }

        /* ═══════════════════════════════════════════
           SUBMIT
        ═══════════════════════════════════════════ */
        _submit(root) {
            const responses = Object.entries(this.answers).map(([qId, answer]) => ({
                questionId: parseInt(qId) || qId,
                answer: String(answer)
            }));

            if (this.livePreview) {
                this._showThankYou(root);
                return;
            }

            this.hasSubmitted = true;
            this._sendEvent('submit');

            fetch(this.config.submitUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    companyId: this.config.companyId,
                    sessionId: this.sessionId,
                    responses,
                    pageUrl: location.href
                })
            })
                .then(r => { if (!r.ok) throw new Error('fail'); return r.json(); })
                .then(() => this._showThankYou(root))
                .catch(() => this._showThankYou(root));
        }

        _showThankYou(root) {
            const container = this.livePreview ? root : root.querySelector('.pes-dialog');
            if (container) container.innerHTML = this._buildThankYou();

            if (!this.livePreview) {
                setTimeout(() => this._close(), 3000);
            } else {
                setTimeout(() => {
                    this.currentStep = 0;
                    this.answers = {};
                    this._renderInline();
                }, 3000);
            }
        }

        /* ═══════════════════════════════════════════
           CLOSE / CLEANUP
        ═══════════════════════════════════════════ */
        _close() {
            if (!this.livePreview && !this.hasSubmitted) {
                this._sendEvent('dismiss');
            }

            const modal = document.getElementById(this.containerId);
            if (modal) {
                if (this.config && this.config.enableAnimation === false) {
                    modal.remove();
                } else {
                    modal.classList.remove('pes-visible');
                    setTimeout(() => modal.remove(), 350);
                }
            }

            // Tüm event listener'ı temizle
            if (this._boundMouseLeave) {
                document.removeEventListener('mouseleave', this._boundMouseLeave);
                this._boundMouseLeave = null;
            }
            if (this._boundBeforeUnload) {
                window.removeEventListener('beforeunload', this._boundBeforeUnload);
                this._boundBeforeUnload = null;
            }
            if (this._boundLinkClick) {
                document.removeEventListener('click', this._boundLinkClick, true);
                this._boundLinkClick = null;
            }
            if (this._boundFormSubmit) {
                document.removeEventListener('submit', this._boundFormSubmit, true);
                this._boundFormSubmit = null;
            }
            if (this._boundVisibilityChange) {
                document.removeEventListener('visibilitychange', this._boundVisibilityChange);
                this._boundVisibilityChange = null;
            }
            if (this._mobileDelayTimer) {
                clearTimeout(this._mobileDelayTimer);
                this._mobileDelayTimer = null;
            }

            this._markShown();
        }

        _detectPageType() {
            const url = (location.pathname + location.search).toLowerCase();
            const title = (document.title || '').toLowerCase();
            const og = (document.querySelector('meta[property="og:type"]')?.getAttribute('content') || '').toLowerCase();

            if (og === 'product' || url.match(/\/(product|urun|item|p)\b/) || document.querySelector('[data-product-id], .product-detail, .product-info, #product-detail'))
                return 'product';
            if (url.match(/\/(cart|sepet|basket)\b/) || document.querySelector('.cart-page, .basket-page, #cart, #sepet'))
                return 'cart';
            if (url.match(/\/(checkout|odeme|payment|siparis)\b/) || document.querySelector('.checkout-page, #checkout, .payment-step'))
                return 'checkout';
            if (url.match(/\/(category|kategori|koleksiyon|collection|list)\b/) || document.querySelector('.category-page, .collection-page, .product-list'))
                return 'category';
            if (url === '/' || url === '/index' || url === '/home' || url.match(/^\/?(index\.html?)?(\?.*)?$/))
                return 'homepage';

            return 'other';
        }

        _sendEvent(type) {
            if (this.livePreview) return;
            fetch(this.config.trackUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    companyId: this.config.companyId,
                    eventType: type,
                    pageUrl: location.href,
                    pageType: this._detectPageType(),
                    sessionId: this.sessionId
                })
            }).catch(() => { });
        }

        _markShown() {
            if (this.livePreview) return;
            this._setCookieData({ lastShown: Date.now(), shown: true });
        }

        /* ═══════════════════════════════════════════
           STYLES
        ═══════════════════════════════════════════ */
        _injectStyles() {
            const old = document.getElementById(STYLE_ID);
            if (old) old.remove();

            const s = this.style;
            const accent = s.submitButtonColor;
            const accentRGB = this._hexToRgb(accent);
            const tag = document.createElement('style');
            tag.id = STYLE_ID;
            tag.textContent = `
/* ─── PES Reset ─── */
.pes-modal,.pes-modal *,.pes-inline-wrap,.pes-inline-wrap *{box-sizing:border-box;margin:0;padding:0;font-family:${s.fontFamily};}

/* ─── Modal overlay ─── */
.pes-modal{position:fixed;inset:0;z-index:999999;display:flex;align-items:center;justify-content:center;opacity:0;visibility:hidden;transition:opacity .35s ease,visibility .35s ease;}
.pes-modal.pes-visible{opacity:1;visibility:visible;}
.pes-overlay{position:absolute;inset:0;background:${s.overlayColor};backdrop-filter:blur(6px);-webkit-backdrop-filter:blur(6px);}
.pes-dialog{position:relative;z-index:1;width:92%;max-width:520px;max-height:90vh;overflow-y:auto;transform:scale(.92) translateY(20px);opacity:0;transition:transform .4s cubic-bezier(.22,1,.36,1),opacity .35s ease;}
.pes-visible .pes-dialog{transform:scale(1) translateY(0);opacity:1;}

/* ─── Inline wrap ─── */
.pes-inline-wrap{width:100%;max-width:520px;margin:0 auto;}

/* ─── Card ─── */
.pes-card{background:${s.backgroundColor};color:${s.textColor};border-radius:${s.borderRadius};padding:2rem;box-shadow:0 25px 60px -12px rgba(0,0,0,.25);position:relative;}

/* ─── Close ─── */
.pes-close{position:absolute;top:1rem;right:1rem;background:none;border:none;cursor:pointer;color:${s.textColor};opacity:.45;transition:all .2s;width:36px;height:36px;display:flex;align-items:center;justify-content:center;border-radius:50%;}
.pes-close:hover{opacity:1;background:rgba(0,0,0,.06);}

/* ─── Logo ─── */
.pes-logo{text-align:center;margin-bottom:1rem;}
.pes-logo img{height:32px;object-fit:contain;}

/* ─── Header ─── */
.pes-header{text-align:center;margin-bottom:1.5rem;}
.pes-title{font-size:1.35rem;font-weight:700;line-height:1.3;color:${s.textColor};}
.pes-desc{font-size:.9rem;opacity:.65;margin-top:.5rem;line-height:1.5;color:${s.textColor};}

/* ─── Progress ─── */
.pes-progress{height:4px;background:rgba(0,0,0,.08);border-radius:99px;overflow:hidden;margin-bottom:.5rem;}
.pes-progress-bar{height:100%;background:${accent};border-radius:99px;transition:width .4s ease;}
.pes-step-label{text-align:center;font-size:.75rem;opacity:.45;margin-bottom:1.25rem;font-weight:500;}

/* ─── Question ─── */
.pes-question-area{margin-bottom:1.5rem;min-height:100px;}
.pes-q-text{font-size:1.05rem;font-weight:600;margin-bottom:1rem;line-height:1.4;text-align:center;}
.pes-req{color:#ef4444;font-weight:700;}

/* ─── Options (Card buttons) ─── */
.pes-options{display:flex;flex-direction:column;gap:.6rem;}
.pes-yn{flex-direction:row !important;gap:.75rem;}
.pes-yn .pes-opt-card{flex:1;}
.pes-opt-card{padding:.85rem 1.2rem;border:2px solid rgba(0,0,0,.1);border-radius:12px;background:transparent;cursor:pointer;font-size:.95rem;font-weight:500;color:${s.textColor};transition:all .2s ease;text-align:center;display:flex;align-items:center;justify-content:center;gap:.5rem;}
.pes-opt-card:hover{border-color:${accent};background:rgba(${accentRGB},.06);transform:translateY(-1px);}
.pes-opt-card.pes-selected{border-color:${accent};background:rgba(${accentRGB},.1);box-shadow:0 0 0 3px rgba(${accentRGB},.15);}

/* ─── Textarea ─── */
.pes-textarea{width:100%;padding:.9rem 1rem;border:2px solid rgba(0,0,0,.1);border-radius:12px;font-size:.95rem;font-family:inherit;resize:vertical;min-height:100px;color:${s.textColor};background:transparent;transition:all .2s;}
.pes-textarea:focus{outline:none;border-color:${accent};box-shadow:0 0 0 3px rgba(${accentRGB},.12);}

/* ─── Stars ─── */
.pes-stars{display:flex;justify-content:center;gap:.5rem;}
.pes-star{cursor:pointer;color:#d1d5db;transition:all .2s;display:inline-flex;}
.pes-star:hover,.pes-star.pes-star-hover{color:#facc15;transform:scale(1.15);}
.pes-star.pes-star-on{color:#f59e0b;}

/* ─── NPS ─── */
.pes-nps{display:flex;flex-wrap:wrap;gap:6px;justify-content:center;}
.pes-nps-btn{width:40px;height:40px;border-radius:10px;border:2px solid rgba(0,0,0,.1);background:transparent;cursor:pointer;font-size:.85rem;font-weight:600;color:${s.textColor};transition:all .2s;display:flex;align-items:center;justify-content:center;}
.pes-nps-btn:hover{transform:translateY(-2px);box-shadow:0 4px 12px rgba(0,0,0,.1);}
.pes-nps-btn.pes-nps-active{color:#fff !important;transform:translateY(-2px);box-shadow:0 4px 12px rgba(0,0,0,.15);}
.pes-nps-red:hover,.pes-nps-red.pes-nps-active{border-color:#ef4444;background:#ef4444;}
.pes-nps-yellow:hover,.pes-nps-yellow.pes-nps-active{border-color:#f59e0b;background:#f59e0b;}
.pes-nps-green:hover,.pes-nps-green.pes-nps-active{border-color:#22c55e;background:#22c55e;}
.pes-nps-labels{display:flex;justify-content:space-between;width:100%;font-size:.7rem;opacity:.45;margin-top:.35rem;padding:0 2px;font-weight:500;}

/* ─── Checkboxes (MULTI_CHOICE) ─── */
.pes-checkboxes{display:flex;flex-direction:column;gap:.65rem;}
.pes-checkbox-item{display:flex;align-items:center;gap:.75rem;padding:.85rem 1rem;border:2px solid rgba(0,0,0,.1);border-radius:12px;cursor:pointer;transition:all .2s;position:relative;}
.pes-checkbox-item input{position:absolute;opacity:0;pointer-events:none;}
.pes-checkbox-icon{width:22px;height:22px;border:2px solid rgba(0,0,0,.2);border-radius:6px;display:flex;align-items:center;justify-content:center;background:transparent;transition:all .2s;flex-shrink:0;}
.pes-checkbox-icon svg{opacity:0;transform:scale(.7);transition:all .2s;}
.pes-checkbox-item:hover{border-color:${accent};background:rgba(${accentRGB},.04);}
.pes-checkbox-item.pes-checked{border-color:${accent};background:rgba(${accentRGB},.08);}
.pes-checkbox-item.pes-checked .pes-checkbox-icon{background:${accent};border-color:${accent};}
.pes-checkbox-item.pes-checked .pes-checkbox-icon svg{opacity:1;transform:scale(1);stroke:#fff;}
.pes-checkbox-label{font-size:.95rem;font-weight:500;color:${s.textColor};}

/* ─── Emoji Rating ─── */
.pes-emojis{display:flex;gap:.75rem;justify-content:center;}
.pes-emoji-btn{padding:1rem;border:2px solid rgba(0,0,0,.1);border-radius:16px;background:transparent;cursor:pointer;transition:all .25s;display:flex;flex-direction:column;align-items:center;gap:.5rem;min-width:90px;}
.pes-emoji-icon{font-size:2.5rem;line-height:1;transition:transform .2s;}
.pes-emoji-label{font-size:.8rem;font-weight:600;color:${s.textColor};opacity:.7;}
.pes-emoji-btn:hover{transform:translateY(-4px);border-color:currentColor;}
.pes-emoji-btn:hover .pes-emoji-icon{transform:scale(1.1);}
.pes-emoji-btn.pes-emoji-active{border-width:3px;box-shadow:0 6px 20px rgba(0,0,0,.12);}
.pes-emoji-btn.pes-emoji-active .pes-emoji-label{opacity:1;}
.pes-emoji-btn[data-color="#ef4444"]:hover,.pes-emoji-btn[data-color="#ef4444"].pes-emoji-active{border-color:#ef4444;background:rgba(239,68,68,.08);}
.pes-emoji-btn[data-color="#f59e0b"]:hover,.pes-emoji-btn[data-color="#f59e0b"].pes-emoji-active{border-color:#f59e0b;background:rgba(245,158,11,.08);}
.pes-emoji-btn[data-color="#22c55e"]:hover,.pes-emoji-btn[data-color="#22c55e"].pes-emoji-active{border-color:#22c55e;background:rgba(34,197,94,.08);}

/* ─── Footer ─── */
.pes-footer{display:flex;justify-content:space-between;align-items:center;gap:.75rem;}
.pes-branding{display:flex;justify-content:center;align-items:center;margin-top:1rem;padding-top:.75rem;border-top:1px solid rgba(0,0,0,.06);}
.pes-branding a{opacity:.6;transition:opacity .2s;}
.pes-branding a:hover{opacity:1;}
.pes-btn{padding:.7rem 1.5rem;border-radius:10px;font-size:.9rem;font-weight:600;cursor:pointer;transition:all .25s;display:inline-flex;align-items:center;gap:.4rem;border:none;}
.pes-btn:disabled{opacity:.4;cursor:not-allowed;transform:none !important;}
.pes-btn-back{background:transparent;color:${s.textColor};opacity:.6;}
.pes-btn-back:hover{opacity:1;background:rgba(0,0,0,.05);}
.pes-btn-next{background:${accent};color:#fff;margin-left:auto;}
.pes-btn-next:hover:not(:disabled){transform:translateY(-2px);box-shadow:0 6px 20px rgba(${accentRGB},.35);}

/* ─── Thank You ─── */
.pes-thankyou{text-align:center;padding:3rem 2rem;}
.pes-ty-icon{color:#22c55e;margin-bottom:1.25rem;display:flex;justify-content:center;}
.pes-ty-icon svg{width:56px;height:56px;}
.pes-thankyou .pes-title{font-size:1.25rem;}

/* ─── Mobile ─── */
@media(max-width:640px){
    .pes-modal{align-items:flex-end;}
    .pes-dialog{width:100%;max-width:100%;max-height:85vh;border-radius:0;transform:translateY(100%);opacity:1;}
    .pes-visible .pes-dialog{transform:translateY(0);}
    .pes-card{border-radius:24px 24px 0 0;padding:1.5rem 1.25rem 2rem;}
    .pes-nps-btn{width:34px;height:34px;font-size:.8rem;border-radius:8px;}
    .pes-yn .pes-opt-card{padding:.75rem .5rem;font-size:.85rem;}
}

/* ─── No Animation ─── */
.pes-no-animation,.pes-no-animation *{transition:none !important;animation:none !important;}
.pes-no-animation.pes-modal{opacity:1;visibility:visible;}
.pes-no-animation .pes-dialog{transform:none;opacity:1;}
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
    }

    /* ═══════════════════════════════════════════
       GLOBAL EXPORTS
    ═══════════════════════════════════════════ */
    function initExitSurvey(widgetConfig) {
        new PushonicaExitSurvey({ config: widgetConfig });
    }

    function closeExitSurveyModal() {
        document.querySelectorAll('[id^="exitSurveyModal_"]').forEach(m => {
            m.classList.remove('pes-visible');
            setTimeout(() => m.remove(), 350);
        });
    }

    // Expose class globally for admin usage
    window.PushonicaExitSurvey = PushonicaExitSurvey;
    window.initExitSurvey = initExitSurvey;
    window.closeExitSurveyModal = closeExitSurveyModal;

    // Auto-init: if loaded on production site with data-module-id
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            window.pushonicaExitSurvey = new PushonicaExitSurvey();
        });
    } else {
        window.pushonicaExitSurvey = new PushonicaExitSurvey();
    }
})();
