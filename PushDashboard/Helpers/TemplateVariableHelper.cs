using System.Collections.Generic;

namespace PushDashboard.Helpers;

public static class TemplateVariableHelper
{
    public static Dictionary<string, string> GetVariablesForModule(string moduleKey)
    {
        var result = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(moduleKey)) return result;

        switch (moduleKey)
        {
            case "BulkMessaging":
            case "Toplu Mesaj Gönderimi":
                result.Add("customer_name", "Müşteri Tam Adı");
                result.Add("phone_number", "Müşteri Telefon");
                result.Add("email_address", "Müşteri E-Posta");
                result.Add("customer_city", "Müşteri Şehir");
                result.Add("customer_first_name", "Müşteri Adı");
                result.Add("customer_last_name", "Müşteri Soyadı");
                break;

            case "FirstOrder":
            case "İlk Alışveriş Mesajı":
                result.Add("customer_name", "Müşteri Adı Soyadı");
                result.Add("customer_first_name", "Müşteri Adı");
                result.Add("customer_last_name", "Müşteri Soyadı");
                result.Add("customer_email", "Müşteri Email");
                result.Add("customer_phone", "Müşteri Telefon");
                result.Add("current_date", "Güncel Tarih");
                result.Add("current_time", "Güncel Saat");
                result.Add("gift_voucher_code", "Hediye Çeki Kodu");
                result.Add("gift_voucher_amount", "Hediye Çeki Tutarı");
                result.Add("has_gift_voucher", "Hediye Çeki Var Mı?");
                break;
            
            case "Birthday":
            case "Doğum Günü Mesajı":
                result.Add("customer_name", "Müşteri Adı Soyadı");
                result.Add("customer_first_name", "Müşteri Adı");
                result.Add("customer_last_name", "Müşteri Soyadı");
                result.Add("customer_email", "Müşteri Email");
                result.Add("customer_phone", "Müşteri Telefon");
                result.Add("current_date", "Güncel Tarih");
                result.Add("current_time", "Güncel Saat");
                result.Add("gift_voucher_code", "Hediye Çeki Kodu");
                result.Add("gift_voucher_amount", "Hediye Çeki Tutarı");
                result.Add("has_gift_voucher", "Hediye Çeki Var Mı?");
                result.Add("age", "Müşteri Yaşı");
                result.Add("birthdate", "Müşteri Doğum Günü");
                break;
            
            case "BasketReminder":
            case "Sepet Hatırlatma":
                result.Add("customer_name", "Müşteri Adı Soyadı");
                result.Add("customer_first_name", "Müşteri Adı");
                result.Add("customer_last_name", "Müşteri Soyadı");
                result.Add("customer_email", "Müşteri Email");
                result.Add("customer_phone", "Müşteri Telefon");
                break;
            
            case "StockReminder":
            case "Stok Hatırlatma":
                result.Add("customer_name", "Müşteri Adı Soyadı");
                result.Add("customer_first_name", "Müşteri Adı");
                result.Add("customer_last_name", "Müşteri Soyadı");
                result.Add("customer_email", "Müşteri Email");
                result.Add("customer_phone", "Müşteri Telefon");
                result.Add("current_date", "Güncel Tarih");
                result.Add("current_time", "Güncel Saat");
                result.Add("product_name", "Ürün Adı");
                result.Add("product_url", "Ürün Linki");
                break;
            
            case "GiftWheel":
            case "Hediye Çarkı":
                result.Add("customer_name", "Müşteri Adı Soyadı");
                result.Add("customer_email", "Müşteri Email");
                result.Add("customer_phone", "Müşteri Telefon");
                result.Add("gift_voucher_code", "Hediye Çeki Kodu");
                result.Add("gift_voucher_amount", "Hediye Çeki Tutarı");
                result.Add("has_gift_voucher", "Hediye Çeki Var Mı?");
                result.Add("validity_period", "Geçerlilik Süresi");
                result.Add("discount_type", "İndirim Tipi");
                result.Add("prize", "Kampanya Adı");
                break;

            case "OrderStatus":
            case "Sipariş Durumu Bildirimleri":
                result.Add("customer_name", "Müşteri Adı Soyadı");
                result.Add("customer_email", "Müşteri Email");
                result.Add("customer_phone", "Müşteri Telefon");
                result.Add("order_number", "Sipariş Numarası");
                result.Add("order_status", "Sipariş Durumu");
                result.Add("order_total", "Sipariş Tutarı");
                result.Add("order_currency", "Para Birimi");
                result.Add("order_payment_status", "Ödeme Durumu");
                result.Add("order_date", "Sipariş Tarihi");
                result.Add("payment_date", "Ödeme Tarihi");
                result.Add("shipping_cost", "Kargo Ücreti");
                result.Add("total_tax", "Toplam KDV");
                result.Add("advertising_source", "Reklam Kaynağı");
                result.Add("cargo_tracking_link", "Kargo Takip Linki");
                result.Add("cargo_tracking_number", "Kargo Takip No");
                break;
            
            case "FavoriteProduct":
            case "Favori Ürünler":
                result.Add("customer_name", "Müşteri Adı Soyadı");
                result.Add("customer_first_name", "Müşteri Adı");
                result.Add("customer_last_name", "Müşteri Soyadı");
                result.Add("customer_email", "Müşteri Email");
                result.Add("customer_phone", "Müşteri Telefon");
                result.Add("current_date", "Güncel Tarih");
                result.Add("current_time", "Güncel Saat");
                result.Add("product_name", "Ürün Adı");
                result.Add("product_price", "Ürün Fiyatı");
                result.Add("product_url", "Ürün Linki");
                break;

            case "Özel":
                 break;
        }

