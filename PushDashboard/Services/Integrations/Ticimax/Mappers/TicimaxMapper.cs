using PushDashboard.Services.Integrations.Common.Models;
using PushDashboard.CustomServis;
using PushDashboard.UyeServis;
using PushDashboard.SiparisServis;

namespace PushDashboard.Services.Integrations.Ticimax.Mappers
{
    /// <summary>
    /// Mapper class for converting between Ticimax-specific models and global e-commerce models
    /// </summary>
    public static class TicimaxMapper
    {
        #region Webhook Mapping

        /// <summary>
        /// Maps Ticimax webhook to global e-commerce webhook
        /// </summary>
        public static EcommerceWebhook MapToEcommerceWebhook(Webhook ticimaxWebhook)
        {
            return new EcommerceWebhook
            {
                Id = ticimaxWebhook.ID.ToString(),
                EventType = MapWebhookEventType(ticimaxWebhook.IslemTipi),
                Url = ticimaxWebhook.Url ?? string.Empty,
                IsActive = true, // Ticimax doesn't have active/inactive status
                CreatedAt = DateTime.Now, // Ticimax doesn't provide creation date, use current time
                Metadata = new Dictionary<string, object>
                {
                    ["ticimaxId"] = ticimaxWebhook.ID,
                    ["ticimaxEventType"] = ticimaxWebhook.IslemTipi.ToString(),
                    ["username"] = ticimaxWebhook.KullaniciAdi ?? string.Empty
                }
            };
        }

        /// <summary>
        /// Maps global webhook request to Ticimax webhook
        /// </summary>
        public static Webhook MapToTicimaxWebhook(EcommerceWebhookRequest request)
        {
            return new Webhook
            {
                ID = 0, // New webhook
                IslemTipi = MapToTicimaxWebhookType(request.EventType),
                Url = request.Url,
                KullaniciAdi = request.Username ?? "pushonica",
                Sifre = request.Password ?? "pushonica123!"
            };
        }

        /// <summary>
        /// Maps Ticimax webhook event type to global event type
        /// </summary>
        public static string MapWebhookEventType(WebhookIslem ticimaxEventType)
        {
            return ticimaxEventType switch
            {
                WebhookIslem.SiparisOlustu => EcommerceWebhookEventType.OrderCreated.ToString(),
                WebhookIslem.UyeKayitOldu => EcommerceWebhookEventType.CustomerCreated.ToString(),
                WebhookIslem.UyeBilgileriGuncellendi => EcommerceWebhookEventType.CustomerUpdated.ToString(),
                WebhookIslem.SiparisDurumuDegistirildi => EcommerceWebhookEventType.OrderStatusChanged.ToString(),
                WebhookIslem.UyeFavoriUrunEkledi => EcommerceWebhookEventType.CustomerFavoriteProductAdded.ToString(),
                WebhookIslem.UyeFavoriUrunSildi => EcommerceWebhookEventType.CustomerFavoriteProductRemoved.ToString(),
                WebhookIslem.UrunGuncellendi => EcommerceWebhookEventType.ProductUpdated.ToString(),
                WebhookIslem.VaryasyonGuncellendi => EcommerceWebhookEventType.ProductStockChanged.ToString(),
                _ => ticimaxEventType.ToString()
            };
        }

        /// <summary>
        /// Maps global event type to Ticimax webhook type
        /// </summary>
        public static WebhookIslem MapToTicimaxWebhookType(string eventType)
        {
            return eventType switch
            {
                nameof(EcommerceWebhookEventType.OrderCreated) => WebhookIslem.SiparisOlustu,
                nameof(EcommerceWebhookEventType.OrderStatusChanged) => WebhookIslem.SiparisDurumuDegistirildi,
                nameof(EcommerceWebhookEventType.CustomerCreated) => WebhookIslem.UyeKayitOldu,
                nameof(EcommerceWebhookEventType.CustomerUpdated) => WebhookIslem.UyeBilgileriGuncellendi,
                nameof(EcommerceWebhookEventType.CustomerFavoriteProductAdded) => WebhookIslem.UyeFavoriUrunEkledi,
                nameof(EcommerceWebhookEventType.CustomerFavoriteProductRemoved) => WebhookIslem.UyeFavoriUrunSildi,
                nameof(EcommerceWebhookEventType.ProductUpdated) => WebhookIslem.UrunGuncellendi,
                nameof(EcommerceWebhookEventType.ProductStockChanged) => WebhookIslem.VaryasyonGuncellendi,
                _ => Enum.TryParse<WebhookIslem>(eventType, out var result) ? result : WebhookIslem.SiparisOlustu
            };
        }

        #endregion

