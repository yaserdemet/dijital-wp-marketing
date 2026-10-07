using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;

namespace PushDashboard.Controllers;

public class UnsubscribeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UnsubscribeController> _logger;

    public UnsubscribeController(ApplicationDbContext context, ILogger<UnsubscribeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index(string? phone)
    {
        ViewBag.Phone = phone;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            TempData["Error"] = "Lütfen geçerli bir telefon numarası giriniz.";
            return RedirectToAction(nameof(Index));
        }

        // Clean phone number format
        var cleanPhone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (cleanPhone.StartsWith("0")) cleanPhone = cleanPhone.Substring(1);
        if (cleanPhone.StartsWith("90")) cleanPhone = cleanPhone.Substring(2);

        // Find customer
        // We might have duplicates, so we disable permission for ALL matching records
        var customers = await _context.Customers
            .Where(c => c.Phone != null && (c.Phone.Contains(cleanPhone) || c.MobilePhone.Contains(cleanPhone)))
            .ToListAsync();

        if (!customers.Any())
        {
            // Even if not found, simulate success for security/privacy
             TempData["Success"] = "İletişim tercihleriniz başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var customer in customers)
        {
            customer.SmsPermission = false;
            customer.EmailPermission = false;
            customer.IsNewsletterSubscribed = false;
            // Also disable WhatsApp specific permission if you have one? 
            // Currently Customer model only has SmsPermission, usually covers both.
            customer.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Unsubscribed {Count} customers with phone ending in {Phone}", customers.Count, cleanPhone);

        TempData["Success"] = "İletişim tercihleriniz başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }
}
