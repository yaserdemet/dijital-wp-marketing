using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services.WhatsApp;

namespace PushDashboard.Services.Notifications;

/// <summary>
/// WhatsApp template component hazırlama servisi interface'i.
/// Bu interface, farklı servisler tarafından kullanılarak WhatsApp template
/// component'lerini merkezi bir yerden hazırlamayı sağlar.
/// </summary>
public interface IWhatsAppTemplateComponentService
{
    /// <summary>
    /// WhatsApp template component'lerini hazırlar.
    /// Variable mappings'e göre BODY ve HEADER parametrelerini oluşturur.
    /// </summary>
    /// <param name="companyId">Şirket ID</param>
    /// <param name="template">Facebook WhatsApp template</param>
    /// <param name="context">Database context</param>
    /// <param name="variables">Template değişkenleri dictionary'si</param>
    /// <returns>WhatsApp template component listesi</returns>
    Task<List<WhatsAppTemplateComponent>> PrepareWhatsAppTemplateComponentsAsync(
        Guid companyId,
        FacebookMessageTemplate template,
        ApplicationDbContext context,
        Dictionary<string, string> variables);
}
