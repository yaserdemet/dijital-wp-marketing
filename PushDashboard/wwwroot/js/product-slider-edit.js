// Product Slider Edit Page JavaScript

// Global variables
let currentSliderId = null;
let selectedEcommerceProducts = [];
let currentPage = 1;
let totalPages = 1;
let currentEditingProductId = null;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    initializeProductSliderEdit();
});

function initializeProductSliderEdit() {
    currentSliderId = document.getElementById('sliderId').value;

    // Initialize form handlers
    initializePreview();
    initializeButtonPreview();
    initializeSliderDesign();

    // Initialize drag and drop for products
    initializeDragAndDrop();

    // Update preview initially
    updatePreview();
    updateButtonPreview();

    console.log('Product Slider Edit initialized for slider:', currentSliderId);
}



// Slider Settings Form
function initializeSliderSettingsForm() {
    const form = document.getElementById('sliderSettingsForm');
    if (form) {
        form.addEventListener('submit', handleSliderSettingsUpdate);
    }
}

async function handleSliderSettingsUpdate(event) {
    event.preventDefault();

    // Get form values with proper defaults
    const autoPlayElement = document.getElementById('autoPlay');
    const autoPlayIntervalElement = document.getElementById('autoPlayInterval');
    const showArrowsElement = document.getElementById('showArrows');
    const showDotsElement = document.getElementById('showDots');
    const itemsPerViewElement = document.getElementById('itemsPerView');
    const itemsPerViewTabletElement = document.getElementById('itemsPerViewTablet');
    const itemsPerViewMobileElement = document.getElementById('itemsPerViewMobile');
    const showProductPriceElement = document.getElementById('showProductPrice');
    const showProductImageElement = document.getElementById('showProductImage');

    const data = {
        sliderId: parseInt(currentSliderId),
        autoPlay: autoPlayElement ? autoPlayElement.checked : true,
        autoPlayInterval: autoPlayIntervalElement ? parseInt(autoPlayIntervalElement.value) || 5000 : 5000,
        showArrows: showArrowsElement ? showArrowsElement.checked : true,
        showDots: showDotsElement ? showDotsElement.checked : true,
        itemsPerView: itemsPerViewElement ? parseInt(itemsPerViewElement.value) || 4 : 4,
        itemsPerViewTablet: itemsPerViewTabletElement ? parseInt(itemsPerViewTabletElement.value) || 2 : 2,
        itemsPerViewMobile: itemsPerViewMobileElement ? parseInt(itemsPerViewMobileElement.value) || 1 : 1,
        showProductPrice: showProductPriceElement ? showProductPriceElement.checked : true,
        showProductImage: showProductImageElement ? showProductImageElement.checked : true,
        theme: 'default',
        primaryColor: '#3B82F6',
        secondaryColor: '#1F2937',
        backgroundColor: '#FFFFFF',
        textColor: '#1F2937',
        enableAnimations: true,
        transitionDuration: 300,
        animationType: 'slide',
        showProductDescription: false,
        imageAspectRatio: 'square'
    };

    try {
        const response = await fetch('/ProductSlider/UpdateSliderSettings', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({
                ...data,
                __RequestVerificationToken: getAntiForgeryToken()
            })
        });

        const result = await response.json();

        if (result.success) {
            showNotification('Slider ayarları başarıyla güncellendi!', 'success');
        } else {
            showNotification(result.message || 'Ayarlar güncellenirken bir hata oluştu.', 'error');
        }
    } catch (error) {
        console.error('Error updating slider settings:', error);
        showNotification('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
    }
}

// Preview Functions
function initializePreview() {
    // Set initial preview type based on current display type
    const displayType = document.getElementById('displayType').value || 'slider';
    changePreviewType(displayType);

    // Listen for display type changes
    const displayTypeSelect = document.getElementById('displayType');
    if (displayTypeSelect) {
        displayTypeSelect.addEventListener('change', function() {
            changePreviewType(this.value);
            updatePreview();
        });
    }

    // Initial preview update
    updatePreview();
}

function changePreviewType(type) {
    // Update button states
    document.querySelectorAll('.preview-type-btn').forEach(btn => {
        if (btn.dataset.type === type) {
            btn.classList.add('bg-blue-600', 'text-white', 'border-blue-600');
            btn.classList.remove('bg-white', 'text-gray-700', 'border-gray-300');
        } else {
            btn.classList.remove('bg-blue-600', 'text-white', 'border-blue-600');
            btn.classList.add('bg-white', 'text-gray-700', 'border-gray-300');
        }
    });

    // Update display type select if different
    const displayTypeSelect = document.getElementById('displayType');
    if (displayTypeSelect && displayTypeSelect.value !== type) {
        displayTypeSelect.value = type;
    }

    // Update preview
    updatePreview();
}

function updatePreview() {
    const previewContent = document.getElementById('previewContent');
    const displayType = document.getElementById('displayType')?.value || 'slider';

    // Get current products from the page
    let products = getCurrentProducts();

    // If no products, use dummy data for preview
    if (products.length === 0) {
        products = getDummyProducts();
    }

    // Generate preview based on display type
    switch (displayType) {
        case 'slider':
            generateSliderPreview(products);
            break;
        case 'tabs':
            generateTabsPreview(products);
            break;
        case 'grid':
            generateGridPreview(products);
            break;
        default:
            generateSliderPreview(products);
    }
}

// Dummy products for preview when no real products are added
function getDummyProducts() {
    return [
        {
            name: 'Akıllı Telefon',
            image: 'https://upload.wikimedia.org/wikipedia/commons/a/ac/No_image_available.svg',
            price: '2.999 TRY'
        },
        {
            name: 'Laptop Bilgisayar',
            image: 'https://upload.wikimedia.org/wikipedia/commons/a/ac/No_image_available.svg',
            price: '8.499 TRY'
        },
        {
            name: 'Bluetooth Kulaklık',
            image: 'https://upload.wikimedia.org/wikipedia/commons/a/ac/No_image_available.svg',
            price: '299 TRY'
        },
        {
            name: 'Akıllı Saat',
            image: 'https://upload.wikimedia.org/wikipedia/commons/a/ac/No_image_available.svg',
            price: '1.299 TRY'
        },
        {
            name: 'Tablet',
            image: 'https://upload.wikimedia.org/wikipedia/commons/a/ac/No_image_available.svg',
            price: '3.499 TRY'
        },
        {
            name: 'Kamera',
            image: 'https://upload.wikimedia.org/wikipedia/commons/a/ac/No_image_available.svg',
            price: '4.999 TRY'
        }
    ];
}

function getCurrentProducts() {
    const productElements = document.querySelectorAll('.product-item');
    const products = [];

    productElements.forEach(element => {
        const img = element.querySelector('img');
        const title = element.querySelector('h4');
        const price = element.querySelector('.text-green-600');

        products.push({
            name: title?.textContent || 'Ürün',
            image: img?.src || 'https://upload.wikimedia.org/wikipedia/commons/a/ac/No_image_available.svg',
            price: price?.textContent || '0 TRY'
        });
    });

    return products.slice(0, 6); // Limit to 6 products for preview
}

function generateSliderPreview(products) {
    const previewContent = document.getElementById('previewContent');
    if (!previewContent) return;

    const settings = getSliderDesignSettings();

    // Determine how many products to show based on itemsPerView
    const itemsToShow = Math.min(parseInt(settings.itemsPerView) || 4, products.length);
    const productsToShow = products.slice(0, itemsToShow);

    previewContent.innerHTML = `
        <div class="pushonica-product-slider">
            ${settings.showArrows ? '<div class="slider-arrows">← →</div>' : ''}
            ${productsToShow.map(product => `
                <div class="product-item">
                    ${settings.showProductImage ? `
                        <div class="product-image">
                            <img src="${product.image || '/images/no-image.svg'}" alt="${product.name}" style="aspect-ratio: ${getAspectRatio(settings.imageAspectRatio)}">
                        </div>
                    ` : ''}
                    <div class="product-title">${product.name}</div>
                    ${settings.showProductDescription ? `
                        <div class="product-description">Ürün açıklaması burada görünecek</div>
                    ` : ''}
                    ${settings.showProductPrice ? `
                        <div class="product-price">${product.price}</div>
                    ` : ''}
                    <div class="product-button">Ürünü İncele</div>
                </div>
            `).join('')}
            ${settings.showDots ? '<div class="slider-dots">● ● ●</div>' : ''}
            ${products.length > itemsToShow ? `
                <div class="more-products">+${products.length - itemsToShow} ürün daha...</div>
            ` : ''}
        </div>
    `;

    // Apply enhanced CSS with all settings
    applyEnhancedCSS();
}

