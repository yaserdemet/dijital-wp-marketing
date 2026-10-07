using Microsoft.AspNetCore.Mvc;
using PushDashboard.Models.ViewModels;
using PushDashboard.Services;
using System.Text.Json;

namespace PushDashboard.Controllers;

[ApiController]
[Route("api/cookie-management")]
public class CookieManagementApiController : ControllerBase
{
    private readonly ICookieManagementService _cookieManagementService;
    private readonly ILogger<CookieManagementApiController> _logger;

    public CookieManagementApiController(
        ICookieManagementService cookieManagementService,
        ILogger<CookieManagementApiController> logger)
    {
        _cookieManagementService = cookieManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Çerez onay event'lerini kaydeder
    /// </summary>
    [HttpPost("track")]
    public async Task<IActionResult> Track([FromBody] CookieManagementTrackingViewModel model)
    {
        try
        {
            var validEventTypes = new[] { "banner_view", "accept_all", "reject_all", "open_preferences", "save_preferences", "impression", "save_settings" };
            if (!validEventTypes.Contains(model.EventType))
            {
                return BadRequest(new { success = false, message = "Geçersiz event type" });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var success = await _cookieManagementService.RecordCookieTrackingEventAsync(model, ipAddress, userAgent);

            return success
                ? Ok(new { success = true })
                : BadRequest(new { success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking cookie consent event");
            return StatusCode(500, new { success = false });
        }
    }

    /// <summary>
    /// JavaScript widget script'ini döner
    /// </summary>
    [HttpGet("script/{companyId}")]
    public async Task<IActionResult> Script(Guid companyId)
    {
        var config = await _cookieManagementService.GetScriptConfigAsync(companyId);

        if (!config.IsActive)
        {
            return Content("// Çerez Yönetimi modülü aktif değil", "application/javascript");
        }

        var configJson = JsonSerializer.Serialize(config,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        var scriptContent = GenerateJavaScriptWidget(configJson);

        Response.Headers["Cache-Control"] = "public, max-age=300"; // 5 dakika cache
        Response.Headers["Content-Type"] = "application/javascript; charset=utf-8";

        return Content(scriptContent, "application/javascript");

    }

    private string GenerateJavaScriptWidget(string configJson)
    {
        var script = $@"
(function() {{
    'use strict';
    
    // Konfigürasyon
    const config = {configJson};
    
    if (!config.isActive) {{
        return;
    }}

    // Cookie Management Widget Class
    class CookieManagementWidget {{
        constructor(config) {{
            this.config = config;
            this.bannerElement = null;
            this.modalElement = null;
            this.isVisible = false;
            this.isModalOpen = false;
            this.consentKey = 'cookie-consent';
            this.preferencesKey = 'cookie-preferences';
            
            this.init();
        }}
        
        init() {{
            this.createStyles();
            this.checkConsent();
        }}
        
        createStyles() {{
            const styleId = 'cookie-management-styles';
            if (document.getElementById(styleId)) return;
            
            const style = document.createElement('style');
            style.id = styleId;
            style.textContent = `
                .cookie-banner {{
                    position: fixed;
                    z-index: 999999;
                    background: ${{this.config.bannerBackgroundColor}};
                    color: ${{this.config.bannerTextColor}};
                    padding: 20px;
                    border-radius: ${{this.config.borderRadius}};
                    box-shadow: 0 4px 20px rgba(0,0,0,0.15);
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                    font-size: 14px;
                    line-height: 1.5;
                    max-width: 400px;
                    ${{this.getBannerPositionStyles()}}
                    ${{this.config.enableAnimation ? 'transition: all 0.3s ease-in-out; transform: translateY(100%);' : ''}}
                }}
                
                .cookie-banner.visible {{
                    ${{this.config.enableAnimation ? 'transform: translateY(0);' : ''}}
                }}
                
                .cookie-banner h3 {{
                    margin: 0 0 10px 0;
                    font-size: 16px;
                    font-weight: 600;
                }}
                
                .cookie-banner p {{
                    margin: 0 0 15px 0;
                    opacity: 0.9;
                }}
                
                .cookie-buttons {{
                    display: flex;
                    gap: 10px;
                    flex-wrap: wrap;
                }}
                
                .cookie-btn {{
                    padding: 8px 16px;
                    border: none;
                    border-radius: 4px;
                    cursor: pointer;
                    font-size: 13px;
                    font-weight: 500;
                    transition: all 0.2s ease;
                    text-decoration: none;
                    display: inline-block;
                }}
                
                .cookie-btn:hover {{
                    transform: translateY(-1px);
                    box-shadow: 0 2px 8px rgba(0,0,0,0.15);
                }}
                
                .cookie-btn-accept {{
                    background: ${{this.config.acceptButtonColor}};
                    color: white;
                }}
                
                .cookie-btn-reject {{
                    background: ${{this.config.rejectButtonColor}};
                    color: white;
                }}
                
                .cookie-btn-settings {{
                    background: ${{this.config.settingsButtonColor}};
                    color: white;
                }}
                
                .cookie-modal {{
                    position: fixed;
                    top: 0;
                    left: 0;
                    width: 100%;
                    height: 100%;
                    background: rgba(0,0,0,0.5);
                    z-index: 1000000;
                    display: none;
                    align-items: center;
                    justify-content: center;
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                }}
                
                .cookie-modal-content {{
                    background: white;
                    padding: 30px;
                    border-radius: 12px;
                    max-width: 500px;
                    width: 90%;
                    max-height: 80vh;
                    overflow-y: auto;
                    position: relative;
                }}
                
                .cookie-modal h3 {{
                    margin: 0 0 20px 0;
                    font-size: 18px;
                    font-weight: 600;
                }}
                
                .cookie-category {{
                    margin-bottom: 20px;
                    padding: 15px;
                    border: 1px solid #e0e0e0;
                    border-radius: 8px;
                }}
                
                .cookie-category-header {{
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    margin-bottom: 8px;
                }}
                
                .cookie-category h4 {{
                    margin: 0;
                    font-size: 14px;
                    font-weight: 600;
                }}
                
                .cookie-category p {{
                    margin: 0;
                    font-size: 12px;
                    color: #666;
                    line-height: 1.4;
                }}
                
                .cookie-toggle {{
                    position: relative;
                    display: inline-block;
                    width: 44px;
                    height: 24px;
                }}
                
                .cookie-toggle input {{
                    opacity: 0;
                    width: 0;
                    height: 0;
                }}
                
                .cookie-slider {{
                    position: absolute;
                    cursor: pointer;
                    top: 0;
                    left: 0;
                    right: 0;
                    bottom: 0;
                    background-color: #ccc;
                    transition: .4s;
                    border-radius: 24px;
                }}
                
                .cookie-slider:before {{
                    position: absolute;
                    content: '';
                    height: 18px;
                    width: 18px;
                    left: 3px;
                    bottom: 3px;
                    background-color: white;
                    transition: .4s;
                    border-radius: 50%;
                }}
                
                input:checked + .cookie-slider {{
                    background-color: ${{this.config.acceptButtonColor}};
                }}
                
                input:checked + .cookie-slider:before {{
                    transform: translateX(20px);
                }}
                
                input:disabled + .cookie-slider {{
                    opacity: 0.6;
                    cursor: not-allowed;
                }}
                
                .cookie-modal-buttons {{
                    display: flex;
                    gap: 10px;
                    justify-content: flex-end;
                    margin-top: 20px;
                    padding-top: 20px;
                    border-top: 1px solid #e0e0e0;
                }}
                
                .cookie-close {{
                    position: absolute;
                    top: 15px;
                    right: 15px;
                    background: none;
                    border: none;
                    font-size: 20px;
                    cursor: pointer;
                    color: #999;
                }}
                
                @media (max-width: 480px) {{
                    .cookie-banner {{
                        left: 10px !important;
                        right: 10px !important;
                        bottom: 10px !important;
                        max-width: none;
                    }}
                    
                    .cookie-buttons {{
                        flex-direction: column;
                    }}
                    
                    .cookie-btn {{
                        text-align: center;
                    }}
                }}
            `;
            document.head.appendChild(style);
        }}
        
        getBannerPositionStyles() {{
            switch(this.config.bannerPosition) {{
                case 'top':
                    return 'top: 20px; left: 20px; right: 20px; margin: 0 auto;';
                case 'left':
                    return 'left: 20px; top: 50%; transform: translateY(-50%);';
                case 'right':
                    return 'right: 20px; top: 50%; transform: translateY(-50%);';
                default: // bottom
                    return 'bottom: 20px; left: 20px;';
            }}
        }}
        
        checkConsent() {{
            const consent = localStorage.getItem(this.consentKey);
            if (!consent) {{
                this.showBanner();
            }}
        }}
        
        showBanner() {{
            if (this.isVisible) return;
            
            this.bannerElement = document.createElement('div');
            this.bannerElement.className = 'cookie-banner';
            this.bannerElement.innerHTML = `
                <h3>${{this.config.bannerTitle}}</h3>
                <p>${{this.config.bannerDescription}}</p>
                <div class=""cookie-buttons"">
                    <button class=""cookie-btn cookie-btn-accept"" onclick=""window.CookieManagement.acceptAll()"">${{this.config.acceptButtonText}}</button>
                    <button class=""cookie-btn cookie-btn-reject"" onclick=""window.CookieManagement.rejectAll()"">${{this.config.rejectButtonText}}</button>
                    ${{this.config.showSettingsButton ? `<button class=""cookie-btn cookie-btn-settings"" onclick=""window.CookieManagement.openSettings()"">${{this.config.settingsButtonText}}</button>` : ''}}
                </div>
            `;
            
            document.body.appendChild(this.bannerElement);
            
            if (this.config.enableAnimation) {{
                setTimeout(() => {{
                    this.bannerElement.classList.add('visible');
                }}, 100);
            }}
            
            this.isVisible = true;
        }}
        
        hideBanner() {{
            if (this.bannerElement) {{
                this.bannerElement.remove();
                this.bannerElement = null;
                this.isVisible = false;
            }}
        }}
        
        acceptAll() {{
            const preferences = {{}};
            this.config.categories.forEach(category => {{
                preferences[category.id] = true;
            }});
            
            this.saveConsent(preferences);
            this.hideBanner();
        }}
        
        rejectAll() {{
            const preferences = {{}};
            this.config.categories.forEach(category => {{
                preferences[category.id] = category.isRequired;
            }});
            
            this.saveConsent(preferences);
            this.hideBanner();
        }}
        
        openSettings() {{
            this.showModal();
        }}
        
        showModal() {{
            if (this.isModalOpen) return;
            
            this.modalElement = document.createElement('div');
            this.modalElement.className = 'cookie-modal';
            this.modalElement.style.display = 'flex';
            
            const currentPreferences = this.getPreferences();
            
            let categoriesHtml = '';
            this.config.categories.forEach(category => {{
                const isChecked = currentPreferences[category.id] || category.isRequired;
                const isDisabled = category.isRequired ? 'disabled' : '';
                
                categoriesHtml += `
                    <div class=""cookie-category"">
                        <div class=""cookie-category-header"">
                            <h4>${{category.name}}</h4>
                            <label class=""cookie-toggle"">
                                <input type=""checkbox"" id=""cookie-${{category.id}}"" ${{isChecked ? 'checked' : ''}} ${{isDisabled}}>
                                <span class=""cookie-slider""></span>
                            </label>
                        </div>
                        <p>${{category.description}}</p>
                    </div>
                `;
            }});
            
            this.modalElement.innerHTML = `
                <div class=""cookie-modal-content"">
                    <button class=""cookie-close"" onclick=""window.CookieManagement.closeSettings()"">&times;</button>
                    <h3>Çerez Ayarları</h3>
                    ${{categoriesHtml}}
                    <div class=""cookie-modal-buttons"">
                        <button class=""cookie-btn cookie-btn-settings"" onclick=""window.CookieManagement.saveSettings()"">${{this.config.saveButtonText}}</button>
                    </div>
                </div>
            `;
            
            document.body.appendChild(this.modalElement);
            this.isModalOpen = true;
        }}
        
        closeSettings() {{
            if (this.modalElement) {{
                this.modalElement.remove();
                this.modalElement = null;
                this.isModalOpen = false;
            }}
        }}
        
        saveSettings() {{
            const preferences = {{}};
            this.config.categories.forEach(category => {{
                const checkbox = document.getElementById(`cookie-${{category.id}}`);
                preferences[category.id] = checkbox ? checkbox.checked : category.isRequired;
            }});
            
            this.saveConsent(preferences);
            this.closeSettings();
            this.hideBanner();
        }}
        
        saveConsent(preferences) {{
            localStorage.setItem(this.consentKey, 'true');
            localStorage.setItem(this.preferencesKey, JSON.stringify(preferences));
            
            const expiryDate = new Date();
            expiryDate.setDate(expiryDate.getDate() + this.config.cookieExpiryDays);
            localStorage.setItem(this.consentKey + '_expiry', expiryDate.toISOString());
            
            // Trigger custom event
            window.dispatchEvent(new CustomEvent('cookieConsentChanged', {{
                detail: {{ preferences: preferences }}
            }}));
        }}
        
        getPreferences() {{
            try {{
                const preferences = localStorage.getItem(this.preferencesKey);
                return preferences ? JSON.parse(preferences) : {{}};
            }} catch {{
                return {{}};
            }}
        }}
        
        hasConsent(categoryId) {{
            const preferences = this.getPreferences();
            return preferences[categoryId] === true;
        }}
        
        destroy() {{
            this.hideBanner();
            this.closeSettings();
        }}
    }}
    
    // Widget'ı başlat
    let widget = null;
    
    function initWidget() {{
        if (widget) {{
            widget.destroy();
        }}
        widget = new CookieManagementWidget(config);
    }}
    
    // DOM hazır olduğunda başlat
    if (document.readyState === 'loading') {{
        document.addEventListener('DOMContentLoaded', initWidget);
    }} else {{
        initWidget();
    }}
    
    // Global erişim için
    window.CookieManagement = {{
        acceptAll: () => widget && widget.acceptAll(),
        rejectAll: () => widget && widget.rejectAll(),
        openSettings: () => widget && widget.openSettings(),
        closeSettings: () => widget && widget.closeSettings(),
        saveSettings: () => widget && widget.saveSettings(),
        hasConsent: (categoryId) => widget ? widget.hasConsent(categoryId) : false,
        getPreferences: () => widget ? widget.getPreferences() : {{}},
        destroy: () => widget && widget.destroy(),
        restart: () => initWidget()
    }};
    
}})();";

        return script;
    }
}
