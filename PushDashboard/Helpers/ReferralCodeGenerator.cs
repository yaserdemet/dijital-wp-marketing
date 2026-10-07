using System.Linq;
using System.Security.Cryptography;

namespace PushDashboard.Helpers;

/// <summary>
/// Referans kodu üretimi için static helper sınıfı.
/// </summary>
public static class ReferralCodeGenerator
{
    private const int MinCode = 100000;
    private const int MaxCode = 999999;
    private const int CodeLength = 6;

    /// <summary>
    /// 6 haneli (100000-999999 arası) rastgele referans kodu üretir. Üretilen kod sadece rakamlardan oluşur ve kalıcı bir referans ID'dir .
    /// </summary>
    public static string Generate()
    {
        using var rng = RandomNumberGenerator.Create();

        var randomBytes = new byte[4];
        rng.GetBytes(randomBytes);

        var randomValue = BitConverter.ToUInt32(randomBytes, 0);

        var codeNumber = MinCode + (int)(randomValue % (MaxCode - MinCode + 1));

        return codeNumber.ToString("D6");
    }

    /// <summary>
    /// Referans kodunun geçerli formatını kontrol eder. Geçerli ise true, değilse false döner.
    /// </summary>
    public static bool IsValid(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        if (code.Length != CodeLength)
            return false;

        if (!code.All(char.IsDigit))
            return false;

        if (!int.TryParse(code, out int numericCode))
            return false;

        return numericCode >= MinCode && numericCode <= MaxCode;
    }
}