// Helper function for aspect ratio
function getAspectRatio(ratio) {
    switch (ratio) {
        case 'landscape': return '16/9';
        case 'portrait': return '3/4';
        case 'square':
        default: return '1/1';
    }
}

// Enhanced CSS for better color coverage using correct IDs
function applyEnhancedCSS() {
    // Remove existing enhanced styles
    const existingStyle = document.getElementById('enhanced-preview-styles');
    if (existingStyle) {
        existingStyle.remove();
    }

    // Get current settings using all backend-supported IDs
    const settings = getSliderDesignSettings();
    const primaryColor = settings.primaryColor || '#3B82F6';
    const secondaryColor = settings.secondaryColor || '#1F2937';
    const backgroundColor = settings.backgroundColor || '#FFFFFF';
    const textColor = settings.textColor || '#1F2937';

    // Backend-supported animation settings
    const transitionDuration = (settings.transitionDuration || '300') + 'ms';
    const enableAnimations = settings.enableAnimations !== false;

    // Backend-supported display settings
    const showArrows = settings.showArrows !== false;
    const showDots = settings.showDots !== false;
    const itemsPerView = parseInt(settings.itemsPerView) || 4;

    // Backend-supported layout settings
    const showProductImage = settings.showProductImage !== false;
    const showProductPrice = settings.showProductPrice !== false;
    const showProductDescription = settings.showProductDescription || false;

    // Create enhanced styles with all settings
    const style = document.createElement('style');
    style.id = 'enhanced-preview-styles';
    style.textContent = `
        .slider-preview .pushonica-product-slider {
            background: ${backgroundColor} !important;
            border-radius: 8px !important;
            padding: 20px !important;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1) !important;
            border: 2px solid ${primaryColor} !important;
            transition: all ${transitionDuration} !important;
        }

        .slider-preview .product-item {
            background: ${backgroundColor} !important;
            border: 1px solid ${primaryColor} !important;
            border-radius: 6px !important;
            padding: 15px !important;
            margin-bottom: 10px !important;
            transition: all ${transitionDuration} !important;
        }

        .slider-preview .product-item:hover {
            background: ${primaryColor}15 !important;
            ${enableAnimations ? `
                transform: translateY(-2px) !important;
                box-shadow: 0 4px 12px rgba(0,0,0,0.15) !important;
            ` : ''}
        }

        .slider-preview .product-title {
            color: ${textColor} !important;
            font-size: 16px !important;
            font-weight: 600 !important;
            margin-bottom: 8px !important;
            transition: all ${transitionDuration} !important;
        }

        .slider-preview .product-price {
            color: ${primaryColor} !important;
            font-size: 18px !important;
            font-weight: bold !important;
            margin-bottom: 10px !important;
            transition: all ${transitionDuration} !important;
        }

        .slider-preview .product-button {
            background: ${primaryColor} !important;
            color: white !important;
            padding: 8px 16px !important;
            border-radius: 4px !important;
            font-size: 14px !important;
            font-weight: 500 !important;
            text-align: center !important;
            cursor: pointer !important;
            transition: all ${transitionDuration} !important;
        }

        .slider-preview .product-button:hover {
            background: ${secondaryColor} !important;
            ${enableAnimations ? `
                transform: translateY(-1px) !important;
                box-shadow: 0 4px 12px rgba(0,0,0,0.2) !important;
            ` : ''}
        }

        .slider-preview .more-products {
            color: ${textColor} !important;
            text-align: center !important;
            font-size: 14px !important;
            margin-top: 15px !important;
            padding: 10px !important;
            background: ${primaryColor}10 !important;
            border-radius: 4px !important;
            transition: all ${transitionDuration} !important;
        }

        /* New elements for backend settings */
        .slider-preview .slider-arrows {
            text-align: center !important;
            color: ${primaryColor} !important;
            font-size: 20px !important;
            margin-bottom: 10px !important;
            ${!showArrows ? 'display: none !important;' : ''}
        }

        .slider-preview .slider-dots {
            text-align: center !important;
            color: ${primaryColor} !important;
            margin-top: 15px !important;
            font-size: 12px !important;
            ${!showDots ? 'display: none !important;' : ''}
        }

        .slider-preview .product-image {
            margin-bottom: 10px !important;
            ${!showProductImage ? 'display: none !important;' : ''}
        }

        .slider-preview .product-image img {
            width: 100% !important;
            height: 120px !important;
            object-fit: cover !important;
            border-radius: 6px !important;
        }

        .slider-preview .product-description {
            color: #666 !important;
            font-size: 12px !important;
            margin-bottom: 8px !important;
            line-height: 1.3 !important;
            ${!showProductDescription ? 'display: none !important;' : ''}
        }

        .slider-preview .product-price {
            ${!showProductPrice ? 'display: none !important;' : ''}
        }

        /* Alt alta sıralama - her zaman dikey liste */
        .slider-preview .pushonica-product-slider {
            display: flex !important;
            flex-direction: column !important;
            gap: 10px !important;
        }

        .slider-preview .product-item {
            width: 100% !important;
        }
    `;

    // Apply theme-specific styles
    applyThemeStyles(style, settings);

    // Apply custom CSS if provided
    if (settings.customCSS && settings.customCSS.trim()) {
        style.textContent += `\n/* Custom CSS */\n${settings.customCSS}`;
    }

    document.head.appendChild(style);
}



function applyThemeStyles(style, settings) {
    const theme = settings.theme || 'default';
    let themeCSS = '';

    switch (theme) {
        case 'modern':
            themeCSS = `
                .slider-preview .pushonica-product-slider {
                    background: linear-gradient(135deg, ${settings.backgroundColor} 0%, ${settings.primaryColor}10 100%) !important;
                    border: none !important;
                    border-radius: 12px !important;
                }
                .slider-preview .product-item {
                    border-radius: 10px !important;
                    backdrop-filter: blur(10px) !important;
                }
            `;
            break;
        case 'minimal':
            themeCSS = `
                .slider-preview .pushonica-product-slider {
                    border: 1px solid #e5e5e5 !important;
                    box-shadow: none !important;
                }
                .slider-preview .product-item {
                    border: none !important;
                    border-bottom: 1px solid #f0f0f0 !important;
                    border-radius: 0 !important;
                }
            `;
            break;
        case 'colorful':
            themeCSS = `
                .slider-preview .pushonica-product-slider {
                    background: linear-gradient(45deg, ${settings.primaryColor}20, ${settings.secondaryColor}20) !important;
                    border: 2px solid ${settings.primaryColor} !important;
                }
                .slider-preview .product-item {
                    background: linear-gradient(135deg, ${settings.backgroundColor} 0%, ${settings.primaryColor}05 100%) !important;
                }
            `;
            break;
    }

    if (themeCSS) {
        style.textContent += `\n/* Theme: ${theme} */\n${themeCSS}`;
    }
}

