/**
 * Simple Product Slider
 * Auto-detects company ID and loads sliders from R2
 */

// Define slider navigation functions in global scope
window.moveSliderPrev = function (specificTrack = null) {
    const sliderTracks = specificTrack ? [specificTrack] : document.querySelectorAll('.slider-track');

    sliderTracks.forEach(track => {
        // Calculate current position
        const currentTransform = track.style.transform || 'translateX(0px)';
        const currentTranslateX = parseInt(currentTransform.replace(/translateX\((.*)px\)/, '$1')) || 0;

        // Get the container to access navigation buttons
        const container = track.parentElement;
        const prevBtn = container.querySelector('.slider-prev');
        const nextBtn = container.querySelector('.slider-next');

        // Get the slider container and dots container
        const sliderContainer = track.closest('.pushonica-product-slider') ||
            track.closest('.pushonica-slider-popup') ||
            track.closest('.slider-container').parentElement;
        const dotsContainer = sliderContainer?.querySelector('.slider-dots');
        const dots = dotsContainer?.querySelectorAll('.slider-dot') || [];

        // Try to determine current slide index based on transform
        const firstItem = track.querySelector('.product-item, .slider-item');
        if (!firstItem) return;

        const itemWidth = firstItem.offsetWidth;
        const gap = 16; // Match the gap in CSS
        const stepSize = itemWidth + gap;

        // Calculate current index from translateX
        let currentIndex = Math.round(Math.abs(currentTranslateX) / stepSize);

        // Only move if not at the beginning already
        if (currentTranslateX < 0) {
            // Move one slide back
            currentIndex = Math.max(0, currentIndex - 1);
            const newTranslateX = -(currentIndex * stepSize);

            track.style.transform = `translateX(${newTranslateX}px)`;

            // Always enable the next button when moving back
            if (nextBtn) {
                nextBtn.removeAttribute('disabled');
                nextBtn.style.opacity = '1';
            }

            // If we reached the beginning, disable the prev button
            if (currentIndex === 0 && prevBtn) {
                prevBtn.setAttribute('disabled', 'disabled');
                prevBtn.style.opacity = '0.3';
            }

            // Update dots if available
            if (dots.length > 0) {
                dots.forEach((dot, i) => {
                    dot.classList.toggle('active', i === currentIndex);
                    dot.style.backgroundColor = i === currentIndex ? '#667eea' : 'rgba(200, 200, 200, 0.5)';
                    dot.style.opacity = i === currentIndex ? '1' : '0.8';
                });
            }
        } else {
            track.style.transform = 'translateX(0px)';

            // Disable prev button and enable next button
            if (prevBtn) {
                prevBtn.setAttribute('disabled', 'disabled');
                prevBtn.style.opacity = '0.3';
            }
            if (nextBtn) {
                nextBtn.removeAttribute('disabled');
                nextBtn.style.opacity = '1';
            }

            // Reset dots to first position
            if (dots.length > 0) {
                dots.forEach((dot, i) => {
                    dot.classList.toggle('active', i === 0);
                    dot.style.backgroundColor = i === 0 ? '#667eea' : 'rgba(200, 200, 200, 0.5)';
                    dot.style.opacity = i === 0 ? '1' : '0.8';
                });
            }
        }
    });
};

window.moveSliderNext = function (specificTrack = null) {
    const sliderTracks = specificTrack ? [specificTrack] : document.querySelectorAll('.slider-track');

    sliderTracks.forEach(track => {
        // Calculate current position
        const currentTransform = track.style.transform || 'translateX(0px)';
        const currentTranslateX = parseFloat(currentTransform.replace(/translateX\((.*)px\)/, '$1')) || 0;

        // Calculate maximum scroll distance
        const items = track.querySelectorAll('.product-item, .slider-item');
        const totalItems = items.length;

        if (totalItems === 0) {
            console.log('No items found in the slider');
            return;
        }

        // Get item width and gap
        const firstItem = items[0];
        const lastItem = items[items.length - 1];
        const itemWidth = firstItem.offsetWidth;
        const gap = 16; // Match the gap in CSS

        // Calculate actual track width using bounding client rect for precision
        const firstItemRect = firstItem.getBoundingClientRect();
        const lastItemRect = lastItem.getBoundingClientRect();
        const actualTrackWidth = lastItemRect.right - firstItemRect.left;

        const container = track.parentElement;

        // Get container's padding
        const containerStyle = window.getComputedStyle(container);
        const paddingLeft = parseFloat(containerStyle.paddingLeft) || 0;
        const paddingRight = parseFloat(containerStyle.paddingRight) || 0;

        // Calculate visible width (content box width)
        const visibleWidth = container.clientWidth - paddingLeft - paddingRight;

        // Check if we're on mobile
        const isMobile = window.innerWidth <= 767;

        // Get the exact position that makes the last item fully visible
        // This ensures the last item is completely in view
        const lastItemFullyVisiblePosition = visibleWidth - actualTrackWidth;

        // Ensure position is never positive (no empty space at start)
        const minPosition = Math.min(0, lastItemFullyVisiblePosition);

        // Calculate step size based on item width
        const stepSize = itemWidth + gap;

        // Get the slider container and dots container
        const sliderContainer = track.closest('.pushonica-product-slider') ||
            track.closest('.pushonica-slider-popup') ||
            track.closest('.slider-container').parentElement;
        const dotsContainer = sliderContainer?.querySelector('.slider-dots');
        const dots = dotsContainer?.querySelectorAll('.slider-dot') || [];

        // Calculate the current index and total dots
        let currentIndex = Math.round(Math.abs(currentTranslateX) / stepSize);
        const maxDotsIndex = dots.length - 1;
        const maxSlidesIndex = Math.ceil(Math.abs(minPosition) / stepSize);

        // Don't scroll past the end
        if (currentTranslateX > minPosition) {
            let newTranslateX;
            let isAtEnd = false;

            // Normal step movement
            currentIndex += 1;
            newTranslateX = -(currentIndex * stepSize);

            // Check if we reached or passed the end
            if (newTranslateX <= minPosition) {
                newTranslateX = minPosition;
                isAtEnd = true;
                // Update current index to max
                currentIndex = maxSlidesIndex;
            }

            // Apply the transform
            track.style.transform = `translateX(${newTranslateX}px)`;

            // Update navigation button states
            const prevBtn = container.querySelector('.slider-prev');
            const nextBtn = container.querySelector('.slider-next');

            if (prevBtn) {
                prevBtn.removeAttribute('disabled');
                prevBtn.style.opacity = '1';
            }

            // Check if we've reached the end position
            if (nextBtn && isAtEnd) {
                nextBtn.setAttribute('disabled', 'disabled');
                nextBtn.style.opacity = '0.3';
            }

            // Update dots if available
            if (dots.length > 0) {
                // Make sure currentIndex doesn't exceed dots length
                const dotIndex = Math.min(currentIndex, maxDotsIndex);

                dots.forEach((dot, i) => {
                    dot.classList.toggle('active', i === dotIndex);
                    dot.style.backgroundColor = i === dotIndex ? '#667eea' : 'rgba(200, 200, 200, 0.5)';
                    dot.style.opacity = i === dotIndex ? '1' : '0.8';
                });
            }
        } else {
            // We're already at the end
            // Ensure we are exactly at minPosition
            if (currentTranslateX !== minPosition) {
                track.style.transform = `translateX(${minPosition}px)`;
            }

            // Make sure the next button is disabled
            const prevBtn = container.querySelector('.slider-prev');
            const nextBtn = container.querySelector('.slider-next');

            if (prevBtn) {
                prevBtn.removeAttribute('disabled');
                prevBtn.style.opacity = '1';
            }

            if (nextBtn) {
                nextBtn.setAttribute('disabled', 'disabled');
                nextBtn.style.opacity = '0.3';
            }

            // Set dots to last position
            if (dots.length > 0) {
                const lastDotIndex = dots.length - 1;
                dots.forEach((dot, i) => {
                    dot.classList.toggle('active', i === lastDotIndex);
                    dot.style.backgroundColor = i === lastDotIndex ? '#667eea' : 'rgba(200, 200, 200, 0.5)';
                    dot.style.opacity = i === lastDotIndex ? '1' : '0.8';
                });
            }
        }
    });
};

