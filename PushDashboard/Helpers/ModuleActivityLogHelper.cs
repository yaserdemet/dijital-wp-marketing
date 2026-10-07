using System.Text.Json;
using PushDashboard.Models;

namespace PushDashboard.Helpers;

public static class ModuleActivityLogHelper
{
    public static (string ActivityModule, string Description) GenerateActivityLogForModule(
        int moduleId,
        string moduleName,
        Dictionary<string, object> settings)
    {
        return moduleId switch
        {
            5 => GenerateBirthdayReminderActivityLog(moduleName, settings),
            1 => GenerateSocialProofActivityLog(moduleName, settings),
            2 => GenerateCookieManagementActivityLog(moduleName, settings),
            3 => GenerateFirstOrderActivityLog(moduleName, settings),
            4 => GenerateCommentTransferActivityLog(moduleName, settings),
            6 => GenerateBulkMessagingActivityLog(moduleName, settings),
            7 => GenerateBasketReminderActivityLog(moduleName, settings),
            8 => GenerateOrderStatusActivityLog(moduleName, settings),
            9 => GenerateGiftWheelActivityLog(moduleName, settings),
            10 => GenerateVideoHostingActivityLog(moduleName, settings),
            11 => GenerateExitSurveyActivityLog(moduleName, settings),
            12 => GenerateProductSliderActivityLog(moduleName, settings),
            13 => GenerateStockReminderActivityLog(moduleName, settings),
            15 => GenerateFavoriteProductsActivityLog(moduleName, settings),
            _ => GenerateDefaultActivityLog(moduleName, settings)
        };
    }

    private static (string ActivityModule, string Description) GenerateBirthdayReminderActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.Notification;
        var enabledChannels = new List<string>();
        
        if (settings.ContainsKey("enableEmail") && GetBoolValue(settings["enableEmail"]))
            enabledChannels.Add("Email");
        if (settings.ContainsKey("enableSms") && GetBoolValue(settings["enableSms"]))
            enabledChannels.Add("SMS");
        if (settings.ContainsKey("enableWhatsapp") && GetBoolValue(settings["enableWhatsapp"]))
            enabledChannels.Add("WhatsApp");

        var autoSend = GetBoolValue(settings.GetValueOrDefault("autoSend"));
        var sendTime = settings.ContainsKey("sendTime") ? settings["sendTime"]?.ToString() : "09:00";
        var channelsText = enabledChannels.Any() ? string.Join(", ", enabledChannels) : "Yok";

        var description = $"Doğum günü hatırlatma ayarları güncellendi (Otomatik: {(autoSend ? "Açık" : "Kapalı")}, Saat: {sendTime}, Kanallar: {channelsText})";