function generateTabsPreview(products) {
    const previewContent = document.getElementById('previewContent');
    if (!previewContent) return

    const settings = getSliderDesignSettings();

    const itemsToShow = Math.min(parseInt(settings.itemsPerView) || 3, products.length);
    const productsToShow = products.slice(0, itemsToShow);

    previewContent.innerHTML = `
        <div class="pushonica-product-slider">
            ${productsToShow.map(product => `
                <div class="product-item">
                    ${settings.showProductImage ? `
                        <div class="product-image">
                            <img src="${product.image || '/images/no-image.svg'}" alt="${product.name}" style="aspect-ratio: ${getAspectRatio(settings.imageAspectRatio)}">
                        </div>
                    ` : ''}
                    <div class="product-title">${product.name}</div>
                    ${settings.showProductDescription ? `
                        <div class="product-description">Ürün açıklaması burada görünecek</div>
                    ` : ''}
                    ${settings.showProductPrice ? `
                        <div class="product-price">${product.price}</div>
                    ` : ''}
                    <div class="product-button">Görüntüle</div>
                </div>
            `).join('')}
            ${products.length > itemsToShow ? `
                <div class="more-products">+${products.length - itemsToShow} ürün daha...</div>
            ` : ''}
        </div>
    `;

    applyEnhancedCSS();
}

function generateGridPreview(products) {
    const previewContent = document.getElementById('previewContent');
    if (!previewContent) return

    const settings = getSliderDesignSettings();

    const itemsToShow = Math.min(parseInt(settings.itemsPerView) || 3, products.length);
    const productsToShow = products.slice(0, itemsToShow);

    previewContent.innerHTML = `
        <div class="pushonica-product-slider">
            ${productsToShow.map(product => `
                <div class="product-item">
                    ${settings.showProductImage ? `
                        <div class="product-image">
                            <img src="${product.image || '/images/no-image.svg'}" alt="${product.name}" style="aspect-ratio: ${getAspectRatio(settings.imageAspectRatio)}">
                        </div>
                    ` : ''}
                    <div class="product-title">${product.name}</div>
                    ${settings.showProductDescription ? `
                        <div class="product-description">Ürün açıklaması burada görünecek</div>
                    ` : ''}
                    ${settings.showProductPrice ? `
                        <div class="product-price">${product.price}</div>
                    ` : ''}
                    <div class="product-button">Detay</div>
                </div>
            `).join('')}
            ${products.length > itemsToShow ? `
                <div class="more-products">+${products.length - itemsToShow} ürün daha...</div>
            ` : ''}
        </div>
    `;

    applyEnhancedCSS();
}

// Button Preview Functions
function initializeButtonPreview() {
    // Set up event listeners for all button design controls
    const buttonControls = [
        'buttonText', 'buttonSize', 'buttonPosition', 'buttonBgColor',
        'buttonTextColor', 'buttonHoverColor', 'buttonBorderRadius',
        'buttonIcon', 'buttonShadow', 'buttonPulse'
    ];

    buttonControls.forEach(controlId => {
        const element = document.getElementById(controlId);
        if (element) {
            if (element.type === 'checkbox') {
                element.addEventListener('change', updateButtonPreview);
            } else {
                element.addEventListener('input', updateButtonPreview);
                element.addEventListener('change', updateButtonPreview);
            }
        }
    });

    // Initial preview update
    updateButtonPreview();
}

function updateButtonPreview() {
    const button = document.getElementById('previewButton');
    if (!button) return;

    // Get current settings
    const text = document.getElementById('buttonText').value || 'Ürünleri Gör';
    const size = document.getElementById('buttonSize').value || 'medium';
    const position = document.getElementById('buttonPosition').value || 'middle-right';
    const bgColor = document.getElementById('buttonBgColor').value || '#3B82F6';
    const textColor = document.getElementById('buttonTextColor').value || '#FFFFFF';
    const hoverColor = document.getElementById('buttonHoverColor').value || '#2563EB';
    const icon = document.getElementById('buttonIcon').value || 'eye';
    const hasShadow = document.getElementById('buttonShadow').checked;
    const hasPulse = document.getElementById('buttonPulse').checked;

    // Update button content
    const iconSvg = getButtonIcon(icon);
    button.innerHTML = `
        <div class="flex items-center space-x-2">
            ${iconSvg}
            <span>${text}</span>
        </div>
    `;

    // Apply size classes
    const sizeClasses = getButtonSizeClasses(size);
    button.className = `absolute transition-all duration-200 transform hover:scale-105 ${sizeClasses}`;

    // Apply position styles
    const positionStyles = getButtonPositionStyles(position);

    // Apply colors and styles
    const shadowClass = hasShadow ? 'shadow-lg' : '';
    const pulseClass = hasPulse ? 'animate-pulse' : '';

    button.className += ` ${shadowClass} ${pulseClass}`;

    // Apply inline styles
    button.style.cssText = `
        ${positionStyles}
        background-color: ${bgColor};
        color: ${textColor};
        border-radius: 10px;
        border: none;
        cursor: pointer;
    `;

    // Create hover effect with CSS
    updateButtonHoverEffect(button, hoverColor);
}

function getButtonIcon(iconType) {
    const iconClasses = "w-4 h-4";

    switch (iconType) {
        case 'eye':
            return `<svg class="${iconClasses}" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path>
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"></path>
                    </svg>`;
        case 'cart':
            return `<svg class="${iconClasses}" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 3h2l.4 2M7 13h10l4-8H5.4m0 0L7 13m0 0l-2.5 5M7 13l2.5 5m6-5v6a2 2 0 01-2 2H9a2 2 0 01-2-2v-6m8 0V9a2 2 0 00-2-2H9a2 2 0 00-2 2v4.01"></path>
                    </svg>`;
        case 'arrow':
            return `<svg class="${iconClasses}" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6"></path>
                    </svg>`;
        case 'none':
        default:
            return '';
    }
}

function getButtonSizeClasses(size) {
    switch (size) {
        case 'small':
            return 'px-3 py-1 text-sm';
        case 'large':
            return 'px-6 py-3 text-lg';
        case 'medium':
        default:
            return 'px-4 py-2 text-base';
    }
}

function getButtonPositionStyles(position) {
    switch (position) {
        case 'top-left':
            return 'top: 10px; left: 10px; transform: none;';
        case 'top-right':
            return 'top: 10px; right: 10px; transform: none;';
        case 'middle-left':
            return 'top: 50%; left: 10px; transform: translateY(-50%);';
        case 'middle-right':
            return 'top: 50%; right: 10px; transform: translateY(-50%);';
        case 'bottom-left':
            return 'bottom: 10px; left: 10px; transform: none;';
        case 'bottom-right':
            return 'bottom: 10px; right: 10px; transform: none;';
        case 'center':
            return 'top: 50%; left: 50%; transform: translate(-50%, -50%);';
        default:
            return 'top: 50%; right: 10px; transform: translateY(-50%);';
    }
}

function updateButtonHoverEffect(button, hoverColor) {
    // Remove existing hover style if any
    const existingStyle = document.getElementById('buttonHoverStyle');
    if (existingStyle) {
        existingStyle.remove();
    }

    // Create new hover style
    const style = document.createElement('style');
    style.id = 'buttonHoverStyle';
    style.textContent = `
        #previewButton:hover {
            background-color: ${hoverColor} !important;
        }
    `;
    document.head.appendChild(style);
}

function simulateButtonClick() {
    const button = document.getElementById('previewButton');
    if (!button) return;

    // Add click animation
    button.style.transform += ' scale(0.95)';

    setTimeout(() => {
        // Reset transform after animation
        const currentTransform = button.style.transform.replace(' scale(0.95)', '');
        button.style.transform = currentTransform;
    }, 150);

    // Show notification
    showNotification('Buton tıklandı! 🎉', 'success');
}

function updateBorderRadiusValue(value) {
    document.getElementById('borderRadiusValue').textContent = value;
}



function updateTransitionDurationValue(value) {
    const element = document.getElementById('transitionDurationValue');
    if (element) {
        element.textContent = value;
    }
    updateSliderDesign();
}

