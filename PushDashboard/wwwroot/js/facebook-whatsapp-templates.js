$(document).ready(function() {
    let currentTemplateId = null;
    let currentTemplate = null;
    let templateComponents = [];
    let templateVariables = {};
    let currentMediaComponentId = null;

    // Variables storage
    let availableVariables = {};
    
    // Get available variables for selected module (now returns from loaded state)
    function getAvailableVariablesForModule(moduleType) {
        return availableVariables;
    }

    // Load available variables initially (for View mode)
    function loadInitialVariables() {
        $.get('/WhatsAppTemplate/GetModuleVariables')
            .done(function(response) {
                if (response.success && response.variables) {
                    availableVariables = {};
                    response.variables.forEach(v => {
                        availableVariables[v.key] = { label: v.label, example: `[${v.label}]`, order: 0 };
                    });
                     window.predefinedVariables = availableVariables;
                }
            });
    }
    loadInitialVariables();

    // Module type selection - stored globally
    let selectedModuleType = '';

    // Create template button handlers - REMOVED (Handled by module selector flow in whatsapp-templates-ui.js)
    // $('#createTemplateBtn, #createFirstTemplateBtn').on('click', function() {
    //     showCreateTemplateModal();
    // });
    
    // Module type selection - update UI state and fetch variables
    $(document).on('click', '.module-type-option', function() {
        // Update UI Selection
        $('.module-type-option').removeClass('ring-1 ring-primary border-primary bg-primary-50');
        $(this).addClass('ring-1 ring-primary border-primary bg-primary-50');
        
        selectedModuleType = $(this).data('module');
        console.log(`Module type selected: ${selectedModuleType}`);
        
        // Fetch variables for the selected module
        showLoading();
        $.get(`/WhatsAppTemplate/GetModuleVariables?moduleType=${selectedModuleType}`)
            .done(function(response) {
                if (response.success && response.variables) {
                    availableVariables = {};
                    response.variables.forEach(v => {
                        availableVariables[v.key] = { label: v.label, example: `[${v.label}]`, order: 0 };
                    });
                     // Expose globally for preview renderer
                     window.predefinedVariables = availableVariables;
                     
                     // If we have text content, re-parse variables
                     if ($('#bodyText').val()) {
                         extractVariablesFromText($('#bodyText').val());
                     }
                }
            })
            .always(function() {
                hideLoading();
            });
    });
    
    // Module type change handler - REMOVED (no longer have dropdown in template form)
    // Module selection is now done BEFORE opening template modal

    // Template item click handler
    $('.template-item').on('click', function() {
        const templateId = $(this).data('template-id');
        loadTemplate(templateId);
    });

    // View template button handler
    $('.view-template-btn').on('click', function(e) {
        e.stopPropagation();
        const templateId = $(this).data('template-id');
        showViewTemplateModal(templateId);
    });

    // Delete template button handler
    $('.delete-template-btn').on('click', function(e) {
        e.stopPropagation();
        const templateName = $(this).data('template-name');
        deleteTemplate(templateName);
    });

    // Close modal handlers
    $('#closeModalBtn, #closeModalBtn2, #closeViewModalBtn').on('click', function() {
        hideTemplateModal();
    });

    // Modal backdrop click
    $('#templateModal').on('click', function(e) {
        if (e.target === this) {
            hideTemplateModal();
        }
    });

    // --- NEW FORM EVENT HANDLERS ---

    // Header Type Change
    $('#headerType').on('change', function() {
        const type = $(this).val();
        updateHeaderUI(type);
        updateTemplateData();
    });

    // Header Text Change
    $('#headerText').on('input', function() {
        $('#headerTextCount').text($(this).val().length);
        updateTemplateData();
    });

    // Header Media Upload
    $('#uploadHeaderMediaBtn').on('click', function() {
        // We'll use a special ID for header media
        currentMediaComponentId = 'HEADER_MEDIA'; 
        showMediaUploadModal();
    });

    // Header Media URL Change
    $('#headerMediaUrl').on('input', function() {
        // Clear upload preview if URL is entered manually (optional, or keep both in sync)
        updateTemplateData();
    });

    // Body Text Change
    $('#bodyText').on('input', function() {
        $('#bodyTextCount').text($(this).val().length);
        extractVariablesFromText($(this).val());
        updateTemplateData();
    });

    // Variable Add Button
    $('#addVariableBtn').on('click', function() {
        const $textarea = $('#bodyText');
        const text = $textarea.val();
        
        // Find next variable number
        const currentVars = (text.match(/\{\{(\d+)\}\}/g) || []).map(v => parseInt(v.match(/\d+/)[0]));
        let nextVar = 1;
        while (currentVars.includes(nextVar)) nextVar++;
        
        const varText = `{{${nextVar}}}`;
        const cursorPos = $textarea.prop('selectionStart');
        
        // Correct substring usage
        const newTextCorrect = text.substring(0, cursorPos) + varText + text.substring(cursorPos);

        $textarea.val(newTextCorrect).trigger('input');
        $textarea.focus();
        const newPos = cursorPos + varText.length;
        $textarea[0].setSelectionRange(newPos, newPos);
    });

    // Text Editor Toolbar events
    $('.format-btn').on('click', function() {
        const format = $(this).data('format');
        const $textarea = $('#bodyText');
        
        if ($textarea.length === 0) return;
        
        const textarea = $textarea[0];
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const text = textarea.value;
        const selectedText = text.substring(start, end);
        
        let newText = '';
        let newCursorPos = 0;
        
        if (format === 'bold') {
            newText = text.substring(0, start) + '*' + selectedText + '*' + text.substring(end);
            newCursorPos = end + 2; // Move cursor after the closing *
            if (start === end) newCursorPos = start + 1; // If no selection, put cursor between **
        } else if (format === 'italic') {
            newText = text.substring(0, start) + '_' + selectedText + '_' + text.substring(end);
            newCursorPos = end + 2;
            if (start === end) newCursorPos = start + 1;
        } else if (format === 'strike') {
            newText = text.substring(0, start) + '~' + selectedText + '~' + text.substring(end);
            newCursorPos = end + 2;
            if (start === end) newCursorPos = start + 1;
        } else if (format === 'code') {
            newText = text.substring(0, start) + '```' + selectedText + '```' + text.substring(end);
            newCursorPos = end + 6;
            if (start === end) newCursorPos = start + 3;
        }
        
        textarea.value = newText;
        textarea.focus();
        textarea.setSelectionRange(newCursorPos, newCursorPos);
        
        // Trigger input event to update component data
        $textarea.trigger('input');
    });


    // Add Button Click
    $('#addNewButtonBtn').on('click', function() {
        addNewButton();
        updateTemplateData();
    });

    // Preview with sample data
    $('#previewWithSampleData').on('click', function() {
        updatePreviewWithSampleData();
    });

    // Sample templates handlers
    $('#showSampleTemplatesBtn').on('click', function() {
        showSampleTemplatesModal();
    });

    $('#closeSampleModalBtn, #closeSampleModalBtn2').on('click', function() {
        hideSampleTemplatesModal();
    });

    // --- HELPER FUNCTIONS FOR NEW FORM ---

    function updateHeaderUI(type) {
        $('#headerConfig').removeClass('hidden');
        $('#headerTextConfig').addClass('hidden');
        $('#headerMediaConfig').addClass('hidden');

        // Clear inputs when switching types
        if (type !== 'TEXT') {
            $('#headerText').val('');
        }
        if (type === 'NONE' || type === 'TEXT') {
            $('#headerMediaUrl').val('');
            $('#headerMediaPreview').empty().addClass('hidden').removeData('media-id');
        }

        if (type === 'NONE') {
            $('#headerConfig').addClass('hidden');
        } else if (type === 'TEXT') {
            $('#headerTextConfig').removeClass('hidden');
        } else {
            $('#headerMediaConfig').removeClass('hidden');
            // Update upload button text based on type
            const typeLabels = { 'IMAGE': 'Görsel', 'VIDEO': 'Video', 'DOCUMENT': 'Döküman' };
            $('#uploadHeaderMediaBtn').html(`
                <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"></path></svg>
                ${typeLabels[type]} Yükle
            `);
        }
    }

    function addNewButton(type = 'QUICK_REPLY', text = '', url = '', phoneNumber = '') {
        const buttonCount = $('#buttonsContainer .button-item').length;
        // WhatsApp limit is 3 buttons total. We reserve 1 for Unsubscribe, so user can add max 2.
        if (buttonCount >= 2) { 
            showNotification('En fazla 2 özel buton ekleyebilirsiniz. "Bildirimden Çık" butonu otomatik eklenir.', 'warning');
            return;
        }

        $('#noButtonsMessage').hide();

        const buttonHtml = `
            <div class="button-item bg-gray-50 border border-gray-200 rounded-lg p-3 relative group">
                <button type="button" class="remove-button absolute top-2 right-2 text-gray-400 hover:text-red-500">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
                </button>
                <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
                    <div class="sm:col-span-1">
                        <label class="block text-xs font-medium text-gray-700 mb-1">Tip</label>
                        <select class="button-type-select w-full px-2 py-1.5 text-sm border border-gray-300 rounded focus:ring-primary focus:border-primary">
                            <option value="QUICK_REPLY" ${type === 'QUICK_REPLY' ? 'selected' : ''}>Hızlı Yanıt</option>
                            <option value="URL" ${type === 'URL' ? 'selected' : ''}>URL</option>
                            <option value="PHONE_NUMBER" ${type === 'PHONE_NUMBER' ? 'selected' : ''}>Telefon</option>
                        </select>
                    </div>
                    <div class="sm:col-span-2">
                        <label class="block text-xs font-medium text-gray-700 mb-1">Metin</label>
                        <input type="text" class="button-text-input w-full px-2 py-1.5 text-sm border border-gray-300 rounded focus:ring-primary focus:border-primary" placeholder="Buton metni" maxlength="25" value="${text}">
                    </div>
                </div>
                <div class="button-extra-fields mt-2 ${type === 'QUICK_REPLY' ? 'hidden' : ''}">
                    ${type === 'URL' ? `
                        <label class="block text-xs font-medium text-gray-700 mb-1">URL</label>
                        <input type="url" class="button-url-input w-full px-2 py-1.5 text-sm border border-gray-300 rounded focus:ring-primary focus:border-primary" placeholder="https://example.com" value="${url}">
                    ` : type === 'PHONE_NUMBER' ? `
                        <label class="block text-xs font-medium text-gray-700 mb-1">Telefon Numarası</label>
                        <input type="tel" class="button-phone-input w-full px-2 py-1.5 text-sm border border-gray-300 rounded focus:ring-primary focus:border-primary" placeholder="+90 555 123 4567" value="${phoneNumber}">
                    ` : ''}
                </div>
            </div>
        `;

        const $button = $(buttonHtml);
        $('#buttonsContainer').append($button);

        // Bind events for this button
        $button.find('.remove-button').on('click', function() {
            $button.remove();
            if ($('#buttonsContainer .button-item').length === 0) {
                $('#noButtonsMessage').show();
            }
            updateTemplateData();
        });

        $button.find('input, select').on('input change', function() {
            if ($(this).hasClass('button-type-select')) {
                updateButtonExtraFields($button);
            }
            updateTemplateData();
        });
    }

    function updateButtonExtraFields($button) {
        const type = $button.find('.button-type-select').val();
        const $extra = $button.find('.button-extra-fields');
        
        if (type === 'URL') {
            $extra.html(`
                <label class="block text-xs font-medium text-gray-700 mb-1">URL</label>
                <input type="url" class="button-url-input w-full px-2 py-1.5 text-sm border border-gray-300 rounded focus:ring-primary focus:border-primary" placeholder="https://example.com">
            `);
            $extra.removeClass('hidden');
        } else if (type === 'PHONE_NUMBER') {
            $extra.html(`
                <label class="block text-xs font-medium text-gray-700 mb-1">Telefon Numarası</label>
                <input type="tel" class="button-phone-input w-full px-2 py-1.5 text-sm border border-gray-300 rounded focus:ring-primary focus:border-primary" placeholder="+90 555 123 4567">
            `);
            $extra.removeClass('hidden');
        } else {
            $extra.empty().addClass('hidden');
        }
        
        // Bind events for new inputs
        $extra.find('input').on('input', function() {
            updateTemplateData();
        });
    }

    function updateTemplateData() {
        templateComponents = [];

        // 1. Header
        const headerType = $('#headerType').val();
        if (headerType && headerType !== 'NONE') {
            const headerComponent = {
                id: 'header_component',
                type: 'HEADER',
                format: headerType
            };
            
            if (headerType === 'TEXT') {
                headerComponent.text = $('#headerText').val();
            } else {
                const urlInput = $('#headerMediaUrl').val();
                const uploadedMediaId = $('#headerMediaPreview').data('media-id');
                headerComponent.text = uploadedMediaId || urlInput || '';
            }
            templateComponents.push(headerComponent);
        }

        // 2. Body
        const bodyText = $('#bodyText').val();
        if (bodyText) {
            templateComponents.push({
                id: 'body_component',
                type: 'BODY',
                text: bodyText
            });
        }

        // 3. Footer (Fixed text)
        // Always add footer with fixed text
        const footerText = 'Dijitaldesin tarafından desteklenmektedir.';
        templateComponents.push({
            id: 'footer_component',
            type: 'FOOTER',
            text: footerText
        });

        // 4. Buttons
        const buttons = [];
        $('#buttonsContainer .button-item').each(function() {
            const type = $(this).find('.button-type-select').val();
            const text = $(this).find('.button-text-input').val();
            const btn = { type, text };
            
            if (type === 'URL') {
                btn.url = $(this).find('.button-url-input').val();
            } else if (type === 'PHONE_NUMBER') {
                btn.phoneNumber = $(this).find('.button-phone-input').val();
            }
            
            buttons.push(btn);
        });

        // MANDATORY UNSUBSCRIBE BUTTON
        // Always append the unsubscribe button as the last button
        // URL will be provided/enforced by backend or we can hardcode base URL here if needed for preview
        buttons.push({
            type: 'URL',
            text: 'Bildirimden Çık',
            url: 'https://admin.pushonica.com/Unsubscribe' // Static for now, can be dynamic
        });

        if (buttons.length > 0) {
            templateComponents.push({
                id: 'buttons_component',
                type: 'BUTTONS',
                buttons: buttons
            });
        }

        updatePreview();
        validateTemplate();
    }



    // Media upload modal handlers
    $('#closeMediaModalBtn, #closeMediaModalBtn2').on('click', function() {
        hideMediaUploadModal();
    });

    $('#mediaFileInput').on('change', function() {
        handleFileSelection(this);
    });

    $('#uploadMediaBtn').on('click', function() {
        uploadSelectedMedia();
    });

    // Show create template modal
    window.showCreateTemplateModal = function(moduleType) {
        currentTemplateId = null;
        currentTemplate = null;
        templateComponents = [];
        templateVariables = {};

        $('#templateModalLabel span').text('Yeni WhatsApp Şablonu Oluştur');
        $('#createForm')[0].reset();

        // Reset Module Selection if not passed, or set it
        if (moduleType) {
            selectedModuleType = moduleType;
            // Visual update if needed (buttons are in other modal, so not strictly needed here unless we show it)
             console.log(`Module type set from selector: ${selectedModuleType}`);
             
             // Fetch variables for the selected module
            $.get(`/WhatsAppTemplate/GetModuleVariables?moduleType=${selectedModuleType}`)
                .done(function(response) {
                    if (response.success && response.variables) {
                        availableVariables = {};
                        response.variables.forEach(v => {
                            availableVariables[v.key] = { label: v.label, example: `[${v.label}]`, order: 0 };
                        });
                         window.predefinedVariables = availableVariables;
                         // Update UI or placeholders if needed
                    }
                });
        } else {
            selectedModuleType = '';
            $('.module-type-option').removeClass('ring-1 ring-primary border-primary bg-primary-50');
        }

        // Reset Fixed Form UI
        $('#headerType').val('NONE').trigger('change');
        $('#bodyText').val('').trigger('input');
        $('#buttonsContainer').empty();
        $('#noButtonsMessage').show();
        $('#variableDefinitions').empty();
        $('#headerMediaPreview').empty().addClass('hidden').removeData('media-id');

        // Reset Preview
        $('#whatsappPreview').html('<div class="text-center text-gray-500 text-sm">Şablon içeriğini soldaki formdan düzenleyin</div>');
        $('#templateValidation').addClass('hidden');

        // Enable template name editing for new templates
        enableTemplateNameEditing();

        // Show create form, hide edit form
        $('#createTemplateForm').removeClass('hidden');
        $('#editTemplateForm').addClass('hidden');

        showTemplateModal();
    }

    // Show view template modal
    function showViewTemplateModal(templateId) {
        showLoading();

        $.get(`/WhatsAppTemplate/GetTemplate/?templateId=${templateId}`)
            .done(function(response) {
                if (response.success) {
                    currentTemplateId = templateId;
                    currentTemplate = response.template;
                    populateViewTemplateForm(response.template);
                    showViewModal(); // Show view modal, not create modal
                } else {
                    showNotification(response.message || 'Şablon yüklenirken hata oluştu.', 'error');
                }
            })
            .fail(function() {
                showNotification('Şablon yüklenirken hata oluştu.', 'error');
            })
            .always(function() {
                hideLoading();
            });
    }

    // Show view modal (for viewing existing templates)
    function showViewModal() {
        $('#templateModal').removeClass('hidden');
        $('body').addClass('overflow-hidden');

        // Show view form, hide create form
        $('#viewTemplateForm').removeClass('hidden');
        $('#createTemplateForm').addClass('hidden');

        // Update modal title (will be updated with template name in populateViewTemplateForm)
        $('#templateModalLabel span').text('Şablon Detayı');
    }

    // Load template data (legacy function - kept for compatibility)
    function loadTemplate(templateId) {
        showViewTemplateModal(templateId);
    }

    // Populate view form with template data
    function populateViewTemplateForm(template) {
        $('#templateModalLabel span').text(`${template.name} - Görüntüle`);

        // Show view form, hide create form
        $('#viewTemplateForm').removeClass('hidden');
        $('#createTemplateForm').addClass('hidden');

        // Populate template info
        $('#viewTemplateName').text(template.name);
        $('#viewTemplateCategory').text(template.category);
        $('#viewTemplateStatus').text(template.status);
        $('#viewTemplateLanguage').text(template.language);
        $('#viewTemplateCreated').text(template.createdTime ? new Date(template.createdTime).toLocaleDateString('tr-TR') : '-');
        $('#viewTemplateQuality').text(template.qualityScore || '-');

        // Status badge color
        const statusBadge = $('#viewTemplateStatus');
        statusBadge.removeClass('bg-emerald-50 text-emerald-700 bg-amber-50 text-amber-700 bg-rose-50 text-rose-700 bg-gray-100 text-gray-800');

        if (template.status === 'APPROVED') {
            statusBadge.addClass('bg-emerald-50 text-emerald-700');
        } else if (template.status === 'PENDING') {
            statusBadge.addClass('bg-amber-50 text-amber-700');
        } else if (template.status === 'REJECTED') {
            statusBadge.addClass('bg-rose-50 text-rose-700');
        } else {
            statusBadge.addClass('bg-gray-100 text-gray-800');
        }

        // Show rejection reason if exists
        if (template.rejectionReason) {
            $('#viewRejectionReasonContainer').removeClass('hidden');
            $('#viewRejectionReason').text(template.rejectionReason);
        } else {
            $('#viewRejectionReasonContainer').addClass('hidden');
        }

        // Convert template to component builder format
        convertTemplateToComponents(template);

        // Populate components in builder format
        populateViewComponents(templateComponents);

        // Update preview
        updateViewPreview();

        // Extract and show variables
        extractAndShowVariables();
    }



    // Populate components
    function populateComponents(components) {
        const container = $('#componentsContainer');
        container.empty();

        components.forEach((component, index) => {
            const componentHtml = createComponentHtml(component, index);
            container.append(componentHtml);
        });

        if (components.length === 0) {
            container.html('<p class="text-gray-500 text-sm">Bu şablonda henüz component bulunmuyor.</p>');
        }
    }

    // Create component HTML
    function createComponentHtml(component, index) {
        let componentContent = '';

        if (component.type === 'HEADER') {
            componentContent = `
                <div class="bg-sky-50 border border-sky-100 rounded-lg p-3">
                    <div class="flex items-center mb-2">
                        <svg class="w-4 h-4 text-sky-700 mr-2" fill="currentColor" viewBox="0 0 20 20">
                            <path d="M3 4a1 1 0 011-1h12a1 1 0 011 1v2a1 1 0 01-1 1H4a1 1 0 01-1-1V4zM3 10a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H4a1 1 0 01-1-1v-6zM14 9a1 1 0 00-1 1v6a1 1 0 001 1h2a1 1 0 001-1v-6a1 1 0 00-1-1h-2z"></path>
                        </svg>
                        <span class="font-medium text-sky-700">Header</span>
                    </div>
                    <p class="text-sm text-sky-700">${component.text || 'Metin yok'}</p>
                    ${component.format && component.format !== 'TEXT' ? `<p class="text-xs text-sky-700 mt-1">Format: ${component.format}</p>` : ''}
                </div>
            `;
        } else if (component.type === 'BODY') {
            componentContent = `
                <div class="bg-gray-50 border border-gray-200 rounded p-3">
                    <div class="flex items-center mb-2">
                        <svg class="w-4 h-4 text-gray-600 mr-2" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M3 4a1 1 0 011-1h12a1 1 0 011 1v12a1 1 0 01-1 1H4a1 1 0 01-1-1V4zm2 3a1 1 0 000 2h10a1 1 0 100-2H5zm0 4a1 1 0 100 2h10a1 1 0 100-2H5z" clip-rule="evenodd"></path>
                        </svg>
                        <span class="font-medium text-gray-800">Body</span>
                    </div>
                    <p class="text-sm text-gray-700 whitespace-pre-wrap">${component.text || 'Metin yok'}</p>
                </div>
            `;
        } else if (component.type === 'FOOTER') {
            componentContent = `
                <div class="bg-gray-50 border border-gray-200 rounded p-3">
                    <div class="flex items-center mb-2">
                        <svg class="w-4 h-4 text-gray-600 mr-2" fill="currentColor" viewBox="0 0 20 20">
                            <path d="M3 17a1 1 0 011-1h12a1 1 0 011 1v1a1 1 0 01-1 1H4a1 1 0 01-1-1v-1z"></path>
                        </svg>
                        <span class="font-medium text-gray-800">Footer</span>
                    </div>
                    <p class="text-sm text-gray-700">${component.text || 'Metin yok'}</p>
                </div>
            `;
        } else if (component.type === 'BUTTONS') {
            const buttonsHtml = component.buttons ? component.buttons.map(button => `
                <div class="bg-emerald-50 border border-emerald-100 rounded-lg p-2 mb-2">
                    <div class="flex items-center justify-between">
                        <span class="text-sm font-medium text-emerald-700">${button.text}</span>
                        <span class="text-xs text-primary">${button.type}</span>
                    </div>
                    ${button.url ? `<p class="text-xs text-primary mt-1">${button.url}</p>` : ''}
                    ${button.phoneNumber ? `<p class="text-xs text-primary mt-1">${button.phoneNumber}</p>` : ''}
                </div>
            `).join('') : '';

            componentContent = `
                <div class="bg-emerald-50 border border-emerald-100 rounded-lg p-3">
                    <div class="flex items-center mb-2">
                        <svg class="w-4 h-4 text-primary mr-2" fill="currentColor" viewBox="0 0 20 20">
                            <path d="M3 4a1 1 0 011-1h12a1 1 0 011 1v2a1 1 0 01-1 1H4a1 1 0 01-1-1V4zM3 10a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H4a1 1 0 01-1-1v-6zM14 9a1 1 0 00-1 1v6a1 1 0 001 1h2a1 1 0 001-1v-6a1 1 0 00-1-1h-2z"></path>
                        </svg>
                        <span class="font-medium text-emerald-700">Buttons</span>
                    </div>
                    ${buttonsHtml || '<p class="text-sm text-emerald-700">Buton yok</p>'}
                </div>
            `;
        }

        return `<div class="component-item mb-3" data-index="${index}">${componentContent}</div>`;
    }

    // Create template
    $('#createTemplateBtn2').on('click', function() {
        // Validate template first
        if (!validateTemplate()) {
            showNotification('Lütfen tüm doğrulama hatalarını düzeltin. Özellikle değişken türlerinin seçildiğinden emin olun.', 'warning');

            // Scroll to validation messages
            const $validation = $('#templateValidation');
            if (!$validation.hasClass('hidden')) {
                $validation[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
            return;
        }

        const templateName = $('#createTemplateName').val().trim();
        if (!templateName) {
            showNotification('Şablon adı gereklidir.', 'warning');
            $('#createTemplateName').focus();
            return;
        }

        if (!selectedModuleType) {
            showNotification('Lütfen bir modül türü seçiniz.', 'warning');
            $('#createTemplateForm .custom-scrollbar').animate({ scrollTop: 0 }, "fast");
            return;
        }

        // Prepare components for Facebook API (Business Management API format)
        const facebookComponents = templateComponents.map(component => {
            const fbComponent = {
                type: component.type.toUpperCase()
            };

            if (component.type === 'HEADER') {
                console.log(`Processing HEADER component: format=${component.format}, text=${component.text}`);
                if (component.format === 'TEXT') {
                    fbComponent.format = 'TEXT';
                    fbComponent.text = component.text || '';
                } else {
                    // IMAGE, VIDEO, DOCUMENT
                    fbComponent.format = component.format?.toUpperCase();
                    if (component.text) {
                        fbComponent.text = component.text;
                        fbComponent.example = {
                            header_handle: [component.text]
                        };
                    }
                }
                console.log(`HEADER fbComponent result:`, fbComponent);
            } else if (component.type === 'BODY') {
                fbComponent.text = component.text || '';

                // Extract variables and add example values
                const variableMatches = component.text.match(/\{\{(\d+)\}\}/g);
                if (variableMatches) {
                    const uniqueVars = [...new Set(variableMatches.map(match =>
                        parseInt(match.replace(/[{}]/g, ''))
                    ))].sort((a, b) => a - b);

                    fbComponent.example = {
                        body_text: [uniqueVars.map(varNum => {
                            const variable = templateVariables[varNum];
                            if (variable && variable.type && availableVariables[variable.type]) {
                                return availableVariables[variable.type].example;
                            }
                            return `sample_value_${varNum}`;
                        })]
                    };
                }
            } else if (component.type === 'FOOTER') {
                fbComponent.text = component.text || '';
            } else if (component.type === 'BUTTONS') {
                if (component.buttons && component.buttons.length > 0) {
                    fbComponent.buttons = component.buttons.map(button => {
                        const fbButton = {
                            type: (button.type || 'QUICK_REPLY').toUpperCase(),
                            text: button.text || ''
                        };

                        if (button.type === 'URL' && button.url) {
                            fbButton.url = button.url;
                            // If URL contains variables, add example
                            if (button.url.includes('{{')) {
                                fbButton.example = ['https://example.com/sample'];
                            }
                        } else if (button.type === 'PHONE_NUMBER' && button.phoneNumber) {
                            fbButton.phone_number = button.phoneNumber;
                        }

                        return fbButton;
                    });
                }
            }

            return fbComponent;
        });

        // Prepare variable mappings for database
        const variableMappings = {};
        Object.keys(templateVariables).forEach(varNum => {
            const variable = templateVariables[varNum];
            if (variable && variable.type) {
                // Determine component type (currently only BODY has variables in this code)
                const componentType = 'BODY';
                
                if (!variableMappings[componentType]) {
                    variableMappings[componentType] = {};
                }
                
                variableMappings[componentType][varNum] = {
                    variableType: variable.type,
                    variableLabel: availableVariables[variable.type]?.label || ''
                };
            }
        });

        // Extract media URL from HEADER component if present
        let mediaUrl = null;
        const headerComp = templateComponents.find(c => c.type === 'HEADER');
        if (headerComp && headerComp.format && headerComp.format !== 'TEXT' && headerComp.format !== 'NONE') {
            mediaUrl = headerComp.text;
        }

        const formData = {
            name: templateName,
            category: $('input[name="category"]:checked').val(),
            language: $('#createTemplateLanguage').val(),
            moduleType: selectedModuleType,  // Use globally selected module type
            mediaUrl: mediaUrl, // NEW: Send original media URL/Handle
            components: facebookComponents,
            variableMappings: variableMappings
        };

        // Debug: Log the data being sent
        console.log('=== TEMPLATE DEBUG INFO ===');
        console.log('Template Name:', templateName);
        console.log('Template Components:', templateComponents);

        // Debug each component in detail
        templateComponents.forEach((comp, index) => {
            console.log(`Component ${index}:`, {
                id: comp.id,
                type: comp.type,
                text: comp.text,
                format: comp.format,
                buttons: comp.buttons
            });

            // Special debug for HEADER components
            if (comp.type === 'HEADER') {
                console.log(`HEADER Component Debug:`, {
                    format: comp.format,
                    text: comp.text,
                    formatType: typeof comp.format,
                    textType: typeof comp.text
                });
            }
        });

        console.log('Facebook Components:', facebookComponents);
        console.log('Final Form Data:', JSON.stringify(formData, null, 2));
        console.log('=== END DEBUG INFO ===');

        showLoading();

        $.ajax({
            url: '/WhatsAppTemplate/CreateTemplate',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            }
        })
        .done(function(response) {
            if (response.success) {
                showNotification(response.message || 'WhatsApp şablonu Facebook\'a gönderildi. Onay sürecini bekleyin.', 'success');
                hideTemplateModal();

                // Refresh page after a short delay
                setTimeout(() => {
                    window.location.reload();
                }, 1500);
            } else {
                showNotification(response.message || 'Şablon oluşturulurken hata oluştu.', 'error');
            }
        })
        .fail(function(xhr, status, error) {
            console.error("AJAX error:", xhr, status, error);
            showNotification('Şablon oluşturulurken hata oluştu.', 'error');
        })
        .always(function() {
            hideLoading();
        });
    });

    // Delete template
    function deleteTemplate(templateName) {
        showConfirmationDialog('Sil', 'Bu şablonu silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.', 'Sil', 'İptal', () => {
            showLoading();

            $.ajax({
                url: '/WhatsAppTemplate/DeleteTemplate',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    templateName: templateName
                }),
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                }
            })
            .done(function(response) {
                if (response.success) {
                    showNotification(response.message || 'WhatsApp şablonu başarıyla silindi.', 'success');

                    // Refresh page after a short delay
                    setTimeout(() => {
                        window.location.reload();
                    }, 1500);
                } else {
                    console.error("Delete template error:", response);
                    showNotification(response.message || "Şablon silinirken hata oluştu.", "error");
                }
            })
            .fail(function(xhr, status, error) {
                console.error("AJAX error:", xhr, status, error);
                showNotification('Şablon silinirken hata oluştu.', 'error');
            })
            .always(function() {
                hideLoading();
            });
        });
    }

    // Show/hide modal
    function showTemplateModal() {
        $('#templateModal').removeClass('hidden');
        $('body').addClass('overflow-hidden');

        // Show create form, hide view form
        $('#createTemplateForm').removeClass('hidden');
        $('#viewTemplateForm').addClass('hidden');

        // Update modal title
        $('#templateModalLabel span').text('Yeni WhatsApp Şablon');
    }

    function hideTemplateModal() {
        $('#templateModal').addClass('hidden');
        $('body').removeClass('overflow-hidden');
    }

    // Loading and toast functions
    function showLoading() {
        BusyIndicator.show(null, { text: 'İşlem yapılıyor...' });
    }

    function hideLoading() {
        BusyIndicator.hide(null);
    }



    // Component Builder Functions - REMOVED (Replaced by Fixed Form Logic)



    // Variable Management
    function extractVariablesFromText(text) {
        const variableRegex = /\{\{(\d+)\}\}/g;
        const foundVariables = new Set();
        let match;

        while ((match = variableRegex.exec(text)) !== null) {
            foundVariables.add(parseInt(match[1]));
        }

        // Update templateVariables
        const sortedVariables = Array.from(foundVariables).sort((a, b) => a - b);

        // Remove variables that are no longer used
        Object.keys(templateVariables).forEach(key => {
            if (!sortedVariables.includes(parseInt(key))) {
                delete templateVariables[key];
            }
        });

        // Add new variables
        sortedVariables.forEach(varNum => {
            if (!templateVariables[varNum]) {
                templateVariables[varNum] = {
                    name: `Değişken ${varNum}`,
                    description: '',
                    type: '', // Will be selected by user
                    example: ''
                };
            }
        });

        renderVariableDefinitions();
    }

    function renderVariableDefinitions() {
        const $container = $('#variableDefinitions');
        const $wrapper = $('#variableDefinitionsContainer');
        $container.empty();

        const sortedVars = Object.keys(templateVariables).sort((a, b) => parseInt(a) - parseInt(b));

        // Always show the wrapper
        $wrapper.removeClass('hidden');

        if (sortedVars.length === 0) {
            $container.html('<div class="text-sm text-gray-400 italic text-center py-2 bg-gray-50/50 rounded border border-gray-100/50">Şablon metninde henüz değişken ({{1}}, {{2}} vb.) bulunmuyor.</div>');
            return;
        }

        sortedVars.forEach(varNum => {
            const variable = templateVariables[varNum];
            const html = `
                <div class="variable-definition border border-gray-200 rounded p-3 mb-2" data-var-num="${varNum}">
                    <div class="flex items-center justify-between mb-2">
                        <span class="text-sm font-medium text-gray-700">{{${varNum}}}</span>
                        <span class="text-xs text-gray-500">Değişken ${varNum}</span>
                    </div>
                    <div class="space-y-2">
                        <select class="variable-type w-full px-2 py-1 text-sm border border-gray-300 rounded ${!variable.type ? 'border-red-300 bg-red-50' : ''}" data-var-num="${varNum}">
                            <option value="">Değişken türü seçin *</option>
                            ${generateVariableOptions(variable.type)}
                        </select>
                        <div class="variable-preview text-xs text-gray-600 bg-gray-50 px-2 py-1 rounded" style="display: ${variable.type ? 'block' : 'none'}">
                            Örnek: <span class="font-medium">${variable.type ? predefinedVariables[variable.type]?.example || '' : ''}</span>
                        </div>
                    </div>
                </div>
            `;
            $container.append(html);
        });

        // Bind events for variable type selection
        bindVariableEvents();
    }

    function generateVariableOptions(selectedType) {
        // Use the globally selected module type
        const availableVariables = getAvailableVariablesForModule(selectedModuleType);
        
        let options = '';

        // Sort by order
        const sortedVariables = Object.entries(availableVariables)
            .sort(([, a], [, b]) => a.order - b.order);

        sortedVariables.forEach(([key, variable]) => {
            const selected = selectedType === key ? 'selected' : '';
            options += `<option value="${key}" ${selected}>${variable.label}</option>`;
        });

        return options;
    }

    function bindVariableEvents() {
        $('.variable-type').off('change').on('change', function() {
            const varNum = $(this).data('var-num');
            const selectedType = $(this).val();
            const $definition = $(this).closest('.variable-definition');
            const $preview = $definition.find('.variable-preview');
            const $select = $(this);

            // Update template variable
            if (templateVariables[varNum]) {
                templateVariables[varNum].type = selectedType;
                templateVariables[varNum].description = selectedType ? predefinedVariables[selectedType]?.label || '' : '';
            }

            // Update select styling based on selection
            if (selectedType) {
                $select.removeClass('border-red-300 bg-red-50').addClass('border-green-300 bg-green-50');
            } else {
                $select.removeClass('border-green-300 bg-green-50').addClass('border-red-300 bg-red-50');
            }

            // Update preview
            if (selectedType && predefinedVariables[selectedType]) {
                $preview.find('span').text(predefinedVariables[selectedType].example);
                $preview.show();
            } else {
                $preview.hide();
            }

            // Update WhatsApp preview
            updatePreview();

            // Trigger validation to check if all variables are selected
            validateTemplate();
        });
    }

    // WhatsApp Preview Functions
    function updatePreview() {
        const $preview = $('#whatsappPreview');
        let previewHtml = '';

        if (templateComponents.length === 0) {
            $preview.html('<div class="text-center text-gray-500 text-sm">Şablon bileşenlerini ekleyin</div>');
            return;
        }

        // Sort components in WhatsApp order: HEADER, BODY, FOOTER, BUTTONS
        const sortedComponents = [...templateComponents].sort((a, b) => {
            const order = { 'HEADER': 1, 'BODY': 2, 'FOOTER': 3, 'BUTTONS': 4 };
            return (order[a.type] || 5) - (order[b.type] || 5);
        });

        sortedComponents.forEach(component => {
            previewHtml += createPreviewComponentHtml(component);
        });

        $preview.html(previewHtml);
    }

    function createPreviewComponentHtml(component) {
        let html = '';

        if (component.type === 'HEADER') {
            if (component.format === 'TEXT' && component.text) {
                html += `<div class="font-semibold text-sm text-gray-900 mb-2">${escapeHtml(component.text)}</div>`;
            } else if (component.format !== 'TEXT' && component.text) {
                if (component.format === 'IMAGE' && (component.text.startsWith('http') || component.text.startsWith('data:image'))) {
                    html += `<div class="mb-2"><img src="${component.text}" class="w-full rounded-lg object-cover max-h-48" onerror="this.onerror=null;this.parentNode.innerHTML='<div class=\\'bg-gray-200 rounded p-2 text-center text-xs text-gray-600\\'>Görsel Yüklenemedi</div>'"></div>`;
                } else if (component.format === 'VIDEO' && (component.text.startsWith('http') || component.text.startsWith('data:video'))) {
                    html += `<div class="mb-2"><video src="${component.text}" controls class="w-full rounded-lg max-h-48"></video></div>`;
                } else {
                    html += `<div class="mb-2">
                    <div class="bg-gray-200 rounded p-2 text-center text-xs text-gray-600">
                        ${component.format} Media<br>
                        <span class="text-xs">${escapeHtml(component.text)}</span>
                    </div>
                </div>`;
                }
            }
        } else if (component.type === 'BODY') {
            if (component.text) {
                let formattedText = escapeHtml(component.text);
                
                // Apply WhatsApp formatting
                // Bold: *text* -> <strong>text</strong>
                formattedText = formattedText.replace(/\*([^*]+)\*/g, '<strong>$1</strong>');
                
                // Italic: _text_ -> <em>text</em>
                formattedText = formattedText.replace(/_([^_]+)_/g, '<em>$1</em>');
                
                // Strikethrough: ~text~ -> <del>text</del>
                formattedText = formattedText.replace(/~([^~]+)~/g, '<del>$1</del>');
                
                // Code: ```text``` -> <code>text</code>
                formattedText = formattedText.replace(/```([^`]+)```/g, '<code class="bg-gray-100 px-1 rounded font-mono text-xs">$1</code>');
                
                html += `<div class="text-sm text-gray-800 mb-2 whitespace-pre-wrap">${formattedText}</div>`;
            }
        } else if (component.type === 'FOOTER') {
            if (component.text) {
                html += `<div class="text-xs text-gray-600 mt-2">${escapeHtml(component.text)}</div>`;
            }
        } else if (component.type === 'BUTTONS') {
            if (component.buttons && component.buttons.length > 0) {
                html += '<div class="mt-3 space-y-1">';
                component.buttons.forEach(button => {
                    if (button && button.text) {
                        let buttonClass = 'block w-full text-center py-2 px-3 text-sm border rounded';
                        if (button.type === 'URL') {
                            buttonClass += ' bg-blue-50 border-blue-200 text-sky-700';
                        } else if (button.type === 'PHONE_NUMBER') {
                            buttonClass += ' bg-green-50 border-green-200 text-emerald-700';
                        } else {
                            buttonClass += ' bg-gray-50 border-gray-200 text-gray-800';
                        }
                        html += `<div class="${buttonClass}">${escapeHtml(button.text)}</div>`;
                    }
                });
                html += '</div>';
            }
        }

        return html;
    }

    function updatePreviewWithSampleData() {
        // Generate sample data based on selected variable types
        const sampleData = {};
        Object.keys(templateVariables).forEach(varNum => {
            const variable = templateVariables[varNum];
            if (variable && variable.type && predefinedVariables[variable.type]) {
                sampleData[varNum] = predefinedVariables[variable.type].example;
            } else {
                // Fallback to default values
                const fallbackData = {
                    '1': 'Ahmet Yılmaz',
                    '2': 'ORD-2024-001',
                    '3': '₺299,90',
                    '4': 'Kadıköy, İstanbul',
                    '5': '15.01.2024',
                    '6': 'ABC123'
                };
                sampleData[varNum] = fallbackData[varNum] || `Örnek Değer ${varNum}`;
            }
        });

        const $preview = $('#whatsappPreview');
        let previewHtml = '';

        if (templateComponents.length === 0) {
            $preview.html('<div class="text-center text-gray-500 text-sm">Şablon bileşenlerini ekleyin</div>');
            return;
        }

        const sortedComponents = [...templateComponents].sort((a, b) => {
            const order = { 'HEADER': 1, 'BODY': 2, 'FOOTER': 3, 'BUTTONS': 4 };
            return (order[a.type] || 5) - (order[b.type] || 5);
        });

        sortedComponents.forEach(component => {
            let componentText = component.text || '';

            // Replace variables with sample data
            if (component.type === 'BODY') {
                Object.keys(sampleData).forEach(varNum => {
                    componentText = componentText.replace(new RegExp(`\\{\\{${varNum}\\}\\}`, 'g'), sampleData[varNum]);
                });
            }

            const tempComponent = { ...component, text: componentText };
            previewHtml += createPreviewComponentHtml(tempComponent);
        });

        $preview.html(previewHtml);
    }

    // Template Validation - Facebook API Requirements
    function validateTemplate() {
        const validationMessages = [];

        // Check if template has required components
        const hasBody = templateComponents.some(c => c.type === 'BODY');
        if (!hasBody) {
            validationMessages.push('Şablon en az bir BODY bileşeni içermelidir');
        }

        // Validate body text
        const bodyComponent = templateComponents.find(c => c.type === 'BODY');
        if (bodyComponent) {
            if (!bodyComponent.text || !bodyComponent.text.trim()) {
                validationMessages.push('BODY bileşeni boş olamaz');
            } else {
                // Facebook API Variable Validation Rules
                const bodyText = bodyComponent.text;

                // Check for dangling parameters (template cannot start or end with a variable)
                if (/^\s*\{\{\d+\}\}/.test(bodyText)) {
                    validationMessages.push('Şablon metni değişken ile başlayamaz (örn: {{1}} ile başlayamaz)');
                }
                if (/\{\{\d+\}\}\s*$/.test(bodyText)) {
                    validationMessages.push('Şablon metni değişken ile bitemez (örn: {{1}} ile bitemez)');
                }

                // Check for adjacent parameters (two variables cannot be next to each other)
                if (/\{\{\d+\}\}\s*\{\{\d+\}\}/.test(bodyText)) {
                    validationMessages.push('İki değişken yan yana olamaz (örn: {{1}}{{2}} geçersiz)');
                }

                // Check for sequential variable numbering
                const variableMatches = bodyText.match(/\{\{(\d+)\}\}/g);
                if (variableMatches) {
                    const varNumbers = [...new Set(variableMatches.map(m => parseInt(m.replace(/[{}]/g, ''))))].sort((a, b) => a - b);

                    // Variables must start from 1
                    if (varNumbers.length > 0 && varNumbers[0] !== 1) {
                        validationMessages.push('Değişkenler 1\'den başlamalıdır ({{1}}, {{2}}, ...)');
                    }

                    // Variables must be sequential (no gaps)
                    for (let i = 0; i < varNumbers.length - 1; i++) {
                        if (varNumbers[i + 1] - varNumbers[i] !== 1) {
                            validationMessages.push(`Değişkenler sıralı olmalıdır. {{${varNumbers[i]}}} ve {{${varNumbers[i + 1]}}} arasında boşluk var`);
                            break;
                        }
                    }
                }

                // Check for special characters in variable placeholders (not allowed: #, $, %)
                if (/\{\{[^}]*[#$%][^}]*\}\}/.test(bodyText)) {
                    validationMessages.push('Değişken içinde özel karakterler (#, $, %) kullanılamaz');
                }

                // Check body text length (max 1024 characters)
                if (bodyText.length > 1024) {
                    validationMessages.push(`BODY metni en fazla 1024 karakter olabilir (şu an: ${bodyText.length})`);
                }
            }
        }

        // Validate header
        const headerComponent = templateComponents.find(c => c.type === 'HEADER');
        if (headerComponent) {
            if (headerComponent.format === 'TEXT') {
                if (!headerComponent.text || !headerComponent.text.trim()) {
                    validationMessages.push('TEXT formatındaki HEADER boş olamaz');
                } else if (headerComponent.text.length > 60) {
                    validationMessages.push(`HEADER metni en fazla 60 karakter olabilir (şu an: ${headerComponent.text.length})`);
                }
            } else if (headerComponent.format !== 'TEXT' && (!headerComponent.text || !headerComponent.text.trim())) {
                validationMessages.push('Medya HEADER için geçerli URL gereklidir');
            }
        }

        // Validate footer
        const footerComponent = templateComponents.find(c => c.type === 'FOOTER');
        if (footerComponent && footerComponent.text && footerComponent.text.length > 60) {
            validationMessages.push(`FOOTER metni en fazla 60 karakter olabilir (şu an: ${footerComponent.text.length})`);
        }

        // Validate buttons
        const buttonsComponent = templateComponents.find(c => c.type === 'BUTTONS');
        if (buttonsComponent) {
            if (!buttonsComponent.buttons || buttonsComponent.buttons.length === 0) {
                validationMessages.push('BUTTONS bileşeni en az bir buton içermelidir');
            } else {
                // Max 10 buttons total
                if (buttonsComponent.buttons.length > 10) {
                    validationMessages.push('En fazla 10 buton eklenebilir');
                }

                // Count button types
                let urlCount = 0, phoneCount = 0, quickReplyCount = 0;

                buttonsComponent.buttons.forEach((button, index) => {
                    if (!button.text || !button.text.trim()) {
                        validationMessages.push(`Buton ${index + 1} metni boş olamaz`);
                    } else if (button.text.length > 25) {
                        validationMessages.push(`Buton ${index + 1} metni en fazla 25 karakter olabilir`);
                    }

                    if (button.type === 'URL') {
                        urlCount++;
                        if (!button.url || !button.url.trim()) {
                            validationMessages.push(`Buton ${index + 1} için geçerli URL gereklidir`);
                        } else if (button.url.length > 2000) {
                            validationMessages.push(`Buton ${index + 1} URL'si en fazla 2000 karakter olabilir`);
                        }
                    } else if (button.type === 'PHONE_NUMBER') {
                        phoneCount++;
                        if (!button.phoneNumber || !button.phoneNumber.trim()) {
                            validationMessages.push(`Buton ${index + 1} için geçerli telefon numarası gereklidir`);
                        } else if (button.phoneNumber.length > 20) {
                            validationMessages.push(`Buton ${index + 1} telefon numarası en fazla 20 karakter olabilir`);
                        }
                    } else if (button.type === 'QUICK_REPLY') {
                        quickReplyCount++;
                    }
                });

                // Max 2 URL buttons
                if (urlCount > 2) {
                    validationMessages.push('En fazla 2 URL butonu eklenebilir');
                }

                // Max 1 phone number button
                if (phoneCount > 1) {
                    validationMessages.push('En fazla 1 telefon numarası butonu eklenebilir');
                }

                // Max 10 quick reply buttons
                if (quickReplyCount > 10) {
                    validationMessages.push('En fazla 10 hızlı yanıt butonu eklenebilir');
                }
            }
        }

        // Validate template name
        const templateName = $('#createTemplateName').val();
        if (!templateName || !templateName.trim()) {
            validationMessages.push('Şablon adı gereklidir');
        } else if (!/^[a-z0-9_]+$/.test(templateName)) {
            validationMessages.push('Şablon adı sadece küçük harf, rakam ve alt çizgi içerebilir');
        } else if (templateName.length > 512) {
            validationMessages.push('Şablon adı en fazla 512 karakter olabilir');
        }

        // Validate variable selections
        const variableErrors = validateVariableSelections();
        validationMessages.push(...variableErrors);

        // Show/hide validation messages
        const $validation = $('#templateValidation');
        const $messages = $('#validationMessages');

        if (validationMessages.length > 0) {
            $messages.empty();
            validationMessages.forEach(message => {
                $messages.append(`<li>${message}</li>`);
            });
            $validation.removeClass('hidden');
        } else {
            $validation.addClass('hidden');
        }

        return validationMessages.length === 0;
    }

    function validateVariableSelections() {
        const errors = [];

        // Check if there are any variables defined
        const variableCount = Object.keys(templateVariables).length;
        if (variableCount === 0) {
            return errors; // No variables, no validation needed
        }

        // Check each variable has a type selected
        Object.keys(templateVariables).forEach(varNum => {
            const variable = templateVariables[varNum];
            if (!variable.type || variable.type.trim() === '') {
                errors.push(`{{${varNum}}} değişkeni için tür seçimi yapılmalıdır`);
            }
        });

        return errors;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Media Upload Functions
    function showMediaUploadModal() {
        $('#mediaUploadModal').removeClass('hidden');
        $('#mediaFileInput').val('');
        $('#mediaPreview').addClass('hidden');
        $('#uploadMediaBtn').prop('disabled', true);
        $('body').addClass('overflow-hidden');
    }

    function hideMediaUploadModal() {
        $('#mediaUploadModal').addClass('hidden');
        currentMediaComponentId = null;
        $('body').removeClass('overflow-hidden');
    }

    function handleFileSelection(input) {
        const file = input.files[0];
        if (!file) {
            $('#mediaPreview').addClass('hidden');
            $('#uploadMediaBtn').prop('disabled', true);
            return;
        }

        // Validate file
        const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'video/mp4', 'application/pdf'];
        if (!allowedTypes.includes(file.type)) {
            showNotification('Desteklenmeyen dosya türü.', 'error');
            input.value = '';
            return;
        }

        // Size validation
        const maxSize = file.type.startsWith('image/') ? 5 * 1024 * 1024 : // 5MB for images
                       file.type.startsWith('video/') ? 16 * 1024 * 1024 : // 16MB for videos
                       100 * 1024 * 1024; // 100MB for documents

        if (file.size > maxSize) {
            const maxSizeMB = Math.round(maxSize / (1024 * 1024));
            showNotification(`Dosya boyutu ${maxSizeMB}MB'dan büyük olamaz.`, 'error');
            input.value = '';
            return;
        }

        // Show preview
        showFilePreview(file);
        $('#uploadMediaBtn').prop('disabled', false);
    }

    function showFilePreview(file) {
        const $preview = $('#mediaPreviewContent');
        $preview.empty();

        if (file.type.startsWith('image/')) {
            const reader = new FileReader();
            reader.onload = function(e) {
                $preview.html(`
                    <img src="${e.target.result}" alt="Preview" class="max-w-full max-h-32 mx-auto rounded">
                    <p class="text-xs text-gray-600 mt-2">${file.name} (${formatFileSize(file.size)})</p>
                `);
            };
            reader.readAsDataURL(file);
        } else if (file.type.startsWith('video/')) {
            $preview.html(`
                <div class="bg-gray-100 rounded p-4">
                    <svg class="w-12 h-12 mx-auto text-gray-400" fill="currentColor" viewBox="0 0 20 20">
                        <path d="M2 6a2 2 0 012-2h6a2 2 0 012 2v8a2 2 0 01-2 2H4a2 2 0 01-2-2V6zM14.553 7.106A1 1 0 0014 8v4a1 1 0 00.553.894l2 1A1 1 0 0018 13V7a1 1 0 00-1.447-.894l-2 1z"></path>
                    </svg>
                    <p class="text-sm text-gray-700 mt-2">${file.name}</p>
                    <p class="text-xs text-gray-600">${formatFileSize(file.size)}</p>
                </div>
            `);
        } else {
            $preview.html(`
                <div class="bg-gray-100 rounded p-4">
                    <svg class="w-12 h-12 mx-auto text-gray-400" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4zm2 6a1 1 0 011-1h6a1 1 0 110 2H7a1 1 0 01-1-1zm1 3a1 1 0 100 2h6a1 1 0 100-2H7z" clip-rule="evenodd"></path>
                    </svg>
                    <p class="text-sm text-gray-700 mt-2">${file.name}</p>
                    <p class="text-xs text-gray-600">${formatFileSize(file.size)}</p>
                </div>
            `);
        }

        $('#mediaPreview').removeClass('hidden');
    }

    function uploadSelectedMedia() {
        const fileInput = document.getElementById('mediaFileInput');
        const file = fileInput.files[0];

        if (!file) {
            showNotification('Dosya seçilmedi.', 'error');
            return;
        }

        const formData = new FormData();
        formData.append('file', file);

        showLoading();

        $.ajax({
            url: '/WhatsAppTemplate/UploadMedia',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            }
        })
        .done(function(response) {
            if (response.success) {
                const mediaId = response.mediaId;

                // Update the component with the Gupshup media handle ID
                if (currentMediaComponentId === 'HEADER_MEDIA') {
                    // Store the media handle ID for template creation
                    $('#headerMediaPreview').data('media-id', mediaId);
                    
                    $('#headerMediaPreview').html(`
                        <div class="flex items-center justify-between">
                            <span class="truncate flex-1">${response.fileName}</span>
                            <span class="text-primary ml-2 text-xs">✓ Yüklendi</span>
                        </div>
                    `).removeClass('hidden');
                    
                    updateTemplateData();
                } else if (currentMediaComponentId) {
                    const component = templateComponents.find(c => c.id === currentMediaComponentId);
                    if (component) {
                        component.text = fullUrl;

                        // Update the input field
                        const $component = $(`.component-builder[data-component-id="${currentMediaComponentId}"]`);
                        $component.find('.component-text').val(component.text);

                        updatePreview();
                    }
                }

                showNotification('Dosya başarıyla yüklendi.', 'success');
                hideMediaUploadModal();
            } else {
                showNotification(response.message || 'Dosya yüklenirken hata oluştu.', 'error');
            }
        })
        .fail(function(xhr, status, error) {
            console.error("AJAX error:", xhr, status, error);
            showNotification('Dosya yüklenirken hata oluştu.', 'error');
        })
        .always(function() {
            hideLoading();
        });
    }

    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    // Sample Templates Functions
    function showSampleTemplatesModal() {
        $('#sampleTemplatesModal').removeClass('hidden');
        $('body').addClass('overflow-hidden');
        loadSampleTemplates();
    }

    function hideSampleTemplatesModal() {
        $('#sampleTemplatesModal').addClass('hidden');
        $('body').removeClass('overflow-hidden');
    }



    function loadSampleTemplates() {
        showLoading();

        $.get('/WhatsAppTemplate/GetSampleTemplates')
            .done(function(response) {
                if (response.success) {
                    renderSampleTemplates(response.templates);
                } else {
                    showNotification(response.message || 'Hazır şablonlar yüklenirken hata oluştu.', 'error');
                }
            })
            .fail(function(xhr, status, error) {
                console.error("AJAX error:", xhr, status, error);
                showNotification('Hazır şablonlar yüklenirken hata oluştu.', 'error');
            })
            .always(function() {
                hideLoading();
            });
    }

    function renderSampleTemplates(templates) {
        const $container = $('#sampleTemplatesContainer');
        $container.empty();

        if (!templates || templates.length === 0) {
            $container.html('<div class="col-span-full text-center text-gray-500 py-8">Hazır şablon bulunamadı.</div>');
            return;
        }

        templates.forEach(template => {
            const categoryColor = getCategoryColor(template.category);
            const componentCount = template.components ? template.components.length : 0;

            const templateHtml = `
                <div class="sample-template-card border border-gray-200 rounded-xl p-4 hover:border-primary-200 hover:shadow-[0_2px_8px_rgba(0,0,0,0.05)] transition-all cursor-pointer"
                     data-sample-id="${template.id}">
                    <div class="flex items-start justify-between mb-3">
                        <div>
                            <h4 class="font-medium text-gray-900 text-sm">${escapeHtml(template.name)}</h4>
                            <p class="text-xs text-gray-600 mt-1">${escapeHtml(template.description)}</p>
                        </div>
                        <span class="text-xs font-medium px-2 py-1 rounded-full ${categoryColor}">
                            ${template.category}
                        </span>
                    </div>

                    <div class="mb-3">
                        <div class="text-xs text-gray-500 mb-2">${componentCount} bileşen</div>
                        <div class="space-y-1">
                            ${template.components.map(comp => `
                                <div class="text-xs px-2 py-1 bg-gray-100 rounded inline-block mr-1">
                                    ${comp.type}
                                </div>
                            `).join('')}
                        </div>
                    </div>

                    <div class="flex items-center justify-between">
                        <span class="text-xs text-gray-500">${template.language.toUpperCase()}</span>
                        <button class="use-sample-btn text-xs bg-primary text-white px-3 py-1 rounded hover:bg-primary-dark"
                                data-sample-id="${template.id}">
                            Düzenle
                        </button>
                    </div>
                </div>
            `;

            $container.append(templateHtml);
        });

        // Bind click events
        $('.use-sample-btn').on('click', function(e) {
            e.stopPropagation();
            const sampleId = $(this).data('sample-id');
            loadSampleToBuilder(sampleId);
        });

        $('.sample-template-card').on('click', function() {
            const sampleId = $(this).data('sample-id');
            loadSampleToBuilder(sampleId);
        });
    }

    function getCategoryColor(category) {
        switch (category) {
            case 'MARKETING':
                return 'bg-violet-50 text-violet-700';
            case 'UTILITY':
                return 'bg-sky-50 text-sky-700';
            case 'AUTHENTICATION':
                return 'bg-emerald-50 text-emerald-700';
            default:
                return 'bg-gray-100 text-gray-800';
        }
    }

    function loadSampleToBuilder(sampleId) {
        hideSampleTemplatesModal();

        showLoading();

        $.get(`/WhatsAppTemplate/GetSampleTemplate?id=${sampleId}`)
            .done(function(response) {
                if (response.success) {
                    loadSampleIntoComponentBuilder(response.template);
                    showTemplateModal(); // Show the main create template modal
                } else {
                    showNotification(response.message || 'Şablon yüklenirken hata oluştu.', 'error');
                }
            })
            .fail(function(xhr, status, error) {
                console.error("AJAX error:", xhr, status, error);
                showNotification('Şablon yüklenirken hata oluştu.', 'error');
            })
            .always(function() {
                hideLoading();
            });
    }

    function showCreateFromSampleModal(sampleId) {
        hideSampleTemplatesModal();

        showLoading();

        $.get(`/WhatsAppTemplate/GetSampleTemplate?id=${sampleId}`)
            .done(function(response) {
                if (response.success) {
                    populateCreateFromSampleModal(response.template);
                    $('#createFromSampleModal').removeClass('hidden');
                    $('body').addClass('overflow-hidden');
                } else {
                    showNotification(response.message || 'Şablon yüklenirken hata oluştu.', 'error');
                }
            })
            .fail(function(xhr, status, error) {
                console.error("AJAX error:", xhr, status, error);
                showNotification('Şablon yüklenirken hata oluştu.', 'error');
            })
            .always(function() {
                hideLoading();
            });
    }

    function populateCreateFromSampleModal(template) {
        $('#selectedSampleId').val(template.id);
        $('#selectedSampleName').text(template.name);
        $('#sampleTemplateName').val('');

        // Generate preview
        const previewHtml = generateSamplePreview(template);
        $('#samplePreviewContainer').html(previewHtml);
    }

    function generateSamplePreview(template) {
        let html = '<div class="bg-white rounded-lg shadow-sm max-w-xs mx-auto border">';

        // WhatsApp Header
        html += `
            <div class="bg-primary text-white px-4 py-2 rounded-t-lg">
                <div class="flex items-center space-x-2">
                    <div class="w-8 h-8 bg-white bg-opacity-20 rounded-full flex items-center justify-center">
                        <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                            <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893A11.821 11.821 0 0020.885 3.488"/>
                        </svg>
                    </div>
                    <span class="text-sm font-medium">Şirket Adı</span>
                </div>
            </div>
        `;

        // Message Content
        html += '<div class="p-4">';

        if (template.components) {
            const sortedComponents = [...template.components].sort((a, b) => {
                const order = { 'HEADER': 1, 'BODY': 2, 'FOOTER': 3, 'BUTTONS': 4 };
                return (order[a.type] || 5) - (order[b.type] || 5);
            });

            sortedComponents.forEach(component => {
                if (component.type === 'HEADER') {
                    if (component.format === 'TEXT' && component.text) {
                        html += `<div class="font-semibold text-sm text-gray-900 mb-2">${escapeHtml(component.text)}</div>`;
                    } else if (component.format !== 'TEXT' && component.text) {
                        html += `<div class="mb-2">
                            <div class="bg-gray-200 rounded p-2 text-center text-xs text-gray-600">
                                ${component.format} Media<br>
                                <span class="text-xs">${component.text}</span>
                            </div>
                        </div>`;
                    }
                } else if (component.type === 'BODY') {
                    if (component.text) {
                        // Replace variables with sample data
                        let bodyText = component.text;
                        bodyText = bodyText.replace(/\{\{1\}\}/g, 'Ahmet Yılmaz');
                        bodyText = bodyText.replace(/\{\{2\}\}/g, 'ORD-2024-001');
                        bodyText = bodyText.replace(/\{\{3\}\}/g, '₺299,90');
                        bodyText = bodyText.replace(/\{\{4\}\}/g, '15.01.2024');
                        bodyText = bodyText.replace(/\{\{5\}\}/g, 'Kadıköy, İstanbul');
                        bodyText = bodyText.replace(/\{\{6\}\}/g, 'ABC123');

                        html += `<div class="text-sm text-gray-800 mb-2 whitespace-pre-wrap">${escapeHtml(bodyText)}</div>`;
                    }
                } else if (component.type === 'FOOTER') {
                    if (component.text) {
                        html += `<div class="text-xs text-gray-600 mt-2">${escapeHtml(component.text)}</div>`;
                    }
                } else if (component.type === 'BUTTONS') {
                    if (component.buttons && component.buttons.length > 0) {
                        html += '<div class="mt-3 space-y-1">';
                        component.buttons.forEach(button => {
                            if (button.text) {
                                let buttonClass = 'block w-full text-center py-2 px-3 text-sm border rounded';
                                if (button.type === 'URL') {
                                    buttonClass += ' bg-blue-50 border-blue-200 text-sky-700';
                                } else if (button.type === 'PHONE_NUMBER') {
                                    buttonClass += ' bg-green-50 border-green-200 text-emerald-700';
                                } else {
                                    buttonClass += ' bg-gray-50 border-gray-200 text-gray-800';
                                }
                                html += `<div class="${buttonClass}">${escapeHtml(button.text)}</div>`;
                            }
                        });
                        html += '</div>';
                    }
                }
            });
        }

        html += '</div></div>';
        return html;
    }



    // Template View Functions
    function convertTemplateToComponents(template) {
        templateComponents = [];
        templateVariables = {};

        if (template.components) {
            template.components.forEach((comp, index) => {
                const component = {
                    id: `view_component_${index}`,
                    type: comp.type,
                    text: comp.text || '',
                    format: comp.format || 'TEXT'
                };

                // Handle HEADER with media (IMAGE, VIDEO, DOCUMENT)
                if (comp.type === 'HEADER' && comp.format && comp.format !== 'TEXT') {
                    // For media headers, the URL might be in example.header_handle
                    if (comp.example && comp.example.header_handle && comp.example.header_handle.length > 0) {
                        component.text = comp.example.header_handle[0];
                    }
                }

                if (comp.type === 'BUTTONS' && comp.buttons) {
                    component.buttons = comp.buttons.map(btn => ({
                        type: btn.type,
                        text: btn.text,
                        url: btn.url,
                        // Handle both snake_case (from Facebook API) and camelCase
                        phoneNumber: btn.phoneNumber || btn.phone_number || btn['phone_number']
                    }));
                }

                templateComponents.push(component);
            });
        }

        console.log('Converted template components:', templateComponents);
    }

    function populateViewComponents(components) {
        const container = $('#viewComponentsBuilder');
        container.empty();

        if (!components || components.length === 0) {
            container.html('<p class="text-gray-500 text-sm">Bu şablonda component bulunmuyor.</p>');
            return;
        }

        components.forEach(component => {
            const componentHtml = createViewComponentBuilderHtml(component);
            container.append(componentHtml);
        });
    }

    function getComponentIcon(type) {
        switch (type) {
            case 'HEADER':
                return '<svg class="w-4 h-4 text-purple-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7"></path></svg>';
            case 'BODY':
                return '<svg class="w-4 h-4 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h7"></path></svg>';
            case 'FOOTER':
                return '<svg class="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path></svg>';
            case 'BUTTONS':
                return '<svg class="w-4 h-4 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 15l-2 5L9 9l11 4-5 2zm0 0l5 5M7.188 2.239l.777 2.897M5.136 7.965l-2.898-.777M13.95 4.05l-2.122 2.122m-5.657 5.656l-2.12 2.122"></path></svg>';
            default:
                return '<svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"></path></svg>';
        }
    }

    function createViewComponentBuilderHtml(component) {
        let html = `<div class="component-builder-view border border-gray-200 rounded-lg p-3 bg-gray-50" data-component-id="${component.id}">`;

        // Component header
        html += `<div class="flex items-center justify-between mb-3">
            <div class="flex items-center space-x-2">
                <span class="text-sm font-medium text-gray-700">${component.type}</span>
                ${getComponentIcon(component.type)}
            </div>
            <span class="text-xs text-gray-500">Sadece Görüntüleme</span>
        </div>`;

        // Component-specific content (read-only)
        if (component.type === 'HEADER') {
            html += createViewHeaderComponentHtml(component);
        } else if (component.type === 'BODY') {
            html += createViewBodyComponentHtml(component);
        } else if (component.type === 'FOOTER') {
            html += createViewFooterComponentHtml(component);
        } else if (component.type === 'BUTTONS') {
            html += createViewButtonsComponentHtml(component);
        }

        html += '</div>';
        return html;
    }

    function createViewHeaderComponentHtml(component) {
        return `
            <div class="space-y-3">
                <div>
                    <label class="block text-xs font-medium text-gray-700 mb-1">Format</label>
                    <div class="w-full px-2 py-1 text-sm bg-white border border-gray-300 rounded">
                        ${component.format || 'TEXT'}
                    </div>
                </div>
                <div>
                    <label class="block text-xs font-medium text-gray-700 mb-1">
                        ${component.format === 'TEXT' ? 'Header Metni' : 'Medya URL'}
                    </label>
                    <div class="w-full px-2 py-1 text-sm bg-white border border-gray-300 rounded min-h-8">
                        ${component.text || 'Boş'}
                    </div>
                </div>
            </div>
        `;
    }

    function createViewBodyComponentHtml(component) {
        return `
            <div class="space-y-3">
                <div>
                    <label class="block text-xs font-medium text-gray-700 mb-1">Body Metni</label>
                    <div class="w-full px-2 py-1 text-sm bg-white border border-gray-300 rounded min-h-16 whitespace-pre-wrap">
                        ${component.text || 'Boş'}
                    </div>
                </div>
            </div>
        `;
    }

    function createViewFooterComponentHtml(component) {
        return `
            <div>
                <label class="block text-xs font-medium text-gray-700 mb-1">Footer Metni</label>
                <div class="w-full px-2 py-1 text-sm bg-white border border-gray-300 rounded min-h-8">
                    ${component.text || 'Boş'}
                </div>
            </div>
        `;
    }

    function createViewButtonsComponentHtml(component) {
        let html = `
            <div class="space-y-3">
                <div>
                    <label class="block text-xs font-medium text-gray-700 mb-2">Butonlar</label>
                    <div class="buttons-container space-y-2">
        `;

        if (component.buttons && component.buttons.length > 0) {
            component.buttons.forEach((button, index) => {
                html += createViewButtonHtml(button, index);
            });
        } else {
            html += '<div class="text-xs text-gray-500 p-2 bg-white border border-gray-300 rounded">Buton yok</div>';
        }

        html += `
                    </div>
                </div>
            </div>
        `;

        return html;
    }

    function createViewButtonHtml(button, index) {
        return `
            <div class="button-item-view border border-gray-200 rounded p-2 bg-white">
                <div class="flex items-center justify-between mb-2">
                    <span class="text-xs font-medium text-gray-700">Buton ${index + 1}</span>
                    <span class="text-xs px-2 py-1 bg-gray-100 text-gray-600 rounded">${button.type}</span>
                </div>
                <div class="space-y-2">
                    <div>
                        <label class="block text-xs text-gray-600">Metin:</label>
                        <div class="text-xs text-gray-800">${button.text || 'Boş'}</div>
                    </div>
                    ${button.type === 'URL' && button.url ? `
                        <div>
                            <label class="block text-xs text-gray-600">URL:</label>
                            <div class="text-xs text-sky-700 break-all">${button.url}</div>
                        </div>
                    ` : ''}
                    ${button.type === 'PHONE_NUMBER' && button.phoneNumber ? `
                        <div>
                            <label class="block text-xs text-gray-600">Telefon:</label>
                            <div class="text-xs text-primary">${button.phoneNumber}</div>
                        </div>
                    ` : ''}
                </div>
            </div>
        `;
    }

    function updateViewPreview() {
        const $preview = $('#viewWhatsappPreview');
        let previewHtml = '';

        if (templateComponents.length === 0) {
            $preview.html('<div class="text-center text-gray-500 text-sm">Şablon bileşeni bulunamadı</div>');
            return;
        }

        // Sort components in WhatsApp order: HEADER, BODY, FOOTER, BUTTONS
        const sortedComponents = [...templateComponents].sort((a, b) => {
            const order = { 'HEADER': 1, 'BODY': 2, 'FOOTER': 3, 'BUTTONS': 4 };
            return (order[a.type] || 5) - (order[b.type] || 5);
        });

        sortedComponents.forEach(component => {
            previewHtml += createPreviewComponentHtml(component);
        });

        $preview.html(previewHtml);
    }

    function extractAndShowVariables() {
        const bodyComponent = templateComponents.find(c => c.type === 'BODY');
        if (bodyComponent && bodyComponent.text) {
            extractVariablesFromText(bodyComponent.text);
            renderViewVariableDefinitions();
        } else {
            $('#viewVariableDefinitions').html('<p class="text-xs text-gray-500">Değişken bulunamadı</p>');
        }
    }

    function renderViewVariableDefinitions() {
        const $container = $('#viewVariableDefinitions');
        $container.empty();

        const sortedVars = Object.keys(templateVariables).sort((a, b) => parseInt(a) - parseInt(b));

        if (sortedVars.length === 0) {
            $container.html('<p class="text-xs text-gray-500">Değişken bulunamadı</p>');
            return;
        }

        sortedVars.forEach(varNum => {
            const html = `
                <div class="variable-definition-view border border-gray-200 rounded p-2 bg-white">
                    <div class="flex items-center justify-between">
                        <span class="text-xs font-medium text-gray-700">{{${varNum}}}</span>
                        <span class="text-xs text-gray-500">Değişken ${varNum}</span>
                    </div>
                </div>
            `;
            $container.append(html);
        });
    }

    // Add view preview with sample data handler
    $('#viewPreviewWithSampleData').on('click', function() {
        updateViewPreviewWithSampleData();
    });

    function updateViewPreviewWithSampleData() {
        const sampleData = {
            '1': 'Ahmet Yılmaz',
            '2': 'ORD-2024-001',
            '3': '₺299,90',
            '4': '15.01.2024',
            '5': 'Kadıköy, İstanbul',
            '6': 'ABC123'
        };

        const $preview = $('#viewWhatsappPreview');
        let previewHtml = '';

        if (templateComponents.length === 0) {
            $preview.html('<div class="text-center text-gray-500 text-sm">Şablon bileşeni bulunamadı</div>');
            return;
        }

        const sortedComponents = [...templateComponents].sort((a, b) => {
            const order = { 'HEADER': 1, 'BODY': 2, 'FOOTER': 3, 'BUTTONS': 4 };
            return (order[a.type] || 5) - (order[b.type] || 5);
        });

        sortedComponents.forEach(component => {
            let componentText = component.text || '';

            // Replace variables with sample data
            if (component.type === 'BODY') {
                Object.keys(sampleData).forEach(varNum => {
                    componentText = componentText.replace(new RegExp(`\\{\\{${varNum}\\}\\}`, 'g'), sampleData[varNum]);
                });
            }

            const tempComponent = { ...component, text: componentText };
            previewHtml += createPreviewComponentHtml(tempComponent);
        });

        $preview.html(previewHtml);
    }

    // Load sample template into component builder (Updated for Static Form)
    function loadSampleIntoComponentBuilder(template) {
        // Clear existing components
        templateComponents = [];
        templateVariables = {};
        
        // Set template basic info
        $('#createTemplateName').val(template.id); // Sample templates use ID as name usually
        $(`input[name="category"][value="${template.category}"]`).prop('checked', true);
        $('#createTemplateLanguage').val(template.language);

        // Make template name read-only for sample templates
        $('#createTemplateName').prop('readonly', true);
        $('#createTemplateName').addClass('bg-gray-100 cursor-not-allowed');

        // Add info message about fixed name
        $('#templateNameError').remove();
        if (!$('#templateNameInfo').length) {
            $('#createTemplateName').parent().append(`
                <p id="templateNameInfo" class="text-xs text-sky-700 mt-1">
                    <svg class="w-3 h-3 inline mr-1" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"></path>
                    </svg>
                    Hazır şablon ismi sabit olarak belirlenmiştir
                </p>
            `);
        }

        // Reset Form First
        $('#headerType').val('NONE').trigger('change');
        $('#headerText').val('');
        $('#headerMediaUrl').val('');
        $('#bodyText').val('');
        $('#buttonsContainer').empty();

        // Populate Form from Components
        if (template.components && template.components.length > 0) {
            template.components.forEach(comp => {
                if (comp.type === 'HEADER') {
                    $('#headerType').val(comp.format).trigger('change');
                    if (comp.format === 'TEXT') {
                        $('#headerText').val(comp.text || '');
                    } else {
                        // For media, we might not have a URL in the sample, but if we do:
                        $('#headerMediaUrl').val(comp.text || '');
                    }
                } else if (comp.type === 'BODY') {
                    $('#bodyText').val(comp.text || '');
                } else if (comp.type === 'FOOTER') {
                    // Footer is now fixed, no need to set it from template
                } else if (comp.type === 'BUTTONS' && comp.buttons) {
                    comp.buttons.forEach(btn => {
                        addNewButton(btn.type, btn.text, btn.url, btn.phoneNumber);
                    });
                }
            });
        }

        // Extract variables from body text to ensure all variables are detected
        extractVariablesFromText($('#bodyText').val());

        // Populate variable mapping from sample
        if (template.variableMapping) {
            Object.keys(template.variableMapping).forEach(key => {
                const varType = template.variableMapping[key];
                // Check if it's a known type key
                if (predefinedVariables[varType]) {
                    templateVariables[key] = {
                        name: predefinedVariables[varType].label,
                        description: predefinedVariables[varType].label,
                        type: varType,
                        example: predefinedVariables[varType].example
                    }; 
                } 
                // Handle legacy format (using label directly) or unrecognized keys
                else {
                     const foundKey = Object.keys(predefinedVariables).find(k => predefinedVariables[k].label === varType);
                     if(foundKey) {
                        templateVariables[key] = {
                            name: predefinedVariables[foundKey].label,
                            description: predefinedVariables[foundKey].label,
                            type: foundKey,
                            example: predefinedVariables[foundKey].example
                        };
                     }
                }
            });
        }

        // Sync Data and Update Preview
        updateTemplateData();
        renderVariableDefinitions(); // Ensure variables are rendered
        
        // Show success message
        showNotification(`"${template.name}" şablonu yüklendi. İstediğiniz değişiklikleri yapabilirsiniz.`, 'success');
    }


    // Enable template name editing (for new templates)
    function enableTemplateNameEditing() {
        $('#createTemplateName').prop('readonly', false);
        $('#createTemplateName').removeClass('bg-gray-100 cursor-not-allowed');
        $('#createTemplateName').addClass('bg-white');
        $('#templateNameInfo').remove(); // Remove sample template info message
    }

    // Disable template name editing (for sample templates)
    function disableTemplateNameEditing() {
        $('#createTemplateName').prop('readonly', true);
        $('#createTemplateName').addClass('bg-gray-100 cursor-not-allowed');
        $('#createTemplateName').removeClass('bg-white');
    }

    // Expose functions for new UI
    window.deleteTemplate = deleteTemplate;
    window.viewTemplate = showViewTemplateModal;
    window.showCreateTemplateModal = showCreateTemplateModal;
    window.loadSampleIntoComponentBuilder = loadSampleIntoComponentBuilder;
    window.enableTemplateNameEditing = enableTemplateNameEditing;
});
