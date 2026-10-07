/**
 * Gift Wheel Admin JavaScript
 * Handles all admin interface interactions for the Gift Wheel module
 */

class GiftWheelAdmin {
  constructor() {
    this.currentWheelId = null;
    this.currentPrizes = [];
    this.isLoading = false;

    this.init();
  }

  init() {
    this.setupEventListeners();
    this.setupTabNavigation();
    this.loadInitialData();
    this.initLogsEventListeners();
  }

  setupEventListeners() {
    // Wheel management
    document.getElementById("createWheelBtn")?.addEventListener("click", () => this.createWheel());
    document.getElementById("toggleWheelStatus")?.addEventListener("click", (e) => this.toggleWheelStatus(e));
    document.getElementById("exportToR2Btn")?.addEventListener("click", () => this.exportToR2());

    // Data Export
    document.getElementById("exportExcelBtn")?.addEventListener("click", () => this.exportData('Excel'));
    document.getElementById("exportCsvBtn")?.addEventListener("click", () => this.exportData('Csv'));
    document.getElementById("exportPdfBtn")?.addEventListener("click", () => this.exportData('Pdf'));

    // Prize management
    document.getElementById("addPrizeBtn")?.addEventListener("click", () => this.openPrizeModal());
    document.getElementById("addFirstPrizeBtn")?.addEventListener("click", () => this.openPrizeModal());
    document.getElementById("removeAllPrizesBtn")?.addEventListener("click", () => this.removeAllPrizes());

    // Modal close buttons
    document.querySelectorAll("[data-modal-close]").forEach((btn) => {
      btn.addEventListener("click", () => this.closePrizeModal());
    });

    // Close modal when clicking outside
    document.getElementById("prizeModal")?.addEventListener("click", (e) => {
      if (e.target.id === "prizeModal") {
        this.closePrizeModal();
      }
    });

    // Quick Prize Selection
    document.body.addEventListener("click", (e) => {
      const btn = e.target.closest(".quick-prize-btn");
      if (btn) {
        this.fillPrizeForm(btn.dataset);
      }
    });

    // Settings
    document.getElementById("wheelSettingsForm")?.addEventListener("submit", (e) => this.saveSettings(e));
    document.getElementById("previewWheelBtn")?.addEventListener("click", () => this.openPreviewModal());

    // Logo handling
    document.getElementById("logoUrl")?.addEventListener("input", (e) => this.handleLogoUrlChange(e));
    document.getElementById("logoFile")?.addEventListener("change", (e) => this.handleLogoFileUpload(e));

    const enableLogoBtn = document.getElementById("enableLogo");
    if (enableLogoBtn) {
        const logoToggleHandler = () => {
            const isEnabled = enableLogoBtn.dataset.enabled === 'true';
            const newState = !isEnabled;
            enableLogoBtn.dataset.enabled = newState.toString();
            const section = document.getElementById("logoUploadSection");
            const card = enableLogoBtn.closest('.border.rounded-xl');

            if (newState) {
                enableLogoBtn.classList.remove('bg-gray-200');
                enableLogoBtn.classList.add('bg-indigo-600');
                enableLogoBtn.querySelector('span').classList.remove('translate-x-1');
                enableLogoBtn.querySelector('span').classList.add('translate-x-6');
                if (section) section.classList.remove("hidden");
                if (card) { card.classList.add('border-indigo-200', 'bg-indigo-50/30'); card.classList.remove('border-gray-200', 'bg-white'); }
            } else {
                enableLogoBtn.classList.remove('bg-indigo-600');
                enableLogoBtn.classList.add('bg-gray-200');
                enableLogoBtn.querySelector('span').classList.remove('translate-x-6');
                enableLogoBtn.querySelector('span').classList.add('translate-x-1');
                if (section) section.classList.add("hidden");
                if (card) { card.classList.remove('border-indigo-200', 'bg-indigo-50/30'); card.classList.add('border-gray-200', 'bg-white'); }
            }
        };
        enableLogoBtn.addEventListener('click', logoToggleHandler);
        document.getElementById("logoToggleHeader")?.addEventListener('click', (e) => {
            if (e.target.closest('#enableLogo')) return;
            logoToggleHandler();
        });
    }

    // Logo size slider
    const logoSizeSlider = document.getElementById("logoSizePercentage");
    if (logoSizeSlider) {
        logoSizeSlider.addEventListener("input", () => {
            const val = document.getElementById("logoSizeValue");
            if (val) val.textContent = `%${logoSizeSlider.value}`;
            if (window.updateLivePreview) window.updateLivePreview();
        });
    }

    // Background Image handling
    document.getElementById("backgroundImageUrl")?.addEventListener("input", (e) => this.handleBgImageUrlChange(e));
    document.getElementById("bgImageFile")?.addEventListener("change", (e) => this.handleBgImageFileUpload(e));

    // Background Image enable toggle
    const enableBgImageBtn = document.getElementById("enableBgImage");
    if (enableBgImageBtn) {
        const bgToggleHandler = () => {
            const isEnabled = enableBgImageBtn.dataset.enabled === 'true';
            const newState = !isEnabled;
            enableBgImageBtn.dataset.enabled = newState.toString();
            const section = document.getElementById("bgImageUploadSection");
            const card = enableBgImageBtn.closest('.border.rounded-xl');

            if (newState) {
                enableBgImageBtn.classList.remove('bg-gray-200');
                enableBgImageBtn.classList.add('bg-purple-600');
                enableBgImageBtn.querySelector('span').classList.remove('translate-x-1');
                enableBgImageBtn.querySelector('span').classList.add('translate-x-6');
                if (section) section.classList.remove("hidden");
                if (card) { card.classList.add('border-purple-200', 'bg-purple-50/30'); card.classList.remove('border-gray-200', 'bg-white'); }
            } else {
                enableBgImageBtn.classList.remove('bg-purple-600');
                enableBgImageBtn.classList.add('bg-gray-200');
                enableBgImageBtn.querySelector('span').classList.remove('translate-x-6');
                enableBgImageBtn.querySelector('span').classList.add('translate-x-1');
                if (section) section.classList.add("hidden");
                if (card) { card.classList.remove('border-purple-200', 'bg-purple-50/30'); card.classList.add('border-gray-200', 'bg-white'); }
            }
        };
        enableBgImageBtn.addEventListener('click', bgToggleHandler);
        document.getElementById("bgImageToggleHeader")?.addEventListener('click', (e) => {
            if (e.target.closest('#enableBgImage')) return;
            bgToggleHandler();
        });
    }

    // Prize template buttons (setup after DOM is ready)
    setTimeout(() => {
      document.querySelectorAll(".prize-template-btn").forEach((btn) => {
        btn.addEventListener("click", function () {
          try {
            const template = JSON.parse(this.dataset.template);
            window.addPrizeFromTemplate(template);
          } catch (error) {
            console.error("Error parsing template data:", error);
          }
        });
      });
    }, 500);

    // Statistics - Sync Orders Button
    document.getElementById("syncOrdersBtn")?.addEventListener("click", () => this.syncOrders());

    // Date range filter
    const statsDateRange = document.getElementById("statsDateRange");
    if (statsDateRange) {
      const savedDateRange = localStorage.getItem("giftWheelDateRange");
      const savedStartDate = localStorage.getItem("giftWheelStartDate");
      const savedEndDate = localStorage.getItem("giftWheelEndDate");

      if (savedDateRange) {
        statsDateRange.value = savedDateRange;

        if (savedDateRange === "custom") {
          const customDateRange = document.getElementById("customDateRange");
          if (customDateRange) customDateRange.classList.remove("hidden");

          if (savedStartDate) {
            const startDateInput = document.getElementById("startDate");
            if (startDateInput) startDateInput.value = savedStartDate;
          }

          if (savedEndDate) {
            const endDateInput = document.getElementById("endDate");
            if (endDateInput) endDateInput.value = savedEndDate;
          }
        }
      }

      statsDateRange.addEventListener("change", (e) => {
        const value = e.target.value;
        const customDateRange = document.getElementById("customDateRange");

        localStorage.setItem("giftWheelDateRange", value);

        if (value === "custom") {
          if (customDateRange) customDateRange.classList.remove("hidden");
        } else {
          if (customDateRange) customDateRange.classList.add("hidden");
          localStorage.removeItem("giftWheelStartDate");
          localStorage.removeItem("giftWheelEndDate");
          this.loadStatistics();
        }
      });
    }

    // Custom date range apply button
    document.getElementById("applyCustomDateBtn")?.addEventListener("click", () => {
      const startDate = document.getElementById("startDate")?.value;
      const endDate = document.getElementById("endDate")?.value;

      if (!startDate || !endDate) {
        alert("Lütfen başlangıç ve bitiş tarihlerini seçin.");
        return;
      }

      if (new Date(startDate) > new Date(endDate)) {
        alert("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        return;
      }

      localStorage.setItem("giftWheelStartDate", startDate);
      localStorage.setItem("giftWheelEndDate", endDate);
      this.loadStatistics();
    });

    // Script generation
    document.getElementById("refreshScriptBtn")?.addEventListener("click", () => this.loadEmbedScript());
    document.getElementById("fullPreviewBtn")?.addEventListener("click", () => this.openFullPreview());

    // Prize actions (setup after DOM is ready)
    setTimeout(() => {
      this.setupPrizeActions();
    }, 500);

    // Global error handler
    window.addEventListener("unhandledrejection", (e) => {
      console.error("Unhandled promise rejection:", e.reason);
      showNotification("Bir hata oluştu. Lütfen sayfayı yenileyin.", "error");
    });
  }

  setupTabNavigation() {
    const tabButtons = document.querySelectorAll(".tab-button");
    const tabContents = document.querySelectorAll(".tab-content");

    tabButtons.forEach((button) => {
      button.addEventListener("click", () => {
        const targetTab = button.dataset.tab;

        // Update button states
        tabButtons.forEach((btn) => {
          btn.classList.remove("active", "border-indigo-500", "text-indigo-600");
          btn.classList.add("border-transparent", "text-gray-500");
        });

        button.classList.add("active", "border-indigo-500", "text-indigo-600");
        button.classList.remove("border-transparent", "text-gray-500");

        // Update content visibility
        tabContents.forEach((content) => {
          content.classList.add("hidden");
          content.style.display = "none";
        });

        const targetContent = document.getElementById(`${targetTab}-tab`);
        if (targetContent) {
          targetContent.classList.remove("hidden");
          targetContent.style.display = "block";
          console.log("Tab content shown for:", targetTab, targetContent);
        } else {
          console.error("Tab content not found for:", targetTab);
        }

        // Load tab-specific data
        this.onTabChange(targetTab);
      });
    });

    // Show first tab by default
    if (tabButtons.length > 0) {
      const firstTab = tabButtons[0].dataset.tab;
      const firstContent = document.getElementById(`${firstTab}-tab`);
      if (firstContent) {
        // Hide all tabs first
        tabContents.forEach((content) => {
          content.classList.add("hidden");
          content.style.display = "none";
        });

        // Show first tab
        firstContent.classList.remove("hidden");
        firstContent.style.display = "block";
        console.log("First tab shown:", firstTab, firstContent);
      }
    }
  }

  onTabChange(tabName) {
    switch (tabName) {
      case "prizes":
        this.loadPrizes();
        break;
      case "statistics":
        // Ensure sync button event listener is attached when statistics tab is shown
        setTimeout(() => {
          const syncBtn = document.getElementById("syncOrdersBtn");
          if (syncBtn && !syncBtn.hasAttribute("data-listener-attached")) {
            syncBtn.addEventListener("click", () => this.syncOrders());
            syncBtn.setAttribute("data-listener-attached", "true");
          }
        }, 100);

        // Always call loadStatistics which handles KPIs, funnel, charts, and logs
        setTimeout(() => {
          this.loadStatistics();
        }, 100);
        break;
      case "script":
        this.loadEmbedScript();
        break;
    }
  }

  async loadInitialData() {
    try {
      this.showLoading(true);

      // Load basic wheel data if exists
      const response = await fetch("/GiftWheel/GetWheelConfig");
      if (response.ok) {
        const result = await response.json();
        if (result.success && result.data) {
          this.currentWheelId = result.data.wheelId;
        }
      }

      if (typeof this.loadStatistics === "function") {
        await this.loadStatistics();
      }
    } catch (error) {
      console.error("Error loading initial data:", error);
    } finally {
      this.showLoading(false);
    }
  }

  // Wheel Management
  async createWheel() {
    try {
      const response = await fetch("/GiftWheel/CreateOrUpdateWheel", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: this.getAntiForgeryToken(),
        },
        body: JSON.stringify({
          name: "Hediye Çarkı",
        }),
      });

      const result = await response.json();

      if (result.success) {
        showNotification(result.message, "success");
        setTimeout(() => {
          window.location.reload();
        }, 1500);
      } else {
        showNotification(result.message, "error");
      }
    } catch (error) {
      console.error("Error creating wheel:", error);
      showNotification("Çark oluşturulurken hata oluştu.", "error");
    }
  }

  async toggleWheelStatus(event) {
    const button = event.target.closest("button");
    const isActive = button.dataset.active === "true";
    const newStatus = !isActive;

    try {
      const response = await fetch("/GiftWheel/ToggleWheelStatus", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: this.getAntiForgeryToken(),
        },
        body: JSON.stringify({
          isActive: newStatus,
        }),
      });

      const result = await response.json();

      if (result.success) {
        // Update UI
        button.dataset.active = newStatus.toString();
        const statusDot = button.querySelector("span:first-child");
        const statusLabel = button.querySelector("span:last-child");

        if (newStatus) {
          button.classList.remove("bg-gray-50", "text-gray-600", "border-gray-200");
          button.classList.add("bg-green-50", "text-green-700", "border-green-200");
          statusDot.classList.remove("bg-gray-400");
          statusDot.classList.add("bg-green-500", "animate-pulse");
          statusLabel.textContent = "Aktif";
        } else {
          button.classList.remove("bg-green-50", "text-green-700", "border-green-200");
          button.classList.add("bg-gray-50", "text-gray-600", "border-gray-200");
          statusDot.classList.remove("bg-green-500", "animate-pulse");
          statusDot.classList.add("bg-gray-400");
          statusLabel.textContent = "Pasif";
        }

        showNotification(result.message, "success");
      } else {
        showNotification(result.message, "error");
      }
    } catch (error) {
      console.error("Error toggling wheel status:", error);
      showNotification("Durum değiştirilirken hata oluştu.", "error");
    }
  }

  // Export to R2 Storage
  async exportToR2() {
    const btn = document.getElementById("exportToR2Btn");
    const originalContent = btn.innerHTML;

    // Show loading state immediately to prevent double clicks
    btn.disabled = true;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>İşleniyor...';

    try {
      // First, try to save settings if form exists
      const form = document.getElementById("wheelSettingsForm");
      if (form) {
        const saved = await this.saveSettingsInternal(form, true);
        if (!saved) {
          return; // Stop here if save failed
        }
      }

      // Update button text for the next phase
      btn.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>R2\'ye Aktarılıyor...';

      const response = await fetch("/GiftWheel/ExportToR2Ajax", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: this.getAntiForgeryToken(),
        },
      });

      const result = await response.json();

      if (result.success) {
        showNotification(result.message || "Hediye çarkı başarıyla R2'ye aktarıldı.", "success");
      } else {
        showNotification(result.message || "R2 aktarımı başarısız oldu.", "error");
      }
    } catch (error) {
      console.error("Error exporting to R2:", error);
      showNotification("R2 aktarımı sırasında hata oluştu.", "error");
    } finally {
      // Restore button state
      btn.disabled = false;
      btn.innerHTML = originalContent;
    }
  }

  // Data Export Handler
  async exportData(type) {
    const btnId = `export${type}Btn`;
    const btn = document.getElementById(btnId);
    if (!btn) return;

    const originalContent = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = `<i class="fas fa-spinner fa-spin mr-2"></i>İşleniyor...`;

    try {
      // Get current filters
      const dateParams = this.getDateRangeParams();
      const filter = {
        ...this.logsFilter,
        page: 1,
        pageSize: 2147483647 // Max int
      };

      if (dateParams && !filter.startDate && !filter.endDate) {
        filter.startDate = dateParams.startDate;
        filter.endDate = dateParams.endDate;
      }

      // Determine endpoint
      const endpoint = `/GiftWheel/Export${type}`;

      const response = await fetch(endpoint, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: this.getAntiForgeryToken()
        },
        body: JSON.stringify(filter)
      });

      if (response.ok) {
        
        // Handle PDF separately (Client-side generation)
        if (type === 'Pdf') {
            const result = await response.json();
             if (result.success && result.data) {
                const opt = {
                    margin: 10,
                    filename: `HediyeCarki_Rapor_${new Date().toISOString().slice(0,10)}.pdf`,
                    image: { type: 'jpeg', quality: 0.98 },
                    html2canvas: { scale: 2 },
                    jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait' }
                };

                // Create a temporary container
                const container = document.createElement('div');
                container.innerHTML = result.data;
                document.body.appendChild(container);

                try {
                    await html2pdf().set(opt).from(container).save();
                    showNotification("PDF raporu başarıyla oluşturuldu.", "success");
                } catch (e) {
                    console.error("PDF generation error:", e);
                    showNotification("PDF oluşturulurken hata oluştu.", "error");
                } finally {
                    document.body.removeChild(container);
                }
             } else {
                 showNotification("Rapor verisi alınamadı.", "error");
             }
        }
        else {
            // Excel and CSV (Direct Download)
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;

            // Try to get filename from content-disposition
            const contentDisposition = response.headers.get('Content-Disposition');
            let filename = `HediyeCarki_Rapor.${type.toLowerCase() === 'excel' ? 'xlsx' : 'csv'}`;

            if (contentDisposition) {
                const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
                if (filenameMatch && filenameMatch[1]) {
                    filename = filenameMatch[1].replace(/['"]/g, '');
                }
            }

            a.download = filename;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            a.remove();

            showNotification(`${type} raporu başarıyla indirildi.`, "success");
        }

      } else {
        const errorText = await response.text();
        console.error("Export failed:", errorText);
        showNotification("Rapor oluşturulurken bir hata oluştu.", "error");
      }

    } catch (error) {
      console.error(`Error exporting ${type}:`, error);
      showNotification("İşlem sırasında bir hata oluştu.", "error");
    } finally {
      btn.disabled = false;
      btn.innerHTML = originalContent;
    }
  }

  // Prize Management
  async loadPrizes() {
    try {
      const response = await fetch("/GiftWheel/GetPrizes");
      const result = await response.json();

      if (result.success) {
        this.currentPrizes = result.data;
        this.updatePrizesList();
      }
    } catch (error) {
      console.error("Error loading prizes:", error);
    }
  }

  updatePrizesList() {
    // This would update the prizes list in the UI
    // Implementation depends on the specific UI structure
    console.log("Updating prizes list:", this.currentPrizes);
  }

  // Prize Form Submission
  async submitPrizeForm(formData) {
    let loadingTarget = null;
    if (this.prizeDialog && this.prizeDialog.panelElement) {
        loadingTarget = this.prizeDialog.panelElement;
    }

    if (loadingTarget) {
        BusyIndicator.show(loadingTarget, { variant: 'overlay', text: 'Kaydediliyor...' });
    } else {
        this.showLoading(true);
    }

    try {

      // Get prize ID to determine if this is add or update
      const prizeId = formData.get("prizeId");
      const isUpdate = prizeId && prizeId !== "";

      // Prepare request data based on prize type
      const prizeType = formData.get("prizeType");
      let discountAmount = null;
      let discountType = 1;
      let validityDays = 30;
      let voucherMode = "duration";
      let voucherCodeFormat = "full_auto";
      let voucherPrefix = null;
      let allowDiscountedProducts = true;
      let combineWithOtherCampaigns = false;
      let minimumOrderAmount = null;

      if (prizeType === "voucher") {
        discountAmount = formData.get("discountAmount") ? parseFloat(formData.get("discountAmount")) : null;
        discountType = formData.get("discountType") ? parseInt(formData.get("discountType")) : 1;

        voucherMode = formData.get("voucherMode") || "duration";
        if (voucherMode === "usage") {
            validityDays = 365;
        } else {
            validityDays = formData.get("validityDays") ? parseInt(formData.get("validityDays")) : 30;
        }

        voucherCodeFormat = formData.get("voucherCodeFormat") || "full_auto";
        voucherPrefix = formData.get("voucherPrefix")?.trim() || null;

        if (voucherMode === "usage" && voucherCodeFormat === "fixed_prefix") {
            showNotification("Kullanım bazlı modda sabit ön ek kullanılamaz.", "error");
            return;
        }

        allowDiscountedProducts = formData.has("allowDiscountedProducts");
        combineWithOtherCampaigns = formData.has("combineWithOtherCampaigns");
        minimumOrderAmount = formData.get("minimumOrderAmount") ? parseFloat(formData.get("minimumOrderAmount")) : null;

      } else if (prizeType === "cargo") {
        discountAmount = formData.get("cargoDiscountAmount") ? parseFloat(formData.get("cargoDiscountAmount")) : 100;
        discountType = 2; // Cargo is always percentage

        voucherMode = formData.get("cargoVoucherMode") || "duration";
        if (voucherMode === "usage") {
            validityDays = 365;
        } else {
            validityDays = formData.get("cargoValidityDays") ? parseInt(formData.get("cargoValidityDays")) : 30;
        }

        voucherCodeFormat = formData.get("cargoVoucherCodeFormat") || "full_auto";
        voucherPrefix = formData.get("cargoVoucherPrefix")?.trim() || null;

        if (voucherMode === "usage" && voucherCodeFormat === "fixed_prefix") {
            showNotification("Kullanım bazlı modda sabit ön ek kullanılamaz.", "error");
            return;
        }

        allowDiscountedProducts = formData.has("cargoAllowDiscountedProducts");
        combineWithOtherCampaigns = formData.has("cargoCombineWithOtherCampaigns");
        minimumOrderAmount = formData.get("cargoMinimumOrderAmount") ? parseFloat(formData.get("cargoMinimumOrderAmount")) : null;
      }

      const requestData = {
        name: formData.get("prizeName"),
        prizeType: prizeType,
        discountAmount: discountAmount,
        discountType: discountType,
        validityDays: validityDays,
        voucherMode: voucherMode,
        voucherCodeFormat: voucherCodeFormat,
        voucherPrefix: voucherPrefix,
        allowDiscountedProducts: allowDiscountedProducts,
        combineWithOtherCampaigns: combineWithOtherCampaigns,
        minimumOrderAmount: minimumOrderAmount,
        probability: parseInt(formData.get("probability")),
        color: formData.get("prizeColor"),
        textColor: formData.get("textColor"),
        sortOrder: formData.get("sortOrder") ? parseInt(formData.get("sortOrder")) : 1,
        isActive: formData.has("isActive"),
      };

      // Make API call
      const url = isUpdate ? `/GiftWheel/UpdatePrize/${prizeId}` : "/GiftWheel/AddPrize";
      const method = isUpdate ? "PUT" : "POST";

      const response = await fetch(url, {
        method: method,
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: this.getAntiForgeryToken(),
        },
        body: JSON.stringify(requestData),
      });

      const result = await response.json();

      if (result.success) {
        showNotification(result.message, "success");

        // Close modal
        this.closePrizeModal();

        // Update prizes list without page reload
        await this.refreshPrizesUI();

        // Update statistics and probability display
        this.updateProbabilityDisplay();
      } else {
        showNotification(result.message, "error");
      }
    } catch (error) {
      console.error("Error submitting prize form:", error);
      showNotification("Ödül kaydedilirken hata oluştu.", "error");
    } finally {
      if (loadingTarget) {
          BusyIndicator.hide(loadingTarget);
      } else {
          this.showLoading(false);
      }
    }
  }

  async refreshPrizesUI() {
    try {
      // Get updated prizes data
      const response = await fetch("/GiftWheel/GetPrizes", {
        method: "GET",
        headers: {
          RequestVerificationToken: this.getAntiForgeryToken(),
        },
      });

      const result = await response.json();

      if (result.success && result.data) {
        // Update prizes list in DOM
        this.updatePrizesListDOM(result.data);

        // Update total probability display
        this.updateProbabilityDisplay();

        // Update statistics if available
        if (window.loadStatistics) {
          window.loadStatistics();
        }
      }
    } catch (error) {
      console.error("Error refreshing prizes UI:", error);
    }
  }

  updatePrizesListDOM(prizes) {
    const prizesList = document.getElementById("prizesList");
    if (!prizesList) return;

    // Clear existing prizes
    prizesList.innerHTML = "";

    if (prizes.length === 0) {
      // Show empty state
      prizesList.innerHTML = `
                <div class="text-center py-12 bg-gray-50 rounded-lg border-2 border-dashed border-gray-300">
                    <i class="fas fa-gift text-4xl text-gray-400 mb-4"></i>
                    <h3 class="text-lg font-medium text-gray-900 mb-2">Henüz ödül eklenmemiş</h3>
                    <p class="text-gray-600 mb-4">Çarkınıza ödül ekleyerek başlayın</p>
                    <button onclick="window.giftWheelAdmin.openPrizeModal()"
                            class="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg font-medium transition-colors">
                        <i class="fas fa-plus mr-2"></i>
                        İlk Ödülü Ekle
                    </button>
                </div>
            `;
      return;
    }

    // Sort prizes by sort order
    const sortedPrizes = prizes.sort((a, b) => a.sortOrder - b.sortOrder);

    // Generate HTML for each prize
    sortedPrizes.forEach((prize) => {
      const prizeHTML = this.generatePrizeHTML(prize);
      prizesList.insertAdjacentHTML("beforeend", prizeHTML);
    });

    // Reinitialize event listeners for new elements
    this.initializePrizeEventListeners();
  }

  generatePrizeHTML(prize) {
    let discountVal = "-";
    let discountBadge = '<span class="text-gray-300 font-light">-</span>';
    let messageVal = "";
    let infoVal = '<span class="text-amber-500"><i class="fas fa-redo mr-1"></i>Tekrar Dene</span>';

    if (prize.prizeType === "voucher" && prize.discountAmount !== null) {
      discountVal = prize.discountType === 1 ? `${prize.discountAmount} TL` : `%${prize.discountAmount}`;
      discountBadge = `<span class="inline-block bg-yellow-50 text-yellow-700 px-2 py-1 rounded-md border border-yellow-100 text-sm font-medium">${discountVal}</span>`;
      infoVal = `<span class="flex items-center justify-end"><i class="far fa-clock mr-1"></i> ${prize.validityDays} gün</span>`;
    } else if (prize.prizeType === "cargo" && prize.discountAmount !== null) {
      discountVal = prize.discountAmount >= 100 ? "Ücretsiz Kargo" : `%${prize.discountAmount} Kargo`;
      discountBadge = `<span class="inline-block bg-blue-50 text-blue-700 px-2 py-1 rounded-md border border-blue-100 text-sm font-medium"><i class="fas fa-truck mr-1"></i>${discountVal}</span>`;
      infoVal = `<span class="flex items-center justify-end"><i class="far fa-clock mr-1"></i> ${prize.validityDays} gün</span>`;
    }

    if ((prize.prizeType === "voucher" || prize.prizeType === "cargo") && prize.minimumOrderAmount && prize.minimumOrderAmount > 0) {
      infoVal += `<span class="flex items-center justify-end text-[11px] text-gray-500"><i class="fas fa-shopping-cart mr-1"></i> Min Sepet: ${prize.minimumOrderAmount} TL</span>`;
    }

    return `
                <div class="prize-item group relative flex items-center p-4 bg-white rounded-xl border border-gray-100 shadow-sm hover:shadow-md transition-all duration-200 overflow-hidden" data-prize-id="${
                  prize.id
                }">
                    <!-- Color Strip/Glow -->
                    <div class="absolute inset-y-0 left-0 w-1.5 transition-all group-hover:w-2" style="background-color: ${
                      prize.color
                    }; box-shadow: 0 0 10px ${prize.color}66;"></div>

                    <!-- Drag -->
                    <div class="drag-handle cursor-move text-gray-300 group-hover:text-indigo-500 transition-colors pl-4 pr-4">
                        <i class="fas fa-grip-vertical"></i>
                    </div>

                    <!-- Content Grid -->
                    <div class="flex-1 grid grid-cols-12 gap-4 items-center">
                        <!-- Name -->
                        <div class="col-span-4 font-semibold text-gray-800 text-lg tracking-tight">
                             ${prize.name}
                        </div>

                        <!-- Probability Badge -->
                        <div class="col-span-2 flex justify-center">
                             <span class="px-3 py-1 rounded-full text-sm font-bold bg-indigo-50 text-indigo-700 border border-indigo-100 flex items-center shadow-sm" data-probability="${
                               prize.probability
                             }">
                                <i class="fas fa-percentage text-xs mr-1 opacity-50"></i>
                                ${prize.probability}
                             </span>
                        </div>

                        <!-- Color Preview -->
                        <div class="col-span-1 flex justify-center items-center">
                             <div class="flex flex-col items-center gap-1">
                                 <div class="w-8 h-8 rounded-full border-2 border-white shadow-sm ring-1 ring-gray-100 transition-transform group-hover:scale-110 relative flex items-center justify-center"
                                      style="background-color: ${prize.color};" title="Görünüm Önizleme">
                                      <span class="text-xs font-bold select-none" style="color: ${
                                        prize.textColor || "#ffffff"
                                      }">Aa</span>
                                 </div>
                             </div>
                        </div>

                        <!-- Discount -->
                        <div class="col-span-2 text-center">
                             ${discountBadge}
                        </div>

                        <!-- Info -->
                        <div class="col-span-3 text-xs text-gray-500 text-right truncate font-medium">
                            ${infoVal}
                        </div>
                    </div>

                    <!-- Actions -->
                    <div class="flex items-center gap-1 pl-4 border-l border-gray-100 ml-4">
                          <button class="edit-prize-btn w-8 h-8 flex items-center justify-center rounded-lg text-gray-400 hover:text-indigo-600 hover:bg-indigo-50 transition-all tooltip-trigger"
                                  onclick="window.giftWheelAdmin.openPrizeModal(${prize.id})" data-prize-id="${
      prize.id
    }" title="Düzenle">
                                 <i class="fas fa-edit"></i>
                          </button>
                          <button class="delete-prize-btn w-8 h-8 flex items-center justify-center rounded-lg text-gray-400 hover:text-red-600 hover:bg-red-50 transition-all"
                                  onclick="window.giftWheelAdmin.deletePrize(${prize.id})" data-prize-id="${
      prize.id
    }" title="Sil">
                                 <i class="fas fa-trash-alt"></i>
                          </button>
                    </div>
                </div>
        `;
  }

  initializePrizeEventListeners() {
    // Reinitialize drag and drop if needed
    if (window.initializeDragAndDrop) {
      window.initializeDragAndDrop();
    }

    // Update probability display
    this.updateProbabilityDisplay();
  }

  updateProbabilityDisplay() {
    // Calculate probability from all prize items
    const prizeItems = document.querySelectorAll(".prize-item");
    let adminTotalProbability = 0;
    let activePrizeCount = 0;
    let totalPrizeCount = prizeItems.length;

    // Data for visualizing the bar
    const prizeData = [];

    prizeItems.forEach((prizeItem) => {
      // Check if this prize is active by looking at the toggle button
      const toggleButton = prizeItem.querySelector(".toggle-prize-status");
      // If toggle exists, use its state. If not, assume active (as in our new design).
      const isActive = toggleButton ? toggleButton.dataset.active === "true" : true;

      if (isActive) {
        let probability = 0;
        // Find probability from data-probability attribute
        const probabilityElement = prizeItem.querySelector("[data-probability]");
        if (probabilityElement) {
          probability = parseInt(probabilityElement.dataset.probability) || 0;
        } else {
          // Fallback: try to find probability from text content
          const probabilityText = prizeItem.textContent;
          const probabilityMatch = probabilityText.match(/%(\d+)/);
          if (probabilityMatch) {
            probability = parseInt(probabilityMatch[1]);
          }
        }

        adminTotalProbability += probability;
        activePrizeCount++;

        // Collect data for bar/legend
        // Name
        let name = "Hediye";
        const nameEl = prizeItem.querySelector(".font-semibold.text-gray-800");
        if (nameEl) name = nameEl.childNodes[nameEl.childNodes.length - 1].textContent.trim(); // Take last text node to avoid drag handle icon text

        // Color
        let color = "#cccccc";
        const colorEl = prizeItem.querySelector('div[title="Görünüm Önizleme"]');
        if (colorEl) color = colorEl.style.backgroundColor;

        // Text Color
        let textColor = "#ffffff";
        const textColorEl = prizeItem.querySelector('div[title="Görünüm Önizleme"] span');
        if (textColorEl) textColor = textColorEl.style.color;

        prizeData.push({ name, probability, color, textColor });
      }
    });

    // Update total probability displays
    const totalProbabilityElements = document.querySelectorAll("#totalProbability, #totalProbabilityDisplay");
    totalProbabilityElements.forEach((element) => {
      if (element) {
        if (element.id === "totalProbabilityDisplay") {
          element.textContent = adminTotalProbability + "%";
        } else {
          element.textContent = adminTotalProbability;
        }

        // Update text color
        element.classList.remove("text-green-600", "text-yellow-600", "text-red-600");
        if (adminTotalProbability === 100) element.classList.add("text-green-600");
        else if (adminTotalProbability < 100) element.classList.add("text-yellow-600");
        else element.classList.add("text-red-600");
      }
    });

    // Update Total Probability Bar
    const barContainer = document.getElementById("totalProbabilityBar");
    if (barContainer) {
      barContainer.innerHTML = "";
      prizeData.forEach((prize) => {
        if (prize.probability > 0) {
          const segment = document.createElement("div");
          segment.className =
            "h-full relative group transition-all duration-500 hover:brightness-110 flex items-center justify-center overflow-hidden";
          segment.style.width = prize.probability + "%";
          segment.style.backgroundColor = prize.color;
          segment.title = `${prize.name}: ${prize.probability}%`;

          // Helper text for larger segments
          if (prize.probability > 5) {
            segment.innerHTML = `<span class="text-[10px] font-bold px-1 truncate drop-shadow-md select-none" style="color: ${prize.textColor}">${prize.probability}%</span>`;
          }
          barContainer.appendChild(segment);
        }
      });
    }

    // Update Legend
    const legendContainer = document.getElementById("probabilityLegend");
    if (legendContainer) {
      legendContainer.innerHTML = "";
      prizeData.forEach((prize) => {
        const item = document.createElement("div");
        item.className = "flex items-center";
        item.innerHTML = `
                    <span class="w-3 h-3 rounded-full mr-1.5 shadow-sm" style="background-color: ${prize.color}"></span>
                    <span class="font-medium">${prize.name}</span>
                    <span class="ml-1 text-gray-400">(${prize.probability}%)</span>
                 `;
        legendContainer.appendChild(item);
      });
    }

    // Update prize count displays
    const prizeCountElements = document.querySelectorAll("[data-prize-count]");
    prizeCountElements.forEach((element) => {
      element.textContent = totalPrizeCount;
    });

    const activePrizeCountElements = document.querySelectorAll("[data-active-prize-count]");
    activePrizeCountElements.forEach((element) => {
      element.textContent = activePrizeCount;
    });

    // Trigger Live Preview Update (if available)
    if (window.updateLivePreview) {
      window.updateLivePreview();
    }

    // Update probability info box styling (always visible)
    const warningDiv = document.getElementById("probabilityWarning");
    const probabilityIcon = document.getElementById("probabilityIcon");
    const probabilityTitle = document.getElementById("probabilityTitle");
    const probabilityText = document.getElementById("probabilityText");

    if (warningDiv) {
      // Sadece %100 olduğunda yeşil (optimal)
      const isPerfect = adminTotalProbability === 100;
      const isCritical = adminTotalProbability < 50;
      const isWarning = !isPerfect && !isCritical; // %50-99 arası sarı

      // Update container classes
      warningDiv.classList.remove(
        "bg-yellow-50",
        "border-yellow-200",
        "bg-green-50",
        "border-green-200",
        "bg-red-50",
        "border-red-200"
      );
      if (isCritical) {
        warningDiv.classList.add("bg-red-50", "border-red-200");
      } else if (isPerfect) {
        warningDiv.classList.add("bg-green-50", "border-green-200");
      } else {
        warningDiv.classList.add("bg-yellow-50", "border-yellow-200");
      }
      warningDiv.dataset.optimal = isPerfect.toString();
      warningDiv.dataset.critical = isCritical.toString();

      // Update icon
      if (probabilityIcon) {
        probabilityIcon.classList.remove(
          "fa-check-circle",
          "fa-exclamation-triangle",
          "fa-times-circle",
          "text-green-500",
          "text-yellow-500",
          "text-red-500"
        );
        if (isCritical) {
          probabilityIcon.classList.add("fa-times-circle", "text-red-500");
        } else if (isPerfect) {
          probabilityIcon.classList.add("fa-check-circle", "text-green-500");
        } else {
          probabilityIcon.classList.add("fa-exclamation-triangle", "text-yellow-500");
        }
      }

      // Update title
      if (probabilityTitle) {
        probabilityTitle.classList.remove("text-green-800", "text-yellow-800", "text-red-800");
        if (isCritical) {
          probabilityTitle.classList.add("text-red-800");
          probabilityTitle.textContent = "Kritik Olasılık Hatası";
        } else if (isPerfect) {
          probabilityTitle.classList.add("text-green-800");
          probabilityTitle.textContent = "Mükemmel!";
        } else {
          probabilityTitle.classList.add("text-yellow-800");
          probabilityTitle.textContent = "Olasılık Uyarısı";
        }
      }

      // Update text
      if (probabilityText) {
        probabilityText.classList.remove("text-green-700", "text-yellow-700", "text-red-700");
        if (isCritical) {
          probabilityText.classList.add("text-red-700");
          const diff = 100 - adminTotalProbability;
          probabilityText.innerHTML =
            'Toplam olasılık <span id="totalProbability" class="font-semibold">' +
            adminTotalProbability +
            "</span>%. Toplam olasılık %100 olmalıdır. <strong>%" +
            diff +
            " eksik!</strong>";
        } else if (isPerfect) {
          probabilityText.classList.add("text-green-700");
          probabilityText.innerHTML =
            'Toplam olasılık <span id="totalProbability" class="font-semibold">' +
            adminTotalProbability +
            "</span>%. Olasılık değeri tam olarak %100 - her şey hazır!";
        } else {
          probabilityText.classList.add("text-yellow-700");
          const diff = 100 - adminTotalProbability;
          probabilityText.innerHTML =
            'Toplam olasılık <span id="totalProbability" class="font-semibold">' +
            adminTotalProbability +
            "</span>%. Toplam olasılık %100 olmalıdır. <strong>%" +
            diff +
            " eksik!</strong>";
        }
      }
    }

    console.log("Probability updated:", {
      adminTotalProbability,
      activePrizeCount,
      totalPrizeCount,
    });
  }

  openPrizeModal(prizeId = null) {
    if (!this.prizeDialog) {
        this.prizeDialog = new PushDialog({
            size: '2xl',
            closeOnOverlayClick: false,
            stickyFooter: true,
            footer: `
            <button type="submit" form="prizeForm" class="inline-flex w-full justify-center rounded-lg bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500 sm:ml-3 sm:w-auto transition-all transform active:scale-95">
                <i class="fas fa-save mr-2 mt-0.5"></i> Kaydet
            </button>
            <button type="button" onclick="window.giftWheelAdmin.closePrizeModal()" class="mt-3 inline-flex w-full justify-center rounded-lg bg-white px-5 py-2.5 text-sm font-semibold text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 hover:bg-gray-50 sm:mt-0 sm:w-auto transition-all">
                İptal
            </button>
            `
        });
    }

    const template = document.getElementById('prizeFormTemplate');
    if (template) {
        this.prizeDialog.setContent(template.content.cloneNode(true));
    }

    if (prizeId) {
       this.prizeDialog.setTitle('<i class="fas fa-edit mr-2 text-yellow-500"></i>Ödül Düzenle');
    } else {
       this.prizeDialog.setTitle('<i class="fas fa-plus mr-2 text-green-500"></i>Ödül Ekle');
    }

    this.prizeDialog.open();

    if (window.initPrizeFormListeners) {
        window.initPrizeFormListeners();
    }

    if (prizeId) {
      this.loadPrizeForEdit(prizeId);
    } else {
      const prizeIdInput = document.getElementById("prizeId");
      if (prizeIdInput) {
        prizeIdInput.value = "";
      }
    }


  }

  closePrizeModal() {
      if (this.prizeDialog) {
          this.prizeDialog.close();
      }
  }

  fillPrizeForm(data) {
    document.getElementById("prizeName").value = data.name || "";
    document.getElementById("prizeType").value = data.type || "voucher";

    const colorInput = document.getElementById("prizeColor");
    const colorTextInput = document.getElementById("prizeColorText");
    if (colorInput && data.color) {
      colorInput.value = data.color;
      if (colorTextInput) colorTextInput.value = data.color;
    }

    const textColorInput = document.getElementById("textColor");
    const textColorTextInput = document.getElementById("textColorText");
    if (textColorInput && data.textcolor) {
        textColorInput.value = data.textcolor;
        if (textColorTextInput) textColorTextInput.value = data.textcolor;
    }

    // Trigger change on prizeType to show/hide sections
    const prizeTypeSelect = document.getElementById("prizeType");
    if (prizeTypeSelect) prizeTypeSelect.dispatchEvent(new Event("change"));

    if (data.type === "voucher") {
      if (data.amount) document.getElementById("discountAmount").value = data.amount;

      if (data.dtype) {
        // Find radio with value
        const radio = document.querySelector(`input[name="discountType"][value="${data.dtype}"]`);
        if (radio) {
          radio.checked = true;
          // Trigger change on discountType to update suffix
          document.querySelectorAll('input[name="discountType"]').forEach((r) => {
            r.dispatchEvent(new Event("change"));
          });
        }
      }

      // Voucher Mode
      if (data.vouchermode) {
        const modeRadio = document.querySelector(`input[name="voucherMode"][value="${data.vouchermode}"]`);
        if (modeRadio) {
          modeRadio.checked = true;
          modeRadio.dispatchEvent(new Event("change"));
        }
      }

      // Validity
      if (data.validity) {
        const valInput = document.getElementById("validityDays");
        if(valInput) valInput.value = data.validity;
      }

      // Format
      if (data.vouchercodeformat) {
        const formatRadio = document.querySelector(`input[name="voucherCodeFormat"][value="${data.vouchercodeformat}"]`);
        if (formatRadio) {
          formatRadio.checked = true;
          formatRadio.dispatchEvent(new Event("change"));
        }
      }

      // Prefix
      if (data.prefix) {
         const preInput = document.getElementById("voucherPrefix");
         if(preInput) preInput.value = data.prefix;
      }

      if (data.minorderamount) {
         const minOrderInput = document.getElementById("minimumOrderAmount");
         if (minOrderInput) minOrderInput.value = data.minorderamount;
      }

    } else if (data.type === "cargo") {
        if (data.amount) {
            const cargoInput = document.getElementById("cargoDiscountAmount");
            if(cargoInput) cargoInput.value = data.amount;
        }

        // Cargo Mode
        if (data.vouchermode) {
            const modeRadio = document.querySelector(`input[name="cargoVoucherMode"][value="${data.vouchermode}"]`);
            if (modeRadio) {
                modeRadio.checked = true;
                modeRadio.dispatchEvent(new Event("change"));
            }
        }

        // Cargo Validity
        if (data.validity) {
            const valInput = document.getElementById("cargoValidityDays");
            if(valInput) valInput.value = data.validity;
        }

        // Cargo Format
        if (data.vouchercodeformat) {
            const formatRadio = document.querySelector(`input[name="cargoVoucherCodeFormat"][value="${data.vouchercodeformat}"]`);
            if (formatRadio) {
                formatRadio.checked = true;
                formatRadio.dispatchEvent(new Event("change"));
            }
        }

         // Cargo Prefix
         if (data.prefix) {
            const preInput = document.getElementById("cargoVoucherPrefix");
            if(preInput) preInput.value = data.prefix;
         }

               if (data.minorderamount) {
                  const minOrderInput = document.getElementById("cargoMinimumOrderAmount");
                  if (minOrderInput) minOrderInput.value = data.minorderamount;
               }
    }

    // Update preview
    if (window.updateLivePreview) window.updateLivePreview();
  }

  async loadPrizeForEdit(prizeId) {
    let targetElement = null;
    try {
      if (this.prizeDialog && this.prizeDialog.panelElement) {
          targetElement = this.prizeDialog.panelElement;
          BusyIndicator.show(targetElement, {
              variant: 'overlay',
              text: 'Ödül bilgileri yükleniyor...',
              color: 'primary'
          });
      } else {
          this.showLoading(true);
      }

      // First try to find in currentPrizes
      let prize = this.currentPrizes.find((p) => p.id === prizeId);

      // If not found, fetch from API
      if (!prize) {
        const response = await fetch(`/GiftWheel/GetPrize/${prizeId}`, {
          method: "GET",
          headers: {
            RequestVerificationToken: this.getAntiForgeryToken(),
          },
        });

        if (response.ok) {
          const result = await response.json();
          if (result.success && result.data) {
            prize = result.data;
          }
        }
      }

      if (prize) {
        // Set form values
        document.getElementById("prizeId").value = prize.id;
        document.getElementById("prizeName").value = prize.name;
        document.getElementById("prizeType").value = prize.prizeType;
        document.getElementById("discountAmount").value = prize.discountAmount || "";

        // Handle discountType radio buttons
        const discountTypeValue = (prize.discountType || 1).toString();
        const discountTypeRadios = document.getElementsByName("discountType");
        if (discountTypeRadios.length > 0) {
          discountTypeRadios.forEach((radio) => {
            radio.checked = radio.value === discountTypeValue;
          });
        } else {
          // Fallback if it was a select
          const discountTypeSelect = document.getElementById("discountType");
          if (discountTypeSelect) {
            discountTypeSelect.value = prize.discountType || 1;
          }
        }

        document.getElementById("validityDays").value = prize.validityDays || 30;

        // Voucher settings
        const voucherModeRadios = document.getElementsByName("voucherMode");
        const voucherModeValue = prize.voucherMode || "duration";
        voucherModeRadios.forEach((radio) => {
          radio.checked = radio.value === voucherModeValue;
        });

        // Trigger change for visibility
        voucherModeRadios.forEach(r => {
             if(r.checked) r.dispatchEvent(new Event('change'));
        });

        const voucherCodeFormatRadios = document.getElementsByName("voucherCodeFormat");
        const voucherCodeFormatValue = prize.voucherCodeFormat || "full_auto";
        voucherCodeFormatRadios.forEach((radio) => {
          radio.checked = radio.value === voucherCodeFormatValue;
        });

        const voucherPrefixInput = document.getElementById("voucherPrefix");
        if (voucherPrefixInput) {
          voucherPrefixInput.value = prize.voucherPrefix || "";
        }

        const minimumOrderAmountInput = document.getElementById("minimumOrderAmount");
        if (minimumOrderAmountInput) {
          minimumOrderAmountInput.value = prize.minimumOrderAmount || "";
        }

        const allowDiscountedProductsCheckbox = document.getElementById("allowDiscountedProducts");
        if (allowDiscountedProductsCheckbox) {
          allowDiscountedProductsCheckbox.checked = prize.allowDiscountedProducts !== false;
        }

        const combineWithOtherCampaignsCheckbox = document.getElementById("combineWithOtherCampaigns");
        if (combineWithOtherCampaignsCheckbox) {
          combineWithOtherCampaignsCheckbox.checked = !!prize.combineWithOtherCampaigns;
        }

        // Show/hide prefix container and trigger change for listener consistency
        const voucherPrefixContainer = document.getElementById("voucherPrefixContainer");
        if (voucherPrefixContainer) {
          if (voucherCodeFormatValue === "auto_prefix" || voucherCodeFormatValue === "fixed_prefix") {
            voucherPrefixContainer.classList.remove("hidden");
          } else {
            voucherPrefixContainer.classList.add("hidden");
          }
        }

        document.getElementsByName("voucherCodeFormat").forEach(r => {
             if(r.checked) r.dispatchEvent(new Event('change'));
        });

        // Cargo settings (if cargo type)
        if (prize.prizeType === "cargo") {
          document.getElementById("cargoDiscountAmount").value = prize.discountAmount || 100;
          document.getElementById("cargoValidityDays").value = prize.validityDays || 30;

          const cargoVoucherModeRadios = document.getElementsByName("cargoVoucherMode");
          cargoVoucherModeRadios.forEach((radio) => {
            radio.checked = radio.value === voucherModeValue;
          });

          // Trigger change for visibility
          cargoVoucherModeRadios.forEach(r => {
             if(r.checked) r.dispatchEvent(new Event('change'));
          });

          const cargoVoucherCodeFormatRadios = document.getElementsByName("cargoVoucherCodeFormat");
          cargoVoucherCodeFormatRadios.forEach((radio) => {
            radio.checked = radio.value === voucherCodeFormatValue;
          });

          cargoVoucherCodeFormatRadios.forEach(r => {
             if(r.checked) r.dispatchEvent(new Event('change'));
          });

          const cargoVoucherPrefixInput = document.getElementById("cargoVoucherPrefix");
          if (cargoVoucherPrefixInput) {
            cargoVoucherPrefixInput.value = prize.voucherPrefix || "";
          }

          const cargoMinimumOrderAmountInput = document.getElementById("cargoMinimumOrderAmount");
          if (cargoMinimumOrderAmountInput) {
            cargoMinimumOrderAmountInput.value = prize.minimumOrderAmount || "";
          }

          const cargoAllowDiscountedProductsCheckbox = document.getElementById("cargoAllowDiscountedProducts");
          if (cargoAllowDiscountedProductsCheckbox) {
            cargoAllowDiscountedProductsCheckbox.checked = prize.allowDiscountedProducts !== false;
          }

          const cargoCombineWithOtherCampaignsCheckbox = document.getElementById("cargoCombineWithOtherCampaigns");
          if (cargoCombineWithOtherCampaignsCheckbox) {
            cargoCombineWithOtherCampaignsCheckbox.checked = !!prize.combineWithOtherCampaigns;
          }

          const cargoVoucherPrefixContainer = document.getElementById("cargoVoucherPrefixContainer");
          if (cargoVoucherPrefixContainer) {
            if (voucherCodeFormatValue === "auto_prefix" || voucherCodeFormatValue === "fixed_prefix") {
              cargoVoucherPrefixContainer.classList.remove("hidden");
            } else {
              cargoVoucherPrefixContainer.classList.add("hidden");
            }
          }
        }

        document.getElementById("probability").value = prize.probability;
        document.getElementById("probabilitySlider").value = prize.probability;
        document.getElementById("prizeColor").value = prize.color;
        document.getElementById("prizeColorText").value = prize.color;

        // Text Color
        const textColor = prize.textColor || "#ffffff";
        const textColorInput = document.getElementById("textColor");
        const textColorTextInput = document.getElementById("textColorText");

        if (textColorInput) textColorInput.value = textColor;
        if (textColorTextInput) textColorTextInput.value = textColor;

        document.getElementById("sortOrder").value = prize.sortOrder || 0;

        const isActiveCheckbox = document.getElementById("isActive");
        if (isActiveCheckbox) {
          isActiveCheckbox.checked = prize.isActive;
        }

        // Update color preview
        const colorPreview = document.getElementById("prizePreviewColor");
        if (colorPreview) {
          colorPreview.style.backgroundColor = prize.color;
        }

        // Trigger change events to update UI
        setTimeout(() => {
          document.getElementById("prizeType").dispatchEvent(new Event("change"));
          document.getElementById("probability").dispatchEvent(new Event("input"));
          document.getElementById("prizeName").dispatchEvent(new Event("input"));
          document.getElementById("prizeColor").dispatchEvent(new Event("input"));

          const textColorInput = document.getElementById("textColor");
          if (textColorInput) {
            textColorInput.dispatchEvent(new Event("change"));
            textColorInput.dispatchEvent(new Event("input"));
          }
        }, 100);
      } else {
        showNotification("Ödül bilgileri yüklenemedi.", "error");
      }
    } catch (error) {
      console.error("Error loading prize for edit:", error);
      showNotification("Ödül bilgileri yüklenirken hata oluştu.", "error");
    } finally {
      if (targetElement) {
          BusyIndicator.hide(targetElement);
      } else {
          this.showLoading(false);
      }
    }
  }

  // Settings Management
  async saveSettings(event) {
    event.preventDefault();
    await this.saveSettingsInternal(event.target);
  }

  handleLogoUrlChange(event) {
    const url = event.target.value;
    const preview = document.getElementById("logoPreview");
    const placeholder = document.getElementById("logoPlaceholder");

    if (url) {
      // Reset error handler first
      preview.onerror = null;
      preview.onload = null;

      preview.src = url;
      preview.classList.remove("hidden");
      placeholder.classList.add("hidden");

      // Add error handling
      preview.onerror = () => {
        console.warn("Failed to load logo preview:", url);
        preview.classList.add("hidden");
        placeholder.classList.remove("hidden");
        // Optional: Show a small toast or message
      };
    } else {
      preview.src = "";
      preview.classList.add("hidden");
      placeholder.classList.remove("hidden");
    }
  }

  async handleLogoFileUpload(event) {
    const file = event.target.files[0];
    if (!file) return;

    // Validation
    const validTypes = ["image/jpeg", "image/png", "image/svg+xml"];
    // Also check extension just in case MIME is weird
    const extension = file.name.split(".").pop().toLowerCase();
    const validExtensions = ["jpg", "jpeg", "png", "svg"];

    if (file.type && !validTypes.includes(file.type) && !validExtensions.includes(extension)) {
      showNotification("Sadece PNG, JPG, JPEG veya SVG formatları desteklenir.", "error");
      event.target.value = "";
      return;
    }

    if (file.size > 2 * 1024 * 1024) {
      showNotification("Dosya boyutu 2MB'dan küçük olmalıdır.", "error");
      event.target.value = "";
      return;
    }

    // Create FormData
    const formData = new FormData();
    formData.append("file", file);

    try {
      // Show uploading state
      const input = event.target;
      const parent = input.parentElement;
      const originalText = parent.querySelector("p")?.textContent;
      const pText = parent.querySelector("p");
      if (pText) pText.textContent = "Yükleniyor...";
      input.disabled = true;

      const response = await fetch("/GiftWheel/UploadLogo", {
        method: "POST",
        headers: {
          RequestVerificationToken: this.getAntiForgeryToken(),
        },
        body: formData,
      });

      const result = await response.json();

      if (result.success && result.url) {
        document.getElementById("logoUrl").value = result.url;
        // Trigger input event to update preview
        document.getElementById("logoUrl").dispatchEvent(new Event("input"));
        showNotification("Logo başarıyla yüklendi.", "success");
      } else {
        showNotification(result.message || "Logo yüklenirken hata oluştu.", "error");
      }
    } catch (error) {
      console.error("Upload error:", error);
      showNotification("Yükleme sırasında bir hata oluştu.", "error");
    } finally {
      // Reset input state
      const input = event.target;
      input.disabled = false;
      input.value = ""; // Allow re-uploading same file
      const parent = input.parentElement;
      const pText = parent.querySelector("p");
      if (pText) pText.textContent = "PNG, JPG, JPEG, SVG. Maks: 2MB.";
    }
  }

  handleBgImageUrlChange(event) {
    const url = event.target.value;
    const preview = document.getElementById("bgImagePreview");
    const placeholder = document.getElementById("bgImagePlaceholder");

    if (url) {
      preview.onerror = null;
      preview.onload = null;
      preview.src = url;
      preview.classList.remove("hidden");
      if (placeholder) placeholder.classList.add("hidden");

      preview.onerror = () => {
        preview.classList.add("hidden");
        if (placeholder) placeholder.classList.remove("hidden");
      };
    } else {
      preview.src = "";
      preview.classList.add("hidden");
      if (placeholder) placeholder.classList.remove("hidden");
    }

    if (window.updateLivePreview) window.updateLivePreview();
  }

  async handleBgImageFileUpload(event) {
    const file = event.target.files[0];
    if (!file) return;

    const validTypes = ["image/jpeg", "image/png", "image/webp"];
    const extension = file.name.split(".").pop().toLowerCase();
    const validExtensions = ["jpg", "jpeg", "png", "webp"];

    if (file.type && !validTypes.includes(file.type) && !validExtensions.includes(extension)) {
      showNotification("Sadece PNG, JPG, JPEG veya WEBP formatları desteklenir.", "error");
      event.target.value = "";
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      showNotification("Dosya boyutu 5MB'dan küçük olmalıdır.", "error");
      event.target.value = "";
      return;
    }

    const formData = new FormData();
    formData.append("file", file);

    try {
      const input = event.target;
      input.disabled = true;
      const pText = input.parentElement.querySelector("p");
      if (pText) pText.textContent = "Yükleniyor...";

      const response = await fetch("/GiftWheel/UploadLogo", {
        method: "POST",
        headers: {
          RequestVerificationToken: this.getAntiForgeryToken(),
        },
        body: formData,
      });

      const result = await response.json();

      if (result.success && result.url) {
        document.getElementById("backgroundImageUrl").value = result.url;
        document.getElementById("backgroundImageUrl").dispatchEvent(new Event("input"));
        showNotification("Arkaplan görseli başarıyla yüklendi.", "success");
      } else {
        showNotification(result.message || "Görsel yüklenirken hata oluştu.", "error");
      }
    } catch (error) {
      console.error("Upload error:", error);
      showNotification("Yükleme sırasında bir hata oluştu.", "error");
    } finally {
      const input = event.target;
      input.disabled = false;
      input.value = "";
      const pText = input.parentElement.querySelector("p");
      if (pText) pText.textContent = "PNG, JPG, JPEG, WEBP. Maks: 5MB. Önerilen: Dikey; 1080x1920. Yatay için 1920x1080.";
    }
  }

  async saveSettingsInternal(form, manageLoading = true) {
    try {
      if (manageLoading) this.showLoading(true);

      const formData = new FormData(form);

      // Handle wheel size logic
      const useDefaultSize = document.getElementById("useDefaultSize")?.checked;
      const wheelSizeVal = formData.get("wheelSize");
      const wheelSize = useDefaultSize ? 0 : (wheelSizeVal ? parseInt(wheelSizeVal) : 300);

      const settings = {
        wheelTitle: formData.get("wheelTitle"),
        wheelSubtitle: formData.get("wheelSubtitle"),
        buttonText: formData.get("buttonText"),
        winMessage: formData.get("winMessage"),
        loseMessage: formData.get("loseMessage"),
        maxSpinsPerDay: parseInt(formData.get("maxSpinsPerDay")),
        winFrequencyDays: parseInt(formData.get("winFrequencyDays")) || 0,
        requirePhone: formData.has("requirePhone"),
        requireEmail: formData.has("requireEmail"),
        notificationTemplate: formData.get("notificationTemplate"),
        primaryColor: formData.get("primaryColor"),
        secondaryColor: formData.get("secondaryColor"),
        wheelSize: wheelSize,
        layout: formData.get("layout"),
        logoUrl: formData.get("logoUrl"),
        logoAnimation: formData.has("logoAnimation"),
        logoSizePercentage: parseInt(formData.get("logoSizePercentage")) || 15, // fallback to 15 if 0 or NaN
        backgroundImageUrl: formData.get("backgroundImageUrl"),
        modalTextColor: formData.get("modalTextColor") || "#1f2937",
        privacyPolicyUrl: formData.get("privacyPolicyUrl"),
        termsUrl: formData.get("termsUrl"),
        showConfetti: document.getElementById("showConfettiToggle")?.dataset.enabled === "true",
        buttonPosition: formData.get("buttonPosition") || "right",
        buttonBottomPx: parseInt(formData.get("buttonBottomPx")) || 20,
        buttonMobileBottomPx: document.getElementById("useMobileCustomBottom")?.checked
          ? (parseInt(formData.get("buttonMobileBottomPx")) || 20)
          : (parseInt(formData.get("buttonBottomPx")) || 20),
        whatsappTemplateId: formData.get("WhatsappTemplateId"),
        whatsappTemplateName: formData.get("WhatsappTemplateName"),
      };

      const response = await fetch("/GiftWheel/UpdateSettings", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: this.getAntiForgeryToken(),
        },
        body: JSON.stringify(settings),
      });

      const result = await response.json();

      if (result.success) {
        if (manageLoading) showNotification(result.message, "success");
        return true;
      } else {
        showNotification(result.message, "error");
        return false;
      }
    } catch (error) {
      console.error("Error saving settings:", error);
      showNotification("Ayarlar kaydedilirken hata oluştu.", "error");
      return false;
    } finally {
      if (manageLoading) this.showLoading(false);
    }
  }

  // Statistics
  getDateRangeParams() {
    const days = document.getElementById("statsDateRange")?.value || 30;

    if (days === "custom") {
      const startDate = document.getElementById("startDate")?.value;
      const endDate = document.getElementById("endDate")?.value;

      if (startDate && endDate) {
        return {
          startDate: new Date(startDate).toISOString(),
          endDate: new Date(endDate + "T23:59:59").toISOString()
        };
      }
      return null;
    } else {
      const endDate = new Date();
      const startDate = new Date();

      if (days === "1") {
        startDate.setHours(startDate.getHours() - 24);
      } else {
        startDate.setDate(startDate.getDate() - parseInt(days));
        startDate.setHours(0, 0, 0, 0);
        endDate.setHours(23, 59, 59, 999);
      }

      return {
        startDate: startDate.toISOString(),
        endDate: endDate.toISOString()
      };
    }
  }

  async syncOrders() {
    const syncBtn = document.getElementById('syncOrdersBtn');
    const syncBtnText = document.getElementById('syncOrdersBtnText');
    const syncIcon = syncBtn?.querySelector('svg');

    if (!syncBtn || !syncBtnText) return;

    // Butonu devre dışı bırak
    syncBtn.disabled = true;
    syncBtnText.textContent = 'Senkronize ediliyor...';
    syncBtn.classList.add('opacity-50', 'cursor-not-allowed');

    // Icon'u döndür
    if (syncIcon) {
      syncIcon.classList.add('animate-spin');
    }

    // Bilgilendirme mesajı göster
    showNotification('Ticimax\'tan siparişler çekiliyor, lütfen bekleyin...', 'info');

    try {
      const dateParams = this.getDateRangeParams();
      let syncUrl = `/GiftWheel/SyncOrders`;

      if (dateParams) {
        syncUrl += `?startDate=${dateParams.startDate}&endDate=${dateParams.endDate}`;
      } else {
        const endDate = new Date();
        const startDate = new Date();
        startDate.setDate(startDate.getDate() - 30);
        startDate.setHours(0, 0, 0, 0);
        endDate.setHours(23, 59, 59, 999);
        syncUrl += `?startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`;
      }

      // 1. ADIM: Ticimax'tan siparişleri çek ve veritabanını güncelle
      syncBtnText.textContent = 'Siparişler çekiliyor...';
      const syncResponse = await fetch(syncUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        }
      });

      const syncResult = await syncResponse.json();

      if (!syncResult.success) {
        showNotification('Hata: ' + (syncResult.message || 'Siparişler senkronize edilemedi.'), 'error');
        return;
      }

      // 2. ADIM: Veritabanı güncellemesi tamamlandı
      const message = syncResult.updatedCount > 0
        ? `${syncResult.updatedCount} kupon kullanımı güncellendi.`
        : 'Siparişler senkronize edildi. Veritabanı güncellendi.';
      showNotification(message, 'success');

      // 3. ADIM: Veritabanı güncellendikten sonra istatistikleri yükle
      syncBtnText.textContent = 'İstatistikler güncelleniyor...';
      await Promise.all([
        this.loadDashboardKpis(),
        this.loadFunnelStats(),
        this.loadChartData()
      ]);

      const logsContainer = document.getElementById("detailedLogsTable");
      if (logsContainer && logsContainer.offsetParent !== null) {
        await this.loadDetailedLogs();
      }

      showNotification('İstatistikler başarıyla güncellendi.', 'success');
    } catch (error) {
      console.error("Error syncing orders:", error);
      showNotification('Senkronizasyon sırasında bir hata oluştu: ' + error.message, 'error');
    } finally {
      // Butonu tekrar aktif et
      syncBtn.disabled = false;
      syncBtnText.textContent = 'Senkronize Et';
      syncBtn.classList.remove('opacity-50', 'cursor-not-allowed');

      // Icon'u durdur
      if (syncIcon) {
        syncIcon.classList.remove('animate-spin');
      }
    }
  }

  async loadStatistics() {
    await Promise.all([
      this.loadDashboardKpis(),
      this.loadFunnelStats(),
      this.loadChartData()
    ]);

    const logsContainer = document.getElementById("detailedLogsTable");
    if (logsContainer && logsContainer.offsetParent !== null) {
      await this.loadDetailedLogs();
    }
  }

  // Chart instances cache
  dailyActivityChartInstance = null;
  prizeDistributionChartInstance = null;

  async loadChartData() {
    try {
      const dateParams = this.getDateRangeParams();
      let url = "/GiftWheel/GetStats";
      if (dateParams) {
        url += `?startDate=${dateParams.startDate}&endDate=${dateParams.endDate}`;
      }

      const response = await fetch(url);
      const result = await response.json();

      if (result.success && result.data) {
        this.renderDailyActivityChart(result.data.dailyStats || []);
        this.renderPrizeDistributionChart(result.data.prizeStats || []);
        this.renderPrizePerformanceTable(result.data.prizeStats || [], result.data.totalSpins || 0);
      }
    } catch (error) {
      console.error("Error loading chart data:", error);
    }
  }

  renderDailyActivityChart(dailyStats) {
    const canvas = document.getElementById("dailyActivityChart");
    if (!canvas) return;

    // Destroy existing chart
    if (this.dailyActivityChartInstance) {
      this.dailyActivityChartInstance.destroy();
      this.dailyActivityChartInstance = null;
    }

    // If no data, show empty state
    if (!dailyStats || dailyStats.length === 0) {
      const ctx = canvas.getContext("2d");
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.fillStyle = "#9ca3af";
      ctx.font = "14px Arial";
      ctx.textAlign = "center";
      ctx.fillText("Bu dönemde veri bulunmamaktadır.", canvas.width / 2, canvas.height / 2);
      return;
    }

    // Sort by date
    dailyStats.sort((a, b) => new Date(a.date) - new Date(b.date));

    const labels = dailyStats.map(d => {
      const date = new Date(d.date);
      return date.toLocaleDateString("tr-TR", { day: "2-digit", month: "short" });
    });

    const spinCounts = dailyStats.map(d => d.spinCount);
    const voucherCounts = dailyStats.map(d => d.voucherCount);

    this.dailyActivityChartInstance = new Chart(canvas, {
      type: "line",
      data: {
        labels: labels,
        datasets: [
          {
            label: "Çevirme",
            data: spinCounts,
            borderColor: "#6366f1",
            backgroundColor: "rgba(99, 102, 241, 0.1)",
            borderWidth: 2,
            fill: true,
            tension: 0.3,
            pointRadius: 3,
            pointBackgroundColor: "#6366f1"
          },
          {
            label: "Kupon",
            data: voucherCounts,
            borderColor: "#10b981",
            backgroundColor: "rgba(16, 185, 129, 0.1)",
            borderWidth: 2,
            fill: true,
            tension: 0.3,
            pointRadius: 3,
            pointBackgroundColor: "#10b981"
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: {
          mode: "index",
          intersect: false
        },
        plugins: {
          legend: {
            position: "top",
            labels: {
              usePointStyle: true,
              padding: 15,
              font: { size: 12 }
            }
          },
          tooltip: {
            backgroundColor: "rgba(0, 0, 0, 0.8)",
            padding: 12,
            titleFont: { size: 13 },
            bodyFont: { size: 12 },
            callbacks: {
              title: function(items) {
                return items[0].label;
              }
            }
          }
        },
        scales: {
          x: {
            grid: { display: false },
            ticks: {
              font: { size: 11 },
              maxRotation: 45,
              maxTicksLimit: 15
            }
          },
          y: {
            beginAtZero: true,
            grid: { color: "rgba(0, 0, 0, 0.05)" },
            ticks: {
              font: { size: 11 },
              precision: 0
            }
          }
        }
      }
    });
  }

  renderPrizeDistributionChart(prizeStats) {
    const canvas = document.getElementById("prizeDistributionChart");
    if (!canvas) return;

    // Destroy previous chart
    if (this.prizeDistributionChartInstance) {
      this.prizeDistributionChartInstance.destroy();
      this.prizeDistributionChartInstance = null;
    }

    // Filter out prizes with 0 wins
    const filteredStats = (prizeStats || []).filter(p => p.timesWon > 0);

    if (filteredStats.length === 0) {
      const ctx = canvas.getContext("2d");
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.fillStyle = "#9ca3af";
      ctx.font = "14px Arial";
      ctx.textAlign = "center";
      ctx.fillText("Bu dönemde kazanılan ödül bulunmamaktadır.", canvas.width / 2, canvas.height / 2);
      return;
    }

    // Match prize colors from server data
    const prizeColors = [
      "#6366f1", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6",
      "#ec4899", "#14b8a6", "#f97316", "#06b6d4", "#84cc16"
    ];

    // Try to get colors from server prizes
    const serverPrizes = window.serverPrizes || [];

    const labels = filteredStats.map(p => p.prizeName);
    const data = filteredStats.map(p => p.timesWon);
    const colors = filteredStats.map((p, i) => {
      const serverPrize = serverPrizes.find(sp => sp.id === p.prizeId);
      return serverPrize?.color || prizeColors[i % prizeColors.length];
    });

    this.prizeDistributionChartInstance = new Chart(canvas, {
      type: "doughnut",
      data: {
        labels: labels,
        datasets: [{
          data: data,
          backgroundColor: colors,
          borderColor: "#ffffff",
          borderWidth: 2,
          hoverOffset: 8
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: "55%",
        plugins: {
          legend: {
            position: "right",
            labels: {
              usePointStyle: true,
              padding: 12,
              font: { size: 12 },
              generateLabels: function(chart) {
                const dataset = chart.data.datasets[0];
                const total = dataset.data.reduce((a, b) => a + b, 0);
                return chart.data.labels.map((label, i) => ({
                  text: `${label} (${dataset.data[i]})`,
                  fillStyle: dataset.backgroundColor[i],
                  strokeStyle: dataset.borderColor,
                  lineWidth: 1,
                  pointStyle: "circle",
                  hidden: false,
                  index: i
                }));
              }
            }
          },
          tooltip: {
            backgroundColor: "rgba(0, 0, 0, 0.8)",
            padding: 12,
            callbacks: {
              label: function(context) {
                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                const percentage = total > 0 ? ((context.raw / total) * 100).toFixed(1) : 0;
                return `${context.label}: ${context.raw} (${percentage}%)`;
              }
            }
          }
        }
      }
    });
  }

  renderPrizePerformanceTable(prizeStats, totalSpins) {
    const table = document.getElementById("prizePerformanceTable");
    if (!table) return;

    const serverPrizes = window.serverPrizes || [];

    if (!prizeStats || prizeStats.length === 0) {
      table.innerHTML = `<tr><td colspan="5" class="px-6 py-16 text-center"><div class="flex flex-col items-center gap-2"><div class="w-10 h-10 rounded-full bg-amber-50 flex items-center justify-center"><i class="fas fa-trophy text-amber-300"></i></div><span class="text-sm text-gray-400">Bu dönemde veri bulunmamaktadır.</span></div></td></tr>`;
      return;
    }

    table.innerHTML = prizeStats.map(stat => {
      const prize = serverPrizes.find(p => p.id === stat.prizeId);
      if (!prize) return "";

      const performance = prize.probability > 0 ? stat.winRate / prize.probability : 0;
      const perfBg = performance > 1.2 ? "bg-red-50 text-red-700 ring-red-600/10" : performance < 0.8 ? "bg-amber-50 text-amber-700 ring-amber-600/10" : "bg-emerald-50 text-emerald-700 ring-emerald-600/10";
      const perfIcon = performance > 1.2 ? "fa-arrow-trend-up" : performance < 0.8 ? "fa-arrow-trend-down" : "fa-check";
      const barWidth = Math.min(stat.winRate, 100);

      return `
        <tr class="group hover:bg-gray-50/70 transition-colors duration-150">
          <td class="px-6 py-4">
            <div class="flex items-center gap-3">
              <div class="w-8 h-8 rounded-lg flex items-center justify-center shadow-sm ring-1 ring-black/5" style="background-color: ${prize.color}">
                <i class="fas fa-gift text-white text-xs"></i>
              </div>
              <span class="font-semibold text-sm text-gray-900">${prize.name}</span>
            </div>
          </td>
          <td class="px-6 py-4 text-center">
            <span class="text-sm font-bold text-gray-900">${stat.timesWon}</span>
            <span class="text-xs text-gray-400 ml-0.5">kez</span>
          </td>
          <td class="px-6 py-4">
            <div class="flex flex-col items-center gap-1.5">
              <span class="text-sm font-semibold text-gray-800">${stat.winRate.toFixed(1)}%</span>
              <div class="w-full max-w-[80px] h-1.5 bg-gray-100 rounded-full overflow-hidden">
                <div class="h-full rounded-full transition-all duration-500" style="width: ${barWidth}%; background-color: ${prize.color}"></div>
              </div>
            </div>
          </td>
          <td class="px-6 py-4 text-center">
            <span class="text-sm text-gray-500 font-medium">${prize.probability}%</span>
          </td>
          <td class="px-6 py-4 text-center">
            <span class="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-xs font-bold ring-1 ring-inset ${perfBg}">
              <i class="fas ${perfIcon} text-[10px]"></i>
              ${performance.toFixed(2)}x
            </span>
          </td>
        </tr>
      `;
    }).join("");
  }

  async loadDashboardKpis() {
    try {
      const dateParams = this.getDateRangeParams();
      let url = `/GiftWheel/GetDashboardKpis`;

      if (dateParams) {
        url += `?startDate=${dateParams.startDate}&endDate=${dateParams.endDate}`;
      }

      const response = await fetch(url);
      const result = await response.json();

      if (result.success && result.data) {
        this.updateKpiDisplay(result.data);
      }
    } catch (error) {
      console.error("Error loading dashboard KPIs:", error);
    }
  }

  updateKpiDisplay(kpis) {
    const formatCurrency = (value) => new Intl.NumberFormat("tr-TR", { style: "currency", currency: "TRY" }).format(value || 0);

    const setIfExists = (id, value) => {
      const el = document.getElementById(id);
      if (el) el.textContent = value;
    };

    setIfExists("kpiTotalSpins", kpis.totalSpins || 0);
    setIfExists("kpiSpinsToday", kpis.spinsToday || 0);
    setIfExists("kpiVouchersCreated", kpis.totalVouchersCreated || 0);
    setIfExists("kpiCreationRate", `${kpis.totalSpins > 0 ? ((kpis.totalVouchersCreated / kpis.totalSpins) * 100).toFixed(1) : 0}%`);
    setIfExists("kpiVouchersUsed", kpis.totalVouchersUsed || 0);
    setIfExists("kpiUsageRate", `${kpis.voucherUsageRate || 0}%`);
    setIfExists("kpiOrderAmountWithKDV", formatCurrency(kpis.orderAmountWithKDV || 0));
    setIfExists("kpiTotalDiscountAmount", formatCurrency(kpis.totalDiscountAmount || 0));
    setIfExists("kpiTotalCost", formatCurrency(kpis.totalCost || 0));
    setIfExists("kpiNotificationsThisWeek", kpis.notificationsSuccessfulThisWeek || 0);
  }

  async loadFunnelStats() {
    try {
      const dateParams = this.getDateRangeParams();
      let url = `/GiftWheel/GetFunnelStats`;

      if (dateParams) {
        url += `?startDate=${dateParams.startDate}&endDate=${dateParams.endDate}`;
      }

      const response = await fetch(url);
      const result = await response.json();

      if (result.success && result.data) {
        this.updateFunnelDisplay(result.data);
      }
    } catch (error) {
      console.error("Error loading funnel stats:", error);
    }
  }

  updateFunnelDisplay(funnel) {
    const formatCurrency = (value) => new Intl.NumberFormat("tr-TR", { style: "currency", currency: "TRY" }).format(value || 0);

    const setIfExists = (id, value) => {
      const el = document.getElementById(id);
      if (el) el.textContent = value;
    };

    setIfExists("funnelSpins", funnel.spinCount || 0);
    setIfExists("funnelVouchers", funnel.voucherCreatedCount || 0);
    setIfExists("funnelUsed", funnel.voucherUsedCount || 0);
    setIfExists("funnelRevenue", formatCurrency(funnel.totalRevenue));
    setIfExists("funnelSpinToVoucherRate", `${funnel.spinToVoucherRate || 0}%`);
    setIfExists("funnelVoucherToUsageRate", `${funnel.voucherToUsageRate || 0}%`);
    setIfExists("funnelAvgRevenue", `Ort: ${formatCurrency(funnel.usageToRevenueRate)}`);
  }

  logsCurrentPage = 1;
  logsFilter = {};

  async loadDetailedLogs(page = 1) {
    try {
      this.logsCurrentPage = page;
      const container = document.getElementById("detailedLogsTable");
      if (!container) return;

      container.innerHTML = `<tr><td colspan="8" class="px-6 py-16 text-center"><div class="flex flex-col items-center gap-2"><div class="w-10 h-10 rounded-full bg-indigo-50 flex items-center justify-center"><i class="fas fa-spinner fa-spin text-indigo-400"></i></div><span class="text-sm text-gray-400">Yükleniyor...</span></div></td></tr>`;

      const dateParams = this.getDateRangeParams();
      const filter = {
        ...this.logsFilter,
        page: page,
        pageSize: 20
      };

      if (dateParams && !filter.startDate && !filter.endDate) {
        filter.startDate = dateParams.startDate;
        filter.endDate = dateParams.endDate;
      }

      const response = await fetch("/GiftWheel/GetDetailedLogs", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(filter)
      });
      const result = await response.json();

      if (result.success && result.data) {
        this.renderDetailedLogs(result.data);
      } else {
        container.innerHTML = `<tr><td colspan="8" class="px-6 py-16 text-center"><div class="flex flex-col items-center gap-3"><div class="w-12 h-12 rounded-full bg-gray-50 flex items-center justify-center"><i class="fas fa-inbox text-gray-300 text-lg"></i></div><span class="text-sm text-gray-400">Kayıt bulunamadı.</span></div></td></tr>`;
      }
    } catch (error) {
      console.error("Error loading detailed logs:", error);
    }
  }

  renderDetailedLogs(data) {
    const container = document.getElementById("detailedLogsTable");
    if (!container) return;

    const items = data.items || [];

    document.getElementById("logsTotalCount").textContent = data.totalCount || 0;
    document.getElementById("logsCurrentPage").textContent = data.page || 1;
    document.getElementById("logsTotalPages").textContent = data.totalPages || 1;

    const prevBtn = document.getElementById("logsPrevPage");
    const nextBtn = document.getElementById("logsNextPage");
    if (prevBtn) prevBtn.disabled = !data.hasPreviousPage;
    if (nextBtn) nextBtn.disabled = !data.hasNextPage;

    if (items.length === 0) {
      container.innerHTML = `<tr><td colspan="8" class="px-6 py-16 text-center"><div class="flex flex-col items-center gap-3"><div class="w-12 h-12 rounded-full bg-gray-50 flex items-center justify-center"><i class="fas fa-inbox text-gray-300 text-lg"></i></div><span class="text-sm text-gray-400">Kayıt bulunamadı.</span></div></td></tr>`;
      return;
    }

    const formatCurrency = (value) => value ? new Intl.NumberFormat("tr-TR", { style: "currency", currency: "TRY" }).format(value) : null;
    const formatDate = (dateStr) => {
      if (!dateStr) return { date: "-", time: "" };
      const d = new Date(dateStr);
      return {
        date: d.toLocaleDateString("tr-TR", { day: "2-digit", month: "short", year: "numeric" }),
        time: d.toLocaleTimeString("tr-TR", { hour: "2-digit", minute: "2-digit" })
      };
    };

    container.innerHTML = items.map((log, idx) => {
      const dt = formatDate(log.spinDate);
      const orderTotal = log.orderId ? formatCurrency((log.orderAmount || 0) + (log.kdvAmount || 0)) : null;

      return `
      <tr class="group hover:bg-indigo-50/30 transition-colors duration-150">
        <td class="px-5 py-4">
          <div class="flex items-center gap-2.5">
            <div class="w-8 h-8 rounded-full bg-gradient-to-br from-indigo-100 to-purple-100 flex items-center justify-center flex-shrink-0">
              <span class="text-xs font-bold text-indigo-600">${(log.customerName || "?").charAt(0).toUpperCase()}</span>
            </div>
            <span class="font-semibold text-sm text-gray-900 truncate max-w-[140px]">${log.customerName || "-"}</span>
          </div>
        </td>
        <td class="px-5 py-4">
          <div class="space-y-1">
            <div class="flex items-center gap-1.5 text-xs text-gray-600">
              <i class="fas fa-phone text-[10px] text-gray-300"></i>
              <span class="font-mono">${log.customerPhone || "-"}</span>
            </div>
            ${log.customerEmail ? `
            <div class="flex items-center gap-1.5 text-xs text-gray-400">
              <i class="fas fa-at text-[10px] text-gray-300"></i>
              <span class="truncate max-w-[150px]">${log.customerEmail}</span>
            </div>` : ''}
          </div>
        </td>
        <td class="px-5 py-4">
          <div class="flex flex-col gap-0.5">
            <span class="text-sm font-medium text-gray-900">${log.prizeName}</span>
            ${log.discountAmount ? `
              <span class="inline-flex items-center text-[11px] font-medium text-indigo-600">
                <i class="fas fa-tag text-[9px] mr-1 text-indigo-400"></i>
                ${log.discountType === 1 ? `${log.discountAmount} TL` : `%${log.discountAmount}`}
              </span>` : ''}
          </div>
        </td>
        <td class="px-5 py-4">
          ${log.voucherCode ? `
            <div class="flex items-center gap-2">
              <code class="text-[11px] font-mono font-semibold bg-gradient-to-r from-indigo-50 to-violet-50 text-indigo-700 px-2.5 py-1 rounded-md border border-indigo-100/80 tracking-wider">${log.voucherCode}</code>
              ${log.voucherUsed
                ? '<span class="w-5 h-5 rounded-full bg-emerald-100 flex items-center justify-center" title="Kullanıldı"><i class="fas fa-check text-emerald-600 text-[9px]"></i></span>'
                : '<span class="w-5 h-5 rounded-full bg-gray-100 flex items-center justify-center" title="Bekliyor"><i class="fas fa-hourglass-half text-gray-400 text-[9px]"></i></span>'
              }
            </div>
          ` : '<span class="text-xs text-gray-300">&mdash;</span>'}
        </td>
        <td class="px-5 py-4">
          ${orderTotal ? `
            <div class="flex flex-col gap-0.5">
              <span class="text-sm font-bold text-gray-900">${orderTotal}</span>
              <span class="text-[11px] text-gray-400">${log.orderDate || ''}</span>
            </div>
          ` : '<span class="text-xs text-gray-300">&mdash;</span>'}
        </td>
        <td class="px-5 py-4 text-center">
          ${log.communicationConsent
            ? '<span class="w-6 h-6 inline-flex items-center justify-center rounded-full bg-teal-50 ring-1 ring-teal-200" title="İzin verildi"><i class="fas fa-check text-teal-600 text-[10px]"></i></span>'
            : '<span class="w-6 h-6 inline-flex items-center justify-center rounded-full bg-gray-50 ring-1 ring-gray-200" title="İzin yok"><i class="fas fa-times text-gray-400 text-[10px]"></i></span>'
          }
        </td>
        <td class="px-5 py-4">
          <div class="flex flex-col">
            <span class="text-xs font-medium text-gray-700">${dt.date}</span>
            <span class="text-[11px] text-gray-400">${dt.time}</span>
          </div>
        </td>
        <td class="px-5 py-4 text-center">
          <div class="flex flex-col items-center gap-1">
            ${log.isSuccessful
              ? '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-semibold bg-emerald-50 text-emerald-700 ring-1 ring-inset ring-emerald-600/10"><i class="fas fa-circle-check text-[9px]"></i>Başarılı</span>'
              : '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-semibold bg-red-50 text-red-700 ring-1 ring-inset ring-red-600/10"><i class="fas fa-circle-xmark text-[9px]"></i>Başarısız</span>'
            }
            ${log.notificationSent
              ? (log.isNotificationSuccessful
                  ? '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-medium bg-sky-50 text-sky-700 ring-1 ring-inset ring-sky-600/10"><i class="fas fa-bell text-[9px]"></i>Bildirim</span>'
                  : '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-medium bg-amber-50 text-amber-700 ring-1 ring-inset ring-amber-600/10"><i class="fas fa-triangle-exclamation text-[9px]"></i>Hata</span>')
              : ''
            }
          </div>
        </td>
      </tr>
    `}).join("");
  }

  initLogsEventListeners() {
    document.getElementById("toggleLogsFilter")?.addEventListener("click", () => {
      const section = document.getElementById("logsFilterSection");
      if (section) section.classList.toggle("hidden");
    });

    document.getElementById("applyLogsFilter")?.addEventListener("click", () => {
      this.logsFilter = {
        startDate: document.getElementById("logsStartDate")?.value || null,
        endDate: document.getElementById("logsEndDate")?.value || null,
        voucherUsed: document.getElementById("logsVoucherUsed")?.value === "" ? null : document.getElementById("logsVoucherUsed")?.value === "true",
        searchTerm: document.getElementById("logsSearchTerm")?.value || null
      };
      this.loadDetailedLogs(1);
    });

    document.getElementById("clearLogsFilter")?.addEventListener("click", () => {
      document.getElementById("logsStartDate").value = "";
      document.getElementById("logsEndDate").value = "";
      document.getElementById("logsVoucherUsed").value = "";
      document.getElementById("logsSearchTerm").value = "";
      this.logsFilter = {};
      this.loadDetailedLogs(1);
    });

    document.getElementById("refreshLogsBtn")?.addEventListener("click", () => {
      this.loadDetailedLogs(this.logsCurrentPage);
    });

    document.getElementById("logsPrevPage")?.addEventListener("click", () => {
      if (this.logsCurrentPage > 1) {
        this.loadDetailedLogs(this.logsCurrentPage - 1);
      }
    });

    document.getElementById("logsNextPage")?.addEventListener("click", () => {
      this.loadDetailedLogs(this.logsCurrentPage + 1);
    });
  }


  // Script Generation
  async loadEmbedScript() {
    try {
      const response = await fetch("/GiftWheel/GetEmbedScript");
      const script = await response.text();

      document.getElementById("jsCodeContent").textContent = script;
    } catch (error) {
      console.error("Error loading embed script:", error);
      document.getElementById("jsCodeContent").textContent = "// Script yüklenirken hata oluştu.";
    }
  }

  // Full Preview
  async openFullPreview() {
    try {
      // Get current wheel configuration
      const data = await this.loadWheelPreviewData();
      if (!data) {
        showNotification("Önizleme verileri yüklenemedi.", "error");
        return;
      }

      // Open in new window/tab
      const previewWindow = window.open("", "_blank", "width=800,height=600,scrollbars=yes,resizable=yes");

      if (!previewWindow) {
        showNotification("Pop-up engelleyici nedeniyle önizleme açılamadı.", "error");
        return;
      }

      // Generate full preview HTML
      const previewHtml = this.generateFullPreviewHtml(data);
      previewWindow.document.write(previewHtml);
      previewWindow.document.close();
    } catch (error) {
      console.error("Error opening full preview:", error);
      showNotification("Tam ekran önizleme açılırken hata oluştu.", "error");
    }
  }

  generateFullPreviewHtml(data) {
    return `
            <!DOCTYPE html>
            <html lang="tr">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Hediye Çarkı - Tam Ekran Önizleme</title>
                <style>
                    body { margin: 0; padding: 20px; font-family: Arial, sans-serif; background: #f5f5f5; }
                    .preview-container { max-width: 600px; margin: 0 auto; background: white; padding: 20px; border-radius: 10px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }
                    .wheel-container { text-align: center; margin: 20px 0; }
                    .wheel { width: 300px; height: 300px; border: 5px solid #333; border-radius: 50%; margin: 0 auto; background: conic-gradient(${this.generateWheelGradient(
                      data.prizes
                    )}); }
                    .form-container { margin: 20px 0; }
                    .form-input { width: 100%; padding: 10px; margin: 5px 0; border: 1px solid #ddd; border-radius: 5px; }
                    .spin-button { width: 100%; padding: 15px; background: ${
                      data.primaryColor || "#4f46e5"
                    }; color: white; border: none; border-radius: 5px; font-size: 16px; cursor: pointer; }
                    .spin-button:hover { opacity: 0.9; }
                    .title { font-size: 24px; font-weight: bold; margin-bottom: 10px; }
                    .subtitle { color: #666; margin-bottom: 20px; }
                </style>
            </head>
            <body>
                <div class="preview-container">
                    <h1 class="title">${data.title || "Çarkı Çevir, Hediyeni Kazan!"}</h1>
                    <p class="subtitle">${data.subtitle || "Şansını dene ve harika hediyeler kazan"}</p>

                    <div class="wheel-container">
                        <div class="wheel"></div>
                    </div>

                    <div class="form-container">
                        <input type="text" class="form-input" placeholder="Adınız" required>
                        <input type="tel" class="form-input" placeholder="Telefon Numaranız" required>
                        ${
                          data.requireEmail
                            ? '<input type="email" class="form-input" placeholder="E-posta Adresiniz" required>'
                            : ""
                        }
                        <button class="spin-button">${data.buttonText || "Çarkı Çevir"}</button>
                    </div>

                    <div style="margin-top: 20px; padding: 15px; background: #f0f9ff; border-radius: 5px; text-align: center;">
                        <small style="color: #0369a1;">Bu bir önizlemedir. Gerçek çark işlevselliği için kodu sitenize entegre edin.</small>
                    </div>
                </div>
            </body>
            </html>
        `;
  }

  generateWheelGradient(prizes) {
    if (!prizes || prizes.length === 0) return "#f3f4f6";

    const activePrizes = prizes.filter((p) => p.isActive);
    if (activePrizes.length === 0) return "#f3f4f6";

    const segmentSize = 100 / activePrizes.length;
    let gradient = "";

    activePrizes.forEach((prize, index) => {
      const start = index * segmentSize;
      const end = (index + 1) * segmentSize;
      gradient += `${prize.color || "#6b7280"} ${start}% ${end}%`;
      if (index < activePrizes.length - 1) gradient += ", ";
    });

    return gradient;
  }

  setupPrizeActions() {
    document.querySelectorAll(".toggle-prize-status").forEach((btn) => {
      btn.addEventListener("click", (e) => {
        const prizeId = parseInt(e.currentTarget.dataset.prizeId);
        const isActive = e.currentTarget.dataset.active === "true";
        this.togglePrizeStatus(prizeId, !isActive);
      });
    });

    // Enable all prizes
    document.getElementById("enableAllPrizes")?.addEventListener("click", () => {
      this.toggleAllPrizes(true);
    });

    // Disable all prizes
    document.getElementById("disableAllPrizes")?.addEventListener("click", () => {
      this.toggleAllPrizes(false);
    });

    // Initialize drag and drop
    if (typeof window.initializeDragAndDrop === "function") {
      window.initializeDragAndDrop();
    }
  }

  // Prize Management Actions
  async removeAllPrizes() {
    showConfirmationDialog('Tümünü Sil', "Tüm ödülleri silmek istediğinizden emin misiniz? Bu işlem geri alınamaz!", 'Tümünü Sil', 'İptal', async () => {
      try {
        this.showLoading(true);

        const response = await fetch("/GiftWheel/DeleteAllPrizes", {
          method: "DELETE",
          headers: {
            RequestVerificationToken: this.getAntiForgeryToken(),
          },
        });

        const result = await response.json();

        if (result.success) {
          showNotification(result.message, "success");
          if (typeof this.refreshPrizesUI === "function") {
            await this.refreshPrizesUI();
          } else {
            window.location.reload();
          }
        } else {
          showNotification(result.message, "error");
        }
      } catch (error) {
        console.error("Error deleting all prizes:", error);
        showNotification("Ödüller silinirken hata oluştu.", "error");
      } finally {
        this.showLoading(false);
      }
    });
  }

  async deletePrize(prizeId) {
    showConfirmationDialog('Sil', "Bu ödülü silmek istediğinizden emin misiniz?", 'Sil', 'İptal', async () => {
      try {
        this.showLoading(true);

        const response = await fetch(`/GiftWheel/DeletePrize/${prizeId}`, {
          method: "DELETE",
          headers: {
            RequestVerificationToken: this.getAntiForgeryToken(),
          },
        });

        const result = await response.json();

        if (result.success) {
          showNotification(result.message, "success");

          // Remove prize from DOM
          const prizeElement = document.querySelector(`[data-prize-id="${prizeId}"]`);
          if (prizeElement) {
            prizeElement.remove();
          }

          // Update probability display
          this.updateProbabilityDisplay();

          // Check if no prizes left and show empty state
          const prizesList = document.getElementById("prizesList");
          if (prizesList && prizesList.children.length === 0) {
            prizesList.innerHTML = `
                        <div class="text-center py-12 bg-gray-50 rounded-lg border-2 border-dashed border-gray-300">
                            <i class="fas fa-gift text-4xl text-gray-400 mb-4"></i>
                            <h3 class="text-lg font-medium text-gray-900 mb-2">Henüz ödül eklenmemiş</h3>
                            <p class="text-gray-600 mb-4">Çarkınıza ödül ekleyerek başlayın</p>
                            <button onclick="window.giftWheelAdmin.openPrizeModal()"
                                    class="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg font-medium transition-colors">
                                <i class="fas fa-plus mr-2"></i>
                                İlk Ödülü Ekle
                            </button>
                        </div>
                    `;
          }
        } else {
          showNotification(result.message, "error");
        }
      } catch (error) {
        console.error("Error deleting prize:", error);
        showNotification("Ödül silinirken hata oluştu.", "error");
      } finally {
        this.showLoading(false);
      }
    });
  }

  async togglePrizeStatus(prizeId, isActive) {
    try {
      const response = await fetch(`/GiftWheel/TogglePrizeStatus/${prizeId}`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: this.getAntiForgeryToken(),
        },
        body: JSON.stringify({ isActive }),
      });

      const result = await response.json();

      if (result.success) {
        // Update prize status in DOM
        const prizeElement = document.querySelector(`.prize-item[data-prize-id="${prizeId}"]`);
        if (prizeElement) {
          const toggleButton = prizeElement.querySelector(".toggle-prize-status");

          if (toggleButton) {
            // Update toggle button with eye icon
            toggleButton.dataset.active = isActive.toString();
            toggleButton.title = isActive ? "Aktif - Pasif yapmak için tıklayın" : "Pasif - Aktif yapmak için tıklayın";

            // Update onclick handler with new state
            toggleButton.setAttribute("onclick", `window.giftWheelAdmin.togglePrizeStatus(${prizeId}, ${!isActive})`);

            // Update icon: aktif ise eye, pasif ise eye-slash
            const icon = toggleButton.querySelector("i");
            if (icon) {
              icon.className = `fas fa-${isActive ? "eye" : "eye-slash"}`;
            }

            // Update button color: aktif ise yeşil, pasif ise gri
            if (isActive) {
              toggleButton.className = "toggle-prize-status p-2 transition-colors text-green-600 hover:text-green-800";
            } else {
              toggleButton.className = "toggle-prize-status p-2 transition-colors text-gray-400 hover:text-gray-600";
            }
          }
        }

        // Update probability display
        this.updateProbabilityDisplay();

        showNotification(result.message, "success");
      } else {
        showNotification(result.message, "error");
      }
    } catch (error) {
      console.error("Error toggling prize status:", error);
      showNotification("Ödül durumu değiştirilirken hata oluştu.", "error");
    }
  }

  async toggleAllPrizes(isActive) {
    const action = isActive ? "aktif" : "pasif";

    showConfirmationDialog('Durum Değiştir', `Tüm ödülleri ${action} yapmak istediğinizden emin misiniz?`, 'Evet', 'İptal', async () => {
      try {
        this.showLoading(true);

        const response = await fetch("/GiftWheel/ToggleAllPrizes", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            RequestVerificationToken: this.getAntiForgeryToken(),
          },
          body: JSON.stringify({ isActive }),
        });

        const result = await response.json();

        if (result.success) {
          showNotification(result.message, "success");

          // Update all prize statuses in DOM
          await this.refreshPrizesUI();
        } else {
          showNotification(result.message, "error");
        }
      } catch (error) {
        console.error("Error toggling all prizes:", error);
        showNotification("Ödül durumları değiştirilirken hata oluştu.", "error");
      } finally {
        this.showLoading(false);
      }
    });
  }

  // Preview Modal
  async openPreviewModal() {
    console.log("Opening preview modal...");
    const modal = document.getElementById("previewModal");
    if (!modal) {
      console.error("Preview modal not found");
      showNotification("Önizleme modalı bulunamadı.", "error");
      return;
    }

    modal.classList.remove("hidden");

    // Wait a bit for modal to be visible and scripts to load
    setTimeout(async () => {
      try {
        // Load preview data
        const data = await this.loadWheelPreviewData();
        console.log("Preview data loaded:", data);

        if (!data) {
          console.error("No preview data available");
          showNotification("Önizleme verileri yüklenemedi.", "error");
          return;
        }

        const container = document.getElementById("modalPreviewWidget");
        if (container) {
          // Clear previous content
          container.innerHTML = "";

          // Initialize new wheel instance
          const previewInstance = new PushonicaGiftWheel({
            config: data,
            targetElement: container,
            manualInit: true,
            styleId: "modal-preview-styles",
            containerId: "modal-preview-widget",
            livePreview: true, // Enable preview mode (no API calls)
          });

          previewInstance.init();

          // Store instance for later use (e.g. reset)
          this.modalPreviewInstance = previewInstance;
        }
      } catch (error) {
        console.error("Error in openPreviewModal:", error);
        showNotification("Önizleme açılırken hata oluştu.", "error");
      }
    }, 100);
  }

  async loadWheelPreviewData() {
    try {
      console.log("Loading wheel preview data...");
      const response = await fetch("/GiftWheel/GetWheelConfig");

      if (!response.ok) {
        console.error("Response not ok:", response.status, response.statusText);
        return null;
      }

      const result = await response.json();
      console.log("Raw response:", result);

      if (result.success && result.data) {
        console.log("Preview data loaded successfully:", result.data);
        console.log("Prizes count:", result.data.prizes ? result.data.prizes.length : 0);
        return result.data;
      } else {
        console.error("Response indicates failure:", result);
        return null;
      }
    } catch (error) {
      console.error("Error loading preview data:", error);
    }

    return null;
  }

  // Utility Methods
  getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
  }

  showLoading(show, options = {}) {
    this.isLoading = show;

    if (show) {
        // Use Global Fullscreen Loader by default
        BusyIndicator.show(null, {
            text: 'İşlem yapılıyor...',
            ...options
        });
    } else {
        BusyIndicator.hide(null);
    }
  }
}