// Slider Design Functions
function initializeSliderDesign() {
    // Set up event listeners for all backend-supported slider controls
    const sliderDesignControls = [
        // Backend-supported color settings
        'sliderPrimaryColor', 'sliderSecondaryColor', 'sliderBackgroundColor', 'textColor',

        // Backend-supported animation settings
        'sliderTransitionDuration', 'animationType', 'enableAnimations',

        // Backend-supported theme settings
        'theme', 'customCSS',

        // Backend-supported display settings
        'autoPlay', 'autoPlayInterval', 'showArrows', 'showDots',
        'itemsPerView', 'itemsPerViewTablet', 'itemsPerViewMobile',

        // Backend-supported layout settings
        'showProductPrice', 'showProductImage', 'showProductDescription', 'imageAspectRatio'
    ];

    sliderDesignControls.forEach(controlId => {
        const element = document.getElementById(controlId);
        if (element) {
            if (element.type === 'checkbox') {
                element.addEventListener('change', updateSliderDesign);
            } else {
                element.addEventListener('input', updateSliderDesign);
                element.addEventListener('change', updateSliderDesign);
            }
        }
    });

    // Initial design update
    updateSliderDesign();
}

function toggleSliderDesignPanel() {
    const panel = document.getElementById('sliderDesignPanel');
    const icon = document.getElementById('designPanelIcon');

    if (panel.classList.contains('hidden')) {
        panel.classList.remove('hidden');
        icon.style.transform = 'rotate(180deg)';
    } else {
        panel.classList.add('hidden');
        icon.style.transform = 'rotate(0deg)';
    }
}

function updateSliderDesign() {
    // Update preview with new design settings
    updatePreview();
}

function applyCSSToPreview(settings) {
    // Remove existing dynamic styles
    const existingStyle = document.getElementById('dynamic-slider-styles');
    if (existingStyle) {
        existingStyle.remove();
    }

    // Create new style element
    const style = document.createElement('style');
    style.id = 'dynamic-slider-styles';

    // Generate CSS based on settings
    let css = `
        .slider-preview .pushonica-product-slider {
            background-color: ${settings.backgroundColor || '#ffffff'} !important;
        }

        .slider-preview .item-content {
            background-color: ${settings.backgroundColor || '#ffffff'} !important;
            color: ${settings.textColor || '#333333'} !important;
        }

        .slider-preview .item-title {
            color: ${settings.textColor || '#333333'} !important;
        }

        .slider-preview .item-description {
            color: ${settings.textColor || '#666666'} !important;
        }

        .slider-preview .item-price {
            color: ${settings.priceColor || '#e74c3c'} !important;
        }

        .slider-preview .item-link {
            background: linear-gradient(135deg, ${settings.primaryColor || '#667eea'} 0%, ${settings.secondaryColor || '#764ba2'} 100%) !important;
            color: white !important;
        }

        .slider-preview .slider-arrow {
            background-color: ${settings.primaryColor || '#667eea'} !important;
        }

        .slider-preview .slider-dot.active {
            background-color: ${settings.primaryColor || '#667eea'} !important;
        }
    `;

    style.textContent = css;
    document.head.appendChild(style);

    console.log('Applied CSS to preview:', settings);
}

function getSliderDesignSettings() {
    return {
        // Backend-supported color settings
        primaryColor: document.getElementById('sliderPrimaryColor')?.value || '#667eea',
        secondaryColor: document.getElementById('sliderSecondaryColor')?.value || '#764ba2',
        backgroundColor: document.getElementById('sliderBackgroundColor')?.value || '#FFFFFF',
        textColor: document.getElementById('textColor')?.value || '#1F2937',

        // Backend-supported animation settings
        transitionDuration: document.getElementById('sliderTransitionDuration')?.value || '300',
        animationType: document.getElementById('animationType')?.value || 'slide',
        enableAnimations: document.getElementById('enableAnimations')?.checked !== false,

        // Backend-supported theme settings
        theme: document.getElementById('theme')?.value || 'default',
        customCSS: document.getElementById('customCSS')?.value || '',

        // Backend-supported display settings
        autoPlay: document.getElementById('autoPlay')?.checked !== false,
        autoPlayInterval: document.getElementById('autoPlayInterval')?.value || '5000',
        showArrows: document.getElementById('showArrows')?.checked !== false,
        showDots: document.getElementById('showDots')?.checked !== false,
        itemsPerView: document.getElementById('itemsPerView')?.value || '4',
        itemsPerViewTablet: document.getElementById('itemsPerViewTablet')?.value || '2',
        itemsPerViewMobile: document.getElementById('itemsPerViewMobile')?.value || '1',

        // Backend-supported layout settings
        showProductPrice: document.getElementById('showProductPrice')?.checked !== false,
        showProductImage: document.getElementById('showProductImage')?.checked !== false,
        showProductDescription: document.getElementById('showProductDescription')?.checked || false,
        imageAspectRatio: document.getElementById('imageAspectRatio')?.value || 'square'
    };
}



// Value update functions for sliders
function updatePriceFontSizeValue(value) {
    document.getElementById('priceFontSizeValue').textContent = value;
}

function updateCardBorderRadiusValue(value) {
    document.getElementById('cardBorderRadiusValue').textContent = value;
}

function updateBorderWidthValue(value) {
    document.getElementById('borderWidthValue').textContent = value;
}

function updateItemSpacingValue(value) {
    document.getElementById('itemSpacingValue').textContent = value;
}

function updatePaddingValue(value) {
    document.getElementById('paddingValue').textContent = value;
}

function updateMarginValue(value) {
    document.getElementById('marginValue').textContent = value;
}

function updateTransitionDurationValue(value) {
    document.getElementById('transitionDurationValue').textContent = value;
}

// Slider Design Presets
function applySliderPreset(presetName) {
    const presets = {
        modern: {
            bgColor: '#FFFFFF',
            textColor: '#1F2937',
            priceColor: '#059669',
            accentColor: '#3B82F6',
            hoverColor: '#F8FAFC',
            borderColor: '#E2E8F0',
            fontSize: 'medium',
            priceFontSize: '16',
            boldText: true,
            cardBorderRadius: '12',
            shadow: 'md',
            borderWidth: '0',
            itemSpacing: '16',
            padding: '16',
            margin: '8',
            transitionDuration: '300',
            hoverAnimation: true,
            scaleEffect: true
        },
        minimal: {
            bgColor: '#FAFAFA',
            textColor: '#374151',
            priceColor: '#111827',
            accentColor: '#6B7280',
            hoverColor: '#F3F4F6',
            borderColor: '#D1D5DB',
            fontSize: 'small',
            priceFontSize: '13',
            boldText: false,
            cardBorderRadius: '4',
            shadow: 'none',
            borderWidth: '1',
            itemSpacing: '8',
            padding: '12',
            margin: '4',
            transitionDuration: '150',
            hoverAnimation: false,
            scaleEffect: false
        },
        colorful: {
            bgColor: '#FEF3C7',
            textColor: '#92400E',
            priceColor: '#DC2626',
            accentColor: '#7C3AED',
            hoverColor: '#FDE68A',
            borderColor: '#F59E0B',
            fontSize: 'large',
            priceFontSize: '18',
            boldText: true,
            cardBorderRadius: '16',
            shadow: 'lg',
            borderWidth: '2',
            itemSpacing: '20',
            padding: '20',
            margin: '12',
            transitionDuration: '400',
            hoverAnimation: true,
            scaleEffect: true
        }
    };

    const preset = presets[presetName];
    if (!preset) return;

    // Apply preset values to form controls
    Object.keys(preset).forEach(key => {
        const elementId = 'slider' + key.charAt(0).toUpperCase() + key.slice(1);
        const element = document.getElementById(elementId);

        if (element) {
            if (element.type === 'checkbox') {
                element.checked = preset[key];
            } else {
                element.value = preset[key];
            }

            // Update value displays for range inputs
            if (element.type === 'range') {
                const valueDisplayId = key + 'Value';
                const valueDisplay = document.getElementById(valueDisplayId);
                if (valueDisplay) {
                    valueDisplay.textContent = preset[key];
                }
            }
        }
    });

    // Update preview
    updateSliderDesign();
    showNotification(`${presetName.charAt(0).toUpperCase() + presetName.slice(1)} teması uygulandı!`, 'success');
}

