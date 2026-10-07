$(document).ready(function() {
    if ($('#surveyId').length) {
        $('#exitSurveySettingsForm').on('submit', function(e) {
            e.preventDefault();
            saveSettings();
        });

        $('#saveSettingsBtn').on('click', function() {
            saveSettings();
        });

        $('#saveQuestionBtn').on('click', function() {
            saveQuestion();
        });

        $('#addQuestionBtn').on('click', function() {
            openQuestionModal();
        });

        $(document).on('click', '.edit-question-btn', function() {
            const questionId = $(this).data('question-id');
            editQuestion(questionId);
        });

        $(document).on('click', '.delete-question-btn', function() {
            const questionId = $(this).data('question-id');
            deleteQuestion(questionId);
        });

        // Question type radio card sync → hidden select
        $(document).on('change', 'input[name="questionTypeRadio"]', function() {
            $('#questionType').val($(this).val()).trigger('change');
        });

        $('#questionType').on('change', function() {
            toggleOptionsContainer();
        });

        $('#addOptionBtn').on('click', function() {
            addOption();
        });

        $(document).on('click', '.remove-option-btn', function() {
            $(this).closest('.option-item').remove();
        });

        $('#closeModalBtn, #cancelModalBtn').on('click', function() {
            closeModal();
        });

        $('#questionModal').on('click', function(e) {
            if (e.target === this) {
                closeModal();
            }
        });

        // Live preview on any form input change
        $('#exitSurveySettingsForm input, #exitSurveySettingsForm textarea, #exitSurveySettingsForm select').on('input change', function() {
            updatePreview();
        });

        // showOnMobile toggle → mobileDelayMinutes input göster/gizle
        $('#showOnMobile').on('change', function() {
            if ($(this).is(':checked')) {
                $('#mobileDelayContainer').slideDown(200);
            } else {
                $('#mobileDelayContainer').slideUp(200);
            }
        });

        // Color picker ↔ text input sync
        syncColorInputs('backgroundColorPicker', 'backgroundColor');
        syncColorInputs('textColorPicker', 'textColor');
        syncColorInputs('submitButtonColorPicker', 'submitButtonColor');
        syncColorInputs('cancelButtonColorPicker', 'cancelButtonColor');

        updatePreview();
    }

    if ($('#createSurveyForm').length) {
        $('#createSurveyForm').on('submit', function(e) {
            e.preventDefault();
            handleCreateSurvey();
        });
    }
});

// ==================== UTILITY ====================

function syncColorInputs(pickerId, textId) {
    const $picker = $('#' + pickerId);
    const $text = $('#' + textId);
    if (!$picker.length || !$text.length) return;
    $picker.on('input change', function() {
        $text.val(this.value).trigger('input');
    });
    $text.on('change', function() {
        $picker.val(this.value);
    });
}

function toggleSection(headerEl) {
    const $header = $(headerEl);
    const $body = $header.next('.es-section-body');
    const $chevron = $header.find('.es-section-chevron');
    $body.toggleClass('collapsed');
    $chevron.toggleClass('collapsed');
}

function toggleExitSurveyStatus() {
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

// ==================== INDEX SAYFASI ====================

function openCreateSurveyModal() {
    $('#surveyName').val('');
    $('#surveyDescription').val('');
    $('#createSurveyModal').removeClass('hidden');
}

function closeCreateSurveyModal() {
    $('#createSurveyModal').addClass('hidden');
}

function handleCreateSurvey() {
    const name = $('#surveyName').val().trim();
    const description = $('#surveyDescription').val().trim();

    if (!name) {
        showNotification('Anket adı boş olamaz!', 'error');
        return;
    }

    const submitBtn = $('#createSurveyForm button[type="submit"]');
    submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin mr-2"></i> Oluşturuluyor...');

    $.ajax({
        url: '/ExitSurvey/CreateSurvey',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ name: name, description: description }),
        success: function(response) {
            if (response.success) {
                showNotification('Anket başarıyla oluşturuldu!', 'success');
                closeCreateSurveyModal();
                setTimeout(() => location.reload(), 500);
            } else {
                showNotification(response.message || 'Bir hata oluştu!', 'error');
            }
        },
        error: function() {
            showNotification('Bir hata oluştu!', 'error');
        },
        complete: function() {
            submitBtn.prop('disabled', false).html('<i class="fas fa-plus"></i> Oluştur');
        }
    });
}

