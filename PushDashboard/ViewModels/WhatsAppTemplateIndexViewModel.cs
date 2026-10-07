using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.Services.WhatsApp;

namespace PushDashboard.ViewModels;

public class WhatsAppTemplateIndexViewModel
{
    public Dictionary<string, List<FacebookMessageTemplate>> GroupedFacebookTemplates { get; set; } = new();
    public List<FacebookMessageTemplate> AllTemplates { get; set; } = new();
    public List<WhatsAppTemplateSample> SampleTemplates { get; set; } = new();
    public Guid CompanyId { get; set; }
}