function resetSliderDesign() {
    // Reset to default values
    // Reset only existing elements
    const sliderBgColor = document.getElementById('sliderBgColor');
    const sliderTextColor = document.getElementById('sliderTextColor');
    const sliderPriceColor = document.getElementById('sliderPriceColor');
    const sliderAccentColor = document.getElementById('sliderAccentColor');
    const sliderFontSize = document.getElementById('sliderFontSize');
    const sliderPriceFontSize = document.getElementById('sliderPriceFontSize');
    const sliderBoldText = document.getElementById('sliderBoldText');
    const sliderShadow = document.getElementById('sliderShadow');
    const sliderTransitionDuration = document.getElementById('sliderTransitionDuration');
    const sliderHoverAnimation = document.getElementById('sliderHoverAnimation');
    const sliderScaleEffect = document.getElementById('sliderScaleEffect');

    if (sliderBgColor) sliderBgColor.value = '#FFFFFF';
    if (sliderTextColor) sliderTextColor.value = '#1F2937';
    if (sliderPriceColor) sliderPriceColor.value = '#059669';
    if (sliderAccentColor) sliderAccentColor.value = '#3B82F6';
    if (sliderFontSize) sliderFontSize.value = 'medium';
    if (sliderPriceFontSize) sliderPriceFontSize.value = '14';
    if (sliderBoldText) sliderBoldText.checked = false;
    if (sliderShadow) sliderShadow.value = 'sm';
    if (sliderTransitionDuration) sliderTransitionDuration.value = '200';
    if (sliderHoverAnimation) sliderHoverAnimation.checked = true;
    if (sliderScaleEffect) sliderScaleEffect.checked = false;

    // Update value displays (only if elements exist)
    const priceFontSizeValue = document.getElementById('priceFontSizeValue');
    const transitionDurationValue = document.getElementById('transitionDurationValue');

    if (priceFontSizeValue) priceFontSizeValue.textContent = '14';
    if (transitionDurationValue) transitionDurationValue.textContent = '200';

    // Update preview
    updateSliderDesign();
    showNotification('Tasarım ayarları varsayılan değerlere sıfırlandı.', 'success');
}

