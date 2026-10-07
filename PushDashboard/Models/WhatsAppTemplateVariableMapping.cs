namespace PushDashboard.Models;

/// <summary>
/// WhatsApp template değişkenlerinin ({{1}}, {{2}} vb.) hangi veri alanlarına karşılık geldiğini saklar.
/// Bu sayede template oluşturma sırasında belirlenen mapping'ler mesaj gönderirken kullanılabilir.
/// </summary>
public class WhatsAppTemplateVariableMapping
{
    public int Id { get; set; }

    /// <summary>
    /// Şirket ID'si
    /// </summary>
    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// WhatsApp template adı (FacebookMessageTemplate.Name)
    /// </summary>
    public string WhatsAppTemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Template component tipi (HEADER, BODY, FOOTER, BUTTONS)
    /// Genellikle BODY kullanılır
    /// </summary>
    public string ComponentType { get; set; } = "BODY";

    /// <summary>
    /// Değişkenin template içindeki pozisyonu (1, 2, 3, ...)
    /// Örnek: {{1}} için 1, {{2}} için 2
    /// </summary>
    public int VariablePosition { get; set; }

    /// <summary>
    /// Değişkenin tipi (customer_name, order_number, order_total, vb.)
    /// Bu değer predefinedVariables içindeki key'lerden biri olmalıdır
    /// </summary>
    public string VariableType { get; set; } = string.Empty;

    /// <summary>
    /// Değişkenin insan tarafından okunabilir etiketi
    /// Örnek: "Müşteri Adı", "Sipariş Numarası", vb.
    /// </summary>
    public string? VariableLabel { get; set; }

    /// <summary>
    /// Kayıt oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Kayıt güncellenme tarihi
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
