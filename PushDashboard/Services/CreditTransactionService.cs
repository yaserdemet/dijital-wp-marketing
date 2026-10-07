using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Models.Enums;

namespace PushDashboard.Services;

public class CreditTransactionService : ICreditTransactionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreditTransactionService> _logger;

    public CreditTransactionService(ApplicationDbContext context, ILogger<CreditTransactionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreditTransaction> CreatePaymentTransactionAsync(Guid companyId, decimal amount, string userId, string paytrOrderId)
    {
        var company = await _context.Companies.FindAsync(companyId);
        if (company == null)
            throw new InvalidOperationException($"Company not found: {companyId}");

        var transaction = new CreditTransaction
        {
            CompanyId = companyId,
            TransactionType = CreditTransactionType.Payment,
            Amount = amount,
            BalanceBefore = company.CreditBalance,
            BalanceAfter = company.CreditBalance, // Henüz güncellenmedi, callback'te güncellenecek
            Description = $"₺{amount:N2} kredi yükleme",
            PaytrOrderId = paytrOrderId,
            Status = "Pending",
            CreatedByUserId = userId,
            CreatedAt = DateTime.Now
        };

        _context.CreditTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created pending payment transaction {OrderId} for company {CompanyId}, amount: {Amount}",
            paytrOrderId, companyId, amount);

        return transaction;
    }

    public async Task<bool> CompleteTransactionAsync(string paytrOrderId, string? paytrTransactionId, string? cardLastFour)
    {
        await using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var transaction = await _context.CreditTransactions
                .FirstOrDefaultAsync(ct => ct.PaytrOrderId == paytrOrderId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found for order {OrderId}", paytrOrderId);
                return false;
            }

            if (transaction.Status != "Pending")
            {
                _logger.LogWarning("Transaction {OrderId} is not in Pending status (current: {Status})", paytrOrderId, transaction.Status);
                return true;
            }

            var company = await _context.Companies.FindAsync(transaction.CompanyId);
            if (company == null)
            {
                _logger.LogError("Company not found for transaction {OrderId}", paytrOrderId);
                return false;
            }

            // Bakiyeyi güncelle
            var balanceBefore = company.CreditBalance;
            company.CreditBalance += transaction.Amount;

            // İşlemi güncelle
            transaction.Status = "Success";
            transaction.PaytrTransactionId = paytrTransactionId;
            transaction.CardLastFour = cardLastFour;
            transaction.BalanceBefore = balanceBefore;
            transaction.BalanceAfter = company.CreditBalance;

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            _logger.LogInformation("Completed transaction {OrderId}. Balance: {Before} -> {After}",
                paytrOrderId, balanceBefore, company.CreditBalance);

            return true;
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Error completing transaction {OrderId}", paytrOrderId);
            return false;
        }
    }

    public async Task<bool> FailTransactionAsync(string paytrOrderId, string? errorMessage)
    {
        try
        {
            var transaction = await _context.CreditTransactions
                .FirstOrDefaultAsync(ct => ct.PaytrOrderId == paytrOrderId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found for order {OrderId}", paytrOrderId);
                return false;
            }

            if (transaction.Status != "Pending")
            {
                _logger.LogWarning("Transaction {OrderId} is not in Pending status (current: {Status})", paytrOrderId, transaction.Status);
                return false;
            }

            transaction.Status = "Failed";
            transaction.ErrorMessage = errorMessage;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Failed transaction {OrderId}: {Error}", paytrOrderId, errorMessage);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error failing transaction {OrderId}", paytrOrderId);
            return false;
        }
    }

    public async Task<List<CreditTransaction>> GetTransactionHistoryAsync(Guid companyId, int page = 1, int pageSize = 20)
    {
        return await _context.CreditTransactions
            .Where(ct => ct.CompanyId == companyId)
            .OrderByDescending(ct => ct.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(ct => ct.CreatedByUser)
            .ToListAsync();
    }

    public async Task<int> GetTransactionCountAsync(Guid companyId)
    {
        return await _context.CreditTransactions
            .CountAsync(ct => ct.CompanyId == companyId);
    }
}
