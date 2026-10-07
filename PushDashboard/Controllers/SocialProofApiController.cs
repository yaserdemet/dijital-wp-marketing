using Microsoft.AspNetCore.Mvc;
using PushDashboard.Services;
using System.Text;
using System.Text.Json;

namespace PushDashboard.Controllers;

[ApiController]
[Route("api/social-proof")]
public class SocialProofApiController : ControllerBase
{
    private readonly ISocialProofService _socialProofService;
    private readonly ILogger<SocialProofApiController> _logger;

    public SocialProofApiController(
        ISocialProofService socialProofService,
        ILogger<SocialProofApiController> logger)
    {
        _socialProofService = socialProofService;
        _logger = logger;
    }

    /// <summary>
    /// JavaScript widget script'ini döner
    /// </summary>
    [HttpGet("script/{companyId}")]
    public async Task<IActionResult> Script(Guid companyId)
    {
        var config = await _socialProofService.GetScriptConfigAsync(companyId);

        if (!config.IsActive)
        {
            return Content("// Social Proof modülü aktif değil", "application/javascript");
        }

        var configJson = JsonSerializer.Serialize(config,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        var scriptContent = GenerateJavaScriptWidget(configJson);

        Response.Headers.Add("Cache-Control", "public, max-age=300"); // 5 dakika cache
        Response.Headers.Add("Content-Type", "application/javascript; charset=utf-8");

        return Content(scriptContent, "application/javascript");
    }

    /// <summary>
    /// Konfigürasyon bilgilerini JSON olarak döner
    /// </summary>
    [HttpGet("config/{companyId}")]
    public async Task<IActionResult> Config(Guid companyId)
    {
        var config = await _socialProofService.GetScriptConfigAsync(companyId);

        Response.Headers.Add("Cache-Control", "public, max-age=60"); // 1 dakika cache
        Response.Headers.Add("Access-Control-Allow-Origin", "*");

        return Ok(config);
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

    // Social Proof Widget Class
    class SocialProofWidget {{
        constructor(config) {{
            this.config = config;
            this.currentType = 0; // 0: viewers, 1: followers, 2: buyers
            this.types = ['viewers', 'followers', 'buyers'];
            this.element = null;
            this.isVisible = false;
            this.updateTimer = null;
            this.hideTimer = null;
            
            this.init();
        }}
        
        init() {{
            this.createStyles();
            this.createWidget();
            this.startUpdateCycle();
        }}
        
        createStyles() {{
            const styleId = 'social-proof-styles';
            if (document.getElementById(styleId)) return;
            
            const styles = `
                .social-proof-widget {{
                    position: fixed;
                    z-index: 999999;
                    padding: 12px 16px;
                    background-color: ${{this.config.displaySettings.backgroundColor}};
                    color: ${{this.config.displaySettings.textColor}};
                    border-radius: ${{this.config.displaySettings.borderRadius}}px;
                    font-size: ${{this.config.displaySettings.fontSize}}px;
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                    cursor: pointer;
                    transition: all 0.3s ease;
                    opacity: 0;
                    transform: translateY(20px);
                    max-width: 300px;
                    word-wrap: break-word;
                    ${{this.config.displaySettings.enableShadow ? 'box-shadow: 0 4px 12px rgba(0,0,0,0.15);' : ''}}
                }}
                
                .social-proof-widget.visible {{
                    opacity: 1;
                    transform: translateY(0);
                }}
                
                .social-proof-widget.bottom-left {{
                    bottom: 20px;
                    left: 20px;
                }}
                
                .social-proof-widget.bottom-right {{
                    bottom: 20px;
                    right: 20px;
                }}
                
                .social-proof-widget.top-left {{
                    top: 20px;
                    left: 20px;
                }}
                
                .social-proof-widget.top-right {{
                    top: 20px;
                    right: 20px;
                }}
                
                .social-proof-widget:hover {{
                    transform: translateY(-2px);
                    ${{this.config.displaySettings.enableShadow ? 'box-shadow: 0 6px 20px rgba(0,0,0,0.2);' : ''}}
                }}
                
                @media (max-width: 768px) {{
                    .social-proof-widget {{
                        font-size: ${{Math.max(12, this.config.displaySettings.fontSize - 2)}}px;
                        padding: 10px 14px;
                        max-width: 250px;
                    }}
                }}
            `;
            
            const styleElement = document.createElement('style');
            styleElement.id = styleId;
            styleElement.textContent = styles;
            document.head.appendChild(styleElement);
        }}
        
        createWidget() {{
            this.element = document.createElement('div');
            this.element.className = `social-proof-widget ${{this.config.displaySettings.position}}`;
            this.element.style.display = 'none';
            
            document.body.appendChild(this.element);
        }}
        
        generateRandomNumber(min, max) {{
            return Math.floor(Math.random() * (max - min + 1)) + min;
        }}
        
        getCurrentTypeConfig() {{
            const type = this.types[this.currentType];
            switch(type) {{
                case 'viewers':
                    return {{
                        min: this.config.viewersMin,
                        max: this.config.viewersMax,
                        template: this.config.textTemplates.viewersTemplate
                    }};
                case 'followers':
                    return {{
                        min: this.config.followersMin,
                        max: this.config.followersMax,
                        template: this.config.textTemplates.followersTemplate
                    }};
                case 'buyers':
                    return {{
                        min: this.config.buyersMin,
                        max: this.config.buyersMax,
                        template: this.config.textTemplates.buyersTemplate
                    }};
                default:
                    return null;
            }}
        }}
        
        updateContent() {{
            const typeConfig = this.getCurrentTypeConfig();
            if (!typeConfig) return;
            
            const count = this.generateRandomNumber(typeConfig.min, typeConfig.max);
            const text = typeConfig.template.replace('{{count}}', count);
            
            this.element.textContent = text;
        }}
        
        show() {{
            if (this.isVisible) return;
            
            this.updateContent();
            this.element.style.display = 'block';
            
            // Kısa bir gecikme sonrası görünür yap (CSS transition için)
            setTimeout(() => {{
                this.element.classList.add('visible');
                this.isVisible = true;
            }}, 50);
            
            // Belirtilen süre sonra gizle
            this.hideTimer = setTimeout(() => {{
                this.hide();
            }}, this.config.displayDuration);
        }}
        
        hide() {{
            if (!this.isVisible) return;
            
            this.element.classList.remove('visible');
            this.isVisible = false;
            
            setTimeout(() => {{
                this.element.style.display = 'none';
            }}, 300); // CSS transition süresi
            
            if (this.hideTimer) {{
                clearTimeout(this.hideTimer);
                this.hideTimer = null;
            }}
        }}
        
        nextType() {{
            this.currentType = (this.currentType + 1) % this.types.length;
        }}
        
        startUpdateCycle() {{
            // İlk gösterimi biraz geciktir
            setTimeout(() => {{
                this.show();
            }}, 2000);
            
            // Periyodik güncelleme
            this.updateTimer = setInterval(() => {{
                this.nextType();
                this.show();
            }}, this.config.updateInterval);
        }}
        
        destroy() {{
            if (this.updateTimer) {{
                clearInterval(this.updateTimer);
            }}
            if (this.hideTimer) {{
                clearTimeout(this.hideTimer);
            }}
            if (this.element) {{
                this.element.remove();
            }}
        }}
    }}
    
    // Widget'ı başlat
    let widget = null;
    
    function initWidget() {{
        if (widget) {{
            widget.destroy();
        }}
        widget = new SocialProofWidget(config);
    }}
    
    // DOM hazır olduğunda başlat
    if (document.readyState === 'loading') {{
        document.addEventListener('DOMContentLoaded', initWidget);
    }} else {{
        initWidget();
    }}
    
    // Global erişim için
    window.SocialProofWidget = {{
        destroy: () => widget && widget.destroy(),
        restart: () => initWidget()
    }};
    
}})();";

        return script;
    }
}