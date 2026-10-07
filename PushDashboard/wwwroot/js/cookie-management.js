/**
 * Cookie Management Admin Page JavaScript
 * Handles settings form, live preview, color syncing, export, and section toggling
 * Matches exit-survey.js architecture
 */
$(document).ready(function () {
    if ($('#cookieSettingsForm').length) {

        // Save settings
        $('#saveSettingsBtn').on('click', function () {
            saveSettings();
        });

        // Export to site
        $('#exportToSiteBtn').on('click', function () {
            exportToSite();
        });

        // Preview modal: preview type radio change
        $(document).on('change', 'input[name="cookiePreviewType"]', function () {
            refreshCookiePreview();
        });

        // ESC to close preview modal
        $(document).on('keydown', function (e) {
            if (e.key === 'Escape') {
                var $modal = $('#cookiePreviewModal');
                if (!$modal.hasClass('hidden')) {
                    closeCookiePreviewModal();
                }
            }
        });

        // Toggle settings modal preview
        $('#toggleSettingsPreview').on('click', function () {
            $('#settingsPreviewWrapper').toggleClass('hidden');
            if (!$('#settingsPreviewWrapper').hasClass('hidden')) {
                updateSettingsPreview();
            }
        });

        // Position selector cards
        $(document).on('click', '.cm-position-card', function () {
            $('.cm-position-card').removeClass('active');
            $(this).addClass('active');
            $('#bannerPosition').val($(this).data('position'));
            updatePreview();
        });

        // Live preview on any form input change
        $('#cookieSettingsForm input, #cookieSettingsForm textarea, #cookieSettingsForm select').on('input change', function () {
            updatePreview();
        });

        // Category changes → update preview
        $(document).on('change input', '.category-enabled, .category-description', function () {
            updatePreview();
        });

        // Color picker ↔ text input sync
        syncColorInputs('bannerBackgroundColorPicker', 'bannerBackgroundColor');
        syncColorInputs('bannerTextColorPicker', 'bannerTextColor');
        syncColorInputs('acceptButtonColorPicker', 'acceptButtonColor');
        syncColorInputs('rejectButtonColorPicker', 'rejectButtonColor');
        syncColorInputs('settingsButtonColorPicker', 'settingsButtonColor');

        // Initial preview
        updatePreview();
    }
});

// ==================== UTILITY ====================

function syncColorInputs(pickerId, textId) {
    const $picker = $('#' + pickerId);
    const $text = $('#' + textId);
    if (!$picker.length || !$text.length) return;
    $picker.on('input change', function () {
        $text.val(this.value).trigger('input');
    });
    $text.on('change', function () {
        $picker.val(this.value);
    });
}

function toggleSection(headerEl) {
    const $header = $(headerEl);
    const $body = $header.next('.cm-section-body');
    const $chevron = $header.find('.cm-section-chevron');
    $body.toggleClass('collapsed');
    $chevron.toggleClass('collapsed');
}

function toggleCookieStatus() {
    const $hidden = $('#isActive');
    const isActive = $hidden.val() === 'true';
    const newStatus = !isActive;
    $hidden.val(newStatus ? 'true' : 'false');

    const $btn = $('#statusBtn');
    const $indicator = $('#statusIndicator');
    const $text = $('#statusText');

    if (newStatus) {
        $btn.removeClass('bg-gray-50 text-gray-600 border-gray-200').addClass('bg-green-50 text-green-700 border-green-200');
        $indicator.removeClass('bg-gray-400').addClass('bg-green-500 animate-pulse');
        $text.text('Aktif');
    } else {
        $btn.removeClass('bg-green-50 text-green-700 border-green-200').addClass('bg-gray-50 text-gray-600 border-gray-200');
        $indicator.removeClass('bg-green-500 animate-pulse').addClass('bg-gray-400');
        $text.text('Pasif');
    }
}

// ==================== DATA COLLECTION ====================