function toggleSurveyStatus(surveyId) {
    $.ajax({
        url: '/ExitSurvey/ToggleSurveyStatus',
        type: 'POST',
        data: { id: surveyId },
        success: function(response) {
            if (response.success) {
                showNotification('Anket durumu güncellendi!', 'success');
                location.reload();
            } else {
                showNotification(response.message || 'Bir hata oluştu!', 'error');
            }
        },
        error: function() {
            showNotification('Bir hata oluştu!', 'error');
        }
    });
}

function deleteSurvey(surveyId, surveyName) {
    showConfirmationDialog('Sil', `"${surveyName}" anketini silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.`, 'Sil', 'İptal', () => {
        $.ajax({
            url: '/ExitSurvey/DeleteSurvey',
            type: 'POST',
            data: { id: surveyId },
            success: function(response) {
                if (response.success) {
                    showNotification('Anket başarıyla silindi!', 'success');
                    location.reload();
                } else {
                    showNotification(response.message || 'Bir hata oluştu!', 'error');
                }
            },
            error: function() {
                showNotification('Bir hata oluştu!', 'error');
            }
        });
    });
}

function exportToSite() {
    const exportBtn = $('#exportToSiteBtn');
    const originalHTML = exportBtn.html();

    exportBtn.prop('disabled', true);
    exportBtn.html(`
        <svg class="animate-spin w-5 h-5 mr-2 inline" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        Aktarılıyor...
    `);

    $.ajax({
        url: '/ExitSurvey/ExportToR2',
        type: 'POST',
        contentType: 'application/json',
        success: function(response) {
            if (response.success) {
                showNotification('Siteye başarıyla aktarıldı!', 'success');
            } else {
                showNotification(response.message || 'Aktarım sırasında bir hata oluştu!', 'error');
            }
        },
        error: function() {
            showNotification('Aktarım sırasında bir hata oluştu!', 'error');
        },
        complete: function() {
            exportBtn.prop('disabled', false);
            exportBtn.html(originalHTML);
        }
    });
}

// ==================== EDIT SAYFASI ====================

function getFormData() {
    return {
        id: parseInt($('#surveyId').val()) || 0,
        title: $('#title').val(),
        description: $('#description').val(),
        submitButtonText: $('#submitButtonText').val(),
        cancelButtonText: $('#cancelButtonText').val(),
        thankYouMessage: $('#thankYouMessage').val(),
        backgroundColor: $('#backgroundColor').val(),
        textColor: $('#textColor').val(),
        submitButtonColor: $('#submitButtonColor').val(),
        cancelButtonColor: $('#cancelButtonColor').val(),
        borderRadius: ($('#borderRadius').val() || '8') + 'px',
        enableAnimation: $('#enableAnimation').is(':checked'),
        showOnPageExit: $('#showOnPageExit').is(':checked'),
        showOnTabClose: $('#showOnTabClose').is(':checked'),
        delayBeforeShow: parseInt($('#delayBeforeShow').val()) || 0,
        showFrequencyDays: isNaN(parseInt($('#showFrequencyDays').val())) ? 30 : parseInt($('#showFrequencyDays').val()),
        showOnMobile: $('#showOnMobile').is(':checked'),
        mobileDelayMinutes: parseInt($('#mobileDelayMinutes').val()) || 0,
        showOnDesktop: $('#showOnDesktop').is(':checked'),
        isActive: $('#isActive').val() === 'true'
    };
}

