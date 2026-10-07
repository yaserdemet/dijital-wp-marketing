/**
 * Ticimax DOM Analyzer
 * Ticimax e-ticaret sitelerinin DOM yapısını analiz eden modül
 */

class TicimaxDOMAnalyzer {
    constructor() {
        this.pageType = null;
        this.selectors = this.getTicimaxSelectors();
    }
    
    getTicimaxSelectors() {
        return {
            // Ana sayfa selectors
            homepage: {
                indicators: [
                    '.homepage',
                    '.main-slider',
                    '.featured-products',
                    '.home-content',
                    'body.homepage',
                    '.slider-container'
                ],
                insertPoints: [
                    '.main-content',
                    '.content',
                    '.container',
                    'main',
                    '#content'
                ]
            },
            
            // Kategori sayfası selectors
            category: {
                indicators: [
                    '.category-page',
                    '.product-list',
                    '.products-grid',
                    '.category-products',
                    '.products-container',
                    '.category-content',
                    'body.category'
                ],
                insertPoints: [
                    '.product-list',
                    '.products-grid',
                    '.category-products',
                    '.products-container'
                ]
            },
            
            // Ürün detay sayfası selectors
            product: {
                indicators: [
                    '.product-page',
                    '.product-detail',
                    '.product-info',
                    '.product-content',
                    '.product-images',
                    'body.product',
                    '.product-main'
                ],
                insertPoints: [
                    '.product-detail',
                    '.product-info',
                    '.product-content',
                    '.product-main'
                ]
            },
            
            // Sepet sayfası selectors
            cart: {
                indicators: [
                    '.cart-page',
                    '.shopping-cart',
                    '.cart-content',
                    '.basket-page',
                    'body.cart',
                    '.cart-items'
                ],
                insertPoints: [
                    '.cart-content',
                    '.shopping-cart',
                    '.cart-items'
                ]
            },
            
            // Ödeme sayfası selectors
            checkout: {
                indicators: [
                    '.checkout-page',
                    '.payment-page',
                    '.order-page',
                    '.checkout-content',
                    'body.checkout',
                    '.payment-form'
                ],
                insertPoints: [
                    '.checkout-content',
                    '.payment-page',
                    '.order-page'
                ]
            }
        };
    }
    
    analyzePageType() {
        if (this.pageType) return this.pageType;
        
        const url = window.location.href.toLowerCase();
        const pathname = window.location.pathname.toLowerCase();
        const title = document.title.toLowerCase();
        
        // URL tabanlı analiz
        if (this.isHomepage(url, pathname)) {
            this.pageType = 'homepage';
        } else if (this.isCategoryPage(url, pathname)) {
            this.pageType = 'category';
        } else if (this.isProductPage(url, pathname)) {
            this.pageType = 'product';
        } else if (this.isCartPage(url, pathname, title)) {
            this.pageType = 'cart';
        } else if (this.isCheckoutPage(url, pathname, title)) {
            this.pageType = 'checkout';
        } else {
            // DOM tabanlı analiz
            this.pageType = this.analyzeDOMStructure();
        }
        
        return this.pageType || 'other';
    }
    
    isHomepage(url, pathname) {
        return pathname === '/' || 
               pathname === '/index.html' || 
               pathname === '' ||
               pathname === '/anasayfa' ||
               pathname === '/home';
    }
    
    isCategoryPage(url, pathname) {
        const categoryPatterns = [
            '/kategori/',
            '/category/',
            '/categories/',
            '/kategoriler/',
            '/c/',
            '/cat/'
        ];
        
        return categoryPatterns.some(pattern => 
            url.includes(pattern) || pathname.includes(pattern)
        );
    }
    
    isProductPage(url, pathname) {
        const ogType = document.querySelector('meta[property="og:type"]');
        if (ogType && ogType.content === 'product') {
            return true;
        }

        const productPatterns = [
            '/urun/',
            '/product/',
            '/products/',
            '/p/',
            '/item/',
            '/detay/'
        ];
        
        return productPatterns.some(pattern => 
            url.includes(pattern) || pathname.includes(pattern)
        );
    }
    