function getFormData() {
    return {
        bannerTitle: $('#bannerTitle').val(),
        bannerDescription: $('#bannerDescription').val(),
        acceptButtonText: $('#acceptButtonText').val(),
        rejectButtonText: $('#rejectButtonText').val(),
        settingsButtonText: $('#settingsButtonText').val(),
        saveButtonText: $('#saveButtonText').val(),
        bannerPosition: $('#bannerPosition').val(),
        bannerBackgroundColor: $('#bannerBackgroundColor').val(),
        bannerTextColor: $('#bannerTextColor').val(),
        acceptButtonColor: $('#acceptButtonColor').val(),
        rejectButtonColor: $('#rejectButtonColor').val(),
        settingsButtonColor: $('#settingsButtonColor').val(),
        borderRadius: ($('#borderRadius').val() || '8') + 'px',
        showSettingsButton: $('#showSettingsButton').is(':checked'),
        enableAnimation: $('#enableAnimation').is(':checked'),
        cookieExpiryDays: parseInt($('#cookieExpiryDays').val()) || 365,
        isActive: $('#isActive').val() === 'true',
        categories: collectCategories()
    };
}

function collectCategories() {
    const categories = [];
    $('.category-item').each(function () {
        const $el = $(this);
        categories.push({
            id: $el.data('category-id'),
            name: $el.find('.category-name').text().trim(),
            description: $el.find('.category-description').val(),
            isRequired: $el.find('.category-enabled').is(':disabled'),
            isEnabled: $el.find('.category-enabled').is(':checked')
        });
    });
    return categories;
}

// ==================== SAVE SETTINGS ====================

function saveSettings() {
    const $btn = $('#saveSettingsBtn');
    const originalHTML = $btn.html();

    $btn.prop('disabled', true);
    $btn.html('<i class="fas fa-spinner fa-spin mr-1"></i> Kaydediliyor...');

    const formData = getFormData();
    const token = $('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '/CookieManagement/SaveSettings',
        type: 'POST',
        contentType: 'application/json',
        headers: { 'RequestVerificationToken': token },
        data: JSON.stringify(formData),
        success: function (response) {
            if (response.success) {
                showNotification(response.message || 'Ayarlar başarıyla kaydedildi!', 'success');
            } else {
                showNotification(response.message || 'Bir hata oluştu!', 'error');
            }
        },
        error: function () {
            showNotification('Bir hata oluştu!', 'error');
        },
        complete: function () {
            $btn.prop('disabled', false);
            $btn.html(originalHTML);
        }
    });
}

// ==================== EXPORT TO SITE ====================

function exportToSite() {
    const $btn = $('#exportToSiteBtn');
    const originalHTML = $btn.html();

    $btn.prop('disabled', true);
    $btn.html('<i class="fas fa-spinner fa-spin mr-1"></i> Aktarılıyor...');

    const formData = getFormData();
    const token = $('input[name="__RequestVerificationToken"]').val();

    // First save settings
    $.ajax({
        url: '/CookieManagement/SaveSettings',
        type: 'POST',
        contentType: 'application/json',
        headers: { 'RequestVerificationToken': token },
        data: JSON.stringify(formData),
        success: function (saveResult) {
            if (!saveResult.success) {
                showNotification(saveResult.message || 'Ayarlar kaydedilemedi!', 'error');
                $btn.prop('disabled', false);
                $btn.html(originalHTML);
                return;
            }

            // Then export to R2
            $.ajax({
                url: '/CookieManagement/ExportToR2',
                type: 'POST',
                contentType: 'application/json',
                headers: { 'RequestVerificationToken': token },
                success: function (exportResult) {
                    if (exportResult.success) {
                        showNotification('Ayarlar kaydedildi ve siteye aktarıldı!', 'success');
                    } else {
                        showNotification(exportResult.message || 'Aktarım sırasında bir hata oluştu!', 'error');
                    }
                },
                error: function () {
                    showNotification('Aktarım sırasında bir hata oluştu!', 'error');
                },
                complete: function () {
                    $btn.prop('disabled', false);
                    $btn.html(originalHTML);
                }
            });
        },
        error: function () {
            showNotification('Kaydetme sırasında bir hata oluştu!', 'error');
            $btn.prop('disabled', false);
            $btn.html(originalHTML);
        }
    });
}

// ==================== PREVIEW MODAL ====================

function openCookiePreviewModal() {
    $('#cookiePreviewModal').removeClass('hidden');
    document.body.style.overflow = 'hidden';
    refreshCookiePreview();
}

function closeCookiePreviewModal() {
    $('#cookiePreviewModal').addClass('hidden');
    document.body.style.overflow = '';
    // Clear preview content
    $('#cookiePreviewContent').empty();
}

