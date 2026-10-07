
const TemplateModuleSelector = {
    selectedModule: null,
    options: {},

    init: function(options) {
        this.options = options || {};
        this.fetchModules();
    },

    fetchModules: function() {
        const container = document.getElementById('moduleListContainer');
        if (!container) return;

        fetch('/TemplateModule/GetModules')
            .then(res => res.json())
            .then(data => {
                if (data.success && data.modules) {
                    this.renderModules(data.modules);
                    this.bindEvents();
                } else {
                    container.innerHTML = '<div class="text-red-500">Modüller yüklenemedi.</div>';
                }
            })
            .catch(err => {
                console.error('Error loading modules:', err);
                container.innerHTML = '<div class="text-red-500">Bir hata oluştu.</div>';
            });
    },

    renderModules: function(modules) {
        const container = document.getElementById('moduleListContainer');
        let html = '';

        modules.forEach((module, index) => {
             let icon = this.getModuleIcon(module.key);
             let colorClass = this.getModuleColor(module.key);

             html += `
                <button type="button" class="module-type-option flex flex-col items-center justify-center text-center p-4 bg-white border border-gray-200 rounded-xl hover:border-primary-200 hover:bg-primary-50/50 hover:shadow-[0_2px_8px_rgba(0,0,0,0.05)] transition-all group focus:outline-none min-h-[120px]" data-module="${module.key}">
                    <div class="w-12 h-12 rounded-lg bg-${colorClass}-50 border border-${colorClass}-100 text-${colorClass}-600 flex items-center justify-center transition-colors mb-3">
                       ${icon}
                    </div>
                    <p class="text-sm font-medium text-gray-900 leading-tight">${module.name}</p>
                    ${module.description ? `<p class="text-xs text-gray-400 mt-1 line-clamp-3">${module.description}</p>` : ''}
                </button>
             `;
        });

        container.innerHTML = html;
    },

    getModuleIcon: function(key) {
        if (key === 'OrderStatus') return '<svg class="w-6 h-6" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" viewBox="0 0 24 24"><path d="M14 18V6a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2v11a1 1 0 0 0 1 1h2"></path><path d="M15 18H9"></path><path d="M19 18h2a1 1 0 0 0 1-1v-3.65a1 1 0 0 0-.22-.624l-3.48-4.35A1 1 0 0 0 17.52 8H14"></path><path d="M9 18a2 2 0 1 1-4 0 2 2 0 0 1 4 0Z"></path><path d="M19 18a2 2 0 1 1-4 0 2 2 0 0 1 4 0Z"></path></svg>';
        if (key === 'BulkMessaging') return '<svg class="w-6 h-6" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" viewBox="0 0 24 24"><path d="M14.536 21.686a.5.5 0 0 0 .937-.024l6.5-19a.496.496 0 0 0-.635-.635l-19 6.5a.5.5 0 0 0-.024.937l7.93 3.18a2 2 0 0 1 1.112 1.11z"></path><path d="m21.854 2.147-10.94 10.939"></path></svg>';
        if (key === 'BasketReminder') return '<svg class="w-6 h-6" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" viewBox="0 0 24 24"><path d="M9 21a1 1 0 1 1-2 0 1 1 0 0 1 2 0Z"></path><path d="M20 21a1 1 0 1 1-2 0 1 1 0 0 1 2 0Z"></path><path d="M2.05 2.05h2l2.66 12.42a2 2 0 0 0 2 1.58h9.78a2 2 0 0 0 1.95-1.57l1.65-7.43H5.12"></path></svg>';
        if (key === 'General') return '<svg class="w-6 h-6" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" viewBox="0 0 24 24"><path d="M4 3h5a1 1 0 0 1 1 1v5a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Z"></path><path d="M15 3h5a1 1 0 0 1 1 1v5a1 1 0 0 1-1 1h-5a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Z"></path><path d="M15 14h5a1 1 0 0 1 1 1v5a1 1 0 0 1-1 1h-5a1 1 0 0 1-1-1v-5a1 1 0 0 1 1-1Z"></path><path d="M4 14h5a1 1 0 0 1 1 1v5a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1v-5a1 1 0 0 1 1-1Z"></path></svg>';
        
        if (key === 'FirstOrder') return '<svg class="w-6 h-6" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" viewBox="0 0 24 24"><path d="M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4Z"></path><path d="M3 6h18"></path><path d="M16 10a4 4 0 0 1-8 0"></path></svg>';
        if (key === 'Birthday') return '<svg class="w-6 h-6" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" viewBox="0 0 24 24"><path d="M20 21v-8a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v8"></path><path d="M4 16s.5-1 2-1 2.5 2 4 2 2.5-2 4-2 2.5 2 4 2 2-1 2-1"></path><path d="M2 21h20"></path><path d="M7 8v3"></path><path d="M12 8v3"></path><path d="M17 8v3"></path><path d="M7 4h.01"></path><path d="M12 4h.01"></path><path d="M17 4h.01"></path></svg>';
        if (key === 'StockReminder') return '<svg class="w-6 h-6" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" viewBox="0 0 24 24"><path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9"></path><path d="M10.3 21a1.94 1.94 0 0 0 3.4 0"></path></svg>';
        if (key === 'GiftWheel') return '<svg class="w-6 h-6" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" viewBox="0 0 24 24"><rect x="3" y="8" width="18" height="4" rx="1"></rect><path d="M12 8v13"></path><path d="M19 12v7a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2v-7"></path><path d="M7.5 8a2.5 2.5 0 0 1 0-5A4.8 8 0 0 1 12 8a4.8 8 0 0 1 4.5-5 2.5 2.5 0 0 1 0 5"></path></svg>';
        if (key === 'FavoriteProduct') return '<svg class="w-6 h-6" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" viewBox="0 0 24 24"><path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z"></path></svg>';

        // Default
        return '<svg class="w-6 h-6" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" viewBox="0 0 24 24"><path d="M12.83 2.18a2 2 0 0 0-1.66 0L2.6 6.08a1 1 0 0 0 0 1.83l8.58 3.91a2 2 0 0 0 1.66 0l8.58-3.9a1 1 0 0 0 0-1.83Z"></path><path d="m22 17.65-9.17 4.16a2 2 0 0 1-1.66 0L2 17.65"></path><path d="m22 12.65-9.17 4.16a2 2 0 0 1-1.66 0L2 12.65"></path></svg>';
    },

    getModuleColor: function(key) {
        if (key === 'OrderStatus') return 'sky';
        if (key === 'BulkMessaging') return 'emerald';
        if (key === 'BasketReminder') return 'amber';
        if (key === 'General') return 'violet';
        if (key === 'FirstOrder') return 'indigo';
        if (key === 'Birthday') return 'pink';
        if (key === 'StockReminder') return 'orange';
        if (key === 'GiftWheel') return 'purple';
        if (key === 'FavoriteProduct') return 'red';
        return 'rose';
    },

    bindEvents: function() {
        const self = this;
        const options = document.querySelectorAll('.module-type-option');
        
        options.forEach(btn => {
            btn.addEventListener('click', function(e) {
                e.preventDefault(); 
                
                // Visual Update
                options.forEach(b => {
                    // Remove all potential color classes since we don't know the exact previous one easily without lookup
                    // Or just remove generic rings and borders which is what we added
                    b.classList.remove('ring-1', 'ring-primary', 'bg-primary-50', 'border-primary');
                    b.classList.add('border-gray-200');
                });

                this.classList.remove('border-gray-200');
                const key = this.dataset.module;
                const color = self.getModuleColor(key);
                this.classList.add('ring-1', 'ring-primary', 'bg-primary-50', 'border-primary');

                self.selectedModule = key;

                // Helper: Update common hidden input if present
                const hiddenInput = document.getElementById('commonModuleId');
                if (hiddenInput) hiddenInput.value = key; 

                if (typeof self.options.onModuleChange === 'function') {
                    self.options.onModuleChange(key);
                }

                // Remove any previously injected inline variable panels
                document.querySelectorAll('.module-inline-variables').forEach(el => el.remove());

                const slot = document.getElementById('moduleInlineVariablesSlot');
                if (slot) {
                    const varPanel = document.createElement('div');
                    varPanel.className = 'module-inline-variables';
                    varPanel.innerHTML = `
                        <div class="p-3 bg-${color}-50 border border-${color}-100 rounded-lg">
                            <p class="text-xs font-medium text-gray-600 mb-2">Bu modüldeki değişkenler:</p>
                            <div id="inlineModuleVariablesList" class="flex flex-wrap gap-1.5">
                                <span class="text-xs text-gray-400">Yükleniyor...</span>
                            </div>
                        </div>
                    `;
                    slot.appendChild(varPanel);
                    self.fetchAndRenderVariables('inlineModuleVariablesList', key);
                }
            });
        });
    },

    getSelectedModule: function() {
        return this.selectedModule;
    },
    
    // Helper to fetch and render variables into any container
    fetchAndRenderVariables: function(containerId, moduleKey, onClickCallback) {
        const container = document.getElementById(containerId);
        if (!container) return;
        
        container.innerHTML = '<div class="text-xs text-gray-400">Değişkenler yükleniyor...</div>';
        
        // Use the controller endpoint
        fetch(`/TemplateModule/GetVariables?moduleKey=${moduleKey}`)
            .then(res => res.json())
            .then(data => {
                if (data.success && data.variables && data.variables.length > 0) {
                     let html = '';
                     data.variables.forEach(v => {
                        html += `
                            <span class="inline-flex items-center px-2 py-1 rounded-md text-xs font-medium bg-white text-gray-600 border border-gray-200 variable-badge ${onClickCallback ? 'cursor-pointer hover:bg-primary-50 hover:text-primary hover:border-primary-100' : ''}" data-variable="{{${v.key}}}" title="{{${v.key}}}">
                                ${v.label}
                            </span>
                        `;
                     });
                     container.innerHTML = html;
                     
                     if (onClickCallback) {
                         container.querySelectorAll('.variable-badge').forEach(badge => {
                             badge.addEventListener('click', function() {
                                 onClickCallback(this.dataset.variable);
                             });
                         });
                     }
                } else {
                     container.innerHTML = '<div class="text-xs text-gray-400">Bu modül için değişken bulunmuyor.</div>';
                }
            })
            .catch(err => {
                console.error(err);
                container.innerHTML = '<div class="text-xs text-red-400">Değişkenler yüklenemedi.</div>';
            });
    },

    reset: function() {
        this.selectedModule = null;
        document.querySelectorAll('.module-type-option').forEach(b => {
            b.classList.remove('ring-1', 'ring-primary', 'bg-primary-50', 'border-primary');
            b.classList.add('border-gray-200');
        });
        const hiddenInput = document.getElementById('commonModuleId');
        if (hiddenInput) hiddenInput.value = '';

        // Remove inline variable panels
        document.querySelectorAll('.module-inline-variables').forEach(el => el.remove());
    }
};
