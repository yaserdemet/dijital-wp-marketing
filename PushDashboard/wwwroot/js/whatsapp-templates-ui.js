// whatsapp-templates-ui.js

$(document).ready(function () {
    // State
    let moduleSelectionDialog = null;
    const state = {
        currentTab: 'my-templates', // 'my-templates', 'approved-templates', 'template-library'
        viewMode: 'grid', // 'grid', 'list'
        statusFilter: 'ALL',
        categoryFilter: 'ALL',
        languageFilter: 'ALL',
        searchQuery: '',
        sortBy: 'updated_desc',
        allTemplates: window.initialTemplates || [],
        sampleTemplates: window.sampleTemplates || [],
        pendingCloneTemplate: null // Klonlanacak şablonu saklamak için
    };

    // DOM Elements - Cache them
    const $container = $('#templatesContainer');
    const $emptyState = $('#emptyState');
    const $statusFilters = $('#statusFilterContainer'); // Container for status radio buttons
    const $categoryFilter = $('#categoryFilter');

    // Initialize
    init();

    function init() {
        bindEvents();
        renderStatusFilters();
        renderCategoryOptions();
        applyFiltersAndRender();
        
        // Initialize Shared Module Selector
        if (typeof TemplateModuleSelector !== 'undefined') {
            TemplateModuleSelector.init({
                onModuleChange: (key) => {
                    const btn = document.getElementById('confirmModuleSelectionBtn');
                    if(btn) {
                        btn.disabled = false;
                        btn.classList.remove('opacity-50', 'cursor-not-allowed');
                    }
                }
            });
        }

        // Initialize PushDialog for module selection
        initModuleSelectionDialog();
    }

    function initModuleSelectionDialog() {
        const contentNode = document.getElementById('templateModuleSelectorContent');
        if (!contentNode || typeof PushDialog === 'undefined') return;

        // Build footer as a persistent DOM node so it can be re-used across opens
        const footerNode = document.createElement('div');
        footerNode.className = 'flex flex-row-reverse gap-3 w-full';

        const confirmBtn = document.createElement('button');
        confirmBtn.type = 'button';
        confirmBtn.id = 'confirmModuleSelectionBtn';
        confirmBtn.disabled = true;
        confirmBtn.className = 'inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-sm font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-primary disabled:opacity-50 disabled:cursor-not-allowed transition-colors';
        confirmBtn.textContent = 'Devam Et';
        confirmBtn.addEventListener('click', function () {
            const selectedModule = TemplateModuleSelector.getSelectedModule();
            if (selectedModule) {
                closeModuleSelectionModal();
                if (typeof window.showCreateTemplateModal === 'function') {
                    window.showCreateTemplateModal(selectedModule);
                    if (state.pendingCloneTemplate) {
                        setTimeout(() => {
                            if (typeof window.loadSampleIntoComponentBuilder === 'function' &&
                                typeof window.enableTemplateNameEditing === 'function') {
                                window.loadSampleIntoComponentBuilder(state.pendingCloneTemplate);
                                window.enableTemplateNameEditing();
                                if (state.pendingCloneTemplate.isSample) {
                                    $('#createTemplateName').val('');
                                } else {
                                    $('#createTemplateName').val(state.pendingCloneTemplate.name + '_copy');
                                }
                                state.pendingCloneTemplate = null;
                            }
                        }, 50);
                    }
                }
            }
        });

        const cancelBtn = document.createElement('button');
        cancelBtn.type = 'button';
        cancelBtn.className = 'inline-flex justify-center rounded-md border border-gray-200 px-4 py-2 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-primary transition-colors';
        cancelBtn.textContent = 'İptal';
        cancelBtn.addEventListener('click', function () {
            closeModuleSelectionModal();
        });

        footerNode.appendChild(confirmBtn);
        footerNode.appendChild(cancelBtn);

        moduleSelectionDialog = new PushDialog({
            title: 'Şablon Türü Seçimi',
            content: contentNode,
            footer: footerNode,
            stickyFooter: true,
            size: '4xl',
            onClose: () => {
                state.pendingCloneTemplate = null;
            }
        });
    }

    function bindEvents() {
        // Tab Switching
        $('.tab-btn').on('click', function () {
            $('.tab-btn').removeClass('active border-primary text-primary').addClass('border-transparent text-gray-500');
            $(this).addClass('active border-primary text-primary').removeClass('border-transparent text-gray-500');
            
            state.currentTab = $(this).data('tab');
            
            // Toggle Status Filter visibility
            if (state.currentTab === 'template-library') {
                $('#statusFilterWrapper').addClass('hidden');
            } else {
                $('#statusFilterWrapper').removeClass('hidden');
            }
            
            // Re-render categories based on the new tab's content
            renderCategoryOptions();

            // Adjust available filters based on tab? 
            // For now, filters apply to "my-templates" and "approved". Library has no status.
            applyFiltersAndRender();
        });

        // Search
        $('#templateSearch').on('input', function () {
            state.searchQuery = $(this).val();
            applyFiltersAndRender();
        });

        // Sort
        $('#sortSelect').on('change', function () {
            state.sortBy = $(this).val();
            applyFiltersAndRender();
        });

        // View Mode
        $('#viewGrid').on('click', function () {
            state.viewMode = 'grid';
            updateViewToggle();
            applyFiltersAndRender();
        });

        $('#viewList').on('click', function () {
            state.viewMode = 'list';
            updateViewToggle();
            applyFiltersAndRender();
        });

        // Status Filter
        $(document).on('change', 'input[name="statusFilter"]', function () {
            state.statusFilter = $(this).val();
            applyFiltersAndRender();
        });

        // Advanced Filters Toggle
        $('#advancedFiltersToggle').on('click', function() {
            const $content = $('#advancedFiltersContent');
            const $arrow = $('#advancedFiltersArrow');
            
            $content.toggleClass('hidden');
            $arrow.toggleClass('rotate-180');
        });

        // Category Filter
        $(document).on('click', '.filter-category-btn', function() {
            state.categoryFilter = $(this).data('value');
            renderCategoryOptions(); // Re-render to update active classes
            applyFiltersAndRender();
        });

        // Language Filter
        $('#languageFilter').on('change', function() {
            state.languageFilter = $(this).val();
            applyFiltersAndRender();
        });

        // Reset Filters
        $('#resetFilters').on('click', function() {
            state.searchQuery = '';
            $('#templateSearch').val('');
            state.statusFilter = 'ALL';
            state.categoryFilter = 'ALL';
            state.languageFilter = 'ALL';
            $('input[name="statusFilter"][value="ALL"]').prop('checked', true);
            renderCategoryOptions(); // Reset visual state
            $('#languageFilter').val('ALL');
            applyFiltersAndRender();
        });

        // Manage Dropdown Actions (Delegated)
        $(document).on('click', '.manage-btn', function (e) {
            e.stopPropagation();
            // Close other open dropdowns
            $('.manage-dropdown').addClass('hidden');
            
            const dropdown = $(this).siblings('.manage-dropdown');
            dropdown.toggleClass('hidden');
        });

        // Close dropdowns when clicking outside
        $(document).on('click', function () {
            $('.manage-dropdown').addClass('hidden');
        });

        // Action Buttons Delegation
        $(document).on('click', '.action-delete', function(e) {
            e.preventDefault();
            const name = $(this).data('name');
            // Call existing delete function if available in global scope
            if(typeof deleteTemplate === 'function') {
                deleteTemplate(name);
            } else {
                console.error("deleteTemplate function not found");
            }
        });

        $(document).on('click', '.action-view', function(e) {
             e.preventDefault();
             const id = $(this).data('id');
             if(typeof viewTemplate === 'function') {
                 viewTemplate(id);
             }
        });
        
        $(document).on('click', '.action-clone', function(e) {
             e.preventDefault();
             const name = $(this).data('name');
             const template = state.allTemplates.find(t => t.name === name);
             
             if(template) {
                 state.pendingCloneTemplate = template;
                 openModuleSelectionModal();
             }
        });

        // New Template Button
        $('#createTemplateBtn').on('click', function(e) {
            e.preventDefault();
            state.pendingCloneTemplate = null;
            openModuleSelectionModal();
        });
        
        // Handle "Use Sample" from Library
        $(document).on('click', '.action-use-sample', function(e) {
             e.preventDefault();
             const sampleId = $(this).data('id');
             const template = state.sampleTemplates.find(t => t.name === sampleId || t.id === sampleId);
             
             if (template) {
                // Use the same mechanism as cloning: set as pending template and open module selection
                state.pendingCloneTemplate = {
                    ...template,
                    isSample: true // flag to distinguish if needed (e.g. naming logic)
                };
                openModuleSelectionModal();
             }
        });

        // Copy to Clipboard
        $(document).on('click', '.action-copy', function(e) {
             e.preventDefault();
             // Determine if we need to get by name or if it's generic content
             // In the grid, I didn't verify if I added data-name to the copy button.
             // Let's check the HTML generation in renderActionsButton
             const name = $(this).data('name'); 
             
             let template = null;
             if (name) {
                 template = state.allTemplates.find(t => t.name === name);
             }
             
             // If we found a template object, copy it as JSON
             if (template) {
                 const text = JSON.stringify(template, null, 2);
                 navigator.clipboard.writeText(text).then(() => {
                     showNotification('Şablon verisi panoya kopyalandı!', 'success');
                 }).catch(err => {
                     console.error('Kopyalama hatası:', err);
                     showNotification('Kopyalama sırasında bir hata oluştu.', 'error');
                 });
             } else {
                 // Fallback if data attribute is missing or generic button
                 showNotification('Kopyalanacak veri bulunamadı.', 'error');
             }
        });

        // Compare Templates (Placeholder)
        $(document).on('click', '.action-compare', function(e) {
            e.preventDefault();
            const name = $(this).data('name');
            showNotification('Karşılaştırma özelliği henüz aktif değil: ' + name, 'info');
        });

        // Copy as cURL listener removed

        $(document).on('click', '.action-test', function(e) {
             e.preventDefault();
             const name = $(this).data('name');
             showNotification('Reis eğer gerekliyse backend için ilgili fonksiyon oluşturulunca bağlarız ya da kaldıralım dönüşünüze göre bakarız...', 'info'); 
        });

        $(document).on('click', '.action-preview', function(e) {
            e.preventDefault();
            const name = $(this).data('name');
            const template = state.allTemplates.find(t => t.name === name) || state.sampleTemplates.find(t => t.name === name);
            if(template) {
                if(typeof window.loadSampleIntoComponentBuilder === 'function') {
                   $('#previewModalName').text(template.name);
                   
                   const previewHtml = renderWhatsAppPreview(template, false); // isCard = false

                   $('#previewModal .chat-content').html(previewHtml);
                   $('#previewModal').removeClass('hidden');
                }
            }
        });

        $(document).on('click', '.action-analytics', function(e) {
            e.preventDefault();
            const templateId = $(this).data('id');
            const templateName = $(this).data('name');
            showAnalyticsModal(templateId, templateName);
        });

        // Analytics Modal Close
        $('#closeAnalyticsModalBtn, #closeAnalyticsModalBtn2').on('click', function() {
            $('#analyticsModal').addClass('hidden');
        });

        // Refresh Analytics
        $('#refreshAnalyticsBtn').on('click', function() {
            const templateId = $('#analyticsModal').data('templateId');
            const templateName = $('#analyticsModal').data('templateName');
            if (templateId) {
                fetchTemplateAnalytics(templateId);
            }
            if (templateName) {
                fetchTemplateConversions(templateName);
            }
        });

        $('#closePreviewModalBtn').on('click', function() {
            $('#previewModal').addClass('hidden');
        });
    }

    function updateViewToggle() {
        if(state.viewMode === 'grid') {
            $('#viewGrid').addClass('bg-white text-primary').removeClass('text-gray-500 hover:text-primary hover:bg-gray-200');
            $('#viewList').removeClass('bg-white text-primary').addClass('text-gray-500 hover:text-primary hover:bg-gray-200');
        } else {
            $('#viewList').addClass('bg-white text-primary').removeClass('text-gray-500 hover:text-primary hover:bg-gray-200');
            $('#viewGrid').removeClass('bg-white text-primary').addClass('text-gray-500 hover:text-primary hover:bg-gray-200');
        }
    }

    function renderStatusFilters() {
        // Calculate counts
        const counts = {
            'ALL': state.allTemplates.length,
            'APPROVED': 0,
            'REJECTED': 0,
            'PENDING': 0,
            'PAUSED': 0,
            'DISABLED': 0
        };

        state.allTemplates.forEach(t => {
            if (counts[t.status] !== undefined) {
                counts[t.status]++;
            } else {
                // Handle unknown statuses if any
            }
        });

        // Generate HTML
        const statuses = [
            { id: 'ALL', label: 'Tümü', color: 'green', count: counts['ALL'] },
            { id: 'APPROVED', label: 'Onaylandı', color: 'blue', count: counts['APPROVED'] },
            { id: 'PENDING', label: 'Bekliyor', color: 'yellow', count: counts['PENDING'] },
            { id: 'REJECTED', label: 'Reddedildi', color: 'red', count: counts['REJECTED'] },
            { id: 'PAUSED', label: 'Duraklatıldı', color: 'gray', count: counts['PAUSED'] || 0 }, // Assuming we might have this
            { id: 'DISABLED', label: 'Devre Dışı', color: 'gray', count: counts['DISABLED'] || 0 }
        ];

        let html = '';
        statuses.forEach(s => {
            const isChecked = state.statusFilter === s.id ? 'checked' : '';
            const bgClass = s.id === state.statusFilter ? `bg-${s.color}-50` : 'hover:bg-gray-50';
            const textClass = s.id === state.statusFilter ? `text-${s.color}-700` : 'text-gray-700';

            html += `
                <label class="flex items-center space-x-2 cursor-pointer p-2 rounded ${bgClass} ${textClass} font-medium transition-colors">
                    <input type="radio" name="statusFilter" value="${s.id}" ${isChecked} class="form-radio text-primary focus:ring-primary h-4 w-4 border-gray-300">
                    <span class="text-sm">${s.label}</span>
                    <span class="ml-auto text-xs text-gray-500 bg-white px-1.5 py-0.5 rounded border border-gray-100">${s.count}</span>
                </label>
            `;
        });

        $statusFilters.html(html);
    }

    function renderCategoryOptions() {
        const templates = state.currentTab === 'template-library' ? state.sampleTemplates : state.allTemplates;
        const categories = {};
        
        // Count categories
        templates.forEach(t => {
            if (t.category) {
                categories[t.category] = (categories[t.category] || 0) + 1;
            }
        });

        // Generate HTML for Category Filter
        let categoryHtml = '';
        const categoryList = Object.keys(categories).sort();
        
        // Add "All" option check
        const allActive = state.categoryFilter === 'ALL' ? 'bg-primary-50 text-primary border-primary-100' : 'bg-gray-50 text-gray-700 border-transparent hover:bg-gray-100';
        categoryHtml += `
            <button class="filter-category-btn w-full text-left px-3 py-2 rounded-md text-sm font-medium border flex justify-between items-center transition-all ${allActive}" data-value="ALL">
                <span>Tümü</span>
                <span class="text-xs bg-white bg-opacity-50 px-1.5 rounded-full">${templates.length}</span>
            </button>
        `;

        categoryList.forEach(cat => {
            const count = categories[cat];
            const isActive = state.categoryFilter === cat ? 'bg-primary-50 text-primary border-primary-100' : 'bg-gray-50 text-gray-700 border-transparent hover:bg-gray-100';
            categoryHtml += `
            <button class="filter-category-btn w-full text-left px-3 py-2 rounded-md text-sm font-medium border flex justify-between items-center transition-all ${isActive}" data-value="${cat}">
                <span>${cat}</span>
                <span class="text-xs bg-white bg-opacity-50 px-1.5 rounded-full text-gray-500">${count}</span>
            </button>
            `;
        });
        
        $('#categoryFilterContainer').html(categoryHtml);
    }

    function applyFiltersAndRender() {
        let filtered = [];

        if (state.currentTab === 'template-library') {
            filtered = state.sampleTemplates;
            // Library usually doesn't have status, but might have search
            if (state.searchQuery) {
                const q = state.searchQuery.toLowerCase();
                filtered = filtered.filter(t => 
                    t.name.toLowerCase().includes(q) || 
                    (t.components && JSON.stringify(t.components).toLowerCase().includes(q))
                );
            }
            // Hide filters sidebar for Library? Or keep it disabled.
            // For simplicity, we just ignore status filter for Library.
        } else {
            // My Templates
            filtered = state.allTemplates;

            // 1. Status Filter
            if (state.statusFilter !== 'ALL') {
                filtered = filtered.filter(t => t.status === state.statusFilter);
            }

            // 1.1 Category Filter
            if (state.categoryFilter !== 'ALL') {
                filtered = filtered.filter(t => t.category === state.categoryFilter);
            }

            // 1.2 Language Filter
            if (state.languageFilter !== 'ALL') {
                filtered = filtered.filter(t => {
                    if (state.languageFilter === 'en') {
                        return t.language === 'en' || t.language === 'en_US';
                    }
                    return t.language === state.languageFilter;
                });
            }

            // 2. Tab Specific Filter (Approved for "Pre-approved" tab?)
            if (state.currentTab === 'approved-templates') {
                 // Or whatever logic defines "Pre-approved". 
                 // Assuming "Approved" status for now.
                 filtered = filtered.filter(t => t.status === 'APPROVED');
            }

            // 3. Search
            if (state.searchQuery) {
                const q = state.searchQuery.toLowerCase();
                filtered = filtered.filter(t => 
                    t.name.toLowerCase().includes(q) || 
                    t.id.toLowerCase().includes(q) ||
                    (t.components && JSON.stringify(t.components).toLowerCase().includes(q))
                );
            }
        }

        // 4. Sort
        filtered.sort((a, b) => {
            if (state.sortBy === 'name_asc') return a.name.localeCompare(b.name);
            if (state.sortBy === 'name_desc') return b.name.localeCompare(a.name);
            // Updated Date logic (mocking if not available in object)
            // Assuming objects might not have `lastUpdated`, we might fallback to nothing or IDs
            // If the model has Updated field, use it. `FacebookMessageTemplate` doesn't seem to have `UpdatedAt`.
            // So we might only sort by name reliably.
            return 0;
        });

        renderTemplates(filtered);
    }

    function renderTemplates(templates) {
        if (templates.length === 0) {
            $container.addClass('hidden');
            $emptyState.removeClass('hidden');
            return;
        }

        $container.removeClass('hidden');
        $emptyState.addClass('hidden');

        // Update grid classes based on view mode
        if (state.viewMode === 'list') {
            $container.removeClass('grid-cols-1 md:grid-cols-2 xl:grid-cols-3').addClass('grid-cols-1');
        } else {
            $container.removeClass('grid-cols-1').addClass('grid-cols-1 md:grid-cols-2 xl:grid-cols-3');
        }

        let html = '';
        templates.forEach(t => {
            html += createTemplateCard(t);
        });

        $container.html(html);
    }

    function createTemplateCard(template) {
        // Safe check for properties
        const isSample = !template.status; // Samples don't have status usually
        const status = template.status || 'SAMPLE';
        const name = template.name || 'Untitled';
        const language = template.language || 'tr';
        
        let statusBadge = '';
        if (!isSample) {
            const statusColors = {
                'APPROVED': 'bg-emerald-50 text-emerald-700',
                'PENDING': 'bg-amber-50 text-amber-700',
                'REJECTED': 'bg-rose-50 text-rose-700',
                'DISABLED': 'bg-gray-100 text-gray-800'
            };
            const statusLabels = {
                'APPROVED': 'Onaylandı',
                'PENDING': 'Onay Bekliyor',
                'REJECTED': 'Reddedildi',
                'DISABLED': 'Devre Dışı'
            };
            statusBadge = `<span class="px-2 py-1 text-xs font-medium rounded-md ${statusColors[status] || 'bg-gray-100'}">${statusLabels[status] || status}</span>`;
        } else {
            statusBadge = `<span class="px-2 py-1 text-xs font-medium rounded-md bg-sky-50 text-sky-700">Şablon</span>`;
        }

        // Preview Content Extraction
        let previewText = "İçerik önizlemesi...";
        if (template.components) {
            const body = template.components.find(c => c.type === 'BODY');
            if (body && body.text) previewText = body.text;
        }
        
        // Truncate preview
        if (previewText.length > 100) previewText = previewText.substring(0, 100) + '...';

        const isList = state.viewMode === 'list';
        
        if (isList) {
             return `
                <div class="bg-white rounded-xl border border-gray-200 p-4 flex items-center justify-between hover:shadow-[0_2px_8px_rgba(0,0,0,0.05)] transition-shadow">
                    <div class="flex items-center gap-4 flex-1">
                        <div class="flex-shrink-0">
                           <div class="w-10 h-10 rounded-full bg-gray-100 flex items-center justify-center text-gray-500">
                                <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z" /></svg>
                           </div>
                        </div>
                        <div class="min-w-0 flex-1">
                            <div class="flex items-center gap-2 mb-1">
                                <h4 class="text-sm font-medium text-gray-900 truncate">${name}</h4>
                                ${statusBadge}
                                <span class="text-xs text-gray-500 border border-gray-200 px-1.5 rounded uppercase">${language}</span>
                            </div>
                            <p class="text-sm text-gray-500 truncate">${previewText}</p>
                        </div>
                    </div>
                    
                    <div class="flex items-center gap-4">
                        <span class="text-xs text-gray-400 hidden sm:block">Son güncelleme: -</span>
                        <div class="flex items-center">
                            ${!isSample ? `
                            <button class="action-analytics p-1.5 text-gray-400 hover:text-primary transition-colors" data-id="${template.id}" data-name="${template.name}" title="Analitikler">
                                <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                                </svg>
                            </button>
                            ` : ''}
                            ${renderActionsButton(template, isSample)}
                        </div>
                    </div>
                </div>
             `;
        } else {
            // Card View
            return `
                <div class="bg-white rounded-xl border border-gray-200 flex flex-col hover:shadow-[0_2px_8px_rgba(0,0,0,0.05)] transition-shadow duration-200 group relative h-full">
                    <!-- Header -->
                    <div class="p-3 border-b border-gray-100 flex justify-between items-start bg-white rounded-t-xl z-20 relative">
                        <div class="min-w-0 pr-2">
                            <div class="flex items-center gap-1">
                                <h4 class="font-semibold text-gray-900 truncate text-sm" title="${name}">${name}</h4>
                            </div>
                            <div class="flex items-center gap-2 mt-1">
                                ${statusBadge}
                                <span class="text-[10px] text-gray-500 uppercase border border-gray-100 px-1 rounded">${language}</span>
                            </div>
                        </div>
                        <div class="flex items-center">
                            ${!isSample ? `
                            <button class="action-analytics p-1.5 text-gray-400 hover:text-primary transition-colors" data-id="${template.id}" data-name="${template.name}" title="Analitikler">
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                                </svg>
                            </button>
                            ` : ''}
                            ${renderActionsButton(template, isSample)}
                        </div>
                    </div>
                    
                    <!-- Content - WhatsApp Preview -->
                    <div class="flex-1 min-h-[220px]">
                        ${renderWhatsAppPreview(template, true)}
                    </div>
                    
                    <!-- Footer -->
                    <div class="p-3 border-t border-gray-100 flex justify-between items-center text-xs text-gray-500 bg-white rounded-b-xl relative z-20">
                        <div class="flex items-center gap-1 opacity-100">
                             <svg class="w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                            <span>Son güncelleme: -</span>
                        </div>
                    </div>
                </div>
            `;
        }
    }

    function renderActionsButton(template, isSample) {
        if (isSample) {
             return `
             <div class="relative">
                <button class="manage-btn p-1.5 rounded-full hover:bg-gray-100 text-gray-400 hover:text-gray-600 transition-colors">
                    <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v.01M12 12v.01M12 19v.01M12 6a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2z" /></svg>
                </button>
                <div class="manage-dropdown hidden absolute right-0 top-8 w-48 bg-white rounded-lg shadow-[0_8px_30px_rgba(0,0,0,0.08)] border border-gray-200 py-1 z-10 text-sm">
                    <button class="action-use-sample w-full text-left px-4 py-2 hover:bg-gray-50 text-gray-700 flex items-center gap-2" data-id="${template.id}">
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2" /></svg>
                        Bu Şablonu Kullan
                    </button>

                    <button class="action-preview w-full text-left px-4 py-2 hover:bg-gray-50 text-gray-700 flex items-center gap-2" data-name="${template.name}">
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" /></svg>
                        Önizle
                    </button>
                </div>
            </div>
             `;
        }
        
        return `
            <div class="relative">
                <button class="manage-btn p-1.5 rounded-full hover:bg-gray-100 text-gray-400 hover:text-gray-600 transition-colors">
                    <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v.01M12 12v.01M12 19v.01M12 6a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2z" /></svg>
                </button>
                <div class="manage-dropdown hidden absolute right-0 top-8 w-48 bg-white rounded-lg shadow-[0_8px_30px_rgba(0,0,0,0.08)] border border-gray-200 py-1 z-10 text-sm">
                     ${template.status !== 'APPROVED' ? `
                    <button class="action-edit w-full text-left px-4 py-2 hover:bg-gray-50 text-gray-700 flex items-center gap-2" data-name="${template.name}">
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                        Düzenle
                    </button>
                    ` : ''}

                    <button class="action-copy w-full text-left px-4 py-2 hover:bg-gray-50 text-gray-700 flex items-center gap-2" data-name="${template.name}">
                         <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3m2 4H10m0 0l3-3m-3 3l3 3" /></svg>
                        Panoya Kopyala
                    </button>

                    <button class="action-clone w-full text-left px-4 py-2 hover:bg-gray-50 text-gray-700 flex items-center gap-2" data-name="${template.name}">
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2" /></svg>
                        Şablonu Kopyala
                    </button>

                    <button class="action-preview w-full text-left px-4 py-2 hover:bg-gray-50 text-gray-700 flex items-center gap-2" data-name="${template.name}">
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" /></svg>
                        Önizle
                    </button>
                     <div class="border-t border-gray-100 my-1"></div>
                    <button class="action-delete w-full text-left px-4 py-2 hover:bg-red-50 text-red-600 flex items-center gap-2" data-name="${template.name}">
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                        Sil
                    </button>
                </div>
            </div>
        `;
    }

    function renderWhatsAppPreview(template, isCard = true) {
        let headerHtml = '';
        let bodyText = '';
        let footerText = '';
        let buttonsHtml = '';

        if (template.components) {
            template.components.forEach(c => {
                if (c.type === 'HEADER') {
                    if (c.format === 'TEXT') {
                        headerHtml = `<div class="font-bold text-[#111b21] mb-1.5 text-sm">${c.text || ''}</div>`;
                    } else if (c.format === 'IMAGE' || c.format === 'VIDEO' || c.format === 'DOCUMENT') {
                        if (c.format === 'IMAGE' && c.text && (c.text.startsWith('http') || c.text.startsWith('data:image'))) {
                            headerHtml = `<div class="mb-2"><img src="${c.text}" class="w-full rounded-lg object-cover max-h-48" onerror="this.onerror=null;this.parentNode.innerHTML='<div class=\\'bg-gray-100 p-2 text-xs text-center\\'>Görsel Yüklenemedi</div>'"></div>`;
                        } else if (c.format === 'VIDEO' && c.text && (c.text.startsWith('http') || c.text.startsWith('data:video'))) {
                            headerHtml = `<div class="mb-2"><video src="${c.text}" controls class="w-full rounded-lg max-h-48"></video></div>`;
                        } else {
                            let icon = '';
                            let label = c.format;
                            if(c.format === 'IMAGE') icon = '<svg class="w-8 h-8 opacity-40" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>';
                            else if(c.format === 'VIDEO') icon = '<svg class="w-8 h-8 opacity-40" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg>';
                            else icon = '<svg class="w-8 h-8 opacity-40" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>';

                            headerHtml = `<div class="aspect-video w-full bg-[#f0f2f5] rounded-lg mb-2 flex flex-col items-center justify-center text-gray-500 text-xs border border-gray-100">
                                 ${icon}
                                 <span class="mt-1 font-medium tracking-wide text-[10px]">${label}</span>
                                 ${c.text ? `<span class="text-[9px] text-gray-400 mt-1 max-w-[90%] truncate">${c.text}</span>` : ''}
                            </div>`;
                        }
                    }
                } else if (c.type === 'BODY') {
                    bodyText = c.text || '';
                    // Handle variable highlighting - WhatsApp style
                    bodyText = bodyText.replace(/{{(\d+)}}/g, (match, p1) => {
                        let displayText = p1;
                        if (template.variableMapping && template.variableMapping[p1]) {
                            const val = template.variableMapping[p1];
                            // Try to resolve key to label using global predefinedVariables if available
                            if (window.predefinedVariables && window.predefinedVariables[val]) {
                                displayText = window.predefinedVariables[val].label;
                            } else {
                                displayText = val; // Fallback to value (it might be the label itself in old data)
                            }
                        } else if (template.variableMapping  && template.variableMapping[p1.toString()]) { 
                             const val = template.variableMapping[p1.toString()];
                             if (window.predefinedVariables && window.predefinedVariables[val]) {
                                displayText = window.predefinedVariables[val].label;
                            } else {
                                displayText = val;
                            }
                        }
                        
                        return `<span class="inline-block bg-[#f0f2f5] text-gray-600 px-1.5 rounded text-xs leading-5 border border-black/5 mx-0.5 font-medium" title="Variable ${p1}">{{${displayText}}}</span>`;
                    });
                    
                    // Simple Markdown support
                    bodyText = bodyText.replace(/\*([^*]+)\*/g, '<strong>$1</strong>');
                    bodyText = bodyText.replace(/_([^_]+)_/g, '<em>$1</em>');
                } else if (c.type === 'FOOTER') {
                    footerText = c.text || '';
                } else if (c.type === 'BUTTONS' && c.buttons) {
                     c.buttons.forEach(b => {
                        const icon = b.type === 'URL' ? 
                            '<svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"></path></svg>' 
                            : b.type === 'PHONE_NUMBER' ?
                            '<svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"></path></svg>'
                            : '<svg class="w-4 h-4 mr-2 transform rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6" /></svg>';
                            
                        buttonsHtml += `
                            <div class="bg-white border-t border-gray-100 text-[#00a884] font-medium text-sm py-2.5 px-4 hover:bg-gray-50 flex items-center justify-center cursor-pointer transition-colors w-full">
                                ${icon} <span>${b.text}</span>
                            </div>`;
                     });
                }
            });
        }
        
        // Classic WhatsApp Tail
        // Classic WhatsApp Tail
        const tailSvg = `
        <svg viewBox="0 0 8 13" height="13" width="8" class="absolute -left-[8px] top-0 fill-white block filter drop-shadow-sm">
            <path d="M1.533 3.568L8 12.193V1H2.812C1.042 1 .474 2.156 1.533 3.568z"></path>
        </svg>`;

        // Card View: h-full overflow-hidden
        // Modal View: flex-1 to fill the parent container (chat-content) which is min-h-full
        const wrapperClass = isCard 
            ? "relative bg-[#efe7dd] w-full h-full min-h-[260px] flex flex-col p-4 overflow-hidden" 
            : "relative bg-[#efe7dd] w-full flex-1 flex flex-col p-4"; // Changed min-h-full to flex-1

        return `
            <div class="${wrapperClass}">
                <!-- Chat Wallpaper Pattern -->
                <div class="absolute inset-0 opacity-[0.4]" style="background-image: url('https://user-images.githubusercontent.com/15075759/28719144-86dc0f70-73b1-11e7-911d-60d70fcded21.png'); background-repeat: repeat; background-size: 350px;"></div>

                <!-- Date Separator -->
                <div class="flex justify-center mb-4 relative z-10 w-full">
                    <span class="bg-white/80 backdrop-blur-sm text-gray-500 text-[10px] uppercase font-bold px-2 py-1 rounded-md shadow-sm border border-black/5">Bugün</span>
                </div>

                 <!-- Message Container -->
                 <div class="flex flex-col relative z-10 max-w-[92%] self-start filter drop-shadow-sm">
                      <!-- Message Bubble -->
                      <div class="bg-white rounded-lg rounded-tl-none relative overflow-visible shadow-sm">
                           ${tailSvg}
                           <div class="p-2 pb-1.5 min-w-[140px]">
                               ${headerHtml}
                               <div class="text-[#111b21] whitespace-pre-wrap leading-relaxed text-sm break-words">${bodyText}</div>
                               <div class="flex justify-between items-end mt-1 space-x-2">
                                    <span class="text-gray-400 text-[11px] leading-tight pt-1 truncate max-w-[150px]">${footerText}</span>
                                    <span class="text-[10px] text-gray-400 whitespace-nowrap ml-auto self-end mb-0.5">19:07</span>
                               </div>
                           </div>
                      </div>
                      
                      <!-- Buttons (Stacked below bubble) -->
                      ${buttonsHtml ? `<div class="mt-[2px] rounded-lg overflow-hidden shadow-sm flex flex-col divide-y divide-gray-100">${buttonsHtml}</div>` : ''}
                 </div>
            </div>
        `;
    }

    function openModuleSelectionModal() {
        if (!moduleSelectionDialog) return;
        moduleSelectionDialog.open();
        // Content is synchronously in DOM after open(); reset state
        if (typeof TemplateModuleSelector !== 'undefined') TemplateModuleSelector.reset();
        const btn = document.getElementById('confirmModuleSelectionBtn');
        if (btn) {
            btn.disabled = true;
            btn.classList.add('opacity-50', 'cursor-not-allowed');
        }
    }

    function closeModuleSelectionModal() {
        if (moduleSelectionDialog) moduleSelectionDialog.close();
    }

    // Analytics Functions
    function showAnalyticsModal(templateId, templateName) {
        // Store template ID and name for refresh
        $('#analyticsModal').data('templateId', templateId);
        $('#analyticsModal').data('templateName', templateName);
        $('#analyticsTemplateName').text(templateName);
        
        // Set default date range (end = tomorrow, start = 29 days before end)
        const endDate = new Date();
        endDate.setDate(endDate.getDate() + 1); // Tomorrow
        const startDate = new Date(endDate);
        startDate.setDate(startDate.getDate() - 29);
        
        $('#analyticsStartDate').val(formatDate(startDate));
        $('#analyticsEndDate').val(formatDate(endDate));
        
        // Show modal
        $('#analyticsModal').removeClass('hidden');
        
        // Fetch data
        fetchTemplateAnalytics(templateId);
        fetchTemplateConversions(templateName);
    }
    
    function formatDate(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
    
    function validateDateRange() {
        const startDate = new Date($('#analyticsStartDate').val());
        const endDate = new Date($('#analyticsEndDate').val());
        
        if (isNaN(startDate.getTime()) || isNaN(endDate.getTime())) {
            return { valid: false, message: 'Lütfen geçerli bir tarih aralığı seçin.' };
        }
        
        const diffTime = endDate - startDate;
        const diffDays = diffTime / (1000 * 60 * 60 * 24);
        
        if (diffDays < 0) {
            return { valid: false, message: 'Başlangıç tarihi bitiş tarihinden sonra olamaz.' };
        }
        
        if (diffDays >= 30) {
            return { valid: false, message: 'Tarih aralığı 30 günden az olmalıdır.' };
        }
        
        return { valid: true };
    }
    
    function fetchTemplateAnalytics(templateId) {
        const validation = validateDateRange();
        if (!validation.valid) {
            $('#analyticsLoading').addClass('hidden');
            $('#analyticsContent').addClass('hidden');
            $('#analyticsEmpty').addClass('hidden');
            $('#analyticsError').removeClass('hidden');
            $('#analyticsErrorMessage').text(validation.message);
            return;
        }
        
        const startDate = $('#analyticsStartDate').val();
        const endDate = $('#analyticsEndDate').val();
        
        // Show loading, hide others
        $('#analyticsLoading').removeClass('hidden');
        $('#analyticsContent').addClass('hidden');
        $('#analyticsError').addClass('hidden');
        $('#analyticsEmpty').addClass('hidden');
        
        // Build URL
        let url = `/WhatsAppTemplate/GetTemplateAnalytics?templateId=${encodeURIComponent(templateId)}`;
        if (startDate) url += `&startDate=${startDate}`;
        if (endDate) url += `&endDate=${endDate}`;
        
        $.ajax({
            url: url,
            type: 'GET',
            success: function(response) {
                $('#analyticsLoading').addClass('hidden');
                
                if (response.success) {
                    if (response.templates && response.templates.length > 0) {
                        displayAnalyticsData(response);
                    } else if (response.totals) {
                        displayAnalyticsData(response);
                    } else {
                        $('#analyticsEmpty').removeClass('hidden');
                    }
                } else {
                    $('#analyticsError').removeClass('hidden');
                    $('#analyticsErrorMessage').text(response.message || 'Analitik verileri yüklenemedi.');
                }
            },
            error: function(xhr, status, error) {
                $('#analyticsLoading').addClass('hidden');
                $('#analyticsError').removeClass('hidden');
                $('#analyticsErrorMessage').text('Bir hata oluştu. Lütfen tekrar deneyin.');
                console.error('Analytics fetch error:', error);
            }
        });
    }
    
    function displayAnalyticsData(data) {
        // Get first template data or use totals
        let stats = {
            sent: 0,
            delivered: 0,
            read: 0,
            failed: 0,
            deliveryRate: 0,
            readRate: 0,
            buttonClicks: null
        };
        
        if (data.templates && data.templates.length > 0) {
            const template = data.templates[0];
            stats.sent = template.sent || 0;
            stats.delivered = template.delivered || 0;
            stats.read = template.read || 0;
            stats.failed = template.failed || 0;
            stats.deliveryRate = template.deliveryRate || 0;
            stats.readRate = template.readRate || 0;
            stats.buttonClicks = template.buttonClicks;
        } else if (data.totals) {
            stats.sent = data.totals.totalSent || 0;
            stats.delivered = data.totals.totalDelivered || 0;
            stats.read = data.totals.totalRead || 0;
            stats.failed = data.totals.totalFailed || 0;
            // Calculate rates if not provided
            if (stats.sent > 0) {
                stats.deliveryRate = ((stats.delivered / stats.sent) * 100).toFixed(1);
                stats.readRate = ((stats.read / stats.delivered) * 100).toFixed(1);
            }
        }
        
        // Update stats
        $('#statSent').text(formatNumber(stats.sent));
        $('#statDelivered').text(formatNumber(stats.delivered));
        $('#statRead').text(formatNumber(stats.read));
        $('#statFailed').text(formatNumber(stats.failed));
        
        // Update rates
        const deliveryRate = parseFloat(stats.deliveryRate) || 0;
        const readRate = parseFloat(stats.readRate) || 0;
        
        $('#rateDelivery').text(deliveryRate.toFixed(1) + '%');
        $('#rateDeliveryBar').css('width', Math.min(deliveryRate, 100) + '%');
        
        $('#rateRead').text(readRate.toFixed(1) + '%');
        $('#rateReadBar').css('width', Math.min(readRate, 100) + '%');
        
        // Button clicks
        if (stats.buttonClicks && stats.buttonClicks.length > 0) {
            // Group button clicks by buttonText
            const buttonGroups = {};
            stats.buttonClicks.forEach(btn => {
                const key = btn.buttonText || 'Buton';
                if (!buttonGroups[key]) {
                    buttonGroups[key] = { total: 0, unique: 0 };
                }
                if (btn.type === 'unique_url_button') {
                    buttonGroups[key].unique = btn.clicks;
                } else {
                    buttonGroups[key].total = btn.clicks;
                }
            });
            
            let buttonHtml = '';
            Object.keys(buttonGroups).forEach(buttonText => {
                const group = buttonGroups[buttonText];
                buttonHtml += `
                    <div class="bg-white p-4 rounded-lg border border-gray-200">
                        <div class="flex items-center justify-between mb-2">
                            <span class="text-sm font-medium text-gray-800">${buttonText}</span>
                        </div>
                        <div class="grid grid-cols-2 gap-3">
                            <div class="bg-emerald-50 rounded-lg p-2 text-center">
                                <div class="text-lg font-semibold text-emerald-700">${formatNumber(group.total)}</div>
                                <div class="text-xs text-gray-500">Toplam Tıklama</div>
                            </div>
                            <div class="bg-sky-50 rounded-lg p-2 text-center">
                                <div class="text-lg font-semibold text-sky-700">${formatNumber(group.unique)}</div>
                                <div class="text-xs text-gray-500">Benzersiz Tıklama</div>
                            </div>
                        </div>
                    </div>
                `;
            });
            $('#buttonClicksList').html(buttonHtml);
            $('#buttonClicksSection').removeClass('hidden');
        } else {
            $('#buttonClicksSection').addClass('hidden');
        }
        
        // Show content
        $('#analyticsContent').removeClass('hidden');
    }
    
    function fetchTemplateConversions(templateName) {
        const startDate = $('#analyticsStartDate').val();
        const endDate = $('#analyticsEndDate').val();
        
        // Build URL
        let url = `/WhatsAppTemplate/GetTemplateConversions?templateName=${encodeURIComponent(templateName)}`;
        if (startDate) url += `&startDate=${startDate}`;
        if (endDate) url += `&endDate=${endDate}`;
        
        $.ajax({
            url: url,
            type: 'GET',
            success: function(response) {
                if (response.success) {
                    displayConversionData(response);
                } else {
                    // Hide conversion section if no data or error
                    $('#conversionSection').addClass('hidden');
                }
            },
            error: function(xhr, status, error) {
                console.error('Conversion fetch error:', error);
                $('#conversionSection').addClass('hidden');
            }
        });
    }
    
    function displayConversionData(data) {
        const orderCount = data.orderCount || 0;
        const totalRevenue = data.totalRevenue || 0;
        const currency = data.currency || 'TRY';
        const orders = data.orders || [];
        
        // Update stats
        $('#conversionOrderCount').text(formatNumber(orderCount));
        
        // Format revenue with currency
        const formattedRevenue = currency === 'TRY' ? 
            `₺${totalRevenue.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}` :
            `${totalRevenue.toLocaleString('tr-TR', { minimumFractionDigits: 2 })} ${currency}`;
        $('#conversionRevenue').text(formattedRevenue);
        
        // Show recent orders if available
        if (orders.length > 0) {
            let ordersHtml = '';
            orders.slice(0, 5).forEach(order => {
                ordersHtml += `
                    <div class="flex justify-between items-center text-xs bg-white p-2 rounded border border-gray-100">
                        <div>
                            <span class="font-medium text-gray-700">#${order.orderNumber}</span>
                            <span class="text-gray-500 ml-2">${order.customerName}</span>
                        </div>
                        <div class="text-right">
                            <span class="font-medium text-primary">₺${order.totalAmount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</span>
                            <span class="text-gray-400 ml-1">${order.orderDate}</span>
                        </div>
                    </div>
                `;
            });
            $('#conversionOrdersListContent').html(ordersHtml);
            $('#conversionOrdersList').removeClass('hidden');
        } else {
            $('#conversionOrdersList').addClass('hidden');
        }
        
        // Show conversion section
        $('#conversionSection').removeClass('hidden');
    }
    
    function formatNumber(num) {
        if (num >= 1000000) {
            return (num / 1000000).toFixed(1) + 'M';
        } else if (num >= 1000) {
            return (num / 1000).toFixed(1) + 'K';
        }
        return num.toString();
    }
});
