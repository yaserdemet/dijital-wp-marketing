let currentPage = 1;
let currentSearchTerm = '';
let currentStatus = '';
let currentSource = '';
let currentDateFilter = '';
let currentSortBy = '';
let currentSortDirection = '';
let currentPageSize = 50;
let selectedCustomerIds = [];
let currentCustomStartDate = '';
let currentCustomEndDate = '';

// Sayfa yüklendiğinde
document.addEventListener('DOMContentLoaded', function() {
    // İlk veri yüklemesi
    loadCustomers();

    // --- NEW Custom UI Listeners ---

    // Toggle logic
    const dateBtn = document.getElementById('dateFilterBtn');
    const dateDropdown = document.getElementById('dateFilterDropdown');
    const sortBtn = document.getElementById('sortToggleBtn');
    const sortDropdown = document.getElementById('sortDropdown');

    if(dateBtn && dateDropdown) {
        dateBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            dateDropdown.classList.toggle('hidden');
            if(sortDropdown) sortDropdown.classList.add('hidden');
        });
        dateDropdown.addEventListener('click', (e) => e.stopPropagation());
    }

    if(sortBtn && sortDropdown) {
        sortBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            sortDropdown.classList.toggle('hidden');
            if(dateDropdown) dateDropdown.classList.add('hidden');
        });
        // sortDropdown.addEventListener('click', (e) => e.stopPropagation()); // Optional: close on selection or not
    }

    // Close on outside click
    document.addEventListener('click', () => {
        if(dateDropdown) dateDropdown.classList.add('hidden');
        if(sortDropdown) sortDropdown.classList.add('hidden');
    });

    // Date Presets
    document.querySelectorAll('.date-preset-btn').forEach(btn => {
        btn.addEventListener('click', function() {
             const val = this.dataset.value;
             const label = this.dataset.label || this.textContent;
             
             document.getElementById('dateFilter').value = val;
             document.getElementById('dateFilterLabel').textContent = label;
             
             // Reset custom inputs visually
             const customStartInfo = document.getElementById('customStartDate');
             const customEndInfo = document.getElementById('customEndDate');
             if(customStartInfo) customStartInfo.value = '';
             if(customEndInfo) customEndInfo.value = '';
             
             triggerChange('dateFilter');
             if(dateDropdown) dateDropdown.classList.add('hidden'); // Close
        });
    });

    // Custom Date Apply
    const applyBtn = document.getElementById('applyCustomDateBtn');
    if(applyBtn) {
        applyBtn.addEventListener('click', function() {
            const start = document.getElementById('customStartDate').value;
            const end = document.getElementById('customEndDate').value;
            
            if(start || end) {
                document.getElementById('dateFilter').value = 'custom';
                
                // Format custom date label
                const formatDate = (d) => {
                    if(!d) return '';
                    const date = new Date(d);
                    return date.toLocaleDateString('tr-TR');
                };

                let labelText = 'Özel Tarih';
                if(start && end) {
                    labelText = `${formatDate(start)} - ${formatDate(end)}`;
                } else if(start) {
                    labelText = `${formatDate(start)} itibariyle`;
                } else if(end) {
                    labelText = `${formatDate(end)} tarihine kadar`;
                }
                
                document.getElementById('dateFilterLabel').textContent = labelText;
                
                currentCustomStartDate = start;
                currentCustomEndDate = end;
                
                currentPage = 1;
                loadCustomers();
                if(dateDropdown) dateDropdown.classList.add('hidden'); // Close
            }
        });
    }

    // Sort Filter Interactions
    document.querySelectorAll('.sort-option-btn').forEach(btn => {
        btn.addEventListener('click', function() {
             const val = this.dataset.value;
             document.getElementById('sortSelect').value = val;
             triggerChange('sortSelect');
             if(sortDropdown) sortDropdown.classList.add('hidden'); // Close
        });
    });

    // Sayfa boyutu seçimi
    const pageSizeSelect = document.getElementById('pageSizeSelect');
    if (pageSizeSelect) {
        pageSizeSelect.addEventListener('change', function() {
            currentPageSize = parseInt(this.value);
            currentPage = 1;
            loadCustomers();
        });
    }

    // Arama input'u
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('input', debounce(function(e) {
            currentSearchTerm = e.target.value.trim();
            currentPage = 1;
            console.log('Search term:', currentSearchTerm); // Debug için
            loadCustomers();
        }, 500));
    }

    // Durum filtresi
    const statusSelect = document.getElementById('statusFilter');
    if (statusSelect) {
        statusSelect.addEventListener('change', function() {
            currentStatus = this.value;
            currentPage = 1;
            loadCustomers();
        });
    }

    // Kaynak filtresi
    const sourceSelect = document.getElementById('sourceFilter');
    if (sourceSelect) {
        sourceSelect.addEventListener('change', function() {
            currentSource = this.value;
            currentPage = 1;
            loadCustomers();
        });
    }

    // Senkronizasyon butonu
    const syncBtn = document.getElementById('syncCustomersBtn');
    if (syncBtn) {
        syncBtn.addEventListener('click', syncCustomers);
    }

    // Select All Checkbox
    const selectAllCheckbox = document.getElementById('selectAllCustomers');
    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function() {
            const checkboxes = document.querySelectorAll('.customer-select-checkbox');
            const isChecked = this.checked;
            
            checkboxes.forEach(cb => {
                cb.checked = isChecked;
                handleCustomerSelection(cb.value, isChecked);
            });
            
            updateBulkActionButtons();
        });
    }

    // Individual Checkbox Selection (Event Delegation)
    document.getElementById('customerTableBody').addEventListener('change', function(e) {
        if (e.target.classList.contains('customer-select-checkbox')) {
            handleCustomerSelection(e.target.value, e.target.checked);
            updateSelectAllCheckboxState();
            updateBulkActionButtons();
        }
    });
});

function handleCustomerSelection(customerId, isSelected) {
    if (isSelected) {
        if (!selectedCustomerIds.includes(customerId)) {
            selectedCustomerIds.push(customerId);
        }
    } else {
        selectedCustomerIds = selectedCustomerIds.filter(id => id !== customerId);
    }
}

function updateSelectAllCheckboxState() {
    const selectAllCheckbox = document.getElementById('selectAllCustomers');
    const checkboxes = document.querySelectorAll('.customer-select-checkbox');
    const allChecked = Array.from(checkboxes).every(cb => cb.checked);
    const someChecked = Array.from(checkboxes).some(cb => cb.checked);
    
    if (selectAllCheckbox) {
        selectAllCheckbox.checked = allChecked && checkboxes.length > 0;
        selectAllCheckbox.indeterminate = someChecked && !allChecked;
    }
}

function updateBulkActionButtons() {
    const bulkMessagingBtn = document.getElementById('bulkMessagingBtn');
    if (bulkMessagingBtn) {
        if (selectedCustomerIds.length > 0) {
             bulkMessagingBtn.innerHTML = `
                <svg class="h-4 w-4 mr-2" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z" />
                </svg>
                Toplu Mesaj (${selectedCustomerIds.length})
            `;
        } else {
             bulkMessagingBtn.innerHTML = `
                <svg class="h-4 w-4 mr-2 text-indigo-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z" />
                </svg>
                Toplu Mesaj
            `;
        }
    }
}

function triggerChange(elementId) {
    const el = document.getElementById(elementId);
    if(elementId === 'dateFilter') {
        currentDateFilter = el.value;
        if(currentDateFilter !== 'custom') {
            currentCustomStartDate = '';
            currentCustomEndDate = '';
        }
    } else if (elementId === 'sortSelect') {
        const val = el.value;
        if (val) {
            const [sortBy, sortDirection] = val.split('-');
            currentSortBy = sortBy;
            currentSortDirection = sortDirection;
        } else {
            currentSortBy = '';
            currentSortDirection = '';
        }
    }
    currentPage = 1;
    loadCustomers();
}

// Debounce fonksiyonu
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Müşterileri yükle
function loadCustomers() {
    const params = new URLSearchParams({
        page: currentPage,
        pageSize: currentPageSize,
        searchTerm: currentSearchTerm || '',
        status: currentStatus || '',
        source: currentSource || '',
        dateFilter: currentDateFilter || '',
        sortBy: currentSortBy || '',
        sortDirection: currentSortDirection || '',
        startDate: currentCustomStartDate || '',
        endDate: currentCustomEndDate || ''
    });

    console.log('Loading customers with params:', {
        page: currentPage,
        searchTerm: currentSearchTerm,
        status: currentStatus,
        source: currentSource,
        dateFilter: currentDateFilter,
        sortBy: currentSortBy,
        sortDirection: currentSortDirection
    }); // Debug için

    // Loading göster
    showLoading(true);

    fetch(`/Customer/GetCustomers?${params}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                updateCustomerTable(data.data.customers);
                updatePagination(data.data.pagination);
                updateStats(data.data.stats);
            } else {
                showNotification('Hata: ' + data.message, 'error');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showNotification('Bir hata oluştu.', 'error');
        })
        .finally(() => {
            showLoading(false);
        });
}

// Sayfa değiştir
function loadPage(page) {
    currentPage = page;
    loadCustomers();
}

// Müşteri tablosunu güncelle
function updateCustomerTable(customers) {
    const tbody = document.getElementById('customerTableBody');
    if (!tbody) return;

    tbody.innerHTML = '';

    customers.forEach(customer => {
        const row = createCustomerRow(customer);
        tbody.appendChild(row);
    });

    updateSelectAllCheckboxState();
}

// Müşteri satırı oluştur
function createCustomerRow(customer) {
    const row = document.createElement('tr');
    const isSelected = selectedCustomerIds.includes(customer.id.toString());
    
    // Phone
    const phone = customer.mobilePhone || customer.phone || 'Belirtilmemiş';

    // Permissions icons
    const emailPerm = customer.emailPermission 
        ? '<div class="h-8 w-8 rounded-full bg-blue-100 flex items-center justify-center text-blue-600 mr-1" title="Email İzni Var"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"></path></svg></div>' 
        : '<div class="h-8 w-8 rounded-full bg-gray-100 flex items-center justify-center text-gray-400 mr-1 opacity-50" title="Email İzni Yok"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"></path></svg></div>';
    
    const smsPerm = customer.smsPermission 
        ? '<div class="h-8 w-8 rounded-full bg-green-100 flex items-center justify-center text-green-600" title="WhatsApp/SMS İzni Var"><svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24"><path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z"/></svg></div>' 
        : '<div class="h-8 w-8 rounded-full bg-gray-100 flex items-center justify-center text-gray-400 opacity-50" title="WhatsApp/SMS İzni Yok"><svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24"><path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z"/></svg></div>';

    row.innerHTML = `
        <td class="px-6 py-4 whitespace-nowrap">
            <div class="flex items-center">
                <input type="checkbox" class="customer-select-checkbox h-4 w-4 text-primary focus:ring-primary border-gray-300 rounded" value="${customer.id}" ${isSelected ? 'checked' : ''}>
            </div>
        </td>
        <td class="px-6 py-4 whitespace-nowrap">
            <div class="flex items-center">
                <div class="flex-shrink-0 h-10 w-10 relative">
                    <div class="h-10 w-10 rounded-full bg-gray-300 flex items-center justify-center text-gray-600 font-medium">
                        ${customer.fullName && customer.fullName.length > 0 ? customer.fullName.charAt(0).toUpperCase() : '?'}
                    </div>
                    ${customer.isActive 
                        ? '<span class="absolute bottom-0 right-0 block h-2.5 w-2.5 rounded-full ring-2 ring-white bg-green-400" title="Aktif"></span>' 
                        : '<span class="absolute bottom-0 right-0 block h-2.5 w-2.5 rounded-full ring-2 ring-white bg-gray-400" title="Pasif"></span>'}
                </div>
                <div class="ml-4">
                    <div class="text-sm font-medium text-gray-900">${customer.fullName}</div>
                    <div class="text-xs text-gray-500">${customer.email}</div>
                </div>
            </div>
        </td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
            ${phone}
        </td>
        <td class="px-6 py-4 whitespace-nowrap">
            <span class="px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full ${customer.sourceBadgeClass}">
                ${customer.sourceText}
            </span>
        </td>
        <td class="px-6 py-4 whitespace-nowrap">
            <div class="flex">
                ${emailPerm}${smsPerm}
            </div>
        </td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
            ${customer.formattedMembershipDate}
        </td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
            ${customer.formattedLastLoginDate}
        </td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
            <div class="flex space-x-2">
                <button onclick="viewCustomer(${customer.id})" class="text-primary hover:text-primary-dark" title="Müşteri Detayları">
                    <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"></path>
                        <circle cx="12" cy="12" r="3"></circle>
                    </svg>
                </button>
                <button onclick="viewCustomerBaskets(${customer.id})" class="text-green-600 hover:text-green-800" title="Müşteri Sepetleri">
                    <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <circle cx="8" cy="21" r="1"></circle>
                        <circle cx="19" cy="21" r="1"></circle>
                        <path d="M2.05 2.05h2l2.66 12.42a2 2 0 0 0 2 1.58h9.78a2 2 0 0 0 1.95-1.57l1.65-7.43H5.12"></path>
                    </svg>
                </button>
            </div>
        </td>
    `;
    return row;
}

// Sayfalama güncelle
function updatePagination(pagination) {
    const paginationContainer = document.getElementById('paginationContainer');
    if (!paginationContainer) return;

    // Bilgi metni ve PageSize selector güncelle
    const infoText = paginationContainer.querySelector('.text-sm.text-gray-500');
    if (infoText) {
        infoText.textContent = `${pagination.startRecord} - ${pagination.endRecord} arası (Toplam ${pagination.totalRecords.toLocaleString()})`;
    }

    // Sayfa butonları güncelle
    const buttonContainer = paginationContainer.querySelector('#paginationButtons');
    if (buttonContainer) {
        buttonContainer.innerHTML = '';

        // Helper: Create Button
        const createBtn = (text, page, isActive = false, isDisabled = false, isIcon = false) => {
            const btn = document.createElement('button');
            
            let classes = 'relative inline-flex items-center px-4 py-2 border text-sm font-medium transition-colors focus:z-20';
            
            if (isActive) {
                // Active Page
                classes += ' z-10 bg-blue-50 border-blue-500 text-blue-600';
            } else if (isDisabled) {
                // Disabled (dots or edges)
                classes += ' bg-white border-gray-300 text-gray-300 cursor-not-allowed';
            } else {
                // Normal
                classes += ' bg-white border-gray-300 text-gray-500 hover:bg-gray-50 hover:text-gray-700';
            }
            
            // Rounded corners logic handled by parent flex container or first/last child usually
            // but here we can just round them all slightly or handle group classes.
            // Let's use individual rounded buttons for modern look
            classes = classes.replace('border', 'border border-gray-300'); // Reset base border
            if (isActive) classes = classes.replace('border-gray-300', 'border-blue-500');
            
            btn.className = classes + ' rounded-md mx-0.5 min-w-[36px] justify-center'; // Spaced out style
            
            if (isIcon) {
                btn.innerHTML = text;
            } else {
                btn.textContent = text;
            }
            
            if (!isDisabled && !isActive) {
                btn.onclick = () => loadPage(page);
            }
            
            return btn;
        };

        // Previous
        const prevContent = `<svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" /></svg>`;
        buttonContainer.appendChild(createBtn(prevContent, pagination.currentPage - 1, false, !pagination.hasPreviousPage, true));

        // Logic for "..." gaps
        const total = pagination.totalPages;
        const current = pagination.currentPage;
        let pages = [];

        // Always show first, last, current, and neighbors
        if (total <= 7) {
            for (let i = 1; i <= total; i++) pages.push(i);
        } else {
            if (current <= 4) {
                pages = [1, 2, 3, 4, 5, '...', total];
            } else if (current >= total - 3) {
                pages = [1, '...', total - 4, total - 3, total - 2, total - 1, total];
            } else {
                pages = [1, '...', current - 1, current, current + 1, '...', total];
            }
        }

        pages.forEach(p => {
            if (p === '...') {
                const dots = document.createElement('span');
                dots.className = "relative inline-flex items-center px-4 py-2 border border-transparent bg-white text-sm font-medium text-gray-700";
                dots.textContent = '...';
                buttonContainer.appendChild(dots);
            } else {
                buttonContainer.appendChild(createBtn(p, p, p === current));
            }
        });

        // Next
        const nextContent = `<svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" /></svg>`;
        buttonContainer.appendChild(createBtn(nextContent, pagination.currentPage + 1, false, !pagination.hasNextPage, true));
    }
}