        return (activityModule, description);
    }

    private static (string ActivityModule, string Description) GenerateSocialProofActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.SocialProof;
        var enabled = GetBoolValue(settings.GetValueOrDefault("enabled"));
        var description = $"{moduleName} modülü ayarları güncellendi (Durum: {(enabled ? "Aktif" : "Pasif")})";
        return (activityModule, description);
    }

    private static (string ActivityModule, string Description) GenerateCookieManagementActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.CookieManagement;
        var enabled = GetBoolValue(settings.GetValueOrDefault("enabled"));
        var description = $"{moduleName} modülü ayarları güncellendi (Durum: {(enabled ? "Aktif" : "Pasif")})";
        return (activityModule, description);
    }

    private static (string ActivityModule, string Description) GenerateFirstOrderActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.FirstOrder;
        var enabledChannels = GetEnabledChannels(settings, new[] { "email", "sms", "whatsapp", "push" });
        var giftVoucherEnabled = GetBoolValue(settings.GetValueOrDefault("giftVoucherEnabled"));
        var giftVoucherAmount = GetDecimalValue(settings.GetValueOrDefault("giftVoucherAmount"), 50);
        var channelsText = enabledChannels.Any() ? string.Join(", ", enabledChannels) : "Yok";
        var giftVoucherText = giftVoucherEnabled ? $"Açık (Tutar: {giftVoucherAmount}₺)" : "Kapalı";
        
        var description = $"{moduleName} modülü ayarları güncellendi (Kanallar: {channelsText}, Hediye Çeki: {giftVoucherText})";
        return (activityModule, description);
    }

    private static (string ActivityModule, string Description) GenerateCommentTransferActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.Comment;
        var enabled = GetBoolValue(settings.GetValueOrDefault("enabled"));
        var description = $"{moduleName} modülü ayarları güncellendi (Durum: {(enabled ? "Aktif" : "Pasif")})";
        return (activityModule, description);
    }

    private static (string ActivityModule, string Description) GenerateBulkMessagingActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.BulkMessaging;
        var enabledChannels = new List<string>();
        
        if (settings.ContainsKey("enabledChannels"))
        {
            var channelsObj = settings["enabledChannels"];
            if (channelsObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                enabledChannels = jsonElement.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString() ?? "")
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => x.Substring(0, 1).ToUpper() + x.Substring(1).ToLower())
                    .ToList();
            }
        }
        
        var channelsText = enabledChannels.Any() ? string.Join(", ", enabledChannels) : "Yok";
        var description = $"{moduleName} modülü ayarları güncellendi (Kanallar: {channelsText})";
        return (activityModule, description);
    }

    private static (string ActivityModule, string Description) GenerateBasketReminderActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.BasketReminder;
        var enabled = GetBoolValue(settings.GetValueOrDefault("enabled"));
        var description = $"{moduleName} modülü ayarları güncellendi (Durum: {(enabled ? "Aktif" : "Pasif")})";
        return (activityModule, description);
    }

    private static (string ActivityModule, string Description) GenerateOrderStatusActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.OrderStatus;
        var enabled = GetBoolValue(settings.GetValueOrDefault("enabled"));
        var description = $"{moduleName} modülü ayarları güncellendi (Durum: {(enabled ? "Aktif" : "Pasif")})";
        return (activityModule, description);
    }

    private static (string ActivityModule, string Description) GenerateGiftWheelActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.GiftWheel;
        var enabled = GetBoolValue(settings.GetValueOrDefault("enabled"));
        var description = $"{moduleName} modülü ayarları güncellendi (Durum: {(enabled ? "Aktif" : "Pasif")})";
        return (activityModule, description);
    }

    private static (string ActivityModule, string Description) GenerateVideoHostingActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.VideoHosting;
        var enabled = GetBoolValue(settings.GetValueOrDefault("enabled"));
        var description = $"{moduleName} modülü ayarları güncellendi (Durum: {(enabled ? "Aktif" : "Pasif")})";
        return (activityModule, description);
    }

    private static (string ActivityModule, string Description) GenerateExitSurveyActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.ExitSurvey;
        var enabled = GetBoolValue(settings.GetValueOrDefault("enabled"));
        var description = $"{moduleName} modülü ayarları güncellendi (Durum: {(enabled ? "Aktif" : "Pasif")})";
        return (activityModule, description);
    }

    private static (string ActivityModule, string Description) GenerateProductSliderActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.ProductSlider;
        var enabled = GetBoolValue(settings.GetValueOrDefault("enabled"));
        var description = $"{moduleName} modülü ayarları güncellendi (Durum: {(enabled ? "Aktif" : "Pasif")})";
        return (activityModule, description);
    }

    private static (string ActivityModule, string Description) GenerateStockReminderActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.StockReminder;
        var enabledChannels = GetEnabledChannels(settings, new[] { "email", "sms", "whatsapp", "push" });
        var stockThreshold = GetIntValue(settings.GetValueOrDefault("stockThreshold"), 10);
        var autoNotifyOnStockArrival = GetBoolValue(settings.GetValueOrDefault("autoNotifyOnStockArrival"));
        var channelsText = enabledChannels.Any() ? string.Join(", ", enabledChannels) : "Yok";
        
        var description = $"{moduleName} modülü ayarları güncellendi (Stok Eşiği: {stockThreshold}, Otomatik Bildirim: {(autoNotifyOnStockArrival ? "Açık" : "Kapalı")}, Kanallar: {channelsText})";
        return (activityModule, description);
    }

    private static (string ActivityModule, string Description) GenerateFavoriteProductsActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.Store;
        var enabled = GetBoolValue(settings.GetValueOrDefault("enabled"));
        var description = $"{moduleName} modülü ayarları güncellendi (Durum: {(enabled ? "Aktif" : "Pasif")})";
        return (activityModule, description);
    }

    private static (string ActivityModule, string Description) GenerateDefaultActivityLog(
        string moduleName,
        Dictionary<string, object> settings)
    {
        var activityModule = ActivityModules.Store;
        var enabled = GetBoolValue(settings.GetValueOrDefault("enabled"));
        var description = $"{moduleName} modülü ayarları güncellendi (Durum: {(enabled ? "Aktif" : "Pasif")})";
        return (activityModule, description);
    }

    private static List<string> GetEnabledChannels(Dictionary<string, object> settings, string[] channelTypes)
    {
        var enabledChannels = new List<string>();
        
        foreach (var channelType in channelTypes)
        {
            var settingKey = $"{channelType}Enabled";
            if (settings.ContainsKey(settingKey) && GetBoolValue(settings[settingKey]))
            {
                var displayName = channelType switch
                {
                    "email" => "Email",
                    "sms" => "SMS",
                    "whatsapp" => "WhatsApp",
                    "push" => "Push",
                    _ => channelType.Substring(0, 1).ToUpper() + channelType.Substring(1).ToLower()
                };
                enabledChannels.Add(displayName);
            }
        }
        
        return enabledChannels;
    }

    private static int GetIntValue(object? value, int defaultValue = 0)
    {
        if (value == null)
            return defaultValue;

        if (value is int intValue)
            return intValue;

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Number && jsonElement.TryGetInt32(out var jsonInt))
                return jsonInt;
        }

        if (int.TryParse(value.ToString(), out var parsed))
            return parsed;

        return defaultValue;
    }

    private static decimal GetDecimalValue(object? value, decimal defaultValue = 0)
    {
        if (value == null)
            return defaultValue;

        if (value is decimal decimalValue)
            return decimalValue;

        if (value is double doubleValue)
            return (decimal)doubleValue;

        if (value is float floatValue)
            return (decimal)floatValue;

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Number && jsonElement.TryGetDecimal(out var jsonDecimal))
                return jsonDecimal;
        }

        if (decimal.TryParse(value.ToString(), out var parsed))
            return parsed;

        return defaultValue;
    }

    private static bool GetBoolValue(object? value)
    {
        if (value == null)
            return false;

        if (value is bool boolValue)
            return boolValue;

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.True)
                return true;
            if (jsonElement.ValueKind == JsonValueKind.False)
                return false;
        }

        if (bool.TryParse(value.ToString(), out var parsed))
            return parsed;

        var stringValue = value.ToString()?.ToLower();
        return stringValue == "true" || stringValue == "1";
    }
}
