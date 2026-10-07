
(function () {
    'use strict';

    // Konfigürasyon
    const CONFIG = {
        // R2 Base URL - yorum dosyalarının bulunduğu temel URL
        r2BaseUrl: 'https://assets.pushonica.com/reviews',

        // Company ID - script'ten çekilecek
        companyId: null,

        // Company ID'yi çıkarma fonksiyonu (gift-wheel/slider yapısında)
        getCompanyId: function () {
            // Önce manuel set edilmiş companyId'yi kontrol et
            if (this.companyId) {
                return this.companyId;
            }

            // Script tag üzerindeki data-module-id attribute'unu kontrol et
            const scripts = document.querySelectorAll('script[data-module-id]');
            for (let script of scripts) {
                const id = script.getAttribute('data-module-id');
                if (id) return id;
            }

            return null;
        },

        // Hedef CSS sınıfları
        targetClasses: {
            ratingElement: '.Formline.puanVer',
            commentsContainer: '.commentTiciNewComment',
            productDetailMainRow: '.ProductDetailMainRow'
        },

        getBarkod: function () {
            try {
                if (typeof productDetailModel !== 'undefined' && productDetailModel && productDetailModel.product && productDetailModel.product.barkod) {
                    return productDetailModel.product.barkod.toString().trim();
                }
            } catch (e) { }
            return null;
        },

        /**
         * Bir ürün kartı elementinden barkod bilgisini çıkar
         * data-barkod, data-product-barkod, data-id, link URL vb. kaynaklardan dener
         */
        getCardBarkod: function (productItemEl) {
            if (!productItemEl) return null;

            // 1. data-barkod özniteliği
            const attrs = ['data-barkod', 'data-product-barkod', 'data-barcode', 'data-product-id'];
            for (const attr of attrs) {
                const val = productItemEl.getAttribute(attr);
                if (val && val.trim()) return val.trim();
            }

            // 2. İç elementlerde data-barkod ara
            const innerWithBarkod = productItemEl.querySelector('[data-barkod], [data-product-barkod], [data-barcode]');
            if (innerWithBarkod) {
                const val = innerWithBarkod.getAttribute('data-barkod') ||
                    innerWithBarkod.getAttribute('data-product-barkod') ||
                    innerWithBarkod.getAttribute('data-barcode');
                if (val && val.trim()) return val.trim();
            }

            // 3. .productBarcode içindeki span
            const barcodeSpan = productItemEl.querySelector('.productBarcode span');
            if (barcodeSpan && barcodeSpan.textContent) {
                return barcodeSpan.textContent.trim();
            }

            return null;
        }
    };

    // Global değişkenler
    let isInitialized = false;
    let allComments = [];
    let filteredComments = [];
    let currentFilters = {
        rating: 'all',
        selectedRatings: ['all'],
        sortBy: 'newest'
    };
    let currentSlide = 0;
    let commentsPerSlide = 10;
    let initialCommentsPerLoad = 10;
    let commentsIncrement = 10;

    /**
     * Sistemi başlat
     */
    function init() {
        if (isInitialized) return;

        function waitForDOM() {
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', function () {
                    waitForDOM();
                });
                return;
            }
            if (!isInitialized) {
                isInitialized = true;
                setupReviewsIntegration();
            }
        }
        waitForDOM();
    }

    /**
     * Ana kurulum fonksiyonu
     */
    function setupReviewsIntegration() {
        try {
            addModernStyles();

            // Detay sayfası kontrolü
            const barkod = CONFIG.getBarkod();
            if (barkod) {
                setupAverageRatingDisplay();
                setupModernCommentsContainer();
                fetchReviews(barkod);
            }

            // Listeleme sayfası: ürün kartlarına değerlendirme enjekte et
            setupCardReviews();

        } catch (error) {
            console.error('Yorum entegrasyonu kurulumunda hata:', error);
        }
    }

    /**
     * Ortalama puan gösterimini kur
     */
    function setupAverageRatingDisplay() {
        const targetElement = document.querySelector(CONFIG.targetClasses.ratingElement);
        if (!targetElement) {
            return;
        }

        // Orijinal elementi gizle
        targetElement.style.display = 'none';

        // Özel puan gösterimini oluştur
        const ratingContainer = document.createElement('div');
        ratingContainer.className = 'custom-rating-display';
        ratingContainer.innerHTML = `
            <div class="rating-summary" id="ratingSummary">
                <div class="rating-loading">
                    <div class="skeleton-rating-container">
                        <div class="skeleton-rating-score"></div>
                        <div class="skeleton-stars"></div>
                        <span class="skeleton-dot">•</span>
                        <div class="skeleton-rating-count"></div>
                    </div>
                </div>
            </div>
        `;

        // Orijinal elementin yanına ekle
        targetElement.parentNode.insertBefore(ratingContainer, targetElement.nextSibling);
    }

    /**
     * Rating hesaplama ve analiz yardımcı fonksiyonları
     */
    const RatingAnalytics = {
        /**
         * Yorumlardan ortalama rating hesapla
         * @param {Array} comments - Yorum dizisi
         * @returns {number} Ortalama rating (0-5 arası)
         */
        calculateAverageRating(comments) {
            if (!Array.isArray(comments) || comments.length === 0) {
                return 0;
            }

            const validRatings = comments
                .map(comment => StarRating.normalizeRating(comment.rating))
                .filter(rating => rating > 0);

            if (validRatings.length === 0) {
                return 0;
            }

            const sum = validRatings.reduce((acc, rating) => acc + rating, 0);
            return Math.round((sum / validRatings.length) * 10) / 10; // 1 decimal places
        },

        /**
         * Rating dağılımını hesapla
         * @param {Array} comments - Yorum dizisi
         * @returns {Object} Rating breakdown (1-5 star counts)
         */
        calculateRatingBreakdown(comments) {
            const breakdown = { 1: 0, 2: 0, 3: 0, 4: 0, 5: 0 };

            if (!Array.isArray(comments)) {
                return breakdown;
            }

            comments.forEach(comment => {
                const rating = Math.round(StarRating.normalizeRating(comment.rating));
                if (rating >= 1 && rating <= 5) {
                    breakdown[rating]++;
                }
            });

            return breakdown;
        },

        /**
         * Rating istatistiklerini hesapla
         * @param {Array} comments - Yorum dizisi
         * @returns {Object} Detaylı rating istatistikleri
         */
        calculateRatingStats(comments) {
            const breakdown = this.calculateRatingBreakdown(comments);
            const average = this.calculateAverageRating(comments);
            const total = comments.length;

            const photoCount = comments.reduce((count, comment) => {
                return count + comment.photos.length;
            }, 0);


            // Rating yüzdelerini hesapla
            const percentages = {};
            Object.keys(breakdown).forEach(rating => {
                percentages[rating] = total > 0 ? Math.round((breakdown[rating] / total) * 100) : 0;
            });

            return {
                average,
                total,
                breakdown,
                percentages,
                hasRatings: total > 0,
                photoCount
            };
        }
    };

    /**
     * Filter seçenekleri oluşturma yardımcı fonksiyonları
     */
    const FilterHelpers = {
        /**
         * Rating filter seçenekleri oluştur
         * @returns {string} HTML options
         */
        generateRatingFilterOptions() {
            const options = ['<option value="all">Tüm Puanlar</option>'];

            for (let rating = 5; rating >= 1; rating--) {
                options.push(`<option value="${rating}">${rating} Yıldız</option>`);
            }

            return options.join('');
        }
    };

    /**
     * Modern yorum konteynerini ProductDetailMainRow altında kur
     */
    function setupModernCommentsContainer() {
        try {
            // Element bulma işlemini retry mekanizması ile yap
            function findAndSetupContainer(retryCount = 0) {
                const productDetailMainRow = document.querySelector(CONFIG.targetClasses.productDetailMainRow);

                if (!productDetailMainRow && retryCount < 5) {
                    findAndSetupContainer(retryCount + 1)
                    return;
                }

                if (!productDetailMainRow) {
                    return;
                }

                if (document.getElementById('modernReviewsContainer')) {
                    return;
                }

                createModernContainer(productDetailMainRow);
            }

            findAndSetupContainer();

        } catch (error) {
            console.error('Modern yorum konteyneri kurulumunda hata:', error);
        }
    }

    /**
     * Modern container'ı oluştur
     */
    function createModernContainer(productDetailMainRow) {
        try {
            const modernContainer = document.createElement('div');
            modernContainer.id = 'modernReviewsContainer';
            modernContainer.className = 'modern-reviews-container';
            modernContainer.innerHTML = `
                <h1 class="reviews-main-title">Ürün Değerlendirmeleri</h1>
                <div class="reviews-header">
                    <h2 class="reviews-title">
                        <div class="title-rating-container">
                            ${StarRating.render(0, {
                size: 'normal',
                className: 'title-rating-stars',
                showValue: false
            })}
                            <span class="title-rating-score" id="titleRatingScore">0.0</span>
                            <span class="title-dropdown-arrow" onmouseenter="showReviewsTitleTooltip(event)" onmouseleave="hideReviewsTitleTooltip()">▼</span>
                            <span class="title-rating-separator">•</span>
                            <span class="title-rating-count" id="titleRatingCount">0 Değerlendirme</span>
                            <span class="title-photo-icon" id="titlePhotoIcon" style="display: none;">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"></path><circle cx="12" cy="13" r="4"></circle></svg>
                            </span>
                        </div>

                        <!-- Reviews Title Tooltip -->
                        <div class="reviews-title-tooltip" id="reviewsTitleTooltip" style="display: none;">
                            <div class="tooltip-header">
                                <b class="tooltip-average" id="tooltipTitleAverage">0.0</b>
                                <span class="tooltip-title">Ortalama Puan</span>
                            </div>
                            <div class="rating-breakdown-container" id="tooltipTitleBreakdown">
                                <!-- Rating breakdown will be populated dynamically -->
                            </div>
                        </div>
                    </h2>
                </div>

                <!-- Search and Sort Controls -->
                <div class="reviews-search-controls">
                    <div class="search-input-container">
                        <input type="text" class="reviews-search-input" id="reviewsSearchInput" placeholder="Değerlendirmelerde Ara">
                        <span class="search-icon">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
                        </span>
                    </div>
                    <div class="sort-and-filter-container">
                        <div class="rating-filter-container">
                        <div class="rating-filter-dropdown">
                            <div class="rating-filter-button" id="ratingFilterButton">
                                <span class="filter-text">Puan</span>
                                <span class="filter-arrow">▼</span>
                            </div>
                            <div class="rating-filter-menu" id="ratingFilterMenu">
                                <div class="rating-filter-options">
                                    <div class="rating-option" data-rating="all">
                                        <input type="checkbox" id="rating-all" checked>
                                        <label for="rating-all">
                                            <span class="rating-text">Tüm Puanlar</span>
                                        </label>
                                    </div>
                                    <div class="rating-option" data-rating="5">
                                        <input type="checkbox" id="rating-5">
                                        <label for="rating-5">
                                            <span class="rating-score">5.0</span>
                                            <span class="rating-stars" data-rating="5"></span>
                                            <span class="rating-count">(0)</span>
                                        </label>
                                    </div>
                                    <div class="rating-option" data-rating="4">
                                        <input type="checkbox" id="rating-4">
                                        <label for="rating-4">
                                            <span class="rating-score">4.0</span>
                                            <span class="rating-stars" data-rating="4"></span>
                                            <span class="rating-count">(0)</span>
                                        </label>
                                    </div>
                                    <div class="rating-option" data-rating="3">
                                        <input type="checkbox" id="rating-3">
                                        <label for="rating-3">
                                            <span class="rating-score">3.0</span>
                                            <span class="rating-stars" data-rating="3"></span>
                                            <span class="rating-count">(0)</span>
                                        </label>
                                    </div>
                                    <div class="rating-option" data-rating="2">
                                        <input type="checkbox" id="rating-2">
                                        <label for="rating-2">
                                            <span class="rating-score">2.0</span>
                                            <span class="rating-stars" data-rating="2"></span>
                                            <span class="rating-count">(0)</span>
                                        </label>
                                    </div>
                                    <div class="rating-option" data-rating="1">
                                        <input type="checkbox" id="rating-1">
                                        <label for="rating-1">
                                            <span class="rating-score">1.0</span>
                                            <span class="rating-stars" data-rating="1"></span>
                                            <span class="rating-count">(0)</span>
                                        </label>
                                    </div>
                                </div>
                                <div class="filter-actions">
                                    <button class="btn-clear" id="clearRatingFilter">Temizle</button>
                                    <button class="btn-apply" id="applyRatingFilter">Uygula</button>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="sort-select-container">
                        <select class="reviews-sort-select" id="reviewsSortSelect">
                            <option value="newest">En Yeni</option>
                            <option value="oldest">En Eski</option>
                            <option value="highest">En Yüksek Puan</option>
                            <option value="lowest">En Düşük Puan</option>
                            <option value="most_liked">En Beğenilen</option>
                        </select>
                    </div>
                    </div>
                </div>

                <!-- Photo Reviews Section -->
                <div class="photo-reviews-section" id="photoReviewsSection" style="display: none;">
                    <h3 class="photo-reviews-title">Fotoğraflı Değerlendirmeler</h3>
                    <div class="photo-reviews-container" id="photoReviewsContainer">
                        <!-- Photos will be populated dynamically -->
                    </div>
                </div>

                <div class="reviews-content-slider" id="reviewsContent">
                    <div class="reviews-loading">
                        <div class="loading-spinner"></div>
                        <span>Yorumlar yükleniyor...</span>
                    </div>
                </div>

                <div class="show-more-container" id="showMoreContainer" style="display: none;">
                    <hr class="expand-button-line">
                    <button type="button" class="show-more-btn" id="showMoreBtn" onclick="showMoreComments()">
                        <span>Daha Fazla Göster</span>
                        <span class="show-more-arrow">›</span>
                    </button>
                </div>

                <div class="reviews-footer" style="display: flex; justify-content: space-between; align-items: center; margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666;">
                    <div class="footer-left">
                        Kaynak: Trendyol
                    </div>
                    <div class="footer-right">
                        <a href="https://dijitaldesin.com" target="_blank" style="text-decoration: none; display: flex; align-items: center;">
                            <img src="https://dijitaldesin.com/wp-content/uploads/2023/02/dijitaldesin.svg" alt="Dijitaldesin" style="height: 24px;">
                        </a>
                    </div>
                </div>
            `;

            // ProductDetailMainRow'un ALTINA ekle (nextSibling kullan)
            if (productDetailMainRow.nextSibling) {
                productDetailMainRow.parentNode.insertBefore(modernContainer, productDetailMainRow.nextSibling);
            } else {
                productDetailMainRow.parentNode.appendChild(modernContainer);
            }

            setTimeout(function () {
                setupSearchAndSort();
            }, 100);

        } catch (error) {
            console.error('Modern container oluşturma hatası:', error);
        }
    }

    /**
     * Yorum verisini normalize et (güvenlik kontrolü)
     */
    function normalizeComment(comment) {
        try {
            // Try different possible field names for like count and handle different data types
            let likeCount = 0;

            // Check for different possible field names and data types
            const possibleFields = ['like_count', 'likeCount', 'likes', 'like_number', 'beğeni', 'begeni', 'helpful_count', 'helpfulCount'];

            for (const field of possibleFields) {
                if (comment[field] !== undefined && comment[field] !== null) {
                    const value = comment[field];
                    if (typeof value === 'number' && !isNaN(value) && value >= 0) {
                        likeCount = value;
                        break;
                    } else if (typeof value === 'string') {
                        const parsed = parseInt(value, 10);
                        if (!isNaN(parsed) && parsed >= 0) {
                            likeCount = parsed;
                            break;
                        }
                    }
                }
            }

            const normalized = {
                rating: typeof comment.rating === 'number' ? comment.rating : 0,
                user: comment.user || 'Anonim',
                date: comment.date || 'Tarih bilinmiyor',
                elit_customer: !!comment.elit_customer,
                comment: comment.comment || 'Yorum metni bulunamadı',
                photos: Array.isArray(comment.photos) ? comment.photos : [],
                like_count: likeCount
            };

            return normalized;
        } catch (error) {
            return {
                rating: 0,
                user: 'Anonim',
                date: 'Tarih bilinmiyor',
                elit_customer: false,
                comment: 'Yorum metni bulunamadı',
                photos: [],
                like_count: 0
            };
        }
    }

    /**
     * API'den yorumları getir
     */

    /**
     * Previous slide function - direct function call instead of event listener
     */
    window.goToPrevSlide = function (event) {
        try {
            if (event) {
                event.preventDefault();
                event.stopPropagation();
            }
            if (currentSlide > 0) {
                currentSlide--;
                updateSliderDisplay();
            }
        } catch (error) {
            console.error('Previous slide hatası:', error);
        }
    };

    /**
     * Next slide function - direct function call instead of event listener
     */
    window.goToNextSlide = function (event) {
        try {
            if (event) {
                event.preventDefault();
                event.stopPropagation();
            }
            const maxSlides = Math.ceil(filteredComments.length / commentsPerSlide);
            if (currentSlide < maxSlides - 1) {
                currentSlide++;
                updateSliderDisplay();
            }
        } catch (error) {
            console.error('Next slide hatası:', error);
        }
    };

    /**
     * Show more comments function - loads more comments incrementally
     */
    window.showMoreComments = function () {
        try {
            // Increase comments per slide incrementally
            commentsPerSlide += commentsIncrement;

            // Don't exceed total available comments
            if (commentsPerSlide >= filteredComments.length) {
                commentsPerSlide = filteredComments.length;
            }

            currentSlide = 0;

            // Update display
            updateSliderDisplay();

            // Hide button if all comments are shown
            const showMoreContainer = document.getElementById('showMoreContainer');
            if (showMoreContainer) {
                if (commentsPerSlide >= filteredComments.length) {
                    showMoreContainer.style.display = 'none';
                } else {
                    showMoreContainer.style.display = 'flex';
                }
            }
        } catch (error) {
            console.error('Show more comments hatası:', error);
        }
    };

    function fetchReviews(barkod) {
        try {
            // Company ID'yi al
            const companyId = CONFIG.getCompanyId();
            if (!companyId) {
                console.error('TrendyolReviews: companyId bulunamadı. Script tag\'a data-module-id attribute ekleyin veya TrendyolReviews.configure({ companyId: "xxx" }) kullanın.');
                return;
            }

            if (!barkod) {
                console.error('TrendyolReviews: barkod bulunamadı.');
                return;
            }

            const apiUrl = `${CONFIG.r2BaseUrl}/customer_${companyId}/comments/${barkod}_comment.json`;

            updateModernLoadingState('🔄 Yorumlar getiriliyor...');

            fetch(apiUrl)
                .then(response => {
                    if (!response.ok) {
                        throw new Error(`HTTP hatası! durum: ${response.status}`);
                    }
                    return response.json();
                })
                .then(data => {
                    displayReviews(data);
                })
                .catch(error => {
                    handleFetchError(error);
                });
        } catch (error) {
            console.error('Yorum getirme işleminde hata:', error);
        }
    }

    /**
     * Yorumları UI'da göster
     */
    function displayReviews(data) {
        try {
            if (!data || !Array.isArray(data) || data.length === 0) {
                console.log('Yorum verisi boş veya array değil, showNoReviewsMessage çağrılıyor');
                showNoReviewsMessage();
                return;
            }

            const normalizedComments = data.map(comment => normalizeComment(comment));

            allComments = normalizedComments;
            window.allComments = allComments;
            filteredComments = [...allComments];

            updateAverageRatingDisplay({ Comments: normalizedComments });
            setupSearchAndSort();
            updatePhotoReviews(normalizedComments);
            const modernContainer = document.getElementById('modernReviewsContainer');
            if (modernContainer) {
                updateModernCommentsDisplay();
            }

        } catch (error) {
            console.error('Yorumları gösterirken hata:', error);
        }
    }

    /**
     * Modern yorum container'ını güncelle
     */
    function updateModernCommentsDisplay() {
        try {
            function updateWithRetry(retryCount = 0) {
                const modernContainer = document.getElementById('modernReviewsContainer');
                const reviewsCount = document.getElementById('reviewsCount');
                const reviewsContent = document.getElementById('reviewsContent');

                if ((!modernContainer || !reviewsCount || !reviewsContent) && retryCount < 3) {
                    setTimeout(() => updateWithRetry(retryCount + 1), 300);
                    return;
                }

                if (!modernContainer) {
                    console.warn('Modern container bulunamadı!');
                    return;
                }

                // Elementleri güncelle
                if (reviewsCount) {
                    reviewsCount.textContent = `(${allComments.length})`;
                }

                if (reviewsContent) {
                    displayFilteredComments();
                } else {
                    console.error('reviewsContent elementi bulunamadı!');
                }
            }

            updateWithRetry();

        } catch (error) {
            console.error('Modern yorum container güncellemede hata:', error);
        }
    }

    /**
     * Filtrelenmiş yorumları slider ile göster
     */
    function displayFilteredComments() {
        try {
            const reviewsContent = document.getElementById('reviewsContent');

            if (!reviewsContent) {
                console.error('reviewsContent elementi bulunamadı!');
                return;
            }

            if (!filteredComments || filteredComments.length === 0) {
                reviewsContent.innerHTML = `
                    <div class="no-reviews-filtered">
                        <div class="no-reviews-icon">
                            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
                        </div>
                        <h3>Filtreye uygun yorum bulunamadı</h3>
                        <p>Farklı filtre seçeneklerini deneyebilirsiniz.</p>
                        <button type="button" class="clear-filters-btn" onclick="clearAllFilters(event)">
                            Filtreleri Temizle
                        </button>
                    </div>
                `;
                const showMoreContainer = document.getElementById('showMoreContainer');
                if (showMoreContainer) showMoreContainer.style.display = 'none';
                return;
            }

            // Reset to initial load when filters change
            if (commentsPerSlide > initialCommentsPerLoad && commentsPerSlide !== filteredComments.length) {
                commentsPerSlide = initialCommentsPerLoad;
            }

            // Don't exceed available comments
            if (commentsPerSlide > filteredComments.length) {
                commentsPerSlide = filteredComments.length;
            }

            // Show comments
            updateSliderDisplay();

            // Show "Show More" button if there are more comments to load
            const showMoreContainer = document.getElementById('showMoreContainer');
            if (showMoreContainer) {
                showMoreContainer.style.display = (filteredComments.length > commentsPerSlide) ? 'flex' : 'none';
            }
        } catch (error) {
            console.error('Filtrelenmiş yorumları gösterirken hata:', error);
        }
    }

    /**
     * Slider görünümünü güncelle
     */
    function updateSliderDisplay() {
        try {
            const reviewsContent = document.getElementById('reviewsContent');
            if (!reviewsContent) return;

            const startIndex = currentSlide * commentsPerSlide;
            const endIndex = startIndex + commentsPerSlide;
            const currentComments = filteredComments.slice(startIndex, endIndex);

            const commentsHTML = currentComments.map(comment => generateTrendyolCommentHTML(comment)).join('');

            reviewsContent.innerHTML = `
                <div class="trendyol-comments-slider">
                    ${commentsHTML}
                </div>
            `;

            // Slider kontrollerini güncelle
            updateSliderControls();

        } catch (error) {
            console.error('Slider görünümü güncellenirken hata:', error);
        }
    }

    /**
     * Slider kontrollerini güncelle
     */
    function updateSliderControls() {
        try {
            const prevBtn = document.getElementById('prevSlide');
            const nextBtn = document.getElementById('nextSlide');
            const dotsContainer = document.getElementById('sliderDots');

            const maxSlides = Math.ceil(filteredComments.length / commentsPerSlide);

            // Butonları güncelle
            if (prevBtn) {
                prevBtn.disabled = currentSlide === 0;
                prevBtn.style.opacity = currentSlide === 0 ? '0.5' : '1';
            }

            if (nextBtn) {
                nextBtn.disabled = currentSlide >= maxSlides - 1;
                nextBtn.style.opacity = currentSlide >= maxSlides - 1 ? '0.5' : '1';
            }

            // Dots'ları güncelle
            if (dotsContainer && maxSlides > 1) {
                let dotsHTML = '';
                for (let i = 0; i < maxSlides; i++) {
                    dotsHTML += `<span class="slider-dot ${i === currentSlide ? 'active' : ''}" onclick="goToSlide(${i}, event)"></span>`;
                }
                dotsContainer.innerHTML = dotsHTML;
            }

        } catch (error) {
            console.error('Slider kontrolleri güncellenirken hata:', error);
        }
    }

    /**
     * Trendyol tarzı yorum kartı HTML'i oluştur
     */
    function generateTrendyolCommentHTML(comment) {
        try {
            if (!comment) {
                console.error('Comment verisi boş!');
                return '';
            }

            const photosHTML = comment.photos && comment.photos.length > 0 ?
                `<div class="trendyol-comment-photos">
                    ${comment.photos.slice(0, 2).map(photo =>
                    `<img src="${photo}" alt="Yorum fotoğrafı" class="trendyol-comment-photo" onclick="openPhotoModal('${photo}')" onerror="this.onerror=null;this.src='data:image/svg+xml;charset=UTF-8,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20width%3D%2224%22%20height%3D%2224%22%20viewBox%3D%220%200%2024%2024%22%20fill%3D%22none%22%20stroke%3D%22%23999%22%20stroke-width%3D%222%22%20stroke-linecap%3D%22round%22%20stroke-linejoin%3D%22round%22%3E%3Crect%20x%3D%223%22%20y%3D%223%22%20width%3D%2218%22%20height%3D%2218%22%20rx%3D%222%22%20ry%3D%222%22%3E%3C%2Frect%3E%3Ccircle%20cx%3D%228.5%22%20cy%3D%228.5%22%20r%3D%221.5%22%3E%3C%2Fcircle%3E%3Cpolyline%20points%3D%2221%2015%2016%2010%205%2021%22%3E%3C%2Fpolyline%3E%3C%2Fsvg%3E';this.classList.add('error-image');">`
                ).join('')}
                    ${comment.photos.length > 2 ? `<div class="more-photos-trendyol">+${comment.photos.length - 2}</div>` : ''}
                </div>` : '';

            const eliteBadge = comment.elit_customer ? '<span class="trendyol-elite-badge">Elite Üye</span>' : '';

            const html = `
                <div class="trendyol-comment-card">
                    <div class="trendyol-comment-header">
                        <div class="trendyol-comment-rating">
                            ${StarRating.render(comment.rating || 0, {
                size: 'small',
                className: 'comment-rating-stars',
                ariaLabel: `${comment.rating || 0} yıldız değerlendirme`
            })}
                        </div>
                        <div class="trendyol-comment-meta">
                            <span class="trendyol-user-name">${comment.user || 'Anonim'}</span>
                            <span class="comment-meta-separator">•</span>
                            <span class="trendyol-comment-date">${comment.date || 'Tarih bilinmiyor'}</span>
                        </div>
                    </div>
                    <div class="trendyol-comment-body">
                        <p class="trendyol-comment-text">${comment.comment || 'Yorum metni bulunamadı'}</p>
                        ${photosHTML}
                    </div>
                    <div class="trendyol-comment-footer">
                        <div class="trendyol-comment-likes" style="margin-left: auto;">
                            <span class="like-icon">
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 9V5a3 3 0 0 0-3-3l-4 9v11h11.28a2 2 0 0 0 2-1.7l1.38-9a2 2 0 0 0-2-2.3zM7 22H4a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2h3"></path></svg>
                            </span>
                            <span class="like-count">(${comment.like_count !== undefined ? comment.like_count : 0})</span>
                        </div>
                    </div>
                </div>
            `;

            return html;
        } catch (error) {
            console.error('Trendyol yorum kartı oluşturulurken hata:', error, comment);
            return `<div class="trendyol-comment-card error">Yorum yüklenirken hata oluştu</div>`;
        }
    }

    /**
     * Tüm filtreleri temizle
     */
    window.clearAllFilters = function (event) {
        try {
            if (event) {
                event.preventDefault();
                event.stopPropagation();
            }

            // Filtreleri sıfırla
            currentFilters.rating = 'all';
            currentFilters.selectedRatings = ['all'];
            currentFilters.sortBy = 'newest';

            // Form elementlerini sıfırla
            const ratingFilter = document.getElementById('ratingFilter');
            const sortFilter = document.getElementById('sortFilter');
            const sortSelect = document.getElementById('reviewsSortSelect');
            const searchInput = document.getElementById('reviewsSearchInput');

            if (ratingFilter) ratingFilter.value = 'all';
            if (sortFilter) sortFilter.value = 'newest';
            if (sortSelect) sortSelect.value = 'newest';
            if (searchInput) searchInput.value = '';

            // Rating filter checkboxlarını da sıfırla
            const allCheckbox = document.getElementById('rating-all');
            const otherCheckboxes = document.querySelectorAll('.rating-option input[type="checkbox"]:not(#rating-all)');
            if (allCheckbox) allCheckbox.checked = true;
            if (otherCheckboxes) {
                otherCheckboxes.forEach(cb => {
                    if (cb) cb.checked = false;
                });
            }

            // Rating filter button text'ini güncelle
            const filterText = document.querySelector('.rating-filter-button .filter-text');
            if (filterText) filterText.textContent = 'Puan';

            if (ratingFilter) ratingFilter.value = 'all';
            if (sortFilter) sortFilter.value = 'newest';
            if (sortSelect) sortSelect.value = 'newest';
            if (searchInput) searchInput.value = '';

            // Slider'ı da sıfırla
            currentSlide = 0;
            commentsPerSlide = initialCommentsPerLoad; // Reset to initial load amount

            // Filtreleri uygula - use the new consistent function
            try {
                applySearchAndSort();
            } catch (error) {
                console.error('Filtreler uygulanırken hata:', error);
            }
        } catch (error) {
            console.error('Filtreleri temizlerken hata:', error);
        }
    };

    /**
     * Modern loading state güncelle
     */
    function updateModernLoadingState(message) {
        try {
            const reviewsContent = document.getElementById('reviewsContent');
            if (reviewsContent) {
                reviewsContent.innerHTML = `
                    <div class="skeleton-reviews-container">
                        <div class="skeleton-review-item">
                            <div class="skeleton-review-header">
                                <div class="skeleton-stars-rating"></div>
                                <div class="skeleton-date-small"></div>
                            </div>
                            <div class="skeleton-review-text">
                                <div class="skeleton-text-line full"></div>
                                <div class="skeleton-text-line medium"></div>
                                <div class="skeleton-text-line short"></div>
                            </div>
                            <div class="skeleton-review-footer">
                                <div class="skeleton-user-name"></div>
                                <div class="skeleton-verified"></div>
                            </div>
                        </div>

                        <div class="skeleton-review-item">
                            <div class="skeleton-review-header">
                                <div class="skeleton-stars-rating"></div>
                                <div class="skeleton-date-small"></div>
                            </div>
                            <div class="skeleton-review-text">
                                <div class="skeleton-text-line full"></div>
                                <div class="skeleton-text-line long"></div>
                            </div>
                            <div class="skeleton-review-footer">
                                <div class="skeleton-user-name"></div>
                                <div class="skeleton-verified"></div>
                            </div>
                        </div>

                        <div class="skeleton-review-item">
                            <div class="skeleton-review-header">
                                <div class="skeleton-stars-rating"></div>
                                <div class="skeleton-date-small"></div>
                            </div>
                            <div class="skeleton-review-text">
                                <div class="skeleton-text-line full"></div>
                                <div class="skeleton-text-line medium"></div>
                            </div>
                            <div class="skeleton-review-footer">
                                <div class="skeleton-user-name"></div>
                            </div>
                        </div>
                    </div>
                `;
            }
        } catch (error) {
            console.error('Modern loading state güncellemede hata:', error);
        }
    }

    /**
     * Ortalama puan gösterimini güncelle - Modern RatingAnalytics kullanır
     */
    function updateAverageRatingDisplay(data) {
        const ratingSummary = document.getElementById('ratingSummary');
        if (!ratingSummary) return;

        const comments = data.Comments || [];
        const stats = RatingAnalytics.calculateRatingStats(comments);

        // Modern StarRating component kullanarak yıldızları oluştur
        const starsHTML = StarRating.render(stats.average, {
            showValue: false,
            className: 'rating-stars-clean-display',
            ariaLabel: `${stats.average} üzerinden 5 yıldız, ${stats.total} değerlendirme`
        });

        ratingSummary.innerHTML = `
            <div class="rating-display-clean">
                <div class="rating-hover-area" onmouseenter="showRatingTooltip(event)" onmouseleave="hideRatingTooltip()">
                    <span class="rating-score-clean">${stats.average.toFixed(1)}</span>
                    <div class="rating-stars-clean-display">
                        ${starsHTML}
                    </div>
                </div>
                <span class="dot-splitter">•</span>
                <span class="rating-count-clean" onclick="scrollToComments()">
                    <span class="rating-count-number">${stats.total}</span> Değerlendirme
                </span>
                <div class="rating-tooltip-clean" id="ratingTooltipClean" style="display: none;">
                    <div class="tooltip-header">
                        <b class="tooltip-average">${stats.average.toFixed(1)}</b>
                        <span class="tooltip-title">Ortalama Puan</span>
                    </div>
                    <div class="rating-breakdown-container">
                        ${[5, 4, 3, 2, 1].map(rating => {
            const count = stats.breakdown[rating] || 0;
            const percentage = stats.total > 0 ? Math.round((count / stats.total) * 100) : 0;
            return `
                                <div class="rating-breakdown-row">
                                    <span class="rating-number">${rating}</span>
                                    <div class="rating-stars-small">
                                        ${StarRating.render(rating, {
                size: 'small',
                className: 'tooltip-stars'
            })}
                                    </div>
                                    <div class="rating-progress-bar">
                                        <div class="rating-progress-fill" data-target-width="${percentage}%" style="width: 0%"></div>
                                    </div>
                                    <span class="rating-count-number">${count}</span>
                                </div>
                            `;
        }).join('')}
                    </div>
                </div>
            </div>
        `;

        // Store rating breakdown for tooltip
        window.currentRatingBreakdown = stats.breakdown;

        // Update title rating display
        const titleRatingScore = document.getElementById('titleRatingScore');
        const titleRatingCount = document.getElementById('titleRatingCount');
        const titlePhotoIcon = document.getElementById('titlePhotoIcon');
        const titleRatingContainer = document.querySelector('.title-rating-container');

        if (titleRatingScore) {
            titleRatingScore.textContent = stats.average.toFixed(1);
        }

        if (titleRatingCount) {
            titleRatingCount.textContent = `${stats.total} Değerlendirme`;
        }

        // Check if there are comments with photos and show/hide camera icon
        if (titlePhotoIcon) {
            const hasPhotos = comments.some(comment => comment.photos && comment.photos.length > 0);
            titlePhotoIcon.style.display = hasPhotos ? 'inline-flex' : 'none';
        }

        // Update title rating stars
        if (titleRatingContainer) {
            const titleStarsContainer = titleRatingContainer.querySelector('.title-rating-stars');
            if (titleStarsContainer) {
                titleStarsContainer.innerHTML = StarRating.render(stats.average, {
                    size: 'normal',
                    className: 'title-rating-stars',
                    showValue: false
                });
            }
        }
    }

    /**
     * Update photo reviews section
     */
    function updatePhotoReviews(comments) {
        try {
            const photoReviewsSection = document.getElementById('photoReviewsSection');
            const photoReviewsContainer = document.getElementById('photoReviewsContainer');

            if (!photoReviewsSection || !photoReviewsContainer) return;

            // Get all comments with photos
            const commentsWithPhotos = comments.filter(comment => comment.photos && comment.photos.length > 0);

            if (commentsWithPhotos.length === 0) {
                photoReviewsSection.style.display = 'none';
                return;
            }

            // Show the section
            photoReviewsSection.style.display = 'block';

            // Collect all photos from all comments
            const allPhotos = [];
            commentsWithPhotos.forEach(comment => {
                comment.photos.forEach(photo => {
                    allPhotos.push({
                        url: photo,
                        commentUser: comment.user,
                        commentRating: comment.rating,
                        commentText: comment.comment
                    });
                });
            });

            // Limit to first 20 photos for performance
            const displayPhotos = allPhotos.slice(0, 20);

            // Generate HTML for photos
            const photosHTML = displayPhotos.map(photo => `
                <div class="photo-review-item" onclick="openPhotoModal('${photo.url}', '${photo.commentUser}', ${photo.commentRating}, '${photo.commentText.substring(0, 100)}...')" title="${photo.commentUser} - ${photo.commentRating} yıldız">
                    <img src="${photo.url}" alt="Kullanıcı fotoğrafı" loading="lazy" onerror="this.onerror=null;this.src='data:image/svg+xml;charset=UTF-8,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20width%3D%2224%22%20height%3D%2224%22%20viewBox%3D%220%200%2024%2024%22%20fill%3D%22none%22%20stroke%3D%22%23999%22%20stroke-width%3D%222%22%20stroke-linecap%3D%22round%22%20stroke-linejoin%3D%22round%22%3E%3Crect%20x%3D%223%22%20y%3D%223%22%20width%3D%2218%22%20height%3D%2218%22%20rx%3D%222%22%20ry%3D%222%22%3E%3C%2Frect%3E%3Ccircle%20cx%3D%228.5%22%20cy%3D%228.5%22%20r%3D%221.5%22%3E%3C%2Fcircle%3E%3Cpolyline%20points%3D%2221%2015%2016%2010%205%2021%22%3E%3C%2Fpolyline%3E%3C%2Fsvg%3E';this.classList.add('error-image');">
                </div>
            `).join('');

            photoReviewsContainer.innerHTML = photosHTML;
        } catch (error) {
            console.error('Fotoğraflı yorumlar güncellenirken hata:', error);
        }
    }

    /**
     * Setup search and sort functionality
     */
    function setupSearchAndSort() {
        try {
            // Retry mechanism for elements
            function setupWithRetry(retryCount = 0) {
                const searchInput = document.getElementById('reviewsSearchInput');
                const sortSelect = document.getElementById('reviewsSortSelect');
                const ratingFilterButton = document.getElementById('ratingFilterButton');
                const ratingFilterMenu = document.getElementById('ratingFilterMenu');

                if ((!searchInput || !sortSelect || !ratingFilterButton || !ratingFilterMenu) && retryCount < 5) {
                    setTimeout(() => setupWithRetry(retryCount + 1), 200);
                    return;
                }

                if (searchInput) {
                    searchInput.addEventListener('input', function () {
                        applySearchAndSort();
                    });
                } else {
                    console.warn('reviewsSearchInput elementi bulunamadı!');
                }

                if (sortSelect) {
                    // Set initial sort value from currentFilters
                    sortSelect.value = currentFilters.sortBy;

                    sortSelect.addEventListener('change', function () {
                        applySearchAndSort();
                    });

                    // Test the functionality immediately
                    applySearchAndSort();
                } else {
                    console.warn('reviewsSortSelect elementi bulunamadı!');
                }

                // Setup rating filter
                if (ratingFilterButton && ratingFilterMenu) {
                    setupRatingFilter(ratingFilterButton, ratingFilterMenu);
                } else {
                    console.warn('Rating filter elementleri bulunamadı!');
                }
            }

            setupWithRetry();
        } catch (error) {
            console.error('Arama ve sıralama kurulurken hata:', error);
        }
    }

    /**
     * Setup rating filter dropdown functionality
     */
    function setupRatingFilter(button, menu) {
        try {
            if (!button || !menu) {
                console.error('Rating filter elements not found:', { button, menu });
                return;
            }

            // Prevent duplicate setup
            if (button.dataset.setupComplete === 'true') {
                return;
            }
            button.dataset.setupComplete = 'true';

            // Update rating counts
            updateRatingCounts();

            // Render star ratings for each option
            renderFilterStars();

            // Toggle dropdown menu
            button.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();

                const isOpen = menu.classList.contains('show');

                // Close all other dropdowns (if any)
                document.querySelectorAll('.rating-filter-menu.show').forEach(dropdown => {
                    dropdown.classList.remove('show');
                    dropdown.parentElement.querySelector('.rating-filter-button').classList.remove('active');
                });

                // Toggle current dropdown
                if (!isOpen) {
                    menu.classList.add('show');
                    button.classList.add('active');
                } else {
                    menu.classList.remove('show');
                    button.classList.remove('active');
                }
            }, { once: false, capture: false });

            // Close dropdown when clicking outside
            document.addEventListener('click', function (e) {
                if (!button.contains(e.target) && !menu.contains(e.target)) {
                    menu.classList.remove('show');
                    button.classList.remove('active');
                }
            }, { once: false });

            // Prevent menu from closing when clicking inside it
            menu.addEventListener('click', function (e) {
                e.stopPropagation();
            });

            // Handle checkbox changes
            const checkboxes = menu.querySelectorAll('input[type="checkbox"]');
            const allCheckbox = menu.querySelector('#rating-all');

            checkboxes.forEach(checkbox => {
                checkbox.addEventListener('change', function () {
                    if (this.id === 'rating-all') {
                        // If 'All' is checked, uncheck others
                        if (this.checked) {
                            checkboxes.forEach(cb => {
                                if (cb.id !== 'rating-all') cb.checked = false;
                            });
                            currentFilters.selectedRatings = ['all'];
                        }
                    } else {
                        // If any specific rating is checked, uncheck 'All'
                        if (this.checked && allCheckbox.checked) {
                            allCheckbox.checked = false;
                        }

                        // Update selectedRatings based on checked items
                        const checkedRatings = [];
                        checkboxes.forEach(cb => {
                            if (cb.checked && cb.id !== 'rating-all') {
                                const rating = cb.closest('.rating-option').dataset.rating;
                                checkedRatings.push(rating);
                            }
                        });

                        // If no specific ratings are selected, select 'All'
                        if (checkedRatings.length === 0) {
                            allCheckbox.checked = true;
                            currentFilters.selectedRatings = ['all'];
                        } else {
                            currentFilters.selectedRatings = checkedRatings;
                        }
                    }

                    // Update button text
                    updateRatingFilterButtonText();
                });
            });

            // Clear button
            const clearBtn = menu.querySelector('#clearRatingFilter');
            if (clearBtn) {
                clearBtn.addEventListener('click', function (e) {
                    e.preventDefault();
                    e.stopPropagation();

                    try {
                        // Reset checkboxes
                        checkboxes.forEach(cb => cb.checked = false);
                        if (allCheckbox) allCheckbox.checked = true;

                        // Reset filters
                        currentFilters.selectedRatings = ['all'];

                        // Update button text
                        updateRatingFilterButtonText();

                        // Apply filters
                        applySearchAndSort();
                    } catch (error) {
                        console.error('Rating filter temizlenirken hata:', error);
                        // Hata durumunda sayfayı bozma, sadece log'la
                        return false;
                    }
                });
            }

            // Apply button
            const applyBtn = menu.querySelector('#applyRatingFilter');
            if (applyBtn) {
                applyBtn.addEventListener('click', function (e) {
                    e.preventDefault();
                    e.stopPropagation();

                    menu.classList.remove('show');
                    button.classList.remove('active');

                    try {
                        applySearchAndSort();
                    } catch (error) {
                        console.error('Rating filter uygulanırken hata:', error);
                        // Hata durumunda sayfayı bozma, sadece log'la
                        return false;
                    }
                });
            }

            // Update button text based on selection
            function updateRatingFilterButtonText() {
                try {
                    const filterText = button.querySelector('.filter-text');
                    if (!filterText) return;

                    if (!currentFilters.selectedRatings ||
                        currentFilters.selectedRatings.includes('all') ||
                        currentFilters.selectedRatings.length === 0) {
                        filterText.textContent = 'Puan';
                    } else if (currentFilters.selectedRatings.length === 1) {
                        filterText.textContent = `${currentFilters.selectedRatings[0]} ⭐`;
                    } else {
                        filterText.textContent = `${currentFilters.selectedRatings.length} Puan`;
                    }
                } catch (error) {
                    console.error('Button text güncellenirken hata:', error);
                }
            }

        } catch (error) {
            console.error('Rating filter kurulurken hata:', error);
        }
    }

    /**
     * Update rating counts based on available comments
     */
    function updateRatingCounts() {
        try {
            if (!allComments || allComments.length === 0) {
                return;
            }

            // Count ratings
            const ratingCounts = {
                5: 0,
                4: 0,
                3: 0,
                2: 0,
                1: 0
            };

            allComments.forEach(comment => {
                if (comment && comment.rating) {
                    const roundedRating = Math.round(comment.rating);
                    if (ratingCounts.hasOwnProperty(roundedRating)) {
                        ratingCounts[roundedRating]++;
                    }
                }
            });

            // Update count displays
            Object.keys(ratingCounts).forEach(rating => {
                const countElement = document.querySelector(`#rating-${rating} + label .rating-count`);
                if (countElement) {
                    countElement.textContent = `(${ratingCounts[rating]})`;
                }
            });

        } catch (error) {
            console.error('Rating count güncellenirken hata:', error);
        }
    }

    /**
     * Render star ratings for filter options
     */
    function renderFilterStars() {
        try {
            // Render stars for each rating level
            [1, 2, 3, 4, 5].forEach(rating => {
                const starContainer = document.querySelector(`label[for="rating-${rating}"] .rating-stars`);
                if (starContainer) {
                    starContainer.innerHTML = StarRating.render(rating, {
                        size: 'small',
                        showValue: false,
                        className: 'filter-stars'
                    });
                }
            });
        } catch (error) {
            console.error('Filter stars render edilirken hata:', error);
        }
    }

    /**
     * Apply search and sort filters
     */
    function applySearchAndSort() {
        try {
            const searchInput = document.getElementById('reviewsSearchInput');
            const sortSelect = document.getElementById('reviewsSortSelect');

            if (!searchInput || !sortSelect) {
                console.error('Gerekli elementler bulunamadı:', {
                    searchInput: !!searchInput,
                    sortSelect: !!sortSelect
                });
                return;
            }

            const searchTerm = searchInput.value.toLowerCase().trim();
            const sortBy = sortSelect.value;

            // Update global filter state to keep consistency
            currentFilters.sortBy = sortBy;

            // Check if we have comments to filter
            if (!allComments || !Array.isArray(allComments) || allComments.length === 0) {
                console.warn('No comments available for filtering');
                return;
            }

            // Start with all comments
            let filtered = [...allComments];

            // Apply search filter
            if (searchTerm) {
                filtered = filtered.filter(comment =>
                    comment.comment && comment.comment.toLowerCase().includes(searchTerm) ||
                    comment.user && comment.user.toLowerCase().includes(searchTerm)
                );
            }

            // Apply rating filter
            if (currentFilters.selectedRatings &&
                Array.isArray(currentFilters.selectedRatings) &&
                !currentFilters.selectedRatings.includes('all')) {

                filtered = filtered.filter(comment => {
                    if (!comment || typeof comment.rating === 'undefined' || comment.rating === null) {
                        return false;
                    }

                    const commentRating = Math.round(Number(comment.rating));
                    return currentFilters.selectedRatings.includes(commentRating.toString());
                });
            }

            // Apply sort - use consistent date parsing with Turkish month support
            // Helper function to parse Turkish dates
            function parseDate(dateString) {
                if (!dateString || dateString === 'Tarih bilinmiyor' || dateString === 'Elite Üye') {
                    return new Date(0); // Return epoch for invalid dates
                }

                // Turkish month mapping
                const turkishMonths = {
                    'Ocak': '01', 'Şubat': '02', 'Mart': '03', 'Nisan': '04',
                    'Mayıs': '05', 'Haziran': '06', 'Temmuz': '07', 'Ağustos': '08',
                    'Eylül': '09', 'Ekim': '10', 'Kasım': '11', 'Aralık': '12'
                };

                // Try to parse Turkish date format: "20 Ocak 2024"
                const turkishDateMatch = dateString.match(/(\d{1,2})\s+([A-Za-zıüöçğşİÜÖÇĞŞ]+)\s+(\d{4})/);
                if (turkishDateMatch) {
                    const [, day, monthName, year] = turkishDateMatch;
                    const month = turkishMonths[monthName];
                    if (month) {
                        return new Date(`${year}-${month}-${day.padStart(2, '0')}`);
                    }
                }

                // Fallback to regular date parsing
                const date = new Date(dateString);
                return isNaN(date.getTime()) ? new Date(0) : date;
            }

            switch (sortBy) {
                case 'oldest':
                    filtered.sort((a, b) => {
                        const dateA = parseDate(a.date);
                        const dateB = parseDate(b.date);
                        return dateA.getTime() - dateB.getTime();
                    });
                    break;
                case 'highest':
                    filtered.sort((a, b) => (b.rating || 0) - (a.rating || 0));
                    break;
                case 'lowest':
                    filtered.sort((a, b) => (a.rating || 0) - (b.rating || 0));
                    break;
                case 'most_liked':
                    filtered.sort((a, b) => (b.like_count || 0) - (a.like_count || 0));
                    break;
                case 'newest':
                default:
                    filtered.sort((a, b) => {
                        const dateA = parseDate(a.date);
                        const dateB = parseDate(b.date);
                        return dateB.getTime() - dateA.getTime();
                    });
                    break;
            }

            // Update filtered comments
            filteredComments = filtered;
            commentsPerSlide = initialCommentsPerLoad;
            currentSlide = 0; // Reset to first slide

            // Update display
            displayFilteredComments();
            updatePhotoReviews(filtered);

            // Update rating counts after filtering
            updateRatingCounts();
        } catch (error) {
            console.error('Arama ve sıralama uygulanırken hata:', error);
        }
    }

    /**
     * Show rating tooltip
     */
    window.showRatingTooltip = function (event) {
        try {
            const tooltip = document.getElementById('ratingTooltipClean');
            if (tooltip) {
                tooltip.style.display = 'block';

                // Position tooltip below and centered relative to the hover element
                const rect = event.target.getBoundingClientRect();
                const tooltipRect = tooltip.getBoundingClientRect();

                // Center horizontally
                let left = rect.left + (rect.width / 2) - (tooltipRect.width / 2);

                // Ensure tooltip stays within viewport
                const viewportWidth = window.innerWidth;
                if (left < 10) left = 10;
                if (left + tooltipRect.width > viewportWidth - 10) {
                    left = viewportWidth - tooltipRect.width - 10;
                }

                tooltip.style.left = left + 'px';
                tooltip.style.top = (rect.bottom + 8) + 'px';

                // Trigger progress bar animations
                setTimeout(() => {
                    triggerProgressBarAnimations();
                }, 100); // Small delay for smooth effect
            }
        } catch (error) {
            console.error('Tooltip gösterme hatası:', error);
        }
    };

    /**
     * Trigger progress bar fill animations
     */
    function triggerProgressBarAnimations() {
        try {
            const progressBars = document.querySelectorAll('.rating-progress-fill');
            progressBars.forEach((bar) => {
                // Reset animation
                bar.style.width = '0%';
                bar.style.transition = 'none';
                bar.classList.remove('animating');

                // Force reflow
                bar.offsetHeight;

                // Start all animations simultaneously
                setTimeout(() => {
                    const targetWidth = bar.dataset.targetWidth || '0%';
                    bar.style.transition = 'width .6s ease-out';
                    bar.style.width = targetWidth;

                    // Add shimmer effect
                    if (targetWidth !== '0%') {
                        bar.classList.add('animating');

                        // Remove shimmer after animation
                        setTimeout(() => {
                            bar.classList.remove('animating');
                        }, 600);
                    }
                }, 0); // No delay - all start together
            });
        } catch (error) {
            console.error('Progress bar animasyon hatası:', error);
        }
    }

    /**
     * Hide rating tooltip
     */
    window.hideRatingTooltip = function () {
        try {
            const tooltip = document.getElementById('ratingTooltipClean');
            if (tooltip) {
                tooltip.style.display = 'none';
            }
        } catch (error) {
            console.error('Tooltip gizleme hatası:', error);
        }
    };

    /**
     * Show reviews title tooltip
     */
    window.showReviewsTitleTooltip = function (event) {
        try {
            const tooltip = document.getElementById('reviewsTitleTooltip');
            if (tooltip) {
                // Update tooltip content with current rating data
                updateReviewsTitleTooltipContent();

                tooltip.style.display = 'block';

                // Position tooltip below and centered relative to the hover element
                const rect = event.currentTarget.getBoundingClientRect();
                const tooltipRect = tooltip.getBoundingClientRect();

                // Center horizontally
                let left = rect.left + (rect.width / 2) - (tooltipRect.width / 2);

                // Ensure tooltip stays within viewport
                const viewportWidth = window.innerWidth;
                if (left < 10) left = 10;
                if (left + tooltipRect.width > viewportWidth - 10) {
                    left = viewportWidth - tooltipRect.width - 10;
                }

                tooltip.style.left = left + 'px';
                tooltip.style.top = (rect.bottom + 8) + 'px';

                // Trigger progress bar animations
                setTimeout(() => {
                    triggerProgressBarAnimations();
                }, 100);
            }
        } catch (error) {
            console.error('Reviews title tooltip gösterme hatası:', error);
        }
    };

    /**
     * Hide reviews title tooltip
     */
    window.hideReviewsTitleTooltip = function () {
        try {
            const tooltip = document.getElementById('reviewsTitleTooltip');
            if (tooltip) {
                tooltip.style.display = 'none';
            }
        } catch (error) {
            console.error('Reviews title tooltip gizleme hatası:', error);
        }
    };

    /**
     * Update reviews title tooltip content
     */
    function updateReviewsTitleTooltipContent() {
        try {
            const tooltipAverage = document.getElementById('tooltipTitleAverage');
            const tooltipBreakdown = document.getElementById('tooltipTitleBreakdown');

            if (!tooltipAverage || !tooltipBreakdown || !allComments) return;

            const stats = RatingAnalytics.calculateRatingStats(allComments);

            // Update average score
            tooltipAverage.textContent = stats.average.toFixed(1);

            // Generate breakdown HTML
            const breakdownHTML = [5, 4, 3, 2, 1].map(rating => {
                const count = stats.breakdown[rating] || 0;
                const percentage = stats.total > 0 ? Math.round((count / stats.total) * 100) : 0;
                return `
                    <div class="rating-breakdown-row">
                        <span class="rating-number">${rating}</span>
                        <div class="rating-stars-small">
                            ${StarRating.render(rating, {
                    size: 'small',
                    className: 'tooltip-stars'
                })}
                        </div>
                        <div class="rating-progress-bar">
                            <div class="rating-progress-fill" data-target-width="${percentage}%" style="width: 0%"></div>
                        </div>
                        <span class="rating-count-number">${count}</span>
                    </div>
                `;
            }).join('');

            tooltipBreakdown.innerHTML = breakdownHTML;

        } catch (error) {
            console.error('Reviews title tooltip content güncelleme hatası:', error);
        }
    }



    /**
     * Star Rating Component - Modern ve kullanışlı yıldız rating sistemi
     */
    const StarRating = {
        // Konfigürasyon sabitleri
        CONFIG: {
            MAX_STARS: 5,
            STAR_SYMBOL: '★',
            PRECISION: 0.5, // Yarım yıldız hassasiyeti
            THEME: {
                FILLED_COLOR: '#ffc107',
                HALF_COLOR: '#ffc107',
                EMPTY_COLOR: '#e0e0e0',
                SHADOW: '0 1px 2px rgba(255, 193, 7, 0.3)'
            }
        },

        /**
         * Rating değerini normalize et ve doğrula
         * @param {number} rating - Ham rating değeri
         * @returns {number} Normalize edilmiş rating (0-5 arası)
         */
        normalizeRating(rating) {
            const numRating = parseFloat(rating) || 0;
            return Math.max(0, Math.min(this.CONFIG.MAX_STARS, numRating));
        },

        /**
         * Rating'i yıldız bileşenlerine ayır
         * @param {number} rating - Normalize edilmiş rating değeri
         * @returns {Object} Yıldız bileşenleri
         */
        calculateStarComponents(rating) {
            const normalizedRating = this.normalizeRating(rating);
            const fullStars = Math.floor(normalizedRating);
            const remainder = normalizedRating - fullStars;
            const hasHalfStar = remainder >= this.CONFIG.PRECISION;
            const emptyStars = this.CONFIG.MAX_STARS - fullStars - (hasHalfStar ? 1 : 0);

            return {
                full: Math.max(0, fullStars),
                half: hasHalfStar ? 1 : 0,
                empty: Math.max(0, emptyStars),
                percentage: Math.round((normalizedRating / this.CONFIG.MAX_STARS) * 100)
            };
        },

        /**
         * Tek bir yıldız elementi oluştur
         * @param {string} type - Yıldız tipi ('filled', 'half', 'empty')
         * @param {Object} options - Styling seçenekleri
         * @returns {string} HTML string
         */
        createStar(type, options = {}) {
            const baseClass = options.size === 'small' ? 'star-small' : 'star';
            const typeClass = `star-${type}`;
            const customClass = options.className || '';

            return `<span class="${baseClass} ${typeClass} ${customClass}"
                          data-star-type="${type}"
                          role="img"
                          aria-hidden="true">${this.CONFIG.STAR_SYMBOL}</span>`;
        },

        /**
         * Yıldız konteynerini oluştur
         * @param {number} rating - Rating değeri
         * @param {Object} options - Render seçenekleri
         * @returns {string} Tam HTML yapısı
         */
        render(rating, options = {}) {
            const defaults = {
                size: 'normal', // 'small', 'normal', 'large'
                showValue: false,
                showCount: false,
                interactive: false,
                className: '',
                ariaLabel: null
            };

            const config = { ...defaults, ...options };
            const components = this.calculateStarComponents(rating);
            const normalizedRating = this.normalizeRating(rating);

            let starsHTML = '';

            // Dolu yıldızlar
            for (let i = 0; i < components.full; i++) {
                starsHTML += this.createStar('filled', config);
            }

            // Yarım yıldız
            if (components.half > 0) {
                starsHTML += this.createStar('half', config);
            }

            // Boş yıldızlar
            for (let i = 0; i < components.empty; i++) {
                starsHTML += this.createStar('empty', config);
            }

            // Konteyner elementini oluştur
            const ariaLabel = config.ariaLabel || `${normalizedRating} out of ${this.CONFIG.MAX_STARS} stars`;
            const containerClass = `stars-container ${config.className}`.trim();

            let containerHTML = `<div class="${containerClass}"
                                     role="img"
                                     aria-label="${ariaLabel}"
                                     data-rating="${normalizedRating}"
                                     data-percentage="${components.percentage}">
                                    <div class="stars-display">${starsHTML}</div>`;

            // Rating değerini göster
            if (config.showValue) {
                containerHTML += `<span class="rating-value">${normalizedRating.toFixed(1)}</span>`;
            }

            // Review sayısını göster
            if (config.showCount && typeof config.count === 'number') {
                containerHTML += `<span class="rating-count">(${config.count})</span>`;
            }

            containerHTML += `</div>`;

            return containerHTML;
        },

        /**
         * Mevcut bir rating display'ini güncelle
         * @param {HTMLElement|string} element - Hedef element veya selector
         * @param {number} newRating - Yeni rating değeri
         * @param {Object} options - Render seçenekleri
         */
        updateDisplay(element, newRating, options = {}) {
            const target = typeof element === 'string' ? document.querySelector(element) : element;
            if (!target) {
                console.warn('StarRating.updateDisplay: Element bulunamadı:', element);
                return;
            }

            const newHTML = this.render(newRating, options);
            target.innerHTML = newHTML;
            target.setAttribute('data-rating', this.normalizeRating(newRating));
        },

        /**
         * Bir element'ten mevcut rating'i al
         * @param {HTMLElement|string} element - Hedef element veya selector
         * @returns {number|null} Mevcut rating değeri
         */
        getRatingFromElement(element) {
            const target = typeof element === 'string' ? document.querySelector(element) : element;
            if (!target) return null;

            const ratingAttr = target.getAttribute('data-rating');
            return ratingAttr ? parseFloat(ratingAttr) : null;
        },

    };

    /**
     * Belirli slide'a git
     */
    window.goToSlide = function (slideIndex, event) {
        try {
            if (event) {
                event.preventDefault();
                event.stopPropagation();
            }
            const maxSlides = Math.ceil(filteredComments.length / commentsPerSlide);
            if (slideIndex >= 0 && slideIndex < maxSlides) {
                currentSlide = slideIndex;
                updateSliderDisplay();
            }
        } catch (error) {
            console.error('Slide değiştirirken hata:', error);
        }
    };

    /**
     * Yorumlar bölümüne kaydır
     */
    window.scrollToComments = function () {
        const modernContainer = document.getElementById('modernReviewsContainer');

        if (modernContainer) {
            modernContainer.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    };



    /**
     * Modern ve basit fotoğraf yorumu modali - gelişmiş navigation ile
     */
    window.openPhotoModal = function (photoUrl, userName = '', rating = 0, commentText = '') {
        // Akıllı navigation sistemi: Önce aktif yorumun fotoğrafları, sonra diğer yorumlar
        let currentCommentIndex = -1;
        let currentPhotoIndex = 0;
        let currentComment = null;

        // Mevcut fotoğrafın hangi yoruma ait olduğunu bul
        if (window.allComments && window.allComments.length > 0) {
            window.allComments.forEach((comment, commentIndex) => {
                if (comment.photos && comment.photos.length > 0) {
                    comment.photos.forEach((photo, photoIndex) => {
                        if (photo === photoUrl) {
                            currentCommentIndex = commentIndex;
                            currentPhotoIndex = photoIndex;
                            currentComment = comment;
                        }
                    });
                }
            });
        }

        // Eğer yorum bulunamadıysa, manuel comment oluştur
        if (!currentComment) {
            currentComment = {
                user: userName || 'Anonim',
                rating: rating || 0,
                comment: commentText || '',
                date: new Date().toLocaleDateString('tr-TR'),
                elit_customer: false,
                like_count: 0,
                photos: [photoUrl]
            };
            currentCommentIndex = -1; // Manual comment indicator
            currentPhotoIndex = 0;
        }

        const modal = document.createElement('div');
        modal.className = 'photo-comment-modal';
        modal.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0,0,0,0.6);
            backdrop-filter: blur(10px);
            z-index: 100000;
            display: flex;
            align-items: center;
            justify-content: center;
            animation: fadeIn 0.4s ease;
            padding: 10px;
            box-sizing: border-box;
        `;

        // Media query check for mobile - önce tanımla
        const isMobile = window.innerWidth <= 768;

        const modalContent = document.createElement('div');
        modalContent.style.cssText = `
            background: white;
            border-radius: 16px;
            width: ${isMobile ? '100%' : '900px'};
            max-width: ${isMobile ? '95vw' : '900px'};
            max-height: 90vh;
            overflow: hidden;
            box-shadow: 0 25px 60px rgba(0,0,0,0.2);
            display: flex;
            position: relative;
            animation: modalSlideIn 0.4s ease-out;
            flex-direction: row;
        `;

        if (isMobile) {
            modalContent.style.flexDirection = 'column';
            modalContent.style.maxHeight = '95vh';
            modalContent.style.maxWidth = '95vw';
            modalContent.style.width = '100%';
            modal.style.padding = '20px 10px';
        }

        // Sol kısım - Fotoğraf alanı (3:4 aspect ratio)
        const photoSection = document.createElement('div');
        const aspectRatioHeight = isMobile ? '250px' : Math.floor(470 * 4 / 3) + 'px'; // 3:4 ratio için 470 * 4/3 = 627px

        photoSection.style.cssText = `
            flex: ${isMobile ? '0 0 auto' : '0 0 470px'};
            width: ${isMobile ? '100%' : '470px'};
            height: ${isMobile ? '250px' : aspectRatioHeight};
            background: #f8f9fa;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
            border-radius: ${isMobile ? '16px 16px 0 0' : '16px 0 0 16px'};
            overflow: hidden;
        `;

        // Fotoğraf - başlangıçta mevcut fotoğrafı göster
        const photo = document.createElement('img');
        photo.src = photoUrl;
        photo.onerror = function () {
            this.onerror = null;
            this.src = 'data:image/svg+xml;charset=UTF-8,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20width%3D%2224%22%20height%3D%2224%22%20viewBox%3D%220%200%2024%2024%22%20fill%3D%22none%22%20stroke%3D%22%23999%22%20stroke-width%3D%222%22%20stroke-linecap%3D%22round%22%20stroke-linejoin%3D%22round%22%3E%3Crect%20x%3D%223%22%20y%3D%223%22%20width%3D%2218%22%20height%3D%2218%22%20rx%3D%222%22%20ry%3D%222%22%3E%3C%2Frect%3E%3Ccircle%20cx%3D%228.5%22%20cy%3D%228.5%22%20r%3D%221.5%22%3E%3C%2Fcircle%3E%3Cpolyline%20points%3D%2221%2015%2016%2010%205%2021%22%3E%3C%2Fpolyline%3E%3C%2Fsvg%3E';
            this.classList.add('error-image');
            this.style.padding = '20px';
            this.style.background = '#f0f0f0';
        };
        photo.style.cssText = `
            max-width: 100%;
            max-height: 100%;
            object-fit: cover;
            border-radius: 12px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.15);
        `;

        // Akıllı navigation fonksiyonları
        const getNextPhoto = () => {
            const commentsArray = window.allComments || allComments || [];

            // 1. Önce aktif yorumun diğer fotoğraflarını kontrol et
            if (currentComment.photos && currentPhotoIndex < currentComment.photos.length - 1) {
                return {
                    commentIndex: currentCommentIndex,
                    photoIndex: currentPhotoIndex + 1,
                    comment: currentComment
                };
            }

            // 2. Aktif yorumun fotoğrafları bittiyse, sonraki fotoğraflı yoruma geç
            if (commentsArray.length > 0 && currentCommentIndex >= 0) {
                for (let i = currentCommentIndex + 1; i < commentsArray.length; i++) {
                    const nextComment = commentsArray[i];
                    if (nextComment.photos && nextComment.photos.length > 0) {
                        return {
                            commentIndex: i,
                            photoIndex: 0,
                            comment: nextComment
                        };
                    }
                }

                // 3. Son yorum da bittiyse, ilk fotoğraflı yoruma dön (döngü)
                for (let i = 0; i <= currentCommentIndex; i++) {
                    const firstComment = commentsArray[i];
                    if (firstComment.photos && firstComment.photos.length > 0) {
                        return {
                            commentIndex: i,
                            photoIndex: 0,
                            comment: firstComment
                        };
                    }
                }
            }

            return null; // Başka fotoğraf yok
        };

        const getPrevPhoto = () => {
            const commentsArray = window.allComments || allComments || [];

            // 1. Aktif yorumun önceki fotoğrafı var mı?
            if (currentPhotoIndex > 0) {
                return {
                    commentIndex: currentCommentIndex,
                    photoIndex: currentPhotoIndex - 1,
                    comment: currentComment
                };
            }

            // 2. Aktif yorumun ilk fotoğrafındaysa, önceki fotoğraflı yoruma geç
            if (commentsArray.length > 0 && currentCommentIndex >= 0) {
                for (let i = currentCommentIndex - 1; i >= 0; i--) {
                    const prevComment = commentsArray[i];
                    if (prevComment.photos && prevComment.photos.length > 0) {
                        return {
                            commentIndex: i,
                            photoIndex: prevComment.photos.length - 1, // Son fotoğrafından başla
                            comment: prevComment
                        };
                    }
                }

                // 3. İlk yorum da geçildiyse, son fotoğraflı yorumun sonuna git (döngü)
                for (let i = commentsArray.length - 1; i > currentCommentIndex; i--) {
                    const lastComment = commentsArray[i];
                    if (lastComment.photos && lastComment.photos.length > 0) {
                        return {
                            commentIndex: i,
                            photoIndex: lastComment.photos.length - 1,
                            comment: lastComment
                        };
                    }
                }
            }

            return null; // Başka fotoğraf yok
        };

        // Fotoğraf ve yorum güncelleme fonksiyonu
        const updatePhotoAndComment = (photoData) => {
            if (!photoData) return;

            currentCommentIndex = photoData.commentIndex;
            currentPhotoIndex = photoData.photoIndex;
            currentComment = photoData.comment;

            // Fotoğrafı güncelle
            photo.onerror = null;
            photo.classList.remove('error-image');
            photo.style.padding = '';
            photo.style.background = '';
            photo.src = currentComment.photos[currentPhotoIndex];

            photo.onerror = function () {
                this.onerror = null;
                this.src = 'data:image/svg+xml;charset=UTF-8,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20width%3D%2224%22%20height%3D%2224%22%20viewBox%3D%220%200%2024%2024%22%20fill%3D%22none%22%20stroke%3D%22%23999%22%20stroke-width%3D%222%22%20stroke-linecap%3D%22round%22%20stroke-linejoin%3D%22round%22%3E%3Crect%20x%3D%223%22%20y%3D%223%22%20width%3D%2218%22%20height%3D%2218%22%20rx%3D%222%22%20ry%3D%222%22%3E%3C%2Frect%3E%3Ccircle%20cx%3D%228.5%22%20cy%3D%228.5%22%20r%3D%221.5%22%3E%3C%2Fcircle%3E%3Cpolyline%20points%3D%2221%2015%2016%2010%205%2021%22%3E%3C%2Fpolyline%3E%3C%2Fsvg%3E';
                this.classList.add('error-image');
                this.style.padding = '20px';
                this.style.background = '#f0f0f0';
            };

            // Yorum bilgilerini güncelle
            const updatedComment = {
                user: currentComment.user || 'Anonim',
                rating: currentComment.rating || 0,
                date: currentComment.date || new Date().toLocaleDateString('tr-TR'),
                comment: currentComment.comment || '',
                elit_customer: currentComment.elit_customer || false,
                photos: [], // Modal'da fotoğraf tekrarı önlemek için
                like_count: currentComment.like_count !== undefined ? currentComment.like_count : 0
            };

            commentSection.innerHTML = generateTrendyolCommentHTML(updatedComment);

            // Sayacı güncelle
            updatePhotoCounter();
        };

        // Navigation sistemi - basitleştirilmiş kontrol
        const getTotalPhotoCount = () => {
            let totalPhotos = 0;

            // Önce window.allComments'ten tüm fotoğrafları say
            let commentsSource = window.allComments || allComments || [];

            if (commentsSource && commentsSource.length > 0) {
                commentsSource.forEach((comment) => {
                    if (comment.photos && comment.photos.length > 0) {
                        totalPhotos += comment.photos.length;
                    }
                });
            }

            // Eğer hiç fotoğraf bulunamadıysa, mevcut comment'i kontrol et
            if (totalPhotos === 0) {
                if (currentComment.photos && currentComment.photos.length > 0) {
                    totalPhotos = currentComment.photos.length;
                } else {
                    totalPhotos = 1; // En az bir fotoğraf var (açılan modal)
                }
            }

            return totalPhotos;
        };

        const totalPhotoCount = getTotalPhotoCount();

        // Navigation butonlarını göster - birden fazla fotoğraf varsa
        if (totalPhotoCount > 1) {
            const prevBtn = createNavButton('‹', 'left: 20px;', isMobile);
            const nextBtn = createNavButton('›', 'right: 20px;', isMobile);

            prevBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                const prevPhotoData = getPrevPhoto();
                if (prevPhotoData) {
                    updatePhotoAndComment(prevPhotoData);
                }
            });

            nextBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                const nextPhotoData = getNextPhoto();
                if (nextPhotoData) {
                    updatePhotoAndComment(nextPhotoData);
                }
            });

            prevBtn.style.opacity = '1';
            nextBtn.style.opacity = '1';

            photoSection.appendChild(prevBtn);
            photoSection.appendChild(nextBtn);

            // Fotoğraf sayacı ekle
            const photoCounter = document.createElement('div');
            photoCounter.className = 'photo-counter';
            photoCounter.style.cssText = `
                position: absolute;
                bottom: 15px;
                right: 15px;
                background: rgba(0,0,0,0.7);
                color: white;
                padding: 6px 12px;
                border-radius: 20px;
                font-size: ${isMobile ? '12px' : '14px'};
                font-weight: 500;
                backdrop-filter: blur(10px);
                z-index: 6;
            `;

            // Sayaç güncelleme fonksiyonu
            window.updatePhotoCounter = function () {
                // Sadece aktif yorumdaki fotoğraf pozisyonu ve sayısını göster
                const currentPhotoNumber = currentPhotoIndex + 1;
                const totalPhotosInComment = currentComment.photos ? currentComment.photos.length : 1;

                photoCounter.textContent = `${currentPhotoNumber} / ${totalPhotosInComment}`;
            };

            updatePhotoCounter();
            photoSection.appendChild(photoCounter);
        }

        photoSection.appendChild(photo);

        // Sağ kısım - Yorum bilgileri (430px desktop, responsive)
        const commentSection = document.createElement('div');
        commentSection.style.cssText = `
            flex: ${isMobile ? '1' : '0 0 430px'};
            width: ${isMobile ? '100%' : '430px'};
            padding: ${isMobile ? '15px' : '20px'};
            display: flex;
            flex-direction: column;
            overflow-y: auto;
            background: white;
            border-radius: ${isMobile ? '0 0 16px 16px' : '0 16px 16px 0'};
            min-height: ${isMobile ? '200px' : 'auto'};
        `;

        // Başlangıç yorum verisi - mevcut fotoğrafın yorumu
        const initialComment = {
            user: currentComment.user || 'Anonim',
            rating: currentComment.rating || 0,
            date: currentComment.date || new Date().toLocaleDateString('tr-TR'),
            comment: currentComment.comment || '',
            elit_customer: currentComment.elit_customer || false,
            photos: [], // Fotoğraf modalında foto tekrar göstermeyelim
            like_count: currentComment.like_count !== undefined ? currentComment.like_count : 0
        };

        commentSection.innerHTML = generateTrendyolCommentHTML(initialComment);

        // Kapatma butonu (responsive)
        const closeButton = document.createElement('button');
        closeButton.innerHTML = '×';
        closeButton.style.cssText = `
            position: absolute;
            top: ${isMobile ? '10px' : '20px'};
            right: ${isMobile ? '10px' : '20px'};
            background: white;
            color: #718096;
            border: 1px solid #e2e8f0;
            width: ${isMobile ? '36px' : '40px'};
            height: ${isMobile ? '36px' : '40px'};
            border-radius: 50%;
            font-size: ${isMobile ? '16px' : '18px'};
            cursor: pointer;
            z-index: 10;
            box-shadow: 0 4px 15px rgba(0,0,0,0.1);
            transition: all 0.3s ease;
            display: flex;
            align-items: center;
            justify-content: center;
            touch-action: manipulation;
        `;

        closeButton.addEventListener('mouseenter', () => {
            closeButton.style.background = '#f7fafc';
            closeButton.style.color = '#2d3748';
            closeButton.style.borderColor = '#cbd5e0';
            closeButton.style.transform = 'scale(1.05)';
        });

        closeButton.addEventListener('mouseleave', () => {
            closeButton.style.background = 'white';
            closeButton.style.color = '#718096';
            closeButton.style.borderColor = '#e2e8f0';
            closeButton.style.transform = 'scale(1)';
        });

        closeButton.addEventListener('click', () => {
            modal.style.animation = 'fadeOut 0.3s ease';
            setTimeout(() => modal.remove(), 300);
        });

        // Ana modal montajı

        // Ana modal montajı
        modalContent.appendChild(photoSection);
        modalContent.appendChild(commentSection);
        modalContent.appendChild(closeButton);
        modal.appendChild(modalContent);

        // Modal event handlers
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                modal.style.animation = 'fadeOut 0.3s ease';
                setTimeout(() => modal.remove(), 300);
            }
        });

        // Keyboard navigation - gelişmiş fotoğraf navigation
        const handleKeyboard = (e) => {
            if (e.key === 'Escape') {
                modal.style.animation = 'fadeOut 0.3s ease';
                setTimeout(() => modal.remove(), 300);
                document.removeEventListener('keydown', handleKeyboard);
            } else if (e.key === 'ArrowLeft') {
                const prevPhotoData = getPrevPhoto();
                if (prevPhotoData) {
                    updatePhotoAndComment(prevPhotoData);
                }
            } else if (e.key === 'ArrowRight') {
                const nextPhotoData = getNextPhoto();
                if (nextPhotoData) {
                    updatePhotoAndComment(nextPhotoData);
                }
            }
        };

        document.addEventListener('keydown', handleKeyboard);

        // Responsive resize handler
        const handleResize = () => {
            const newIsMobile = window.innerWidth <= 768;
            if (newIsMobile !== isMobile) {
                // Modal boyutlarını güncelle
                if (newIsMobile) {
                    modalContent.style.flexDirection = 'column';
                    modalContent.style.maxHeight = '95vh';
                    modalContent.style.maxWidth = '95vw';
                    modalContent.style.width = '100%';
                    modal.style.padding = '20px 10px';
                    photoSection.style.flex = '0 0 auto';
                    photoSection.style.width = '100%';
                    photoSection.style.height = '250px';
                    photoSection.style.borderRadius = '16px 16px 0 0';
                    commentSection.style.flex = '1';
                    commentSection.style.width = '100%';
                    commentSection.style.borderRadius = '0 0 16px 16px';
                    commentSection.style.padding = '15px';
                    closeButton.style.top = '10px';
                    closeButton.style.right = '10px';
                    closeButton.style.width = '36px';
                    closeButton.style.height = '36px';
                    closeButton.style.fontSize = '16px';
                } else {
                    const desktopHeight = Math.floor(470 * 4 / 3) + 'px'; // 3:4 aspect ratio
                    modalContent.style.flexDirection = 'row';
                    modalContent.style.maxHeight = '90vh';
                    modalContent.style.maxWidth = '900px';
                    modalContent.style.width = '900px';
                    modal.style.padding = '10px';
                    photoSection.style.flex = '0 0 470px';
                    photoSection.style.width = '470px';
                    photoSection.style.height = desktopHeight;
                    photoSection.style.borderRadius = '16px 0 0 16px';
                    commentSection.style.flex = '0 0 430px';
                    commentSection.style.width = '430px';
                    commentSection.style.borderRadius = '0 16px 16px 0';
                    commentSection.style.padding = '20px';
                    closeButton.style.top = '20px';
                    closeButton.style.right = '20px';
                    closeButton.style.width = '40px';
                    closeButton.style.height = '40px';
                    closeButton.style.fontSize = '18px';
                }
            }
        };

        window.addEventListener('resize', handleResize);

        // Cleanup resize listener when modal closes
        const originalRemove = modal.remove;
        modal.remove = function () {
            window.removeEventListener('resize', handleResize);
            document.removeEventListener('keydown', handleKeyboard);
            originalRemove.call(this);
        };

        document.body.appendChild(modal);
    };

    // Yardımcı fonksiyonlar
    function createNavButton(arrow, position, isMobile = false) {
        const btn = document.createElement('button');
        btn.textContent = arrow;
        const buttonSize = isMobile ? '35px' : '45px';
        const fontSize = isMobile ? '16px' : '20px';
        const positioning = isMobile ? position.replace('20px', '10px') : position;

        btn.style.cssText = `
            position: absolute;
            top: 50%;
            transform: translateY(-50%);
            ${positioning}
            width: ${buttonSize};
            height: ${buttonSize};
            background: rgba(255,255,255,0.9);
            border: 1px solid rgba(226, 232, 240, 0.8);
            border-radius: 50%;
            font-size: ${fontSize};
            color: #4a5568;
            cursor: pointer;
            transition: all 0.3s ease;
            box-shadow: 0 4px 15px rgba(0,0,0,0.15);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 15;
            touch-action: manipulation;
            pointer-events: auto;
        `;

        btn.addEventListener('mouseenter', () => {
            btn.style.background = 'white';
            btn.style.borderColor = '#cbd5e0';
            btn.style.transform = 'translateY(-50%) scale(1.1)';
            btn.style.boxShadow = '0 6px 20px rgba(0,0,0,0.2)';
        });

        btn.addEventListener('mouseleave', () => {
            btn.style.background = 'rgba(255,255,255,0.9)';
            btn.style.borderColor = 'rgba(226, 232, 240, 0.8)';
            btn.style.transform = 'translateY(-50%) scale(1)';
            btn.style.boxShadow = '0 4px 15px rgba(0,0,0,0.15)';
        });

        return btn;
    }

    /**
     * CSS Animasyonları ve Responsive Modal Styles
     */
    const cssAnimations = `
        @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
        }

        @keyframes fadeOut {
            from { opacity: 1; }
            to { opacity: 0; }
        }

        @keyframes modalSlideIn {
            from {
                opacity: 0;
                transform: translateY(50px) scale(0.95);
            }
            to {
                opacity: 1;
                transform: translateY(0) scale(1);
            }
        }

        @keyframes modalSlideOut {
            from {
                opacity: 1;
                transform: translateY(0) scale(1);
            }
            to {
                opacity: 0;
                transform: translateY(50px) scale(0.95);
            }
        }

        /* Modal Responsive Styles */
        @media (max-width: 768px) {
            .photo-comment-modal {
                padding: 20px 10px !important;
            }

            .photo-comment-modal > div {
                flex-direction: column !important;
                max-height: 95vh !important;
                max-width: 95vw !important;
            }

            .trendyol-comment-card {
                font-size: 14px;
            }

            .trendyol-comment-text {
                font-size: 14px;
                line-height: 1.5;
            }

            .trendyol-comment-header {
                flex-direction: column;
                align-items: flex-start !important;
                gap: 8px;
            }

            .trendyol-comment-meta {
                font-size: 12px;
            }

            .trendyol-comment-footer {
                font-size: 12px;
                display: flex;
                justify-content: space-between;
                align-items: center;
                flex-direction: row; /* Aynı satırda tut */
            }
        }

        /* Touch improvements for mobile */
        @media (hover: none) and (pointer: coarse) {
            .photo-comment-modal button {
                min-height: 44px;
                min-width: 44px;
            }
        }
    `;

    // CSS'i sayfaya ekle
    if (!document.getElementById('product-modal-animations')) {
        const styleSheet = document.createElement('style');
        styleSheet.id = 'product-modal-animations';
        styleSheet.textContent = cssAnimations;
        document.head.appendChild(styleSheet);
    }

    /**
     * Fetch hatalarını işle - Hata durumunda hiçbir değişiklik yapma
     */
    function handleFetchError(error) {
        try {
            console.error('Yorum getirme hatası:', error);

            // Modern container varsa onu gizle
            const modernContainer = document.getElementById('modernReviewsContainer');
            if (modernContainer) {
                modernContainer.style.display = 'none';
            }

            // Rating display'i de gizle
            const ratingDisplay = document.querySelector('.custom-rating-display');
            if (ratingDisplay) {
                ratingDisplay.style.display = 'none';
            }

            // Hiçbir görsel değişiklik yapma - sessizce başarısız ol
        } catch (error) {
            console.error('Hata yönetiminde hata:', error);
        }
    }

    /**
     * Yorum yok mesajını göster
     */
    function showNoReviewsMessage() {
        try {
            const modernContainer = document.getElementById('modernReviewsContainer');

            if (modernContainer) {
                const reviewsContent = document.getElementById('reviewsContent');
                if (reviewsContent) {
                    reviewsContent.innerHTML = `
                        <div class="no-reviews-modern">
                            <div class="no-reviews-icon">💬</div>
                            <h3>Henüz yorum bulunmuyor</h3>
                            <p>Bu ürün için ilk yorumu siz yazabilirsiniz!</p>
                        </div>
                    `;
                }

                // Filtreleri gizle
                const reviewsFilters = document.getElementById('reviewsFilters');
                if (reviewsFilters) {
                    reviewsFilters.style.display = 'none';
                }
            }
        } catch (error) {
            console.error('Yorum yok mesajı gösterilirken hata:', error);
        }
    }



    /**
     * Modern CSS stillerini ekle
     */
    function addModernStyles() {
        if (document.getElementById('trendyol-modern-styles')) return;

        const style = document.createElement('style');
        style.id = 'trendyol-modern-styles';
        style.textContent = `
            /* Clean Rating Display Styles */
            .error-image {
                padding: 10px;
                background-color: #f5f5f5;
                object-fit: contain !important;
                opacity: 0.6;
            }

            .custom-rating-display {
                margin: 15px 0;
                font-family: "Source Sans 3", sans-serif;
            }

            .rating-summary {
                clear: both;
            }

            .rating-display-clean {
                display: inline-flex;
                align-items: center;
                padding: 12px 0px;
                transition: all 0.3s ease;
                color: #333;
                font-weight: 500;
                font-size: 12px;
                position: relative;
            }

            .rating-hover-area {
                display: inline-flex;
                align-items: flex-end;
                cursor: pointer;
            }

            .rating-score-clean {
                font-weight: 700;
                font-size: 16px;
                color: #333;
                margin-right: 4px;
                transition: all 0.2s ease;
            }

            .rating-stars-clean-display {
                transition: all 0.2s ease;
            }


            .dot-splitter {
                margin: 0 4px;
                font-size: 12px;
                color: #ccc;
            }

            .rating-stars-clean {
                display: flex;
                gap: 1px;
            }

            .rating-count-clean {
                color: #666;
                font-size: 12px;
                cursor: pointer;
                transition: all 0.2s ease;
                text-decoration: none;
                border-radius: 4px;
                padding: 2px 4px;
            }

            .rating-count-clean:hover {
                color: #ff6000;
                // background-color: rgba(255, 96, 0, 0.1);
                text-decoration: underline;
            }

            .rating-count-clean:hover .rating-count-number {
                color: #ff6000;
            }

            .rating-count-number {
                font-weight: 700;
                color: #333;
            }

            .rating-tooltip-clean {
                position: fixed;
                background: white;
                border: 1px solid #e0e0e0;
                border-radius: 12px;
                padding: 8px 12px;
                box-shadow: 0 8px 24px rgba(0,0,0,0.12);
                z-index: 1000;
                min-width: 280px;
                font-size: 13px;
                font-family: "Source Sans 3", -apple-system, BlinkMacSystemFont, sans-serif;
                opacity: 0;
                transform: translateY(-8px);
                transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
                pointer-events: none;
            }

            .rating-tooltip-clean[style*="display: block"] {
                opacity: 1;
                transform: translateY(0);
            }

            .tooltip-header {
                align-items: flex-end;
                color: #666;
                display: flex;
                font-size: 12px;
                font-weight: 400;
                gap: 8px;
                line-height: 0.5;
                margin-block: 16px 20px;
            }

            .tooltip-average {
                font-size: 28px;
                color: #333;
            }

            .rating-breakdown-container {
                display: flex;
                flex-direction: column;
                gap: 8px;
            }

            .rating-breakdown-container .rating-count-number {
                text-align: left;
                font-weight: normal;
                font-size: 12px;
            }

            .rating-breakdown-row {
                display: flex;
                align-items: center;
                gap: 8px;
                padding: 4px 0;
            }

            .rating-number {
                font-size: 12px;
                font-weight: 600;
                color: #333;
                min-width: 12px;
                text-align: right;
            }

            .rating-stars-small {
                display: flex;
                align-items: center;
                min-width: 80px;
            }

            .rating-progress-bar {
                width: 180px;
                height: 4px;
                background-color: #f0f0f0;
                border-radius: 4px;
                overflow: hidden;
                position: relative;
            }

            .rating-progress-fill {
                height: 100%;
                background: linear-gradient(90deg, #ffc107 0%, #ff9800 100%);
                border-radius: 4px;
                transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
                position: relative;
                transform-origin: left;
            }

            .rating-progress-fill::after {
                content: '';
                position: absolute;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: linear-gradient(90deg, transparent 0%, rgba(255, 255, 255, 0.3) 50%, transparent 100%);
                transform: translateX(-100%);
                animation: none;
            }

            .rating-progress-fill.animating::after {
                animation: shimmer 0.5s ease-out;
            }

            @keyframes shimmer {
                0% {
                    transform: translateX(-100%);
                }
                100% {
                    transform: translateX(100%);
                }
            }

            .rating-count-number {
                font-size: 13px;
                font-weight: 600;
                color: #333;
                min-width: 30px;
                text-align: right;
            }
            }

            /* Trendyol Slider Styles */
            .reviews-content-slider {
                margin: 20px 0;
                min-height: 200px;
            }

            .trendyol-comments-slider {
                display: flex;
                flex-direction: column;
                gap: 0;
            }

            @media (max-width: 1200px) {
                .trendyol-comments-slider {
                    flex-direction: column;
                }
            }

            @media (max-width: 768px) {
                .trendyol-comments-slider {
                    flex-direction: column;
                }
            }

            .trendyol-comment-card {
                background: white;
                border: none;
                border-bottom: 1px solid #f0f0f0;
                border-radius: 0;
                padding: 16px 12px;
                transition: all 0.3s ease;
                box-shadow: none;
            }

            .trendyol-comment-card:hover {
                background-color: #fafafa;
            }

            .trendyol-comment-header {
                display: flex;
                flex-direction: column;
                gap: 8px;
                margin-bottom: 12px;
            }

            .trendyol-comment-rating {
                display: flex;
                gap: 2px;
            }

            .trendyol-comment-meta {
                display: flex;
                align-items: center;
                gap: 6px;
                font-size: 12px;
                color: #999;
            }

            .trendyol-user-name {
                font-weight: 400;
                color: #999;
                font-size: 12px;
            }

            .comment-meta-separator {
                color: #ccc;
                font-size: 10px;
            }

            .trendyol-comment-date {
                font-size: 12px;
                color: #999;
            }

            .trendyol-comment-body {
                margin-bottom: 12px;
            }

            .trendyol-comment-text {
                font-size: 14px;
                line-height: 1.5;
                color: #333;
                margin: 0 0 12px 0;
                display: -webkit-box;
                -webkit-line-clamp: 3;
                -webkit-box-orient: vertical;
                overflow: hidden;
            }

            .trendyol-comment-photos {
                display: flex;
                gap: 8px;
                margin-top: 8px;
            }

            .trendyol-comment-photo {
                width: 64px;
                height: 64px;
                object-fit: cover;
                border-radius: 4px;
                cursor: pointer;
                border: 1px solid transparent;
                background-color: #f5f5f5;
            }

            .more-photos-trendyol {
                width: 50px;
                height: 50px;
                background: #f5f5f5;
                border-radius: 8px;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 12px;
                color: #666;
                font-weight: 600;
            }

            .trendyol-comment-body {
                margin-bottom: 12px;
            }

            .trendyol-comment-text {
                font-size: 14px;
                line-height: 1.5;
                color: #333;
                margin: 0 0 8px 0;
            }

            .trendyol-comment-footer {
                display: flex;
                justify-content: space-between;
                align-items: center;
                font-size: 12px;
                color: #999;
            }

            .trendyol-comment-purchase-info {
                font-size: 12px;
                color: #999;
            }

            .trendyol-comment-likes {
                display: flex;
                align-items: center;
                font-size: 12px;
                color: #666;
            }

            .like-icon {
                margin-right: 2px;
            }

            .like-count {
                margin-right: 4px;
            }

            /* Show More Button */
            .show-more-container {
                display: flex;
                justify-content: center;
                align-items: center;
                margin: 24px 0;
                position: relative;
            }

            .expand-button-line {
                background-color: #e6e6e6;
                border: none;
                height: 1px;
                left: 0;
                margin: 0;
                position: absolute;
                top: 50%;
                width: 100%;
                z-index: 0;
            }

            .show-more-btn {
                all: unset;
                background-color: #f5f5f5;
                border-radius: 100px;
                cursor: pointer;
                display: flex;
                justify-content: center;
                max-width: 328px;
                position: relative;
                width: 100%;
                z-index: 1;
                padding: 8px;
                display: flex;
                color: #333;
                gap: 8px;
            }

            .show-more-arrow {
                font-size: 16px;
                display: inline-block;
                transform: rotate(90deg);
            }

            /* Star Rating Component Styles */
            .stars-container {
                display: inline-flex;
                align-items: center;
                gap: 4px;
                line-height: 1;
            }

            .stars-display {
                display: inline-flex;
                align-items: center;
                gap: 1px;
            }

            .star, .star-small {
                display: inline-block;
                transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
                user-select: none;
                position: relative;
            }

            .star {
                font-size: 20px;
                color: #e0e0e0;
            }

            .star-small {
                font-size: 16px;
                color: #e0e0e0;
            }

            /* Star States */
            .star-filled {
                color: #ffc107 !important;
                text-shadow: 0 1px 3px rgba(255, 193, 7, 0.4);
            }

            .star-half {
                color: #ffc107 !important;
                opacity: 0.75;
                text-shadow: 0 1px 3px rgba(255, 193, 7, 0.3);
                position: relative;
            }

            .star-half::after {
                content: attr(data-star-symbol);
                position: absolute;
                left: 0;
                top: 0;
                width: 50%;
                overflow: hidden;
                color: #ffc107;
                opacity: 1;
            }

            .star-empty {
                color: #e0e0e0 !important;
            }

            /* Rating Value Display */
            .rating-value {
                margin-left: 6px;
                font-size: 14px;
                font-weight: 600;
                color: #333;
            }

            .rating-count {
                font-size: 13px;
                color: #666;
                font-weight: 400;
            }

            /* Legacy Support */
            .legacy-stars {
                gap: 2px;
            }

            /* Hover Effects for Interactive Stars (future feature) */
            .stars-container[data-interactive="true"] .star {
                cursor: pointer;
            }

            .stars-container[data-interactive="true"] .star:hover {
                transform: scale(1.1);
                filter: brightness(1.2);
            }

            /* Accessibility Improvements */
            .stars-container:focus-visible {
                outline: 2px solid #ff6000;
                outline-offset: 2px;
                border-radius: 4px;
            }

            /* High Contrast Mode */
            @media (prefers-contrast: high) {
                .star-filled {
                    color: #ff8800 !important;
                    text-shadow: none;
                }

                .star-half {
                    color: #ff8800 !important;
                    text-shadow: none;
                }

                .star-empty {
                    color: #666 !important;
                }
            }

            /* Enhanced Rating Display Styles */
            .rating-stars-clean-display .stars-container {
                display: inline-flex;
                align-items: center;
                margin: 0 4px;
            }

            .comment-rating-stars .stars-container {
                display: inline-flex;
                align-items: center;
                gap: 1px;
            }

            .tooltip-stars .stars-container {
                display: inline-flex;
                align-items: center;
                gap: 1px;
                margin-right: 8px;
            }

            .rating-breakdown-info {
                display: flex;
                align-items: center;
                gap: 4px;
            }

            .rating-percentage-tooltip {
                font-size: 11px;
                color: #888;
                font-weight: 400;
            }

            /* Rating Tooltip Enhancements */
            .rating-breakdown-row {
                display: flex;
                align-items: center;
                justify-content: space-between;
                padding: 4px 0;
                border-bottom: 1px solid #f0f0f0;
            }

            .rating-breakdown-row:last-child {
                border-bottom: none;
            }

            .rating-count-tooltip {
                font-weight: 600;
                color: #333;
                min-width: 20px;
                text-align: right;
            }

            /* Responsive Star Sizes */
            @media (max-width: 768px) {
                .rating-value {
                    font-size: 13px;
                }
            }

            /* Enhanced Star Animation */
            .stars-container[data-interactive="true"] .star {
                transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
            }

            .stars-container[data-interactive="true"] .star:hover {
                transform: scale(1.15) rotate(5deg);
                filter: brightness(1.3);
            }

            /* Print Styles */
            @media print {
                .stars-container {
                    -webkit-print-color-adjust: exact;
                    color-adjust: exact;
                }

                .star-filled {
                    color: #000 !important;
                    text-shadow: none;
                }
            }

            /* Motion Reduced */
            @media (prefers-reduced-motion: reduce) {
                .star, .star-small {
                    transition: none;
                }

                .stars-container[data-interactive="true"] .star {
                    transition: none;
                }

                .stars-container[data-interactive="true"] .star:hover {
                    transform: none;
                    filter: none;
                }
            }

            /* Modern Reviews Container */
            .modern-reviews-container {
                margin: 30px 0;
                font-family: "Source Sans 3", sans-serif;
                background: white;
                overflow: hidden;
                display: flex;
                flex-direction: column;
                gap: 16px;
            }

            .reviews-header {
                padding: 16px 20px;
                background: #fff9eb);
                border-bottom: 1px solid #ffe0b3;
            }

            .reviews-main-title {
                margin: 0;
                font-size: 20px;
                font-weight: 700;
                color: #333;
            }

            .reviews-title {
                margin: 0;
                font-size: 16px;
                font-weight: 400;
                color: #333;
                display: flex;
                align-items: center;
                gap: 12px;
                position: relative;
                padding: 12px 0;
            }

            .title-rating-container {
                display: inline-flex;
                align-items: center;
                gap: 8px;
            }

            .title-rating-stars {
                margin-right: 4px;
            }

            .title-rating-stars .star {
                font-size: 30px;
                color: #ffc107;
            }

            .title-rating-stars .star-empty {
                color: #e0e0e0;
            }

            .title-rating-score {
                font-size: 24px;
                font-weight: 700;
                color: #333;
                transition: all 0.2s ease;
            }

            .title-dropdown-arrow {
                font-size: 14px;
                color: #999;
                transition: transform 0.3s ease, color 0.2s ease;
                cursor: pointer;
                display: inline-flex;
                align-items: center;
                justify-content: center;
            }

            .title-dropdown-arrow:hover {
                color: #ff6000;
            }

            .title-rating-separator {
                font-size: 12px;
                color: #999;
                margin: 0 4px;
            }

            .title-rating-count {
                font-size: 16px;
                color: #333;
                font-weight: 700;
            }

            .title-photo-icon {
                font-size: 20px;
                color: #666;
                display: inline-flex;
                align-items: center;
                justify-content: center;
                line-height: 1;
                vertical-align: middle;
                height: 26px;
                transition: all 0.2s ease;
            }

            .title-photo-icon:hover {
                transform: scale(1.05);
            }

            .reviews-title-tooltip {
                position: fixed;
                background: white;
                border: 1px solid #e0e0e0;
                border-radius: 12px;
                box-shadow: 0 8px 32px rgba(0,0,0,0.1);
                padding: 16px;
                min-width: 280px;
                z-index: 10001;
                display: none;
                backdrop-filter: blur(8px);
                animation: tooltipSlideIn 0.3s ease-out;
            }

            @keyframes tooltipSlideIn {
                from {
                    opacity: 0;
                    transform: translateY(-8px) scale(0.95);
                }
                to {
                    opacity: 1;
                    transform: translateY(0) scale(1);
                }
            }

            @keyframes fadeIn {
                from {
                    opacity: 0;
                }
                to {
                    opacity: 1;
                }
            }

            @keyframes fadeOut {
                from {
                    opacity: 1;
                }
                to {
                    opacity: 0;
                }
            }

            .reviews-count {
                font-size: 16px;
                color: #ff8533;
                font-weight: 600;
            }

            /* Search and Sort Controls */
            .reviews-search-controls {
                display: flex;
                justify-content: space-between;
                gap: 16px;
            }

            .search-input-container {
                position: relative;
                flex: 1;
                max-width: 400px;
                display: flex;
            }

            /* Right side controls container */
            .sort-and-filter-container {
                display: flex;
                align-items: center;
                gap: 12px;
            }

            .reviews-search-input {
                width: 100%;
                padding: 8px 36px 8px 12px;
                border: 1px solid #e0e0e0;
                border-radius: 8px;
                font-size: 14px;
                font-family: "Source Sans 3", sans-serif;
                transition: all 0.2s ease;
                background: white;
            }

            .reviews-search-input:focus {
                outline: none;
                border-color: #ff6000;
                box-shadow: 0 0 0 2px rgba(255, 96, 0, 0.1);
            }

            .search-icon {
                position: absolute;
                right: 12px;
                top: 50%;
                transform: translateY(-50%);
                color: #999;
                pointer-events: none;
                display: flex;
                align-items: center;
                justify-content: center;
            }

            .sort-select-container {
                min-width: 180px;
                position: relative;
            }

            .reviews-sort-select {
                width: 100%;
                padding: 10px 40px 10px 16px; /* Right padding için arrow alanı */
                border: 1px solid #e0e0e0;
                border-radius: 8px;
                font-size: 14px;
                font-family: "Source Sans 3", sans-serif;
                font-weight: 400;
                background: white;
                cursor: pointer;
                transition: all 0.2s ease;
                height: 44px;
                box-sizing: border-box;
                appearance: none; /* Default arrow'u kaldır */
                -webkit-appearance: none;
                -moz-appearance: none;
                line-height: 22px; /* Yazıyı dikey olarak ortala */
                vertical-align: middle;
            }

            /* Custom dropdown arrow for sort select */
            .sort-select-container::after {
                content: "▼";
                position: absolute;
                right: 12px;
                top: 50%;
                transform: translateY(-50%);
                color: #666;
                font-size: 12px;
                pointer-events: none;
                transition: transform 0.2s ease;
            }

            .reviews-sort-select:focus {
                outline: none;
                border-color: #ff6000;
                box-shadow: 0 0 0 2px rgba(255, 96, 0, 0.1);
            }

            .reviews-sort-select:hover {
                border-color: #ff6000;
            }

            /* Rating Filter Styles */
            .rating-filter-container {
                min-width: 120px;
                position: relative;
            }

            .rating-filter-dropdown {
                position: relative;
            }

            .rating-filter-button {
                display: flex;
                align-items: center;
                justify-content: space-between;
                width: 100%;
                padding: 10px 16px;
                border: 1px solid #e0e0e0;
                border-radius: 8px;
                font-size: 14px;
                font-family: "Source Sans 3", sans-serif;
                background: white;
                cursor: pointer;
                transition: all 0.2s ease;
                user-select: none;
                height: 44px;
                box-sizing: border-box;
            }

            .rating-filter-button:hover {
                border-color: #ff6000;
            }

            .rating-filter-button.active {
                border-color: #ff6000;
                box-shadow: 0 0 0 2px rgba(255, 96, 0, 0.1);
            }

            .filter-text {
                color: #333;
                font-weight: 500;
            }

            .filter-arrow {
                color: #666;
                font-size: 12px;
                transition: transform 0.2s ease;
            }

            .rating-filter-button.active .filter-arrow {
                transform: rotate(180deg);
            }

            .rating-filter-menu {
                position: absolute;
                top: 100%;
                left: 0;
                right: 0;
                background: white;
                border: 1px solid #e0e0e0;
                border-radius: 8px;
                box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
                z-index: 1000;
                display: none;
                margin-top: 4px;
                overflow-y: auto;
                width: fit-content;
            }

            .rating-filter-menu.show {
                display: block;
            }

            .rating-filter-options {
                padding: 12px 16px;
                display: flex;
                flex-direction: column;
                gap: 8px;
            }

            .rating-option {
                padding: 0;
                border-bottom: 1px solid #f5f5f5;
                display: flex;
                align-items: center;
                flex-direction: row;
            }

            .rating-option:last-of-type {
                border-bottom: none;
            }

            .rating-option label {
                display: flex;
                align-items: center;
                cursor: pointer;
                transition: background-color 0.2s ease;
                font-size: 14px;
                font-family: "Source Sans 3", sans-serif;
                margin: 0;
                width: 100%;
                box-sizing: border-box;
                gap: 8px;
            }

            .rating-option input[type="checkbox"] {
                width: 16px;
                height: 16px;
                accent-color: #ff6000;
                flex-shrink: 0;
            }

            .rating-text {
                color: #333;
                font-weight: 500;
            }

            .rating-score {
                color: #333;
                font-weight: 600;
                min-width: 30px;
                text-align: center;
            }

            .rating-stars {
                flex-shrink: 0;
                display: flex;
                align-items: center;
            }

            .rating-stars .filter-stars {
                margin: 0;
                padding: 0;
            }

            .rating-stars .filter-stars .star {
                font-size: 14px !important;
                margin: 0 1px !important;
            }

            .rating-count {
                color: #666;
                font-size: 12px;
                margin-left: auto;
                flex-shrink: 0;
            }

            .filter-actions {
                display: flex;
                gap: 8px;
                padding: 12px 16px;
                border-top: 1px solid #f0f0f0;
                background-color: #fafafa;
                border-radius: 0 0 8px 8px;
                margin-top: auto;
            }

            .btn-clear, .btn-apply {
                flex: 1;
                padding: 8px 12px;
                border: 1px solid #e0e0e0;
                border-radius: 4px;
                font-size: 12px;
                font-family: "Source Sans 3", sans-serif;
                font-weight: 500;
                cursor: pointer;
                transition: all 0.2s ease;
                white-space: nowrap;
            }

            .btn-clear {
                background: white;
                color: #666;
            }

            .btn-clear:hover {
                background-color: #f8f8f8;
                border-color: #ccc;
            }

            .btn-apply {
                background: #ff6000;
                color: white;
                border-color: #ff6000;
            }

            .btn-apply:hover {
                background: #e55500;
                border-color: #e55500;
            }

            /* Photo Reviews Section */
            .photo-reviews-section {
                border-top: 1px solid #f0f0f0;
            }

            .photo-reviews-title {
                font-size: 14px;
                color: #333;
            }

            .photo-reviews-container {
                display: flex;
                gap: 8px;
                overflow-x: auto;
                padding: 8px 0;
                scrollbar-width: thin;
                scrollbar-color: #ddd transparent;
            }

            .photo-reviews-container::-webkit-scrollbar {
                height: 4px;
            }

            .photo-reviews-container::-webkit-scrollbar-track {
                background: #f0f0f0;
                border-radius: 2px;
            }

            .photo-reviews-container::-webkit-scrollbar-thumb {
                background: #ddd;
                border-radius: 2px;
            }

            .photo-reviews-container::-webkit-scrollbar-thumb:hover {
                background: #bbb;
            }

            .photo-review-item {
                flex-shrink: 0;
                width: 80px;
                height: 80px;
                border-radius: 8px;
                overflow: hidden;
                cursor: pointer;
                border: 1px solid transparent;
            }

            .photo-review-item:hover {
                border-color: #ff6000;
            }

            .photo-review-item img {
                width: 100%;
                height: 100%;
                object-fit: cover;
            }

            /* Responsive Design for Search Controls */
            @media (max-width: 768px) {
                .reviews-search-controls {
                    flex-direction: column;
                    gap: 12px;
                }

                .search-input-container {
                    max-width: none;
                }

                .sort-and-filter-container {
                    flex-direction: row;
                    gap: 8px;
                    width: 100%;
                }

                .sort-select-container {
                    min-width: auto;
                    flex: 1;
                }

                .rating-filter-container {
                    min-width: auto;
                    flex: 1;
                }

                .rating-filter-button {
                    width: 100%;
                }

                .rating-filter-menu {
                    left: 0;
                    right: 0;
                    min-width: 100%;
                    max-height: 350px;
                }

            }



            /* Skeleton Loading Styles */
            @keyframes shimmer {
                0% {
                    background-position: -200px 0;
                }
                100% {
                    background-position: calc(200px + 100%) 0;
                }
            }

            .skeleton-base {
                background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                background-size: 200px 100%;
                animation: shimmer 1.5s infinite linear;
                border-radius: 4px;
            }

            /* Rating Skeleton Loading */
            .skeleton-rating-container {
                display: flex;
                align-items: center;
                gap: 8px;
            }

            .skeleton-rating-score {
                width: 32px;
                height: 20px;
                background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                background-size: 200% 100%;
                animation: skeleton-shimmer 1.5s infinite;
                border-radius: 4px;
            }

            .skeleton-stars {
                width: 80px;
                height: 16px;
                background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                background-size: 200% 100%;
                animation: skeleton-shimmer 1.5s infinite;
                border-radius: 4px;
            }

            .skeleton-dot {
                color: #e0e0e0;
                font-size: 16px;
                line-height: 1;
            }

            .skeleton-rating-count {
                width: 110px;
                height: 16px;
                background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                background-size: 200% 100%;
                animation: skeleton-shimmer 1.5s infinite;
                border-radius: 4px;
            }

            /* Rating Loading Positioning */
            .rating-loading {
                display: inline-flex;
                align-items: center;
                padding: 12px 0px;
                font-family: "Source Sans 3", sans-serif;
            }

            /* Reviews Skeleton */
            .skeleton-reviews-container {
                display: flex;
                flex-direction: column;
                gap: 1px;
                background: white;
            }

            .skeleton-review-item {
                padding: 16px 0;
                border-bottom: 1px solid #f0f0f0;
                display: flex;
                flex-direction: column;
                gap: 12px;
            }

            .skeleton-review-item:last-child {
                border-bottom: none;
            }

            .skeleton-review-header {
                display: flex;
                align-items: center;
                gap: 8px;
            }

            .skeleton-stars-rating {
                width: 80px;
                height: 14px;
                background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                background-size: 200px 100%;
                animation: shimmer 1.5s infinite linear;
                border-radius: 4px;
            }

            .skeleton-date-small {
                width: 60px;
                height: 10px;
                background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                background-size: 200px 100%;
                animation: shimmer 1.5s infinite linear;
                border-radius: 4px;
                margin-left: auto;
            }

            .skeleton-review-text {
                display: flex;
                flex-direction: column;
                gap: 8px;
            }

            .skeleton-text-line {
                height: 14px;
                background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                background-size: 200px 100%;
                animation: shimmer 1.5s infinite linear;
                border-radius: 4px;
            }

            .skeleton-text-line.full { width: 100%; }
            .skeleton-text-line.long { width: 85%; }
            .skeleton-text-line.medium { width: 70%; }
            .skeleton-text-line.short { width: 45%; }

            .skeleton-review-footer {
                display: flex;
                align-items: center;
                justify-content: space-between;
            }

            .skeleton-user-name {
                width: 120px;
                height: 12px;
                background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                background-size: 200px 100%;
                animation: shimmer 1.5s infinite linear;
                border-radius: 4px;
            }

            .skeleton-verified {
                width: 80px;
                height: 10px;
                background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                background-size: 200px 100%;
                animation: shimmer 1.5s infinite linear;
                border-radius: 4px;
            }

            /* Rating Summary Skeleton */
            .skeleton-rating-summary {
                padding: 16px 8px;
                display: flex;
                align-items: center;
            }

            .skeleton-main-rating {
                display: flex;
                align-items: center;
                gap: 12px;
            }

            .skeleton-score-large {
                width: 32px;
                height: 24px;
                background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                background-size: 200px 100%;
                animation: shimmer 1.5s infinite linear;
                border-radius: 4px;
            }

            .skeleton-stars-row {
                width: 100px;
                height: 18px;
                background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                background-size: 200px 100%;
                animation: shimmer 1.5s infinite linear;
                border-radius: 4px;
            }

            .skeleton-count-text {
                width: 80px;
                height: 12px;
                background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                background-size: 200px 100%;
                animation: shimmer 1.5s infinite linear;
                border-radius: 4px;
            }

            .loading-spinner {
                width: 20px;
                height: 20px;
                border: 2px solid #ffe0b3;
                border-top: 2px solid #ff6000;
                border-radius: 50%;
                animation: spin 1s linear infinite;
                display: inline-block;
                margin-right: 8px;
            }

            .no-reviews-filtered, .no-reviews-modern {
                text-align: center;
                padding: 40px 20px;
                color: #666;
                background: linear-gradient(135deg, #fff5e6 0%, #fff 100%);
                border-radius: 12px;
                border: 1px solid #ffe0b3;
            }

            .no-reviews-icon {
                margin-bottom: 16px;
                opacity: 0.7;
                display: flex;
                justify-content: center;
                color: #999;
            }

            .no-reviews-filtered h3, .no-reviews-modern h3 {
                margin: 0 0 8px 0;
                color: #ff6000;
                font-size: 18px;
                font-weight: 600;
            }

            .no-reviews-filtered p, .no-reviews-modern p {
                margin: 0 0 16px 0;
                color: #666;
                font-size: 14px;
            }

            .clear-filters-btn {
                background: linear-gradient(135deg, #ff6000 0%, #ff8533 100%);
                color: white;
                border: none;
                padding: 8px 16px;
                border-radius: 8px;
                font-size: 14px;
                font-weight: 600;
                cursor: pointer;
                transition: all 0.3s ease;
            }

            .clear-filters-btn:hover {
                background: linear-gradient(135deg, #e55a00 0%, #ff6000 100%);
            }

            /* Responsive Design */
            @media (max-width: 768px) {
                .comment-card {
                    flex: 0 0 280px;
                }

                .rating-display {
                    flex-direction: column;
                    align-items: center;
                    gap: 16px;
                    text-align: center;
                }

                .rating-tooltip {
                    position: fixed;
                    top: 50%;
                    left: 50%;
                    transform: translate(-50%, -50%);
                    max-width: 90vw;
                }

                .comments-scroll-container {
                    padding: 15px;
                }

                .scroll-controls {
                    gap: 12px;
                }

                .scroll-btn {
                    width: 45px;
                    height: 45px;
                    font-size: 16px;
                }
            }

            /* Modern Reviews Container Styles - Ruiada Inspired */
            .modern-reviews-container {
                margin: 40px 0;
                padding: 40px 0;
                font-family: "Source Sans 3", sans-serif;
                position: relative;
                z-index: 1;
                width: 100%;
                box-sizing: border-box;
                clear: both;
            }

            .reviews-header {
                text-align: left;
                background: #fff9eb;
                border-bottom: 1px solid #e0e0e0;
            }

            .reviews-title {
                margin: 0;
                font-size: 22px;
                font-weight: 600;
                color: #333333;
                display: flex;
                align-items: center;
                gap: 8px;
            }

            .reviews-icon {
                font-size: 24px;
                color: #666666;
            }

            .reviews-count {
                background: #f5f5f5;
                color: #666666;
                padding: 4px 12px;
                border-radius: 16px;
                font-size: 14px;
                font-weight: 500;
                margin-left: 8px;
                border: 1px solid #e0e0e0;
            }

            .reviews-filters {
                display: flex;
                justify-content: center;
                align-items: center;
                gap: 20px;
                margin-bottom: 30px;
                padding: 20px;
                background: white;
                border-radius: 16px;
                box-shadow: 0 4px 16px rgba(0,0,0,0.05);
                flex-wrap: wrap;
            }

            .filter-group {
                display: flex;
                align-items: center;
                gap: 8px;
            }

            .filter-label {
                font-weight: 600;
                color: #495057;
                font-size: 14px;
            }

            .filter-select {
                padding: 8px 16px;
                border: 2px solid #e9ecef;
                border-radius: 12px;
                background: white;
                font-size: 14px;
                font-weight: 500;
                color: #495057;
                cursor: pointer;
                transition: all 0.3s ease;
                min-width: 120px;
            }

            .filter-select:focus {
                outline: none;
                border-color: #ff6000;
                box-shadow: 0 0 0 3px rgba(255, 96, 0, 0.1);
            }

            .filter-select:hover {
                border-color: #ff6000;
            }

            .filter-reset-btn {
                background: linear-gradient(135deg, #6c757d 0%, #495057 100%);
                color: white;
                border: none;
                padding: 8px 16px;
                border-radius: 12px;
                font-size: 14px;
                font-weight: 600;
                cursor: pointer;
                transition: all 0.3s ease;
                display: flex;
                align-items: center;
                gap: 6px;
            }

            .filter-reset-btn:hover {
                background: linear-gradient(135deg, #495057 0%, #343a40 100%);
                transform: translateY(-2px);
                box-shadow: 0 4px 12px rgba(108, 117, 125, 0.3);
            }

            .modern-comments-grid {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
                gap: 16px;
                margin-bottom: 25px;
            }

            .modern-comment-card {
                background: white;
                border: 1px solid #e0e0e0;
                border-radius: 8px;
                padding: 20px;
                transition: all 0.2s ease;
                position: relative;
                box-shadow: 0 1px 4px rgba(0,0,0,0.1);
            }

            .modern-comment-card:hover {
                box-shadow: 0 4px 12px rgba(0,0,0,0.15);
                border-color: #cccccc;
            }

            .modern-comment-header {
                display: flex;
                justify-content: space-between;
                align-items: flex-start;
                margin-bottom: 12px;
                padding-bottom: 10px;
                border-bottom: 1px solid #f0f0f0;
            }

            .modern-comment-user {
                display: flex;
                align-items: center;
                gap: 10px;
            }

            .user-avatar {
                width: 40px;
                height: 40px;
                background: #007bff;
                color: white;
                border-radius: 50%;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 16px;
                font-weight: 600;
            }

            .user-info {
                display: flex;
                flex-direction: column;
                gap: 2px;
            }

            .user-name {
                font-size: 14px;
                font-weight: 600;
                color: #333333;
            }

            .modern-elite-badge {
                background: #28a745;
                color: white;
                padding: 2px 6px;
                border-radius: 4px;
                font-size: 9px;
                font-weight: 600;
                text-transform: uppercase;
            }

            .modern-comment-rating {
                display: flex;
                align-items: center;
                gap: 6px;
            }

            .modern-comment-text {
                font-size: 14px;
                line-height: 1.5;
                color: #555555;
                margin: 0 0 12px 0;
            }

            .modern-comment-photos {
                display: flex;
                gap: 6px;
                margin-top: 10px;
                position: relative;
            }

            .modern-comment-photo {
                width: 50px;
                height: 50px;
                object-fit: cover;
                border-radius: 6px;
                cursor: pointer;
                transition: all 0.2s ease;
                border: 1px solid #e0e0e0;
            }

            .modern-comment-photo:hover {
                transform: scale(1.05);
                border-color: #007bff;
            }

            .more-photos {
                width: 50px;
                height: 50px;
                background: rgba(0,0,0,0.6);
                color: white;
                border-radius: 6px;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 11px;
                font-weight: 600;
                cursor: pointer;
            }

            .modern-comment-footer {
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-top: 12px;
                padding-top: 10px;
                border-top: 1px solid #f0f0f0;
                font-size: 12px;
                color: #888888;
            }

            .modern-comment-date {
                font-style: italic;
            }

            .modern-comment-likes {
                display: flex;
                align-items: center;
                gap: 4px;
                background: #f5f5f5;
                padding: 4px 8px;
                border-radius: 12px;
                transition: all 0.2s ease;
            }

            .modern-comment-likes:hover {
                background: #e9ecef;
            }

            .like-count {
                font-weight: 600;
                color: #666666;
            }

            .modern-reviews-loading {
                text-align: center;
                padding: 60px 20px;
                color: #6c757d;
            }

            .modern-loading-spinner {
                width: 40px;
                height: 40px;
                border: 4px solid #f8f9fa;
                border-top: 4px solid #ff6000;
                border-radius: 50%;
                animation: spin 1s linear infinite;
                margin: 0 auto 20px;
            }

            @keyframes spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }

            .no-reviews-modern, .no-reviews-filtered {
                text-align: center;
                padding: 60px 20px;
                color: #6c757d;
            }

            .no-reviews-icon {
                margin-bottom: 16px;
                opacity: 0.7;
                display: flex;
                justify-content: center;
                color: #999;
            }

            .no-reviews-modern h3, .no-reviews-filtered h3 {
                margin: 0 0 12px 0;
                font-size: 20px;
                font-weight: 600;
                color: #495057;
            }

            .no-reviews-modern p, .no-reviews-filtered p {
                margin: 0;
                font-size: 16px;
                opacity: 0.8;
            }

            .clear-filters-btn {
                background: linear-gradient(135deg, #ff6000 0%, #ff8533 100%);
                color: white;
                border: none;
                padding: 12px 24px;
                border-radius: 12px;
                font-size: 14px;
                font-weight: 600;
                cursor: pointer;
                transition: all 0.3s ease;
                margin-top: 16px;
            }

            .clear-filters-btn:hover {
                background: linear-gradient(135deg, #e55a00 0%, #ff6000 100%);
                box-shadow: 0 6px 20px rgba(255, 96, 0, 0.4);
            }

            /* Responsive Design for Modern Container */
            @media (max-width: 768px) {
                .modern-reviews-container {
                    margin: 20px 0;
                    padding: 20px;
                }

                .reviews-title {
                    flex-direction: column;
                    align-items: flex-start;
                    gap: 12px;a
                    padding: 0;
                }

                .title-rating-container {
                    flex-wrap: wrap;
                    gap: 6px;
                }

                .title-rating-separator {
                    display: none;
                }

                .reviews-filters {
                    flex-direction: column;
                    gap: 12px;
                }

                .modern-comments-grid {
                    grid-template-columns: 1fr;
                    gap: 16px;
                }

                .modern-comment-card {
                    padding: 16px;
                }

                .user-avatar {
                    width: 40px;
                    height: 40px;
                    font-size: 16px;
                }

                .trendyol-comment-footer {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    flex-direction: row; /* Aynı satırda tut */
                    font-size: 12px; /* Normal boyut */
                }
            }

            /* ===== Card Review Badge Styles ===== */
            .card-review-badge {
                display: flex;
                align-items: center;
                justify-content: center;
                gap: 5px;
                padding-top: 4px;
                font-family: "Source Sans 3", sans-serif;
                line-height: 1;
            }

            .card-review-stars {
                display: inline-flex;
                align-items: center;
            }

            .card-review-stars .stars-container {
                display: inline-flex;
                align-items: center;
            }

            .card-review-stars .stars-display {
                display: inline-flex;
                gap: 1px;
            }

            .card-review-stars .star-small {
                font-size: 20px;
                line-height: 1;
            }

            .card-review-score {
                font-size: 15px;
                line-height: normal;
                font-weight: 600;
                color: #666;
            }

            .card-review-count {
                font-size: 12px;
                color: #999;
            }

            .card-review-photo {
                font-size: 15px;
                margin-bottom: 2px;
            }
        `;

        document.head.appendChild(style);
    }

    // ==================== CARD (LİSTELEME) REVIEW ENJEKSİYON ====================

    /**
     * Listeleme sayfasındaki ürün kartlarına değerlendirme yıldızları enjekte et
     * .ProductList .productItem elementlerini bulur ve her birine mini rating ekler
     */
    function setupCardReviews() {
        try {
            const companyId = CONFIG.getCompanyId();
            if (!companyId) return;

            function findAndInjectCards(retryCount) {
                retryCount = retryCount || 0;
                const productItems = document.querySelectorAll('.ProductList .productItem');

                if (productItems.length === 0 && retryCount < 8) {
                    findAndInjectCards(retryCount + 1);
                    return;
                }

                if (productItems.length === 0) return;

                // Benzersiz barkodları topla ve ilgili elementlerle eşle
                var barkodMap = {}; // barkod -> [element, ...]
                productItems.forEach(function (item) {
                    if (item.querySelector('.card-review-badge')) return; // zaten enjekte edilmiş

                    var barkod = CONFIG.getCardBarkod(item);
                    if (!barkod) return;

                    if (!barkodMap[barkod]) barkodMap[barkod] = [];
                    barkodMap[barkod].push(item);
                });

                var barkods = Object.keys(barkodMap);
                if (barkods.length === 0) return;

                // Her barkod için review verisini getir
                barkods.forEach(function (barkod) {
                    fetchCardReviewData(companyId, barkod, barkodMap[barkod]);
                });
            }

            findAndInjectCards(0);

            // MutationObserver ile yeni eklenen kartları da yakala (infinite scroll vb.)
            setupCardReviewsObserver(companyId);

        } catch (error) {
            console.error('Kart değerlendirme kurulumunda hata:', error);
        }
    }

    /**
     * MutationObserver: Dinamik yüklenen ürün kartlarını yakala
     */
    var _cardObserverActive = false;
    function setupCardReviewsObserver(companyId) {
        if (_cardObserverActive) return;
        _cardObserverActive = true;

        var debounceTimer = null;
        var observer = new MutationObserver(function () {
            if (debounceTimer) clearTimeout(debounceTimer);
            debounceTimer = setTimeout(function () {
                var newItems = document.querySelectorAll('.ProductList .productItem:not(:has(.card-review-badge))');
                if (newItems.length === 0) return;

                var barkodMap = {};
                newItems.forEach(function (item) {
                    var barkod = CONFIG.getCardBarkod(item);
                    if (!barkod) return;
                    if (!barkodMap[barkod]) barkodMap[barkod] = [];
                    barkodMap[barkod].push(item);
                });

                Object.keys(barkodMap).forEach(function (barkod) {
                    fetchCardReviewData(companyId, barkod, barkodMap[barkod]);
                });
            }, 500);
        });

        var targetNode = document.querySelector('.ProductList') || document.body;
        observer.observe(targetNode, { childList: true, subtree: true });
    }

    /**
     * Tek bir barkod için review verisini getir ve kartlara enjekte et
     */
    var _cardReviewCache = {};
    function fetchCardReviewData(companyId, barkod, elements) {
        // Önbellek kontrolü
        if (_cardReviewCache[barkod]) {
            injectCardRating(elements, _cardReviewCache[barkod]);
            return;
        }

        var apiUrl = CONFIG.r2BaseUrl + '/customer_' + companyId + '/comments/' + barkod + '_comment.json';

        fetch(apiUrl)
            .then(function (response) {
                if (!response.ok) throw new Error('HTTP ' + response.status);
                return response.json();
            })
            .then(function (data) {
                if (!data || !Array.isArray(data) || data.length === 0) return;

                var normalizedComments = data.map(function (c) { return normalizeComment(c); });
                var stats = RatingAnalytics.calculateRatingStats(normalizedComments);

                // Önbelleğe kaydet
                _cardReviewCache[barkod] = stats;

                injectCardRating(elements, stats);
            })
            .catch(function () {
                // Yorum bulunamadı - sessizce geç
            });
    }

    /**
     * Ürün kartlarına mini değerlendirme badge'i enjekte et
     * .productDetail elementinin içine en üste ekler
     */
    function injectCardRating(elements, stats) {
        if (!stats || !stats.hasRatings) return;

        elements.forEach(function (item) {
            if (item.querySelector('.card-review-badge')) return; // zaten eklendi

            var productDetail = item.querySelector('.productDetail');
            if (!productDetail) {
                // productDetail yoksa, kartın sonuna ekle
                productDetail = null;
            }

            var badge = document.createElement('div');
            badge.className = 'card-review-badge';
            badge.innerHTML =
                '<span class="card-review-score">' + stats.average.toFixed(1) + '</span>' +
                '<div class="card-review-stars">' +
                StarRating.render(stats.average, {
                    size: 'small',
                    showValue: false,
                    className: 'card-stars'
                }) +
                '</div>' +
                '<span class="card-review-count">(' + stats.total + ')</span>';
            if (stats.photoCount > 0) {
                badge.innerHTML += '<span class="card-review-photo" title="' + stats.photoCount + ' fotoğraflı değerlendirme">📸</span>';
            }

            if (productDetail) {
                // productDetail içine, en başa ekle
                productDetail.insertBefore(badge, productDetail.firstChild);
            } else {
                item.appendChild(badge);
            }
        });
    }

    /**
     * Public API for external configuration
     *
     * Kullanım Örneği:
     *
     * 1. Script tag ile:
     *    <script src="trendyol-reviews.js" data-company-id="your-company-id"></script>
     *
     * 2. JavaScript ile:
     *    TrendyolReviews.configure({ companyId: 'your-company-id' });
     *    TrendyolReviews.init();
     *
     * URL Pattern: https://assets.pushonica.com/reviews/customer_{companyId}/comments/{barkod}_comment.json
     */
    window.TrendyolReviews = {
        init: init,
        /**
         * Widget'ı yapılandır
         * @param {Object} options
         * @param {string} options.companyId - Şirket ID'si (zorunlu)
         * @param {string} [options.r2BaseUrl] - R2 base URL (opsiyonel, varsayılan: https://assets.pushonica.com/reviews)
         * @param {Object} [options.targetClasses] - Hedef CSS sınıfları (opsiyonel)
         */
        configure: function (options) {
            if (options.companyId) {
                CONFIG.companyId = options.companyId;
            }
            if (options.r2BaseUrl) {
                CONFIG.r2BaseUrl = options.r2BaseUrl;
            }
            if (options.targetClasses) {
                Object.assign(CONFIG.targetClasses, options.targetClasses);
            }
        },
        setCompanyId: function (companyId) {
            CONFIG.companyId = companyId;
        },
        setBarkod: function (barkod) {
            CONFIG.getBarkod = function () { return barkod; };
        },
        refresh: function () {
            const barkod = CONFIG.getBarkod();
            if (barkod) {
                isInitialized = false;
                fetchReviews(barkod);
            }
        },
        /**
         * Listeleme sayfasındaki ürün kartlarına değerlendirmeleri yeniden enjekte et
         * Infinite scroll veya AJAX ile yüklenen yeni kartlar için kullanışlı
         */
        refreshCards: function () {
            setupCardReviews();
        },
        getConfig: function () {
            return {
                companyId: CONFIG.getCompanyId(),
                r2BaseUrl: CONFIG.r2BaseUrl,
                barkod: CONFIG.getBarkod()
            };
        }
    };

    // Auto-initialize when script loads with improved timing
    function autoInit() {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', function () {
                init();
            });
        } else {
            init();
        }
    }

    // Başlat
    autoInit();

})();