function refreshCookiePreview() {
    const config = getFormData();
    config.isActive = true;

    const previewType = $('input[name="cookiePreviewType"]:checked').val() || 'banner';
    const $container = $('#cookiePreviewContent');
    const $wrapper = $('#cookiePreviewWidget');

    $container.empty();

    // Reset wrapper positioning
    $wrapper.css({
        position: 'absolute',
        inset: '0',
        zIndex: 10,
        pointerEvents: 'none'
    });
    $container.css('pointer-events', 'auto');

    if (previewType === 'banner') {
        renderModalBannerPreview(config, $container);
    } else {
        renderModalSettingsPreview(config, $container);
    }
}

function renderModalBannerPreview(config, $container) {
    const pos = config.bannerPosition || 'bottom';
    const borderRadius = config.borderRadius || '8px';
    const isFullWidth = pos === 'top' || pos === 'bottom';
    const br = isFullWidth ? '0' : borderRadius;

    let posStyles = '';
    switch (pos) {
        case 'top':    posStyles = 'position:absolute;top:0;left:0;right:0;'; break;
        case 'left':   posStyles = 'position:absolute;bottom:16px;left:16px;max-width:300px;'; break;
        case 'right':  posStyles = 'position:absolute;bottom:16px;right:16px;max-width:300px;'; break;
        default:       posStyles = 'position:absolute;bottom:0;left:0;right:0;'; break;
    }

    const html = `
        <div style="${posStyles}
            background:${config.bannerBackgroundColor || '#ffffff'};
            color:${config.bannerTextColor || '#333333'};
            padding:16px 20px;
            border-radius:${br};
            box-shadow:0 -2px 20px rgba(0,0,0,0.08),0 0 0 1px rgba(0,0,0,0.04);
            font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;
            z-index:10;
            ${config.enableAnimation ? 'animation:cmFadeIn 0.35s ease;' : ''}
        ">
            <div style="display:flex;align-items:${isFullWidth ? 'center' : 'flex-start'};gap:16px;flex-wrap:wrap;${isFullWidth ? 'justify-content:space-between;' : 'flex-direction:column;'}">
                <div style="flex:1;min-width:180px;">
                    <div style="font-size:14px;font-weight:700;margin-bottom:4px;line-height:1.3;">
                        ${escapeHtml(config.bannerTitle || 'Bu site çerezleri kullanır')}
                    </div>
                    <div style="font-size:12px;opacity:0.85;line-height:1.4;">
                        ${escapeHtml(config.bannerDescription || '')}
                    </div>
                </div>
                <div style="display:flex;gap:8px;flex-wrap:wrap;flex-shrink:0;">
                    <button style="padding:7px 14px;border:none;border-radius:${borderRadius};background:${config.acceptButtonColor || '#4CAF50'};color:white;font-size:12px;font-weight:600;cursor:pointer;white-space:nowrap;">${escapeHtml(config.acceptButtonText || 'Tümünü Kabul Et')}</button>
                    <button style="padding:7px 14px;border:none;border-radius:${borderRadius};background:${config.rejectButtonColor || '#f44336'};color:white;font-size:12px;font-weight:600;cursor:pointer;white-space:nowrap;">${escapeHtml(config.rejectButtonText || 'Tümünü Reddet')}</button>
                    ${config.showSettingsButton ? `<button style="padding:7px 14px;border:none;border-radius:${borderRadius};background:${config.settingsButtonColor || '#2196F3'};color:white;font-size:12px;font-weight:600;cursor:pointer;white-space:nowrap;">${escapeHtml(config.settingsButtonText || 'Ayarları Yönet')}</button>` : ''}
                </div>
            </div>
        </div>
    `;
    $container.html(html);
}