// İstatistikleri güncelle
function updateStats(stats) {
    const totalElement = document.getElementById('totalCustomers');
    const smsPermittedElement = document.getElementById('smsPermittedCustomers');
    const newElement = document.getElementById('newCustomers');
    const inactiveElement = document.getElementById('inactiveCustomers');
    const smsPermittedPercentageElement = document.getElementById('smsPermittedPercentage');
    const lastSyncDateElement = document.getElementById('lastSyncDate');

    if (totalElement) totalElement.textContent = stats.totalCustomers.toLocaleString();
    if (smsPermittedElement) smsPermittedElement.textContent = stats.smsPermittedCustomers.toLocaleString();
    if (newElement) newElement.textContent = stats.newCustomersThisMonth.toLocaleString();
    if (inactiveElement) inactiveElement.textContent = stats.inactiveCustomers.toLocaleString();

    // SMS izinli müşteri yüzdesini hesapla ve göster
    if (smsPermittedPercentageElement) {
        const percentage = stats.totalCustomers > 0
            ? ((stats.smsPermittedCustomers / stats.totalCustomers) * 100).toFixed(1)
            : 0;
        smsPermittedPercentageElement.textContent = `%${percentage}`;
    }

    // Son senkronizasyon tarihini göster
    if (lastSyncDateElement) {
        const syncDate = stats.lastSyncDate || stats.LastSyncDate;

        if (syncDate) {
            try {
                const date = new Date(syncDate);
                if (!isNaN(date.getTime())) {
                    const formattedDate = date.toLocaleDateString('tr-TR', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                    });
                    lastSyncDateElement.textContent = formattedDate;
                } else {
                    lastSyncDateElement.textContent = 'Henüz senkronize edilmemiş';
                }
            } catch (error) {
                console.error('Date parsing error:', error);
                lastSyncDateElement.textContent = 'Henüz senkronize edilmemiş';
            }
        } else {
            lastSyncDateElement.textContent = 'Henüz senkronize edilmemiş';
        }
    }
}

// Global dialog instance
let customerDetailDialog = null;

// Müşteri detaylarını görüntüle
function viewCustomer(customerId) {
    if (!customerDetailDialog) {
        customerDetailDialog = new PushDialog({
            title: 'Müşteri Detayları',
            size: '3xl',
            footer: '<button type="button" class="w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm" onclick="customerDetailDialog.close()">Kapat</button>',
            onClose: () => {}
        });
    }

    customerDetailDialog.open();
    customerDetailDialog.setLoading(true);

    fetch(`/Customer/GetCustomerDetails?id=${customerId}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                const template = document.getElementById('customerDetailsTemplate');
                if (template) {
                     const clone = template.content.cloneNode(true);
                     const contentWrapper = document.createElement('div');
                     contentWrapper.appendChild(clone);
                     
                     populateCustomerModal(data.data, contentWrapper);
                     customerDetailDialog.setContent(contentWrapper);
                }
            } else {
                showNotification('Hata: ' + data.message, 'error');
                customerDetailDialog.close();
            }
        })
        .catch(error => {
            console.error('Error fetching customer details:', error);
            showNotification('Bir hata oluştu: ' + error.message, 'error');
            customerDetailDialog.close();
        })
        .finally(() => {
            customerDetailDialog.setLoading(false);
        });
}

// Old functions kept for compatibility
function openCustomerModal() {} 
function closeCustomerModal() {
    if (customerDetailDialog && customerDetailDialog.isOpen) {
        customerDetailDialog.close();
    }
}

// Modal dışına tıklama/ESC listener'ları PushDialog tarafından yönetildiği için kaldırıldı

// Müşteri verilerini modal'a doldur
function populateCustomerModal(customer, root) {
    // Header Bilgileri
    setElementText('headerFullName', customer.fullName || '-', root);
    setElementText('headerEmail', customer.email || '-', root);
    
    const avatar = root.querySelector('[data-id="headerAvatar"]');
    if (avatar) {
        avatar.textContent = customer.fullName && customer.fullName.length > 0 ? customer.fullName.charAt(0).toUpperCase() : '?';
    }
    
    // Status Badge
    const statusContainer = root.querySelector('[data-id="customerStatusBadgeContainer"]');
    if (statusContainer) {
         statusContainer.innerHTML = `<span class="px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full ${customer.statusBadgeClass}">${customer.statusText}</span>`;
    }

    // Kişisel Bilgiler
    setElementText('customerFullName', customer.fullName || '-', root);
    setElementText('customerEmail', customer.email || '-', root);
    setElementText('customerPhone', customer.phone || '-', root);
    setElementText('customerMobilePhone', customer.mobilePhone || '-', root);
    setElementText('customerBirthDate', formatDate(customer.birthDate) || '-', root);
    setElementText('customerGender', getGenderText(customer.genderId) || '-', root);

    // Adres Bilgileri
    setElementText('customerCity', customer.city || '-', root);
    setElementText('customerDistrict', customer.district || '-', root);

    // Üyelik Bilgileri
    setElementText('customerCode', customer.customerCode || '-', root);
    setElementText('customerMembershipType', customer.membershipType || '-', root);
    setElementText('customerMembershipDate', formatDate(customer.membershipDate) || '-', root);

    // Aktivite Bilgileri
    setElementText('customerLastLoginDate', formatDate(customer.lastLoginDate) || '-', root);
    setElementText('customerLastUpdateDate', formatDate(customer.lastUpdateDate) || '-', root);

    // Mali Bilgiler
    setElementText('customerPointBalance', customer.pointBalance ? customer.pointBalance.toLocaleString() + ' Puan' : '0 Puan', root);
    setElementText('customerCreditLimit', customer.creditLimit ? customer.creditLimit.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' }) : '0,00 ₺', root);

    // İzinler ve Sözleşmeler
    setPermissionText('customerEmailPermission', customer.emailPermission, root);
    setPermissionText('customerSmsPermission', customer.smsPermission, root);
    setPermissionText('customerKvkkApproval', customer.kvkkApproval, root);
    setPermissionText('customerMembershipAgreement', customer.membershipAgreementApproval, root);

    // Mesleki Bilgiler
    setElementText('customerProfession', customer.profession || '-', root);
    setElementText('customerEducationLevel', customer.educationLevel || '-', root);
}

// Yardımcı fonksiyonlar
function setElementText(dataId, text, root) {
    const element = root.querySelector(`[data-id="${dataId}"]`);
    if (element) {
        element.textContent = text;
    }
}

function setPermissionText(dataId, permission, root) {
    const element = root.querySelector(`[data-id="${dataId}"]`);
    if (element) {
        if (permission) {
            element.innerHTML = `
                <div class="flex items-center gap-2">
                    <span class="text-[10px] font-semibold text-green-600 uppercase tracking-widest">Aktif</span>
                    <div class="relative inline-flex h-4 w-8 flex-shrink-0 cursor-default rounded-full border-2 border-transparent bg-green-500 transition-colors duration-200 ease-in-out">
                        <span class="translate-x-4 inline-block h-3 w-3 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out"></span>
                    </div>
                </div>`;
        } else {
            element.innerHTML = `
                <div class="flex items-center gap-2">
                    <span class="text-[10px] font-semibold text-gray-400 uppercase tracking-widest">Pasif</span>
                    <div class="relative inline-flex h-4 w-8 flex-shrink-0 cursor-default rounded-full border-2 border-transparent bg-gray-200 transition-colors duration-200 ease-in-out">
                        <span class="translate-x-0 inline-block h-3 w-3 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out"></span>
                    </div>
                </div>`;
        }
    }
}

function setBlockedText(dataId, blocked, root) {
    const element = root.querySelector(`[data-id="${dataId}"]`);
    if (element) {
        if (blocked) {
            element.innerHTML = '<span class="text-red-600 font-medium">✗ Yasaklı</span>';
        } else {
            element.innerHTML = '<span class="text-green-600 font-medium">✓ İzinli</span>';
        }
    }
}

function formatDate(dateString) {
    if (!dateString) return null;

    try {
        const date = new Date(dateString);
        if (isNaN(date.getTime()) || date.getFullYear() < 1900) return null;

        return date.toLocaleDateString('tr-TR', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    } catch (e) {
        return null;
    }
}

function getGenderText(genderId) {
    switch (genderId) {
        case 1:
            return 'Erkek';
        case 2:
            return 'Kadın';
        default:
            return 'Belirtilmemiş';
    }
}

// Müşterileri ve sepetleri senkronize et
function syncCustomers() {
    const syncBtn = document.getElementById('syncCustomersBtn');
    const syncBtnText = document.getElementById('syncBtnText');

    if (!syncBtn || !syncBtnText) return;

    // Butonu devre dışı bırak
    syncBtn.disabled = true;
    syncBtnText.textContent = 'Senkronize ediliyor...';
    syncBtn.classList.add('opacity-50', 'cursor-not-allowed');

    // Bilgilendirme mesajı göster
    showNotification('Müşteriler ve sepetler senkronize ediliyor, lütfen bekleyin...', 'info');

    fetch('/Customer/SyncCustomers', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showNotification(data.message, 'success');
            loadCustomers();
        } else {
            showNotification('Hata: ' + data.message, 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('Senkronizasyon sırasında bir hata oluştu: ' + error.message, 'error');
    })
    .finally(() => {
        // Butonu tekrar aktif et
        syncBtn.disabled = false;
        syncBtnText.textContent = 'Senkronize Et';
        syncBtn.classList.remove('opacity-50', 'cursor-not-allowed');
    });
}

// Loading göster/gizle
function showLoading(show) {
    const tbody = document.getElementById('customerTableBody');
    if (!tbody) return;

    if (show) {
        tbody.innerHTML = `
            <tr>
                <td colspan="7" class="px-6 py-12 text-center">
                    <div class="flex items-center justify-center">
                        <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-primary" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                        </svg>
                        <span class="text-gray-500">Yükleniyor...</span>
                    </div>
                </td>
            </tr>
        `;
    }
}

// Müşteri sepetlerini görüntüle
function viewCustomerBaskets(customerId) {
    openBasketModal();

    fetch(`/Basket/GetBasketsByCustomer?customerId=${customerId}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                populateBasketModal(data.data);
            } else {
                showNotification('Hata: ' + data.message, 'error');
                closeBasketModal();
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showNotification('Bir hata oluştu.', 'error');
            closeBasketModal();
        });
}

