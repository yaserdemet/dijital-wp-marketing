using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using PushDashboard.Models.DTOs;
using System.Text.Json;

namespace PushDashboard.Services;

public class PayTRService : IPayTRService
{
    private readonly HttpClient _httpClient;
    private readonly PayTRSettings _settings;
    private readonly ILogger<PayTRService> _logger;

    public PayTRService(HttpClient httpClient, IOptions<PayTRSettings> settings, ILogger<PayTRService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PayTRPaymentResult> ProcessPaymentAsync(PayTRPaymentRequest request)
    {
        try
        {
            var merchantOid = request.MerchantOid;
            var paymentAmountKurus = (int)(request.Amount);
            var currency = "TL";
            var noInstallment = "1";
            var maxInstallment = "0";
            var testMode = _settings.TestMode ? "1" : "0";
            var storeCard = request.StoreCard ? "1" : "0";

            // PayTR token oluşturma
            // Hash: merchant_id + user_ip + merchant_oid + email + payment_amount + 
            //       payment_type + installment_count + currency + test_mode + non_3d + merchant_salt
            var paymentType = "card";
            var installmentCount = "0";
            var non3d = "0"; // 3D Secure ile ödeme

            var hashStr = $"{_settings.MerchantId}{request.UserIp}{merchantOid}{request.Email}{paymentAmountKurus}{paymentType}{installmentCount}{currency}{testMode}{non3d}";
            
            _logger.LogError("PayTR Hash String Components: MerchantId={MerchantId}, UserIp={UserIp}, MerchantOid={MerchantOid}, Email={Email}, PaymentAmount={PaymentAmount}, PaymentType={PaymentType}, InstallmentCount={InstallmentCount}, Currency={Currency}, TestMode={TestMode}, Non3d={Non3d}", 
                _settings.MerchantId, request.UserIp, merchantOid, request.Email, paymentAmountKurus, paymentType, installmentCount, currency, testMode, non3d);
            
            var paytrToken = GenerateHmacToken(hashStr, _settings.MerchantKey, _settings.MerchantSalt);
            _logger.LogError("Generated PayTR Token: {Token} (HashStr: {HashStr})", paytrToken, hashStr);

            var formData = new Dictionary<string, string>
            {
                { "merchant_id", _settings.MerchantId },
                { "user_ip", request.UserIp },
                { "merchant_oid", merchantOid },
                { "email", request.Email },
                { "payment_type", paymentType },
                { "payment_amount", paymentAmountKurus.ToString() },
                { "currency", currency },
                { "test_mode", testMode },
                { "non_3d", non3d },
                { "merchant_ok_url", _settings.MerchantOkUrl + $"?oid={merchantOid}" },
                { "merchant_fail_url", _settings.MerchantFailUrl + $"?oid={merchantOid}" },
                { "cc_owner", request.CardHolderName },
                { "card_number", request.CardNumber.Replace(" ", "") },
                { "expiry_month", request.ExpiryMonth },
                { "expiry_year", request.ExpiryYear },
                { "cvv", request.Cvv },
                { "no_installment", noInstallment },
                { "max_installment", maxInstallment },
                { "installment_count", installmentCount },
                { "paytr_token", paytrToken },
                { "debug_on", "1" },
                { "store_card", storeCard },
                { "user_name", request.CardHolderName },
                { "user_address", "test adres" },
                { "user_phone", "05555555555" },
                { "lang", "tr" },
                { "user_basket", JsonSerializer.Serialize(new object[][] { new object[] { "Kredi Yukleme", request.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), 1 } }) }
            };

            // Eğer kullanıcının mevcut utoken'ı varsa ekle
            if (!string.IsNullOrEmpty(request.Utoken))
            {
                formData["utoken"] = request.Utoken;
            }

            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/odeme", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("PayTR Payment Response for {OrderId}: {Response}", merchantOid, responseBody);

            // PayTR yanıtını parse et
            var result = ParsePaymentResponse(responseBody, merchantOid);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayTR payment processing error");
            return new PayTRPaymentResult
            {
                Success = false,
                ErrorMessage = "Ödeme işlemi sırasında bir hata oluştu."
            };
        }
    }