// Product Management Functions
async function toggleProductStatus(itemId) {
    showConfirmationDialog('Durum Değiştir', 'Ürün durumunu değiştirmek istediğinizden emin misiniz?', 'Evet, Değiştir', 'İptal', async () => {
        try {
            const formData = new FormData();
            formData.append('id', itemId);
            formData.append('__RequestVerificationToken', getAntiForgeryToken());

            const response = await fetch('/ProductSlider/ToggleSliderItemStatus', {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                showNotification(result.message, 'success');
                window.location.reload();
            } else {
                showNotification(result.message || 'Durum değiştirilirken bir hata oluştu.', 'error');
            }
        } catch (error) {
            console.error('Error toggling product status:', error);
            showNotification('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
        }
    });
}

async function deleteProduct(itemId) {
    showConfirmationDialog('Sil', 'Bu ürünü silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.', 'Sil', 'İptal', async () => {
        try {
            const formData = new FormData();
            formData.append('id', itemId);
            formData.append('__RequestVerificationToken', getAntiForgeryToken());

            const response = await fetch('/ProductSlider/DeleteSliderItem', {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                showNotification(result.message, 'success');
                window.location.reload();
            } else {
                showNotification(result.message || 'Ürün silinirken bir hata oluştu.', 'error');
            }
        } catch (error) {
            console.error('Error deleting product:', error);
            showNotification('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
        }
    });
}

async function syncWithTicimax() {
    if (!currentSliderId) return;
    const btn = document.getElementById('syncTicimaxBtn');
    if (btn) { btn.disabled = true; btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Eşitleniyor...'; }
    try {
        const formData = new FormData();
        formData.append('id', currentSliderId);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());
        const response = await fetch('/ProductSlider/SyncWithTicimax', {
            method: 'POST',
            body: formData
        });
        const result = await response.json();
        if (result.success) {
            showNotification(result.message || 'Eşitleme tamamlandı.', 'success');
            window.location.reload();
        } else {
            showNotification(result.message || 'Eşitleme başarısız.', 'error');
        }
    } catch (error) {
        console.error('SyncWithTicimax error:', error);
        showNotification('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
    } finally {
        if (btn) { btn.disabled = false; btn.innerHTML = '<i class="fas fa-sync-alt"></i> <span class="hidden sm:inline">Ticimaxla Eşitle</span>'; }
    }
}

function editProduct(itemId) {
    showNotification('Ürün düzenleme özelliği yakında eklenecek.', 'info');
}

// Drag and Drop Functions
function initializeDragAndDrop() {
    const productsList = document.getElementById('productsList');
    if (!productsList) return;

    // Remove old event listeners by cloning
    const productItems = productsList.querySelectorAll('.product-item');

    productItems.forEach(item => {
        // Make item draggable
        item.draggable = true;

        item.addEventListener('dragstart', handleDragStart);
        item.addEventListener('dragend', handleDragEnd);
        item.addEventListener('dragover', handleDragOver);
        item.addEventListener('dragenter', handleDragEnter);
        item.addEventListener('dragleave', handleDragLeave);
        item.addEventListener('drop', handleDrop);
    });
}

let draggedItem = null;

function handleDragStart(e) {
    draggedItem = this;
    this.style.opacity = '0.4';
    this.classList.add('dragging');
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', this.dataset.itemId);
}

function handleDragEnd(e) {
    this.style.opacity = '1';
    this.classList.remove('dragging');

    // Remove all drag-over classes
    document.querySelectorAll('.product-item').forEach(item => {
        item.classList.remove('drag-over');
    });

    draggedItem = null;
}

function handleDragOver(e) {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    return false;
}

function handleDragEnter(e) {
    this.classList.add('drag-over');
}

function handleDragLeave(e) {
    this.classList.remove('drag-over');
}

function handleDrop(e) {
    e.stopPropagation();
    e.preventDefault();

    if (draggedItem && draggedItem !== this) {
        const productsList = document.getElementById('productsList');
        const items = Array.from(productsList.querySelectorAll('.product-item'));
        const draggedIndex = items.indexOf(draggedItem);
        const targetIndex = items.indexOf(this);

        if (draggedIndex < targetIndex) {
            // Aşağı doğru sürükleme - hedefin sonrasına ekle
            this.parentNode.insertBefore(draggedItem, this.nextSibling);
        } else {
            // Yukarı doğru sürükleme - hedefin öncesine ekle
            this.parentNode.insertBefore(draggedItem, this);
        }

        updateProductOrder();
    }

    this.classList.remove('drag-over');
    return false;
}

async function updateProductOrder() {
    const productItems = document.querySelectorAll('.product-item');
    const itemIds = Array.from(productItems).map(item => parseInt(item.dataset.itemId));

    try {
        const response = await fetch('/ProductSlider/UpdateItemsOrder', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({
                sliderId: parseInt(currentSliderId),
                itemIds: itemIds,
                __RequestVerificationToken: getAntiForgeryToken()
            })
        });

        const result = await response.json();

        if (!result.success) {
            console.error('Sıra güncellenirken hata:', result.message);
        }
    } catch (error) {
        console.error('Error updating product order:', error);
    }
}

// DEPRECATED: Eski embed code sistemi - artık kullanılmıyor
function showEmbedCode(sliderId, sliderName) {
    showNotification('Bu özellik artık kullanılmıyor. Lütfen ana sayfadan "R2 Embed Kodu Al" butonunu kullanın.', 'warning');
    return;
}

function closeEmbedCodeModal() {
    document.getElementById('embedCodeModal').classList.add('hidden');
}

function copyEmbedCode() {
    const textarea = document.getElementById('embedCodeText') || document.getElementById('embedCodeDisplay');
    if (textarea) {
        textarea.select();
        textarea.setSelectionRange(0, 99999);

        try {
            document.execCommand('copy');
            showNotification('Embed kodu kopyalandı!', 'success');
        } catch (err) {
            console.error('Error copying embed code:', err);
            showNotification('Kopyalama başarısız. Lütfen manuel olarak kopyalayın.', 'error');
        }
    }
}

// Save Slider Settings Function
async function saveSliderSettings() {
    const saveBtn = document.getElementById('saveSliderBtn');
    const originalText = saveBtn.innerHTML;

    // Show loading state
    saveBtn.disabled = true;
    saveBtn.innerHTML = `
        <svg class="w-5 h-5 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
        </svg>
        Kaydediliyor...
    `;

    try {
        // Collect all settings including design settings
        const result = await saveAllSliderSettings();

        showNotification('Slider ayarları başarıyla kaydedildi! Yönlendiriliyorsunuz...', 'success');

        // Başarılı kaydetme sonrası ana sayfaya yönlendir
        setTimeout(() => {
            window.location.href = '/ProductSlider';
        }, 1000);
    } catch (error) {
        console.error('Save error:', error);
        showNotification('Kaydetme sırasında hata oluştu: ' + error.message, 'error');
        saveBtn.disabled = false;
        saveBtn.innerHTML = originalText;
    }
}

// Comprehensive function to save all slider settings including design
async function saveAllSliderSettings() {
    // Get basic slider settings
    const autoPlayElement = document.getElementById('autoPlay');
    const autoPlayIntervalElement = document.getElementById('autoPlayInterval');
    const showArrowsElement = document.getElementById('showArrows');
    const showDotsElement = document.getElementById('showDots');
    const itemsPerViewElement = document.getElementById('itemsPerView');
    const itemsPerViewTabletElement = document.getElementById('itemsPerViewTablet');
    const itemsPerViewMobileElement = document.getElementById('itemsPerViewMobile');
    const showProductPriceElement = document.getElementById('showProductPrice');
    const showProductImageElement = document.getElementById('showProductImage');
    const showProductDescriptionElement = document.getElementById('showProductDescription');
    const imageAspectRatioElement = document.getElementById('imageAspectRatio');

    // Get design settings - NEW COLOR INPUTS
    const sliderPrimaryColorElement = document.getElementById('sliderPrimaryColor');
    const sliderSecondaryColorElement = document.getElementById('sliderSecondaryColor');
    const sliderBackgroundColorElement = document.getElementById('sliderBackgroundColor');

    // Legacy color inputs (for backward compatibility)
    const sliderBgColorElement = document.getElementById('sliderBgColor');
    const sliderTextColorElement = document.getElementById('sliderTextColor');
    const sliderPriceColorElement = document.getElementById('sliderPriceColor');
    const sliderAccentColorElement = document.getElementById('sliderAccentColor');
    const sliderFontSizeElement = document.getElementById('sliderFontSize');
    const sliderPriceFontSizeElement = document.getElementById('sliderPriceFontSize');
    const sliderBoldTextElement = document.getElementById('sliderBoldText');
    const sliderShadowElement = document.getElementById('sliderShadow');
    const sliderTransitionDurationElement = document.getElementById('sliderTransitionDuration');
    const sliderHoverAnimationElement = document.getElementById('sliderHoverAnimation');
    const sliderScaleEffectElement = document.getElementById('sliderScaleEffect');
    const enableAnimationsElement = document.getElementById('enableAnimations');
    const animationTypeElement = document.getElementById('animationType');
    const themeElement = document.getElementById('theme');
    const customCSSElement = document.getElementById('customCSS');

    const data = {
        sliderId: parseInt(currentSliderId),
        // Basic settings
        autoPlay: autoPlayElement ? autoPlayElement.checked : true,
        autoPlayInterval: autoPlayIntervalElement ? parseInt(autoPlayIntervalElement.value) || 5000 : 5000,
        showArrows: showArrowsElement ? showArrowsElement.checked : true,
        showDots: showDotsElement ? showDotsElement.checked : true,
        itemsPerView: itemsPerViewElement ? parseInt(itemsPerViewElement.value) || 4 : 4,
        itemsPerViewTablet: itemsPerViewTabletElement ? parseInt(itemsPerViewTabletElement.value) || 2 : 2,
        itemsPerViewMobile: itemsPerViewMobileElement ? parseInt(itemsPerViewMobileElement.value) || 1 : 1,
        showProductPrice: showProductPriceElement ? showProductPriceElement.checked : true,
        showProductImage: showProductImageElement ? showProductImageElement.checked : true,
        showProductDescription: showProductDescriptionElement ? showProductDescriptionElement.checked : false,
        imageAspectRatio: imageAspectRatioElement ? imageAspectRatioElement.value || 'square' : 'square',

        // Design settings - NEW COLOR SYSTEM
        primaryColor: sliderPrimaryColorElement ? sliderPrimaryColorElement.value || '#667eea' : '#667eea',
        secondaryColor: sliderSecondaryColorElement ? sliderSecondaryColorElement.value || '#764ba2' : '#764ba2',
        backgroundColor: sliderBackgroundColorElement ? sliderBackgroundColorElement.value || '#FFFFFF' : '#FFFFFF',

        // Legacy color settings for backward compatibility
        textColor: sliderTextColorElement ? sliderTextColorElement.value || '#1F2937' : '#1F2937',

        // Animation settings
        enableAnimations: enableAnimationsElement ? enableAnimationsElement.checked : true,
        transitionDuration: sliderTransitionDurationElement ? parseInt(sliderTransitionDurationElement.value) || 300 : 300,
        animationType: animationTypeElement ? animationTypeElement.value || 'slide' : 'slide',

        // Theme
        theme: themeElement ? themeElement.value || 'default' : 'default',

        // Custom CSS
        customCSS: customCSSElement ? customCSSElement.value || null : null
    };

    const response = await fetch('/ProductSlider/UpdateSliderSettings', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify(data)
    });

    if (!response.ok) {
        throw new Error('Sunucu hatası: ' + response.status);
    }

    const result = await response.json();

    if (!result.success) {
        throw new Error(result.message || 'Ayarlar güncellenirken bir hata oluştu.');
    }
    return true;
}

// Utility Functions
function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}

// Close modals when clicking outside
document.addEventListener('click', function(event) {
    const embedModal = document.getElementById('embedCodeModal');

    if (event.target === embedModal) {
        closeEmbedCodeModal();
    }
});

// E-commerce Product Functions
function openEcommerceProductModal() {
    selectedEcommerceProducts = [];
    currentPage = 1;
    document.getElementById('ecommerceProductModal').classList.remove('hidden');
    loadEcommerceProducts();
}

function closeEcommerceProductModal() {
    document.getElementById('ecommerceProductModal').classList.add('hidden');
    selectedEcommerceProducts = [];
    updateSelectedProductsDisplay();
}

async function loadEcommerceProducts(page = 1) {
    const searchTerm = document.getElementById('productSearch').value;
    const categoryId = document.getElementById('categoryFilter').value;
    const brandId = document.getElementById('brandFilter').value;
    const inStock = document.getElementById('stockFilter').value;

    showEcommerceLoading(true);

    try {
        const params = new URLSearchParams({
            page: page,
            pageSize: 10,
            sliderId: currentSliderId,
            ...(searchTerm && { searchTerm }),
            ...(categoryId && { categoryId }),
            ...(brandId && { brandId }),
            ...(inStock && { inStock })
        });

        const response = await fetch(`/ProductSlider/GetEcommerceProducts?${params}`);
        const result = await response.json();

        if (result.success) {
            displayEcommerceProducts(result.data);
            displayPagination(result.pagination);
            currentPage = page;
        } else {
            showError(result.message || 'Ürünler yüklenirken hata oluştu.');
            showEcommerceEmpty(true);
            hidePagination();
        }
    } catch (error) {
        console.error('Error loading ecommerce products:', error);
        showError('Ürünler yüklenirken hata oluştu.');
        showEcommerceEmpty(true);
        hidePagination();
    } finally {
        showEcommerceLoading(false);
    }
}

const refreshBtnDefaultHTML = `
    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
    </svg>
    Yenile
`;

async function refreshProductCache() {
    const refreshBtn = document.getElementById('refreshCacheBtn');
    if (refreshBtn) {
        refreshBtn.disabled = true;
        refreshBtn.innerHTML = `
            <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            Yenileniyor...
        `;
    }

    try {
        await loadEcommerceProducts(1);
    } catch (error) {
        console.error('Error loading products:', error);
        showError('Ürün listesi yüklenirken hata oluştu.');
    } finally {
        if (refreshBtn) {
            refreshBtn.disabled = false;
            refreshBtn.innerHTML = refreshBtnDefaultHTML;
        }
    }
}

function displayEcommerceProducts(products) {
    const container = document.getElementById('ecommerceProductsList');

    if (!products || products.length === 0) {
        showEcommerceEmpty(true);
        return;
    }

    showEcommerceEmpty(false);

    container.innerHTML = products.map(product => {
        const isAlreadyAdded = product.isAlreadyAdded;
        const isSelected = selectedEcommerceProducts.includes(product.id);

        return `
        <div class="border rounded-lg p-4 hover:bg-gray-50 transition-colors ${isAlreadyAdded ? 'bg-green-50 border-green-200' : ''}">
            <div class="flex items-center gap-4">
                <div class="flex-shrink-0 relative">
                    <img src="${product.image || '/images/no-image.svg'}"
                         alt="${product.name}"
                         class="w-16 h-16 object-cover rounded-lg"
                         onerror="this.src='/images/no-image.svg'">
                    ${isAlreadyAdded ? `
                    <div class="absolute -top-1 -right-1 bg-green-500 rounded-full p-1">
                        <svg class="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"></path>
                        </svg>
                    </div>
                    ` : ''}
                </div>

                <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2">
                        <h4 class="font-medium text-gray-900 truncate">${product.name}</h4>
                        ${isAlreadyAdded ? '<span class="text-xs text-green-600 bg-green-100 px-2 py-0.5 rounded">Eklendi</span>' : ''}
                    </div>
                    <p class="text-sm text-gray-500">${product.code}</p>
                    <div class="flex items-center gap-2 mt-1">
                        <span class="text-lg font-semibold text-gray-900">
                            ${product.discountPrice ? product.discountPrice.toFixed(2) : product.price.toFixed(2)} ${product.currency}
                        </span>
                        ${product.discountPrice ? `<span class="text-sm text-gray-500 line-through">${product.price.toFixed(2)} ${product.currency}</span>` : ''}
                    </div>
                    <div class="flex items-center gap-4 mt-1">
                        ${product.categoryName ? `<span class="text-xs text-blue-600 bg-blue-100 px-2 py-1 rounded">${product.categoryName}</span>` : ''}
                        ${product.brandName ? `<span class="text-xs text-purple-600 bg-purple-100 px-2 py-1 rounded">${product.brandName}</span>` : ''}
                        <span class="text-xs ${product.inStock ? 'text-green-600 bg-green-100' : 'text-red-600 bg-red-100'} px-2 py-1 rounded">
                            ${product.inStock ? `Stokta (${product.stockQuantity})` : 'Stokta Yok'}
                        </span>
                        ${product.supplierName ? `<span class="text-xs text-orange-600 bg-orange-100 px-2 py-1 rounded">${product.supplierName}</span>` : ''}
                        ${product.tags && product.tags.length > 0 ? product.tags.map(tag => `<span class="text-xs text-teal-600 bg-teal-100 px-2 py-1 rounded">${tag}</span>`).join('') : ''}
                    </div>
                </div>

                <div class="flex-shrink-0">
                    ${isAlreadyAdded ? `
                    <div class="text-green-500" title="Bu ürün zaten slider'da">
                        <svg class="w-6 h-6" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"></path>
                        </svg>
                    </div>
                    ` : `
                    <input type="checkbox"
                           id="product_${product.id}"
                           ${isSelected ? 'checked' : ''}
                           onchange="toggleProductSelection('${product.id}', this.checked)"
                           class="w-5 h-5 text-blue-600 border-gray-300 rounded focus:ring-blue-500">
                    `}
                </div>
            </div>
        </div>
    `}).join('');
}

function toggleProductSelection(productId, isSelected) {
    if (isSelected) {
        if (!selectedEcommerceProducts.includes(productId)) {
            selectedEcommerceProducts.push(productId);
        }
    } else {
        selectedEcommerceProducts = selectedEcommerceProducts.filter(id => id !== productId);
    }

    updateSelectedProductsDisplay();
}

function updateSelectedProductsDisplay() {
    const section = document.getElementById('selectedProductsSection');
    const countSpan = document.getElementById('selectedCount');
    const addBtn = document.getElementById('addSelectedBtn');

    countSpan.textContent = selectedEcommerceProducts.length;
    addBtn.disabled = selectedEcommerceProducts.length === 0;

    if (selectedEcommerceProducts.length > 0) {
        section.classList.remove('hidden');
    } else {
        section.classList.add('hidden');
    }
}

function searchEcommerceProducts() {
    currentPage = 1;
    loadEcommerceProducts(1);
}

function showEcommerceLoading(show) {
    const loading = document.getElementById('ecommerceProductsLoading');
    const list = document.getElementById('ecommerceProductsList');

    if (show) {
        loading.classList.remove('hidden');
        list.classList.add('hidden');
    } else {
        loading.classList.add('hidden');
        list.classList.remove('hidden');
    }
}

function showEcommerceEmpty(show) {
    const empty = document.getElementById('ecommerceProductsEmpty');
    const list = document.getElementById('ecommerceProductsList');

    if (show) {
        empty.classList.remove('hidden');
        list.classList.add('hidden');
    } else {
        empty.classList.add('hidden');
        list.classList.remove('hidden');
    }
}

// Pagination Functions
function displayPagination(pagination) {
    let paginationContainer = document.getElementById('ecommercePagination');

    // Container yoksa oluştur
    if (!paginationContainer) {
        const productsList = document.getElementById('ecommerceProductsList');
        if (productsList && productsList.parentNode) {
            paginationContainer = document.createElement('div');
            paginationContainer.id = 'ecommercePagination';
            paginationContainer.className = 'flex items-center justify-center gap-2 mt-4 pt-4 border-t';
            productsList.parentNode.insertBefore(paginationContainer, productsList.nextSibling);
        }
    }

    if (!paginationContainer) return;

    const { currentPage, hasNextPage, hasPreviousPage } = pagination;

    paginationContainer.innerHTML = `
        <button onclick="goToPage(${currentPage - 1})"
                ${!hasPreviousPage ? 'disabled' : ''}
                class="px-3 py-1 rounded border ${hasPreviousPage ? 'bg-white hover:bg-gray-50 text-gray-700' : 'bg-gray-100 text-gray-400 cursor-not-allowed'}">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"></path>
            </svg>
        </button>
        <span class="px-4 py-1 text-sm text-gray-600">Sayfa ${currentPage}</span>
        <button onclick="goToPage(${currentPage + 1})"
                ${!hasNextPage ? 'disabled' : ''}
                class="px-3 py-1 rounded border ${hasNextPage ? 'bg-white hover:bg-gray-50 text-gray-700' : 'bg-gray-100 text-gray-400 cursor-not-allowed'}">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
            </svg>
        </button>
    `;
}

function hidePagination() {
    const paginationContainer = document.getElementById('ecommercePagination');
    if (paginationContainer) {
        paginationContainer.innerHTML = '';
    }
}

function goToPage(page) {
    if (page < 1) return;
    loadEcommerceProducts(page);
}

// Create HTML for a new product item
function createProductItemHTML(product, itemId) {
    const manualDescription = product.manualDescription || '';
    const hasImage = product.image && product.image !== '';
    const hasPrice = product.price && product.price > 0;

    return `
        <div class="border border-gray-200 rounded-lg p-4 product-item" data-item-id="${itemId}">
            <div class="flex items-center gap-4">
                <div class="drag-handle cursor-move text-gray-400">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 8h16M4 16h16"></path>
                    </svg>
                </div>
                ${hasImage ?
                    `<img src="${product.image}" alt="${product.name}" class="w-16 h-16 object-cover rounded" onerror="this.style.display='none'; this.nextElementSibling.style.display='flex';">
                     <div class="w-16 h-16 bg-gray-200 rounded flex items-center justify-center" style="display: none;">
                        <svg class="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"></path>
                        </svg>
                     </div>` :
                    `<div class="w-16 h-16 bg-gray-200 rounded flex items-center justify-center">
                        <svg class="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"></path>
                        </svg>
                     </div>`
                }
                <div class="flex-1">
                    <h4 class="font-medium text-gray-900">${product.name}</h4>
                    ${hasPrice ?
                        `<p class="text-sm text-blue-600 font-semibold">${product.discountPrice || product.price} ${product.currency || 'TRY'}</p>` :
                        ''
                    }
                    <p class="text-sm text-gray-500 truncate">${product.productUrl || product.url || ''}</p>
                </div>
                <div class="flex items-center gap-2">
                    <button type="button" onclick="toggleProductStatus(${itemId})" class="text-green-600 hover:text-green-800 p-1 rounded" title="Aktif / Pasif"><i class="fas fa-eye"></i></button>
                    <button onclick="deleteProduct(${itemId})" class="text-red-600 hover:text-red-800">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                        </svg>
                    </button>
                </div>
            </div>
        </div>
    `;
}

// Add product to DOM
function addProductToDOM(product, itemId) {
    let productsList = document.getElementById('productsList');

    // Check if we need to create the products list (if it was empty before)
    if (!productsList) {
        // Find the parent container and create the products list
        const parentContainer = document.querySelector('.bg-white.rounded-lg.shadow');
        if (parentContainer) {
            // Remove empty state if it exists
            const emptyState = parentContainer.querySelector('.p-12.text-center');
            if (emptyState) {
                emptyState.remove();
            }

            // Create products list container
            const productsContainer = document.createElement('div');
            productsContainer.className = 'p-6';
            productsContainer.innerHTML = '<div id="productsList" class="space-y-4"></div>';
            parentContainer.appendChild(productsContainer);

            productsList = document.getElementById('productsList');
        }
    }

    if (!productsList) {
        console.error('productsList element not found and could not be created');
        return;
    }

    // Create new product element
    const productHTML = createProductItemHTML(product, itemId);

    // Add to the end of the list
    productsList.insertAdjacentHTML('beforeend', productHTML);

    // Update product count in header
    updateProductCount();

    // Re-initialize drag and drop for the new item
    initializeDragAndDrop();
}

// Update product count in header
function updateProductCount() {
    const productItems = document.querySelectorAll('.product-item');
    const countElement = document.querySelector('h3');
    if (countElement && countElement.textContent.includes('Ürünler')) {
        countElement.textContent = `Ürünler (${productItems.length})`;
    }
}

// Refresh products list and preview
function refreshProductsList() {
    // Update preview
    updatePreview();

    // Re-initialize drag and drop
    initializeDragAndDrop();
}

async function addSelectedProducts() {
    if (selectedEcommerceProducts.length === 0) {
        showError('Lütfen en az bir ürün seçin.');
        return;
    }

    try {
        // Get selected products data
        const params = new URLSearchParams({
            page: 1,
            pageSize: 100
        });

        const response = await fetch(`/ProductSlider/GetEcommerceProducts?${params}`);
        const result = await response.json();

        if (result.success) {
            const selectedProducts = result.data.filter(p => selectedEcommerceProducts.includes(p.id));
            const addedProducts = [];

            // Add each selected product to the slider
            for (const product of selectedProducts) {
                try {
                    const itemId = await addEcommerceProductToSlider(product);
                    if (itemId) {
                        // Add to DOM immediately
                        addProductToDOM(product, itemId);
                        addedProducts.push(product);
                    }
                } catch (error) {
                    console.error('Error adding product:', product.name, error);
                }
            }

            closeEcommerceProductModal();

            if (addedProducts.length > 0) {
                showSuccess(`${addedProducts.length} ürün başarıyla eklendi.`);

                // Refresh the products list and preview
                refreshProductsList();
            } else {
                showError('Hiçbir ürün eklenemedi.');
            }
        }
    } catch (error) {
        console.error('Error adding selected products:', error);
        showError('Ürünler eklenirken hata oluştu.');
    }
}

async function addEcommerceProductToSlider(product) {
    const data = {
        SliderId: parseInt(currentSliderId),
        ProductTitle: product.name,
        ProductDescription: product.shortDescription || product.description || '',
        ProductPrice: parseFloat(product.discountPrice || product.price) || null,
        DiscountPrice: product.discountPrice != null ? (parseFloat(product.price) || null) : null,
        TaxRate: typeof product.taxRate === 'number' ? product.taxRate : (parseFloat(product.taxRate) || 18),
        KdvIncluded: product.kdvIncluded !== false,
        Currency: product.currency || 'TRY',
        ProductImage: product.image || '',
        ProductUrl: product.productUrl || product.url || '',
        ExternalProductId: product.id || null,
        SortOrder: 0,
        __RequestVerificationToken: getAntiForgeryToken()
    };

    console.log('Sending data to AddSliderItem:', data);

    const response = await fetch('/ProductSlider/AddSliderItem', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify(data)
    });

    if (!response.ok) {
        const errorText = await response.text();
        console.error('AddSliderItem error response:', response.status, errorText);
        throw new Error(`HTTP error! status: ${response.status} - ${errorText}`);
    }

    const result = await response.json();
    console.log('AddSliderItem response:', result);
    if (!result.success) {
        throw new Error(result.message || 'Ürün eklenirken hata oluştu.');
    }

    // Return the item ID for DOM manipulation
    return result.itemId;
}

// Close modals with Escape key
document.addEventListener('keydown', function(event) {
    if (event.key === 'Escape') {
        closeEmbedCodeModal();
        closeEcommerceProductModal();
    }
});

// Value update functions for range inputs
function updatePriceFontSizeValue(value) {
    document.getElementById('priceFontSizeValue').textContent = value;
}

function updateCardBorderRadiusValue(value) {
    const element = document.getElementById('cardBorderRadiusValue');
    if (element) element.textContent = value;
}

function updatePaddingValue(value) {
    const element = document.getElementById('paddingValue');
    if (element) element.textContent = value;
}

function updateMarginValue(value) {
    const element = document.getElementById('marginValue');
    if (element) element.textContent = value;
}

function updateTransitionDurationValue(value) {
    document.getElementById('transitionDurationValue').textContent = value;
}

// Initialize Slider Items Table
function initializeSliderItemsTable() {
    console.log('Initializing slider items table...');
    // Table initialization logic here
    // This function handles the slider items table setup
}

// Initialize Ecommerce Products Modal
function initializeEcommerceProductsModal() {
    console.log('Initializing ecommerce products modal...');
    // Modal initialization logic here
    // This function handles the product selection modal setup
}

function loadSliderItems() {
    console.log('Dummy loadSliderItems called');
}

// Initialize everything when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    currentSliderId = document.getElementById('sliderId').value;

    initializeSliderSettingsForm();
    initializeSliderItemsTable();
    initializeEcommerceProductsModal();
    initializeButtonPreview();
    initializeSliderDesign();

    // Load initial data
    loadSliderItems();
});