function renderModalSettingsPreview(config, $container) {
    const categories = config.categories || [];
    let catHtml = '';

    categories.forEach(function (cat) {
        const isChecked = cat.isRequired || cat.isEnabled;
        const isDisabled = cat.isRequired;
        catHtml += `
            <div style="padding:14px;border:1px solid #e5e7eb;border-radius:10px;margin-bottom:10px;">
                <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:6px;">
                    <div style="display:flex;align-items:center;gap:8px;">
                        <span style="font-size:13px;font-weight:600;color:#1f2937;">${escapeHtml(cat.name)}</span>
                        ${cat.isRequired ? '<span style="font-size:10px;padding:1px 6px;border-radius:4px;background:#fef2f2;color:#dc2626;font-weight:500;">Zorunlu</span>' : ''}
                    </div>
                    <div style="position:relative;display:inline-block;width:36px;height:20px;">
                        <div style="width:36px;height:20px;background:${isChecked ? (config.acceptButtonColor || '#4CAF50') : '#d1d5db'};border-radius:20px;${isDisabled ? 'opacity:0.6;' : ''}">
                            <div style="width:16px;height:16px;background:white;border-radius:50%;position:absolute;top:2px;left:${isChecked ? '18px' : '2px'};box-shadow:0 1px 3px rgba(0,0,0,0.15);"></div>
                        </div>
                    </div>
                </div>
                <div style="font-size:11px;color:#6b7280;line-height:1.4;">${escapeHtml(cat.description || '')}</div>
            </div>
        `;
    });

    const html = `
        <div style="position:absolute;inset:0;display:flex;align-items:center;justify-content:center;background:rgba(0,0,0,0.3);backdrop-filter:blur(2px);z-index:10;">
            <div style="background:white;border-radius:16px;padding:24px;max-width:400px;width:90%;max-height:80%;overflow-y:auto;box-shadow:0 20px 60px rgba(0,0,0,0.15);font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;">
                <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
                    <h3 style="font-size:16px;font-weight:700;color:#1f2937;margin:0;">Çerez Ayarları</h3>
                    <span style="font-size:16px;color:#9ca3af;cursor:pointer;">&times;</span>
                </div>
                ${catHtml}
                <div style="display:flex;justify-content:flex-end;gap:10px;margin-top:16px;padding-top:16px;border-top:1px solid #e5e7eb;">
                    <button style="padding:8px 20px;border:none;border-radius:${config.borderRadius || '8px'};background:${config.acceptButtonColor || '#4CAF50'};color:white;font-size:13px;font-weight:600;cursor:pointer;">${escapeHtml(config.saveButtonText || 'Seçimleri Kaydet')}</button>
                </div>
            </div>
        </div>
    `;
    $container.html(html);
}

// ==================== LIVE PREVIEW ====================

function updatePreview() {
    const config = getFormData();
    config.isActive = true; // Always show in preview
    renderBannerPreview(config);

    // Update settings modal preview if visible
    if (!$('#settingsPreviewWrapper').hasClass('hidden')) {
        updateSettingsPreview();
    }
}