function saveSettings() {
    const saveBtn = $('#saveSettingsBtn');
    const originalHTML = saveBtn.html();

    saveBtn.prop('disabled', true);
    saveBtn.html('<i class="fas fa-spinner fa-spin mr-1"></i> Kaydediliyor...');

    const formData = getFormData();

    $.ajax({
        url: '/ExitSurvey/SaveSettings',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function(response) {
            if (response.success) {
                showNotification('Ayarlar başarıyla kaydedildi!', 'success');
            } else {
                showNotification(response.message || 'Bir hata oluştu!', 'error');
            }
        },
        error: function() {
            showNotification('Bir hata oluştu!', 'error');
        },
        complete: function() {
            saveBtn.prop('disabled', false);
            saveBtn.html(originalHTML);
        }
    });
}

// ==================== ANLIK ÖNİZLEME ====================

function getQuestionsFromDOM() {
    const questions = [];
    $('.question-item').each(function() {
        const $el = $(this);
        let options = [];
        try {
            const dataOptions = $el.data('question-options');
            if (typeof dataOptions === 'string') {
                options = JSON.parse(dataOptions || '[]');
            } else if (Array.isArray(dataOptions)) {
                options = dataOptions;
            }
        } catch(e) { 
            options = []; 
        }
        questions.push({
            id: $el.data('question-id'),
            text: $el.data('question-text'),
            type: parseInt($el.data('question-type')),
            options: options,
            isRequired: $el.data('question-required') === true || $el.data('question-required') === 'True'
        });
    });
    return questions;
}

function updatePreview() {
    const container = $('#surveyPreviewContainer');
    if (!container.length) return;

    const title = $('#title').val() || 'Anket Başlığı';
    const desc = $('#description').val() || '';
    const bgColor = $('#backgroundColor').val() || '#ffffff';
    const txtColor = $('#textColor').val() || '#333333';
    const submitColor = $('#submitButtonColor').val() || '#4CAF50';
    const cancelColor = $('#cancelButtonColor').val() || '#6c757d';
    const submitText = $('#submitButtonText').val() || 'Gönder';
    const cancelText = $('#cancelButtonText').val() || 'Kapat';
    const borderRadius = ($('#borderRadius').val() || '8') + 'px';
    const questions = getQuestionsFromDOM();

    const config = {
        isActive: true,
        companyId: $('#companyId').val() || 1,
        title: title,
        description: desc,
        backgroundColor: bgColor,
        textColor: txtColor,
        submitButtonColor: submitColor,
        cancelButtonColor: cancelColor,
        submitButtonText: submitText,
        cancelButtonText: cancelText,
        borderRadius: borderRadius,
        questions: questions.map((q, idx) => ({
            id: q.id || idx,
            questionText: q.text,
            questionType: q.type,
            options: q.options || [],
            isRequired: q.isRequired || false
        }))
    };

    const targetElement = container[0];
    if (targetElement) {
        new PushonicaExitSurvey({
            config: config,
            targetElement: targetElement,
            livePreview: true,
            manualInit: false
        });
    }
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.appendChild(document.createTextNode(text));
    return div.innerHTML;
}

// ==================== SORU YÖNETİMİ ====================

function openQuestionModal(questionData = null) {
    const isEdit = questionData && questionData.id > 0;

    $('#questionId').val(questionData ? questionData.id : 0);
    $('#questionText').val(questionData ? questionData.questionText : '');
    
    const typeVal = questionData ? questionData.questionType : 0;
    $('#questionType').val(typeVal);
    // Sync radio card UI
    $('input[name="questionTypeRadio"]').prop('checked', false);
    $('input[name="questionTypeRadio"][value="' + typeVal + '"]').prop('checked', true);
    
    $('#isRequired').prop('checked', questionData ? questionData.isRequired : false);

    $('#questionModalTitle').text(isEdit ? 'Soru Düzenle' : 'Yeni Soru Ekle');
    $('#saveQuestionBtn').html(isEdit ? '<i class="fas fa-check"></i> Güncelle' : '<i class="fas fa-check"></i> Kaydet');

    $('#optionsList').empty();

    if (questionData && questionData.options) {
        questionData.options.forEach(option => {
            addOption(option);
        });
    }

    toggleOptionsContainer();
    showModal();
}

function showModal() {
    $('#questionModal').removeClass('hidden');
}

function closeModal() {
    $('#questionModal').addClass('hidden');
}

