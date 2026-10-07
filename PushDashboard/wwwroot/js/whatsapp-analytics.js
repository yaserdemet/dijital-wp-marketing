// WhatsApp Analytics Dashboard JavaScript

(function() {
    'use strict';

    let currentPage = 1;
    let currentFilters = {};

    // Initialize on page load
    document.addEventListener('DOMContentLoaded', function() {
        initializeFilters();
        loadStatistics();
        loadMessages();
        loadTemplateAnalytics();
        
        // Template analytics refresh button
        const refreshBtn = document.getElementById('btn-refresh-template-analytics');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', function() {
                loadTemplateAnalytics();
            });
        }
        
        // Auto-refresh every 30 seconds
        setInterval(() => {
            loadStatistics();
            loadMessages();
        }, 30000);
        
        // Auto-refresh template analytics every 5 minutes
        setInterval(() => {
            loadTemplateAnalytics();
        }, 300000);
    });

    function initializeFilters() {
        // Set default date range (last 7 days)
        const endDate = new Date();
        const startDate = new Date();
        startDate.setDate(startDate.getDate() - 7);
        
        document.getElementById('filter-end-date').valueAsDate = endDate;
        document.getElementById('filter-start-date').valueAsDate = startDate;

        // Filter form submit
        document.getElementById('filter-form').addEventListener('submit', function(e) {
            e.preventDefault();
            currentPage = 1;
            applyFilters();
        });

        // Reset filters
        document.getElementById('btn-reset-filters').addEventListener('click', function() {
            document.getElementById('filter-form').reset();
            const endDate = new Date();
            const startDate = new Date();
            startDate.setDate(startDate.getDate() - 7);
            document.getElementById('filter-end-date').valueAsDate = endDate;
            document.getElementById('filter-start-date').valueAsDate = startDate;
            currentPage = 1;
            applyFilters();
        });
    }

    function applyFilters() {
        const formData = new FormData(document.getElementById('filter-form'));
        currentFilters = {};
        
        for (let [key, value] of formData.entries()) {
            if (value) {
                currentFilters[key] = value;
            }
        }
        
        loadStatistics();
        loadMessages();
    }

    function loadStatistics() {
        const params = new URLSearchParams(currentFilters);
        
        fetch(`/WhatsAppAnalytics/GetStatistics?${params}`)
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    updateStatistics(data.statistics);
                } else {
                    console.error('Failed to load statistics:', data.message);
                }
            })
            .catch(error => {
                console.error('Error loading statistics:', error);
            });
    }

    function updateStatistics(stats) {
        document.getElementById('stat-total-sent').textContent = stats.totalSent.toLocaleString();
        document.getElementById('stat-total-delivered').textContent = stats.totalDelivered.toLocaleString();
        document.getElementById('stat-total-read').textContent = stats.totalRead.toLocaleString();
        document.getElementById('stat-total-failed').textContent = stats.totalFailed.toLocaleString();
        
        document.getElementById('stat-delivery-rate').textContent = `${stats.deliveryRate}% teslim`;
        document.getElementById('stat-read-rate').textContent = `${stats.readRate}% okunma`;
        document.getElementById('stat-failure-rate').textContent = `${stats.failureRate}% başarısız`;
    }

    function loadTemplateAnalytics() {
        const params = new URLSearchParams(currentFilters);
        const tbody = document.getElementById('template-analytics-tbody');
        
        // Show loading state
        tbody.innerHTML = `
            <tr>
                <td colspan="7" class="px-6 py-8 text-center">
                    <svg class="animate-spin h-8 w-8 text-primary mx-auto" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    <p class="text-gray-500 mt-2">Şablon istatistikleri yükleniyor...</p>
                </td>
            </tr>
        `;
        
        fetch(`/WhatsAppAnalytics/GetTemplateAnalytics?${params}`)
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    renderTemplateAnalytics(data.templates);
                } else {
                    tbody.innerHTML = `
                        <tr>
                            <td colspan="7" class="px-6 py-8 text-center text-gray-500">
                                ${data.message || 'Şablon istatistikleri yüklenemedi'}
                            </td>
                        </tr>
                    `;
                }
            })
            .catch(error => {
                console.error('Error loading template analytics:', error);
                tbody.innerHTML = `
                    <tr>
                        <td colspan="7" class="px-6 py-8 text-center text-red-500">
                            Şablon istatistikleri yüklenirken bir hata oluştu
                        </td>
                    </tr>
                `;
            });
    }

    function renderTemplateAnalytics(templates) {
        const tbody = document.getElementById('template-analytics-tbody');
        
        if (!templates || templates.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="7" class="px-6 py-8 text-center">
                        <svg class="w-12 h-12 text-gray-400 mx-auto mb-2" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path>
                        </svg>
                        <p class="text-gray-500 text-lg">Şablon istatistiği bulunamadı</p>
                    </td>
                </tr>
            `;
            return;
        }
        
        tbody.innerHTML = templates.map(t => `
            <tr class="hover:bg-gray-50">
                <td class="px-6 py-4 whitespace-nowrap">
                    <span class="text-sm font-medium text-gray-900">${t.templateName}</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                    <span class="text-sm text-blue-600 font-semibold">${t.sent.toLocaleString()}</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                    <span class="text-sm text-green-600 font-semibold">${t.delivered.toLocaleString()}</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                    <span class="text-sm text-purple-600 font-semibold">${t.read.toLocaleString()}</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                    <span class="text-sm text-red-600 font-semibold">${t.failed.toLocaleString()}</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                    ${getPercentageBadge(t.deliveryRate, 'green')}
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                    ${getPercentageBadge(t.readRate, 'purple')}
                </td>
            </tr>
        `).join('');
    }

    function getPercentageBadge(rate, color) {
        const bgColor = color === 'green' ? 'bg-green-100 text-green-800' : 'bg-purple-100 text-purple-800';
        return `<span class="px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full ${bgColor}">${rate}%</span>`;
    }

    function loadMessages() {
        const params = new URLSearchParams({
            ...currentFilters,
            page: currentPage,
            pageSize: 50
        });

        const tbody = document.getElementById('messages-tbody');
        tbody.innerHTML = `
            <tr>
                <td colspan="6" class="text-center">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Yükleniyor...</span>
                    </div>
                </td>
            </tr>
        `;

        fetch(`/WhatsAppAnalytics/GetMessages?${params}`)
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    renderMessages(data.messages);
                    renderPagination(data.pagination);
                } else {
                    tbody.innerHTML = `
                        <tr>
                            <td colspan="6" class="text-center text-danger">
                                ${data.message || 'Mesajlar yüklenirken hata oluştu'}
                            </td>
                        </tr>
                    `;
                }
            })
            .catch(error => {
                console.error('Error loading messages:', error);
                tbody.innerHTML = `
                    <tr>
                        <td colspan="6" class="text-center text-danger">
                            Mesajlar yüklenirken bir hata oluştu
                        </td>
                    </tr>
                `;
            });
    }

    function renderMessages(messages) {
        const tbody = document.getElementById('messages-tbody');
        
        if (messages.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="6" class="px-6 py-8 text-center">
                        <svg class="w-12 h-12 text-gray-400 mx-auto mb-2" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M21.5 12H16c-.7 2-2 3-4 3s-3.3-1-4-3H2.5"></path>
                            <path d="M5.5 5.1L2 12v6c0 1.1.9 2 2 2h16a2 2 0 002-2v-6l-3.4-6.9A2 2 0 0016.8 4H7.2a2 2 0 00-1.8 1.1z"></path>
                        </svg>
                        <p class="text-gray-500 text-lg">Mesaj bulunamadı</p>
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = messages.map(msg => `
            <tr class="hover:bg-gray-50">
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">${msg.templateName}</td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">${formatPhone(msg.recipientPhone)}</td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">${msg.sentAt}</td>
                <td class="px-6 py-4 whitespace-nowrap">${getStatusBadge(msg.status)}</td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${msg.source || '-'}</td>
                <td class="px-6 py-4 whitespace-nowrap text-sm">
                    <button class="text-primary hover:text-primary-dark" onclick="showMessageDetails('${msg.id}', '${msg.gupshupMessageId}')" title="Detayları Gör">
                        <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"></path>
                            <circle cx="12" cy="12" r="3"></circle>
                        </svg>
                    </button>
                </td>
            </tr>
        `).join('');
    }

    function getStatusBadge(status) {
        const badges = {
            'pending': '<span class="px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full bg-gray-200 text-gray-800"><svg class="w-3 h-3 mr-1" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>Beklemede</span>',
            'sent': '<span class="px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full bg-blue-light text-blue"><svg class="w-3 h-3 mr-1" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m3 3 3 9-3 9 19-9Z"></path></svg>Gönderildi</span>',
            'delivered': '<span class="px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-light text-green"><svg class="w-3 h-3 mr-1" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"></path></svg>Teslim Edildi</span>',
            'read': '<span class="px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full bg-purple-light text-purple"><svg class="w-3 h-3 mr-1" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 8 9 19l-5-5"></path><path d="M22 12l-9 11-5-5"></path></svg>Okundu</span>',
            'failed': '<span class="px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full bg-red-light text-red"><svg class="w-3 h-3 mr-1" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><path d="m15 9-6 6"></path><path d="m9 9 6 6"></path></svg>Başarısız</span>'
        };
        return badges[status] || `<span class="px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full bg-gray-200 text-gray-800">${status}</span>`;
    }

    function formatPhone(phone) {
        if (!phone) return '-';
        // Format: 90532XXXXXXX -> +90 532 XXX XX XX
        if (phone.startsWith('90') && phone.length === 12) {
            return `+90 ${phone.substr(2, 3)} ${phone.substr(5, 3)} ${phone.substr(8, 2)} ${phone.substr(10, 2)}`;
        }
        return phone;
    }

    function renderPagination(pagination) {
        const paginationEl = document.getElementById('pagination');
        const paginationInfo = document.getElementById('pagination-info');
        
        // Update info
        const startRecord = (pagination.page - 1) * pagination.pageSize + 1;
        const endRecord = Math.min(pagination.page * pagination.pageSize, pagination.totalCount);
        paginationInfo.textContent = `${startRecord}-${endRecord} arası gösteriliyor (Toplam ${pagination.totalCount} mesaj)`;
        
        if (pagination.totalPages <= 1) {
            paginationEl.innerHTML = '';
            return;
        }

        let html = '';
        
        // Previous button
        if (pagination.page > 1) {
            html += `
                <button onclick="changePage(${pagination.page - 1})" class="px-3 py-1 border border-gray-300 rounded text-sm hover:bg-gray-50">
                    Önceki
                </button>
            `;
        }

        // Page numbers
        const maxPages = 5;
        let startPage = Math.max(1, pagination.page - Math.floor(maxPages / 2));
        let endPage = Math.min(pagination.totalPages, startPage + maxPages - 1);
        
        if (endPage - startPage < maxPages - 1) {
            startPage = Math.max(1, endPage - maxPages + 1);
        }

        for (let i = startPage; i <= endPage; i++) {
            if (i === pagination.page) {
                html += `
                    <button class="px-3 py-1 bg-primary text-white rounded text-sm">
                        ${i}
                    </button>
                `;
            } else {
                html += `
                    <button onclick="changePage(${i})" class="px-3 py-1 border border-gray-300 rounded text-sm hover:bg-gray-50">
                        ${i}
                    </button>
                `;
            }
        }

        // Next button
        if (pagination.page < pagination.totalPages) {
            html += `
                <button onclick="changePage(${pagination.page + 1})" class="px-3 py-1 border border-gray-300 rounded text-sm hover:bg-gray-50">
                    Sonraki
                </button>
            `;
        }

        paginationEl.innerHTML = html;
    }

    // Global functions
    window.changePage = function(page) {
        currentPage = page;
        loadMessages();
        window.scrollTo({ top: 0, behavior: 'smooth' });
    };

    window.showMessageDetails = function(messageId, gupshupMessageId) {
        const modal = document.getElementById('messageDetailModal');
        document.getElementById('modal-message-details').innerHTML = `
            <div class="space-y-2">
                <div class="flex justify-between py-2 border-b">
                    <span class="font-medium text-gray-700">Mesaj ID:</span>
                    <span class="text-gray-900">${messageId}</span>
                </div>
                <div class="flex justify-between py-2 border-b">
                    <span class="font-medium text-gray-700">ID:</span>
                    <span class="text-gray-900 font-mono text-sm">${gupshupMessageId}</span>
                </div>
                <p class="text-sm text-gray-500 mt-4">Daha fazla detay yakında eklenecek...</p>
            </div>
        `;
        modal.classList.remove('hidden');
    };

    window.closeMessageDetailModal = function() {
        document.getElementById('messageDetailModal').classList.add('hidden');
    };

})();
