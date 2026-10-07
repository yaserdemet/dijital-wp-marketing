/**
 * SuperAdmin Company Switcher Module
 * Allows SuperAdmin users to switch between companies
 */
(function () {
    'use strict';

    const switcherBtn = document.getElementById('companySwitcherBtn');
    const dropdown = document.getElementById('companySwitcherDropdown');
    const chevron = document.getElementById('companySwitcherChevron');
    const searchInput = document.getElementById('companySearchInput');
    const companyList = document.getElementById('companyList');
    const resetBtn = document.getElementById('resetCompanyBtn');
    const selectedLabel = document.getElementById('selectedCompanyLabel');

    if (!switcherBtn || !dropdown) return;

    let companies = [];
    let isOpen = false;
    let isLoaded = false;

    // Toggle dropdown
    switcherBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        isOpen = !isOpen;

        if (isOpen) {
            dropdown.classList.remove('hidden');
            chevron.classList.add('rotate-180');
            if (!isLoaded) {
                loadCompanies();
            }
            setTimeout(() => searchInput?.focus(), 100);
        } else {
            closeDropdown();
        }
    });

    // Close on outside click
    document.addEventListener('click', function (e) {
        if (isOpen && !dropdown.contains(e.target) && !switcherBtn.contains(e.target)) {
            closeDropdown();
        }
    });

    // Close on Escape
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && isOpen) {
            closeDropdown();
        }
    });

    function closeDropdown() {
        isOpen = false;
        dropdown.classList.add('hidden');
        chevron.classList.remove('rotate-180');
    }

    // Search filter
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const query = this.value.toLowerCase().trim();
            const items = companyList.querySelectorAll('.company-item');
            items.forEach(item => {
                const name = item.getAttribute('data-name').toLowerCase();
                item.style.display = name.includes(query) ? '' : 'none';
            });
        });
    }

    // Reset company
    if (resetBtn) {
        resetBtn.addEventListener('click', async function (e) {
            e.stopPropagation();
            try {
                const response = await fetch('/api/superadmin/reset-company', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' }
                });
                if (response.ok) {
                    window.location.reload();
                }
            } catch (err) {
                console.error('Reset company failed:', err);
            }
        });
    }

    // Load companies from API
    async function loadCompanies() {
        try {
            const response = await fetch('/api/superadmin/companies');
            if (!response.ok) throw new Error('API error');

            companies = await response.json();
            isLoaded = true;
            renderCompanies(companies);
        } catch (err) {
            console.error('Failed to load companies:', err);
            companyList.innerHTML = `
                <div class="px-4 py-6 text-sm text-red-500 text-center">
                    <svg class="w-6 h-6 mx-auto mb-2" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z" />
                    </svg>
                    <span>Company'ler yüklenemedi</span>
                </div>
            `;
        }
    }

    function renderCompanies(list) {
        if (list.length === 0) {
            companyList.innerHTML = `
                <div class="px-4 py-6 text-sm text-gray-500 text-center">
                    Henüz company bulunmuyor.
                </div>
            `;
            return;
        }

        const currentName = selectedLabel?.textContent?.trim();

        companyList.innerHTML = list.map(c => {
            const isSelected = c.name === currentName;
            const rawWebsite = c.website || c.Website || '';
            const websiteDisplay = rawWebsite ? rawWebsite.replace(/^https?:\/\//, '').replace(/\/$/, '') : '';
            return `
                <button type="button" 
                    class="company-item w-full text-left px-4 py-2.5 text-sm flex items-center hover:bg-amber-50 transition-colors duration-150 border-b border-gray-50 last:border-0 ${isSelected ? 'bg-amber-50 font-semibold' : ''}"
                    data-id="${c.id}" 
                    data-name="${c.name}"
                    onclick="switchCompany('${c.id}', '${c.name.replace(/'/g, "\\'")}')">
                    <div class="w-7 h-7 rounded-full ${isSelected ? 'bg-amber-500 text-white' : 'bg-gray-100 text-gray-500'} flex items-center justify-center mr-3 flex-shrink-0 text-xs font-bold">
                        ${c.name.charAt(0).toUpperCase()}
                    </div>
                    <div class="flex flex-col flex-1 min-w-0">
                        <span class="truncate">${c.name}</span>
                        ${websiteDisplay ? `<span class="text-[10px] text-gray-400 truncate">${websiteDisplay}</span>` : ''}
                    </div>
                    ${isSelected ? '<svg class="w-4 h-4 text-amber-500 flex-shrink-0 ml-2" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" /></svg>' : ''}
                </button>
            `;
        }).join('');
    }

    // Global switch function
    window.switchCompany = async function (companyId, companyName) {
        try {
            // Show loading state on button
            switcherBtn.classList.add('opacity-50', 'pointer-events-none');

            const response = await fetch('/api/superadmin/switch-company', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ companyId: companyId })
            });

            if (response.ok) {
                window.location.reload();
            } else {
                const data = await response.json();
                alert(data.message || 'Company değiştirilemedi.');
                switcherBtn.classList.remove('opacity-50', 'pointer-events-none');
            }
        } catch (err) {
            console.error('Switch company failed:', err);
            alert('Bir hata oluştu.');
            switcherBtn.classList.remove('opacity-50', 'pointer-events-none');
        }
    };
})();
