namespace PushDashboard.Helpers;

/// <summary>
/// API yanıtları için standart helper sınıfı.
/// Kod tekrarını azaltmak ve tutarlı yanıt formatı sağlamak için kullanılır.
/// </summary>
public static class ApiResponse
{
    #region Generic Response Methods

    /// <summary>
    /// Başarısız yanıt döner
    /// </summary>
    public static object Error(string message) => new { success = false, message };

    /// <summary>
    /// Başarılı yanıt döner (data ve/veya message ile)
    /// </summary>
    public static object Success(object? data = null, string? message = null) =>
        new { success = true, data, message };

    /// <summary>
    /// Sadece mesaj içeren başarılı yanıt döner
    /// </summary>
    public static object SuccessMessage(string message) =>
        new { success = true, message };

    /// <summary>
    /// Sadece data içeren başarılı yanıt döner
    /// </summary>
    public static object SuccessData(object data) =>
        new { success = true, data };

    /// <summary>
    /// ModelState hatalarından yanıt oluşturur
    /// </summary>
    public static object ValidationError(IEnumerable<string> errors) =>
        new { success = false, message = string.Join(" ", errors) };

    #endregion

    #region Common Error Messages - Kullanıcı/Şirket

    /// <summary>
    /// "Kullanıcı bulunamadı." hatası
    /// </summary>
    public static object UserNotFound => Error("Kullanıcı bulunamadı.");

    /// <summary>
    /// "Şirket bilgisi bulunamadı." hatası
    /// </summary>
    public static object CompanyNotFound => Error("Şirket bilgisi bulunamadı.");

    /// <summary>
    /// "Kullanıcının şirketi bulunamadı." hatası
    /// </summary>
    public static object UserCompanyNotFound => Error("Kullanıcının şirketi bulunamadı.");

    /// <summary>
    /// "Kullanıcı oturumu bulunamadı." hatası
    /// </summary>
    public static object SessionNotFound => Error("Kullanıcı oturumu bulunamadı.");

    /// <summary>
    /// "Firma bilgisi bulunamadı." hatası
    /// </summary>
    public static object FirmNotFound => Error("Firma bilgisi bulunamadı.");

    #endregion

    #region Common Error Messages - Entegrasyon

    /// <summary>
    /// "Entegrasyon bulunamadı." hatası
    /// </summary>
    public static object IntegrationNotFound => Error("Entegrasyon bulunamadı.");

    /// <summary>
    /// "Bu entegrasyon şirketiniz için zaten eklenmiş." hatası
    /// </summary>
    public static object IntegrationAlreadyExists => Error("Bu entegrasyon şirketiniz için zaten eklenmiş.");

    #endregion

    #region Common Error Messages - Genel

    /// <summary>
    /// "Bir hata oluştu. Lütfen tekrar deneyin." hatası
    /// </summary>
    public static object GenericError => Error("Bir hata oluştu. Lütfen tekrar deneyin.");

    /// <summary>
    /// "Beklenmeyen bir hata oluştu." hatası
    /// </summary>
    public static object UnexpectedError => Error("Beklenmeyen bir hata oluştu.");

    /// <summary>
    /// "İşlem sırasında bir hata oluştu." hatası
    /// </summary>
    public static object OperationError => Error("İşlem sırasında bir hata oluştu.");

    /// <summary>
    /// "Kayıt bulunamadı." hatası
    /// </summary>
    public static object RecordNotFound => Error("Kayıt bulunamadı.");

    /// <summary>
    /// "Bu işlem için yetkiniz yok." hatası
    /// </summary>
    public static object Unauthorized => Error("Bu işlem için yetkiniz yok.");

    /// <summary>
    /// "Geçersiz istek." hatası
    /// </summary>
    public static object InvalidRequest => Error("Geçersiz istek.");

    #endregion

    #region Common Error Messages - Kaydetme/Güncelleme

    /// <summary>
    /// "Ayarlar kaydedilirken bir hata oluştu." hatası
    /// </summary>
    public static object SettingsSaveError => Error("Ayarlar kaydedilirken bir hata oluştu.");

    /// <summary>
    /// "Ayarlar yüklenirken hata oluştu." hatası
    /// </summary>
    public static object SettingsLoadError => Error("Ayarlar yüklenirken hata oluştu.");

    /// <summary>
    /// "Kayıt silinirken bir hata oluştu." hatası
    /// </summary>
    public static object DeleteError => Error("Kayıt silinirken bir hata oluştu.");

    /// <summary>
    /// "Kayıt güncellenirken bir hata oluştu." hatası
    /// </summary>
    public static object UpdateError => Error("Kayıt güncellenirken bir hata oluştu.");

    #endregion

    #region Common Error Messages - Template

    /// <summary>
    /// "Şablon bulunamadı." hatası
    /// </summary>
    public static object TemplateNotFound => Error("Şablon bulunamadı.");

    /// <summary>
    /// "Şablon yüklenirken hata oluştu." hatası
    /// </summary>
    public static object TemplateLoadError => Error("Şablon yüklenirken hata oluştu.");

    /// <summary>
    /// "Şablon oluşturulurken hata oluştu." hatası
    /// </summary>
    public static object TemplateCreateError => Error("Şablon oluşturulurken hata oluştu.");

    #endregion

    #region Common Success Messages

    /// <summary>
    /// "İşlem başarıyla tamamlandı." mesajı
    /// </summary>
    public static object OperationSuccess => SuccessMessage("İşlem başarıyla tamamlandı.");

    /// <summary>
    /// "Ayarlar başarıyla kaydedildi." mesajı
    /// </summary>
    public static object SettingsSaved => SuccessMessage("Ayarlar başarıyla kaydedildi.");

    /// <summary>
    /// "Kayıt başarıyla silindi." mesajı
    /// </summary>
    public static object DeleteSuccess => SuccessMessage("Kayıt başarıyla silindi.");

    /// <summary>
    /// "Kayıt başarıyla güncellendi." mesajı
    /// </summary>
    public static object UpdateSuccess => SuccessMessage("Kayıt başarıyla güncellendi.");

    /// <summary>
    /// "Kayıt başarıyla oluşturuldu." mesajı
    /// </summary>
    public static object CreateSuccess => SuccessMessage("Kayıt başarıyla oluşturuldu.");

    #endregion
}