// Sepet modal'ını aç
function openBasketModal() {
    const modal = document.getElementById('basketModal');
    if (modal) {
        modal.classList.remove('hidden');
        document.body.style.overflow = 'hidden';
    }
}

// Sepet modal'ını kapat
function closeBasketModal() {
    const modal = document.getElementById('basketModal');
    if (modal) {
        modal.classList.add('hidden');
        document.body.style.overflow = 'auto';
    }
}

// Sepet detay modal'ını aç
function openBasketDetailModal() {
    const modal = document.getElementById('basketDetailModal');
    if (modal) {
        modal.classList.remove('hidden');
        document.body.style.overflow = 'hidden';
    }
}

// Sepet detay modal'ını kapat
function closeBasketDetailModal() {
    const modal = document.getElementById('basketDetailModal');
    if (modal) {
        modal.classList.add('hidden');
        document.body.style.overflow = 'auto';
    }
}

// Sepet modal'ını doldur
function populateBasketModal(baskets) {
    const content = document.getElementById('basketModalContent');

    if (!baskets || baskets.length === 0) {
        content.innerHTML = `
            <div class="text-center py-8">
                <svg class="mx-auto h-12 w-12 text-gray-400" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="8" cy="21" r="1"></circle>
                    <circle cx="19" cy="21" r="1"></circle>
                    <path d="M2.05 2.05h2l2.66 12.42a2 2 0 0 0 2 1.58h9.78a2 2 0 0 0 1.95-1.57l1.65-7.43H5.12"></path>
                </svg>
                <h3 class="mt-2 text-sm font-medium text-gray-900">Sepet bulunamadı</h3>
                <p class="mt-1 text-sm text-gray-500">Bu müşteriye ait sepet bulunmuyor.</p>
            </div>
        `;
        return;
    }

    content.innerHTML = `
        <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200">
                <thead class="bg-gray-50">
                    <tr>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Sepet Tarihi</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Ürün Sayısı</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Toplam Tutar</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Durum</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">İşlemler</th>
                    </tr>
                </thead>
                <tbody class="bg-white divide-y divide-gray-200">
                    ${baskets.map(basket => `
                        <tr>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                                ${basket.formattedBasketDate}
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                                ${basket.productCount}
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                                ${basket.formattedTotalAmount}
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap">
                                <span class="px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full ${basket.membershipStatusBadgeClass}">
                                    ${basket.membershipStatusText}
                                </span>
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                <button onclick="viewBasketDetail('${basket.guidBasketId}')" class="text-primary hover:text-primary-dark">
                                    <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                        <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"></path>
                                        <circle cx="12" cy="12" r="3"></circle>
                                    </svg>
                                </button>
                            </td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>
        </div>
    `;
}

// Sepet detayını görüntüle
function viewBasketDetail(guidBasketId) {
    closeBasketModal();
    openBasketDetailModal();

    // Önce sepet bilgilerini yükle
    fetch(`/Basket/GetBasketDetails?guidBasketId=${encodeURIComponent(guidBasketId)}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                populateBasketDetailModal(data.data);
                // Sonra ürünleri sayfalama ile yükle
                loadBasketItemsInModal(guidBasketId, 1);
            } else {
                showNotification('Hata: ' + data.message, 'error');
                closeBasketDetailModal();
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showNotification('Bir hata oluştu.', 'error');
            closeBasketDetailModal();
        });
}

// Sepet detay modal'ını doldur
function populateBasketDetailModal(basket) {
    const content = document.getElementById('basketDetailModalContent');

    content.innerHTML = `
        <div class="space-y-6">
            <!-- Sepet Bilgileri -->
            <div class="bg-gray-50 p-4 rounded-lg">
                <h4 class="text-lg font-medium text-gray-900 mb-3">Sepet Bilgileri</h4>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                        <label class="block text-sm font-medium text-gray-700">Müşteri</label>
                        <div class="mt-1 flex items-center space-x-2">
                            <p class="text-sm text-gray-900">${basket.displayCustomerName || basket.customerName}</p>
                            <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${basket.membershipStatusBadgeClass || 'bg-gray-100 text-gray-800'}">
                                ${basket.membershipStatusText || (basket.customerName ? 'Üyelikli' : 'Üyeliksiz')}
                            </span>
                        </div>
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-gray-700">E-posta</label>
                        <p class="mt-1 text-sm text-gray-900">${basket.displayCustomerEmail || basket.customerEmail}</p>
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-gray-700">Sepet Tarihi</label>
                        <p class="mt-1 text-sm text-gray-900">${basket.formattedBasketDate}</p>
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-gray-700">Toplam Tutar</label>
                        <p class="mt-1 text-sm text-gray-900 font-semibold">${basket.formattedTotalAmount}</p>
                    </div>
                </div>
            </div>

            <!-- Sepet Ürünleri -->
            <div>
                <h4 class="text-lg font-medium text-gray-900 mb-3">Sepet Ürünleri</h4>
                <div id="basketItemsContainer">
                    <!-- Ürünler buraya yüklenecek -->
                </div>
            </div>
        </div>
    `;
}

// Sepet ürünlerini modal'da yükle
function loadBasketItemsInModal(guidBasketId, page = 1) {
    const itemsContainer = document.getElementById('basketItemsContainer');
    if (!itemsContainer) return;

    // Loading göster
    itemsContainer.innerHTML = `
        <div class="text-center py-8">
            <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
            <p class="mt-2 text-sm text-gray-500">Ürünler yükleniyor...</p>
        </div>
    `;

    fetch(`/Basket/GetBasketItemsPaged?guidBasketId=${encodeURIComponent(guidBasketId)}&page=${page}&pageSize=10`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                populateBasketItemsInModal(data.data, guidBasketId);
            } else {
                itemsContainer.innerHTML = `
                    <div class="text-center py-8">
                        <p class="text-sm text-red-600">Ürünler yüklenirken hata oluştu: ${data.message}</p>
                    </div>
                `;
            }
        })
        .catch(error => {
            console.error('Error loading basket items:', error);
            itemsContainer.innerHTML = `
                <div class="text-center py-8">
                    <p class="text-sm text-red-600">Ürünler yüklenirken bir hata oluştu.</p>
                </div>
            `;
        });
}