    isCartPage(url, pathname, title) {
        const cartPatterns = [
            '/sepet',
            '/cart',
            '/basket',
            '/shopping-cart'
        ];
        
        const titlePatterns = [
            'sepet',
            'cart',
            'basket',
            'alışveriş sepeti'
        ];
        
        return cartPatterns.some(pattern => 
            url.includes(pattern) || pathname.includes(pattern)
        ) || titlePatterns.some(pattern => title.includes(pattern));
    }
    
    isCheckoutPage(url, pathname, title) {
        const checkoutPatterns = [
            '/odeme',
            '/checkout',
            '/payment',
            '/siparis',
            '/order'
        ];
        
        const titlePatterns = [
            'ödeme',
            'checkout',
            'payment',
            'sipariş',
            'order'
        ];
        
        return checkoutPatterns.some(pattern => 
            url.includes(pattern) || pathname.includes(pattern)
        ) || titlePatterns.some(pattern => title.includes(pattern));
    }
    
    analyzeDOMStructure() {
        // Her sayfa tipi için DOM elementlerini kontrol et
        for (const [pageType, config] of Object.entries(this.selectors)) {
            if (this.hasPageIndicators(config.indicators)) {
                return pageType;
            }
        }
        
        return 'other';
    }
    
    hasPageIndicators(indicators) {
        return indicators.some(selector => {
            try {
                return document.querySelector(selector) !== null;
            } catch (e) {
                return false;
            }
        });
    }
    
    findInsertionPoint(pageType) {
        if (!this.selectors[pageType]) {
            return this.findFallbackInsertionPoint();
        }
        
        const insertPoints = this.selectors[pageType].insertPoints;
        
        for (const selector of insertPoints) {
            try {
                const element = document.querySelector(selector);
                if (element) {
                    return {
                        element: element,
                        method: this.getInsertionMethod(pageType),
                        selector: selector
                    };
                }
            } catch (e) {
                continue;
            }
        }
        
        return this.findFallbackInsertionPoint();
    }
    
    getInsertionMethod(pageType) {
        switch (pageType) {
            case 'homepage':
                return 'prepend'; // İçeriğin başına ekle
            case 'category':
                return 'before'; // Element'in önüne ekle
            case 'product':
                return 'after'; // Element'in sonrasına ekle
            default:
                return 'prepend';
        }
    }
    
    findFallbackInsertionPoint() {
        const fallbackSelectors = [
            'main',
            '.content',
            '#content',
            '.container',
            '.main-content',
            'body'
        ];
        
        for (const selector of fallbackSelectors) {
            try {
                const element = document.querySelector(selector);
                if (element && element.tagName !== 'BODY') {
                    return {
                        element: element,
                        method: 'prepend',
                        selector: selector
                    };
                }
            } catch (e) {
                continue;
            }
        }
        
        // Son çare: body
        return {
            element: document.body,
            method: 'prepend',
            selector: 'body'
        };
    }
    
    insertElement(container, pageType) {
        const insertionPoint = this.findInsertionPoint(pageType);
        
        if (!insertionPoint) {
            document.body.insertBefore(container, document.body.firstChild);
            return;
        }
        
        const { element, method } = insertionPoint;
        
        switch (method) {
            case 'prepend':
                element.insertBefore(container, element.firstChild);
                break;
            case 'append':
                element.appendChild(container);
                break;
            case 'before':
                element.parentNode.insertBefore(container, element);
                break;
            case 'after':
                element.parentNode.insertBefore(container, element.nextSibling);
                break;
            default:
                element.insertBefore(container, element.firstChild);
        }
    }
    
    getPageInfo() {
        const pageType = this.analyzePageType();
        const insertionPoint = this.findInsertionPoint(pageType);
        
        return {
            pageType: pageType,
            url: window.location.href,
            pathname: window.location.pathname,
            title: document.title,
            insertionPoint: insertionPoint ? insertionPoint.selector : null,
            insertionMethod: insertionPoint ? insertionPoint.method : null
        };
    }
}