        #region Customer Mapping

        /// <summary>
        /// Maps Ticimax customer to global e-commerce customer
        /// </summary>
        public static EcommerceCustomer MapToEcommerceCustomer(Uye ticimaxCustomer)
        {
            return new EcommerceCustomer
            {
                ExternalId = ticimaxCustomer.ID.ToString(),
                Email = ticimaxCustomer.Mail ?? string.Empty,
                FirstName = ticimaxCustomer.Isim,
                LastName = ticimaxCustomer.Soyisim,
                Phone = ticimaxCustomer.CepTelefonu,
                BirthDate = ticimaxCustomer.DogumTarihi,
                Gender = MapGender(ticimaxCustomer.CinsiyetID),
                IsActive = ticimaxCustomer.Aktif,
                EmailPermission = ticimaxCustomer.MailIzin,
                SmsPermission = ticimaxCustomer.SmsIzin,
                CreatedAt = ticimaxCustomer.UyelikTarihi,
                UpdatedAt = ticimaxCustomer.DuzenlemeTarihi,
                CustomFields = new Dictionary<string, object>
                {
                    ["ticimaxId"] = ticimaxCustomer.ID,
                    ["ticimaxCode"] = ticimaxCustomer.MusteriKodu ?? string.Empty,
                    ["city"] = ticimaxCustomer.Il ?? string.Empty,
                    ["district"] = ticimaxCustomer.Ilce ?? string.Empty,
                    ["lastLoginDate"] = ticimaxCustomer.SonGirisTarihi,
                    ["lastLoginIp"] = ticimaxCustomer.SonGirisIp ?? string.Empty
                }
            };
        }

        /// <summary>
        /// Maps global customer filter to Ticimax customer filter
        /// </summary>
        public static UyeFiltre MapToTicimaxCustomerFilter(EcommerceCustomerFilter filter)
        {
            return new UyeFiltre
            {
                Aktif = filter.IsActive switch
                {
                    true => 1,
                    false => 0,
                    null => -1
                },
                Cinsiyet = filter.Gender switch
                {
                    "Male" => 1,
                    "Female" => 2,
                    _ => -1
                },
                AlisverisYapti = filter.HasMadePurchase switch
                {
                    true => 1,
                    false => 0,
                    null => -1
                },
                MailIzin = filter.EmailPermission switch
                {
                    true => 1,
                    false => 0,
                    null => -1
                },
                SmsIzin = filter.SmsPermission switch
                {
                    true => 1,
                    false => 0,
                    null => -1
                },
                UyeID = filter.MemberId != null && int.TryParse(filter.MemberId, out var memberId) ? memberId : 0
            };
        }

        /// <summary>
        /// Maps global pagination to Ticimax pagination
        /// </summary>
        public static UyeSayfalama MapToTicimaxPagination(EcommercePagination pagination)
        {
            return new UyeSayfalama
            {
                SayfaNo = pagination.PageNumber,
                KayitSayisi = pagination.PageSize,
                SiralamaDegeri = pagination.SortField ?? "ID",
                SiralamaYonu = pagination.SortDirection ?? "ASC"
            };
        }

        #endregion

        #region Order Mapping

        /// <summary>
        /// Maps Ticimax order to global e-commerce order
        /// </summary>
        public static EcommerceOrder MapToEcommerceOrder(WebSiparis ticimaxOrder)
        {
            return new EcommerceOrder
            {
                ExternalId = ticimaxOrder.ID.ToString(),
                OrderNumber = ticimaxOrder.SiparisNo,
                CustomerId = ticimaxOrder.UyeID.ToString(),
                CustomerExternalId = ticimaxOrder.UyeID.ToString(),
                CustomerName = ticimaxOrder.AdiSoyadi ?? string.Empty,
                Status = MapOrderStatus(ticimaxOrder.Durum.ToString()),
                PaymentStatus = MapPaymentStatus(ticimaxOrder.Durum.ToString()),
                KDVAmount = (decimal)ticimaxOrder.Urunler.GroupBy(u => u.UrunID).Sum(g => g.First().KdvTutari * g.Sum(x => x.Adet)),
                TotalTax = (decimal)ticimaxOrder.ToplamKdv,
                ShippingCost = (decimal)ticimaxOrder.KargoTutari,
                TotalAmount = (decimal)ticimaxOrder.SiparisToplamTutari,
                Currency = ticimaxOrder.ParaBirimi,
                PaymentCompleted = ticimaxOrder.OdenenTutar >= ticimaxOrder.ToplamTutar,
                OrderDate = ticimaxOrder.SiparisTarihi,
                PaymentDate = ticimaxOrder.Odemeler?.FirstOrDefault()?.Tarih ?? ticimaxOrder.SiparisTarihi,
                AdvertisingSource = ticimaxOrder.ReklamKaynagi,
                CargoTrackingLink = ticimaxOrder.KargoTakipLink,
                CargoTrackingNumber = ticimaxOrder.KargoTakipNo,
                CustomFields = new Dictionary<string, object>
                {
                    ["ticimaxId"] = ticimaxOrder.ID,
                    ["ticimaxOrderNumber"] = ticimaxOrder.SiparisNo ?? string.Empty,
                    ["status"] = ticimaxOrder.Durum,
                    ["customerName"] = ticimaxOrder.AdiSoyadi ?? string.Empty,
                    ["voucherCode"] = ticimaxOrder.HediyeCeki ?? string.Empty,
                    ["discountAmount"] = ticimaxOrder.HediyeCekiTutari
                }
            };
        }