// Sepet ürünlerini modal'da populate et
function populateBasketItemsInModal(pagedData, guidBasketId) {
    const container = document.getElementById('basketItemsContainer');
    if (!container) return;

    if (pagedData.items && pagedData.items.length > 0) {
        container.innerHTML = `
            <div class="overflow-x-auto">
                <table class="min-w-full divide-y divide-gray-200">
                    <thead class="bg-gray-50">
                        <tr>
                            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Ürün</th>
                            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Adet</th>
                            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Birim Fiyat</th>
                            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Toplam</th>
                        </tr>
                    </thead>
                    <tbody class="bg-white divide-y divide-gray-200">
                        ${pagedData.items.map(item => `
                            <tr>
                                <td class="px-6 py-4 whitespace-nowrap">
                                    <div class="flex items-center">
                                        <div class="flex-shrink-0 h-10 w-10">
                                            ${item.productImage ?
                                                `<img class="h-10 w-10 rounded object-cover" src="${item.productImage}" alt="${item.productName}">` :
                                                `<div class="h-10 w-10 rounded bg-gray-200 flex items-center justify-center">
                                                    <svg class="w-5 h-5 text-gray-400" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                                        <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                                                        <circle cx="9" cy="9" r="2"></circle>
                                                        <path d="m21 15-3.086-3.086a2 2 0 0 0-2.828 0L6 21"></path>
                                                    </svg>
                                                </div>`
                                            }
                                        </div>
                                        <div class="ml-4">
                                            <div class="text-sm font-medium text-gray-900">${item.productName}</div>
                                            <div class="text-sm text-gray-500">${item.productCode}</div>
                                        </div>
                                    </div>
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                                    ${item.formattedQuantity}
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                                    ${item.formattedUnitPrice}
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 font-semibold">
                                    ${item.formattedTotalPrice}
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>

            <!-- Sayfalama -->
            ${pagedData.totalPages > 1 ? `
                <div class="flex items-center justify-between border-t border-gray-200 bg-white px-4 py-3 sm:px-6">
                    <div class="flex flex-1 justify-between sm:hidden">
                        <button onclick="loadBasketItemsInModal('${guidBasketId}', ${pagedData.currentPage - 1})"
                                ${!pagedData.hasPreviousPage ? 'disabled' : ''}
                                class="relative inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed">
                            Önceki
                        </button>
                        <button onclick="loadBasketItemsInModal('${guidBasketId}', ${pagedData.currentPage + 1})"
                                ${!pagedData.hasNextPage ? 'disabled' : ''}
                                class="relative ml-3 inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed">
                            Sonraki
                        </button>
                    </div>
                    <div class="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
                        <div>
                            <p class="text-sm text-gray-700">
                                Toplam <span class="font-medium">${pagedData.totalItems}</span> üründen
                                <span class="font-medium">${((pagedData.currentPage - 1) * pagedData.pageSize) + 1}</span> -
                                <span class="font-medium">${Math.min(pagedData.currentPage * pagedData.pageSize, pagedData.totalItems)}</span> arası gösteriliyor
                            </p>
                        </div>
                        <div>
                            <nav class="isolate inline-flex -space-x-px rounded-md shadow-sm" aria-label="Pagination">
                                <button onclick="loadBasketItemsInModal('${guidBasketId}', ${pagedData.currentPage - 1})"
                                        ${!pagedData.hasPreviousPage ? 'disabled' : ''}
                                        class="relative inline-flex items-center rounded-l-md px-2 py-2 text-gray-400 ring-1 ring-inset ring-gray-300 hover:bg-gray-50 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed">
                                    <span class="sr-only">Önceki</span>
                                    <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                                        <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd" />
                                    </svg>
                                </button>

                                ${Array.from({length: pagedData.totalPages}, (_, i) => i + 1).map(pageNum => `
                                    <button onclick="loadBasketItemsInModal('${guidBasketId}', ${pageNum})"
                                            class="relative inline-flex items-center px-4 py-2 text-sm font-semibold ${pageNum === pagedData.currentPage ? 'bg-primary text-white focus:z-20 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary' : 'text-gray-900 ring-1 ring-inset ring-gray-300 hover:bg-gray-50 focus:z-20 focus:outline-offset-0'}">
                                        ${pageNum}
                                    </button>
                                `).join('')}

                                <button onclick="loadBasketItemsInModal('${guidBasketId}', ${pagedData.currentPage + 1})"
                                        ${!pagedData.hasNextPage ? 'disabled' : ''}
                                        class="relative inline-flex items-center rounded-r-md px-2 py-2 text-gray-400 ring-1 ring-inset ring-gray-300 hover:bg-gray-50 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed">
                                    <span class="sr-only">Sonraki</span>
                                    <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                                        <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd" />
                                    </svg>
                                </button>
                            </nav>
                        </div>
                    </div>
                </div>
            ` : ''}
        `;
    } else {
        container.innerHTML = `
            <div class="text-center py-8">
                <svg class="mx-auto h-12 w-12 text-gray-400" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="8" cy="21" r="1"></circle>
                    <circle cx="19" cy="21" r="1"></circle>
                    <path d="M2.05 2.05h2l2.66 12.42a2 2 0 0 0 2 1.58h9.78a2 2 0 0 0 1.95-1.57l1.65-7.43H5.12"></path>
                </svg>
                <h3 class="mt-2 text-sm font-medium text-gray-900">Sepet boş</h3>
                <p class="mt-1 text-sm text-gray-500">Bu sepette henüz ürün bulunmuyor.</p>
            </div>
        `;
    }
}
// Toplu mesaj gönderimi için global değişkenler
let availableChannels = [];
let enabledChannels = [];
let bulkMessageDialog = null;
let currentPreview = null;
let currentTab = 'new';
let jobHistoryData = [];

// Sayfa yüklendiğinde toplu mesaj butonunu kontrol et
document.addEventListener('DOMContentLoaded', function() {
    checkBulkMessagingAccess();
});

// Toplu mesaj erişimini kontrol et
async function checkBulkMessagingAccess() {
    try {
        const response = await fetch('/BulkMessaging/CheckModuleAccess');
        const result = await response.json();

        if (result.success) {
            enabledChannels = result.enabledChannels || [];
        } else {
            console.log('Bulk messaging not available:', result.message);
        }
    } catch (error) {
        console.error('Error checking bulk messaging access:', error);
    }
}

// Toplu mesaj modal'ını aç
function openBulkMessagingModal() {
    const container = document.getElementById('bulkMessagingContainer');
    
    // PushDialog instance yoksa oluştur
    if (!bulkMessageDialog) {
        bulkMessageDialog = new PushDialog({
            title: 'Toplu Mesaj Gönderimi',
            size: '3xl',
            content: container, // DOM elementini içeri aktar
            onClose: () => {
                // Dialog kapandığında container'ı kurtar ve DOM'a geri ekle (gizli olarak)
                if (container) {
                    document.body.appendChild(container);
                    container.classList.add('hidden');
                }
                resetBulkMessagingForm();
            }
        });
    }

    // Container'ı görünür yap (Dialog içine taşınacak)
    container.classList.remove('hidden');
    bulkMessageDialog.open();

    switchBulkMessageTab('new');
    initializeBulkMessaging();
}

// Toplu mesaj modal'ını kapat
function closeBulkMessagingModal() {
    if (bulkMessageDialog) {
        bulkMessageDialog.close();
    }
}

// Toplu mesaj formunu sıfırla
function resetBulkMessagingForm() {
    document.getElementById('bulkMessageTitle').value = '';
    document.getElementById('bulkMessageDescription').value = '';
    document.getElementById('customerStatusFilter').value = '';
    document.getElementById('emailPermissionFilter').value = 'true';
    document.getElementById('smsPermissionFilter').value = 'true';
    document.getElementById('customerSearchFilter').value = '';
    document.getElementById('recipientCount').textContent = '';
    document.getElementById('previewContent').classList.add('hidden');
    document.getElementById('previewCost').textContent = '';
//    document.getElementById('sendBulkMessageBtn').disabled = true;
    currentPreview = null;
}

// Toplu mesaj sistemini başlat
async function initializeBulkMessaging() {
    showBulkMessagingLoading();

    try {
        // Mevcut kanalları yükle
        const channelsResponse = await fetch('/BulkMessaging/GetAvailableChannels');
        const channelsResult = await channelsResponse.json();

        if (!channelsResult.success) {
            throw new Error(channelsResult.message);
        }

        availableChannels = channelsResult.data || [];

        // Kanal ayarlarını oluştur
        createChannelSettings();

        // Formu göster
        showBulkMessagingForm();

    } catch (error) {
        showBulkMessagingError(error.message);
    }
}

// Loading durumunu göster
function showBulkMessagingLoading() {
    document.getElementById('bulkMessagingLoading').classList.remove('hidden');
    document.getElementById('bulkMessagingForm').classList.add('hidden');
    document.getElementById('bulkMessagingError').classList.add('hidden');
}

// Form durumunu göster
function showBulkMessagingForm() {
    document.getElementById('bulkMessagingLoading').classList.add('hidden');
    document.getElementById('bulkMessagingForm').classList.remove('hidden');
    document.getElementById('bulkMessagingError').classList.add('hidden');
}

// Hata durumunu göster
function showBulkMessagingError(message) {
    document.getElementById('bulkMessagingLoading').classList.add('hidden');
    document.getElementById('bulkMessagingForm').classList.add('hidden');
    document.getElementById('bulkMessagingError').classList.remove('hidden');
    document.getElementById('bulkMessagingErrorMessage').textContent = message;
}

// Kanal ayarlarını oluştur
function createChannelSettings() {
    const container = document.getElementById('channelSettings');

    if (availableChannels.length === 0) {
        container.innerHTML = `
            <div class="text-center py-4 text-gray-500">
                <p>Henüz aktif iletişim kanalı bulunamadı.</p>
                <p class="text-sm">Lütfen önce Email veya WhatsApp entegrasyonlarınızı yapılandırın.</p>
            </div>
        `;
        return;
    }

    container.innerHTML = availableChannels.map(channel => `
        <div class="border border-gray-200 rounded-lg p-4 mb-4">
            <div class="flex items-center justify-between mb-3">
                <div class="flex items-center">
                    <input type="checkbox" id="channel_${channel.channelType}"
                           class="h-4 w-4 text-primary focus:ring-primary border-gray-300 rounded"
                           ${enabledChannels.includes(channel.channelType) ? 'checked' : ''}
                           onchange="toggleChannel('${channel.channelType}')">
                    <label for="channel_${channel.channelType}" class="ml-2 text-sm font-medium text-gray-900">
                        ${channel.displayName}
                    </label>
                </div>
                <span class="text-sm text-gray-500">${channel.formattedCostPerMessage} / mesaj</span>
            </div>

            <div id="channelConfig_${channel.channelType}" class="${enabledChannels.includes(channel.channelType) ? '' : 'hidden'}">
                <div class="grid grid-cols-1 gap-3">
                    <div>
                        <label class="block text-sm font-medium text-gray-700 mb-1">Şablon Seçin</label>
                        <select id="template_${channel.channelType}" class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent">
                            <option value="">Şablon seçin...</option>
                            ${channel.availableTemplates.map(template => `
                                <option value="${template.name}">${template.displayName}</option>
                            `).join('')}
                        </select>
                    </div>
                    ${channel.channelType === 'email' ? `
                        <div>
                            <label class="block text-sm font-medium text-gray-700 mb-1">Konu (Opsiyonel)</label>
                            <input type="text" id="subject_${channel.channelType}"
                                   class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent"
                                   placeholder="Email konusu">
                        </div>
                    ` : ''}
                </div>
            </div>
        </div>
    `).join('');
}

// Kanalı aç/kapat
function toggleChannel(channelType) {
    const checkbox = document.getElementById(`channel_${channelType}`);
    const config = document.getElementById(`channelConfig_${channelType}`);

    if (checkbox.checked) {
        config.classList.remove('hidden');
    } else {
        config.classList.add('hidden');
    }
}

// Hedef müşteri sayısını hesapla
async function estimateRecipients() {
    try {
        const filters = getCustomerFilters();

        const response = await fetch('/BulkMessaging/EstimateRecipients', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(filters)
        });

        const result = await response.json();

        if (result.success) {
            document.getElementById('recipientCount').textContent = `Hedef müşteri sayısı: ${result.count.toLocaleString()}`;
        } else {
            document.getElementById('recipientCount').textContent = `Hata: ${result.message}`;
        }
    } catch (error) {
        document.getElementById('recipientCount').textContent = `Hata: ${error.message}`;
    }
}

// Müşteri filtrelerini al
function getCustomerFilters() {
    const statusValue = document.getElementById('customerStatusFilter').value;
    const emailPermissionValue = document.getElementById('emailPermissionFilter').value;
    const smsPermissionValue = document.getElementById('smsPermissionFilter').value;

    return {
        isActive: statusValue === '' ? null : statusValue === 'true',
        emailPermission: emailPermissionValue === '' ? null : emailPermissionValue === 'true',
        smsPermission: smsPermissionValue === '' ? null : smsPermissionValue === 'true',
        searchTerm: document.getElementById('customerSearchFilter').value || null,
        cities: [],
        membershipDateFrom: null,
        membershipDateTo: null,
        lastLoginDateFrom: null,
        lastLoginDateTo: null
    };
}

// Kanal ayarlarını al
function getChannelSettings() {
    const settings = [];

    availableChannels.forEach(channel => {
        const checkbox = document.getElementById(`channel_${channel.channelType}`);
        if (checkbox && checkbox.checked) {
            const templateSelect = document.getElementById(`template_${channel.channelType}`);
            const subjectInput = document.getElementById(`subject_${channel.channelType}`);

            settings.push({
                channelType: channel.channelType,
                isEnabled: true,
                templateName: templateSelect.value,
                subject: subjectInput ? subjectInput.value : null
            });
        }
    });

    return settings;
}

// Önizleme oluştur
async function createPreview() {
    try {
        const title = document.getElementById('bulkMessageTitle').value;
        if (!title.trim()) {
            showNotification('Lütfen başlık girin.', 'warning');
            return;
        }

        const channelSettings = getChannelSettings();
        if (channelSettings.length === 0) {
            showNotification('Lütfen en az bir iletişim kanalı seçin ve şablon belirtin.', 'warning');
            return;
        }

        const model = {
            title: title,
            description: document.getElementById('bulkMessageDescription').value,
            customerFilters: getCustomerFilters(),
            channelSettings: channelSettings,
            enablePreview: true
        };

        const response = await fetch('/BulkMessaging/CreatePreview', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(model)
        });

        const result = await response.json();

        if (result.success) {
            currentPreview = result.data;
            displayPreview(result.data);
            // document.getElementById('sendBulkMessageBtn').disabled = !result.data.canAfford;
        } else {
            showNotification(`Önizleme oluşturulamadı: ${result.message}`, 'error');
        }
    } catch (error) {
        showNotification(`Hata: ${error.message}`, 'error');
    }
}

// Maliyet gösterim yardımcısı: 0 ise Ücretsiz, değilse ₺X.XX
function bulkFormatCost(cost) {
    if (!cost || cost === 0) return 'Ücretsiz';
    return `₺${cost.toFixed(2)}`;
}

// Önizlemeyi göster
function displayPreview(preview) {
    const container = document.getElementById('previewContent');

    container.innerHTML = `
        <div class="bg-gray-50 rounded-lg p-4">
            <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
                <div class="text-center">
                    <div class="text-2xl font-bold text-gray-900">${preview.estimatedRecipients.toLocaleString()}</div>
                    <div class="text-sm text-gray-500">Hedef Müşteri</div>
                </div>
                <div class="text-center">
                    <div class="text-2xl font-bold text-gray-900">${bulkFormatCost(preview.estimatedCost)}</div>
                    <div class="text-sm text-gray-500">Tahmini Maliyet</div>
                </div>
                <div class="text-center">
                    <div class="text-2xl font-bold ${preview.canAfford ? 'text-green-600' : 'text-red-600'}">${preview.canAfford ? 'Yeterli' : 'Yetersiz'}</div>
                    <div class="text-sm text-gray-500">Bakiye Durumu</div>
                </div>
            </div>

            ${preview.warningMessage ? `
                <div class="bg-red-50 border border-red-200 rounded-md p-3 mb-4">
                    <div class="flex">
                        <svg class="h-5 w-5 text-red-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
                        </svg>
                        <div class="ml-3">
                            <p class="text-sm text-red-800">${preview.warningMessage}</p>
                        </div>
                    </div>
                </div>
            ` : ''}

            <div class="space-y-3">
                <h5 class="font-medium text-gray-900">Kanal Detayları:</h5>
                ${preview.channelPreviews.map(channel => `
                    <div class="bg-white border border-gray-200 rounded-md p-3">
                        <div class="flex justify-between items-center">
                            <div>
                                <span class="font-medium">${channel.channelType === 'email' ? 'Email' : channel.channelType === 'sms' ? 'SMS' : 'WhatsApp'}</span>
                                <span class="text-sm text-gray-500 ml-2">${channel.templateName}</span>
                            </div>
                            <div class="text-right">
                                <div class="text-sm font-medium">${channel.formattedTotalCost}</div>
                                <div class="text-xs text-gray-500">${channel.estimatedRecipients} alıcı</div>
                            </div>
                        </div>
                    </div>
                `).join('')}
            </div>

            ${preview.sampleCustomers.length > 0 ? `
                <div class="mt-4">
                    <h5 class="font-medium text-gray-900 mb-2">Örnek Müşteriler (İlk 5):</h5>
                    <div class="space-y-2">
                        ${preview.sampleCustomers.map(customer => `
                            <div class="bg-white border border-gray-200 rounded-md p-2 text-sm">
                                <div class="flex justify-between">
                                    <span class="font-medium">${customer.fullName}</span>
                                    <span class="text-gray-500">${customer.availableChannels.join(', ')}</span>
                                </div>
                                <div class="text-gray-500">${customer.email}</div>
                            </div>
                        `).join('')}
                    </div>
                </div>
            ` : ''}
        </div>
    `;

    container.classList.remove('hidden');
    document.getElementById('previewCost').textContent = `Tahmini Maliyet: ${bulkFormatCost(preview.estimatedCost)}`;
}

// Toplu mesaj gönder
async function sendBulkMessage() {
    // Eğer önizleme yoksa, oluşturmayı dene
    if (!currentPreview) {
        await createPreview();
        if (!currentPreview) {
            // createPreview başarısız olduysa veya hata verdiyse dur
            return;
        }
    }

    if (!currentPreview.canAfford) {
        showNotification('Bakiyeniz bu işlem için yetersiz. Lütfen bakiye yükleyin.', 'error');
        return;
    }

    showConfirmationDialog('Toplu Mesaj', `${currentPreview.estimatedRecipients} müşteriye toplu mesaj gönderilecek. Tahmini maliyet: ${bulkFormatCost(currentPreview.estimatedCost)}. Devam etmek istiyor musunuz?`, 'Evet, Gönder', 'İptal', async () => {
        try {
            const model = {
                title: document.getElementById('bulkMessageTitle').value,
                description: document.getElementById('bulkMessageDescription').value,
                customerFilters: getCustomerFilters(),
                channelSettings: getChannelSettings(),
                enablePreview: true
            };

            const response = await fetch('/BulkMessaging/CreateBulkMessage', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(model)
            });

            const result = await response.json();

            if (result.success) {
                // Gönderimi başlat
                const startResponse = await fetch(`/BulkMessaging/StartBulkMessage?bulkMessageId=${result.bulkMessageId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    }
                });

                const startResult = await startResponse.json();

                if (startResult.success) {
                    showNotification('Toplu mesaj gönderimi başarıyla başlatıldı! İlerleme durumunu Store sayfasından takip edebilirsiniz.', 'success');
                    closeBulkMessagingModal();
                } else {
                    showNotification(`Gönderim başlatılamadı: ${startResult.message}`, 'error');
                }
            } else {
                showNotification(result.message || 'Hata oluştu', 'error');
            }
        } catch (error) {
             console.error('Error sending bulk message:', error);
             showNotification('Bir hata oluştu.', 'error');
        }
    });
}