(function () {
    'use strict';

    // Simple ProductSlider
    class ProductSlider {
        constructor(options = {}) {
            this.options = options;
            this.companyId = options.companyId || this.getCompanyId();
            this.r2BaseUrl = 'https://assets.pushonica.com';
            this.sliders = [];

            if (options.config) {
                 const config = options.config;
                 this.preloadedConfig = config;
            }

            this.targetElement = options.targetElement || null;

            if ((this.companyId || options.config) && !options.manualInit) {
                setTimeout(() => this.init(), 0);
            } else if (!this.companyId && !options.config) {
                console.warn('[ProductSlider] Company ID not found');
            }
        }

        getCompanyId() {
            if (this.options && this.options.companyId) return this.options.companyId;

            // Try to get from script tag
            const scripts = document.querySelectorAll('script[data-module-id]');
            for (let script of scripts) {
                const id = script.getAttribute('data-module-id');
                if (id) return id;
            }

            // Try to get from template
            const templateId = '5e715472-5810-417b-96f1-bfd99ea5df43';
            if (templateId !== '5e715472-5810-417b-96f1-bfd99ea5df43') return templateId;

            return null;
        }

        async init() {
            try {
                if (document.readyState === 'loading') {
                     document.addEventListener('DOMContentLoaded', () => this.runInit());
                } else {
                     await this.runInit();
                }
            } catch (error) {
                console.error('[ProductSlider] Init error:', error);
            }
        }

        async runInit() {
             if (this.options.livePreview && this.preloadedConfig) {
                 this.renderSlider(this.preloadedConfig);
             } else {
                 await this.loadSliders();
             }
        }

        async loadSliders() {
            try {
                let slidersData;

                if (this.preloadedConfig) {
                     slidersData = { sliders: [this.preloadedConfig] };
                } else {
                     slidersData = await this.fetchSlidersData();
                }

                if (!slidersData || !slidersData.sliders) {
                    return;
                }

                // Handle new structure: sliders organized by ID
                let activeSliders = [];

                if (Array.isArray(slidersData.sliders)) {
                    // Old structure compatibility
                    activeSliders = slidersData.sliders.filter(slider =>
                        slider.items && slider.items.length > 0
                    );
                } else if (typeof slidersData.sliders === 'object') {
                    // New structure: sliders by ID
                    activeSliders = Object.keys(slidersData.sliders)
                        .map(sliderId => slidersData.sliders[sliderId])
                        .filter(slider => slider.items && slider.items.length > 0);
                }


                if (activeSliders.length === 0) {
                    console.log('[ProductSlider] No active sliders with items');
                    return;
                }

                // Inject base CSS once
                this.injectStyles();

                for (const slider of activeSliders) {
                    // Inject slider-specific styles
                    this.injectStyles(slider);
                    this.renderSlider(slider);
                }

            } catch (error) {
                console.error('[ProductSlider] Error:', error);
            }
        }

        async fetchSlidersData() {
            try {
                const url = `${this.r2BaseUrl}/${this.companyId}/productSliders.json`;
                const response = await fetch(url);

                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}`);
                }

                return await response.json();
            } catch (error) {
                console.warn('[ProductSlider] R2 fetch failed:', error.message);
                return null;
            }
        }

        renderSlider(sliderConfig) {
            if (this.options.livePreview) {
                this.renderInlineSlider(sliderConfig);
            } else {
                // Create floating button with proper positioning for multiple sliders
                const button = this.createFloatingButton(sliderConfig, this.sliders.length);
                this.sliders.push({
                    config: sliderConfig,
                    button: button
                });
            }
        }

        renderInlineSlider(config, targetContainer = null) {
            const container = targetContainer || this.targetElement || (this.options.targetSelector ? document.querySelector(this.options.targetSelector) : null);

            if(!container) {
                console.warn('[ProductSlider] No target container for inline preview');
                return;
            }

            container.innerHTML = '';

            const sliderId = 'preview';

            const styleId = `pushonica-style-${config.id || 'preview'}`;
            let styleTag = document.getElementById(styleId);
            if(styleTag) styleTag.remove();

            styleTag = document.createElement('style');
            styleTag.id = styleId;
            styleTag.textContent = this.generateDynamicCSS(config.settings, config.id || 'preview');
            document.head.appendChild(styleTag);

            const wrapper = document.createElement('div');
            wrapper.className = `pushonica-product-slider pushonica-slider-${config.id || 'preview'}`;

            wrapper.style.width = '100%';

            if(config.displayType === 'grid') wrapper.classList.add('pushonica-display-grid');
            else if(config.displayType === 'tabs') wrapper.classList.add('pushonica-display-tabs');
            else wrapper.classList.add('pushonica-display-slider');

            wrapper.innerHTML = this.generateSliderHTML(config);
            container.appendChild(wrapper);

            setTimeout(() => {
                 if(window.moveSliderNext) window.moveSliderNext(wrapper.querySelector('.slider-track'));
            }, 100);
        }

        createSliderContainer(config) {
            const sliderId = config.sliderId || config.id;
            const container = document.createElement('div');
            container.className = `pushonica-product-slider pushonica-slider-${sliderId}`;
            container.innerHTML = this.generateSliderHTML(config);
            return container;
        }

        getItemDescription(item, descriptionSource) {
            if (item.manualDescription && item.manualDescription.trim()) {
                return item.manualDescription;
            }

            if (!descriptionSource || descriptionSource === 'manual') {
                return item.productDescription || '';
            }

            return item.productDescription || '';
        }

        getPriceHTML(item, settings) {
            if (settings.showProductPrice === false || item.productPrice == null)   return '';
            const currency = item.currency || 'TRY';
            const kdvIncluded = item.kdvIncluded !== false;
            const taxRate = typeof item.taxRate === 'number' ? item.taxRate : (parseFloat(item.taxRate) || 18);
            const orig = item.discountPrice != null ? parseFloat(item.discountPrice) : null;
            const price = parseFloat(item.productPrice);
            const fmt = (n) => (Number(n).toLocaleString('tr-TR', { minimumFractionDigits: 0, maximumFractionDigits: 2 }));
            const hasDiscount = orig != null && orig > price;

            if (kdvIncluded) {
                const priceDisplay = hasDiscount ? `<span class="product-price-original">${fmt(orig)} ${currency}</span><span class="product-price product-price-current">${fmt(price)} ${currency}</span>` : `<span class="product-price product-price-current">${fmt(price)} ${currency}</span>`;
                return `<div class="product-price-wrap product-price-kdv-dahil">${priceDisplay}</div>`;
            }

            if (hasDiscount) {
                return `<div class="product-price-wrap" style="position: relative; display: inline-flex; align-items: baseline;"><span class="product-price-original">${fmt(orig)} ${currency}</span><span class="product-price product-price-current">${fmt(price)} ${currency}</span><span class="product-price-vat-badge" style="background: #f0f0f0; color: #666; font-size: 9px; padding: 1px 3px; border-radius: 3px; margin-left: 2px; position: relative; top: -8px; font-weight: 500;">+KDV</span></div>`;
            }
            return `<div class="product-price-wrap" style="position: relative; display: inline-flex; align-items: baseline;"><span class="product-price product-price-current">${fmt(price)} ${currency}</span><span class="product-price-vat-badge" style="background: #f0f0f0; color: #666; font-size: 9px; padding: 1px 3px; border-radius: 3px; margin-left: 2px; position: relative; top: -8px; font-weight: 500;">+KDV</span></div>`;
        }

        generateSliderHTML(config) {
            const settings = config.settings || {};
            const items = config.items || [];
            const showDots = this.getShowDots(settings);

            let itemsHTML = '';
            items.forEach((item, index) => {
                itemsHTML += `
                    <div class="product-item slider-item" data-index="${index}">
                        ${settings.showProductImage !== false ? `
                            <div class="product-image" style="aspect-ratio: ${this.getAspectRatio(settings.imageAspectRatio)}">
                                <img src="${item.productImage || ''}" alt="${item.productTitle || ''}" loading="lazy" style="opacity: 0; transition: opacity 0.5s ease;" onload="this.style.opacity='1'" onerror="this.style.display='none'">
                            </div>
                        ` : ''}
                        <div class="product-title">${item.productTitle || ''}</div>
                        ${settings.showProductDescription && item.productDescription ? `
                            <div class="product-description">${item.productDescription}</div>
                        ` : ''}
                        <div class="product-footer">
                            ${this.getPriceHTML(item, settings)}
                            <a class="product-link" href="${item.productUrl || '#'}" target="_blank" rel="noopener">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="color: #9ca3af;">
                                    <path d="M7 17L17 7"></path>
                                    <path d="M7 7h10v10"></path>
                                </svg>
                                İncele
                            </a>
                        </div>
                    </div>
                `;
            });

            let dotsHTML = '';
            if (showDots) {
                const itemsPerView = this.getItemsPerView(settings);
                const totalItems = items.length || 1;
                const maxDotsNeeded = Math.max(1, totalItems - itemsPerView);

                for (let i = 0; i < maxDotsNeeded; i++) {
                    dotsHTML += `<button class="slider-dot ${i === 0 ? 'active' : ''}"
                                       data-index="${i}"
                                       aria-label="Slide ${i + 1}"
                                       style="width: 8px !important;
                                              height: 8px !important;
                                              background-color: ${i === 0 ? '#667eea' : 'rgba(200, 200, 200, 0.5)'} !important;
                                              border-radius: 50% !important;
                                              margin: 0 3px !important;
                                              border: none !important;
                                              display: block !important;
                                              visibility: visible !important;
                                              opacity: ${i === 0 ? '1' : '0.8'} !important;">
                               </button>`;
                }

            }

            // Create a single template with conditional dots container
            const templateHtml = `
                <div class="slider-wrapper">
                    <div class="slider-header">
                        <h2 class="slider-title">${config.name || 'Önerilen Ürünler'}</h2>
                    </div>
                    <div class="slider-container">
                        ${this.getShowArrows(settings) ? '<button class="slider-arrow slider-prev" disabled aria-label="Önceki" onclick="window.moveSliderPrev(); event.stopPropagation();"><svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="15 18 9 12 15 6"></polyline></svg></button>' : ''}
                        <div class="slider-track">
                            ${itemsHTML}
                        </div>
                        ${this.getShowArrows(settings) ? '<button class="slider-arrow slider-next" aria-label="Sonraki" onclick="window.moveSliderNext(); event.stopPropagation();"><svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"></polyline></svg></button>' : ''}
                    </div>
                    ${showDots ? `
                    <div class="slider-dots" role="tablist" style="display: flex !important; visibility: visible !important; opacity: 1 !important; z-index: 10 !important; margin: 16px auto 8px !important; justify-content: center !important; gap: 6px !important; min-height: 16px !important;">
                        ${dotsHTML}
                    </div>
                    ` : ''}
                </div>
            `;

            return templateHtml;
        }

        generateThemeCSS(settings, sliderId) {
            const theme = settings.theme || 'default';
            const primaryColor = settings.primaryColor || '#667eea';
            const secondaryColor = settings.secondaryColor || '#764ba2';
            const backgroundColor = settings.backgroundColor || '#ffffff';
            const lightPrimaryColor = this.adjustColorBrightness(primaryColor, 80); // Daha açık bir ton
            const lightSecondaryColor = this.adjustColorBrightness(secondaryColor, 80); // Daha açık bir ton

            switch (theme) {
                case 'modern':
                    return `
                        /* Modern Tema - Tek renk ve sade */
                        .pushonica-slider-${sliderId} .pushonica-product-slider {
                            background: linear-gradient(135deg, ${backgroundColor} 0%, ${primaryColor}10 100%) !important;
                            border: none !important;
                            border-radius: 12px !important;
                        }

                        .pushonica-slider-${sliderId} .product-item {
                            border-radius: 10px !important;
                            backdrop-filter: blur(10px) !important;
                        }

                        /* Modern tema için tek renkli buton */
                        .pushonica-slider-${sliderId} .product-button {
                            background-color: ${primaryColor} !important;
                            color: white !important;
                            border-radius: 6px !important;
                            transition: all 0.3s ease !important;
                            box-shadow: 0 4px 8px ${primaryColor}40 !important;
                        }

                        .pushonica-slider-${sliderId} .product-button:hover {
                            background-color: ${secondaryColor} !important;
                            transform: translateY(-1px) !important;
                            box-shadow: 0 6px 12px ${primaryColor}60 !important;
                        }

                        /* Modern tema için tek renkli header */
                        .pushonica-slider-${sliderId} .popup-header {
                            background: ${primaryColor} !important;
                            box-shadow: 0 3px 10px rgba(0,0,0,0.1) !important;
                            border-bottom: none !important;
                        }
                    `;
                case 'minimal':
                    return `
                        /* Minimal Tema - Daha hafif ve minimal görünüm */
                        .pushonica-slider-${sliderId} .pushonica-product-slider {
                            border: 1px solid #e5e5e5 !important;
                            box-shadow: none !important;
                            background: ${backgroundColor} !important;
                        }

                        .pushonica-slider-${sliderId} .product-item {
                            border: none !important;
                            border-bottom: 1px solid #f0f0f0 !important;
                            border-radius: 0 !important;
                            box-shadow: none !important;
                        }

                        /* Minimal tema için daha soluk buton */
                        .pushonica-slider-${sliderId} .product-button {
                            background-color: ${lightPrimaryColor} !important;
                            color: ${primaryColor} !important;
                            border: 1px solid ${primaryColor}30 !important;
                            border-radius: 4px !important;
                            font-weight: 400 !important;
                            box-shadow: none !important;
                        }

                        .pushonica-slider-${sliderId} .product-button:hover {
                            background-color: ${primaryColor}20 !important;
                            border-color: ${primaryColor}60 !important;
                            transform: none !important;
                            box-shadow: none !important;
                        }

                        /* Minimal tema için daha soluk header */
                        .pushonica-slider-${sliderId} .popup-header {
                            background: ${backgroundColor} !important;
                            border-bottom: 1px solid #ccc !important; /* Daha belirgin border bottom */
                            box-shadow: none !important;
                        }

                        .pushonica-slider-${sliderId} .popup-header h3 {
                            color: ${primaryColor} !important;
                            font-weight: 500 !important;
                        }

                        /* Minimal tema için özel kapat butonu stili */
                        .pushonica-slider-${sliderId} .popup-header .popup-close {
                            color: ${primaryColor} !important; /* Beyaz arka plana uygun renk */
                            border: 1px solid ${primaryColor}30 !important; /* İnce kenarlık ekle */
                        }

                        .pushonica-slider-${sliderId} .popup-header .popup-close:hover {
                            background: ${primaryColor}20 !important; /* Hover durumunda hafif renkli arka plan */
                        }

                        /* Minimal tema için daha narin oklar */
                        .pushonica-slider-${sliderId} .slider-arrow {
                            background: ${backgroundColor} !important;
                            color: ${primaryColor} !important;
                            border: 1px solid ${primaryColor}30 !important;
                            box-shadow: none !important;
                        }
                    `;
                case 'colorful':
                    return `
                        /* Colorful Tema - Canlı ve gradient efektli */
                        .pushonica-slider-${sliderId} .pushonica-product-slider {
                            background: linear-gradient(45deg, ${primaryColor}20, ${secondaryColor}20) !important;
                            border: 2px solid ${primaryColor} !important;
                            border-radius: 8px !important;
                        }

                        .pushonica-slider-${sliderId} .product-item {
                            background: linear-gradient(135deg, ${backgroundColor} 0%, ${primaryColor}05 100%) !important;
                            border-radius: 8px !important;
                            box-shadow: 0 4px 12px rgba(0,0,0,0.08) !important;
                        }

                        /* Colorful tema için gradient buton */
                        .pushonica-slider-${sliderId} .product-button {
                            background: linear-gradient(135deg, ${primaryColor} 0%, ${secondaryColor} 100%) !important;
                            color: white !important;
                            border-radius: 30px !important;
                            font-weight: 600 !important;
                            box-shadow: 0 4px 15px ${primaryColor}60 !important;
                            border: none !important;
                            padding: 8px 20px !important;
                        }

                        .pushonica-slider-${sliderId} .product-button:hover {
                            background: linear-gradient(135deg, ${secondaryColor} 0%, ${primaryColor} 100%) !important;
                            transform: translateY(-2px) !important;
                            box-shadow: 0 6px 18px ${primaryColor}80 !important;
                        }

                        /* Colorful tema için gradient header */
                        .pushonica-slider-${sliderId} .popup-header {
                            background: linear-gradient(135deg, ${primaryColor} 0%, ${secondaryColor} 100%) !important;
                            box-shadow: 0 4px 15px rgba(0,0,0,0.1) !important;
                            border: none !important;
                        }

                        /* Colorful tema için daha canlı oklar */
                        .pushonica-slider-${sliderId} .slider-arrow {
                            background: white !important;
                            color: ${primaryColor} !important;
                            box-shadow: 0 3px 10px ${primaryColor}60 !important;
                            border: none !important;
                            border-radius: 50% !important;
                        }

                        .pushonica-slider-${sliderId} .slider-arrow:hover {
                            background: linear-gradient(135deg, ${primaryColor} 0%, ${secondaryColor} 100%) !important;
                            color: white !important;
                            box-shadow: 0 5px 15px ${primaryColor}80 !important;
                        }
                    `;
                default:
                    return '';
            }
        }

        createFloatingButton(sliderConfig, buttonIndex = 0) {
            const settings = sliderConfig.settings || {};
            const theme = settings.theme || 'default';
            const primaryColor = settings.primaryColor || '#667eea';
            const secondaryColor = settings.secondaryColor || '#764ba2';

            const button = document.createElement('div');
            button.className = 'pushonica-floating-button';
            button.setAttribute('data-slider-id', sliderConfig.sliderId);
            button.innerHTML = `
                <style>
                    @keyframes thunder-charge {
                        0%, 100% {
                            filter: drop-shadow(0 0 0 rgba(255,255,255,0));
                            fill: transparent;
                            transform: scale(1);
                        }
                        50% {
                            filter: drop-shadow(0 0 8px rgba(255,255,255,0.8));
                            fill: rgba(255,255,255,0.8);
                            transform: scale(1.1);
                        }
                    }
                    .charging-icon {
                        animation: thunder-charge 2s infinite ease-in-out;
                    }
                </style>
                <div class="floating-btn-icon">
                    <svg class="charging-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon>
                    </svg>
                </div>
                <div class="floating-btn-text">${sliderConfig.name || 'Önerilen Ürünler'}</div>
            `;

            // Calculate position for multiple buttons (stack vertically)
            const buttonWidth = 120; // Dikdörtgen için genişlik
            const buttonSpacing = 15; // Space between buttons
            const totalButtonHeight = buttonWidth + buttonSpacing; // Döndürüldüğü için width = height oluyor
            const topOffset = buttonIndex * totalButtonHeight;

            // Apply dynamic styling based on settings and theme
            button.style.position = 'fixed';
            button.style.right = '20px'; // Sağa taşırarak kenardan çıkartıyoruz
            button.style.top = `calc(50% + ${topOffset}px)`;
            button.style.transform = 'translateY(-50%) rotate(-90deg)'; // -90 derece döndür
            button.style.transformOrigin = 'right center'; // Merkez noktasından döndür
            button.style.zIndex = `${999999 - buttonIndex}`; // Higher buttons have higher z-index
            button.style.cursor = 'pointer';

            // Apply theme-based background - only colorful theme gets gradient
            if (theme === 'colorful') {
                button.style.background = `linear-gradient(135deg, ${primaryColor} 0%, ${secondaryColor} 100%)`;
            } else {
                // All other themes (modern, minimal, default) use solid primary color
                button.style.background = primaryColor;
            }

            button.style.color = 'white';
            button.style.padding = '12px 24px'; // Dikdörtgen için padding
            button.style.borderRadius = '8px'; // Köşeleri yuvarla ama tam yuvarlak değil
            button.style.boxShadow = `0 8px 30px ${primaryColor}50`;
            button.style.fontFamily = '"Poppins", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif';
            button.style.fontWeight = '600';
            button.style.fontSize = '13px';
            button.style.display = 'flex';
            button.style.alignItems = 'center';
            button.style.gap = '8px';
            button.style.transition = 'all 0.3s ease';
            button.style.userSelect = 'none';
            button.style.whiteSpace = 'nowrap'; // Metni tek satırda tut

            // Hover effects - only box shadow changes
            button.addEventListener('mouseenter', () => {
                button.style.transform = 'translateY(-50%) rotate(-90deg) scale(1.05)';
                button.style.boxShadow = `0 12px 40px ${primaryColor}70`;
            });

            button.addEventListener('mouseleave', () => {
                button.style.transform = 'translateY(-50%) rotate(-90deg) scale(1)';
                button.style.boxShadow = `0 8px 30px ${primaryColor}50`;
            });

            // Add click event
            button.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.showSliderOverlay(sliderConfig);
            });

            document.body.appendChild(button);

            return button;
        }

        createSliderOverlay(sliderConfig) {
            const settings = sliderConfig.settings || {};
            const sliderId = sliderConfig.sliderId || sliderConfig.id;
            const displayType = sliderConfig.displayType || 'slider';

            const popup = document.createElement('div');
            popup.className = `pushonica-slider-popup pushonica-slider-popup-${sliderId} pushonica-slider-${sliderId} pushonica-display-${displayType}`;
            popup.style.display = 'none';

            // Generate content based on display type
            popup.innerHTML = this.generatePopupContent(sliderConfig, displayType);

            // Apply base styling
            this.applyBasePopupStyles(popup, settings, displayType);

            // Close button event
            const closeBtn = popup.querySelector('.popup-close');
            if (closeBtn) {
                closeBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    this.hideSliderOverlay(sliderId);
                });
            }

            // Backdrop click to close (sadece slider ve grid modları için)
            if (displayType !== 'tabs') {
                // Backdrop click event'i delayed olarak ekle
                setTimeout(() => {
                    const backdrop = popup.querySelector('.popup-backdrop');
                    if (backdrop) {
                        backdrop.addEventListener('click', (e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            // Sadece backdrop'a tıklanırsa kapat, content'e değil
                            if (e.target === backdrop) {
                                this.hideSliderOverlay(sliderId);
                            }
                        });
                    }
                }, 100);
            }

            document.body.appendChild(popup);

            return popup;
        }

        showSliderOverlay(sliderConfig) {
            const sliderId = sliderConfig.sliderId || sliderConfig.id;
            const displayType = sliderConfig.displayType || 'slider';
            const settings = sliderConfig.settings || {};
            const animationType = settings.animationType || (this.settings && this.settings.animationType) || 'slide';

            // Look for specific slider popup
            let popup = document.querySelector(`.pushonica-slider-popup-${sliderId}`);

            // If popup is already visible, close it with smooth animation
            if (popup && popup.style.display === 'block') {
                this.hideSliderOverlay(sliderId);
                return;
            }

            // Close other popups with smooth animations
            const allPopups = document.querySelectorAll('.pushonica-slider-popup');
            allPopups.forEach(p => {
                if (!p.classList.contains(`pushonica-slider-popup-${sliderId}`)) {
                    const pDisplayType = p.classList.contains('pushonica-display-tabs') ? 'tabs' :
                        p.classList.contains('pushonica-display-grid') ? 'grid' : 'slider';
                    // Get animation type for each popup
                    const pAnimationType = p.dataset.animationType || 'slide';
                    this.animateSliderOut(p, pDisplayType, pAnimationType);
                }
            });

            // Create popup if it doesn't exist
            if (!popup) {
                popup = this.createSliderOverlay(sliderConfig);
            }

            // Apply display type specific positioning and styling
            this.applyDisplayTypeStyles(popup, displayType);

            // Store animation type for later use during closing
            popup.dataset.animationType = animationType;

            // Start modern animation with specific type
            this.animateSliderIn(popup, displayType, animationType, sliderConfig);

            // Add subtle staggered animation for content items
            this.animateContentItems(popup, displayType);

            // Make all product items and grid items clickable in the popup
            const clickableItems = popup.querySelectorAll('.product-item, .grid-item, .tabs-item');
            clickableItems.forEach(item => {
                item.addEventListener('click', function (e) {
                    // Don't trigger if clicking on the link itself or arrows/dots (let default behavior work)
                    if (e.target.closest('.product-link') ||
                        e.target.closest('.slider-arrow') ||
                        e.target.closest('.slider-dot')) {
                        return;
                    }

                    // Find the product link URL
                    const link = item.querySelector('.product-link');
                    if (link && link.href) {
                        // Open in new tab like the original link
                        window.open(link.href, '_blank', 'noopener');
                    }
                });
            });

            // Make "İncele" links work explicitly in popups
            const productLinks = popup.querySelectorAll('.product-link');
            productLinks.forEach(link => {
                link.addEventListener('click', function (e) {
                    e.stopPropagation(); // Stop event from bubbling up to parent item
                    if (link.href) {
                        window.open(link.href, '_blank', 'noopener');
                    }
                });
            });

            // Handle dots only if showDots is true
            setTimeout(() => {
                // Make sure dots are created and visible only if showDots is true
                this.ensureSliderDotsVisible(popup, sliderConfig);

                // Only process dots if showDots is true
                if (this.getShowDots(settings)) {
                    const dotsContainer = popup.querySelector('.slider-dots');
                    if (dotsContainer) {
                        // Add click event listener to dots
                        dotsContainer.addEventListener('click', (e) => {
                            if (e.target.classList.contains('slider-dot')) {
                                const index = parseInt(e.target.getAttribute('data-index'));

                                // Find the track and implement navigation
                                const track = popup.querySelector('.slider-track');
                                if (track) {
                                    const items = popup.querySelectorAll('.slider-item, .product-item');
                                    const itemWidth = items[0]?.offsetWidth || 0;
                                    const gap = 16;

                                    // Calculate the new position
                                    // Use the specific index position for exact navigation
                                    const translateX = -(index * (itemWidth + gap));

                                    // Make sure we don't go past the maximum scroll position
                                    const maxScroll = -((items.length - 1) * (itemWidth + gap));
                                    const safeTranslateX = Math.max(maxScroll, translateX);

                                    // Apply the transform with precise position
                                    track.style.transform = `translateX(${safeTranslateX}px)`;

                                    // Update dot states
                                    const dots = dotsContainer.querySelectorAll('.slider-dot');
                                    dots.forEach((dot, i) => {
                                        dot.classList.toggle('active', i === index);
                                        dot.style.backgroundColor = i === index ? '#667eea' : 'rgba(200, 200, 200, 0.5)';
                                        dot.style.opacity = i === index ? '1' : '0.8';
                                    });

                                    // Update arrow states based on dot index
                                    const prevBtn = popup.querySelector('.slider-prev');
                                    const nextBtn = popup.querySelector('.slider-next');

                                    if (prevBtn) {
                                        prevBtn.disabled = index === 0;
                                        prevBtn.style.opacity = index === 0 ? '0.3' : '1';
                                    }

                                    if (nextBtn) {
                                        const maxIndex = dots.length - 1;
                                        nextBtn.disabled = index >= maxIndex;
                                        nextBtn.style.opacity = index >= maxIndex ? '0.3' : '1';
                                    }
                                }
                            }
                        });
                    }
                }
            }, 500); // Give the animation time to complete

            // Initialize autoPlay for popup slider if display type is slider
            if (displayType === 'slider' && this.getAutoPlay(settings)) {
                this.initializePopupAutoPlay(popup, sliderConfig);
            }

            // Add touch support for slider
            if (displayType === 'slider') {
                const track = popup.querySelector('.slider-track');
                if (track) {
                    // Use existing addTouchSupport with callbacks
                    // Swipe Left -> Next Slide
                    // Swipe Right -> Prev Slide
                    this.addTouchSupport(track,
                        () => window.moveSliderNext(track),
                        () => window.moveSliderPrev(track)
                    );
                }
            }
        }

        // Method to handle autoPlay for popup sliders
        initializePopupAutoPlay(popup, sliderConfig) {
            const settings = sliderConfig.settings || {};
            const sliderId = sliderConfig.sliderId || sliderConfig.id;
            const interval = settings.autoPlayInterval || 5000;

            // Only proceed if autoPlay is enabled and we have slider content
            if (!this.getAutoPlay(settings)) {
                return;
            }

            const track = popup.querySelector('.slider-track');
            const items = popup.querySelectorAll('.product-item, .slider-item');
            const dotsContainer = popup.querySelector('.slider-dots');
            const dots = dotsContainer?.querySelectorAll('.slider-dot') || [];

            if (!track || items.length <= 1 || dots.length === 0) {
                return;
            }

            // Initialize autoPlayIntervals object if it doesn't exist
            if (!this.popupAutoPlayIntervals) {
                this.popupAutoPlayIntervals = {};
            }

            // Clear any existing interval for this popup
            if (this.popupAutoPlayIntervals[sliderId]) {
                clearInterval(this.popupAutoPlayIntervals[sliderId]);
            }

            // Track current index
            let currentIndex = 0;

            // Try to recover state from DOM
            const activeDot = dotsContainer?.querySelector('.slider-dot.active');
            if (activeDot) {
                currentIndex = parseInt(activeDot.getAttribute('data-index') || '0');
            }

            const maxIndex = dots.length - 1;


            // Create new interval
            const intervalId = setInterval(() => {
                // Only proceed if not being interacted with
                if (!popup.dataset.userInteracting && popup.style.display === 'block') {
                    // Sync with current DOM state in case manual navigation happened
                    const currentActiveDot = dotsContainer.querySelector('.slider-dot.active');
                    if (currentActiveDot) {
                        currentIndex = parseInt(currentActiveDot.getAttribute('data-index') || '0');
                    }

                    // Advance to next slide
                    currentIndex = (currentIndex < maxIndex) ? currentIndex + 1 : 0;

                    // Calculate position based on items
                    const itemWidth = items[0]?.offsetWidth || 0;
                    const gap = 16;

                    // Calculate position
                    const translateX = -(currentIndex * (itemWidth + gap));

                    // Apply transform
                    track.style.transform = `translateX(${translateX}px)`;

                    // Update dots
                    dots.forEach((dot, i) => {
                        dot.classList.toggle('active', i === currentIndex);
                        dot.style.backgroundColor = i === currentIndex ? '#667eea' : 'rgba(200, 200, 200, 0.5)';
                        dot.style.opacity = i === currentIndex ? '1' : '0.8';
                    });

                    // Update arrow states
                    const prevBtn = popup.querySelector('.slider-prev');
                    const nextBtn = popup.querySelector('.slider-next');

                    if (prevBtn) {
                        prevBtn.disabled = currentIndex === 0;
                        prevBtn.style.opacity = currentIndex === 0 ? '0.3' : '1';
                    }

                    if (nextBtn) {
                        nextBtn.disabled = currentIndex === maxIndex;
                        nextBtn.style.opacity = currentIndex === maxIndex ? '0.3' : '1';
                    }
                }
            }, interval);

            // Store for cleanup
            this.popupAutoPlayIntervals[sliderId] = intervalId;

            // Add user interaction detection
            popup.addEventListener('mouseenter', () => {
                popup.dataset.userInteracting = 'true';
            });

            popup.addEventListener('mouseleave', () => {
                popup.dataset.userInteracting = 'false';
            });

            // Also pause on touch
            popup.addEventListener('touchstart', () => {
                popup.dataset.userInteracting = 'true';
            });

            popup.addEventListener('touchend', () => {
                // Use timeout to allow click events to complete
                setTimeout(() => {
                    popup.dataset.userInteracting = 'false';
                }, 1000);
            });
        }

        // Add staggered animation for content items
        animateContentItems(popup, displayType) {
            const items = popup.querySelectorAll('.product-item, .grid-item, .tabs-item');

            items.forEach((item, index) => {
                // Set initial state - slightly below and invisible
                item.style.opacity = '0';
                item.style.transform = 'translateY(20px)';
                item.style.transition = 'none';

                // Animate in with stagger delay
                setTimeout(() => {
                    item.style.transition = 'opacity 0.4s cubic-bezier(0.25, 0.46, 0.45, 0.94), transform 0.4s cubic-bezier(0.25, 0.46, 0.45, 0.94)';
                    item.style.opacity = '1';
                    item.style.transform = 'translateY(0)';
                }, 150 + (index * 50)); // 50ms stagger delay
            });
        }

        hideSliderOverlay(sliderId = null) {
            if (sliderId) {
                const popup = document.querySelector(`.pushonica-slider-popup-${sliderId}`);
                if (popup && popup.style.display === 'block') {
                    const displayType = popup.classList.contains('pushonica-display-tabs') ? 'tabs' :
                        popup.classList.contains('pushonica-display-grid') ? 'grid' : 'slider';

                    // Get animation type from the popup's dataset or global settings
                    const animationType = popup.dataset.animationType || 'slide';

                    // Clear autoPlay interval if exists
                    if (this.popupAutoPlayIntervals && this.popupAutoPlayIntervals[sliderId]) {
                        clearInterval(this.popupAutoPlayIntervals[sliderId]);
                        delete this.popupAutoPlayIntervals[sliderId];
                    }

                    // Use modern animation with specific type
                    this.animateSliderOut(popup, displayType, animationType, () => { });
                }
            } else {
                this.hideAllSliderOverlays();
            }
        }

        hideAllSliderOverlays() {
            const popups = document.querySelectorAll('.pushonica-slider-popup');
            let animationsCompleted = 0;
            const totalPopups = popups.length;

            if (totalPopups === 0) return;

            // Clear all autoPlay intervals for popups
            if (this.popupAutoPlayIntervals) {
                for (const sliderId in this.popupAutoPlayIntervals) {
                    clearInterval(this.popupAutoPlayIntervals[sliderId]);
                }
                this.popupAutoPlayIntervals = {};
            }

            popups.forEach(popup => {
                if (popup.style.display === 'block') {
                    const displayType = popup.classList.contains('pushonica-display-tabs') ? 'tabs' :
                        popup.classList.contains('pushonica-display-grid') ? 'grid' : 'slider';

                    // Get animation type from each popup's dataset
                    const animationType = popup.dataset.animationType || 'slide';

                    // Extract slider ID from popup class to clear its interval
                    const sliderIdMatch = popup.className.match(/pushonica-slider-(\d+)/);
                    const sliderId = sliderIdMatch ? sliderIdMatch[1] : null;

                    this.animateSliderOut(popup, displayType, animationType, () => {
                        animationsCompleted++;
                        if (animationsCompleted === totalPopups) {
                            // All animations completed
                        }
                    });
                } else {
                    animationsCompleted++;
                }
            });
        }

        // Yeni yardımcı fonksiyonlar
        generatePopupContent(sliderConfig, displayType) {
            const settings = sliderConfig.settings || {};
            const poweredByHtml = `
                <a href="https://dijitaldesin.com" target="_blank" class="pushonica-powered-by-slider" title="Powered by Dijitaldesin">
                    <img src="https://dijitaldesin.com/wp-content/uploads/2023/02/dijitaldesin.svg" alt="Dijitaldesin">
                </a>`;

            switch (displayType) {
                case 'slider':
                    return `
                        <div class="popup-backdrop">
                            <div class="popup-content popup-content-slider">
                                <div class="popup-header">
                                    <h3>${sliderConfig.name || 'Önerilen Ürünler'}</h3>
                                    <button class="popup-close"><svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg></button>
                                </div>
                                <div class="popup-slider-container">
                                    ${this.generateSliderHTML(sliderConfig)}
                                </div>
                                ${poweredByHtml}
                            </div>
                        </div>
                    `;

                case 'grid':
                    return `
                        <div class="popup-backdrop">
                            <div class="popup-content popup-content-grid">
                                <div class="popup-header">
                                    <h3>${sliderConfig.name || 'Önerilen Ürünler'}</h3>
                                    <button class="popup-close"><svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg></button>
                                </div>
                                <div class="popup-grid-container">
                                    ${this.generateGridHTML(sliderConfig)}
                                </div>
                                ${poweredByHtml}
                            </div>
                        </div>
                    `;

                case 'tabs':
                    return `
                        <div class="popup-sidebar">
                            <div class="popup-header">
                                <h3>${sliderConfig.name || 'Önerilen Ürünler'}</h3>
                                <button class="popup-close"><svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg></button>
                            </div>
                            <div class="popup-tabs-container">
                                ${this.generateTabsHTML(sliderConfig)}
                            </div>
                            ${poweredByHtml}
                        </div>
                    `;

                default:
                    return this.generatePopupContent(sliderConfig, 'slider');
            }
        }

        generateGridHTML(config) {
            const items = config.items || [];
            const settings = config.settings || {};

            let itemsHTML = '';
            items.forEach((item, index) => {
                itemsHTML += `
                    <div class="grid-item" data-index="${index}">
                        ${settings.showProductImage !== false ? `
                            <div class="product-image" style="aspect-ratio: ${this.getAspectRatio(settings.imageAspectRatio)}">
                                <img src="${item.productImage || ''}" alt="${item.productTitle || ''}" loading="lazy" style="opacity: 0; transition: opacity 0.5s ease;" onload="this.style.opacity='1'" onerror="this.style.display='none'">
                            </div>
                        ` : ''}
                        <div class="product-title">${item.productTitle || ''}</div>
                        ${settings.showProductDescription ? `
                            <div class="product-description">${this.getItemDescription(item, settings.descriptionSource)}</div>
                        ` : ''}
                        <div class="product-footer">
                            ${this.getPriceHTML(item, settings)}
                            <a class="product-link" href="${item.productUrl || '#'}" target="_blank" rel="noopener">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="color: #9ca3af;">
                                    <path d="M7 17L17 7"></path>
                                    <path d="M7 7h10v10"></path>
                                </svg>
                                İncele
                            </a>
                        </div>
                    </div>
                `;
            });

            return `<div class="grid-wrapper">${itemsHTML}</div>`;
        }

        generateTabsHTML(config) {
            const items = config.items || [];
            const settings = config.settings || {};

            let itemsHTML = '';
            items.forEach((item, index) => {
                itemsHTML += `
                    <div class="tabs-item" data-index="${index}">
                        ${settings.showProductImage !== false ? `
                            <div class="product-image" style="aspect-ratio: ${this.getAspectRatio(settings.imageAspectRatio)}; display: flex; align-items: center; justify-content: center; min-width: 90px; width: 90px; height: 90px;">
                                <img src="${item.productImage || ''}" alt="${item.productTitle || ''}" loading="lazy" onerror="this.style.display='none'" style="width: 100%; height: 100%; object-fit: cover; border-radius: 6px; max-width: 90px; max-height: 90px; opacity: 0; transition: opacity 0.5s ease;" onload="this.style.opacity='1'">
                            </div>
                        ` : ''}
                        <div class="product-content">
                        <div class="product-title">${item.productTitle || ''}</div>
                        ${settings.showProductDescription ? `
                            <div class="product-description">${this.getItemDescription(item, settings.descriptionSource)}</div>
                        ` : ''}
                            <div class="product-footer">
                                ${this.getPriceHTML(item, settings)}
                                <a class="product-link" href="${item.productUrl || '#'}" target="_blank" rel="noopener">
                                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="color: #9ca3af;">
                                        <path d="M7 17L17 7"></path>
                                        <path d="M7 7h10v10"></path>
                                    </svg>
                                    İncele
                                </a>
                            </div>
                        </div>
                    </div>
                `;
            });

            return `<div class="tabs-wrapper">${itemsHTML}</div>`;
        }

        applyBasePopupStyles(popup, settings, displayType) {
            popup.style.position = 'fixed';
            popup.style.zIndex = '1000000';
            popup.style.fontFamily = '"Poppins", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif';

            // Set positioning without transform (animation system will handle transform)
            switch (displayType) {
                case 'slider':
                    popup.style.top = '50%';
                    popup.style.left = '50%';
                    // Remove transform - let animation system handle it
                    popup.style.width = 'min(800px, 90vw)';
                    popup.style.maxHeight = '80vh';
                    break;

                case 'grid':
                    popup.style.top = '50%';
                    popup.style.left = '50%';
                    // Remove transform - let animation system handle it
                    popup.style.width = 'min(900px, 95vw)';
                    popup.style.maxHeight = '85vh';
                    break;

                case 'tabs':
                    popup.style.top = '0';
                    popup.style.right = '0';
                    popup.style.height = '100vh';
                    popup.style.width = '400px';
                    popup.style.maxWidth = '90vw';
                    break;
            }
        }

        applyDisplayTypeStyles(popup, displayType) {
            const backdrop = popup.querySelector('.popup-backdrop');
            const content = popup.querySelector('.popup-content');
            const sidebar = popup.querySelector('.popup-sidebar');

            if (displayType === 'tabs' && sidebar) {
                // Sidebar styling
                sidebar.style.backgroundColor = '#ffffff';
                sidebar.style.boxShadow = '-5px 0 15px rgba(0, 0, 0, 0.1)';
                sidebar.style.overflowY = 'auto';
                sidebar.style.height = '100%';
            } else if (backdrop && content) {
                // Modal styling with backdrop
                backdrop.style.position = 'fixed';
                backdrop.style.top = '0';
                backdrop.style.left = '0';
                backdrop.style.width = '100%';
                backdrop.style.height = '100%';
                backdrop.style.backgroundColor = 'rgba(0, 0, 0, 0.5)';
                backdrop.style.display = 'flex';
                backdrop.style.alignItems = 'center';
                backdrop.style.justifyContent = 'center';
                backdrop.style.zIndex = '1000000';

                content.style.backgroundColor = '#ffffff';
                content.style.borderRadius = '12px';
                content.style.boxShadow = '0 10px 30px rgba(0, 0, 0, 0.2)';
                content.style.overflow = 'hidden';
                content.style.maxHeight = '80vh';
                content.style.overflowY = 'auto';
            }
        }

        // Modern Animation System - Multiple Animation Types
        createModernAnimation(animationType) {
            animationType = animationType || 'slide';
            return {
                duration: 400, // Smooth 400ms animation
                easing: 'cubic-bezier(0.25, 0.46, 0.45, 0.94)', // Smooth easing curve

                // Initial state based on animation type
                getInitialState: (displayType) => {
                    if (displayType === 'tabs') {
                        return {
                            opacity: 0,
                            transform: 'translateX(100%)', // Slide from right
                            backdropOpacity: 0
                        };
                    }

                    // Different animations for slider and grid
                    switch (animationType) {
                        case 'slide':
                            return {
                                opacity: 0,
                                transform: 'translateY(40px) scale(0.95)',
                                backdropOpacity: 0
                            };

                        case 'fade':
                            return {
                                opacity: 0,
                                transform: 'translateY(0) scale(1)',
                                backdropOpacity: 0
                            };

                        case 'zoom':
                            return {
                                opacity: 0,
                                transform: 'translateY(0) scale(0.5)',
                                backdropOpacity: 0
                            };

                        default:
                            return {
                                opacity: 0,
                                transform: 'translateY(40px) scale(0.95)',
                                backdropOpacity: 0
                            };
                    }
                },

                // Final state - same for all animation types
                getFinalState: (displayType) => {
                    if (displayType === 'tabs') {
                        return {
                            opacity: 1,
                            transform: 'translateX(0)',
                            backdropOpacity: 1
                        };
                    }

                    return {
                        opacity: 1,
                        transform: 'translateY(0) scale(1)',
                        backdropOpacity: 1
                    };
                },

                // Exit state based on animation type
                getExitState: (displayType) => {
                    if (displayType === 'tabs') {
                        return {
                            opacity: 0,
                            transform: 'translateX(100%)', // Exit to the right
                            backdropOpacity: 0
                        };
                    }

                    // Different exit animations
                    switch (animationType) {
                        case 'slide':
                            return {
                                opacity: 0,
                                transform: 'translateY(60px) scale(0.9)',
                                backdropOpacity: 0
                            };

                        case 'fade':
                            return {
                                opacity: 0,
                                transform: 'translateY(0) scale(1)',
                                backdropOpacity: 0
                            };

                        case 'zoom':
                            return {
                                opacity: 0,
                                transform: 'translateY(0) scale(0.3)',
                                backdropOpacity: 0
                            };

                        default:
                            return {
                                opacity: 0,
                                transform: 'translateY(60px) scale(0.9)',
                                backdropOpacity: 0
                            };
                    }
                }
            };
        }

        // Performance-optimized animation applier
        applyAnimationState(popup, state, displayType, duration) {
            duration = duration || 0;
            const content = popup.querySelector('.popup-content, .popup-sidebar');
            const backdrop = popup.querySelector('.popup-backdrop');

            // Only show dots if showDots setting is true
            const sliderId = popup.className.match(/pushonica-slider-(\d+)/)?.[1];
            const sliderConfig = this.sliders.find(s => s.config.sliderId == sliderId || s.config.id == sliderId)?.config;
            const settings = sliderConfig?.settings || {};
            const showDots = this.getShowDots(settings);

            // Find any dots container
            const dotsContainer = popup.querySelector('.slider-dots');

            // If showDots is false, ensure dots are hidden
            if (!showDots && dotsContainer) {
                dotsContainer.style.display = 'none';
                dotsContainer.style.visibility = 'hidden';
                dotsContainer.style.opacity = '0';
            } else if (showDots && dotsContainer) {
                dotsContainer.setAttribute('style', 'display: flex !important; visibility: visible !important; opacity: 1 !important; z-index: 10 !important; margin: 16px auto 8px !important; justify-content: center !important; gap: 6px !important;');
            }

            if (!content) return;

            // Use requestAnimationFrame for smooth performance
            requestAnimationFrame(() => {
                // Main popup styles
                popup.style.pointerEvents = state.opacity > 0 ? 'auto' : 'none';

                if (duration > 0) {
                    const animation = this.createModernAnimation();
                    content.style.transition = `opacity ${duration}ms ${animation.easing}, transform ${duration}ms ${animation.easing}`;
                    if (backdrop) {
                        backdrop.style.transition = `opacity ${duration}ms ${animation.easing}`;
                    }
                } else {
                    content.style.transition = 'none';
                    if (backdrop) backdrop.style.transition = 'none';
                }

                // Apply transforms and opacity
                content.style.opacity = state.opacity;
                content.style.transform = state.transform;

                // Handle backdrop separately for better performance
                if (backdrop && state.backdropOpacity !== undefined) {
                    backdrop.style.opacity = state.backdropOpacity;
                }

                // Force hardware acceleration for better performance
                content.style.willChange = duration > 0 ? 'transform, opacity' : 'auto';
                if (backdrop) {
                    backdrop.style.willChange = duration > 0 ? 'opacity' : 'auto';
                }
            });
        }

        // Smooth show animation with animation type support
        animateSliderIn(popup, displayType, animationType, sliderConfig = null) {
            animationType = animationType || 'slide';
            const animation = this.createModernAnimation(animationType);
            const initialState = animation.getInitialState(displayType);
            const finalState = animation.getFinalState(displayType);

            // Set initial state immediately
            popup.style.display = 'block';
            this.applyAnimationState(popup, initialState, displayType, 0);

            // Trigger animation to final state
            requestAnimationFrame(() => {
                setTimeout(() => {
                    this.applyAnimationState(popup, finalState, displayType, animation.duration);

                    // Initialize the slider dots after animation completes
                    this.ensureSliderDotsVisible(popup, sliderConfig);
                }, 10);
            });

        }

        // Helper method to ensure slider dots are visible and created only if showDots is true AND we're in slider display type
        ensureSliderDotsVisible(popup, providedConfig = null) {
            // Find any settings stored in the popup
            const sliderId = popup.className.match(/pushonica-slider-popup-([\w-]+)/)?.[1];

            if (!sliderId) {
                console.log('[ProductSlider] Cannot find slider ID in popup class:', popup.className);
                return;
            }

            // Check if this is tabs or grid display type - never show dots in these modes
            const isTabsType = popup.classList.contains('pushonica-display-tabs');
            const isGridType = popup.classList.contains('pushonica-display-grid');

            if (isTabsType || isGridType) {
                const existingDotsContainer = popup.querySelector('.slider-dots');
                if (existingDotsContainer) {
                    existingDotsContainer.remove();
                }
                return;
            }

            const sliderConfig = providedConfig || this.sliders.find(s => s.config.sliderId == sliderId || s.config.id == sliderId)?.config;
            if (!sliderConfig) {
                console.log('[ProductSlider] Cannot find slider config for ID:', sliderId);
                return;
            }

            const settings = sliderConfig.settings || {};
            const showDots = this.getShowDots(settings);


            // If showDots is false, remove any existing dots container and exit
            if (!showDots) {
                const existingDotsContainer = popup.querySelector('.slider-dots');
                if (existingDotsContainer) {
                    existingDotsContainer.remove();
                }
                return;
            }

            // Find the dots container or create one if missing (only if showDots is true)
            let dotsContainer = popup.querySelector('.slider-dots');
            if (!dotsContainer) {
                dotsContainer = document.createElement('div');
                dotsContainer.className = 'slider-dots';

                // Find a place to append the dots
                const sliderWrapper = popup.querySelector('.slider-wrapper');
                if (sliderWrapper) {
                    sliderWrapper.appendChild(dotsContainer);
                } else {
                    // Try to find a suitable container
                    const container = popup.querySelector('.popup-slider-container') ||
                        popup.querySelector('.slider-container') ||
                        popup;
                    container.appendChild(dotsContainer);
                }
            }

            // Make sure it's visible with minimal styling
            dotsContainer.setAttribute('style', `
                display: flex !important;
                visibility: visible !important;
                opacity: 1 !important;
                z-index: 10 !important;
                margin: 16px auto 8px !important;
                justify-content: center !important;
                gap: 6px !important;
                min-height: 16px !important;
                position: relative !important;
                pointer-events: auto !important;
            `);

            // Find the slider items and track
            const track = popup.querySelector('.slider-track');
            const items = popup.querySelectorAll('.slider-item, .product-item');

            // Force at least one item per view for calculation
            const itemsPerView = Math.max(1, this.getItemsPerView(settings));

            // Calculate maximum index based on items and itemsPerView
            const itemCount = items.length;
            const maxIndex = Math.max(0, Math.ceil(itemCount - itemsPerView));

            // Create one dot for each possible slider position
            // If there are more than 10 items, we'll display all dots to ensure full navigation
            const dotCount = Math.max(1, maxIndex + 1);

            // Check for existing active index before clearing
            let activeIndex = 0;
            const existingActiveDot = dotsContainer.querySelector('.slider-dot.active');
            if (existingActiveDot) {
                activeIndex = parseInt(existingActiveDot.getAttribute('data-index') || '0');
            }

            // Clear existing dots
            while (dotsContainer.firstChild) {
                dotsContainer.removeChild(dotsContainer.firstChild);
            }

            // Create new dots with simplified styles
            for (let i = 0; i < dotCount; i++) {
                const dot = document.createElement('button');
                dot.className = 'slider-dot';
                dot.setAttribute('data-index', i);
                dot.setAttribute('aria-label', `Slide ${i + 1}`);
                if (i === activeIndex) dot.classList.add('active');

                // Set clean, minimal style
                dot.setAttribute('style', `
                    width: 8px !important;
                    height: 8px !important;
                    background-color: ${i === activeIndex ? '#667eea' : 'rgba(200, 200, 200, 0.5)'} !important;
                    border-radius: 50% !important;
                    margin: 0 3px !important;
                    border: none !important;
                    display: block !important;
                    visibility: visible !important;
                    opacity: ${i === activeIndex ? '1' : '0.8'} !important;
                    cursor: pointer !important;
                    pointer-events: auto !important;
                    position: relative !important;
                    z-index: 10 !important;
                `);

                dotsContainer.appendChild(dot);
            }

            // Force a reflow to ensure dots are rendered
            dotsContainer.offsetHeight;
        }

        // Helper method to create dots only when showDots is true AND we're in slider mode
        createForcedDots(popup, dotsContainer) {
            // First check if this is tabs or grid display type - never show dots in these modes
            const isTabsType = popup.classList.contains('pushonica-display-tabs');
            const isGridType = popup.classList.contains('pushonica-display-grid');

            if (isTabsType || isGridType) {
                return;
            }

            // Find the slider ID to check the showDots setting
            const sliderId = popup.className.match(/pushonica-slider-(\d+)/)?.[1];
            const sliderConfig = this.sliders.find(s => s.config.sliderId == sliderId || s.config.id == sliderId)?.config;
            const settings = sliderConfig?.settings || {};
            const showDots = this.getShowDots(settings);

            // Exit early if showDots is false
            if (!showDots) {
                return;
            }

            // If dotsContainer wasn't provided, try to find it or create it
            if (!dotsContainer) {
                dotsContainer = popup.querySelector('.slider-dots');
                if (!dotsContainer) {
                    dotsContainer = document.createElement('div');
                    dotsContainer.className = 'slider-dots';
                    // Try to find a good place to append
                    const container = popup.querySelector('.popup-slider-container') ||
                        popup.querySelector('.slider-container') ||
                        popup.querySelector('.popup-content') ||
                        popup;
                    container.appendChild(dotsContainer);
                }
            }

            // Set minimalist styling
            dotsContainer.setAttribute('style', `
                display: flex !important;
                visibility: visible !important;
                opacity: 1 !important;
                z-index: 10 !important;
                margin: 16px auto 8px !important;
                justify-content: center !important;
                gap: 6px !important;
                min-height: 16px !important;
                position: relative !important;
                pointer-events: auto !important;
            `);

            // Clear any existing dots
            while (dotsContainer.firstChild) {
                dotsContainer.removeChild(dotsContainer.firstChild);
            }

            // Try to find items to determine how many dots to create
            const items = popup.querySelectorAll('.product-item, .slider-item');
            const itemCount = items.length;

            // If we have items, create one dot for each possible position
            const dotCount = Math.max(5, itemCount > 0 ? itemCount : 5);

            for (let i = 0; i < dotCount; i++) {
                const dot = document.createElement('button');
                dot.className = i === 0 ? 'slider-dot active' : 'slider-dot';
                dot.setAttribute('data-index', i);
                dot.setAttribute('aria-label', `Slide ${i + 1}`);

                // Apply clean, minimal style
                dot.setAttribute('style', `
                    width: 8px !important;
                    height: 8px !important;
                    background-color: ${i === 0 ? '#667eea' : 'rgba(200, 200, 200, 0.5)'} !important;
                    border-radius: 50% !important;
                    margin: 0 3px !important;
                    border: none !important;
                    display: block !important;
                    visibility: visible !important;
                    opacity: ${i === 0 ? '1' : '0.8'} !important;
                    cursor: pointer !important;
                    pointer-events: auto !important;
                    position: relative !important;
                    z-index: 10 !important;
                `);

                dotsContainer.appendChild(dot);
            }

        }

        // Smooth hide animation with animation type support
        animateSliderOut(popup, displayType, animationType, callback) {
            animationType = animationType || 'slide';
            const animation = this.createModernAnimation(animationType);
            const exitState = animation.getExitState(displayType);

            // Animate to exit state
            this.applyAnimationState(popup, exitState, displayType, animation.duration);

            // Clean up after animation completes
            setTimeout(() => {
                popup.style.display = 'none';
                const content = popup.querySelector('.popup-content, .popup-sidebar');
                if (content) {
                    content.style.willChange = 'auto';
                    content.style.transition = 'none';
                }
                const backdrop = popup.querySelector('.popup-backdrop');
                if (backdrop) {
                    backdrop.style.willChange = 'auto';
                    backdrop.style.transition = 'none';
                }
                if (callback) callback();
            }, animation.duration + 50); // Small buffer for smooth completion
        }

        initializeSliderFunctionality(container, config) {
            // Manual navigation function that can be called from anywhere
            window.pushonicaSliderNavigate = function (direction, sliderId) {

                const slider = document.querySelector(`.pushonica-slider-${sliderId}`);
                if (!slider) {
                    console.log('[ProductSlider] Slider not found:', sliderId);
                    return;
                }

                const track = slider.querySelector('.slider-track');
                const items = slider.querySelectorAll('.slider-item');

                if (!track || items.length === 0) {
                    console.log('[ProductSlider] No track or items found');
                    return;
                }

                // Get the current transform value
                const currentTransform = track.style.transform || 'translateX(0px)';
                const currentTranslateX = parseInt(currentTransform.replace(/translateX\((.*)px\)/, '$1')) || 0;

                // Calculate item width
                const itemWidth = items[0].offsetWidth + 20; // Add gap

                // Calculate the new position
                let newTranslateX = currentTranslateX;
                if (direction === 'prev') {
                    newTranslateX = Math.min(0, currentTranslateX + itemWidth);
                } else if (direction === 'next') {
                    const maxScroll = -(items.length * itemWidth - track.parentElement.offsetWidth);
                    newTranslateX = Math.max(maxScroll, currentTranslateX - itemWidth);
                }

                track.style.transform = `translateX(${newTranslateX}px)`;
            };

            // Add global click listener for slider arrows
            document.addEventListener('click', function (e) {
                if (e.target.closest('.slider-arrow')) {
                    const arrow = e.target.closest('.slider-arrow');

                    // Find the slider ID
                    const slider = arrow.closest('[class*="pushonica-slider-"]');
                    const sliderClass = Array.from(slider.classList).find(c => c.startsWith('pushonica-slider-'));
                    const sliderId = sliderClass ? sliderClass.replace('pushonica-slider-', '') : '1';

                    if (arrow.classList.contains('slider-prev')) {
                        window.pushonicaSliderNavigate('prev', sliderId);
                    }

                    if (arrow.classList.contains('slider-next')) {
                        window.pushonicaSliderNavigate('next', sliderId);
                    }
                }
            });

            // Only initialize slider functionality for slider displayType
            const displayType = config.displayType || 'slider';

            // Make sure we have the right display type class
            if (!container.classList.contains('pushonica-display-' + displayType)) {
                container.classList.add('pushonica-display-' + displayType);
            }

            if (displayType !== 'slider') {
                return;
            }

            // Check if this is a popup - if so, skip slider functionality (vertical layout)
            if (container.closest('.pushonica-slider-popup')) {
                return;
            }

            const settings = config.settings || {};
            const track = container.querySelector('.slider-track');
            const items = container.querySelectorAll('.slider-item');
            const prevBtn = container.querySelector('.slider-prev');
            const nextBtn = container.querySelector('.slider-next');
            const dotsContainer = container.querySelector('.slider-dots');

            // Add click event listeners to all items to make entire cards clickable
            const clickableItems = container.querySelectorAll('.product-item, .grid-item, .tabs-item');
            clickableItems.forEach(item => {
                item.addEventListener('click', function (e) {
                    // Don't trigger if clicking on the link itself or arrows (let default behavior work)
                    if (e.target.closest('.product-link') ||
                        e.target.closest('.slider-arrow') ||
                        e.target.closest('.slider-dot')) {
                        return;
                    }

                    // Find the product link URL
                    const link = item.querySelector('.product-link');
                    if (link && link.href) {
                        // Open in new tab like the original link
                        window.open(link.href, '_blank', 'noopener');
                    }
                });
            });

            // Make "İncele" links work by adding explicit click handlers
            const productLinks = container.querySelectorAll('.product-link');
            productLinks.forEach(link => {
                link.addEventListener('click', function (e) {
                    e.stopPropagation(); // Stop event from bubbling up to parent item
                    if (link.href) {
                        window.open(link.href, '_blank', 'noopener');
                    }
                });
            });

            if (!track || items.length === 0) {
                console.log('[ProductSlider] No track or items found, skipping functionality');
                return;
            }

            let currentIndex = 0;
            const itemsPerView = this.getItemsPerView(settings);

            // Calculate the itemWidth and gap for accurate measurements
            const itemWidth = items[0]?.offsetWidth || 0;
            const gap = 16; // Match the gap value from CSS
            const containerWidth = track.parentElement?.offsetWidth || 0;
            const totalTrackWidth = items.length * (itemWidth + gap) - gap; // Subtract final gap

            // Calculate how many complete items fit in the view
            const effectiveItemsPerView = Math.floor(containerWidth / (itemWidth + gap));

            // Calculate the correct maxIndex to ensure all items are visible
            // This new calculation ensures we can go to a position where the last item is fully visible
            // For dots and arrows consistency, we use items.length-1 as maxIndex for proper navigation
            const maxIndex = Math.max(0, items.length - 1);
            // Store the actual slide limit separately for slider positioning
            const maxSlidePosition = Math.max(0, items.length - effectiveItemsPerView);

            // Create dots
            const showDots = this.getShowDots(settings);

            if (dotsContainer && showDots) {
                // Clear any existing dots
                while (dotsContainer.firstChild) {
                    dotsContainer.removeChild(dotsContainer.firstChild);
                }

                // Make sure dots container is visible with !important
                dotsContainer.setAttribute('style', 'display: flex !important; visibility: visible !important; opacity: 1 !important; z-index: 50 !important;');

                for (let i = 0; i <= maxIndex; i++) {
                    const dot = document.createElement('button');
                    dot.className = 'slider-dot';
                    dot.setAttribute('data-index', i);
                    dot.setAttribute('aria-label', `Slide ${i + 1}`);
                    if (i === 0) dot.classList.add('active');

                    // Set explicit styles to ensure visibility
                    dot.style.width = '12px';
                    dot.style.height = '12px';
                    dot.style.backgroundColor = i === 0 ? '#667eea' : '#cccccc';
                    dot.style.borderRadius = '50%';
                    dot.style.margin = '0 4px';
                    dot.style.display = 'block';

                    dotsContainer.appendChild(dot);

                }
            }

            // Update slider position
            const updateSlider = () => {
                // Calculate item width (accounting for gap)
                const itemWidth = items[0]?.offsetWidth || 0;
                const gap = 16; // Match the gap value from CSS
                const containerWidth = track.parentElement?.offsetWidth || 0;
                const totalTrackWidth = items.length * (itemWidth + gap) - gap; // Subtract final gap

                // Calculate translateX to ensure full visibility of items
                let translateX = -(currentIndex * (itemWidth + gap));

                // Handle cases where we're beyond the actual slide limit
                if (currentIndex > maxSlidePosition) {
                    // Move to the maxSlidePosition for proper slider positioning
                    translateX = -(maxSlidePosition * (itemWidth + gap));
                }

                // Ensure we don't scroll past the end
                if (currentIndex >= maxSlidePosition) {
                    // Get container's padding
                    const containerStyle = window.getComputedStyle(track.parentElement);
                    const paddingLeft = parseInt(containerStyle.paddingLeft) || 0;
                    const paddingRight = parseInt(containerStyle.paddingRight) || 0;
                    const totalPadding = paddingLeft + paddingRight;

                    // Get the exact position that makes the last item fully visible
                    // Account for container padding exactly
                    const visibleItems = Math.floor(containerWidth / (itemWidth + gap));

                    // Calculate the exact position needed to show the last item fully
                    // For large screens, we need to consider additional right margin
                    const extraMarginForLastItem = 20; // Provide extra margin for the last item to be fully visible
                    const lastItemFullyVisiblePosition = Math.min(0, containerWidth - totalTrackWidth - totalPadding - extraMarginForLastItem);

                    // Ensure position is never positive (no empty space at start)
                    translateX = Math.min(0, lastItemFullyVisiblePosition);

                }

                // Apply the transform
                track.style.transform = `translateX(${translateX}px)`;

                // Update dots if dots are enabled and we're in slider mode
                if (dotsContainer && viewType === 'slider') { // Only update dots in slider mode
                    const dots = container.querySelectorAll('.slider-dot');

                    // Special case: if we're at or beyond maxSlidePosition, highlight the last dot
                    let effectiveIndex = currentIndex;
                    if (currentIndex >= maxSlidePosition) {
                        effectiveIndex = maxIndex;
                    }

                    if (dots.length === 0 || dots.length !== maxIndex + 1) {
                        // Clear existing dots first
                        dotsContainer.innerHTML = '';

                        // Create correct number of dots based on maxIndex
                        for (let i = 0; i <= maxIndex; i++) {
                            const dot = document.createElement('button');
                            dot.className = 'slider-dot';
                            dot.setAttribute('data-index', i);
                            dot.setAttribute('aria-label', `Slide ${i + 1}`);

                            // Set clean, minimal style
                            dot.setAttribute('style', `
                                width: 8px !important;
                                height: 8px !important;
                                background-color: ${i === effectiveIndex ? '#667eea' : 'rgba(200, 200, 200, 0.5)'} !important;
                                border-radius: 50% !important;
                                margin: 0 3px !important;
                                border: none !important;
                                display: block !important;
                                visibility: visible !important;
                                opacity: ${i === effectiveIndex ? '1' : '0.8'} !important;
                                cursor: pointer !important;
                                transition: all 0.2s ease !important;
                            `);

                            if (i === effectiveIndex) dot.classList.add('active');
                            dotsContainer.appendChild(dot);
                        }
                    } else {
                        // Update existing dots
                        dots.forEach((dot, index) => {
                            // Update class and styling
                            dot.classList.toggle('active', index === effectiveIndex);
                            dot.style.backgroundColor = index === effectiveIndex ? '#667eea' : 'rgba(200, 200, 200, 0.5)';
                            dot.style.opacity = index === effectiveIndex ? '1' : '0.8';
                        });
                    }
                } else if (dotsContainer && (viewType === 'tabs' || viewType === 'grid')) {
                    // Hide dots in tabs and grid mode
                    dotsContainer.style.display = 'none';
                }

                // Update arrow states
                if (prevBtn) {
                    prevBtn.disabled = currentIndex === 0;
                    prevBtn.style.opacity = currentIndex === 0 ? '0.3' : '1';
                }

                if (nextBtn) {
                    // Make sure the next button is disabled when we reach the last item (dot) for consistency
                    const isLastSlide = currentIndex >= maxIndex;
                    nextBtn.disabled = isLastSlide;
                    nextBtn.style.opacity = isLastSlide ? '0.3' : '1';
                }

                // Extra check to ensure the last item is fully visible when we're at or beyond the last slide position
                if (currentIndex >= maxSlidePosition) {
                    // Force to exact minimum position after render to guarantee full visibility
                    requestAnimationFrame(() => {
                        // Get container's padding
                        const containerStyle = window.getComputedStyle(track.parentElement);
                        const paddingLeft = parseInt(containerStyle.paddingLeft) || 0;
                        const paddingRight = parseInt(containerStyle.paddingRight) || 0;
                        const totalPadding = paddingLeft + paddingRight;

                        // Calculate position to ensure the last item is fully visible with extra margin
                        const extraMarginForLastItem = 20; // Additional space for better visibility
                        const lastItemFullyVisiblePosition = containerWidth - totalTrackWidth - totalPadding - extraMarginForLastItem;

                        // Ensure we don't have empty space at start
                        const minPosition = Math.min(0, lastItemFullyVisiblePosition);

                        // Apply the position
                        track.style.transform = `translateX(${minPosition}px)`;

                        // At the end, make sure the last dot is highlighted
                        const dots = container.querySelectorAll('.slider-dot');
                        if (dots.length > 0) {
                            dots.forEach((dot, index) => {
                                dot.classList.toggle('active', index === maxIndex);
                                dot.style.backgroundColor = index === maxIndex ? '#667eea' : 'rgba(200, 200, 200, 0.5)';
                                dot.style.opacity = index === maxIndex ? '1' : '0.8';
                            });
                        }

                        // Double-check: if this didn't work, try once more with a timeout
                        // This is needed for some browsers that might not apply the transform immediately
                        setTimeout(() => {
                            const currentPos = parseInt(track.style.transform.match(/translateX\((-?\d+)px\)/)?.[1] || '0');
                            if (currentPos !== minPosition) {
                                track.style.transform = `translateX(${minPosition}px)`;
                            }

                            // Final adjustment to ensure the last item is fully visible on large screens
                            const lastItem = items[items.length - 1];
                            if (lastItem) {
                                const lastItemRect = lastItem.getBoundingClientRect();
                                const containerRect = track.parentElement.getBoundingClientRect();

                                // Check if the last item is partially hidden
                                if (lastItemRect.right > containerRect.right) {
                                    // Calculate additional adjustment needed
                                    const additionalAdjustment = lastItemRect.right - containerRect.right + 20; // 20px extra margin
                                    const adjustedPosition = minPosition - additionalAdjustment;

                                    // Apply final position
                                    track.style.transform = `translateX(${adjustedPosition}px)`;
                                }
                            }
                        }, 50);
                    });
                }
            };

            // Previous button
            if (prevBtn) {
                prevBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    if (currentIndex > 0) {
                        // If we're at the end position, going backward should highlight the previous-to-last dot
                        if (currentIndex >= maxSlidePosition) {
                            currentIndex = maxIndex - 1; // Move to one before the last dot
                        } else {
                            currentIndex--;
                        }

                        // Immediately update all dots visually for better UX
                        const dots = container.querySelectorAll('.slider-dot');
                        dots.forEach((dot, index) => {
                            dot.classList.toggle('active', index === currentIndex);
                            dot.style.backgroundColor = index === currentIndex ? '#667eea' : 'rgba(200, 200, 200, 0.5)';
                            dot.style.opacity = index === currentIndex ? '1' : '0.8';
                        });

                        updateSlider();
                    }
                });
            }

            // Next button
            if (nextBtn) {
                nextBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    if (currentIndex < maxIndex) {
                        currentIndex++;

                        // Special handling for the last position
                        let effectiveIndex = currentIndex;
                        if (currentIndex >= maxSlidePosition) {
                            effectiveIndex = maxIndex; // Ensure we highlight the last dot
                        }

                        // Immediately update all dots visually for better UX
                        const dots = container.querySelectorAll('.slider-dot');
                        dots.forEach((dot, index) => {
                            dot.classList.toggle('active', index === effectiveIndex);
                            dot.style.backgroundColor = index === effectiveIndex ? '#667eea' : 'rgba(200, 200, 200, 0.5)';
                            dot.style.opacity = index === effectiveIndex ? '1' : '0.8';
                        });

                        updateSlider();
                    } else {
                        // Even at the end, make sure the last dot is highlighted
                        const dots = container.querySelectorAll('.slider-dot');
                        if (dots.length > 0) {
                            dots.forEach((dot, index) => {
                                dot.classList.toggle('active', index === maxIndex);
                                dot.style.backgroundColor = index === maxIndex ? '#667eea' : 'rgba(200, 200, 200, 0.5)';
                                dot.style.opacity = index === maxIndex ? '1' : '0.8';
                            });
                        }
                    }
                });
            }

            // Dot clicks
            if (dotsContainer && this.getShowDots(settings)) {
                dotsContainer.addEventListener('click', (e) => {
                    if (e.target.classList.contains('slider-dot')) {
                        const dotIndex = parseInt(e.target.getAttribute('data-index'));

                        // Make sure we can navigate to this position - support all available dots
                        if (dotIndex >= 0 && dotIndex <= maxIndex) {
                            // Update current index
                            currentIndex = dotIndex;

                            // Immediately update all dots visually for better UX
                            const dots = container.querySelectorAll('.slider-dot');
                            dots.forEach((dot, index) => {
                                dot.classList.toggle('active', index === currentIndex);
                                dot.style.backgroundColor = index === currentIndex ? '#667eea' : 'rgba(200, 200, 200, 0.5)';
                                dot.style.opacity = index === currentIndex ? '1' : '0.8';
                            });

                            // Update slider position
                            updateSlider();
                        }
                    }
                });
            }

            // Touch/swipe support
            this.addTouchSupport(track, () => {
                if (currentIndex < maxIndex) {
                    currentIndex++;
                    updateSlider();
                }
            }, () => {
                if (currentIndex > 0) {
                    currentIndex--;
                    updateSlider();
                }
            });

            // Initialize slider position and arrow states
            updateSlider();

            // Auto play with interval tracking
            if (this.getAutoPlay(settings) && maxIndex > 0) {
                const interval = settings.autoPlayInterval || 5000;

                // Store slider ID to manage intervals
                const sliderId = container.className.match(/pushonica-slider-(\d+)/)?.[1];

                // Clear any existing interval for this slider
                if (sliderId && this.autoPlayIntervals && this.autoPlayIntervals[sliderId]) {
                    clearInterval(this.autoPlayIntervals[sliderId]);
                }

                // Initialize autoPlayIntervals object if it doesn't exist
                if (!this.autoPlayIntervals) {
                    this.autoPlayIntervals = {};
                }

                const intervalId = setInterval(() => {
                    // Only advance if not being interacted with
                    if (!container.dataset.userInteracting) {
                        if (currentIndex < maxIndex) {
                            currentIndex++;
                        } else {
                            currentIndex = 0;
                        }

                        // Update dots visually
                        const dots = container.querySelectorAll('.slider-dot');
                        dots.forEach((dot, index) => {
                            dot.classList.toggle('active', index === currentIndex);
                            dot.style.backgroundColor = index === currentIndex ? '#667eea' : 'rgba(200, 200, 200, 0.5)';
                            dot.style.opacity = index === currentIndex ? '1' : '0.8';
                        });

                        updateSlider();
                    }
                }, interval);

                // Store interval ID for later cleanup
                if (sliderId) {
                    this.autoPlayIntervals[sliderId] = intervalId;
                }

                // Add user interaction detection
                container.addEventListener('mouseenter', () => {
                    container.dataset.userInteracting = 'true';
                });

                container.addEventListener('mouseleave', () => {
                    container.dataset.userInteracting = 'false';
                });

                // Also pause on touch
                container.addEventListener('touchstart', () => {
                    container.dataset.userInteracting = 'true';
                });

                container.addEventListener('touchend', () => {
                    // Use timeout to allow click events to complete first
                    setTimeout(() => {
                        container.dataset.userInteracting = 'false';
                    }, 1000);
                });
            }

            // Initial update
            updateSlider();

            // Add window resize listener for better responsiveness on large screens
            let resizeTimeout;
            window.addEventListener('resize', () => {
                // Debounce to prevent too many updates
                clearTimeout(resizeTimeout);
                resizeTimeout = setTimeout(() => {
                    // Update slider positioning after resize, especially important for large screens
                    updateSlider();
                }, 200);
            });

            console.log('[ProductSlider] Slider functionality initialized successfully');
        }

        getItemsPerView(settings) {
            const width = window.innerWidth;

            // Ensure we have valid settings object
            const validSettings = settings || {};

            if (width < 768) {
                // Mobile: Use the exact value from settings or fall back to default
                return typeof validSettings.itemsPerViewMobile !== 'undefined' ? validSettings.itemsPerViewMobile : 1;
            } else if (width < 1024) {
                // Tablet: Use the exact value from settings or fall back to default
                return typeof validSettings.itemsPerViewTablet !== 'undefined' ? validSettings.itemsPerViewTablet : 2;
            } else {
                // Desktop: Use the exact value from settings or fall back to default
                return typeof validSettings.itemsPerView !== 'undefined' ? validSettings.itemsPerView : 4;
            }
        }

        getShowDots(settings) {
            // Ensure we have valid settings object
            const validSettings = settings || {};
            // Default to true if not explicitly set to false
            const showDots = validSettings.showDots !== false;
            return showDots;
        }

        getShowArrows(settings) {
            // Ensure we have valid settings object
            const validSettings = settings || {};
            // Default to true if not explicitly set to false
            const showArrows = validSettings.showArrows !== false;
            return showArrows;
        }

        getAutoPlay(settings) {
            // Ensure we have valid settings object
            const validSettings = settings || {};
            // Default to true if not explicitly set to false
            const autoPlay = validSettings.autoPlay !== false;
            return autoPlay;
        }

        addTouchSupport(element, onSwipeLeft, onSwipeRight) {
            let startX = 0;
            let startY = 0;

            element.addEventListener('touchstart', (e) => {
                startX = e.touches[0].clientX;
                startY = e.touches[0].clientY;
            }, { passive: false }); // passive: false is required to use preventDefault

            element.addEventListener('touchmove', (e) => {
                if (!startX || !startY) return;

                const currentX = e.touches[0].clientX;
                const currentY = e.touches[0].clientY;

                const diffX = startX - currentX;
                const diffY = startY - currentY;

                // Check if it's a horizontal swipe
                // If horizontal movement is greater than vertical movement, prevent scrolling
                if (Math.abs(diffX) > Math.abs(diffY)) {
                    if (e.cancelable) {
                        e.preventDefault();
                    }
                }
            }, { passive: false });

            element.addEventListener('touchend', (e) => {
                if (!startX || !startY) return;

                const endX = e.changedTouches[0].clientX;
                const endY = e.changedTouches[0].clientY;

                const diffX = startX - endX;
                const diffY = startY - endY;

                // Check if it's a horizontal swipe
                if (Math.abs(diffX) > Math.abs(diffY) && Math.abs(diffX) > 50) {
                    if (diffX > 0) {
                        onSwipeLeft();
                    } else {
                        onSwipeRight();
                    }
                }

                startX = 0;
                startY = 0;
            });
        }

        getAspectRatio(ratio) {
            switch (ratio) {
                case 'landscape':
                    return '4/3';  // Yatay - geniş görünüm
                case 'portrait':
                    return '3/4';   // Dikey - uzun görünüm
                case 'square':
                default:
                    return '1/1';   // Kare - varsayılan
            }
        }

        adjustColorBrightness(color, percent) {
            // Renk formatını kontrol et ve işle
            if (!color) return '#ffffff';

            // HEX formatını işle
            if (color.startsWith('#')) {
                // Kısa HEX formatını (#fff) uzun formata (#ffffff) çevir
                let hex = color.substring(1);
                if (hex.length === 3) {
                    hex = hex[0] + hex[0] + hex[1] + hex[1] + hex[2] + hex[2];
                }

                // HEX değerlerini RGB'ye çevir
                const r = parseInt(hex.substring(0, 2), 16);
                const g = parseInt(hex.substring(2, 4), 16);
                const b = parseInt(hex.substring(4, 6), 16);

                // Parlaklığı ayarla (0-100 arası, 100 = beyaz)
                const adjustedR = Math.min(255, Math.max(0, r + (255 - r) * (percent / 100)));
                const adjustedG = Math.min(255, Math.max(0, g + (255 - g) * (percent / 100)));
                const adjustedB = Math.min(255, Math.max(0, b + (255 - b) * (percent / 100)));

                // RGB'yi HEX'e çevir
                return '#' +
                    Math.round(adjustedR).toString(16).padStart(2, '0') +
                    Math.round(adjustedG).toString(16).padStart(2, '0') +
                    Math.round(adjustedB).toString(16).padStart(2, '0');
            }

            // RGB formatını işle
            if (color.startsWith('rgb')) {
                const rgbMatch = color.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)(?:,\s*[\d.]+)?\)/);
                if (rgbMatch) {
                    const r = parseInt(rgbMatch[1], 10);
                    const g = parseInt(rgbMatch[2], 10);
                    const b = parseInt(rgbMatch[3], 10);

                    // Parlaklığı ayarla
                    const adjustedR = Math.min(255, Math.max(0, r + (255 - r) * (percent / 100)));
                    const adjustedG = Math.min(255, Math.max(0, g + (255 - g) * (percent / 100)));
                    const adjustedB = Math.min(255, Math.max(0, b + (255 - b) * (percent / 100)));

                    return `rgb(${Math.round(adjustedR)}, ${Math.round(adjustedG)}, ${Math.round(adjustedB)})`;
                }
            }

            // İşlenemezse orijinal rengi döndür
            return color;
        }

        injectStyles(sliderConfig) {
            sliderConfig = sliderConfig || null;
            // Base styles - inject once
            if (!document.getElementById('pushonica-slider-base-styles')) {
                this.injectBaseStyles();
            }

            // Dynamic styles for specific slider
            if (sliderConfig) {
                this.injectSliderSpecificStyles(sliderConfig);
            }
        }

        injectBaseStyles() {
            const baseStyles = document.createElement('style');
            baseStyles.id = 'pushonica-slider-base-styles';
            baseStyles.textContent = `
                @import url('https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600;700&display=swap');

                /* Base responsive and layout styles */

                /* Base Reset for Module Content */
                .pushonica-product-slider,
                .pushonica-slider-popup {
                    box-sizing: border-box !important;
                    line-height: normal !important;
                }
                .pushonica-product-slider *,
                .pushonica-slider-popup * {
                    box-sizing: border-box !important;
                }

                .pushonica-product-slider {
                    margin: 20px auto;
                    padding: 20px;
                    border-radius: 8px;
                    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                    max-width: 1200px;
                    font-family: 'Poppins', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                }
                .slider-title {
                    font-size: 24px;
                    font-weight: bold;
                    text-align: center;
                }
                .slider-container {
                    position: relative;
                    overflow: hidden;
                    padding: 10px 0; /* Add vertical padding */
                }
                .slider-track {
                    display: flex;
                    transition: transform 0.3s ease;
                    gap: 16px;
                    padding: 15px 0;  /* Add vertical padding for better appearance */
                }
                .slider-item { flex: 0 0 calc(25% - 12px); box-sizing: border-box; }
                @media (max-width: 1023px) { .slider-item { flex: 0 0 calc(50% - 8px); } }
                @media (max-width: 767px) {
                    .slider-item {
                        flex: 0 0 calc(100% - 20px) !important;
                        max-width: calc(100% - 20px) !important;
                    }
                }

                /* Product item styles - matching preview exactly */
                .product-item {
                    border-radius: 6px;
                    padding: 15px;
                    text-align: left;
                    transition: all 0.3s ease;
                    width: 100%;
                    overflow: hidden;
                    box-sizing: border-box;
                    cursor: pointer;
                    position: relative;
                    display: flex;
                    flex-direction: column;
                }

                @media (min-width: 768px) and (max-width: 1023px) {
                    .product-item {
                        max-width: 320px;
                        box-sizing: border-box;
                        padding: 12px;
                    }
                }

                @media (max-width: 767px) {
                    .product-item {
                        width: 90%;
                        box-sizing: border-box;
                        padding: 10px;
                    }
                }
                .product-image {
                    margin: -15px -15px 10px -15px;
                    width: calc(100% + 30px);
                    overflow: hidden;
                }
                .tabs-item .product-image {
                    margin: 0 15px 0 0;
                    width: auto;
                }
                .product-image img {
                    width: 100%;
                    height: 100%;
                    object-fit: cover;
                }

                @media (min-width: 768px) and (max-width: 1023px) {
                    .product-image {
                        max-height: 220px;
                        margin: -12px -12px 10px -12px;
                        width: calc(100% + 24px);
                    }

                    .product-image img {
                        max-height: 220px;
                        object-position: center;
                    }
                }

                @media (max-width: 767px) {
                    .product-image {
                        max-height: 200px;
                        margin: -10px -10px 10px -10px;
                        width: calc(100% + 20px);
                    }

                    .product-image img {
                        max-height: 200px;
                        object-position: center;
                    }
                }
                .product-title {
                    font-size: 14px;
                    font-weight: 600;
                    margin-bottom: 8px;
                    transition: all 0.3s ease;
                    text-align: left;
                    display: -webkit-box;
                    -webkit-line-clamp: 2;
                    -webkit-box-orient: vertical;
                    overflow: hidden;
                    line-height: 1.4;
                    height: 40px; /* Fixed height for 2 lines at 14px font-size and 1.4 line-height */
                    min-height: 40px;
                    max-height: 40px;
                }
                .product-description {
                    font-size: 12px;
                    margin-bottom: 8px;
                    line-height: 1.3;
                    text-align: left;
                    white-space: nowrap;
                    overflow: hidden;
                    text-overflow: ellipsis;
                    width: 0;
                    min-width: 100%;
                    box-sizing: border-box;
                    height: 16px; /* Fixed height for single line description */
                }
                .product-footer {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    margin-top: auto;
                    height: 24px; /* Fixed height for consistent spacing */
                    min-height: 24px;
                    position: relative;
                    margin-top: 10px;
                }
                .product-price-wrap {
                    display: inline-flex;
                    align-items: baseline;
                    flex-wrap: wrap;
                    position: relative;
                }
                .product-price-original {
                    font-size: 12px;
                    color: #9ca3af;
                    text-decoration: line-through;
                    margin: 0;
                    opacity: 0.7;
                    font-weight: 400;
                    position: relative;
                    z-index: 0;
                }
                .product-price-wrap .product-price-current {
                    font-size: 18px;
                    font-weight: 700;
                    color: #111827;
                    position: relative;
                    z-index: 1;
                }
                .product-price {
                    font-size: 18px;
                    font-weight: 700;
                    color: #111827;
                    transition: all 0.3s ease;
                    text-align: left !important;
                    margin: 0;
                    flex-grow: 0;
                    flex-shrink: 0;
                }
                .product-price-kdv-dahil {
                    align-items: center;
                }
                .product-price-vat-label {
                    font-size: 14px;
                    font-weight: 500;
                    color: #374151;
                    margin: 0;
                }
                .product-price-vat-hint {
                    font-size: 12px;
                    color: #6b7280;
                    font-weight: 400;
                    margin: 0;
                }
                .product-link {
                    color: #667eea;
                    text-decoration: underline;
                    font-size: 13px;
                    font-weight: 500;
                    cursor: pointer;
                    transition: all 0.3s ease;
                    display: flex;
                    align-items: center;
                    gap: 2px;
                    flex-shrink: 0;
                    position: relative;
                    z-index: 10; /* Ensure link is above other elements */
                }
                .product-link:hover {
                    color: #764ba2;
                    text-decoration: underline;
                }
                .product-link-container .product-link {
                    color: #9ca3af;
                    text-decoration: none;
                    font-size: 13px;
                    font-weight: 500;
                    display: flex;
                    align-items: center;
                    gap: 2px;
                    padding: 0;
                    border-radius: 0;
                    transition: all 0.3s ease;
                    background: transparent;
                    border: none;
                }
                .product-link-container .product-link:hover {
                    color: #6b7280;
                    background: transparent;
                    border: none;
                    text-decoration: underline;
                }
                .product-link-container .product-link svg {
                    transition: transform 0.3s ease;
                    flex-shrink: 0;
                }
                .product-link-container .product-link:hover svg {
                    transform: translate(2px, -2px);
                }
                .arrow-icon {
                    font-size: 12px;
                    transition: transform 0.3s ease;
                }
                .product-link:hover .arrow-icon {
                    transform: translate(2px, -2px);
                }
                .product-button {
                    padding: 8px 16px;
                    border-radius: 4px;
                    font-size: 14px;
                    font-weight: 500;
                    text-align: center;
                    cursor: pointer;
                    transition: all 0.3s ease;
                    display: inline-block;
                }
                .slider-arrow {
                    position: absolute;
                    top: 50%;
                    transform: translateY(-50%);
                    border: none;
                    width: 40px;
                    height: 40px;
                    border-radius: 50%;
                    cursor: pointer !important;
                    z-index: 1000 !important;
                    transition: all 0.3s ease;
                    background: rgba(255, 255, 255, 0.9);
                    color: #333;
                    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.15);
                    pointer-events: auto !important;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    padding: 0;
                    margin: 0;
                    outline: none;
                }
                .slider-arrow svg {
                    width: 24px;
                    height: 24px;
                    display: block;
                }
                .slider-arrow:disabled {
                    opacity: 0.3;
                    cursor: not-allowed;
                    box-shadow: none;
                }
                .slider-prev { left: 2px; }
                .slider-next { right: 2px; }

                .slider-arrow:hover {
                    background-color: #ffffff;
                    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
                    transform: translateY(-50%) scale(1.1);
                }

                .slider-arrow:active {
                    transform: translateY(-50%) scale(0.95);
                    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
                }
                .slider-dots, .slider-dots-visible {
                    display: flex !important;
                    justify-content: center !important;
                    align-items: center !important;
                    gap: 6px !important;
                    margin: 16px auto 8px !important;
                    min-height: 16px !important;
                    padding: 4px 0 !important;
                    z-index: 10 !important;
                    position: relative !important;
                    width: 100% !important;
                    visibility: visible !important;
                    opacity: 1 !important;
                }

                /* Force global visibility for dots */
                .slider-dots *, .slider-dots-visible * {
                    visibility: visible !important;
                    opacity: 1 !important;
                }

                .slider-dots:empty {
                    display: none !important;
                }

                /* Clean, minimal dot styling */
                .slider-dot {
                    width: 8px !important;
                    height: 8px !important;
                    border-radius: 50% !important;
                    border: none !important;
                    cursor: pointer !important;
                    transition: all 0.2s ease !important;
                    background-color: rgba(200, 200, 200, 0.5) !important; /* Subtle gray */
                    margin: 0 3px !important;
                    padding: 0 !important;
                    box-shadow: none !important;
                    display: block !important;
                    visibility: visible !important;
                    opacity: 0.8 !important;
                    position: relative !important;
                }

                .slider-dot:hover {
                    background-color: rgba(160, 160, 160, 0.7) !important;
                }

                .slider-dot.active {
                    background-color: #667eea !important; /* Active color */
                    opacity: 1 !important;
                    transform: scale(1.2) !important; /* Slightly larger */
                }

                /* Modern Popup Base Styles - Performance Optimized & Centered */
                .pushonica-slider-popup {
                    position: fixed;
                    top: 0;
                    left: 0;
                    width: 100%;
                    height: 100%;
                    z-index: 1000000;
                    pointer-events: none;
                }

                .pushonica-slider-popup[style*="display: block"] {
                    pointer-events: auto;
                }

                .popup-backdrop {
                    position: fixed;
                    top: 0;
                    left: 0;
                    width: 100%;
                    height: 100%;
                    background: rgba(0, 0, 0, 0.5);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    z-index: 1000000;
                    backdrop-filter: blur(2px);
                    /* Hardware acceleration */
                    transform: translateZ(0);
                    will-change: opacity;
                }


                .popup-content {
                    position: relative;
                    background: #ffffff;
                    border-radius: 8px;
                    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.15), 0 8px 30px rgba(0, 0, 0, 0.1);
                    overflow: hidden;
                    max-height: 80vh;
                    /* Hardware acceleration for smooth animations */
                    transform: translateZ(0);
                    will-change: transform, opacity;
                    backface-visibility: hidden;
                    /* Smooth scrolling */
                    -webkit-overflow-scrolling: touch;
                    /* Ensure centering */
                    margin: auto;
                    padding-bottom: 30px; /* Add padding for logo */
                }

                .popup-sidebar {
                    background: #ffffff;
                    box-shadow: -5px 0 25px rgba(0, 0, 0, 0.15);
                    height: 100%;
                    display: flex;
                    flex-direction: column;
                    position: fixed;
                    top: 0;
                    right: 0; /* Position from right side */
                    width: 400px;
                    max-width: 90vw;
                    /* Hardware acceleration */
                    transform: translateZ(0);
                    will-change: transform, opacity;
                    backface-visibility: hidden;
                    padding-bottom: 30px; /* Add padding for logo */
                }

                .pushonica-powered-by-slider {
                    position: absolute;
                    bottom: 8px;
                    right: 8px;
                    z-index: 30;
                    opacity: 0.9;
                    transition: all 0.3s ease;
                    display: block;
                    line-height: 0;
                    padding: 4px;
                    border-radius: 4px;
                }
                
                .pushonica-powered-by-slider:hover {
                    opacity: 1;
                }

                .pushonica-powered-by-slider img {
                    height: 20px; 
                    width: auto;
                    display: block;
                }

                @media (max-width: 767px) {
                    .popup-content, .popup-sidebar {
                        padding-bottom: 40px;
                    }

                    .pushonica-powered-by-slider {
                        position: absolute;
                        bottom: 8px;
                        right: 8px;
                        margin: 0;
                    }
                }

                .popup-header {
                    position: sticky;
                    top: 0;
                    z-index: 10;
                    padding: 15px 20px;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    color: white;
                    border-bottom: 1px solid rgba(255, 255, 255, 0.1);
                    backdrop-filter: blur(8px);
                }

                .popup-header h3 {
                    margin: 0;
                    font-size: 18px;
                    font-weight: 600;
                }

                .popup-close {
                    background: none;
                    border: none;
                    color: white;
                    font-size: 24px;
                    cursor: pointer;
                    padding: 0;
                    width: 30px;
                    height: 30px;
                    border-radius: 50%;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    transition: all 0.3s ease;
                }

                .popup-close:hover {
                    background: rgba(255, 255, 255, 0.2);
                }

                /* Slider Modal Content - Optimized */
                .popup-content-slider {
                    width: min(75rem, 95vw);
                    display: flex;
                    flex-direction: column;
                    max-height: 90vh;
                    /* Performance optimizations */
                    contain: layout style paint;
                }

                .popup-slider-container {
                    padding: 10px 20px; /* Adjusted padding for better vertical appearance */
                    flex: 1;
                    overflow-y: auto;
                    -webkit-overflow-scrolling: touch;
                    /* Smooth scrolling performance */
                    scroll-behavior: smooth;
                }

                /* Grid Modal Content - Optimized */
                .popup-content-grid {
                    width: min(900px, 95vw);
                    display: flex;
                    flex-direction: column;
                    max-height: 85vh;
                    /* Performance optimizations */
                    contain: layout style paint;
                }

                .popup-grid-container {
                    padding: 20px;
                    flex: 1;
                    overflow-y: auto;
                    -webkit-overflow-scrolling: touch;
                    /* Smooth scrolling performance */
                    scroll-behavior: smooth;
                }

                .grid-wrapper {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
                    gap: 15px;
                    /* Performance optimization for grid */
                    contain: layout;
                }

                .grid-item {
                    border-radius: 8px;
                    padding: 15px;
                    text-align: left;
                    transition: all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94);
                    border: 1px solid #e5e5e5;
                    display: flex;
                    flex-direction: column;
                    cursor: pointer;
                    position: relative;
                    /* Hardware acceleration */
                    transform: translateZ(0);
                    will-change: transform, box-shadow;
                }

                .grid-item:hover {
                    transform: translateY(-4px) translateZ(0);
                    box-shadow: 0 8px 25px rgba(0,0,0,0.12);
                }

                .grid-item .product-image {
                    width: calc(100% + 30px);
                    margin: -15px -15px 10px -15px;
                    overflow: hidden;
                }

                .grid-item .product-image img {
                    width: 100%;
                    height: 100%;
                    object-fit: cover;
                }

                .grid-item .product-title {
                    font-size: 14px;
                    font-weight: 600;
                    margin-bottom: 8px;
                    text-align: left;
                    display: -webkit-box;
                    -webkit-line-clamp: 2;
                    -webkit-box-orient: vertical;
                    overflow: hidden;
                    line-height: 1.4;
                    height: 40px; /* Fixed height for 2 lines at 14px font-size and 1.4 line-height */
                    min-height: 40px;
                    max-height: 40px;
                }

                .grid-item .product-description {
                    font-size: 12px;
                    margin-bottom: 8px;
                    line-height: 1.3;
                    text-align: left;
                    white-space: nowrap;
                    overflow: hidden;
                    text-overflow: ellipsis;
                    width: 0;
                    min-width: 100%;
                    box-sizing: border-box;
                }

                .grid-item .product-footer {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    margin-top: 10px;
                }

                .grid-item .product-price {
                    font-size: 16px;
                    font-weight: bold;
                    text-align: left;
                    margin: 0;
                }

                .grid-item .product-link {
                    color: #667eea;
                    text-decoration: underline;
                    font-size: 14px;
                    font-weight: 500;
                    display: flex;
                    align-items: center;
                    gap: 2px;
                    flex-shrink: 0;
                }

                .grid-item .product-link-container .product-link {
                    color: #9ca3af;
                    text-decoration: none;
                    font-size: 13px;
                    font-weight: 500;
                    display: flex;
                    align-items: center;
                    gap: 2px;
                    padding: 0;
                    border-radius: 0;
                    transition: all 0.3s ease;
                    background: transparent;
                    border: none;
                }

                .grid-item .product-link-container .product-link:hover {
                    color: #6b7280;
                    background: transparent;
                    border: none;
                    text-decoration: underline;
                }

                .grid-item .product-link-container .product-link svg {
                    transition: transform 0.3s ease;
                    flex-shrink: 0;
                }

                .grid-item .product-link-container .product-link:hover svg {
                    transform: translate(2px, -2px);
                }

                /* Tabs Sidebar Content */
                .popup-tabs-container {
                    padding: 0;
                    flex: 1;
                    overflow-y: auto;
                }

                .tabs-wrapper {
                    display: flex;
                    flex-direction: column;
                    gap: 0;
                }

                .tabs-item {
                    display: flex;
                    padding: 15px;
                    border-bottom: 1px solid #e5e5e5;
                    transition: all 0.3s ease;
                    cursor: pointer;
                    align-items: center;
                    flex-direction: row;
                    justify-content: flex-start;
                }

                .tabs-item:hover {
                    background: #f8f9fa;
                }

                .tabs-item:last-child {
                    border-bottom: none;
                }

                .tabs-item .product-image {
                    flex-shrink: 0;
                    width: 90px;
                    height: 90px;
                    margin-right: 15px;
                    margin-bottom: 0;
                    overflow: hidden;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    border-radius: 6px;
                }

                .tabs-item .product-image img {
                    width: 100% !important;
                    height: 100% !important;
                    object-fit: cover !important;
                    border-radius: 6px !important;
                    max-width: 90px !important;
                    max-height: 90px !important;
                }

                .tabs-item .product-content {
                    flex: 1;
                    text-align: left;
                }

                .tabs-item .product-title {
                    font-size: 14px;
                    font-weight: 600;
                    margin-bottom: 6px;
                    line-height: 1.3;
                    text-align: left;
                    display: -webkit-box;
                    -webkit-line-clamp: 2;
                    -webkit-box-orient: vertical;
                    overflow: hidden;
                    height: 36px; /* Slightly shorter for tabs with smaller line-height of 1.3 */
                    min-height: 36px;
                    max-height: 36px;
                }

                .tabs-item .product-description {
                    font-size: 12px;
                    color: #666;
                    margin-bottom: 8px;
                    line-height: 1.4;
                    text-align: left;
                    white-space: nowrap;
                    overflow: hidden;
                    text-overflow: ellipsis;
                    width: 0;
                    min-width: 100%;
                    box-sizing: border-box;
                }

                .tabs-item .product-footer {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    width: 100%;
                    margin-top: 10px;
                }

                .tabs-item .product-price {
                    font-size: 16px;
                    font-weight: 700;
                    text-align: left;
                    margin: 0;
                }

                .tabs-item .product-link {
                    color: #667eea;
                    text-decoration: underline;
                    font-size: 12px;
                    font-weight: 500;
                    display: flex;
                    align-items: center;
                    gap: 2px;
                    flex-shrink: 0;
                }

                .tabs-item .product-link-container .product-link {
                    color: #9ca3af;
                    text-decoration: none;
                    font-size: 12px;
                    font-weight: 500;
                    display: flex;
                    align-items: center;
                    gap: 6px;
                    padding: 0;
                    border-radius: 0;
                    transition: all 0.3s ease;
                    background: transparent;
                    border: none;
                }

                .tabs-item .product-link-container .product-link:hover {
                    color: #6b7280;
                    background: transparent;
                    border: none;
                    text-decoration: underline;
                }

                .tabs-item .product-link-container .product-link svg {
                    transition: transform 0.3s ease;
                    flex-shrink: 0;
                }

                .tabs-item .product-link-container .product-link:hover svg {
                    transform: translate(2px, -2px);
                }

                /* Tablet Responsive */
                @media (min-width: 768px) and (max-width: 1023px) {
                    .popup-content-slider,
                    .popup-content-grid {
                        width: min(90vw, 700px);
                    }

                    .popup-slider-container {
                        padding: 15px 10px;
                    }

                    .slider-container {
                        padding: 0 10px;
                    }
                }

                /* Mobile Responsive */
                @media (max-width: 767px) {
                    .popup-content-slider,
                    .popup-content-grid {
                        width: 100vw;
                        max-width: calc(100vw - 20px);
                        border-radius: 0; /* Remove border radius on mobile */
                    }

                    .popup-slider-container {
                        padding: 10px 5px; /* Reduced side padding on mobile */
                    }

                    .slider-container {
                        padding: 0 5px; /* Reduced side padding on mobile */
                    }

                    .popup-sidebar {
                        width: 100vw;
                    }

                    .grid-wrapper {
                        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
                        gap: 10px;
                    }

                    .tabs-item {
                        flex-direction: row;
                        text-align: left;
                        align-items: center;
                    }

                    .tabs-item .product-image {
                        width: 90px;
                        height: 90px;
                        margin: 0 15px 0 0;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        border-radius: 6px;
                        overflow: hidden;
                    }

                    .tabs-item .product-content {
                        text-align: left;
                    }

                    .tabs-item .product-title,
                    .tabs-item .product-description {
                        text-align: left;
                    }

                    .tabs-item .product-footer {
                        justify-content: space-between;
                        text-align: left;
                    }

                    .product-footer {
                        flex-direction: row;
                        align-items: center;
                        justify-content: space-between;
                        width: 100%;
                    }

                    .product-price {
                        text-align: left;
                        margin: 0;
                    }
                }

            `;

            document.head.appendChild(baseStyles);
        }

        injectSliderSpecificStyles(sliderConfig) {
            const settings = sliderConfig.settings || {};
            const sliderId = sliderConfig.sliderId || sliderConfig.id;

            // Remove existing styles for this slider
            const existingStyle = document.getElementById(`pushonica-slider-${sliderId}-styles`);
            if (existingStyle) {
                existingStyle.remove();
            }

            const dynamicStyles = document.createElement('style');
            dynamicStyles.id = `pushonica-slider-${sliderId}-styles`;
            dynamicStyles.textContent = this.generateDynamicCSS(settings, sliderId);

            document.head.appendChild(dynamicStyles);
        }

        generateDynamicCSS(settings, sliderId) {
            // Calculate widths based on itemsPerView
            const gap = 16;
            const getWidthCss = (n) => {
                const gaps = Math.ceil(n) - 1;
                return `calc((100% - ${gaps * gap}px) / ${n})`;
            };

            const desktopN = typeof settings.itemsPerView !== 'undefined' ? settings.itemsPerView : 4;
            const tabletN = typeof settings.itemsPerViewTablet !== 'undefined' ? settings.itemsPerViewTablet : 2;
            const mobileN = typeof settings.itemsPerViewMobile !== 'undefined' ? settings.itemsPerViewMobile : 1;

            const desktopWidth = getWidthCss(desktopN);
            const tabletWidth = getWidthCss(tabletN);
            const mobileWidth = getWidthCss(mobileN);

            // Get colors with fallbacks
            const primaryColor = settings.primaryColor || '#667eea';
            const secondaryColor = settings.secondaryColor || '#764ba2';
            const backgroundColor = settings.backgroundColor || '#ffffff';
            const textColor = settings.textColor || '#333333';

            // Animation settings
            const transitionDuration = (settings.transitionDuration || '300') + 'ms';
            const enableAnimations = settings.enableAnimations !== false;

            // Display settings
            const showArrows = this.getShowArrows(settings);
            const showDots = this.getShowDots(settings);
            const showProductImage = settings.showProductImage !== false;
            const showProductPrice = settings.showProductPrice !== false;
            const showProductDescription = settings.showProductDescription || false;

            return `
                /* Dynamic styles for slider ${sliderId} */
                .pushonica-slider-${sliderId} .pushonica-product-slider {
                    background: ${backgroundColor} !important;
                    border: 2px solid ${primaryColor} !important;
                    transition: all ${transitionDuration} !important;
                }

                .pushonica-slider-${sliderId} .slider-title {
                    color: ${textColor} !important;
                }

                .pushonica-slider-${sliderId} .product-item,
                .pushonica-slider-${sliderId} .grid-item,
                .pushonica-slider-${sliderId} .tabs-item {
                    background: ${backgroundColor} !important;
                    border: 1px solid ${primaryColor} !important;
                    transition: all ${transitionDuration} !important;
                    text-align: left !important;
                }

                .pushonica-slider-${sliderId} .slider-dots {
                    display: flex !important;
                    justify-content: center !important;
                    gap: 8px !important;
                    margin-top: 20px !important;
                    padding: 10px 0 !important; /* Add padding to make dots more clickable */
                }

                .pushonica-slider-${sliderId} .slider-dot {
                    width: 12px !important;
                    height: 12px !important;
                    background-color: #cccccc !important;
                    border-radius: 50% !important;
                    border: 1px solid #999999 !important;
                    cursor: pointer !important;
                    transition: all 0.3s ease !important;
                    display: block !important;
                    padding: 0 !important;
                    margin: 0 6px !important;
                    opacity: 1 !important;
                    visibility: visible !important;
                    position: relative !important;
                    z-index: 50 !important;
                }

                .pushonica-slider-${sliderId} .slider-dot.active {
                    background-color: ${primaryColor} !important;
                    transform: scale(1.2) !important;
                    box-shadow: 0 0 0 2px ${backgroundColor}, 0 0 0 4px ${primaryColor} !important;
                }

                .pushonica-slider-${sliderId} .product-footer {
                    display: flex !important;
                    justify-content: space-between !important;
                    align-items: center !important;
                    margin-top: 10px !important;
                    width: 100% !important;
                }

                .pushonica-slider-${sliderId} .product-price {
                    text-align: left !important;
                    flex-shrink: 0 !important;
                }

                .pushonica-slider-${sliderId} .product-link {
                    color: ${primaryColor} !important;
                    text-decoration: underline !important;
                    font-size: 13px !important;
                    font-weight: 500 !important;
                    display: flex !important;
                    align-items: center !important;
                    gap: 2px !important;
                    flex-shrink: 0 !important;
                }

                .pushonica-slider-${sliderId} .product-link:hover {
                    color: ${secondaryColor} !important;
                }

                .pushonica-slider-${sliderId} .product-link-container .product-link {
                    color: #9ca3af !important;
                    text-decoration: none !important;
                    font-size: 13px !important;
                    font-weight: 500 !important;
                    display: flex !important;
                    align-items: center !important;
                    gap: 2px !important;
                    padding: 0 !important;
                    border-radius: 0 !important;
                    transition: all 0.3s ease !important;
                    background: transparent !important;
                    border: none !important;
                }

                .pushonica-slider-${sliderId} .product-link-container .product-link:hover {
                    color: #6b7280 !important;
                    background: transparent !important;
                    border: none !important;
                    text-decoration: underline !important;
                }

                .pushonica-slider-${sliderId} .product-link-container .product-link svg {
                    transition: transform 0.3s ease !important;
                    flex-shrink: 0 !important;
                }

                .pushonica-slider-${sliderId} .product-link-container .product-link:hover svg {
                    transform: translate(2px, -2px) !important;
                }

                .pushonica-slider-${sliderId} .arrow-icon {
                    font-size: 12px !important;
                    transition: transform 0.3s ease !important;
                }

                .pushonica-slider-${sliderId} .product-link:hover .arrow-icon {
                    transform: translate(2px, -2px) !important;
                }

                .pushonica-slider-${sliderId} .product-item:hover,
                .pushonica-slider-${sliderId} .grid-item:hover,
                .pushonica-slider-${sliderId} .tabs-item:hover {
                    background: ${primaryColor}15 !important;
                    ${enableAnimations ? `
                        box-shadow: 0 4px 12px rgba(0,0,0,0.3) !important;
                    ` : ''}
                    cursor: pointer !important;
                }

                .pushonica-slider-${sliderId} .product-title {
                    color: ${textColor} !important;
                    transition: all ${transitionDuration} !important;
                }

                .pushonica-slider-${sliderId} .product-description {
                    color: #666 !important;
                    white-space: nowrap !important;
                    overflow: hidden !important;
                    text-overflow: ellipsis !important;
                    width: 0 !important;
                    min-width: 100% !important;
                    box-sizing: border-box !important;
                    ${!showProductDescription ? 'display: none !important;' : ''}
                }

                .pushonica-slider-${sliderId} .product-price {
                    color: ${primaryColor} !important;
                    transition: all ${transitionDuration} !important;
                    ${!showProductPrice ? 'display: none !important;' : ''}
                }

                .pushonica-slider-${sliderId} .product-image {
                    ${!showProductImage ? 'display: none !important;' : ''}
                }

                .pushonica-slider-${sliderId} .product-button {
                    background: ${primaryColor} !important;
                    color: white !important;
                    transition: all ${transitionDuration} !important;
                }

                .pushonica-slider-${sliderId} .product-button:hover {
                    background: ${secondaryColor} !important;
                    ${enableAnimations ? `
                        transform: translateY(-1px) !important;
                        box-shadow: 0 4px 12px rgba(0,0,0,0.2) !important;
                    ` : ''}
                }

                /* Custom styled arrows */
                .pushonica-slider-${sliderId} .slider-arrow {
                    background: rgba(255, 255, 255, 0.95) !important;
                    color: ${primaryColor} !important;
                    box-shadow: 0 3px 10px ${primaryColor}40 !important;
                    border: 1px solid ${primaryColor}20 !important;
                }

                .pushonica-slider-${sliderId} .slider-arrow:hover {
                    background: white !important;
                    color: ${secondaryColor} !important;
                    box-shadow: 0 5px 15px ${primaryColor}60 !important;
                }

                .pushonica-slider-${sliderId} .slider-arrow:disabled {
                    color: #999 !important;
                    background: rgba(255, 255, 255, 0.7) !important;
                    box-shadow: none !important;
                    border: 1px solid #eee !important;
                }

                /* Popup Header Styling - Tema tipine göre değişecek */
                .pushonica-slider-${sliderId} .popup-header {
                    background: ${settings.theme === 'colorful' ?
                `linear-gradient(135deg, ${primaryColor} 0%, ${secondaryColor} 100%)` :
                (settings.theme === 'minimal' ? backgroundColor : primaryColor)} !important;
                    color: ${settings.theme === 'minimal' ? primaryColor : 'white'} !important;
                    border-bottom: ${settings.theme === 'minimal' ? `1px solid #ccc` : 'none'} !important;
                }

                /* Tema tipine göre çarpı iconu renklendirme */
                .pushonica-slider-${sliderId} .popup-header .popup-close {
                    color: ${settings.theme === 'minimal' ? primaryColor : 'white'} !important;
                    ${settings.theme === 'minimal' ? `border: 1px solid ${primaryColor}30 !important;` : ''}
                }

                .pushonica-slider-${sliderId} .popup-header .popup-close:hover {
                    background: ${settings.theme === 'minimal' ?
                `${primaryColor}20` :
                'rgba(255, 255, 255, 0.2)'} !important;
                }

                /* Slider Type Specific */
                .pushonica-display-slider.pushonica-slider-${sliderId} .popup-slider-container .slider-track {
                    display: flex !important;
                    transition: transform 0.3s ease !important;
                    padding: 15px 0 !important; /* Add vertical padding for cards */
                    align-items: stretch !important; /* Make all cards the same height */
                }

                .pushonica-display-slider.pushonica-slider-${sliderId} .product-item {
                    flex: 0 0 ${desktopWidth} !important;
                    max-width: ${desktopWidth} !important;
                    box-sizing: border-box !important;
                    display: flex !important;
                    flex-direction: column !important;
                    height: 100% !important; /* Make sure all cards have the same height */
                }

                @media (max-width: 1023px) {
                    .pushonica-display-slider.pushonica-slider-${sliderId} .product-item {
                        flex: 0 0 ${tabletWidth} !important;
                        max-width: ${tabletWidth} !important;
                    }
                }

                @media (max-width: 767px) {
                    .pushonica-display-slider.pushonica-slider-${sliderId} .product-item {
                        flex: 0 0 ${mobileWidth} !important;
                        max-width: ${mobileWidth} !important;
                        box-sizing: border-box !important;
                        padding: 10px !important;
                    }
                }

                /* Grid Type Specific - High Priority */
                .pushonica-display-grid.pushonica-slider-${sliderId} .popup-grid-container .grid-wrapper {
                    display: grid !important;
                    grid-template-columns: repeat(${settings.itemsPerView || 4}, 1fr) !important;
                    gap: 15px !important;
                }

                @media (max-width: 1023px) {
                    .pushonica-display-grid.pushonica-slider-${sliderId} .popup-grid-container .grid-wrapper {
                        grid-template-columns: repeat(${settings.itemsPerViewTablet || 2}, 1fr) !important;
                    }
                }

                @media (max-width: 767px) {
                    .pushonica-display-grid.pushonica-slider-${sliderId} .popup-grid-container .grid-wrapper {
                        grid-template-columns: repeat(${settings.itemsPerViewMobile || 1}, 1fr) !important;
                    }
                }

                .pushonica-display-grid.pushonica-slider-${sliderId} .grid-item {
                    border-radius: 4px !important;
                    padding: 15px !important;
                    text-align: left !important;
                    display: flex !important;
                    flex-direction: column !important;
                }

                .pushonica-display-grid.pushonica-slider-${sliderId} .grid-item .product-title,
                .pushonica-display-grid.pushonica-slider-${sliderId} .grid-item .product-description {
                    text-align: left !important;
                }

                .pushonica-display-grid.pushonica-slider-${sliderId} .grid-item .product-description {
                    white-space: nowrap !important;
                    overflow: hidden !important;
                    text-overflow: ellipsis !important;
                    width: 0 !important;
                    min-width: 100% !important;
                    box-sizing: border-box !important;
                }

                .pushonica-display-grid.pushonica-slider-${sliderId} .grid-item .product-footer {
                    display: flex !important;
                    justify-content: space-between !important;
                    align-items: center !important;
                    margin-top: auto !important;
                }

                .pushonica-display-grid.pushonica-slider-${sliderId} .grid-item .product-price {
                    text-align: left !important;
                    margin: 0 !important;
                }

                .pushonica-display-grid.pushonica-slider-${sliderId} .grid-item .product-link {
                    text-decoration: underline !important;
                    display: flex !important;
                    align-items: center !important;
                    gap: 2px !important;
                    flex-shrink: 0 !important;
                }

                .pushonica-display-grid.pushonica-slider-${sliderId} .grid-item .product-link-container {
                    margin-top: 10px !important;
                    display: flex !important;
                    justify-content: flex-end !important;
                }

                .pushonica-display-grid.pushonica-slider-${sliderId} .grid-item .product-link-container .product-link {
                    color: #9ca3af !important;
                    text-decoration: none !important;
                    font-size: 13px !important;
                    font-weight: 500 !important;
                    display: flex !important;
                    align-items: center !important;
                    gap: 2px !important;
                    padding: 0 !important;
                    border-radius: 0 !important;
                    transition: all 0.3s ease !important;
                    background: transparent !important;
                    border: none !important;
                }

                .pushonica-display-grid.pushonica-slider-${sliderId} .grid-item .product-link-container .product-link:hover {
                    color: #6b7280 !important;
                    background: transparent !important;
                    border: none !important;
                    text-decoration: underline !important;
                }

                .pushonica-display-grid.pushonica-slider-${sliderId} .grid-item .product-link-container .product-link svg {
                    transition: transform 0.3s ease !important;
                    flex-shrink: 0 !important;
                }

                .pushonica-display-grid.pushonica-slider-${sliderId} .grid-item .product-link-container .product-link:hover svg {
                    transform: translate(2px, -2px) !important;
                }

                /* Tabs Type Specific */
                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-wrapper {
                    display: flex !important;
                    flex-direction: column !important;
                    gap: 0 !important;
                }

                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-item {
                    display: flex !important;
                    padding: 15px !important;
                    border-bottom: 1px solid #e5e5e5 !important;
                    align-items: center !important;
                }

                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-item .product-image {
                    flex-shrink: 0 !important;
                    width: 90px !important;
                    height: 90px !important;
                    margin: 0 15px 0 0 !important;
                    padding: 0 !important;
                    display: flex !important;
                    align-items: center !important;
                    justify-content: center !important;
                    border-radius: 6px !important;
                    overflow: hidden !important;
                    align-self: center !important;
                    min-width: 90px !important;
                    max-width: 90px !important;
                }

                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-item .product-content {
                    flex: 1 !important;
                    text-align: left !important;
                }

                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-item .product-title,
                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-item .product-description {
                    text-align: left !important;
                }

                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-item .product-description {
                    white-space: nowrap !important;
                    overflow: hidden !important;
                    text-overflow: ellipsis !important;
                    width: 0 !important;
                    min-width: 100% !important;
                    box-sizing: border-box !important;
                }

                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-item .product-footer {
                    display: flex !important;
                    justify-content: space-between !important;
                    align-items: center !important;
                }

                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-item .product-price {
                    text-align: left !important;
                    margin: 0 !important;
                }

                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-item .product-link {
                    text-decoration: underline !important;
                    display: flex !important;
                    align-items: center !important;
                    gap: 2px !important;
                    flex-shrink: 0 !important;
                }

                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-item .product-link-container {
                    margin-top: 10px !important;
                    display: flex !important;
                    justify-content: flex-end !important;
                }

                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-item .product-link-container .product-link {
                    color: #9ca3af !important;
                    text-decoration: none !important;
                    font-size: 12px !important;
                    font-weight: 500 !important;
                    display: flex !important;
                    align-items: center !important;
                    gap: 2px !important;
                    padding: 0 !important;
                    border-radius: 0 !important;
                    transition: all 0.3s ease !important;
                    background: transparent !important;
                    border: none !important;
                }

                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-item .product-link-container .product-link:hover {
                    color: #6b7280 !important;
                    background: transparent !important;
                    border: none !important;
                    text-decoration: underline !important;
                }

                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-item .product-link-container .product-link svg {
                    transition: transform 0.3s ease !important;
                    flex-shrink: 0 !important;
                }

                .pushonica-display-tabs.pushonica-slider-${sliderId} .tabs-item .product-link-container .product-link:hover svg {
                    transform: translate(2px, -2px) !important;
                }

                /* Hide slider controls for non-slider types */
                .pushonica-display-grid.pushonica-slider-${sliderId} .slider-arrow,
                .pushonica-display-tabs.pushonica-slider-${sliderId} .slider-arrow {
                    display: none !important;
                }

                /* Control dot visibility based on showDots setting */
                /* Control dot visibility based on settings and display type */
                ${!showDots ? `
                /* Hide dots for ALL types when showDots is false */
                .pushonica-display-grid.pushonica-slider-${sliderId} .slider-dots,
                .pushonica-display-tabs.pushonica-slider-${sliderId} .slider-dots,
                .pushonica-display-slider.pushonica-slider-${sliderId} .slider-dots {
                    display: none !important;
                    visibility: hidden !important;
                    opacity: 0 !important;
                    height: 0 !important;
                    overflow: hidden !important;
                    margin: 0 !important;
                    padding: 0 !important;
                }` : `
                /* Show dots ONLY for slider type when showDots is true */
                .pushonica-display-slider.pushonica-slider-${sliderId} .slider-dots {
                    display: flex !important;
                    visibility: visible !important;
                    opacity: 1 !important;
                }
                `}

                /* ALWAYS hide dots for non-slider types regardless of showDots value */
                .pushonica-display-grid.pushonica-slider-${sliderId} .slider-dots,
                .pushonica-display-tabs.pushonica-slider-${sliderId} .slider-dots {
                    display: none !important;
                    visibility: hidden !important;
                    opacity: 0 !important;
                    height: 0 !important;
                    overflow: hidden !important;
                    margin: 0 !important;
                    padding: 0 !important;
                }

                ${this.generateThemeCSS(settings, sliderId)}

                /* Floating Button Styles */
                .pushonica-floating-button {
                    position: fixed;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    padding: 12px 24px;
                    border-radius: 4px;
                    box-shadow: 0 4px 20px rgba(102, 126, 234, 0.3);
                    cursor: pointer;
                    transition: all 0.3s ease;
                    font-family: 'Poppins', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                    font-weight: 600;
                    font-size: 13px;
                    display: flex;
                    align-items: center;
                    gap: 8px;
                    z-index: 999999;
                    user-select: none;
                    white-space: nowrap;
                    transform: rotate(-90deg);
                    right: 20px;
                    transform-origin: right center;
                }
                .pushonica-floating-button:hover {
                    transform: rotate(-90deg) scale(1.05);
                    box-shadow: 0 6px 25px rgba(102, 126, 234, 0.4);
                }
                .floating-btn-icon { font-size: 18px; display: flex; align-items: center; justify-content: center; }
                .floating-btn-text { white-space: nowrap; }
            `;
        }
    }

    // Initialize ProductSlider
    const productSlider = new ProductSlider();
    window.productSlider = productSlider;

})();