    public async Task<PayTRPaymentResult> ProcessSavedCardPaymentAsync(PayTRSavedCardPaymentRequest request)
    {
        try
        {
            var merchantOid = request.MerchantOid;
            var paymentAmountKurus = (int)(request.Amount);
            var currency = "TL";
            var noInstallment = "1";
            var maxInstallment = "0";
            var testMode = _settings.TestMode ? "1" : "0";

            var paymentType = "card";
            var installmentCount = "0";
            var non3d = "0";

            var hashStr = $"{_settings.MerchantId}{request.UserIp}{merchantOid}{request.Email}{paymentAmountKurus}{paymentType}{installmentCount}{currency}{testMode}{non3d}";
            var paytrToken = GenerateHmacToken(hashStr, _settings.MerchantKey, _settings.MerchantSalt);

            var formData = new Dictionary<string, string>
            {
                { "merchant_id", _settings.MerchantId },
                { "user_ip", request.UserIp },
                { "merchant_oid", merchantOid },
                { "email", request.Email },
                { "payment_type", paymentType },
                { "payment_amount", paymentAmountKurus.ToString() },
                { "currency", currency },
                { "test_mode", testMode },
                { "non_3d", non3d },
                { "merchant_ok_url", _settings.MerchantOkUrl + $"?oid={merchantOid}" },
                { "merchant_fail_url", _settings.MerchantFailUrl + $"?oid={merchantOid}" },
                { "no_installment", noInstallment },
                { "max_installment", maxInstallment },
                { "installment_count", installmentCount },
                { "paytr_token", paytrToken },
                { "debug_on", "1" },
                { "utoken", request.Utoken },
                { "ctoken", request.Ctoken },
                { "user_name", request.Username },
                { "user_address", "test adres" },
                { "user_phone", "05555555555" },
                { "lang", "tr" },
                { "user_basket", JsonSerializer.Serialize(new object[][] { new object[] { "Kredi Yukleme", request.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), 1 } }) }
            };

            // CVV gerekiyorsa ekle
            if (!string.IsNullOrEmpty(request.Cvv))
            {
                formData["cvv"] = request.Cvv;
            }

            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/odeme", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("PayTR Saved Card Payment Response for {OrderId}: {Response}", merchantOid, responseBody);

            var result = ParsePaymentResponse(responseBody, merchantOid);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayTR saved card payment processing error");
            return new PayTRPaymentResult
            {
                Success = false,
                ErrorMessage = "Kayıtlı kartla ödeme işlemi sırasında bir hata oluştu."
            };
        }
    }

    public async Task<List<PayTRCard>> ListSavedCardsAsync(string utoken)
    {
        try
        {
            // Token: HMACSHA256(utoken + merchant_salt, merchant_key)
            var hashStr = utoken;
            var paytrToken = GenerateHmacToken(hashStr, _settings.MerchantKey, _settings.MerchantSalt);

            var formData = new Dictionary<string, string>
            {
                { "merchant_id", _settings.MerchantId },
                { "utoken", utoken },
                { "paytr_token", paytrToken }
            };

            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/odeme/capi/list", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("PayTR Card List Response: {Response}", responseBody);

            var cards = ParseCardListResponse(responseBody);
            return cards;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayTR card listing error");
            return new List<PayTRCard>();
        }
    }

    public async Task<bool> DeleteSavedCardAsync(string utoken, string ctoken)
    {
        try
        {
            // Token: HMACSHA256(ctoken + utoken + merchant_salt, merchant_key)
            var hashStr = $"{ctoken}{utoken}";
            var paytrToken = GenerateHmacToken(hashStr, _settings.MerchantKey, _settings.MerchantSalt);

            var formData = new Dictionary<string, string>
            {
                { "merchant_id", _settings.MerchantId },
                { "ctoken", ctoken },
                { "utoken", utoken },
                { "paytr_token", paytrToken }
            };

            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/odeme/capi/delete", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("PayTR Card Delete Response: {Response}", responseBody);

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("status", out var statusProp))
            {
                return statusProp.GetString() == "success";
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayTR card deletion error");
            return false;
        }
    }

    public bool ValidateCallbackHash(PayTRCallbackData callbackData)
    {
        try
        {
            // PayTR callback doğrulama:
            // hash = base64(HMAC-SHA256(merchant_key, merchant_oid + merchant_salt + status + total_amount))
            //var hashStr =  $"{_settings.MerchantId}{callbackData}{merchantOid}{request.Email}{paymentAmountKurus}{paymentType}{installmentCount}{currency}{testMode}{non3d}";
            var hashStr = $"{callbackData.merchant_oid}{_settings.MerchantSalt}{callbackData.status}{callbackData.total_amount}";
            var expectedHash = GenerateHmacToken(hashStr, _settings.MerchantKey);

            return expectedHash == callbackData.hash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayTR callback hash validation error");
            return false;
        }
    }

    private string GenerateHmacToken(string data, string key, string? salt = null)
    {
        var dataToHash = salt != null ? data + salt : data;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
        return Convert.ToBase64String(hash);
    }

    private PayTRPaymentResult ParsePaymentResponse(string responseBody, string merchantOid)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;

            if (status == "success")
            {
                var utoken = root.TryGetProperty("utoken", out var utokenProp) ? utokenProp.GetString() : null;

                return new PayTRPaymentResult
                {
                    Success = true,
                    OrderId = merchantOid,
                    Status = "success",
                    Utoken = utoken
                };
            }
            else
            {
                var errMsg = root.TryGetProperty("err_msg", out var errProp) ? errProp.GetString() : "Bilinmeyen hata";
                var reason = root.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() : null;

                return new PayTRPaymentResult
                {
                    Success = false,
                    OrderId = merchantOid,
                    Status = "failed",
                    ErrorMessage = reason ?? errMsg
                };
            }
        }

        catch (Exception ex)
        {
            // Eğer JSON parse edilemezse ve HTML içeriyorsa, 3D Secure yanıtı olabilir
            if (!string.IsNullOrEmpty(responseBody) && (responseBody.Trim().StartsWith("<") || responseBody.Contains("<!DOCTYPE html")))
            {
                _logger.LogInformation("PayTR returned HTML content (likely 3D Secure redirecion).");
                return new PayTRPaymentResult
                {
                    Success = true,
                    OrderId = merchantOid,
                    Status = "3d_redirect",
                    HtmlContent = responseBody
                };
            }

            _logger.LogError(ex, "Error parsing PayTR response: {Response}", responseBody);
            return new PayTRPaymentResult
            {
                Success = false,
                OrderId = merchantOid,
                ErrorMessage = "PayTR yanıtı işlenirken hata oluştu."
            };
        }
    }

    private List<PayTRCard> ParseCardListResponse(string responseBody)
    {
        var cards = new List<PayTRCard>();

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // PayTR bazen direkt array döner
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var cardEl in root.EnumerateArray())
                {
                    cards.Add(new PayTRCard
                    {
                        Ctoken = cardEl.TryGetProperty("ctoken", out var ct) ? ct.GetString() ?? "" : "",
                        LastFour = cardEl.TryGetProperty("last_4", out var l4) ? l4.GetString() ?? "" : "",
                        CardBrand = cardEl.TryGetProperty("c_brand", out var cb) ? cb.GetString() ?? "" : "",
                        CardType = cardEl.TryGetProperty("c_type", out var type) ? type.GetString() ?? "" : "",
                        RequireCvv = cardEl.TryGetProperty("require_cvv", out var rcvv) && rcvv.GetString() == "1",
                        BankName = cardEl.TryGetProperty("c_bank", out var bn) ? bn.GetString() : null
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing PayTR card list response: {Response}", responseBody);
        }

        return cards;
    }

    public async Task<PayTRPaymentResult> GetPaymentStatusAsync(string merchantOid)
    {
        try
        {
            var paytrToken = GenerateHmacToken($"{_settings.MerchantId}{merchantOid}", _settings.MerchantKey, _settings.MerchantSalt);
            
            var formData = new Dictionary<string, string>
            {
                { "merchant_id", _settings.MerchantId },
                { "merchant_oid", merchantOid },
                { "paytr_token", paytrToken }
            };
            
            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync("https://www.paytr.com/odeme/durum-sorgu", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("PayTR Status Query Response for {OrderId}: {Response}", merchantOid, responseBody);

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.GetProperty("status").GetString() == "success")
            {
                // Başarılı sorguda total_amount döner, transaction'daki tutarı kontrol etmek isterseniz kullanabilirsiniz.
                // Şimdilik sadece başarılı olduğunu kabul ediyoruz.
                return new PayTRPaymentResult
                {
                    Success = true,
                    OrderId = merchantOid,
                    Status = "success",
                    ErrorMessage = null
                };
            }
            else
            {
                var errMsg = "Bilinmeyen hata";
                if (root.TryGetProperty("err_msg", out var errProp))
                {
                   errMsg = errProp.GetString();
                }
                else if (root.TryGetProperty("err_no", out var errNoProp))
                {
                   errMsg = $"Hata Kodu: {errNoProp.GetString()}";
                }

                return new PayTRPaymentResult
                {
                    Success = false,
                    OrderId = merchantOid,
                    Status = "failed",
                    ErrorMessage = errMsg
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying PayTR status for order {OrderId}", merchantOid);
            return new PayTRPaymentResult
            {
                Success = false,
                OrderId = merchantOid,
                Status = "error",
                ErrorMessage = "Durum sorgulama sırasında hata oluştu."
            };
        }
    }
}