function toggleOptionsContainer() {
    const questionType = parseInt($('#questionType').val());
    const optionsContainer = $('#optionsContainer');

    // Show options for: 0=SINGLE_CHOICE, 4=MULTI_CHOICE
    if (questionType === 0 || questionType === 4) {
        optionsContainer.removeClass('hidden');
        if ($('#optionsList').children().length === 0) {
            addOption();
            addOption();
        }
    } else {
        optionsContainer.addClass('hidden');
    }
}

function addOption(value = '') {
    const optionHtml = `
        <div class="option-item flex space-x-2">
            <input type="text" class="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent option-input"
                   value="${value}" placeholder="Seçenek metni">
            <button type="button" class="px-3 py-2 bg-red-100 hover:bg-red-200 text-red-700 rounded-md transition-colors duration-200 remove-option-btn">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `;
    $('#optionsList').append(optionHtml);
}

function saveQuestion() {
    const questionType = parseInt($('#questionType').val());
    const options = [];

    // Collect options for SINGLE_CHOICE (0) and MULTI_CHOICE (4)
    if (questionType === 0 || questionType === 4) {
        $('.option-input').each(function() {
            const value = $(this).val().trim();
            if (value) {
                options.push(value);
            }
        });

        if (options.length < 2) {
            const typeName = questionType === 0 ? 'Tek seçimli' : 'Çoklu seçimli';
            showNotification(`${typeName} sorular için en az 2 seçenek gereklidir!`, 'error');
            return;
        }
    }

    const questionId = parseInt($('#questionId').val()) || 0;
    const isEdit = questionId > 0;
    const surveyId = parseInt($('#surveyId').val()) || 0;

    const questionData = {
        id: questionId,
        exitSurveyId: surveyId,
        questionText: $('#questionText').val(),
        questionType: questionType,
        isRequired: $('#isRequired').is(':checked'),
        isActive: true,
        sortOrder: $('.question-item').length + 1,
        options: options
    };

    $.ajax({
        url: isEdit ? '/ExitSurvey/UpdateQuestion' : '/ExitSurvey/SaveQuestion',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(questionData),
        success: function(response) {
            if (response.success) {
                showNotification('Soru başarıyla kaydedildi!', 'success');
                closeModal();
                location.reload();
            } else {
                showNotification(response.message || 'Bir hata oluştu!', 'error');
            }
        },
        error: function() {
            showNotification('Bir hata oluştu!', 'error');
        }
    });
}

function editQuestion(questionId) {
    $.ajax({
        url: '/ExitSurvey/GetQuestion',
        type: 'GET',
        data: { questionId: questionId },
        success: function(response) {
            if (response.success) {
                const question = response.question;
                const questionData = {
                    id: question.id || question.Id,
                    questionText: question.questionText || question.QuestionText,
                    questionType: question.questionType || question.QuestionType,
                    isRequired: question.isRequired || question.IsRequired,
                    options: question.options ? (typeof question.options === 'string' ? JSON.parse(question.options) : question.options) :
                            (question.Options ? (typeof question.Options === 'string' ? JSON.parse(question.Options) : question.Options) : [])
                };
                openQuestionModal(questionData);
            } else {
                showNotification(response.message || 'Soru bilgileri alınamadı!', 'error');
            }
        },
        error: function() {
            showNotification('Soru bilgileri alınırken bir hata oluştu!', 'error');
        }
    });
}

function deleteQuestion(questionId) {
    showConfirmationDialog('Sil', 'Bu soruyu silmek istediğinizden emin misiniz?', 'Sil', 'İptal', () => {
        $.ajax({
            url: '/ExitSurvey/DeleteQuestion',
            type: 'POST',
            data: { questionId: questionId },
            success: function(response) {
                if (response.success) {
                    showNotification('Soru başarıyla silindi!', 'success');
                    $(`.question-item[data-question-id="${questionId}"]`).remove();
                    updatePreview();
                } else {
                    showNotification(response.message || 'Bir hata oluştu!', 'error');
                }
            },
            error: function() {
                showNotification('Bir hata oluştu!', 'error');
            }
        });
    });
}