function renderBannerPreview(config) {
    const target = document.getElementById('bannerPreviewTarget');
    if (!target) return;

    const pos = config.bannerPosition || 'bottom';
    const borderRadius = config.borderRadius || '8px';

    // Position styles
    let positionStyles = '';
    switch (pos) {
        case 'top':
            positionStyles = 'top: 0; left: 0; right: 0;';
            break;
        case 'left':
            positionStyles = 'bottom: 10px; left: 10px; max-width: 280px;';
            break;
        case 'right':
            positionStyles = 'bottom: 10px; right: 10px; max-width: 280px;';
            break;
        default: // bottom
            positionStyles = 'bottom: 0; left: 0; right: 0;';
            break;
    }

    const isFullWidth = pos === 'top' || pos === 'bottom';
    const br = isFullWidth ? '0' : borderRadius;

    target.innerHTML = `
        <div style="
            position: absolute;
            ${positionStyles}
            background: ${config.bannerBackgroundColor || '#ffffff'};
            color: ${config.bannerTextColor || '#333333'};
            padding: 16px 20px;
            border-radius: ${br};
            box-shadow: 0 -2px 20px rgba(0,0,0,0.08);
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            z-index: 10;
            ${config.enableAnimation ? 'transition: all 0.3s ease;' : ''}
        ">
            <div style="display: flex; align-items: ${isFullWidth ? 'center' : 'flex-start'}; gap: 16px; flex-wrap: wrap; ${isFullWidth ? 'justify-content: space-between;' : 'flex-direction: column;'}">
                <div style="flex: 1; min-width: 200px;">
                    <div style="font-size: 14px; font-weight: 700; margin-bottom: 4px; line-height: 1.3;">
                        ${escapeHtml(config.bannerTitle || 'Bu site çerezleri kullanır')}
                    </div>
                    <div style="font-size: 12px; opacity: 0.85; line-height: 1.4;">
                        ${escapeHtml(config.bannerDescription || '')}
                    </div>
                </div>
                <div style="display: flex; gap: 8px; flex-wrap: wrap; flex-shrink: 0;">
                    <button style="
                        padding: 7px 14px;
                        border: none;
                        border-radius: ${borderRadius};
                        background: ${config.acceptButtonColor || '#4CAF50'};
                        color: white;
                        font-size: 12px;
                        font-weight: 600;
                        cursor: pointer;
                        transition: opacity 0.2s;
                        white-space: nowrap;
                    ">${escapeHtml(config.acceptButtonText || 'Tümünü Kabul Et')}</button>
                    <button style="
                        padding: 7px 14px;
                        border: none;
                        border-radius: ${borderRadius};
                        background: ${config.rejectButtonColor || '#f44336'};
                        color: white;
                        font-size: 12px;
                        font-weight: 600;
                        cursor: pointer;
                        transition: opacity 0.2s;
                        white-space: nowrap;
                    ">${escapeHtml(config.rejectButtonText || 'Tümünü Reddet')}</button>
                    ${config.showSettingsButton ? `
                    <button style="
                        padding: 7px 14px;
                        border: none;
                        border-radius: ${borderRadius};
                        background: ${config.settingsButtonColor || '#2196F3'};
                        color: white;
                        font-size: 12px;
                        font-weight: 600;
                        cursor: pointer;
                        transition: opacity 0.2s;
                        white-space: nowrap;
                    ">${escapeHtml(config.settingsButtonText || 'Ayarları Yönet')}</button>` : ''}
                </div>
            </div>
        </div>
    `;
}

function updateSettingsPreview() {
    const config = getFormData();
    const target = document.getElementById('settingsPreviewTarget');
    if (!target) return;

    const categories = config.categories || [];
    let categoriesHtml = '';

    categories.forEach(function (cat) {
        const isChecked = cat.isRequired || cat.isEnabled;
        const isDisabled = cat.isRequired;

        categoriesHtml += `
            <div style="padding: 14px; border: 1px solid #e5e7eb; border-radius: 10px; margin-bottom: 10px;">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px;">
                    <div style="display: flex; align-items: center; gap: 8px;">
                        <span style="font-size: 13px; font-weight: 600; color: #1f2937;">${escapeHtml(cat.name)}</span>
                        ${cat.isRequired ? '<span style="font-size: 10px; padding: 1px 6px; border-radius: 4px; background: #fef2f2; color: #dc2626; font-weight: 500;">Zorunlu</span>' : ''}
                    </div>
                    <div style="position: relative; display: inline-block; width: 36px; height: 20px;">
                        <div style="
                            width: 36px; height: 20px;
                            background: ${isChecked ? (config.acceptButtonColor || '#4CAF50') : '#d1d5db'};
                            border-radius: 20px;
                            transition: background 0.2s;
                            ${isDisabled ? 'opacity: 0.6;' : ''}
                        ">
                            <div style="
                                width: 16px; height: 16px;
                                background: white;
                                border-radius: 50%;
                                position: absolute;
                                top: 2px;
                                left: ${isChecked ? '18px' : '2px'};
                                transition: left 0.2s;
                                box-shadow: 0 1px 3px rgba(0,0,0,0.15);
                            "></div>
                        </div>
                    </div>
                </div>
                <div style="font-size: 11px; color: #6b7280; line-height: 1.4;">
                    ${escapeHtml(cat.description || '')}
                </div>
            </div>
        `;
    });

    target.innerHTML = `
        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
                <h3 style="font-size: 16px; font-weight: 700; color: #1f2937; margin: 0;">Çerez Ayarları</h3>
                <button style="background: none; border: none; font-size: 18px; color: #9ca3af; cursor: pointer;">&times;</button>
            </div>
            ${categoriesHtml}
            <div style="display: flex; justify-content: flex-end; gap: 10px; margin-top: 16px; padding-top: 16px; border-top: 1px solid #e5e7eb;">
                <button style="
                    padding: 8px 20px;
                    border: none;
                    border-radius: ${config.borderRadius || '8px'};
                    background: ${config.acceptButtonColor || '#4CAF50'};
                    color: white;
                    font-size: 13px;
                    font-weight: 600;
                    cursor: pointer;
                ">${escapeHtml(config.saveButtonText || 'Seçimleri Kaydet')}</button>
            </div>
        </div>
    `;
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.appendChild(document.createTextNode(text));
    return div.innerHTML;
}
