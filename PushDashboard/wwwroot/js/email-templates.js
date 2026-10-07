
$(document).ready(function() {
    let currentTemplateId = null;
    let currentTemplate = null;
    let pendingTemplateCopy = null;

    // Close ready modal function for external calls
    window.closeReadyModal = function() {
        $('#readyTemplatesModal').addClass('hidden');
        $('body').removeClass('overflow-hidden');
    }

    // Ready templates modal
    $('#readyTemplatesBtn').on('click', function() {
        $('#readyTemplatesModal').removeClass('hidden');
        $('body').addClass('overflow-hidden');
    });

    $('#closeReadyModalBtn, #closeReadyModalBtnBottom, #readyModalBackdrop').on('click', function() {
        window.closeReadyModal();
    });

    // Use ready-made template handler
    $('body').on('click', '.use-template-btn', function() {
        const templateId = $(this).data('template-id');
        useTemplate(templateId);
    });

    // Template item click handler
    $('.template-item').on('click', function() {
        const templateId = $(this).data('template-id');
        const companyTemplateId = $(this).data('company-template-id');
        loadTemplate(templateId, companyTemplateId);
    });

    // Create template button click handler
    $('#createTemplateBtn').on('click', function(e) {
        e.preventDefault();
        openModuleSelectionModal();
    });

    // Module Selection Modal Buttons
    $('#closeModuleSelectionBtn, #cancelModuleSelectionBtn').on('click', closeModuleSelectionModal);
    
    $('#confirmModuleSelectionBtn').on('click', function() {
        const selectedModule = TemplateModuleSelector.getSelectedModule();
        if(selectedModule) {
            closeModuleSelectionModal();
            showCreateTemplateModal(selectedModule);
        }
    });

    // Modal close handlers
    $('#closeModalBtn, #cancelBtn, #modalBackdrop').on('click', function() {
        closeTemplateModal();
    });

    $('#closePreviewBtn, #closePreviewModalBtn, #previewModalBackdrop').on('click', function() {
        closePreviewModal();
    });

    $('#closeCreateModalBtn, #cancelCreateBtn').on('click', function() {
        closeCreateTemplateModal();
    });

    // Delete template handler
    $('#deleteBtn').on('click', function() {
        deleteTemplate();
    });

    // Create template modal backdrop click
    $('#createTemplateModal').on('click', function(e) {
        if (e.target === this) {
            closeCreateTemplateModal();
        }
    });

    // Prevent modal close when clicking inside modal content
    $('#templateModal .inline-block, #previewModal .inline-block, #createTemplateModal > div').on('click', function(e) {
        e.stopPropagation();
    });
    
    // Initialize TemplateModuleSelector
    if (typeof TemplateModuleSelector !== 'undefined') {
        TemplateModuleSelector.init({
            onModuleChange: (key, id) => {
                 // Auto-select category based on module if needed
                 const categorySelect = document.getElementById('createTemplateCategory');
                 if (categorySelect) {
                    if (key === 'OrderStatus') categorySelect.value = 'Sipariş';
                    else if (key === 'BasketReminder') categorySelect.value = 'Sepet';
                    else if (key === 'BulkMessaging') categorySelect.value = 'Müşteri';
                    else if (key === 'General') categorySelect.value = 'Genel';
                 }
            },
            onVariableClick: (variableTag) => {
                insertVariableToActiveField(variableTag);
            },
            onModuleChange: (key) => {
                const btn = $('#confirmModuleSelectionBtn'); 
                btn.prop('disabled', false).removeClass('opacity-50 cursor-not-allowed');
            }
        });
    }

    // Load template data
    function loadTemplate(templateId, companyTemplateId) {
        showLoading();

        var url = `/EmailTemplate/GetTemplate/?templateId=${templateId}`;
        if (companyTemplateId) {
            url += `&companyTemplateId=${companyTemplateId}`;
        }

        $.get(url)
            .done(function(response) {
                if (response.success) {
                    // For custom templates, use a positive ID for currentTemplateId
                    currentTemplateId = response.template.id || templateId;
                    currentTemplate = response.template;
                    populateTemplateForm(response.template);
                    showTemplateModal();
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

    // Populate form with template data
    function populateTemplateForm(template) {
        $('#templateId').val(template.id);
        $('#templateName').text(template.name);

        // Use custom content if available, otherwise use default
        const subject = template.customSubject || template.defaultSubject || '';
        const content = template.customContent || template.defaultContent || '';

        $('#templateSubject').val(subject);
        $('#templateContent').val(content);

        // Store companyTemplateId for saving
        if (template.companyTemplateId) {
            $('#templateForm').data('company-template-id', template.companyTemplateId);
        } else {
            $('#templateForm').removeData('company-template-id');
        }

        // Show/hide reset button based on template type
        if (template.isCustomTemplate) {
            $('#resetBtn').hide(); // Hide reset button for custom templates
            $('#deleteBtn').show(); // Show delete button for custom templates
        } else {
            $('#resetBtn').show(); // Show reset button for base templates
            $('#deleteBtn').hide(); // Hide delete button for base templates
        }

        // Populate variables based on module type
        const moduleType = template.moduleType || '';
        populateVariablesForModuleType(moduleType);

        // Update modal title
        const customizedText = template.isCustomized ? ' (Özelleştirilmiş)' : '';
        $('#templateModalLabel span').text(`${template.name}${customizedText}`);

        // Initialize preview
        updateTemplatePreview();
    }

    // Live preview for edit template
    $('#templateContent, #templateSubject').on('input', function() {
        updateTemplatePreview();
    });

    function updateTemplatePreview() {
        const content = $('#templateContent').val();
        const subject = $('#templateSubject').val();
        
        // Update body content
        $('#previewBody').html(content);
        
        // Update subject line
        $('#previewSubject').text(subject);
    }

    // Populate available variables based on module type
    function populateVariablesForModuleType(moduleType) {
        const variablesList = $('#variablesList');
        variablesList.empty();
        variablesList.html('<div class="w-full text-center py-2"><svg class="w-5 h-5 animate-spin mx-auto text-gray-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg></div>');

        // Fetch variables from server based on module type
        $.get(`/EmailTemplate/GetVariables?moduleType=${moduleType || ''}`)
            .done(function(response) {
                variablesList.empty();
                
                if (response.success && response.variables && response.variables.length > 0) {
                    response.variables.forEach(function(variable) {
                        const tag = $(`<span class="inline-block bg-white border border-gray-300 rounded px-2 py-1 text-xs cursor-pointer hover:bg-gray-100 hover:border-primary transition-colors">{{${variable.name}}}</span>`);
                        tag.on('click', function() {
                            insertVariableToContent(variable.name);
                        });
                        variablesList.append(tag);
                    });
                } else {
                    variablesList.html('<span class="text-muted text-sm">Bu şablon için özel değişken bulunmuyor.</span>');
                }
            })
            .fail(function() {
                variablesList.html('<span class="text-red-500 text-sm">Değişkenler yüklenirken hata oluştu.</span>');
            });
    }

    // Insert variable to content at cursor position
    function insertVariableToContent(variable) {
        const textarea = document.getElementById('templateContent');
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const text = textarea.value;
        const before = text.substring(0, start);
        const after = text.substring(end, text.length);

        textarea.value = before + `{{${variable}}}` + after;
        textarea.selectionStart = textarea.selectionEnd = start + variable.length + 4;
        textarea.focus();
        
        // Update preview
        updateTemplatePreview();
    }

    // Save template
    $('#saveTemplateBtn').on('click', function() {
        const formData = {
            templateId: parseInt($('#templateId').val()),
            subject: $('#templateSubject').val().trim(),
            content: $('#templateContent').val().trim()
        };

        // Add companyTemplateId if this is a custom template
        const companyTemplateId = $('#templateForm').data('company-template-id');
        if (companyTemplateId) {
            formData.companyTemplateId = companyTemplateId;
        }

        if (!formData.subject) {
            showNotification('Email konusu boş olamaz.', 'warning');
            $('#templateSubject').focus();
            return;
        }

        if (!formData.content) {
            showNotification('Email içeriği boş olamaz.', 'warning');
            $('#templateContent').focus();
            return;
        }

        showLoading();

        $.ajax({
            url: '/EmailTemplate/SaveTemplate',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            }
        })
        .done(function(response) {
            if (response.success) {
                showNotification(response.message || 'Şablon başarıyla kaydedildi.', 'success');
                closeTemplateModal();

                // Update template item to show it's customized
                $(`.template-item[data-template-id="${currentTemplateId}"]`).addClass('active');
            } else {
                showNotification(response.message || 'Şablon kaydedilirken hata oluştu.', 'error');
            }
        })
        .fail(function() {
            showNotification('Şablon kaydedilirken hata oluştu.', 'error');
        })
        .always(function() {
            hideLoading();
        });
    });

    // Reset template
    $('#resetBtn').on('click', function () {
        if (!currentTemplateId) return;

        showConfirmationDialog('Sıfırla', 'Bu şablonu varsayılan haline sıfırlamak istediğinizden emin misiniz? Tüm özelleştirmeleriniz kaybolacak.', 'Sıfırla', 'İptal', () => {
            showLoading();

            $.ajax({
                url: '/EmailTemplate/ResetTemplate',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ templateId: currentTemplateId }),
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                }
            })
                .done(function (response) {
                    if (response.success) {
                        showNotification(response.message || 'Şablon varsayılan haline sıfırlandı.', 'success');

                        // Reload template data
                        loadTemplate(currentTemplateId);

                        // Remove active class from template item
                        $(`.template-item[data-template-id="${currentTemplateId}"]`).removeClass('active');
                    } else {
                        showNotification(response.message || 'Şablon sıfırlanırken hata oluştu.', 'error');
                    }
                })
                .fail(function () {
                    showNotification('Şablon sıfırlanırken hata oluştu.', 'error');
                })
                .always(function () {
                    hideLoading();
                });
        });
    });

    // Modal functions
    function showTemplateModal() {
        $('#templateModal').removeClass('hidden');
        $('body').addClass('overflow-hidden');
    }

    function closeTemplateModal() {
        $('#templateModal').addClass('hidden');
        $('body').removeClass('overflow-hidden');
    }

    function showPreviewModal() {
        $('#previewModal').removeClass('hidden');
        $('body').addClass('overflow-hidden');
    }

    function closePreviewModal() {
        $('#previewModal').addClass('hidden');
        $('body').removeClass('overflow-hidden');
    }

    function openModuleSelectionModal() {
        if (TemplateModuleSelector) TemplateModuleSelector.reset();
        $('#confirmModuleSelectionBtn').prop('disabled', true).addClass('opacity-50 cursor-not-allowed');
        $('#moduleSelectionModal').removeClass('hidden');
        $('body').addClass('overflow-hidden');
    }

    function closeModuleSelectionModal() {
        $('#moduleSelectionModal').addClass('hidden');
        $('body').removeClass('overflow-hidden');
    }

    function showCreateTemplateModal(selectedModuleKey) {
        $('#createTemplateModal').removeClass('hidden');
        $('body').addClass('overflow-hidden');
        // Clear form
        $('#createTemplateForm')[0].reset();
        // Clear preview content only, keeping the structure
        $('#createPreviewSubject').text('');
        $('#createPreviewBody').empty();

        // Check if there is a pending template copy to use
        if (pendingTemplateCopy) {
            $('#createTemplateName').val(pendingTemplateCopy.name + ' Kopya');
            $('#createTemplateCategory').val(pendingTemplateCopy.category);
            $('#createTemplateDescription').val(pendingTemplateCopy.description);
            $('#createTemplateSubject').val(pendingTemplateCopy.defaultSubject || '');
            $('#createTemplateContent').val(pendingTemplateCopy.defaultContent || '');
            
            // Consume it
            pendingTemplateCopy = null;
            
            // Update previews
            updateCreatePreview();
        }
        // If reusing shared selector, wait for user interact or we can init if needed, 
        // but selector usually defaults to empty or first option.

        // Set module context
        if (selectedModuleKey) {
             const categorySelect = document.getElementById('createTemplateCategory');
             if (categorySelect) {
                 if (selectedModuleKey === 'OrderStatus') categorySelect.value = 'Sipariş';
                 else if (selectedModuleKey === 'BasketReminder') categorySelect.value = 'Sepet';
                 else if (selectedModuleKey === 'BulkMessaging') categorySelect.value = 'Müşteri';
                 else if (selectedModuleKey === 'General') categorySelect.value = 'Genel';
             }
             $('#commonModuleId').val(selectedModuleKey);
             
             // Render Variables
             if (TemplateModuleSelector) {
                TemplateModuleSelector.fetchAndRenderVariables('createVariablesList', selectedModuleKey, (variableTag) => {
                     // Check if insert function exists or use fallback
                     if (typeof insertVariableToActiveField === 'function') {
                         insertVariableToActiveField(variableTag);
                     } else {
                          // Fallback for Email (usually has subject and content)
                          // We need to know which one is focused. Email JS usually tracks active Element.
                          // 'insertVariableToActiveField' handles this.
                     }
                });
            }
        }
    }

    function closeCreateTemplateModal() {
        $('#createTemplateModal').addClass('hidden');
        $('body').removeClass('overflow-hidden');
    }
    
    // Removed showModuleSelectionModal / closeModuleSelectionModal

    // Insert variable to Create Content - now handles variable text from outside
    function insertVariableToCreateContent(variableTag) {
        const textarea = document.getElementById('createTemplateContent');
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const text = textarea.value;
        const before = text.substring(0, start);
        const after = text.substring(end, text.length);
        
        // Check if variableTag already has braces (from shared selector) or just name
        const insertText = variableTag.startsWith('{{') ? variableTag : `{{${variableTag}}}`;

        textarea.value = before + insertText + after;
        textarea.selectionStart = textarea.selectionEnd = start + insertText.length;
        textarea.focus();
        
        // Update preview
        updateCreatePreview();
    }

    function insertVariableToCreateSubject(variableTag) {
        const input = document.getElementById('createTemplateSubject');
        const start = input.selectionStart;
        const end = input.selectionEnd;
        const text = input.value;
        const before = text.substring(0, start);
        const after = text.substring(end, text.length);

        const insertText = variableTag.startsWith('{{') ? variableTag : `{{${variableTag}}}`;

        input.value = before + insertText + after;
        input.selectionStart = input.selectionEnd = start + insertText.length;
        input.focus();
        
        // Update preview
        updateCreatePreview();
    }

    // Insert variable to the currently active field (subject or content)
    function insertVariableToActiveField(variableTag) {
        // We need to track last focused field or active element
        // Since buttons are clicked, activeElement might be the button. 
        // So we might need to store last focused input.
        // For simplicity, let's check which one was last focused? 
        // Or default to Content if Subject is not focused?
        // Actually, let's just use a global or check if subject is focused.
        
        // But clicking the button blurs the text input...
        // Let's rely on a variable to track last focused element in the create form
        
        if (lastFocusedCreateInputId === 'createTemplateSubject') {
             insertVariableToCreateSubject(variableTag);
        } else {
             insertVariableToCreateContent(variableTag);
        }
    }
    
    let lastFocusedCreateInputId = 'createTemplateContent';
    $('#createTemplateSubject, #createTemplateContent').on('focus', function() {
        lastFocusedCreateInputId = this.id;
    });

    // Live preview for create template
    $('#createTemplateContent, #createTemplateSubject').on('input', function() {
        updateCreatePreview();
    });

    function updateCreatePreview() {
        const content = $('#createTemplateContent').val();
        const subject = $('#createTemplateSubject').val();
        
        // Simple XSS protection could be added here if needed, but this is admin panel
        $('#createPreviewBody').html(content);
        $('#createPreviewSubject').text(subject);
    }

    // Full screen preview for create template
    $('#createFullScreenPreviewBtn, #createMobileFullScreenPreviewBtn').on('click', function() {
        const content = $('#createTemplateContent').val();
        const subject = $('#createTemplateSubject').val();
        
        $('#fullScreenPreviewBody').html(content);
        $('#fullScreenPreviewSubject').text(subject);
        showPreviewModal();
    });

    // Full screen preview for edit template
    $('#fullScreenPreviewBtn, #mobileFullScreenPreviewBtn').on('click', function() {
        const content = $('#templateContent').val();
        const subject = $('#templateSubject').val();
        
        $('#fullScreenPreviewBody').html(content);
        $('#fullScreenPreviewSubject').text(subject);
        showPreviewModal();
    });

    // Create template form submission
    $('#createTemplateForm').on('submit', function(e) {
        e.preventDefault();

        const formData = {
            name: $('#createTemplateName').val().trim(),
            category: $('#createTemplateCategory').val(),
            description: $('#createTemplateDescription').val().trim(),
            subject: $('#createTemplateSubject').val().trim(),
            content: $('#createTemplateContent').val().trim(),
            moduleType: $('#commonModuleId').val().trim()
        };

        // Validation
        if (!formData.name) {
            showNotification('Şablon adı boş olamaz.', 'warning');
            $('#createTemplateName').focus();
            return;
        }

        if (!formData.category) {
            showNotification('Kategori seçimi gereklidir.', 'warning');
            $('#createTemplateCategory').focus();
            return;
        }

        if (!formData.description) {
            showNotification('Açıklama boş olamaz.', 'warning');
            $('#createTemplateDescription').focus();
            return;
        }

        if (!formData.subject) {
            showNotification('Email konusu boş olamaz.', 'warning');
            $('#createTemplateSubject').focus();
            return;
        }

        if (!formData.content) {
            showNotification('Email içeriği boş olamaz.', 'warning');
            $('#createTemplateContent').focus();
            return;
        }

        showCreateLoading();

        $.ajax({
            url: '/EmailTemplate/CreateTemplate',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            }
        })
        .done(function(response) {
            if (response.success) {
                showNotification(response.message || 'Şablon başarıyla oluşturuldu.', 'success');
                closeCreateTemplateModal();

                // Reload page to show new template
                setTimeout(function() {
                    window.location.reload();
                }, 1000);
            } else {
                showNotification(response.message || 'Şablon oluşturulurken hata oluştu.', 'error');
            }
        })
        .fail(function() {
            showNotification('Şablon oluşturulurken hata oluştu.', 'error');
        })
        .always(function() {
            hideCreateLoading();
        });
    });

    // Use template (Copy system template)
    function useTemplate(templateId) {
        showLoading();

        $.get(`/EmailTemplate/GetTemplate/?templateId=${templateId}`)
            .done(function(response) {
                if (response.success) {
                    // Store template for later use
                    pendingTemplateCopy = response.template;
                    
                    window.closeReadyModal(); // Explicitly close ready modal
                    showCreateTemplateModal(); // Open create modal direclty
                    
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

    // Utility functions
    function showLoading() {
        // Show loading spinner or disable buttons
        $('#saveTemplateBtn, #previewBtn, #resetBtn').prop('disabled', true);
        BusyIndicator.show(null, { text: 'İşlem yapılıyor...' });
    }

    function hideLoading() {
        // Hide loading spinner or enable buttons
        $('#saveTemplateBtn, #previewBtn, #resetBtn').prop('disabled', false);
        BusyIndicator.hide(null);
    }

    function showCreateLoading() {
        // $('#saveCreateBtn').prop('disabled', true).html('<svg class="w-4 h-4 inline mr-2 animate-spin" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>Oluşturuluyor...');
        BusyIndicator.show('#saveCreateBtn', { variant: 'spinner', size: 'sm', text: 'Oluşturuluyor...' });
    }

    function hideCreateLoading() {
        // $('#saveCreateBtn').prop('disabled', false).html('<svg class="w-4 h-4 inline mr-2" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" /></svg>Şablonu Oluştur');
        BusyIndicator.hide('#saveCreateBtn');
        // Restore button text/icon if needed or BusyIndicator handles cleanup. 
        // BusyIndicator cleanup removes disabled but might not restore exact original HTML if we didn't save it.
        // But the BusyIndicator implementation doesn't overwrite innerHTML for spinner mode, it appends.
        // Wait, looking at BusyIndicator.js implementation:
        // For inline/spinner, it appends child. 
        // So I should simple hide it.
        // However, I might want to ensure the button text is correct.
        // Let's assume BusyIndicator handles it nicely. 
        
        // Actually, for button replacement, BusyIndicator inline mode appends. 
        // The original code was REPLACING html. 
        // So I should probably just let BusyIndicator handle visual and maybe I reset the text manually if needed?
        // But let's look at `showCreateLoading` logic I just wrote: It uses standard BusyIndicator.
    }

    function deleteTemplate() {
        const companyTemplateId = $('#templateForm').data('company-template-id');
        
        if (!companyTemplateId) {
            showNotification('Şablon silinemedi.', 'error');
            return;
        }

        showConfirmationDialog('Şablonu Sil', 'Bu şablonu silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.', 'Sil', 'İptal', () => {
            showLoading();

            $.ajax({
                url: '/EmailTemplate/DeleteTemplate',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ companyTemplateId: companyTemplateId }),
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                }
            })
                .done(function (response) {
                    if (response.success) {
                        showNotification(response.message || 'Şablon başarıyla silindi.', 'success');
                        closeTemplateModal();
                        setTimeout(function() {
                            window.location.reload();
                        }, 1000);
                    } else {
                        showNotification(response.message || 'Şablon silinirken hata oluştu.', 'error');
                    }
                })
                .fail(function () {
                    showNotification('Şablon silinirken hata oluştu.', 'error');
                })
                .always(function () {
                    hideLoading();
                });
        });
    }

    // Add CSRF token to all AJAX requests
    $.ajaxSetup({
        beforeSend: function(xhr, settings) {
            if (settings.type === 'POST' && !settings.crossDomain) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                if (token) {
                    xhr.setRequestHeader('RequestVerificationToken', token);
                }
            }
        }
    });
});