// Global functions for use in partials
window.addPrizeFromTemplate = function (template) {
  const admin = window.giftWheelAdmin;
  if (admin) {
    admin.openPrizeModal();

    // Fill form with template data
    setTimeout(() => {
      document.getElementById("prizeName").value = template.name;
      document.getElementById("prizeType").value = template.prizeType;
      document.getElementById("discountAmount").value = template.discountAmount || "";
      document.getElementById("discountType").value = template.discountType || 1;
      document.getElementById("probability").value = template.probability;
      document.getElementById("probabilitySlider").value = template.probability;
      document.getElementById("prizeColor").value = template.color;
      document.getElementById("prizeColorText").value = template.color;

      // Update color preview
      document.getElementById("prizePreviewColor").style.backgroundColor = template.color;

      // Trigger change events
      document.getElementById("prizeType").dispatchEvent(new Event("change"));
      document.getElementById("probability").dispatchEvent(new Event("input"));
      document.getElementById("prizeName").dispatchEvent(new Event("input"));
    }, 100);
  }
};

// Global function for prize form submission
window.submitPrizeForm = async function (formData) {
  const admin = window.giftWheelAdmin;
  if (admin) {
    await admin.submitPrizeForm(formData);
  }
};

window.loadStatistics = function () {
  const admin = window.giftWheelAdmin;
  if (admin) {
    admin.loadStatistics();
  }
};