// Tab switching fonksiyonu
function switchBulkMessageTab(tab) {
    currentTab = tab;

    // Tab butonlarını güncelle
    const newTab = document.getElementById('newMessageTab');
    const historyTab = document.getElementById('jobHistoryTab');
    const newContent = document.getElementById('newMessageContent');
    const historyContent = document.getElementById('jobHistoryContent');

    if (tab === 'new') {
        newTab.classList.add('text-primary', 'border-b-2', 'border-primary');
        newTab.classList.remove('text-gray-500');
        historyTab.classList.remove('text-primary', 'border-b-2', 'border-primary');
        historyTab.classList.add('text-gray-500');

        newContent.classList.remove('hidden');
        historyContent.classList.add('hidden');
    } else {
        historyTab.classList.add('text-primary', 'border-b-2', 'border-primary');
        historyTab.classList.remove('text-gray-500');
        newTab.classList.remove('text-primary', 'border-b-2', 'border-primary');
        newTab.classList.add('text-gray-500');

        historyContent.classList.remove('hidden');
        newContent.classList.add('hidden');

        // Job history'yi yükle
        loadJobHistory();
    }
}

// Job history yükleme
async function loadJobHistory() {
    showJobHistoryLoading();

    try {
        const response = await fetch('/BulkMessaging/GetJobHistory');
        const result = await response.json();

        if (result.success) {
            jobHistoryData = result.data || [];
            displayJobHistory();
        } else {
            showJobHistoryError(result.message);
        }
    } catch (error) {
        console.error('Error loading job history:', error);
        showJobHistoryError('Gönderim geçmişi yüklenirken hata oluştu.');
    }
}

// Job history yenileme
function refreshJobHistory() {
    if (currentTab === 'history') {
        loadJobHistory();
    }
}

// Job history loading durumu
function showJobHistoryLoading() {
    document.getElementById('jobHistoryLoading').classList.remove('hidden');
    document.getElementById('jobHistoryList').classList.add('hidden');
    document.getElementById('noJobsState').classList.add('hidden');
}

// Job history gösterme
function displayJobHistory() {
    document.getElementById('jobHistoryLoading').classList.add('hidden');

    if (jobHistoryData.length === 0) {
        document.getElementById('noJobsState').classList.remove('hidden');
        document.getElementById('jobHistoryList').classList.add('hidden');
        return;
    }

    const container = document.getElementById('jobHistoryList');
    container.innerHTML = jobHistoryData.map(job => createJobHistoryItem(job)).join('');
    container.classList.remove('hidden');
    document.getElementById('noJobsState').classList.add('hidden');
}