        /// <summary>
        /// Maps Ticimax order status to payment status
        /// </summary>
        private static string MapPaymentStatus(string? ticimaxStatus)
        {
            if (string.IsNullOrEmpty(ticimaxStatus))
                return "Bilinmiyor";

            // Ticimax ödeme durumu mapping
            return ticimaxStatus.ToLowerInvariant() switch
            {
                "1" or "beklemede" => "Ödeme Bekleniyor",
                "2" or "havaleonaybekliyor" or "havale/eft bekliyor" => "Havale/EFT Bekliyor",
                "3" or "odemebasarisiz" => "Ödeme Başarısız",
                "4" or "odemeonaylandi" or "onaylandi" => "Ödeme Onaylandı",
                "5" or "iptal" => "İptal Edildi",
                "6" or "iade" => "İade Edildi",
                _ => ticimaxStatus
            };
        }

        /// <summary>
        /// Maps global order filter to Ticimax order filter
        /// </summary>
        public static WebSiparisFiltre MapToTicimaxOrderFilter(EcommerceOrderFilter filter)
        {
            return new WebSiparisFiltre
            {
                UyeID = !string.IsNullOrEmpty(filter.CustomerId) && int.TryParse(filter.CustomerId, out var customerId) ? customerId : 0,
                OdemeDurumu = filter.PaymentCompleted.HasValue ? 1 : -1,
                EntegrasyonAktarildi = -1,
                OdemeTipi = -1,
                SiparisDurumu = -1,
                SiparisID = filter.OrderId.HasValue ? filter.OrderId.Value : 0,
                SiparisTarihiBas = filter.CreatedAfter
                // Note: Ticimax WebSiparisFiltre doesn't have payment completed or date filters
                // These would need to be handled differently or ignored
            };
        }

        /// <summary>
        /// Maps global pagination to Ticimax order pagination
        /// </summary>
        public static WebSiparisSayfalama MapToTicimaxOrderPagination(EcommercePagination pagination)
        {
            return new WebSiparisSayfalama
            {
                BaslangicIndex = (pagination.PageNumber - 1) * pagination.PageSize,
                KayitSayisi = pagination.PageSize
            };
        }

        #endregion

        #region Cart Mapping

        /// <summary>
        /// Maps Ticimax cart to global e-commerce cart
        /// </summary>
        public static EcommerceCart MapToEcommerceCart(WebSepet ticimaxCart)
        {
            var items = ticimaxCart.Urunler?.Select(MapToEcommerceCartItem).ToList() ?? new List<EcommerceCartItem>();

            return new EcommerceCart
            {
                ExternalId = ticimaxCart.GuidSepetID ?? "0",
                CustomerId = ticimaxCart.UyeID.ToString(),
                CustomerName = ticimaxCart.UyeAdi ?? string.Empty,
                CustomerEmail = ticimaxCart.UyeMail ?? string.Empty,
                SessionId = ticimaxCart.GuidSepetID,
                TotalAmount = (decimal)(ticimaxCart.Urunler?.Sum(u => u.UrunSepetFiyati * u.Adet) ?? 0),
                TotalTax = items.Sum(i => i.TaxAmount),
                ShippingCost = items.Sum(i => i.ShippingCost),
                ItemCount = items.Count,
                IsAbandoned = false, // Ticimax'ta bu bilgi yok, default false
                Currency = "TRY",
                CreatedAt = ticimaxCart.SepetTarihi,
                UpdatedAt = ticimaxCart.SepetTarihi,
                Items = items,
                CustomFields = new Dictionary<string, object>
                {
                    ["ticimaxGuid"] = ticimaxCart.GuidSepetID ?? string.Empty,
                    ["ticimaxMemberId"] = ticimaxCart.UyeID
                }
            };
        }