window.loadEmbedScript = function () {
  const admin = window.giftWheelAdmin;
  if (admin) {
    admin.loadEmbedScript();
  }
};

window.loadWheelPreviewData = function () {
  const admin = window.giftWheelAdmin;
  if (admin) {
    return admin.loadWheelPreviewData();
  }
  return Promise.resolve(null);
};

// Initialize when DOM is ready
document.addEventListener("DOMContentLoaded", function () {
  window.giftWheelAdmin = new GiftWheelAdmin();

  // Voucher mode toggle
  const voucherModeRadios = document.querySelectorAll('input[name="voucherMode"]');
  const validityDaysContainer = document.getElementById("validityDaysContainer");

  function updateValidityDaysVisibility() {
    const selectedMode = document.querySelector('input[name="voucherMode"]:checked')?.value;
    if (validityDaysContainer) {
      validityDaysContainer.style.display = selectedMode === "duration" ? "block" : "none";
    }
  }

  voucherModeRadios.forEach(radio => {
    radio.addEventListener("change", updateValidityDaysVisibility);
  });
  updateValidityDaysVisibility();

  // Voucher code format toggle
  const voucherCodeFormatRadios = document.querySelectorAll('input[name="voucherCodeFormat"]');
  const voucherPrefixContainer = document.getElementById("voucherPrefixContainer");

  function updatePrefixVisibility() {
    const selectedFormat = document.querySelector('input[name="voucherCodeFormat"]:checked')?.value;
    if (voucherPrefixContainer) {
      voucherPrefixContainer.style.display = (selectedFormat === "auto_prefix" || selectedFormat === "fixed_prefix") ? "block" : "none";
    }
  }

  voucherCodeFormatRadios.forEach(radio => {
    radio.addEventListener("change", updatePrefixVisibility);
  });
  updatePrefixVisibility();
});

