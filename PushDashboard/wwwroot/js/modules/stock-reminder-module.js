/**
 * Pushonica Stock Reminder Module — Premium Edition
 * Stok hatırlatma widget'ı — beden seçimi + telefon/email toplama.
 * Admin live-preview (livePreview: true) ve production R2 deploy desteği.
 *
 * Kullanım (e-ticaret sitesinde):
 *   <script src="https://assets.pushonica.com/{companyId}/stockReminderWidget.js" data-module-id="{companyId}"></script>
 *   window.pushonicaStockReminder.show({ productId: '123', productName: 'Ürün Adı' });
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

    const uid = () => 'sr_' + Date.now() + '_' + Math.random().toString(36).slice(2, 9);

    const STYLE_ID = 'pushonica-stock-reminder-styles';

    /* ═══════════════════════════════════════════════════
       CLASS
    ═══════════════════════════════════════════════════ */
    class PushonicaStockReminder {
        constructor(opts = {}) {
            this.options = opts;
            this.companyId = opts.companyId || this.getCompanyId();
            this.apiBaseUrl = opts.apiBaseUrl || 'https://pushadmin-production.up.railway.app';
            this.r2BaseUrl = opts.r2BaseUrl || 'https://assets.pushonica.com';
            this.livePreview = opts.livePreview || false;
            this.targetElement = opts.targetElement || document.body;
            this.config = null;
            this.containerId = 'srWidget_' + uid();
            this.sessionId = uid();
            this.isVisible = false;
            this.isInitialized = false;

            // Current context
            this.currentProductId = null;
            this.currentProductName = null;
            this.currentProductUrl = null;
            this.selectedVariant = null;
            this.selectedVariantId = null;

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
                const configUrl = `${this.r2BaseUrl}/${this.companyId}/stockReminderWidget.json`;
                const response = await fetch(configUrl);
                if (!response.ok) throw new Error(`HTTP ${response.status}`);
                const data = await response.json();
                return data;
            } catch (error) {
                console.warn('[StockReminder] Config yüklenemedi:', error.message);
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
                title: c.title || 'Bedenini Seç, Stokta Olunca Sana Bildirelim!',
                description: c.description || 'Ürün stoğa girdiğinde size WhatsApp veya SMS ile hemen bilgi vereceğiz.',
                submitButtonText: c.submitButtonText || 'Kaydet',
                privacyText: c.privacyText || 'Kampanya, duyuru ve bilgilendirmelerden WhatsApp ve SMS yoluyla haberdar olmak istediğimi belirtiyorum.',
                privacyUrl: c.privacyUrl || '#',
                kvkkLink: c.kvkkLink || '',
                etkLink: c.etkLink || '',
                phonePlaceholder: c.phonePlaceholder || '5XXXXXXXXX',
                emailPlaceholder: c.emailPlaceholder || 'E-posta adresiniz (isteğe bağlı)',
                variantOptions: (c.variantOptions ? (Array.isArray(c.variantOptions) ? c.variantOptions : c.variantOptions.split(',')) : []).map(v => typeof v === 'string' ? v.trim() : v).filter(Boolean),
                variantLabel: c.variantLabel || 'Beden Seçin',
                showVariantSelector: true, // her zaman gösterilir, sayfadan dinamik toplanır
                showPhoneField: c.showPhoneField !== false,
                showEmailField: c.showEmailField === true,
                showNameFields: c.showNameFields === true,
                requirePhone: c.showPhoneField !== false, // seçili alanlar zorunlu
                requireEmail: c.showEmailField === true,   // seçili alanlar zorunlu
                requireName: c.showNameFields === true,    // seçili alanlar zorunlu
                showConsentCheckbox: true, // her zaman görünür
                backgroundColor: c.backgroundColor || '#ffffff',
                textColor: c.textColor || '#333333',
                titleColor: c.titleColor || '#e53935',
                buttonColor: c.buttonColor || '#e53935',
                buttonTextColor: c.buttonTextColor || '#ffffff',
                variantBorderColor: c.variantBorderColor || '#e5e7eb',
                variantSelectedColor: c.variantSelectedColor || '#111111',
                borderRadius: c.borderRadius || '16px',
                showOnMobile: c.showOnMobile !== false,
                showOnDesktop: c.showOnDesktop !== false,
                enableAnimation: c.enableAnimation !== false,
                submitUrl: raw.submitUrl || c.submitUrl || `${this.apiBaseUrl}/api/StockReminderWidgetApi/submit`,
                trackUrl: raw.trackUrl || c.trackUrl || `${this.apiBaseUrl}/api/StockReminderWidgetApi/track`
            };
        }

        /* ── helpers ── */
        _isMobile() {
            return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
        }

        _formatTRPhone(raw) {
            const digits = (raw || '').replace(/\D/g, '').slice(0, 11);
            let d = digits;
            if (d.length > 0 && d[0] === '5') d = ('0' + d).slice(0, 11);
            if (d.length >= 1 && d[0] !== '0') d = ('0' + d).slice(0, 11);
            if (d.length >= 2 && d.slice(0, 2) !== '05') d = d.slice(0, 1);
            const p1 = d.slice(0, 1), p2 = d.slice(1, 4), p3 = d.slice(4, 7), p4 = d.slice(7, 9), p5 = d.slice(9, 11);
            let out = p1;
            if (p2) out += ' ' + p2;
            if (p3) out += ' ' + p3;
            if (p4) out += ' ' + p4;
            if (p5) out += ' ' + p5;
            return out.trim();
        }

        _getTRPhoneDigits(formatted) {
            let d = (formatted || '').replace(/\D/g, '').slice(0, 11);
            if (d.length > 0 && d[0] === '5') d = ('0' + d).slice(0, 11);
            if (d.length >= 2 && d.slice(0, 2) !== '05') return '';
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

                // Platformun kendi "Gelince Haber Ver" butonunu gizle
                document.querySelectorAll('.UGelinceHaberVer').forEach(el => {
                    el.style.setProperty('display', 'none', 'important');
                });

                this._injectStyles();

                if (this.livePreview) {
                    this._renderInline();
                    return;
                }

                // Device check
                const isMobile = this._isMobile();
                if (isMobile && !this.config.showOnMobile) return;
                if (!isMobile && !this.config.showOnDesktop) return;

                this._injectSizeBoxBellIcons();
                this._startObserver();

                // Auto-inject trigger button on product page if out of stock
                if (!this.livePreview) {
                    this._injectTriggerButton();
                }

            } catch (error) {
                console.error('[StockReminder] Başlatma hatası:', error);
            }
        }

        /**
         * Eğer sayfa stok dışı bir ürün sayfasıysa (.RightDetail.StokYok mevcutsa),
         * "Stoğa Gelince Haber Ver" butonunu öncelik sırasına göre uygun yere enjekte eder:
         * 1. #divUrunEkSecenek altına
         * 2. #divStokYok altına
         * 3. .TopList altına (varsayılan)
         */
        _injectTriggerButton() {
            // Stok dışı kontrolü
            const stockOutEl = document.querySelector('.RightDetail.StokYok');
            const divStokYokEl = document.getElementById('divStokYok');
            const isStockOut = stockOutEl || (divStokYokEl && window.getComputedStyle(divStokYokEl).display !== 'none');
            
            if (!isStockOut) return;

            // Zaten enjekte edilmişse tekrarlama
            if (document.getElementById('psr-trigger-btn')) return;

            let targetEl = document.getElementById('divUrunEkSecenek');
            
            if (!targetEl) {
                targetEl = document.getElementById('divStokYok');
            }
            
            if (!targetEl) {
                targetEl = document.querySelector('.TopList');
            }

            if (!targetEl) return;

            // Sayfadan varyantları topla
            this._pendingVariants = this._collectPageVariants();
            this._applyPendingVariants();

            const accentColor = this.config.buttonColor || '#DD1818';

            const btn = document.createElement('div');
            btn.id = 'psr-trigger-btn';
            btn.setAttribute('role', 'button');
            btn.setAttribute('tabindex', '0');
            
            btn.style.cssText = `
                display:flex; font-size:14px; font-weight:600;
                background:#fff; color:${accentColor};
                border:1px solid ${accentColor};
                border-radius:5px; padding:8px;
                align-items:center; gap:10px; cursor:pointer;
                transition:all 0.3s ease; justify-content:center;
            `;
            btn.innerHTML = `
                <svg class="stock_bell_icon bell-animate" height="20" style="fill:${accentColor};pointer-events:none;user-select:none;" viewBox="0 0 512 512" width="20" xmlns="http://www.w3.org/2000/svg">
                  <g>
                    <path d="m437.017 39.687c-5.858-5.857-15.356-5.857-21.213 0-5.858 5.858-5.857 15.356 0 21.213 42.688 42.685 66.196 99.438 66.196 159.807 0 8.284 6.716 15 15 15s15-6.716 15-15c0-68.382-26.629-132.67-74.983-181.02z"></path>
                    <path d="m96.193 39.688c-5.858-5.857-15.355-5.858-21.213 0-48.352 48.354-74.98 112.641-74.98 181.019 0 8.284 6.716 15 15 15s15-6.716 15-15c0-60.365 23.508-117.118 66.193-159.805 5.858-5.858 5.858-15.356 0-21.214z"></path>
                    <path d="m370.358 85.125c-5.858 5.858-5.857 15.355 0 21.213 30.55 30.548 47.374 71.164 47.374 114.368 0 8.284 6.716 15 15 15s15-6.716 15-15c0-51.217-19.945-99.368-56.161-135.582-5.858-5.856-15.355-5.856-21.213.001z"></path>
                    <path d="m141.634 85.132c-5.858-5.857-15.356-5.857-21.213.001-36.211 36.216-56.154 84.363-56.154 135.573 0 8.284 6.716 15 15 15s15-6.716 15-15c0-43.197 16.823-83.811 47.369-114.361 5.857-5.858 5.856-15.355-.002-21.213z"></path>
                    <path d="m383.467 329.632v-76.792c0-28.011-8.918-54.59-25.79-76.865-14.101-18.617-33.332-33.16-54.939-41.717-3.007-23.111-22.82-41.018-46.738-41.018s-43.733 17.906-46.74 41.018c-21.606 8.558-40.837 23.101-54.938 41.717-16.872 22.275-25.79 48.854-25.79 76.865v76.792l-30.549 61.1c-2.325 4.65-2.076 10.172.657 14.594s7.561 7.114 12.76 7.114h66.774c7.033 36.558 39.246 64.267 77.825 64.267 38.578 0 70.791-27.709 77.823-64.267h66.778c5.199 0 10.027-2.692 12.76-7.114s2.981-9.944.657-14.594zm-127.467 117.075c-21.938 0-40.573-14.413-46.933-34.267h93.865c-6.36 19.854-24.994 34.267-46.932 34.267zm64.266-64.267h-128.533-56.062l21.279-42.559c1.042-2.083 1.583-4.379 1.583-6.708v-80.333c0-42.793 28.625-81.203 69.612-93.405 6.36-1.894 10.72-7.741 10.72-14.376v-4.686c0-9.447 7.687-17.133 17.135-17.133 9.447 0 17.133 7.686 17.133 17.133v4.686c0 6.636 4.36 12.483 10.72 14.376 40.987 12.202 69.614 50.611 69.614 93.405v80.333c0 2.329.542 4.625 1.583 6.708l21.279 42.559z"></path>
                  </g>
                </svg>
                Stoğa Gelince Haber Ver
            `;

            // Hover efektleri
            btn.addEventListener('mouseenter', () => {
                btn.style.background = accentColor;
                btn.style.color = '#fff';
                btn.querySelector('svg').style.fill = '#fff';
            });
            btn.addEventListener('mouseleave', () => {
                btn.style.background = '#fff';
                btn.style.color = accentColor;
                btn.querySelector('svg').style.fill = accentColor;
            });

            // Tıklama → modal aç (productId'yi window.urunKartID'den al)
            btn.addEventListener('click', () => {
                this.currentProductId = (typeof window !== 'undefined' && window.urunKartID) || null;
                this._showPopup();
            });
            btn.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    this.currentProductId = (typeof window !== 'undefined' && window.urunKartID) || null;
                    this._showPopup();
                }
            });

            if (targetEl.parentNode) {
                const spacer = document.createElement('div');
                spacer.style.height = '15px';
                spacer.style.width = '100%';
                spacer.style.clear = 'both';
                targetEl.parentNode.insertBefore(spacer, targetEl.nextSibling);

                // Sonra butonu ekleyelim
                targetEl.parentNode.insertBefore(btn, spacer.nextSibling);
            }
        }

        _startObserver() {
            if (this._observer) return;
            this._observer = new MutationObserver((mutations) => {
                let shouldCheck = false;
                for (let m of mutations) {
                    if (m.type === 'childList' && m.addedNodes.length > 0) {
                        shouldCheck = true;
                        break;
                    }
                }
                if (shouldCheck) {
                    this._injectSizeBoxBellIcons();
                    // Dinamik eklenen .UGelinceHaberVer butonlarını da gizle
                    document.querySelectorAll('.UGelinceHaberVer').forEach(el => {
                        el.style.setProperty('display', 'none', 'important');
                    });
                }
            });
            this._observer.observe(document.body, { childList: true, subtree: true });
        }

        /**
         * Her .size_box.nostok elemanının içine bell ikonu ekler,
         * .btnnostok elemanlarını kaldırır. Bell ikonuna tıklanınca modal açılır.
         */
        _injectSizeBoxBellIcons() {
            const boxes = document.querySelectorAll('.size_box.nostok');
            if (!boxes || boxes.length === 0) return;

            boxes.forEach(box => {
                const btnNostok = box.querySelector('.btnnostok');
                if (btnNostok) btnNostok.remove();

                if (box.querySelector('.psr-nostok-bell')) return;

                const bellBtn = document.createElement('span');
                bellBtn.className = 'psr-nostok-bell';
                bellBtn.setAttribute('title', 'Stoğa gelince haber ver');
                bellBtn.innerHTML = `<svg class="stock_bell_icon" height="16" style="fill: #DD1818; pointer-events: none; user-select: none;" viewBox="0 0 512 512" width="16" xmlns="http://www.w3.org/2000/svg">
                  <g>
                    <path d="m437.017 39.687c-5.858-5.857-15.356-5.857-21.213 0-5.858 5.858-5.857 15.356 0 21.213 42.688 42.685 66.196 99.438 66.196 159.807 0 8.284 6.716 15 15 15s15-6.716 15-15c0-68.382-26.629-132.67-74.983-181.02z"></path>
                    <path d="m96.193 39.688c-5.858-5.857-15.355-5.858-21.213 0-48.352 48.354-74.98 112.641-74.98 181.019 0 8.284 6.716 15 15 15s15-6.716 15-15c0-60.365 23.508-117.118 66.193-159.805 5.858-5.858 5.858-15.356 0-21.214z"></path>
                    <path d="m370.358 85.125c-5.858 5.858-5.857 15.355 0 21.213 30.55 30.548 47.374 71.164 47.374 114.368 0 8.284 6.716 15 15 15s15-6.716 15-15c0-51.217-19.945-99.368-56.161-135.582-5.858-5.856-15.355-5.856-21.213.001z"></path>
                    <path d="m141.634 85.132c-5.858-5.857-15.356-5.857-21.213.001-36.211 36.216-56.154 84.363-56.154 135.573 0 8.284 6.716 15 15 15s15-6.716 15-15c0-43.197 16.823-83.811 47.369-114.361 5.857-5.858 5.856-15.355-.002-21.213z"></path>
                    <path d="m383.467 329.632v-76.792c0-28.011-8.918-54.59-25.79-76.865-14.101-18.617-33.332-33.16-54.939-41.717-3.007-23.111-22.82-41.018-46.738-41.018s-43.733 17.906-46.74 41.018c-21.606 8.558-40.837 23.101-54.938 41.717-16.872 22.275-25.79 48.854-25.79 76.865v76.792l-30.549 61.1c-2.325 4.65-2.076 10.172.657 14.594s7.561 7.114 12.76 7.114h66.774c7.033 36.558 39.246 64.267 77.825 64.267 38.578 0 70.791-27.709 77.823-64.267h66.778c5.199 0 10.027-2.692 12.76-7.114s2.981-9.944.657-14.594zm-127.467 117.075c-21.938 0-40.573-14.413-46.933-34.267h93.865c-6.36 19.854-24.994 34.267-46.932 34.267zm64.266-64.267h-128.533-56.062l21.279-42.559c1.042-2.083 1.583-4.379 1.583-6.708v-80.333c0-42.793 28.625-81.203 69.612-93.405 6.36-1.894 10.72-7.741 10.72-14.376v-4.686c0-9.447 7.687-17.133 17.135-17.133 9.447 0 17.133 7.686 17.133 17.133v4.686c0 6.636 4.36 12.483 10.72 14.376 40.987 12.202 69.614 50.611 69.614 93.405v80.333c0 2.329.542 4.625 1.583 6.708l21.279 42.559z"></path>
                  </g>
                </svg>`;

                // Tıklama → modal aç
                const openModal = (e) => {
                    e.stopPropagation();
                    e.preventDefault();

                    const clone = box.cloneNode(true);
                    const b = clone.querySelector('.psr-nostok-bell');
                    if (b) b.remove();
                    const btn = clone.querySelector('.btnnostok');
                    if (btn) btn.remove();

                    const variantText = (clone.textContent || clone.innerText || '').trim();
                    const rel = box.getAttribute('rel') || '';
                    let variantId = '';

                    // Önce kombin ürün kontrolü yap (rel attribute yok)
                    const kombInfo = this._getKombinVariantInfo(box);
                    if (kombInfo) {
                        // Kombin ürün: productId ve variantId'yi model'den al
                        this.currentProductId = kombInfo.productId;
                        this.currentProductName = kombInfo.productName;
                        variantId = kombInfo.variantId;

                        // Kombin ürün için sadece ilgili ürünün stok dışı varyantlarını topla
                        this._pendingVariants = this._collectKombinProductVariants(kombInfo.productId);
                    } else if (rel && typeof window !== 'undefined' && window.productDetailModel && window.productDetailModel.productVariantData) {
                        // Normal ürün: rel attribute üzerinden eşleştir
                        const variantData = window.productDetailModel.productVariantData.find(v => v.id == rel);
                        if (variantData && variantData.urunID) {
                            variantId = variantData.urunID;
                        }
                        this.currentProductId = (typeof window !== 'undefined' && window.urunKartID) || null;
                        this._pendingVariants = this._collectPageVariants();
                    } else {
                        this.currentProductId = (typeof window !== 'undefined' && window.urunKartID) || null;
                        this._pendingVariants = this._collectPageVariants();
                    }

                    this._applyPendingVariants();

                    this.selectedVariant = variantText;
                    this.selectedVariantId = variantId;
                    this._showPopup();
                };

                box.style.cursor = 'pointer';
                box.addEventListener('click', openModal);
                box.addEventListener('keydown', (e) => {
                    if (e.key === 'Enter' || e.key === ' ') openModal(e);
                });

                box.appendChild(bellBtn);
            });
        }

        /* ── Kombin ürün yardımcıları ── */

        /**
         * Kombin ürünlerdeki size_box elementleri rel attribute taşımaz.
         * Bu metod, size_box'ın text içeriğini (ör: "S", "M", "L") kullanarak
         * window.ProductCombine.Model üzerinden variantId ve productId bilgisini döner.
         * @param {HTMLElement} sizeBoxEl - .size_box elementi
         * @returns {{ productId: string, variantId: string, productName: string } | null}
         */
        _getKombinVariantInfo(sizeBoxEl) {
            if (typeof window === 'undefined' || !window.ProductCombine || !window.ProductCombine.Model) return null;

            const kombEkSecenek = sizeBoxEl.closest('.divKombinUrunEkSecenek[data-productid]');
            if (!kombEkSecenek) return null;

            const productId = kombEkSecenek.getAttribute('data-productid');
            if (!productId) return null;

            // size_box text'ini temizle (bell ikonu vs. hariç)
            const clone = sizeBoxEl.cloneNode(true);
            const bell = clone.querySelector('.psr-nostok-bell');
            if (bell) bell.remove();
            const btn = clone.querySelector('.btnnostok');
            if (btn) btn.remove();
            const sizeText = (clone.textContent || clone.innerText || '').trim();

            const model = window.ProductCombine.Model;
            const product = (model.products || []).find(p => String(p.productId) === String(productId));
            if (!product) return null;

            // variants dizisinde variantProperty veya variantPropertyDetail[].name ile eşleştir
            let variantId = '';
            for (const v of (product.variants || [])) {
                if (v.variantProperty === sizeText) {
                    variantId = String(v.variantId || '');
                    break;
                }
                const detail = (v.variantPropertyDetail || []).find(d => d.name === sizeText);
                if (detail) {
                    variantId = String(detail.variantId || v.variantId || '');
                    break;
                }
            }

            // selectVariantTypes üzerinden de dene
            if (!variantId) {
                for (const svt of (product.selectVariantTypes || [])) {
                    const val = (svt.selectVariantTypeValues || []).find(sv => sv.name === sizeText);
                    if (val) {
                        variantId = String(val.variantId || '');
                        break;
                    }
                }
            }

            return {
                productId: String(productId),
                variantId,
                productName: product.productName || ''
            };
        }

        /* ── Public API ── */
        /**
         * Belirli bir ürün için stok hatırlatma popupını gösterir.
         * @param {object} opts - { productId, productName, productUrl }
         */
        show(opts = {}) {
            this.currentProductId = opts.productId || (typeof window !== 'undefined' && window.urunKartID) || null;
            this.currentProductName = opts.productName || null;
            this.currentProductUrl = opts.productUrl || location.href;
            this.selectedVariant = null;
            this.selectedVariantId = null;

            // Variants passed directly via opts take priority
            if (opts.variants && Array.isArray(opts.variants) && opts.variants.length > 0) {
                this._pendingVariants = opts.variants;
            } else {
                // Collect out-of-stock size boxes dynamically from the product page
                this._pendingVariants = this._collectPageVariants();
            }

            if (!this.isInitialized) {
                this.init().then(() => {
                    if (this.config && this.config.isActive) {
                        this._applyPendingVariants();
                        this._showPopup();
                    }
                });
            } else if (this.config && this.config.isActive) {
                this._applyPendingVariants();
                this._showPopup();
            }
        }

        /**
         * Sayfadaki .size_box.nostok elementlerinin metin içeriklerini ve rel (variantId) değerlerini toplayarak
         * stok dışı beden listesini döner. Kombin ürünler hariç — sadece ana ürün varyantları.
         * @returns {Array<{text: string, variantId: string}>}
         */
        _collectPageVariants() {
            // Kombin ürün alanındaki size_box'ları hariç tut
            const els = document.querySelectorAll('.size_box.nostok');
            if (!els || els.length === 0) return [];
            const variants = [];
            const seen = new Set();
            els.forEach(el => {
                // Kombin ürün içindeki size_box'ları atla
                if (el.closest('.kombinItem') || el.closest('.divKombinUrunEkSecenek')) return;

                const clone = el.cloneNode(true);
                const bell = clone.querySelector('.psr-nostok-bell');
                if (bell) bell.remove();
                const btn = clone.querySelector('.btnnostok');
                if (btn) btn.remove();

                const text = (clone.textContent || clone.innerText || '').trim();
                const rel = el.getAttribute('rel') || '';
                let variantId = '';

                if (rel && typeof window !== 'undefined' && window.productDetailModel && window.productDetailModel.productVariantData) {
                    const variantData = window.productDetailModel.productVariantData.find(v => v.id == rel);
                    if (variantData && variantData.urunID) {
                        variantId = variantData.urunID;
                    }
                }

                if (text && !seen.has(text)) {
                    seen.add(text);
                    variants.push({ text, variantId });
                }
            });
            return variants;
        }

        /**
         * Kombin ürünlerden belirli bir productId'nin stok dışı varyantlarını toplar.
         * window.ProductCombine.Model üzerinden varyant bilgilerini text content eşlemesiyle alır.
         * @param {string} productId - Kombin ürün productId'si
         * @returns {Array<{text: string, variantId: string}>}
         */
        _collectKombinProductVariants(productId) {
            if (typeof window === 'undefined' || !window.ProductCombine || !window.ProductCombine.Model) return [];

            const model = window.ProductCombine.Model;
            const product = (model.products || []).find(p => String(p.productId) === String(productId));
            if (!product) return [];

            const variants = [];
            const seen = new Set();

            // selectVariantTypes üzerinden stok dışı varyantları topla
            for (const svt of (product.selectVariantTypes || [])) {
                for (const val of (svt.selectVariantTypeValues || [])) {
                    if (val.stockAmount <= 0 && val.name && !seen.has(val.name)) {
                        seen.add(val.name);
                        variants.push({
                            text: val.name,
                            variantId: String(val.variantId || '')
                        });
                    }
                }
            }

            // selectVariantTypes yoksa variants dizisinden dene
            if (variants.length === 0) {
                for (const v of (product.variants || [])) {
                    if (v.stockNumber <= 0 && v.variantProperty && !seen.has(v.variantProperty)) {
                        seen.add(v.variantProperty);
                        variants.push({
                            text: v.variantProperty,
                            variantId: String(v.variantId || '')
                        });
                    }
                }
            }

            return variants;
        }

        /**
         * Normalizes variant to { text, variantId } object.
         * Supports both string and object inputs for backward compatibility.
         */
        _normalizeVariant(v) {
            if (typeof v === 'string') return { text: v, variantId: '' };
            return { text: v.text || '', variantId: v.variantId || '' };
        }

        /**
         * Eğer sayfadan toplanan varyantlar varsa, config'e uygular.
         */
        _applyPendingVariants() {
            if (this._pendingVariants && this._pendingVariants.length > 0 && this.config) {
                this.config.variantOptions = this._pendingVariants.map(v => this._normalizeVariant(v));
                this.config.showVariantSelector = true;
            }
        }

        /* ═══════════════════════════════════════════
           INLINE PREVIEW (admin panel)
        ═══════════════════════════════════════════ */
        _renderInline() {
            if (!this.targetElement) return;
            this.targetElement.innerHTML = '';
            this._injectStyles();

            const wrap = document.createElement('div');
            wrap.className = 'psr-inline-wrap';
            wrap.innerHTML = this._buildCardHTML();
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
            overlay.className = 'psr-overlay' + (this.config.enableAnimation ? ' psr-animate' : '');

            overlay.innerHTML = `<div class="psr-dialog">${this._buildCardHTML()}</div>`;
            document.body.appendChild(overlay);

            if (this.config.enableAnimation) {
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => overlay.classList.add('psr-visible'));
                });
            } else {
                overlay.classList.add('psr-visible');
            }

            overlay.addEventListener('click', (e) => {
                if (e.target === overlay) this._dismiss();
            });

            this._bindEvents(overlay);
            this._sendEvent('impression');
        }

        /* ═══════════════════════════════════════════
           HTML BUILDER
        ═══════════════════════════════════════════ */
        _buildCardHTML() {
            const c = this.config;
            const bg = c.backgroundColor;
            const txt = c.textColor;
            const btnBg = c.buttonColor;
            const btnTxt = c.buttonTextColor;
            const titleClr = c.titleColor;
            const radius = c.borderRadius;

            let html = `<div class="psr-card" style="background:${bg};color:${txt};border-radius:${radius};">`;

            // Close button
            html += `<button type="button" class="psr-close" aria-label="Kapat">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="${txt}" stroke-width="2.5">
                    <path d="M18 6L6 18M6 6l12 12"/>
                </svg>
            </button>`;

            // Title
            html += `<h3 class="psr-title" style="color:${titleClr};">${esc(c.title)}</h3>`;

            // Variant selector — her zaman göster (variantlar sayfadan toplanıyor)
            if (c.variantOptions && c.variantOptions.length > 0) {
                html += `<div class="psr-variants" role="group" aria-label="${esc(c.variantLabel)}">`;
                c.variantOptions.forEach(v => {
                    const vObj = this._normalizeVariant(v);
                    const isSelected = this.selectedVariant && this.selectedVariant === vObj.text;
                    const borderClr = isSelected ? c.variantSelectedColor : c.variantBorderColor;
                    const bgClr = isSelected ? c.variantSelectedColor : 'transparent';
                    const textClr = isSelected ? '#ffffff' : txt;
                    const fwClr = isSelected ? 'font-weight:600;' : '';
                    const selClass = isSelected ? ' psr-variant-selected' : '';
                    html += `<button type="button" class="psr-variant-btn${selClass}" data-variant="${esc(vObj.text)}" data-variant-id="${esc(vObj.variantId)}"
                        style="border-color:${borderClr};background:${bgClr};color:${textClr};${fwClr}">${esc(vObj.text)}</button>`;
                });
                html += `</div>
                <div class="psr-error psr-variant-error" style="display:none;">Lütfen bir beden seçin.</div>`;
            }

            // Description
            if (c.description) {
                html += `<p class="psr-desc" style="color:${txt};">${esc(c.description)}</p>`;
            }

            // Form
            html += `<form class="psr-form" onsubmit="return false;">`;

            if (c.showNameFields) {
                html += `<div class="psr-name-row">
                <input type="text" class="psr-input psr-input-half" data-field="firstName" placeholder="Ad\u0131n\u0131z" autocomplete="given-name">
                <input type="text" class="psr-input psr-input-half" data-field="lastName" placeholder="Soyad\u0131n\u0131z" autocomplete="family-name">
            </div>
            <div class="psr-error psr-name-error" style="display:none;">
                Lütfen ad ve soyad alanlarını doldurun.
            </div>`;
            }

            // Phone field
            if (c.showPhoneField) {
                html += `<input type="tel" class="psr-input" data-field="phone" placeholder="${esc(c.phonePlaceholder)}"
                    inputmode="numeric" autocomplete="tel" maxlength="15">
                <div class="psr-error psr-phone-error" style="display:none;">
                    Telefon numarası <b>05</b> ile başlamalı ve <b>11 hane</b> olmalıdır.
                </div>`;
            }

            // Email field
            if (c.showEmailField) {
                html += `<input type="email" class="psr-input" data-field="email" placeholder="${esc(c.emailPlaceholder)}"
                    autocomplete="email">
                <div class="psr-error psr-email-error" style="display:none;">
                    Geçerli bir e-posta adresi girin.
                </div>`;
            }

            // Consent checkbox — her zaman görünür
            {
                const kvkkPart = `<a href="${esc(c.kvkkLink) || '#'}" target="_blank" style="color:${txt};text-decoration:underline;">KVKK Aydınlatma Metni</a>`;
                const etkPart = `<a href="${esc(c.etkLink) || '#'}" target="_blank" style="color:${txt};text-decoration:underline;">Ticari Elektronik İleti İzni</a>`;
                const privacyHtml = `${kvkkPart} ve ${etkPart}'ni okudum, kabul ediyorum.`;
                html += `<div class="psr-consent">
                    <input type="checkbox" class="psr-consent-checkbox">
                    <span class="psr-consent-text" style="color:${txt};">${privacyHtml}</span>
                </div>`;
            }

            html += `<button type="submit" class="psr-btn psr-btn-submit" style="background:${btnBg};color:${btnTxt};">
                ${esc(c.submitButtonText)}
            </button>`;

            html += `</form>`;

            // Success state
            html += `<div class="psr-success" style="display:none;">
                <div class="psr-success-icon">
                    <svg viewBox="0 0 24 24" fill="none" stroke="${btnBg}" stroke-width="2">
                        <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                        <polyline points="22 4 12 14.01 9 11.01"/>
                    </svg>
                </div>
                <h4 class="psr-success-title" style="color:${txt};">Kaydedildi!</h4>
                <p class="psr-success-desc" style="color:${txt};">Ürün stoğa girdiğinde sizi bilgilendireceğiz.</p>
            </div>`;

            // Branding
            html += `<div class="psr-branding">
                <span style="opacity:.75;font-size:11px;color:${txt};">
                    <a href="https://dijitaldesin.com" target="_blank" style="text-decoration:none;display:flex;align-items:center;gap:4px;">
                        <img src="https://dijitaldesin.com/wp-content/uploads/2023/02/dijitaldesin.svg" alt="Dijitaldesin" style="height:20px;">
                    </a>
                </span>
            </div>`;

            html += `</div>`;
            return html;
        }

        /* ═══════════════════════════════════════════
           EVENT BINDING
        ═══════════════════════════════════════════ */
        _bindEvents(root) {
            // Close button
            root.querySelectorAll('.psr-close').forEach(b =>
                b.addEventListener('click', () => this._dismiss())
            );

            // Variant buttons
            root.querySelectorAll('.psr-variant-btn').forEach(btn => {
                btn.addEventListener('click', () => {
                    root.querySelectorAll('.psr-variant-btn').forEach(b => {
                        b.classList.remove('psr-variant-selected');
                        b.style.borderColor = this.config.variantBorderColor;
                        b.style.background = '';
                        b.style.color = this.config.textColor;
                        b.style.fontWeight = '';
                    });
                    btn.classList.add('psr-variant-selected');
                    btn.style.borderColor = this.config.variantSelectedColor;
                    btn.style.background = this.config.variantSelectedColor;
                    btn.style.color = '#ffffff';
                    btn.style.fontWeight = '600';
                    this.selectedVariant = btn.getAttribute('data-variant');
                    this.selectedVariantId = btn.getAttribute('data-variant-id') || '';

                    const err = root.querySelector('.psr-variant-error');
                    if (err) err.style.display = 'none';
                });
            });

            // Name fields events
            const firstNameEl = root.querySelector('[data-field="firstName"]');
            const lastNameEl = root.querySelector('[data-field="lastName"]');
            const nameErrEl = root.querySelector('.psr-name-error');

            if (firstNameEl) {
                firstNameEl.addEventListener('input', () => {
                    firstNameEl.style.borderColor = '';
                    if (nameErrEl) nameErrEl.style.display = 'none';
                });
            }
            if (lastNameEl) {
                lastNameEl.addEventListener('input', () => {
                    lastNameEl.style.borderColor = '';
                    if (nameErrEl) nameErrEl.style.display = 'none';
                });
            }

            // Phone input formatting
            const phoneEl = root.querySelector('[data-field="phone"]');
            if (phoneEl) {
                phoneEl.addEventListener('input', () => {
                    phoneEl.value = this._formatTRPhone(phoneEl.value);
                    const errEl = root.querySelector('.psr-phone-error');
                    if (errEl) errEl.style.display = 'none';
                    phoneEl.style.borderColor = '';
                });
                phoneEl.addEventListener('paste', (e) => {
                    e.preventDefault();
                    const text = (e.clipboardData || window.clipboardData).getData('text') || '';
                    phoneEl.value = this._formatTRPhone(text);
                });
            }

            // Form submit
            const form = root.querySelector('.psr-form');
            if (form) {
                form.addEventListener('submit', (e) => {
                    e.preventDefault();
                    this._handleSubmit(root);
                });
            }
        }

        _handleSubmit(root) {
            const c = this.config;

            // Validate variant
            if (c.variantOptions && c.variantOptions.length > 0 && !this.selectedVariant) {
                const err = root.querySelector('.psr-variant-error');
                if (err) err.style.display = 'block';
                return;
            }

            // Validate name (eğer gösteriliyorsa zorunlu)
            let firstNameValue = '';
            let lastNameValue = '';
            if (c.showNameFields) {
                const firstNameEl = root.querySelector('[data-field="firstName"]');
                const lastNameEl = root.querySelector('[data-field="lastName"]');
                const errEl = root.querySelector('.psr-name-error');

                firstNameValue = (firstNameEl ? firstNameEl.value.trim() : '');
                lastNameValue = (lastNameEl ? lastNameEl.value.trim() : '');
                
                let hasError = false;
                if (!firstNameValue) {
                    if (firstNameEl) firstNameEl.style.borderColor = '#ef4444';
                    hasError = true;
                } else if (firstNameEl) {
                    firstNameEl.style.borderColor = '';
                }

                if (!lastNameValue) {
                    if (lastNameEl) lastNameEl.style.borderColor = '#ef4444';
                    hasError = true;
                } else if (lastNameEl) {
                    lastNameEl.style.borderColor = '';
                }

                if (hasError) {
                    if (errEl) errEl.style.display = 'block';
                    return;
                } else if (errEl) {
                    errEl.style.display = 'none';
                }
            }

            // Validate phone (seçiliyse zorunlu)
            let phoneDigits = '';
            if (c.showPhoneField) {
                const inputEl = root.querySelector('[data-field="phone"]');
                const errEl = root.querySelector('.psr-phone-error');
                const rawPhone = (inputEl || {}).value || '';
                phoneDigits = this._getTRPhoneDigits(rawPhone);

                const isValid = phoneDigits && phoneDigits.length === 11 && phoneDigits.startsWith('05');
                if (!isValid) {
                    if (inputEl) inputEl.style.borderColor = '#ef4444';
                    if (errEl) errEl.style.display = 'block';
                    return;
                } else if (inputEl) {
                    inputEl.style.borderColor = '';
                    if (errEl) errEl.style.display = 'none';
                }
            }

            // Validate email (seçiliyse zorunlu)
            const emailEl = root.querySelector('[data-field="email"]');
            const emailValue = emailEl ? emailEl.value.trim() : '';
            if (c.showEmailField) {
                const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
                const errEl = root.querySelector('.psr-email-error');
                if (!emailValue || !emailRegex.test(emailValue)) {
                    if (emailEl) emailEl.style.borderColor = '#ef4444';
                    if (errEl) errEl.style.display = 'block';
                    return;
                } else {
                    if (emailEl) emailEl.style.borderColor = '';
                    if (errEl) errEl.style.display = 'none';
                }
            }

            // Validate consent (her zaman zorunlu)
            {
                const checkbox = root.querySelector('.psr-consent-checkbox');
                if (checkbox && !checkbox.checked) {
                    checkbox.style.outline = '2px solid #ef4444';
                    setTimeout(() => { checkbox.style.outline = ''; }, 2000);
                    return;
                }
            }

            const consentChecked = root.querySelector('.psr-consent-checkbox')?.checked || false;

            // UI feedback
            const btn = root.querySelector('.psr-btn-submit');
            if (btn) {
                btn.disabled = true;
                btn.textContent = 'Kaydediliyor...';
            }

            if (this.livePreview) {
                this._showSuccess(root);
                return;
            }

            fetch(c.submitUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'omit',
                body: JSON.stringify({
                    companyId: c.companyId,
                    productId: this.currentProductId || (typeof window !== 'undefined' && window.urunKartID) || '',
                    variantId: this.selectedVariantId || '',
                    selectedVariant: this.selectedVariant || '',
                    phone: phoneDigits,
                    email: emailValue,
                    firstName: firstNameValue || null,
                    lastName: lastNameValue || null,
                    pageUrl: this.currentProductUrl || location.href,
                    sessionId: this.sessionId,
                    consentGiven: consentChecked
                })
            })
                .then(r => { if (!r.ok) throw new Error('fail'); return r.json(); })
                .then(() => {
                    this._showSuccess(root);
                })
                .catch(() => {
                    // Show success UX anyway
                    this._showSuccess(root);
                });
        }

        _showSuccess(root) {
            const form = root.querySelector('.psr-form');
            const variants = root.querySelector('.psr-variants');
            const desc = root.querySelector('.psr-desc');
            const success = root.querySelector('.psr-success');

            if (form) form.style.display = 'none';
            if (variants) variants.style.display = 'none';
            if (desc) desc.style.display = 'none';
            if (success) success.style.display = 'block';

            if (!this.livePreview) {
                setTimeout(() => this._dismiss(), 3000);
            } else {
                setTimeout(() => this._renderInline(), 3000);
            }
        }

        /* ═══════════════════════════════════════════
           DISMISS / CLOSE
        ═══════════════════════════════════════════ */
        _dismiss() {
            if (!this.livePreview) {
                this._sendEvent('dismiss');
            }

            const overlay = document.getElementById(this.containerId);
            if (overlay) {
                overlay.classList.remove('psr-visible');
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
                fetch(this.config.trackUrl, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        companyId: this.config.companyId,
                        eventType,
                        pageUrl: location.href,
                        sessionId: this.sessionId
                    }),
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
            const titleRGB = this._hexToRgb(c.titleColor);

            const tag = document.createElement('style');
            tag.id = STYLE_ID;
            tag.textContent = `
/* ─── PSR Reset ─── */
.psr-overlay,.psr-overlay *,.psr-inline-wrap,.psr-inline-wrap *{box-sizing:border-box;margin:0;padding:0;font-family:Inter,system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;}

/* ─── Overlay ─── */
.psr-overlay{position:fixed;inset:0;z-index:999999;display:flex;align-items:center;justify-content:center;opacity:0;visibility:hidden;transition:opacity .35s ease,visibility .35s ease;}
.psr-overlay.psr-visible{opacity:1;visibility:visible;}
.psr-overlay::before{content:'';position:absolute;inset:0;background:rgba(0,0,0,0.5);backdrop-filter:blur(4px);-webkit-backdrop-filter:blur(4px);}

/* ─── Dialog ─── */
.psr-dialog{position:relative;z-index:1;width:92%;max-width:420px;max-height:95vh;max-height:95svh;overflow-y:auto;transform:scale(.92) translateY(20px);opacity:0;transition:transform .4s cubic-bezier(.22,1,.36,1),opacity .35s ease;}
.psr-visible .psr-dialog{transform:scale(1) translateY(0);opacity:1;}

/* ─── Inline wrap ─── */
.psr-inline-wrap{width:100%;max-width:420px;margin:0 auto;}

/* ─── Card ─── */
.psr-card{padding:3rem 1.75rem 2.5rem 1.75rem;position:relative;box-shadow:0 25px 60px -12px rgba(0,0,0,.25);}

/* ─── Close ─── */
.psr-close{position:absolute;top:14px;right:14px;background:rgba(0,0,0,.06);border:none;cursor:pointer;width:28px;height:28px;display:flex;align-items:center;justify-content:center;border-radius:50%;transition:all .2s;z-index:2;}
.psr-close:hover{background:rgba(0,0,0,.12);}

/* ─── Title ─── */
.psr-title{font-size:1.3rem;font-weight:800;margin:0 0 30px;line-height:1.4;text-align:center;padding-right:20px;}

/* ─── Variants ─── */
.psr-variants{display:flex;flex-wrap:wrap;gap:10px;justify-content:center;margin-bottom:22px;}
.psr-variant-btn{padding:10px 20px;border:1.5px solid;border-radius:10px;background:transparent;cursor:pointer;font-size:14px;font-weight:500;transition:all .2s ease;font-family:inherit;}
.psr-variant-btn:hover{opacity:.8;}

/* ─── Description ─── */
.psr-desc{font-size:.88rem;opacity:.7;margin:24px 0 28px;line-height:1.65;text-align:center;}

/* ─── Form ─── */
.psr-form{display:flex;flex-direction:column;gap:0;}
.psr-input{width:100%;padding:14px 16px;border:1.5px solid rgba(0,0,0,.1);border-radius:10px;font-size:15px;outline:none;background:rgba(0,0,0,.02);transition:all .2s ease;box-sizing:border-box;margin-bottom:16px;font-family:inherit;color:inherit;}
.psr-input:focus{border-color:${accent};box-shadow:0 0 0 3px rgba(${accentRGB},.12);}
.psr-input::placeholder{opacity:.45;}
.psr-name-row{display:flex;gap:10px;margin-bottom:2px;}
.psr-name-row .psr-input-half{flex:1;min-width:0;width:0;}

/* ─── Consent ─── */
.psr-consent{display:flex;align-items:flex-start;gap:10px;margin-bottom:24px;}
.psr-consent-checkbox{margin-top:3px;flex-shrink:0;width:16px;height:16px;cursor:pointer;accent-color:${accent};}
.psr-consent-text{font-size:12px;opacity:.65;line-height:1.4;}
.psr-consent-text a{color:inherit;text-decoration:underline;}

/* ─── Submit Button ─── */
.psr-btn{width:100%;padding:15px;border:none;border-radius:10px;font-size:15px;font-weight:700;cursor:pointer;transition:all .25s ease;font-family:inherit;letter-spacing:.01em;}
.psr-btn:hover:not(:disabled){transform:translateY(-1px);box-shadow:0 6px 20px rgba(${accentRGB},.4);}
.psr-btn:active:not(:disabled){transform:scale(.98);}
.psr-btn:disabled{opacity:.6;cursor:not-allowed;}

/* ─── Success ─── */
.psr-success{text-align:center;padding:32px 0 16px;}
.psr-success-icon{display:flex;justify-content:center;margin-bottom:20px;}
.psr-success-icon svg{width:64px;height:64px;}
.psr-success-title{font-size:18px;font-weight:700;margin:0 0 12px;}
.psr-success-desc{font-size:14px;opacity:.7;margin:0;line-height:1.6;}

/* ─── Error ─── */
.psr-error{font-size:12px;color:#ef4444;margin:-6px 0 10px;line-height:1.4;}

/* ─── Branding ─── */
.psr-branding{display:flex;justify-content:center;align-items:center;margin-top:1rem;padding-top:0.5rem;border-top:1px solid rgba(0,0,0,.06);}

/* ─── Mobile ─── */
/* ─── Nostok Bell Icon ─── */
.size_box.nostok { display: inline-flex !important; align-items: center !important; gap: 4px !important; pointer-events: auto !important; }
.size_box.nostok .psr-nostok-bell{display:inline-flex !important;align-items:center;justify-content:center;cursor:pointer;flex-shrink:0;padding:2px;border-radius:50%;transition:transform .2s ease;line-height:0; animation: psr-bell-shake 3s infinite ease-in-out; transform-origin: top center;}
.size_box.nostok .psr-nostok-bell:hover{animation:none; transform:scale(1.2);}
.size_box.nostok .psr-nostok-bell:active{transform:scale(0.95);}

@keyframes psr-bell-shake {
    0%, 50%, 100% { transform: rotate(0); }
    5% { transform: rotate(12deg); }
    10% { transform: rotate(-12deg); }
    15% { transform: rotate(10deg); }
    20% { transform: rotate(-10deg); }
    25% { transform: rotate(6deg); }
    30% { transform: rotate(-6deg); }
    35% { transform: rotate(3deg); }
    40% { transform: rotate(-3deg); }
    45% { transform: rotate(0); }
}

/* btnnostok gizle */
.size_box.nostok .btnnostok,
.size_box.nostok .btnnostok.selected {display:none !important; visibility: hidden !important; width: 0 !important; height: 0 !important; overflow: hidden !important; opacity: 0 !important;}

@media(max-width:480px){
    .psr-overlay{align-items:flex-end;}
    .psr-dialog{width:100%;max-width:100%;max-height:95vh;max-height:95svh;transform:translateY(100%);opacity:1;}
    .psr-visible .psr-dialog{transform:translateY(0);}
    .psr-card{border-radius:24px 24px 0 0 !important;padding:1.5rem 1.5rem 0.5rem;}
}

/* ─── No Animation ─── */
.psr-overlay:not(.psr-animate){transition:none;}
.psr-overlay:not(.psr-animate) .psr-dialog{transition:none;}
.psr-overlay:not(.psr-animate).psr-visible{opacity:1;visibility:visible;}
.psr-overlay:not(.psr-animate).psr-visible .psr-dialog{transform:none;opacity:1;}
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
    window.PushonicaStockReminder = PushonicaStockReminder;

    // Auto-init: if loaded on production site (not admin)
    if (!window.__PUSHONICA_ADMIN__) {
        const autoInit = () => {
            const instance = new PushonicaStockReminder();
            window.pushonicaStockReminder = instance;
            // init() zaten constructor'da çağrılıyor ve _injectTriggerButton çalışacak
        };
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', autoInit);
        } else {
            autoInit();
        }
    }
})();
