using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// İlk alışverişini yapmış müşteri kaydı
/// Pushonice'a kayıt olduktan sonra yapılan ilk siparişleri takip eder
/// </summary>
public class FirstOrderCustomer
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Şirket ID
    /// </summary>
    [Required]
    public Guid CompanyId { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public Company? Company { get; set; }

    /// <summary>
    /// Pushonice müşteri ID (Customers tablosundaki ID)
    /// </summary>
    public int? CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    /// <summary>
    /// E-ticaret platformundaki müşteri ID (Ticimax UyeID vb.)
    /// </summary>
    [Required]
    public int CustomerExternalId { get; set; }

    /// <summary>
    /// Müşteri adı soyadı
    /// </summary>
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Müşteri e-posta
    /// </summary>
    [MaxLength(200)]
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Müşteri telefon
    /// </summary>
    [MaxLength(50)]
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// E-ticaret platformundaki sipariş ID
    /// </summary>
    [MaxLength(50)]
    public string OrderExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Sipariş numarası
    /// </summary>
    [MaxLength(100)]
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Sipariş tarihi
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Sipariş tutarı
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal OrderAmount { get; set; }

    /// <summary>
    /// Para birimi
    /// </summary>
    [MaxLength(10)]
    public string OrderCurrency { get; set; } = "TRY";

    /// <summary>
    /// E-posta izni
    /// </summary>
    public bool EmailPermission { get; set; }

    /// <summary>
    /// SMS izni
    /// </summary>
    public bool SmsPermission { get; set; }

    /// <summary>
    /// E-posta bildirimi başarıyla gönderildi mi?
    /// </summary>
    public bool EmailSent { get; set; }

    /// <summary>
    /// SMS bildirimi başarıyla gönderildi mi?
    /// </summary>
    public bool SmsSent { get; set; }

    /// <summary>
    /// WhatsApp bildirimi başarıyla gönderildi mi?
    /// </summary>
    public bool WhatsAppSent { get; set; }

    /// <summary>
    /// Hediye çeki kodu
    /// </summary>
    [MaxLength(50)]
    public string? GiftVoucherCode { get; set; }

    /// <summary>
    /// Hediye çeki tutarı
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? GiftVoucherAmount { get; set; }

    /// <summary>
    /// Hediye çeki kullanıldı mı?
    /// </summary>
    public bool GiftVoucherUsed { get; set; } = false;

    /// <summary>
    /// Hediye çeki kullanım tarihi
    /// </summary>
    public DateTime? GiftVoucherUsedAt { get; set; }

    /// <summary>
    /// Kayıt oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Son güncelleme tarihi
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Bildirim gönderildi mi? (İşlendi mi?)
    /// </summary>
    public bool IsProcessed { get; set; }

    /// <summary>
    /// İşlenme tarihi
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}