// Drag and Drop Logic (added to fix the issue)
window.initializeDragAndDrop = function () {
  const prizesList = document.getElementById("prizesList");
  if (!prizesList) return;

  const prizeItems = prizesList.querySelectorAll(".prize-item");
  if (prizeItems.length === 0) return;

  prizeItems.forEach((item) => {
    item.draggable = true;
    // prevent duplicate listeners
    item.removeEventListener("dragstart", handleDragStart);
    item.removeEventListener("dragend", handleDragEnd);
    item.removeEventListener("dragover", handleDragOver);
    item.removeEventListener("dragenter", handleDragEnter);
    item.removeEventListener("dragleave", handleDragLeave);
    item.removeEventListener("drop", handleDrop);

    item.addEventListener("dragstart", handleDragStart);
    item.addEventListener("dragend", handleDragEnd);
    item.addEventListener("dragover", handleDragOver);
    item.addEventListener("dragenter", handleDragEnter);
    item.addEventListener("dragleave", handleDragLeave);
    item.addEventListener("drop", handleDrop);
  });
};

let draggedItem = null;

function handleDragStart(e) {
  draggedItem = this;
  this.style.opacity = "0.4";
  this.classList.add("dragging");
  e.dataTransfer.effectAllowed = "move";
  e.dataTransfer.setData("text/plain", this.dataset.prizeId);
}

