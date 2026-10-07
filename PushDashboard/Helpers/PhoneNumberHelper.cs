using System.Text.RegularExpressions;

namespace PushDashboard.Helpers;

public static class PhoneNumberHelper
{
    /// <summary>
    /// Telefon numarasını Türkiye formatına göre standardize eder
    /// </summary>
    /// <param name="phoneNumber">Ham telefon numarası</param>
    /// <returns>Standardize edilmiş telefon numarası veya null</returns>
    public static string? FormatPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        // Sadece rakamları al
        var digitsOnly = Regex.Replace(phoneNumber, @"[^\d]", "");

        // Boş ise null döndür
        if (string.IsNullOrEmpty(digitsOnly))
            return null;

        // Türkiye telefon numarası formatları:
        // Cep telefonu: 5XXXXXXXXX (10 haneli)
        // Sabit hat: 2XX XXXXXXX, 3XX XXXXXXX, 4XX XXXXXXX (10 haneli)
        // Uluslararası format: +90 ile başlayan

        // +90 ile başlıyorsa kaldır
        if (digitsOnly.StartsWith("90") && digitsOnly.Length == 12)
        {
            digitsOnly = digitsOnly.Substring(2);
        }

        // 0 ile başlıyorsa kaldır
        if (digitsOnly.StartsWith("0") && digitsOnly.Length == 11)
        {
            digitsOnly = digitsOnly.Substring(1);
        }

        // 10 haneli olmalı
        if (digitsOnly.Length != 10)
            return null;

        // Türkiye telefon numarası kontrolü
        if (!IsValidTurkishPhoneNumber(digitsOnly))
            return null;

        // Formatla: 0XXX XXX XX XX
        return $"0{digitsOnly.Substring(0, 3)} {digitsOnly.Substring(3, 3)} {digitsOnly.Substring(6, 2)} {digitsOnly.Substring(8, 2)}";
    }

    /// <summary>
    /// Telefon numarasının Türkiye formatında geçerli olup olmadığını kontrol eder
    /// </summary>
    /// <param name="phoneNumber">Telefon numarası (sadece rakamlar, 10 haneli)</param>
    /// <returns>Geçerli ise true</returns>
    public static bool IsValidTurkishPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Sadece rakamları al
        var digitsOnly = Regex.Replace(phoneNumber, @"[^\d]", "");

        // +90 ile başlıyorsa kaldır
        if (digitsOnly.StartsWith("90") && digitsOnly.Length == 12)
        {
            digitsOnly = digitsOnly.Substring(2);
        }

        // 0 ile başlıyorsa kaldır
        if (digitsOnly.StartsWith("0") && digitsOnly.Length == 11)
        {
            digitsOnly = digitsOnly.Substring(1);
        }

        // 10 haneli olmalı
        if (digitsOnly.Length != 10)
            return false;

        var firstDigit = digitsOnly[0];
        var secondDigit = digitsOnly[1];

        // Cep telefonu: 5XX ile başlar
        if (firstDigit == '5')
        {
            // İkinci hane 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 olabilir
            return char.IsDigit(secondDigit);
        }

        // Sabit hat: 2XX, 3XX, 4XX ile başlar
        if (firstDigit == '2' || firstDigit == '3' || firstDigit == '4')
        {
            // İkinci hane 0 olamaz
            return secondDigit != '0' && char.IsDigit(secondDigit);
        }

        return false;
    }

    /// <summary>
    /// Telefon numarasını veritabanı formatına çevirir (sadece rakamlar)
    /// </summary>
    /// <param name="phoneNumber">Formatlanmış telefon numarası</param>
    /// <returns>Sadece rakamlardan oluşan telefon numarası</returns>
    public static string? ToDbFormat(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        var formatted = FormatPhoneNumber(phoneNumber);
        if (formatted == null)
            return null;

        return Regex.Replace(formatted, @"[^\d]", "");
    }

    /// <summary>
    /// Telefon numarasını görüntüleme formatına çevirir
    /// </summary>
    /// <param name="phoneNumber">Ham telefon numarası</param>
    /// <returns>Formatlanmış telefon numarası</returns>
    public static string? ToDisplayFormat(string? phoneNumber)
    {
        return FormatPhoneNumber(phoneNumber);
    }

    /// <summary>
    /// Telefon numarasının cep telefonu olup olmadığını kontrol eder
    /// </summary>
    /// <param name="phoneNumber">Telefon numarası</param>
    /// <returns>Cep telefonu ise true</returns>
    public static bool IsMobilePhone(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var digitsOnly = Regex.Replace(phoneNumber, @"[^\d]", "");

        // +90 ile başlıyorsa kaldır
        if (digitsOnly.StartsWith("90") && digitsOnly.Length == 12)
        {
            digitsOnly = digitsOnly.Substring(2);
        }

        // 0 ile başlıyorsa kaldır
        if (digitsOnly.StartsWith("0") && digitsOnly.Length == 11)
        {
            digitsOnly = digitsOnly.Substring(1);
        }

        // 10 haneli ve 5 ile başlıyorsa cep telefonu
        return digitsOnly.Length == 10 && digitsOnly[0] == '5';
    }
}