        return result;
    }

    public static Dictionary<string, string> GetAllVariables()
    {
        var all = new Dictionary<string, string>();
        
        // Helper to safe add
        void AddRange(Dictionary<string, string> source) {
            foreach(var kvp in source) {
                if(!all.ContainsKey(kvp.Key)) all.Add(kvp.Key, kvp.Value);
            }
        }

        AddRange(GetVariablesForModule("OrderStatus"));
        AddRange(GetVariablesForModule("BasketReminder"));
        AddRange(GetVariablesForModule("BulkMessaging"));
        AddRange(GetVariablesForModule("FirstOrder"));
        AddRange(GetVariablesForModule("GiftWheel"));
        AddRange(GetVariablesForModule("Birthday"));
        AddRange(GetVariablesForModule("Authentication"));
        AddRange(GetVariablesForModule("General"));

        return all;
    }
    
    public static string GetModuleKeyByName(string moduleName)
    {
        if (string.IsNullOrEmpty(moduleName)) return "";

        var name = moduleName.Trim();

        switch (name)
        {
            case "Sipariş Durumu Bildirimleri":
                return "OrderStatus";
            case "Sepet Hatırlatma":
                return "BasketReminder";
            case "Toplu Mesaj Gönderimi":
                return "BulkMessaging";
            case "İlk Alışveriş Mesajı":
                return "FirstOrder";
            default:
                return "";
        }

    }
    
    public static string GetModuleNameByKey(string moduleKey)
    {
        if (string.IsNullOrEmpty(moduleKey)) return "";

        var name = moduleKey.Trim();

        switch (name)
        {
            case "BulkMessaging":
                return "Toplu Mesaj Gönderimi";

            case "FirstOrder":
                return "İlk Alışveriş Mesajı";
            
            case "Birthday":
                return "Doğum Günü Mesajı";
            
            case "BasketReminder":
                return "Sepet Hatırlatma";
            case "StockReminder":
                return "Stok Hatırlatma";
            
            case "GiftWheel":
                return "Hediye Çarkı";

            case "OrderStatus":
                return "Sipariş Durumu Bildirimleri";
            
            case "FavoriteProduct":
                return "Favori Ürünler";
            
            default:
                return "";
        }

    }
}