function handleDragEnd(e) {
  this.style.opacity = "1";
  this.classList.remove("dragging");

  document.querySelectorAll(".prize-item").forEach((item) => {
    item.classList.remove("drag-over");
  });

  draggedItem = null;
}

function handleDragOver(e) {
  e.preventDefault();
  e.dataTransfer.dropEffect = "move";
  return false;
}

function handleDragEnter(e) {
  this.classList.add("drag-over");
}

function handleDragLeave(e) {
  this.classList.remove("drag-over");
}

function handleDrop(e) {
  e.stopPropagation();
  e.preventDefault();

  if (draggedItem && draggedItem !== this) {
    // Swap or move elements
    const list = this.parentNode;
    const allItems = [...list.querySelectorAll(".prize-item")];
    const draggedIdx = allItems.indexOf(draggedItem);
    const thisIdx = allItems.indexOf(this);

    if (draggedIdx < thisIdx) {
      this.after(draggedItem);
    } else {
      this.before(draggedItem);
    }

    // Update Live Preview Immediately (Sort Order Change)
    if (window.updateLivePreview) {
      window.updateLivePreview();
    }

    // Update Probability Bar Order
    if (window.giftWheelAdmin) {
      window.giftWheelAdmin.updateProbabilityDisplay();
    }

    // Update order in backend
    updatePrizeOrder();
  }

  return false;
}

async function updatePrizeOrder() {
  const prizesList = document.getElementById("prizesList");
  if (!prizesList) return;

  const prizeOrders = {};
  prizesList.querySelectorAll(".prize-item").forEach((item, index) => {
    const id = parseInt(item.dataset.prizeId);
    prizeOrders[id] = index + 1; // 1-based sort order
  });

  try {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    const response = await fetch("/GiftWheel/ReorderPrizes", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken: token,
      },
      body: JSON.stringify({ prizeOrders: prizeOrders }),
    });

    const result = await response.json();
    if (result.success) {
      // Optional: show small toast
      // showNotification('Sıralama güncellendi', 'success');
      // Refresh preview if available
      if (typeof updateLivePreview === "function") {
        updateLivePreview();
      }
    }
  } catch (err) {
    console.error("Error updating sort order", err);
  }
}