        /// <summary>
        /// Maps Ticimax cart item to global e-commerce cart item
        /// </summary>
        public static EcommerceCartItem MapToEcommerceCartItem(WebSepetUrun ticimaxCartItem)
        {
            return new EcommerceCartItem
            {
                ExternalId = ticimaxCartItem.ID.ToString(),
                ProductId = ticimaxCartItem.UrunID.ToString(),
                ProductCode = ticimaxCartItem.StokKodu ?? string.Empty,
                ProductName = ticimaxCartItem.UrunAdi ?? string.Empty,
                ProductImage = ticimaxCartItem.SpotResim ?? string.Empty,
                Quantity = (int)ticimaxCartItem.Adet, // Convert double to int
                UnitPrice = (decimal)ticimaxCartItem.UrunSepetFiyati,
                TotalPrice = (decimal)(ticimaxCartItem.UrunSepetFiyati * ticimaxCartItem.Adet),
                TaxRate = (decimal)ticimaxCartItem.KDVOrani,
                TaxAmount = (decimal)ticimaxCartItem.KDVTutari,
                ShippingCost = (decimal)ticimaxCartItem.KargoUcreti,
                FreeShipping = ticimaxCartItem.UcretsizKargo,
                Currency = ticimaxCartItem.ParaBirimi ?? "TRY",
                CustomFields = new Dictionary<string, object>
                {
                    ["ticimaxId"] = ticimaxCartItem.ID,
                    ["ticimaxProductId"] = ticimaxCartItem.UrunID,
                    ["ticimaxStokKodu"] = ticimaxCartItem.StokKodu ?? string.Empty
                }
            };
        }

        #endregion

        #region Gift Voucher Mapping

        /// <summary>
        /// Maps global gift voucher request to Ticimax gift voucher request
        /// </summary>
        public static WebHediyeCekiOlusturRequest MapToTicimaxGiftVoucherRequest(EcommerceGiftVoucherRequest request)
        {
            var ticimaxRequest = new WebHediyeCekiOlusturRequest
            {
                GrupAdi = request.Name,
                IndirimTipi = request.FreeShipping ? 2 : (request.DiscountType == "PERCENTAGE" ? 1 : 0), // 1 = YUZDE, 0 = TL
                IndirimDegeri = (double)request.DiscountValue,
                TarihBaslagic = DateTime.Now,
                TarihBitis = request.ExpiryDate ?? DateTime.Now.AddDays(30),
                WebAktif = true,
                MobilAktif = true,
                AndroidAktif = true,
                IOSAktif = true,
                UyeMaksimumKullanimSayisi = request.UsageLimit ?? 1,
                KullanimSayisi = request.UsageLimit ?? 1,
                MinimumUrunTutari = (double)(request.MinOrderAmount ?? 0),
                BirlestirilebilirKampanyalar = request.CombineWithOtherCampaigns ? null : new BLHediyeCekiKampanya[0],
                IndirimliUrunleKullanilabilir = request.AllowDiscountedProducts
            };

            if (!string.IsNullOrEmpty(request.VoucherCode))
            {
                ticimaxRequest.Kod = request.VoucherCode;
            }

            if (!string.IsNullOrEmpty(request.CustomerId) && int.TryParse(request.CustomerId, out var customerId) && customerId > 0)
            {
                ticimaxRequest.UyeID = customerId;
                if (!string.IsNullOrEmpty(request.CustomerName))
                {
                    ticimaxRequest.UyeAdi = request.CustomerName;
                }
            }

            return ticimaxRequest;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Maps Ticimax gender to global gender
        /// </summary>
        private static string? MapGender(int ticimaxGender)
        {
            return ticimaxGender switch
            {
                1 => EcommerceGender.Male.ToString(),
                2 => EcommerceGender.Female.ToString(),
                _ => EcommerceGender.NotSpecified.ToString()
            };
        }

        /// <summary>
        /// Maps Ticimax order status to global order status
        /// </summary>
        private static string MapOrderStatus(string? ticimaxStatus)
        {
            return ticimaxStatus?.ToLower() switch
            {
                "1" => EcommerceOrderStatus.Pending.ToString(),
                "2" => EcommerceOrderStatus.Processing.ToString(),
                "3" => EcommerceOrderStatus.Shipped.ToString(),
                "4" => EcommerceOrderStatus.Delivered.ToString(),
                "5" => EcommerceOrderStatus.Cancelled.ToString(),
                "6" => EcommerceOrderStatus.Refunded.ToString(),
                "7" => EcommerceOrderStatus.Completed.ToString(),
                _ => EcommerceOrderStatus.Pending.ToString()
            };
        }

        #endregion
    }
}
