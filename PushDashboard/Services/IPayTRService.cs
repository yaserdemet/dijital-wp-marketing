using PushDashboard.Models.DTOs;

namespace PushDashboard.Services;

public interface IPayTRService
{
    /// <summary>
    /// Yeni kartla ödeme işlemi başlatır
    /// </summary>
    Task<PayTRPaymentResult> ProcessPaymentAsync(PayTRPaymentRequest request);

    /// <summary>
    /// Kayıtlı kartla ödeme işlemi başlatır
    /// </summary>
    Task<PayTRPaymentResult> ProcessSavedCardPaymentAsync(PayTRSavedCardPaymentRequest request);

    /// <summary>
    /// Kullanıcının kayıtlı kartlarını listeler
    /// </summary>
    Task<List<PayTRCard>> ListSavedCardsAsync(string utoken);

    /// <summary>
    /// Kayıtlı kartı siler
    /// </summary>
    Task<bool> DeleteSavedCardAsync(string utoken, string ctoken);

    /// <summary>
    /// PayTR callback hash doğrulaması yapar
    /// </summary>
    bool ValidateCallbackHash(PayTRCallbackData callbackData);

    /// <summary>
    /// PayTR durum sorgu API'si ile işlem durumunu sorgular
    /// </summary>
    Task<PayTRPaymentResult> GetPaymentStatusAsync(string merchantOid);
}
