using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using OfficeOpenXml;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.ViewModels;
using PushDashboard.Hubs;
using PushDashboard.Helpers;
using System.Text;

namespace PushDashboard.Services;

public class CustomerImportService : ICustomerImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerImportService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IHubContext<CustomerImportHub> _hubContext;
    private readonly IUserNotificationService _userNotificationService;

    public CustomerImportService(
        ApplicationDbContext context,
        ILogger<CustomerImportService> logger,
        IWebHostEnvironment environment,
        IHubContext<CustomerImportHub> hubContext,
        IUserNotificationService userNotificationService)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
        _hubContext = hubContext;
        _userNotificationService = userNotificationService;

        // EPPlus lisans ayarı
        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
    }

    public async Task<int> CreateImportJobAsync(Guid companyId, string userId, IFormFile file)
    {
        // Dosya validasyonları
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("Dosya seçilmedi.");
        }

        if (!IsValidExcelFile(file))
        {
            throw new InvalidOperationException("Geçersiz dosya formatı. Sadece .xlsx dosyaları desteklenir.");
        }

        if (file.Length > 10 * 1024 * 1024) // 10MB limit
        {
            throw new InvalidOperationException("Dosya boyutu 10MB'dan büyük olamaz.");
        }

        // Dosyayı kaydet
        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "customer-imports");
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Excel dosyasını hızlıca kontrol et ve satır sayısını al
        int totalRows;
        try
        {
            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                File.Delete(filePath);
                throw new InvalidOperationException("Excel dosyasında çalışma sayfası bulunamadı.");
            }

            totalRows = worksheet.Dimension?.Rows ?? 0;
            if (totalRows <= 1) // Header row
            {
                File.Delete(filePath);
                throw new InvalidOperationException("Excel dosyasında veri bulunamadı.");
            }

            totalRows--; // Header row'u çıkar
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            File.Delete(filePath);
            _logger.LogError(ex, "Excel dosyası okunurken hata oluştu");
            throw new InvalidOperationException("Excel dosyası okunamadı. Dosya formatını kontrol edin.", ex);
        }

        try
        {
            // Import job'ı oluştur
            var importJob = new CustomerImportJob
            {
                CompanyId = companyId,
                FileName = file.FileName,
                FilePath = filePath,
                Status = "Bekliyor",
                TotalRows = totalRows,
                CreatedByUserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.CustomerImportJobs.Add(importJob);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer import job created with ID {JobId} for company {CompanyId}",
                importJob.Id, companyId);

            return importJob.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer import job for company {CompanyId}", companyId);
            throw new InvalidOperationException("Import işlemi oluşturulurken hata oluştu: " + ex.Message, ex);
        }
    }

    public async Task<CustomerImportProgressViewModel?> GetImportProgressAsync(int jobId, Guid companyId)
    {
        try
        {
            var job = await _context.CustomerImportJobs
                .Where(j => j.Id == jobId && j.CompanyId == companyId)
                .FirstOrDefaultAsync();

            if (job == null)
                return null;

            return new CustomerImportProgressViewModel
            {
                JobId = job.Id,
                FileName = job.FileName,
                Status = job.Status,
                TotalRows = job.TotalRows,
                ProcessedRows = job.ProcessedRows,
                SuccessCount = job.SuccessCount,
                ErrorCount = job.ErrorCount,
                ErrorDetails = job.ErrorDetails,
                ProgressPercentage = job.ProgressPercentage,
                StatusBadgeClass = job.StatusBadgeClass,
                IsCompleted = job.Status == "Tamamlandı" || job.Status == "Hata",
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting import progress for job {JobId}", jobId);
            return null;
        }
    }

    public async Task<List<CustomerImportJobViewModel>> GetImportHistoryAsync(Guid companyId, int limit = 10)
    {
        try
        {
            var jobs = await _context.CustomerImportJobs
                .Where(j => j.CompanyId == companyId)
                .OrderByDescending(j => j.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return jobs.Select(CustomerImportJobViewModel.FromImportJob).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting import history for company {CompanyId}", companyId);
            return new List<CustomerImportJobViewModel>();
        }
    }

    public byte[] GenerateExcelTemplate()
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Müşteriler");

        // Header'ları ekle
        var headers = new[]
        {
            "Ad*", "Soyad*", "E-posta*", "Telefon", "Cep Telefonu", "Aktif (Evet/Hayır)",
            "E-posta İzni (Evet/Hayır)", "SMS İzni (Evet/Hayır)", "Doğum Tarihi (GG.AA.YYYY)",
            "Cinsiyet (1=Erkek, 2=Kadın)", "Şehir", "İlçe", "Müşteri Kodu", "Üyelik Tipi",
            "Puan Bakiyesi", "Kredi Limiti", "KVKK Onayı (Evet/Hayır)", "Üyelik Sözleşmesi Onayı (Evet/Hayır)",
            "Meslek", "Eğitim Seviyesi"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // Örnek veri ekle
        worksheet.Cells[2, 1].Value = "Ahmet";
        worksheet.Cells[2, 2].Value = "Yılmaz";
        worksheet.Cells[2, 3].Value = "ahmet@example.com";
        worksheet.Cells[2, 4].Value = "02121234567";
        worksheet.Cells[2, 5].Value = "05551234567";
        worksheet.Cells[2, 6].Value = "Evet";
        worksheet.Cells[2, 7].Value = "Evet";
        worksheet.Cells[2, 8].Value = "Evet";
        worksheet.Cells[2, 9].Value = "15.06.1985";
        worksheet.Cells[2, 10].Value = "1";
        worksheet.Cells[2, 11].Value = "İstanbul";
        worksheet.Cells[2, 12].Value = "Kadıköy";
        worksheet.Cells[2, 13].Value = "CUST001";
        worksheet.Cells[2, 14].Value = "Premium";
        worksheet.Cells[2, 15].Value = "100";
        worksheet.Cells[2, 16].Value = "5000";
        worksheet.Cells[2, 17].Value = "Evet";
        worksheet.Cells[2, 18].Value = "Evet";
        worksheet.Cells[2, 19].Value = "Mühendis";
        worksheet.Cells[2, 20].Value = "Üniversite";

        // Sütun genişliklerini ayarla
        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    public async Task<int> CreateCustomerAsync(Guid companyId, CreateCustomerViewModel model)
    {
        // Detaylı validasyonlar
        var validationResult = await ValidateCustomerModelAsync(companyId, model);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException(validationResult.ErrorMessage);
        }

        // Telefon numaralarını formatla
        var formattedPhone = PhoneNumberHelper.FormatPhoneNumber(model.Phone);
        var formattedMobilePhone = PhoneNumberHelper.FormatPhoneNumber(model.MobilePhone);

        // Telefon numarası validasyonu
        if (!string.IsNullOrEmpty(model.Phone) && formattedPhone == null)
        {
            throw new InvalidOperationException("Geçersiz telefon numarası formatı. Lütfen geçerli bir Türkiye telefon numarası giriniz.");
        }

        if (!string.IsNullOrEmpty(model.MobilePhone) && formattedMobilePhone == null)
        {
            throw new InvalidOperationException("Geçersiz cep telefonu numarası formatı. Lütfen geçerli bir Türkiye cep telefonu numarası giriniz.");
        }

        // ExternalId oluştur
        var externalId = await GetNextExternalIdAsync(companyId);

        var customer = new Customer
        {
            CompanyId = companyId,
            ExternalId = externalId,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = formattedPhone,
            MobilePhone = formattedMobilePhone,
            IsActive = model.IsActive,
            EmailPermission = model.EmailPermission,
            SmsPermission = model.SmsPermission,
            BirthDate = model.BirthDate,
            GenderId = model.GenderId,
            City = model.City?.Trim(),
            District = model.District?.Trim(),
            CustomerCode = model.CustomerCode?.Trim(),
            MembershipType = model.MembershipType?.Trim(),
            MembershipDate = model.MembershipDate,
            PointBalance = model.PointBalance,
            CreditLimit = model.CreditLimit,
            KvkkApproval = model.KvkkApproval,
            MembershipAgreementApproval = model.MembershipAgreementApproval,
            Profession = model.Profession?.Trim(),
            EducationLevel = model.EducationLevel?.Trim(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            LastUpdateDate = DateTime.Now
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New customer created with ID {CustomerId} for company {CompanyId}",
            customer.Id,
            companyId);

        return customer.Id;
    }

    public async Task ProcessImportJobAsync(int jobId)
    {
        CustomerImportJob? job = null;
        try
        {
            // Job'ı al ve durumunu güncelle
            job = await _context.CustomerImportJobs.FindAsync(jobId);
            if (job == null)
            {
                _logger.LogError("Import job {JobId} not found", jobId);
                return;
            }

            job.Status = "İşleniyor";
            job.StartedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // SignalR ile durum güncelleme gönder
            await SendProgressUpdate(job);

            _logger.LogInformation("Starting import job {JobId} for company {CompanyId}", jobId, job.CompanyId);

            // Excel dosyasını işle
            await ProcessExcelFileAsync(job);

            // İşlem tamamlandı
            job.Status = "Tamamlandı";
            job.CompletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // SignalR ile tamamlanma durumu gönder
            await SendProgressUpdate(job);

            // Bildirim gönder
            await _userNotificationService.SendNotificationAsync(
                job.CreatedByUserId,
                "İçe Aktarma Tamamlandı",
                $"{job.FileName} dosyası başarıyla işlendi. Başarılı: {job.SuccessCount}, Hata: {job.ErrorCount}",
                NotificationType.Success,
                "/Customer", // Müşteri listesine yönlendir
                job.CompanyId);

            _logger.LogInformation("Import job {JobId} completed successfully. Success: {SuccessCount}, Errors: {ErrorCount}",
                jobId, job.SuccessCount, job.ErrorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing import job {JobId}", jobId);


                // Hata bildirimi gönder
                if (job.CreatedByUserId != null)
                {
                    await _userNotificationService.SendNotificationAsync(
                        job.CreatedByUserId,
                        "İçe Aktarma Hatası",
                        $"İşlem sırasında hata oluştu: {ex.Message}",
                        NotificationType.Error,
                        "/Customer",
                        job.CompanyId);
                }
            if (job != null)
            {
                job.Status = "Hata";
                job.ErrorDetails = ex.Message;
                job.CompletedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // SignalR ile hata durumu gönder
                await SendProgressUpdate(job);
            }
        }
        finally
        {
            // Dosyayı temizle
            if (job != null && File.Exists(job.FilePath))
            {
                try
                {
                    File.Delete(job.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete import file {FilePath}", job.FilePath);
                }
            }
        }
    }

    private async Task ProcessExcelFileAsync(CustomerImportJob job)
    {
        using var package = new ExcelPackage(new FileInfo(job.FilePath));
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
            throw new InvalidOperationException("Excel dosyasında çalışma sayfası bulunamadı.");

        var errors = new List<string>();
        var batchSize = 100;
        var currentBatch = new List<Customer>();

        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            try
            {
                var customer = await ParseCustomerFromRowAsync(worksheet, row, job.CompanyId);
                if (customer != null)
                {
                    currentBatch.Add(customer);

                    if (currentBatch.Count >= batchSize)
                    {
                        await SaveBatchAsync(currentBatch, job);
                        currentBatch.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Satır {row}: {ex.Message}");
                job.ErrorCount++;
            }

            job.ProcessedRows++;

            // Her 50 satırda bir progress güncelle
            if (job.ProcessedRows % 50 == 0)
            {
                await _context.SaveChangesAsync();
                // SignalR ile progress güncelleme gönder
                await SendProgressUpdate(job);
            }
        }

        // Kalan batch'i kaydet
        if (currentBatch.Any())
        {
            await SaveBatchAsync(currentBatch, job);
        }

        // Hataları kaydet
        if (errors.Any())
        {
            job.ErrorDetails = string.Join("\n", errors.Take(100)); // İlk 100 hatayı kaydet
        }
    }

    private async Task SaveBatchAsync(List<Customer> customers, CustomerImportJob job)
    {
        var errors = new List<string>();
        var successCount = 0;

        // Her müşteriyi tek tek kaydetmeye çalış
        foreach (var customer in customers)
        {
            try
            {
                // Önce aynı email'e sahip müşteri var mı kontrol et
                var existingCustomer = await _context.Customers
                    .Where(c => c.CompanyId == customer.CompanyId && c.Email == customer.Email)
                    .FirstOrDefaultAsync();

                if (existingCustomer != null)
                {
                    errors.Add($"E-posta zaten mevcut: {customer.Email}");
                    job.ErrorCount++;
                    continue;
                }

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                successCount++;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                var errorMessage = GetDatabaseErrorMessage(ex, customer);
                errors.Add(errorMessage);
                job.ErrorCount++;

                // Entity'yi context'ten kaldır
                _context.Entry(customer).State = EntityState.Detached;

                _logger.LogWarning(ex, "Database error while saving customer {Email}: {Error}", customer.Email, errorMessage);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Müşteri kaydedilirken hata: {customer.Email} - {ex.Message}";
                errors.Add(errorMessage);
                job.ErrorCount++;

                // Entity'yi context'ten kaldır
                _context.Entry(customer).State = EntityState.Detached;

                _logger.LogError(ex, "Unexpected error while saving customer {Email}", customer.Email);
            }
        }

        job.SuccessCount += successCount;

        // Hataları job'a ekle
        if (errors.Any())
        {
            var existingErrors = string.IsNullOrEmpty(job.ErrorDetails) ? new List<string>() : job.ErrorDetails.Split('\n').ToList();
            existingErrors.AddRange(errors);
            job.ErrorDetails = string.Join("\n", existingErrors.Take(200)); // İlk 200 hatayı kaydet
        }
    }

    private async Task<Customer?> ParseCustomerFromRowAsync(ExcelWorksheet worksheet, int row, Guid companyId)
    {
        // Zorunlu alanları kontrol et
        var firstName = worksheet.Cells[row, 1].Text?.Trim();
        var lastName = worksheet.Cells[row, 2].Text?.Trim();
        var email = worksheet.Cells[row, 3].Text?.Trim();

        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email))
        {
            throw new InvalidOperationException("Ad, soyad ve e-posta alanları zorunludur");
        }

        if (!IsValidEmail(email))
        {
            throw new InvalidOperationException($"Geçersiz e-posta adresi: {email}");
        }

        var customer = new Customer
        {
            CompanyId = companyId,
            ExternalId = await GetNextExternalIdAsync(companyId), // Unique ExternalId oluştur
            FirstName = firstName,
            LastName = lastName,
            Email = email.ToLower(),
            Phone = worksheet.Cells[row, 4].Text?.Trim(),
            MobilePhone = worksheet.Cells[row, 5].Text?.Trim(),
            IsActive = ParseBooleanValue(worksheet.Cells[row, 6].Text, true),
            EmailPermission = ParseBooleanValue(worksheet.Cells[row, 7].Text, false),
            SmsPermission = ParseBooleanValue(worksheet.Cells[row, 8].Text, false),
            BirthDate = ParseDateValue(worksheet.Cells[row, 9].Text),
            GenderId = ParseIntValue(worksheet.Cells[row, 10].Text),
            City = worksheet.Cells[row, 11].Text?.Trim(),
            District = worksheet.Cells[row, 12].Text?.Trim(),
            CustomerCode = worksheet.Cells[row, 13].Text?.Trim(),
            MembershipType = worksheet.Cells[row, 14].Text?.Trim(),
            PointBalance = ParseIntValue(worksheet.Cells[row, 15].Text) ?? 0,
            CreditLimit = ParseDecimalValue(worksheet.Cells[row, 16].Text) ?? 0,
            KvkkApproval = ParseBooleanValue(worksheet.Cells[row, 17].Text, false),
            MembershipAgreementApproval = ParseBooleanValue(worksheet.Cells[row, 18].Text, false),
            Profession = worksheet.Cells[row, 19].Text?.Trim(),
            EducationLevel = worksheet.Cells[row, 20].Text?.Trim(),
            MembershipDate = DateTime.Now,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            LastUpdateDate = DateTime.Now
        };

        return customer;
    }

    private static bool ParseBooleanValue(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        value = value.Trim().ToLowerInvariant();
        return value == "evet" || value == "yes" || value == "true" || value == "1";
    }

    private static DateTime? ParseDateValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParse(value, out var date))
            return date;

        return null;
    }

    private static int? ParseIntValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value, out var intValue))
            return intValue;

        return null;
    }

    private static decimal? ParseDecimalValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (decimal.TryParse(value, out var decimalValue))
            return decimalValue;

        return null;
    }

    private async Task<int> GetNextExternalIdAsync(Guid companyId)
    {
        // Excel'den eklenen müşteriler için negatif ExternalId kullan
        // Bu sayede e-ticaret entegrasyonlarından gelen pozitif ID'lerle çakışmaz
        var lastExternalId = await _context.Customers
            .Where(c => c.CompanyId == companyId && c.ExternalId < 0)
            .OrderBy(c => c.ExternalId)
            .Select(c => c.ExternalId)
            .FirstOrDefaultAsync();

        return lastExternalId == 0 ? -1 : lastExternalId - 1;
    }

    private async Task<(bool IsValid, string ErrorMessage)> ValidateCustomerModelAsync(Guid companyId, CreateCustomerViewModel model)
    {
        // Email validasyonu
        if (string.IsNullOrWhiteSpace(model.Email))
        {
            return (false, "E-posta adresi gereklidir.");
        }

        if (!IsValidEmail(model.Email))
        {
            return (false, "Geçerli bir e-posta adresi giriniz.");
        }

        // Email benzersizlik kontrolü
        var existingCustomer = await _context.Customers
            .Where(c => c.CompanyId == companyId && c.Email.ToLower() == model.Email.ToLower())
            .FirstOrDefaultAsync();

        if (existingCustomer != null)
        {
            return (false, "Bu e-posta adresi ile kayıtlı müşteri zaten mevcut.");
        }

        // Ad validasyonu
        if (string.IsNullOrWhiteSpace(model.FirstName))
        {
            return (false, "Ad gereklidir.");
        }

        if (model.FirstName.Trim().Length < 2)
        {
            return (false, "Ad en az 2 karakter olmalıdır.");
        }

        // Soyad validasyonu
        if (string.IsNullOrWhiteSpace(model.LastName))
        {
            return (false, "Soyad gereklidir.");
        }

        if (model.LastName.Trim().Length < 2)
        {
            return (false, "Soyad en az 2 karakter olmalıdır.");
        }

        // Müşteri kodu benzersizlik kontrolü
        if (!string.IsNullOrWhiteSpace(model.CustomerCode))
        {
            var existingCustomerCode = await _context.Customers
                .Where(c => c.CompanyId == companyId && c.CustomerCode == model.CustomerCode.Trim())
                .FirstOrDefaultAsync();

            if (existingCustomerCode != null)
            {
                return (false, "Bu müşteri kodu zaten kullanımda.");
            }
        }

        // Doğum tarihi validasyonu
        if (model.BirthDate.HasValue)
        {
            var today = DateTime.Today;
            var age = today.Year - model.BirthDate.Value.Year;
            if (model.BirthDate.Value.Date > today.AddYears(-age)) age--;

            if (age < 0 || age > 150)
            {
                return (false, "Geçerli bir doğum tarihi giriniz.");
            }
        }

        // Puan bakiyesi validasyonu
        if (model.PointBalance < 0)
        {
            return (false, "Puan bakiyesi negatif olamaz.");
        }

        // Kredi limiti validasyonu
        if (model.CreditLimit < 0)
        {
            return (false, "Kredi limiti negatif olamaz.");
        }

        return (true, string.Empty);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task SendProgressUpdate(CustomerImportJob job)
    {
        try
        {
            var progressData = new
            {
                jobId = job.Id,
                fileName = job.FileName,
                status = job.Status,
                totalRows = job.TotalRows,
                processedRows = job.ProcessedRows,
                successCount = job.SuccessCount,
                errorCount = job.ErrorCount,
                errorDetails = job.ErrorDetails,
                progressPercentage = job.ProgressPercentage,
                statusBadgeClass = job.StatusBadgeClass,
                isCompleted = job.Status == "Tamamlandı" || job.Status == "Hata",
                startedAt = job.StartedAt,
                completedAt = job.CompletedAt,
                formattedStartedAt = job.StartedAt?.ToString("dd.MM.yyyy HH:mm"),
                formattedCompletedAt = job.CompletedAt?.ToString("dd.MM.yyyy HH:mm"),
                formattedDuration = GetFormattedDuration(job.StartedAt, job.CompletedAt)
            };

            var groupName = $"Company_{job.CompanyId}";
            await _hubContext.Clients.Group(groupName).SendAsync("ImportProgressUpdate", progressData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending progress update for job {JobId}", job.Id);
        }
    }

    private static string GetDatabaseErrorMessage(Microsoft.EntityFrameworkCore.DbUpdateException ex, Customer customer)
    {
        var innerException = ex.InnerException?.Message ?? ex.Message;

        // PostgreSQL constraint hatalarını handle et
        if (innerException.Contains("IX_Customers_Email_CompanyId"))
        {
            return $"E-posta zaten mevcut: {customer.Email}";
        }
        else if (innerException.Contains("IX_Customers_ExternalId_CompanyId"))
        {
            return $"Müşteri kodu zaten mevcut: {customer.CustomerCode ?? customer.Email}";
        }
        else if (innerException.Contains("duplicate key"))
        {
            return $"Müşteri bilgileri zaten mevcut: {customer.Email}";
        }
        else if (innerException.Contains("violates foreign key constraint"))
        {
            return $"Geçersiz referans verisi: {customer.Email}";
        }
        else if (innerException.Contains("violates check constraint"))
        {
            return $"Geçersiz veri formatı: {customer.Email}";
        }
        else if (innerException.Contains("violates not-null constraint"))
        {
            return $"Zorunlu alan eksik: {customer.Email}";
        }
        else
        {
            return $"Veritabanı hatası: {customer.Email} - {innerException}";
        }
    }

    private static string? GetFormattedDuration(DateTime? startedAt, DateTime? completedAt)
    {
        if (startedAt == null) return null;

        var endTime = completedAt ?? DateTime.Now;
        var duration = endTime - startedAt.Value;

        if (duration.TotalMinutes < 1)
            return $"{duration.Seconds} saniye";
        else if (duration.TotalHours < 1)
            return $"{duration.Minutes} dakika {duration.Seconds} saniye";
        else
            return $"{duration.Hours} saat {duration.Minutes} dakika";
    }

    private static bool IsValidExcelFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(extension);
    }
}
