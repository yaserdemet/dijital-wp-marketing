// Product Slider Admin Panel JavaScript

// Global variables
let currentSliderId = null;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    initializeProductSliderAdmin();
});

function initializeProductSliderAdmin() {
    // Initialize form handlers
    initializeCreateSliderForm();

    // Initialize tooltips and other UI components
    console.log('Product Slider Admin initialized');
}

// Create Slider Modal Functions
function openCreateSliderModal() {
    document.getElementById('createSliderModal').classList.remove('hidden');
    document.getElementById('sliderName').focus();
}

function closeCreateSliderModal() {
    document.getElementById('createSliderModal').classList.add('hidden');
    document.getElementById('createSliderForm').reset();
}

function initializeCreateSliderForm() {
    const form = document.getElementById('createSliderForm');
    if (form) {
        form.addEventListener('submit', handleCreateSlider);
    }
}

async function handleCreateSlider(event) {
    event.preventDefault();

    const formData = new FormData(event.target);
    const data = {
        name: formData.get('name'),
        description: formData.get('description'),
        displayType: formData.get('displayType')
    };

    try {
        const response = await fetch('/ProductSlider/CreateSlider', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify(data)
        });

        const result = await response.json();

        if (result.success) {
            showNotification('Slider başarıyla oluşturuldu!', 'success');
            closeCreateSliderModal();

            // Redirect to edit page
            if (result.sliderId) {
                window.location.href = `/ProductSlider/Edit/${result.sliderId}`;
            } else {
                // Refresh page to show new slider
                window.location.reload();
            }
        } else {
            showNotification(result.message || 'Slider oluşturulurken bir hata oluştu.', 'error');
        }
    } catch (error) {
        console.error('Error creating slider:', error);
        showNotification('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
    }
}

// Slider Management Functions
async function toggleSliderStatus(sliderId) {
    showConfirmationDialog('Durum Değiştir', 'Slider durumunu değiştirmek istediğinizden emin misiniz?', 'Evet', 'İptal', async () => {
        try {
            const formData = new FormData();
            formData.append('id', sliderId);
            formData.append('__RequestVerificationToken', getAntiForgeryToken());

            const response = await fetch('/ProductSlider/ToggleSliderStatus', {
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
            console.error('Error toggling slider status:', error);
            showNotification('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
        }
    });
}

async function deleteSlider(sliderId, sliderName) {
    showConfirmationDialog('Sil', `"${sliderName}" slider'ını silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.`, 'Evet, Sil', 'İptal', async () => {
        try {
            const formData = new FormData();
            formData.append('id', sliderId);
            formData.append('__RequestVerificationToken', getAntiForgeryToken());

            const response = await fetch('/ProductSlider/DeleteSlider', {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                showNotification(result.message, 'success');
                window.location.reload();
            } else {
                showNotification(result.message || 'Slider silinirken bir hata oluştu.', 'error');
            }
        } catch (error) {
            console.error('Error deleting slider:', error);
            showNotification('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
        }
    });
}

// Utility Functions
function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}


// R2 Export Function
async function exportToR2() {
    const exportBtn = document.getElementById('exportToR2Btn');
    const originalHTML = exportBtn ? exportBtn.innerHTML : '';

    try {
        if (exportBtn) {
            exportBtn.disabled = true;
            exportBtn.innerHTML = `
                <svg class="animate-spin w-5 h-5 mr-2" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Aktarılıyor...
            `;
        }

        const formData = new FormData();
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        const response = await fetch('/ProductSlider/ExportToR2', {
            method: 'POST',
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            showNotification('Slider\'lar başarıyla siteye aktarıldı!', 'success');
            console.log('JSON URL:', result.jsonUrl);
        } else {
            showNotification(result.message || 'Aktarım işlemi başarısız.', 'error');
        }
    } catch (error) {
        console.error('Error exporting to R2:', error);
        showNotification('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
    } finally {
        if (exportBtn) {
            exportBtn.disabled = false;
            exportBtn.innerHTML = originalHTML;
        }
    }
}
