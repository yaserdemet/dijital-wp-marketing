using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models.DTOs;
using PushDashboard.Services;

namespace PushDashboard.Controllers.Api;

/// <summary>
/// PayTR ödeme callback'lerini yakalayan API controller.
/// PayTR, ödeme sonucunu bu endpoint'e POST ile bildirir.
/// Gelen formun hash doğrulaması yapılarak kaçak işlemler engellenir.
/// </summary>
[ApiController]
[Route("api/paytr")]
public class PayTRCallbackController : ControllerBase
{
    private readonly IPayTRService _payTRService;
    private readonly ICreditTransactionService _creditTransactionService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PayTRCallbackController> _logger;

    public PayTRCallbackController(
        IPayTRService payTRService,
        ICreditTransactionService creditTransactionService,
        ApplicationDbContext context,
        ILogger<PayTRCallbackController> logger)
    {
        _payTRService = payTRService;
        _creditTransactionService = creditTransactionService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// PayTR ödeme sonucu callback endpoint'i.
    /// PayTR bu endpoint'e ödeme sonucunu POST ile bildirir.
    /// Hash doğrulaması yaparak güvenlik sağlar.
    /// </summary>
    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromForm] PayTRCallbackData callbackData)
    {
        _logger.LogError("Callback Received: {callbackData}", JsonSerializer.Serialize(callbackData));

        try
        {
            var statusResult = await _payTRService.GetPaymentStatusAsync(callbackData.merchant_oid);
            if (!statusResult.Success)
            {
                _logger.LogError("Error processing PayTR callback for order {OrderId}", callbackData.merchant_oid);
                return StatusCode(500);
            }

            _logger.LogInformation("PayTR callback hash validated for order {OrderId}", callbackData.merchant_oid);

            if (callbackData.status == "success")
            {
                // 2. İşlemi başarılı olarak tamamla ve bakiyeyi güncelle
                var result = await _creditTransactionService.CompleteTransactionAsync(
                    callbackData.merchant_oid,
                    null, // PayTR direkt API'de ayrı transaction ID dönmez
                    null  // Kart son 4 hane callback'te gelmez
                );

                if (!result)
                {
                    _logger.LogError("Failed to complete transaction for order {OrderId}", callbackData.merchant_oid);
                }

                // 3. Eğer utoken döndüyse, şirketin utoken'ını güncelle
                if (!string.IsNullOrEmpty(callbackData.Utoken))
                {
                    await UpdateCompanyUtokenAsync(callbackData.merchant_oid, callbackData.Utoken);
                }
            }
            else
            {
                // İşlemi başarısız olarak işaretle
                await _creditTransactionService.FailTransactionAsync(
                    callbackData.merchant_oid,
                    callbackData.failed_reason_msg ?? $"Ödeme başarısız (Kod: {callbackData.failed_reason_code})"
                );
            }

            // PayTR "OK" yanıtı bekler
            return Ok("OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayTR callback for order {OrderId}", callbackData.merchant_oid);
            return StatusCode(500);
        }
    }

    private async Task UpdateCompanyUtokenAsync(string merchantOid, string utoken)
    {
        try
        {
            var transaction = await _context.CreditTransactions
                .FirstOrDefaultAsync(ct => ct.PaytrOrderId == merchantOid);

            if (transaction != null)
            {
                var company = await _context.Companies.FindAsync(transaction.CompanyId);
                if (company != null)
                {
                    company.PaytrUtoken = utoken;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated utoken for company {CompanyId}", company.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating utoken for order {OrderId}", merchantOid);
        }
    }
}