// Job history item oluşturma - IN-PLACE TOGGLE REDESIGN (ENHANCED VISIBILITY)
function createJobHistoryItem(job) {
    const statusColor = getJobStatusColor(job.status);
    const statusIcon = getJobStatusIcon(job.status);
    const progressPercentage = job.totalRecipients > 0 ? Math.round((job.processedRecipients / job.totalRecipients) * 100) : 0;
    
    // Unique ID for toggle
    const detailsId = `job-details-${job.id}`;
    const arrowId = `job-arrow-${job.id}`;
    const btnTextId = `job-btn-text-${job.id}`;
    
    return `
        <div class="group relative bg-white border border-gray-100 rounded-xl p-5 hover:shadow-xl hover:border-[#02379c]/30 transition-all duration-300 ring-1 ring-black/5 mb-4">
            <!-- Main Content (Always Visible) -->
            <div class="cursor-pointer" onclick="toggleJobDetails(${job.id})">
                <div class="flex flex-col sm:flex-row sm:items-start justify-between mb-4">
                    <div class="flex items-start space-x-3 mb-3 sm:mb-0">
                        <div class="flex-shrink-0 mt-1 transition-transform group-hover:scale-110 duration-300">
                            ${statusIcon}
                        </div>
                        <div>
                            <h4 class="text-base font-bold text-gray-900 group-hover:text-[#02379c] transition-colors flex items-center">
                                ${job.title}
                            </h4>
                            <p class="text-sm text-gray-500 mt-1 line-clamp-1">
                                 ${job.description || 'Açıklama belirtilmedi'}
                            </p>
                        </div>
                    </div>
                    <div class="flex flex-row sm:flex-col items-center sm:items-end justify-between sm:justify-start w-full sm:w-auto space-y-0 sm:space-y-2 pl-12 sm:pl-0">
                         <span class="inline-flex items-center px-3 py-1 rounded-full text-xs font-bold ${statusColor} shadow-sm">
                            ${job.status}
                        </span>
                        <div class="flex items-center text-xs text-gray-400">
                            <i class="fas fa-clock mr-1.5"></i>
                            <span>${formatDateForJob(job.createdAt)}</span>
                        </div>
                    </div>
                </div>

                <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-3">
                    <div class="text-center md:text-left">
                        <div class="text-xs font-medium text-gray-400">Hedef</div>
                        <div class="font-bold text-gray-900">${job.totalRecipients.toLocaleString()}</div>
                    </div>
                    <div class="text-center md:text-left">
                         <div class="text-xs font-medium text-gray-400">Başarılı</div>
                        <div class="font-bold text-emerald-600">${job.successfulSends.toLocaleString()}</div>
                    </div>
                     <div class="text-center md:text-left">
                         <div class="text-xs font-medium text-gray-400">Hatalı</div>
                        <div class="font-bold text-red-600">${job.failedSends.toLocaleString()}</div>
                    </div>
                     <div class="text-center md:text-right">
                         <div class="text-xs font-medium text-gray-400">Maliyet</div>
                        <div class="font-bold text-[#02379c]">${bulkFormatCost(job.actualCost || job.estimatedCost || 0)}</div>
                    </div>
                </div>

                <!-- Toggle Indicator Bar -->
                <div class="flex items-center justify-center pt-2 mt-2 border-t border-dashed border-gray-100 group-hover:border-[#02379c]/20 transition-colors">
                    <span class="text-xs font-semibold text-gray-400 group-hover:text-[#02379c] flex items-center transition-colors">
                        <span id="${btnTextId}">Detayları Göster</span>
                        <i id="${arrowId}" class="fas fa-chevron-down ml-1.5 transition-transform duration-300"></i>
                    </span>
                </div>
            </div>

            <!-- Expandable Details Section -->
            <div id="${detailsId}" class="hidden transition-all duration-300 ease-in-out mt-4">
                
                <!-- Expanded Progress (If Active) -->
                ${job.status === 'Gönderiliyor' ? `
                     <div class="mb-5 bg-[#02379c]/5 rounded-xl p-4 border border-[#02379c]/10">
                        <div class="flex justify-between items-end mb-2">
                            <div>
                                <span class="flex items-center text-xs font-bold text-[#02379c] uppercase tracking-wider">
                                    <span class="relative flex h-2 w-2 mr-2">
                                      <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-[#02379c] opacity-75"></span>
                                      <span class="relative inline-flex rounded-full h-2 w-2 bg-[#02379c]"></span>
                                    </span>
                                    Gönderim Sürüyor...
                                </span>
                                <div class="text-xs text-gray-500 mt-1 pl-4">Batch ${job.currentBatch || 0} / ${job.totalBatches || 0}</div>
                            </div>
                            <span class="text-lg font-black text-[#02379c] mb-[-4px]">${progressPercentage}%</span>
                        </div>
                        <div class="w-full bg-gray-200 rounded-full h-2 overflow-hidden">
                            <div class="bg-[#02379c] h-2 rounded-full transition-all duration-500 relative" style="width: ${progressPercentage}%">
                                 <div class="absolute inset-0 bg-white/20 w-full h-full animate-[shimmer_2s_infinite]"></div>
                            </div>
                        </div>
                    </div>
                ` : ''}

                <div class="grid grid-cols-1 md:grid-cols-2 gap-6 bg-gray-50 rounded-xl p-4">
                     <!-- Left Col: Details -->
                     <div class="space-y-3">
                        <h5 class="text-xs font-bold text-gray-900 uppercase tracking-wider border-b border-gray-200 pb-2">İşlem Detayları</h5>
                        
                        <div class="flex justify-between text-xs">
                            <span class="text-gray-500">ID & Batch</span>
                            <span class="font-mono text-gray-700">#${job.id} (Batch ${job.currentBatch || 0}/${job.totalBatches || 0})</span>
                        </div>
                         <div class="flex justify-between text-xs">
                            <span class="text-gray-500">Başlangıç</span>
                            <span class="text-gray-700">${formatDateForJob(job.createdAt)}</span>
                        </div>
                        <div class="flex justify-between text-xs">
                            <span class="text-gray-500">Bitiş</span>
                            <span class="text-gray-700">${job.completedAt ? formatDateForJob(job.completedAt) : '-'}</span>
                        </div>
                        
                        <div class="pt-2">
                             <span class="block text-xs text-gray-500 mb-1">Açıklama</span>
                             <div class="text-xs text-gray-700 bg-white p-2 rounded border border-gray-200">
                                ${job.description || 'Açıklama yok'}
                             </div>
                        </div>
                     </div>

                     <!-- Right Col: Finance & Errors -->
                     <div class="space-y-3">
                         <h5 class="text-xs font-bold text-gray-900 uppercase tracking-wider border-b border-gray-200 pb-2">Finans & Durum</h5>
                         
                         <div class="flex justify-between items-center bg-white p-2 rounded border border-gray-200">
                            <span class="text-xs text-gray-500">Tahmini Tutar</span>
                            <span class="text-xs font-bold text-gray-600">${bulkFormatCost(job.estimatedCost || 0)}</span>
                         </div>
                         <div class="flex justify-between items-center bg-[#02379c]/5 p-2 rounded border border-[#02379c]/10">
                            <span class="text-xs font-bold text-[#02379c]">Gerçekleşen</span>
                            <span class="text-sm font-black text-[#02379c]">${bulkFormatCost(job.actualCost || 0)}</span>
                         </div>

                         ${job.errorMessage ? `
                            <div class="mt-2 text-xs bg-red-50 text-red-700 p-2 rounded border border-red-100 flex items-start">
                                <i class="fas fa-exclamation-triangle mr-2 mt-0.5"></i>
                                <span>${job.errorMessage}</span>
                            </div>
                         ` : ''}
                     </div>
                </div>

                <!-- Footer Actions -->
                 <div class="pt-4 mt-2 border-t border-gray-200">
                    ${job.status === 'Gönderiliyor' ? `
                        <div class="flex justify-end">
                            <button onclick="refreshJobDetails(${job.id})" class="text-xs bg-white border border-[#02379c] text-[#02379c] px-3 py-1.5 rounded-lg hover:bg-[#02379c] hover:text-white transition-colors mr-2">
                                <i class="fas fa-sync-alt mr-1"></i> Durumu Yenile
                            </button>
                        </div>
                    ` : ''}
                    ${(job.status === 'Tamamlandı' || job.status === 'Hata') ? `
                        <div class="mt-2">
                            <h5 class="text-xs font-bold text-gray-900 uppercase tracking-wider mb-3 flex items-center">
                                <i class="fas fa-redo mr-2 text-[#02379c]"></i> Tekrar Gönder
                            </h5>
                            <p class="text-xs text-gray-500 mb-3">
                                Toplam <strong>${job.totalRecipients.toLocaleString()}</strong> alıcıdan belirli bir aralığa tekrar gönderim yapabilirsiniz.
                            </p>
                            <div class="grid grid-cols-1 sm:grid-cols-3 gap-3 items-end">
                                <div>
                                    <label class="block text-xs font-medium text-gray-600 mb-1">Başlangıç Sırası</label>
                                    <input type="number" id="resend-start-${job.id}" min="1" max="${job.totalRecipients}" value="1"
                                        class="w-full text-sm border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-[#02379c]/30 focus:border-[#02379c] transition-colors" 
                                        placeholder="Ör: 5000" />
                                </div>
                                <div>
                                    <label class="block text-xs font-medium text-gray-600 mb-1">Adet</label>
                                    <input type="number" id="resend-count-${job.id}" min="1" max="${job.totalRecipients}" value="${job.totalRecipients}"
                                        class="w-full text-sm border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-[#02379c]/30 focus:border-[#02379c] transition-colors" 
                                        placeholder="Ör: 2000" />
                                </div>
                                <div>
                                    <button onclick="resendBulkMessage(${job.id}, ${job.totalRecipients})" 
                                        class="w-full text-sm bg-[#02379c] text-white px-4 py-2 rounded-lg hover:bg-[#02379c]/90 transition-colors font-medium shadow-sm hover:shadow-md flex items-center justify-center">
                                        <i class="fas fa-paper-plane mr-2"></i> Tekrar Gönder
                                    </button>
                                </div>
                            </div>
                        </div>
                    ` : ''}
                 </div>
            </div>
        </div>
    `;
}

// Tekrar gönderim fonksiyonu
async function resendBulkMessage(jobId, totalRecipients) {
    const startInput = document.getElementById(`resend-start-${jobId}`);
    const countInput = document.getElementById(`resend-count-${jobId}`);
    
    const startIndex = parseInt(startInput.value);
    const count = parseInt(countInput.value);

    // Validasyon
    if (isNaN(startIndex) || startIndex < 1) {
        showNotification('Başlangıç sırası en az 1 olmalıdır.', 'warning');
        startInput.focus();
        return;
    }

    if (startIndex > totalRecipients) {
        showNotification(`Başlangıç sırası toplam alıcı sayısından (${totalRecipients.toLocaleString()}) büyük olamaz.`, 'warning');
        startInput.focus();
        return;
    }

    if (isNaN(count) || count < 1) {
        showNotification('Gönderilecek adet en az 1 olmalıdır.', 'warning');
        countInput.focus();
        return;
    }

    const endIndex = Math.min(startIndex + count - 1, totalRecipients);
    const actualCount = endIndex - startIndex + 1;

    showConfirmationDialog(
        'Tekrar Gönderim',
        `<strong>${startIndex.toLocaleString()}</strong>. sıradan başlayarak <strong>${actualCount.toLocaleString()}</strong> müşteriye tekrar gönderim yapılacak. Devam etmek istiyor musunuz?`,
        'Evet, Gönder',
        'İptal',
        async () => {
            try {
                const response = await fetch(`/BulkMessaging/ResendBulkMessage?originalBulkMessageId=${jobId}&startIndex=${startIndex}&count=${count}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    }
                });

                const result = await response.json();

                if (result.success) {
                    showNotification(result.message || 'Tekrar gönderim başarıyla başlatıldı!', 'success');
                    // Job history'yi yenile
                    loadJobHistory();
                } else {
                    showNotification(result.message || 'Tekrar gönderim başlatılamadı.', 'error');
                }
            } catch (error) {
                console.error('Error resending bulk message:', error);
                showNotification('Tekrar gönderim sırasında bir hata oluştu.', 'error');
            }
        }
    );
}

// Toggle function for job details
function toggleJobDetails(id) {
    const details = document.getElementById(`job-details-${id}`);
    const arrow = document.getElementById(`job-arrow-${id}`);
    const btnText = document.getElementById(`job-btn-text-${id}`);
    
    if (details.classList.contains('hidden')) {
        // Open
        details.classList.remove('hidden');
        arrow.classList.add('rotate-180');
        if(btnText) btnText.innerText = 'Detayları Gizle';
    } else {
        // Close
        details.classList.add('hidden');
        arrow.classList.remove('rotate-180');
        if(btnText) btnText.innerText = 'Detayları Göster';
    }
}

// Job status renkleri - Modernize edildi
function getJobStatusColor(status) {
    switch (status) {
        case 'Hazırlanıyor': return 'bg-amber-100 text-amber-800 ring-1 ring-amber-500/20';
        case 'Gönderiliyor': return 'bg-[#02379c]/10 text-[#02379c] ring-1 ring-[#02379c]/20';
        case 'Tamamlandı': return 'bg-emerald-100 text-emerald-800 ring-1 ring-emerald-500/20';
        case 'Hata': return 'bg-red-100 text-red-800 ring-1 ring-red-500/20';
        default: return 'bg-gray-100 text-gray-800 ring-1 ring-gray-500/20';
    }
}

// Job status ikonları - Modern ikonlar ve #02379c tema rengi
function getJobStatusIcon(status) {
    switch (status) {
        case 'Hazırlanıyor':
            return '<div class="p-2.5 bg-amber-50 rounded-xl text-amber-600 ring-1 ring-amber-100"><svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg></div>';
        case 'Gönderiliyor':
            // Kullanıcı rengi: #02379c
            return '<div class="p-2.5 bg-[#02379c]/5 rounded-xl text-[#02379c] ring-1 ring-[#02379c]/10"><svg class="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg></div>';
        case 'Tamamlandı':
             return '<div class="p-2.5 bg-emerald-50 rounded-xl text-emerald-600 ring-1 ring-emerald-100"><svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg></div>';
        case 'Hata':
            return '<div class="p-2.5 bg-red-50 rounded-xl text-red-600 ring-1 ring-red-100"><svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg></div>';
        default:
            return '<div class="p-2.5 bg-gray-50 rounded-xl text-gray-600 ring-1 ring-gray-100"><svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg></div>';
    }
}

// Tarih formatlama
function formatDateForJob(dateString) {
    if (!dateString) return '-';

    try {
        const date = new Date(dateString);
        if (isNaN(date.getTime()) || date.getFullYear() < 2000) return '-';

        return date.toLocaleDateString('tr-TR', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    } catch (e) {
        return '-';
    }
}

// Job detaylarını yenile
async function refreshJobDetails(jobId) {
    try {
        const response = await fetch(`/BulkMessaging/GetJobDetails/${jobId}`);
        const result = await response.json();

        if (result.success) {
            // Job history'deki ilgili item'ı güncelle
            const jobIndex = jobHistoryData.findIndex(job => job.id === jobId);
            if (jobIndex !== -1) {
                jobHistoryData[jobIndex] = result.data;
                displayJobHistory();
            }
        }
    } catch (error) {
        console.error('Error refreshing job details:', error);
    }
}

// Job detaylarını görüntüle - Eski Modal Fonksiyonları Kaldırıldı
/* 
    NOT: Toggle yapısına geçildiği için viewJobDetails ve 
    ilgili modal fonksiyonlarına artık ihtiyaç yok.
*/

// Job history hata durumu
function showJobHistoryError(message) {
    document.getElementById('jobHistoryLoading').classList.add('hidden');
    document.getElementById('jobHistoryList').classList.add('hidden');
    document.getElementById('noJobsState').classList.add('hidden');

    // Basit hata gösterimi
    const container = document.getElementById('jobHistoryList');
    container.innerHTML = `
        <div class="text-center py-8">
            <svg class="mx-auto h-12 w-12 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.732-.833-2.464 0L4.35 16.5c-.77.833.192 2.5 1.732 2.5z"></path>
            </svg>
            <h3 class="mt-2 text-sm font-medium text-gray-900">Hata Oluştu</h3>
            <p class="mt-1 text-sm text-gray-500">${message}</p>
            <button onclick="loadJobHistory()" class="mt-4 px-4 py-2 bg-primary text-white rounded-md hover:bg-primary-dark">
                Tekrar Dene
            </button>
        </div>
    `;
    container.classList.remove('hidden');
}



// ===== YENİ MÜŞTERİ EKLEME FONKSİYONLARI =====

// Yeni müşteri modal'ını aç
function openAddCustomerModal() {
    document.getElementById('addCustomerModal').classList.remove('hidden');
    resetAddCustomerForm();
}

// Yeni müşteri modal'ını kapat
function closeAddCustomerModal() {
    document.getElementById('addCustomerModal').classList.add('hidden');
    resetAddCustomerForm();
}

// Yeni müşteri formunu sıfırla
function resetAddCustomerForm() {
    document.getElementById('addCustomerForm').reset();
    document.getElementById('isActive').checked = true;
    document.getElementById('saveCustomerBtnText').textContent = 'Kaydet';
    document.getElementById('saveCustomerBtn').disabled = false;
}



// ===== EXCEL İMPORT FONKSİYONLARI =====

let currentImportJobId = null;
let importProgressInterval = null;
let customerImportConnection = null;

// Excel import modal'ını aç
function openBulkImportModal() {
    document.getElementById('bulkImportModal').classList.remove('hidden');
    resetImportModal();
}

// Excel import modal'ını kapat
function closeBulkImportModal() {
    document.getElementById('bulkImportModal').classList.add('hidden');
    if (importProgressInterval) {
        clearInterval(importProgressInterval);
        importProgressInterval = null;
    }
}

// Import modal'ını sıfırla
function resetImportModal() {
    // Tüm section'ları gizle
    document.getElementById('uploadSection').classList.remove('hidden');
    document.getElementById('progressSection').classList.add('hidden');
    document.getElementById('resultSection').classList.add('hidden');

    // Form'u sıfırla
    document.getElementById('bulkImportForm').reset();
    document.getElementById('selectedFileName').classList.add('hidden');

    // Progress'i sıfırla
    document.getElementById('progressBar').style.width = '0%';
    document.getElementById('progressText').textContent = 'Başlatılıyor...';
    document.getElementById('progressStats').textContent = '';

    // Result'ları sıfırla
    document.getElementById('successResult').classList.add('hidden');
    document.getElementById('errorResult').classList.add('hidden');

    currentImportJobId = null;
    if (importProgressInterval) {
        clearInterval(importProgressInterval);
        importProgressInterval = null;
    }
}





// Progress section'ını göster
function showProgressSection() {
    document.getElementById('uploadSection').classList.add('hidden');
    document.getElementById('progressSection').classList.remove('hidden');
    document.getElementById('resultSection').classList.add('hidden');
}

// Progress tracking başlat (SignalR ile real-time, fallback olarak polling)
function startProgressTracking() {
    if (!currentImportJobId) return;

    // SignalR bağlantısı varsa sadece onu kullan, yoksa polling'e geri dön
    if (customerImportConnection && customerImportConnection.state === signalR.HubConnectionState.Connected) {
        console.log("Using SignalR for real-time progress tracking");
        // SignalR ile real-time güncelleme alacağız, polling'e gerek yok
        return;
    }

    // Fallback: SignalR yoksa polling kullan
    console.log("Falling back to polling for progress tracking");
    importProgressInterval = setInterval(async () => {
        try {
            const response = await fetch(`/Customer/GetImportProgress?jobId=${currentImportJobId}`);
            const result = await response.json();

            if (result.success && result.data) {
                // Modal progress'i güncelle (eğer modal açıksa)
                updateProgress(result.data);

                // Tracking area progress'i güncelle
                updateImportTrackingProgress(result.data);

                if (result.data.isCompleted) {
                    clearInterval(importProgressInterval);
                    importProgressInterval = null;
                    // showResultSection(result.data); // Eski modal gösterimini devre dışı bırakıyoruz

                    // Handle Completion (Popup + Auto Hide)
                    handleImportCompletion(result.data);
                }
            } else {
                clearInterval(importProgressInterval);
                importProgressInterval = null;
                showError('İşlem durumu alınamadı.');

                // Tracking area'da da hata göster
                const statusText = document.getElementById('importStatusText');
                if (statusText) {
                    statusText.textContent = 'İşlem durumu alınamadı.';
                }
            }
        } catch (error) {
            console.error('Progress tracking error:', error);
            clearInterval(importProgressInterval);
            importProgressInterval = null;
            showError('İşlem takibi sırasında hata oluştu.');
        }
    }, 5000); // SignalR olmadığında 5 saniyede bir kontrol et (daha az yük)
}

// Progress güncelle
function updateProgress(data) {
    const progressBar = document.getElementById('progressBar');
    const progressText = document.getElementById('progressText');
    const progressStats = document.getElementById('progressStats');

    progressBar.style.width = `${data.progressPercentage}%`;

    if (data.status === 'Bekliyor') {
        progressText.textContent = 'İşlem sıraya alındı...';
    } else if (data.status === 'İşleniyor') {
        progressText.textContent = 'Dosya işleniyor...';
    }

    if (data.totalRows > 0) {
        progressStats.innerHTML = `
            <div class="grid grid-cols-2 gap-4 text-sm">
                <div>Toplam: ${data.totalRows}</div>
                <div>İşlenen: ${data.processedRows}</div>
                <div>Başarılı: ${data.successCount}</div>
                <div>Hata: ${data.errorCount}</div>
            </div>
        `;
    }
}

// Result section'ını göster
function showResultSection(data) {
    document.getElementById('progressSection').classList.add('hidden');
    document.getElementById('resultSection').classList.remove('hidden');

    if (data.status === 'Tamamlandı') {
        document.getElementById('successResult').classList.remove('hidden');
        document.getElementById('errorResult').classList.add('hidden');

        document.getElementById('successStats').innerHTML = `
            <div class="grid grid-cols-2 gap-4 text-sm">
                <div><strong>Toplam Satır:</strong> ${data.totalRows}</div>
                <div><strong>Başarılı:</strong> ${data.successCount}</div>
                <div><strong>Hata:</strong> ${data.errorCount}</div>
                <div><strong>Süre:</strong> ${data.formattedDuration || '-'}</div>
            </div>
            ${data.errorCount > 0 && data.errorDetails ? `
                <div class="mt-4 p-3 bg-yellow-50 border border-yellow-200 rounded-md">
                    <h4 class="text-sm font-medium text-yellow-800 mb-2">Hatalar:</h4>
                    <div class="text-xs text-yellow-700 max-h-32 overflow-y-auto">
                        ${data.errorDetails.split('\n').slice(0, 10).join('<br>')}
                        ${data.errorDetails.split('\n').length > 10 ? '<br>...' : ''}
                    </div>
                </div>
            ` : ''}
        `;
    } else {
        document.getElementById('successResult').classList.add('hidden');
        document.getElementById('errorResult').classList.remove('hidden');

        document.getElementById('errorMessage').textContent = data.errorDetails || 'Bilinmeyen bir hata oluştu.';
    }
}

// Hata göster
function showError(message) {
    document.getElementById('progressSection').classList.add('hidden');
    document.getElementById('resultSection').classList.remove('hidden');
    document.getElementById('successResult').classList.add('hidden');
    document.getElementById('errorResult').classList.remove('hidden');

    document.getElementById('errorMessage').textContent = message;
}

// ===== ŞABLON İNDİRME FONKSİYONU =====

function downloadExcelTemplate() {
    window.location.href = '/Customer/DownloadExcelTemplate';
}

// ===== İMPORT DETAYLARI MODAL FONKSİYONLARI =====

let currentDetailJobId = null;

// Import detayları modal'ını aç
function openImportDetailsModal() {
    if (!currentImportJobId) {
        showNotification('Aktif bir import işlemi bulunamadı.', 'error');
        return;
    }

    currentDetailJobId = currentImportJobId;
    document.getElementById('importDetailsModal').classList.remove('hidden');
    loadImportDetails();
}

// Import detayları modal'ını kapat
function closeImportDetailsModal() {
    document.getElementById('importDetailsModal').classList.add('hidden');
    currentDetailJobId = null;
}

// Import detaylarını yükle
async function loadImportDetails() {
    if (!currentDetailJobId) return;

    try {
        const response = await fetch(`/Customer/GetImportProgress?jobId=${currentDetailJobId}`);
        const result = await response.json();

        if (result.success && result.data) {
            updateImportDetailsModal(result.data);
        } else {
            showNotification('Import detayları yüklenemedi.', 'error');
        }
    } catch (error) {
        console.error('Error loading import details:', error);
        showNotification('Import detayları yüklenirken hata oluştu.', 'error');
    }
}

// Import detayları modal'ını güncelle
function updateImportDetailsModal(data) {
    // Dosya adı
    document.getElementById('detailFileName').textContent = data.fileName || '-';

    // Durum badge
    const statusBadge = document.getElementById('detailStatusBadge');
    statusBadge.textContent = data.status || '-';
    statusBadge.className = `inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getStatusBadgeClass(data.status)}`;

    // Zamanlar
    document.getElementById('detailStartedAt').textContent = data.formattedStartedAt || '-';
    document.getElementById('detailCompletedAt').textContent = data.formattedCompletedAt || '-';
    document.getElementById('detailDuration').textContent = data.formattedDuration || '-';

    // Progress
    const progressBar = document.getElementById('detailProgressBar');
    const progressText = document.getElementById('detailProgressText');
    progressBar.style.width = `${data.progressPercentage || 0}%`;
    progressText.textContent = `${data.progressPercentage || 0}%`;

    // İstatistikler
    document.getElementById('detailTotalRows').textContent = data.totalRows || 0;
    document.getElementById('detailSuccessCount').textContent = data.successCount || 0;
    document.getElementById('detailErrorCount').textContent = data.errorCount || 0;
    document.getElementById('detailProcessedRows').textContent = data.processedRows || 0;

    // Hata detayları
    const errorSection = document.getElementById('errorDetailsSection');
    const errorDetails = document.getElementById('detailErrorDetails');
    const errorCountBadge = document.getElementById('errorCountBadge');

    if (data.errorDetails && data.errorDetails.trim()) {
        errorSection.classList.remove('hidden');

        // Hata sayısını güncelle
        errorCountBadge.textContent = `${data.errorCount} hata`;

        // Hataları formatla
        const errors = data.errorDetails.split('\n').filter(error => error.trim());
        const formattedErrors = errors.map((error, index) => {
            const errorType = getErrorType(error);
            return `
                <div class="flex items-start space-x-2 py-2 ${index > 0 ? 'border-t border-red-100' : ''}">
                    <div class="flex-shrink-0 w-6 h-6 bg-red-100 rounded-full flex items-center justify-center">
                        <span class="text-xs font-medium text-red-600">${index + 1}</span>
                    </div>
                    <div class="flex-1">
                        <div class="text-sm text-red-800">${escapeHtml(error)}</div>
                        ${errorType ? `<div class="text-xs text-red-600 mt-1">${errorType}</div>` : ''}
                    </div>
                </div>
            `;
        }).join('');

        errorDetails.innerHTML = formattedErrors;
    } else {
        errorSection.classList.add('hidden');
    }
}

// Durum badge class'ını al
function getStatusBadgeClass(status) {
    switch (status) {
        case 'Bekliyor':
            return 'bg-yellow-100 text-yellow-800';
        case 'İşleniyor':
            return 'bg-blue-100 text-blue-800';
        case 'Tamamlandı':
            return 'bg-green-100 text-green-800';
        case 'Hata':
            return 'bg-red-100 text-red-800';
        default:
            return 'bg-gray-100 text-gray-800';
    }
}

// Import detaylarını yenile
function refreshImportDetails() {
    loadImportDetails();
    showNotification('Detaylar yenilendi.', 'success');
}

// Hata tipini belirle
function getErrorType(error) {
    if (error.includes('E-posta zaten mevcut')) {
        return 'Çözüm: Bu e-posta adresi zaten sistemde kayıtlı. Farklı bir e-posta kullanın.';
    } else if (error.includes('Müşteri kodu zaten mevcut')) {
        return 'Çözüm: Bu müşteri kodu zaten kullanımda. Benzersiz bir kod belirleyin.';
    } else if (error.includes('Geçersiz e-posta adresi')) {
        return 'Çözüm: E-posta adresini doğru formatta yazın (örnek: kullanici@domain.com).';
    } else if (error.includes('zorunludur')) {
        return 'Çözüm: Zorunlu alanları doldurun (Ad, Soyad, E-posta).';
    } else if (error.includes('IX_Customers_ExternalId_CompanyId')) {
        return 'Çözüm: Sistem hatası - müşteri kodu çakışması. Lütfen tekrar deneyin.';
    } else if (error.includes('duplicate key')) {
        return 'Çözüm: Bu müşteri bilgileri zaten mevcut. Benzersiz bilgiler kullanın.';
    }
    return null;
}

// HTML escape fonksiyonu
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ===== TELEFON NUMARASI FONKSİYONLARI =====

// Telefon numarasını formatla
function formatPhoneNumber(input) {
    // Sadece rakamları al
    let value = input.value.replace(/\D/g, '');

    // Türkiye telefon numarası formatı
    if (value.length > 0) {
        // 0 ile başlıyorsa kaldır
        if (value.startsWith('0')) {
            value = value.substring(1);
        }

        // 90 ile başlıyorsa kaldır (uluslararası format)
        if (value.startsWith('90') && value.length > 10) {
            value = value.substring(2);
        }

        // Maksimum 10 hane
        if (value.length > 10) {
            value = value.substring(0, 10);
        }

        // Formatla
        if (value.length > 0) {
            let formatted = '0';

            if (value.length > 0) {
                formatted += value.substring(0, 3);
            }
            if (value.length > 3) {
                formatted += ' ' + value.substring(3, 6);
            }
            if (value.length > 6) {
                formatted += ' ' + value.substring(6, 8);
            }
            if (value.length > 8) {
                formatted += ' ' + value.substring(8, 10);
            }

            input.value = formatted;
        }
    }
}

// Telefon numarası validasyonu
function validatePhoneNumber(phoneNumber) {
    if (!phoneNumber) return true; // Opsiyonel alan

    // Sadece rakamları al
    const digitsOnly = phoneNumber.replace(/\D/g, '');

    // 0 ile başlıyorsa kaldır
    let cleanNumber = digitsOnly;
    if (cleanNumber.startsWith('0')) {
        cleanNumber = cleanNumber.substring(1);
    }

    // 90 ile başlıyorsa kaldır
    if (cleanNumber.startsWith('90') && cleanNumber.length === 12) {
        cleanNumber = cleanNumber.substring(2);
    }

    // 10 haneli olmalı
    if (cleanNumber.length !== 10) {
        return false;
    }

    const firstDigit = cleanNumber[0];
    const secondDigit = cleanNumber[1];

    // Cep telefonu: 5XX ile başlar
    if (firstDigit === '5') {
        return true;
    }

    // Sabit hat: 2XX, 3XX, 4XX ile başlar ve ikinci hane 0 olamaz
    if ((firstDigit === '2' || firstDigit === '3' || firstDigit === '4') && secondDigit !== '0') {
        return true;
    }

    return false;
}

// Telefon input'larına event listener ekle
function setupPhoneInputs() {
    const phoneInput = document.getElementById('phone');
    const mobilePhoneInput = document.getElementById('mobilePhone');

    if (phoneInput) {
        phoneInput.addEventListener('input', function() {
            formatPhoneNumber(this);
        });

        phoneInput.addEventListener('blur', function() {
            if (this.value && !validatePhoneNumber(this.value)) {
                this.classList.add('border-red-500');
                this.classList.remove('border-gray-300');

                // Hata mesajı göster
                let errorMsg = this.parentNode.querySelector('.phone-error');
                if (!errorMsg) {
                    errorMsg = document.createElement('p');
                    errorMsg.className = 'phone-error mt-1 text-xs text-red-600';
                    this.parentNode.appendChild(errorMsg);
                }
                errorMsg.textContent = 'Geçerli bir Türkiye telefon numarası giriniz.';
            } else {
                this.classList.remove('border-red-500');
                this.classList.add('border-gray-300');

                // Hata mesajını kaldır
                const errorMsg = this.parentNode.querySelector('.phone-error');
                if (errorMsg) {
                    errorMsg.remove();
                }
            }
        });
    }

    if (mobilePhoneInput) {
        mobilePhoneInput.addEventListener('input', function() {
            formatPhoneNumber(this);
        });

        mobilePhoneInput.addEventListener('blur', function() {
            if (this.value && !validatePhoneNumber(this.value)) {
                this.classList.add('border-red-500');
                this.classList.remove('border-gray-300');

                // Hata mesajı göster
                let errorMsg = this.parentNode.querySelector('.phone-error');
                if (!errorMsg) {
                    errorMsg = document.createElement('p');
                    errorMsg.className = 'phone-error mt-1 text-xs text-red-600';
                    this.parentNode.appendChild(errorMsg);
                }
                errorMsg.textContent = 'Geçerli bir Türkiye cep telefonu numarası giriniz.';
            } else {
                this.classList.remove('border-red-500');
                this.classList.add('border-gray-300');

                // Hata mesajını kaldır
                const errorMsg = this.parentNode.querySelector('.phone-error');
                if (errorMsg) {
                    errorMsg.remove();
                }
            }
        });
    }
}

// ===== İMPORT TRAKİNG ALANI FONKSİYONLARI =====

// Import Result Modal Functions
function showImportResultModal(data) {
    const modal = document.getElementById('importResultModal');
    if (!modal) return;
    
    // Update Content
    document.getElementById('resSuccessCount').textContent = data.successCount || 0;
    document.getElementById('resErrorCount').textContent = data.errorCount || 0;
    document.getElementById('resUpdatedCount').textContent = data.updatedCount || 0;
    
    // Error Details Handling
    const errorContainer = document.getElementById('importErrorDetailsContainer');
    const errorDetails = document.getElementById('importErrorDetails');
    
    if (data.errorDetails && errorContainer && errorDetails) {
        errorDetails.textContent = data.errorDetails;
        errorContainer.classList.remove('hidden');
    } else if (errorContainer) {
        errorContainer.classList.add('hidden');
    }

    const title = document.getElementById('importResultTitle');
    const text = document.getElementById('importResultText');
    const icon = document.getElementById('importResultIcon');
    const iconBg = document.getElementById('importResultIconBg');
    
    if (data.status === 'Hata') {
        title.textContent = 'İşlem Başarısız';
        text.textContent = 'İçe aktarma işlemi sırasında kritik hatalar oluştu.';
        iconBg.className = 'mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-red-100';
        icon.className = 'h-6 w-6 text-red-600';
        icon.innerHTML = '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />';
    } else {
        if (data.errorCount > 0) {
            title.textContent = 'İşlem Kısmen Tamamlandı';
            text.textContent = 'İçe aktarma tamamlandı ancak bazı kayıtlarda hatalar oluştu.';
            iconBg.className = 'mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-yellow-100';
            icon.className = 'h-6 w-6 text-yellow-600';
            icon.innerHTML = '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />';
        } else {
            title.textContent = 'İşlem Başarıyla Tamamlandı';
            text.textContent = 'Tüm müşteri kayıtları başarıyla içe aktarıldı.';
            iconBg.className = 'mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-green-100';
            icon.className = 'h-6 w-6 text-green-600';
            icon.innerHTML = '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />';
        }
    }
    
    modal.classList.remove('hidden');
}

function hideImportResultModal() {
    const modal = document.getElementById('importResultModal');
    if (modal) modal.classList.add('hidden');
    
    // Refresh the customer list to show new data
    if (typeof loadCustomers === 'function') {
        loadCustomers();
    }
}

function handleImportCompletion(data) {
    console.log("Handle Import Completion Triggered with data:", data);

    // Sonuç verisini sakla (Detaylar butonu için)
    window.lastImportResult = data;

    // Listeyi yenile ki kullanıcı verileri görebilsin
    if (typeof loadCustomers === 'function') {
        loadCustomers();
    }
    
    // İşlem takibi alanındaki durumu güncelle (Hata varsa belirtelim)
    const statusText = document.getElementById('importStatusText');
    const trackingArea = document.getElementById('importTrackingArea');
    
    if (statusText && data.errorCount > 0) {
        statusText.textContent = 'İşlem tamamlandı ancak bazı kayıtlar aktarılamadı.';
        statusText.classList.remove('text-blue-700');
        statusText.classList.add('text-orange-600');
        
        if(trackingArea) {
             trackingArea.classList.remove('bg-blue-50', 'border-blue-200');
             trackingArea.classList.add('bg-orange-50', 'border-orange-200');
        }
    }

    // Modal'ı aç ve sonuçları detaylı göster
    const modal = document.getElementById('bulkImportModal');
    if (modal) {
        modal.classList.remove('hidden');
        
        // Section'ları ayarla
        const uploadSection = document.getElementById('uploadSection');
        const progressSection = document.getElementById('progressSection');
        const resultSection = document.getElementById('resultSection');
        
        if (uploadSection) uploadSection.classList.add('hidden');
        if (progressSection) progressSection.classList.add('hidden');
        if (resultSection) resultSection.classList.remove('hidden');

        // Sonuç içeriğini doldur
        const successResult = document.getElementById('successResult');
        const errorResult = document.getElementById('errorResult');
        
        // Helper to escape HTML (basit güvenlik önlemi)
        const escapeHtml = (text) => {
            if (!text) return '';
            return text.toString()
                .replace(/&/g, "&amp;")
                .replace(/</g, "&lt;")
                .replace(/>/g, "&gt;")
                .replace(/"/g, "&quot;")
                .replace(/'/g, "&#039;");
        };

        if (data.errorCount > 0) {
            if (successResult) successResult.classList.add('hidden');
            if (errorResult) {
                errorResult.classList.remove('hidden');
                
                const errorMsgDiv = document.getElementById('errorMessage');
                if (errorMsgDiv) {
                    let htmlContent = `
                        <div class="flex flex-wrap justify-center gap-2 text-sm text-gray-700 mb-4">
                            <span class="px-2 py-1 bg-gray-100 rounded border border-gray-200">Toplam: <strong>${data.totalRows || 0}</strong></span>
                            <span class="px-2 py-1 bg-green-50 text-green-700 rounded border border-green-100">Başarılı: <strong>${data.successCount || 0}</strong></span>
                            <span class="px-2 py-1 bg-red-50 text-red-700 rounded border border-red-100">Hatalı: <strong>${data.errorCount || 0}</strong></span>
                        </div>
                    `;

                    if (data.errorDetails) {
                        htmlContent += `
                            <div class="text-left bg-red-50 border border-red-200 rounded-md p-3 overflow-hidden">
                                <h4 class="text-xs font-bold text-red-800 uppercase mb-2 border-b border-red-200 pb-1 flex items-center">
                                    <svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.732-.833-2.464 0L4.35 16.5c-.77.833.192 2.5 1.732 2.5z"></path></svg>
                                    Hata Detayları
                                </h4>
                                <div class="max-h-64 overflow-y-auto custom-scrollbar">
                                    <pre class="whitespace-pre-wrap text-xs text-red-700 font-mono break-all">${escapeHtml(data.errorDetails)}</pre>
                                </div>
                            </div>
                        `;
                    }
                    errorMsgDiv.innerHTML = htmlContent;
                    
                    // Başlığı durumu yansıtacak şekilde güncelle
                    const parentData = errorResult.parentElement; // text-center div
                    // Direkt h3'ü bulmayı dene, her ihtimale karşı
                    const h3 = errorResult.querySelector('h3');
                    if(h3) h3.textContent = 'İçe Aktarma Tamamlandı';
                }
            }
        } else {
            if (errorResult) errorResult.classList.add('hidden');
            if (successResult) {
                successResult.classList.remove('hidden');
                const successStats = document.getElementById('successStats');
                if (successStats) {
                    successStats.innerHTML = `<p class="text-lg">Toplam <strong>${data.totalRows || 0}</strong> kayıt başarıyla yüklendi.</p>`;
                }
            }
        }
    }
}

// Import tracking alanını göster
function showImportTrackingArea() {
    const trackingArea = document.getElementById('importTrackingArea');
    if (trackingArea) {
        trackingArea.classList.remove('hidden');

        // İlk durumu ayarla
        document.getElementById('importStatusText').textContent = 'İşlem sıraya alındı...';
        document.getElementById('importProgressBar').style.width = '0%';
        document.getElementById('importTotalCount').textContent = '-';
        document.getElementById('importProcessedCount').textContent = '-';
        document.getElementById('importSuccessCount').textContent = '-';
        document.getElementById('importErrorCount').textContent = '-';
        document.getElementById('importActionButtons').classList.add('hidden');
    }
}

// Import tracking alanını gizle
function hideImportTrackingArea() {
    const trackingArea = document.getElementById('importTrackingArea');
    if (trackingArea) {
        trackingArea.classList.add('hidden');
    }

    // Progress tracking'i durdur
    if (importProgressInterval) {
        clearInterval(importProgressInterval);
        importProgressInterval = null;
    }
}

// Import tracking alanındaki progress'i güncelle
function updateImportTrackingProgress(data) {
    // Status text
    const statusText = document.getElementById('importStatusText');
    if (statusText) {
        if (data.status === 'Bekliyor') {
            statusText.textContent = 'İşlem sıraya alındı...';
        } else if (data.status === 'İşleniyor') {
            statusText.textContent = 'Dosya işleniyor...';
        } else if (data.status === 'Tamamlandı') {
            statusText.textContent = 'İşlem başarıyla tamamlandı!';
        } else if (data.status === 'Hata') {
            statusText.textContent = 'İşlem sırasında hata oluştu.';
        }
    }

    // Progress bar
    const progressBar = document.getElementById('importProgressBar');
    if (progressBar) {
        progressBar.style.width = `${data.progressPercentage}%`;
    }

    // Stats
    document.getElementById('importTotalCount').textContent = data.totalRows || '-';
    document.getElementById('importProcessedCount').textContent = data.processedRows || '-';
    document.getElementById('importSuccessCount').textContent = data.successCount || '-';
    document.getElementById('importErrorCount').textContent = data.errorCount || '-';

    // İşlem tamamlandıysa action butonlarını göster
    if (data.isCompleted) {
        document.getElementById('importActionButtons').classList.remove('hidden');

        // Progress tracking'i durdur
        if (importProgressInterval) {
            clearInterval(importProgressInterval);
            importProgressInterval = null;
        }
    }
}

// ===== SİGNALR BAĞLANTISI =====

// SignalR bağlantısını başlat
function initializeSignalRConnection() {
    if (customerImportConnection) {
        return; // Zaten bağlı
    }

    customerImportConnection = new signalR.HubConnectionBuilder()
        .withUrl("/customerImportHub")
        .withAutomaticReconnect()
        .build();

    // Import progress güncellemelerini dinle
    customerImportConnection.on("ImportProgressUpdate", function (progressData) {
        console.log("SignalR Progress Update:", progressData);

        // Eğer tracking area görünürse güncelle
        if (currentImportJobId === progressData.jobId) {
            updateImportTrackingProgress(progressData);

            // İşlem tamamlandıysa notification göster
            if (progressData.isCompleted) {
                // Handle Completion (Popup + Auto Hide)
                handleImportCompletion(progressData);

                // Progress tracking'i durdur
                if (importProgressInterval) {
                    clearInterval(importProgressInterval);
                    importProgressInterval = null;
                }
            }
        }
    });

    // Bağlantıyı başlat
    customerImportConnection.start()
        .then(function () {
            console.log("CustomerImport SignalR connection started");
            // Company grubuna katıl
            return customerImportConnection.invoke("JoinCompanyGroup");
        })
        .then(function () {
            console.log("Joined company group for import updates");
        })
        .catch(function (err) {
            console.error("CustomerImport SignalR connection error:", err);
        });

    // Bağlantı koptuğunda
    customerImportConnection.onclose(function () {
        console.log("CustomerImport SignalR connection closed");
        customerImportConnection = null;
    });

    // Yeniden bağlandığında
    customerImportConnection.onreconnected(function () {
        console.log("CustomerImport SignalR reconnected");
        // Company grubuna tekrar katıl
        customerImportConnection.invoke("JoinCompanyGroup");
    });
}

// SignalR bağlantısını kapat
function closeSignalRConnection() {
    if (customerImportConnection) {
        customerImportConnection.stop();
        customerImportConnection = null;
    }
}

// ===== EVENT LİSTENER'LAR =====

// DOM hazır olduğunda event listener'ları ekle
document.addEventListener('DOMContentLoaded', function() {
    loadCustomers();

    // SignalR bağlantısını başlat
    initializeSignalRConnection();

    // Telefon input'larını setup et
    setupPhoneInputs();
    // Buton event listener'ları ekle
    const addCustomerBtn = document.getElementById('addCustomerBtn');
    const bulkImportBtn = document.getElementById('bulkImportBtn');
    const downloadTemplateBtn = document.getElementById('downloadTemplateBtn');
    const bulkMessagingBtn = document.getElementById('bulkMessagingBtn');

    if (addCustomerBtn) {
        addCustomerBtn.addEventListener('click', openAddCustomerModal);
    }

    if (bulkImportBtn) {
        bulkImportBtn.addEventListener('click', openBulkImportModal);
    }

    if (downloadTemplateBtn) {
        downloadTemplateBtn.addEventListener('click', downloadExcelTemplate);
    }

    if (bulkMessagingBtn) {
        bulkMessagingBtn.addEventListener('click', openBulkMessagingModal);
    }

    // Import tracking area event listeners
    const hideTrackingBtn = document.getElementById('hideTrackingBtn');
    if (hideTrackingBtn) {
        hideTrackingBtn.addEventListener('click', hideImportTrackingArea);
    }

    const refreshCustomersBtn = document.getElementById('refreshCustomersBtn');
    if (refreshCustomersBtn) {
        refreshCustomersBtn.addEventListener('click', function() {
            loadCustomers();
            showNotification('Müşteri listesi yenilendi.', 'success');
        });
    }

    const viewImportDetailsBtn = document.getElementById('viewImportDetailsBtn');
    if (viewImportDetailsBtn) {
        viewImportDetailsBtn.addEventListener('click', function() {
            // Eğer elimizde son işlemin verisi varsa ve job ID eşleşiyorsa, client-side modal'ı aç (daha detaylı)
            if (window.lastImportResult && currentImportJobId && window.lastImportResult.jobId === currentImportJobId) {
                showImportResultModal(window.lastImportResult);
            } else {
                // Yoksa server'dan detayları çeken fonksiyonu kullan
                openImportDetailsModal();
            }
        });
    }

    // Excel file input event listener
    const excelFileInput = document.getElementById('excelFile');
    if (excelFileInput) {
        excelFileInput.addEventListener('change', function(e) {
            const file = e.target.files[0];
            const fileNameDiv = document.getElementById('selectedFileName');

            if (file) {
                fileNameDiv.textContent = `Seçilen dosya: ${file.name} (${(file.size / 1024 / 1024).toFixed(2)} MB)`;
                fileNameDiv.classList.remove('hidden');
            } else {
                fileNameDiv.classList.add('hidden');
            }
        });
    }

    // Form event listeners
    const addCustomerForm = document.getElementById('addCustomerForm');
    if (addCustomerForm) {
        addCustomerForm.addEventListener('submit', async function(e) {
            e.preventDefault();

            const btn = document.getElementById('saveCustomerBtn');
            const btnText = document.getElementById('saveCustomerBtnText');
            const originalText = btnText.textContent;

            // Button'u disable et
            btn.disabled = true;
            btnText.textContent = 'Kaydediliyor...';

            try {
                const formData = new FormData(this);
                const data = {};

                // Form verilerini object'e çevir
                for (let [key, value] of formData.entries()) {
                    if (this.querySelector(`[name="${key}"]`).type === 'checkbox') {
                        data[key] = this.querySelector(`[name="${key}"]`).checked;
                    } else if (this.querySelector(`[name="${key}"]`).type === 'number') {
                        data[key] = value ? parseFloat(value) : 0;
                    } else {
                        data[key] = value || null;
                    }
                }

                this.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
                    data[checkbox.name] = checkbox.checked;
                });

                const response = await fetch('/Customer/CreateCustomer', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(data)
                });

                const result = await response.json();

                if (result.success) {
                    showNotification('Müşteri başarıyla eklendi!', 'success');
                    closeAddCustomerModal();
                    loadCustomers(); // Müşteri listesini yenile
                } else {
                    showNotification(result.message || 'Müşteri eklenirken hata oluştu.', 'error');
                }
            } catch (error) {
                console.error('Error:', error);
                showNotification('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
            } finally {
                btn.disabled = false;
                btnText.textContent = originalText;
            }
        });
    }

    // Bulk import form event listener
    const bulkImportForm = document.getElementById('bulkImportForm');
    if (bulkImportForm) {
        bulkImportForm.addEventListener('submit', async function(e) {
            e.preventDefault();

            const fileInput = document.getElementById('excelFile');
            const file = fileInput.files[0];

            if (!file) {
                showNotification('Lütfen bir dosya seçin.', 'error');
                return;
            }

            // Dosya boyutu kontrolü
            if (file.size > 10 * 1024 * 1024) {
                showNotification('Dosya boyutu 10MB\'dan büyük olamaz.', 'error');
                return;
            }

            // Dosya formatı kontrolü
            const allowedTypes = ['.xlsx', '.xls'];
            const fileExtension = '.' + file.name.split('.').pop().toLowerCase();
            if (!allowedTypes.includes(fileExtension)) {
                showNotification('Sadece .xlsx ve .xls dosyaları desteklenir.', 'error');
                return;
            }

            try {
                const formData = new FormData();
                formData.append('file', file);

                const response = await fetch('/Customer/BulkImport', {
                    method: 'POST',
                    body: formData
                });

                const result = await response.json();

                if (result.success && result.jobId) {
                    currentImportJobId = result.jobId;

                    // Modal'ı kapat ve tracking alanını göster
                    closeBulkImportModal();
                    showImportTrackingArea();
                    startProgressTracking();

                    // showNotification('İçe aktarma işlemi başlatıldı! İşlemi yukarıdaki alandan takip edebilirsiniz.', 'success');
                } else {
                    showNotification(result.message || 'İçe aktarma başlatılırken hata oluştu.', 'error');
                }
            } catch (error) {
                console.error('Error:', error);
                showNotification('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
            }
        });
    }

    // Sayfa kapandığında SignalR bağlantısını kapat
    window.addEventListener('beforeunload', function() {
        closeSignalRConnection();
    });
});
