using PushDashboard.Services.WhatsApp;

namespace PushDashboard.Services;

public interface IWhatsAppTemplateSampleService
{
    List<WhatsAppTemplateSample> GetSampleTemplates();
    WhatsAppTemplateSample? GetSampleTemplate(string id);
}

public class WhatsAppTemplateSampleService : IWhatsAppTemplateSampleService
{
    public List<WhatsAppTemplateSample> GetSampleTemplates()
    {
        return new List<WhatsAppTemplateSample>
        {
            new WhatsAppTemplateSample
            {
                Id = "welcome_message",
                Name = "İlk Alışveriş Hoş Geldin Mesajı",
                Description = "İlk alışveriş yapan müşteriler için hoş geldin mesajı",
                Category = "MARKETING",
                Language = "tr",
                Components = new List<TemplateComponent>
                {
                    new TemplateComponent
                    {
                        Type = "HEADER",
                        Text = "🎉 Hoş Geldiniz!",
                        Format = "TEXT"
                    },
                    new TemplateComponent
                    {
                        Type = "BODY",
                        Text = "Merhaba {{1}}, {{2}} ailesine hoş geldiniz! İlk alışverişiniz için teşekkür ederiz. Sizin için özel fırsatlarımızı kaçırmayın."
                    },
                    new TemplateComponent
                    {
                        Type = "FOOTER",
                        Text = "İyi alışverişler dileriz"
                    },
                    new TemplateComponent
                    {
                        Type = "BUTTONS",
                        Buttons = new List<TemplateButton>
                        {
                            new TemplateButton
                            {
                                Type = "URL",
                                Text = "Alışverişe Devam Et",
                                Url = "https://example.com/shop"
                            }
                        }
                    }
                },
                VariableMapping = new Dictionary<string, string>
                {
                    {"1", "Müşteri Adı"},
                    {"2", "Firma Adı"}
                }
            },
            new WhatsAppTemplateSample
            {
                Id = "birthday_greeting",
                Name = "Doğum Günü Kutlama Mesajı",
                Description = "Müşterilerin doğum günü için kutlama mesajı",
                Category = "MARKETING",
                Language = "tr",
                Components = new List<TemplateComponent>
                {
                    new TemplateComponent
                    {
                        Type = "HEADER",
                        Text = "🎂 Doğum Günün Kutlu Olsun!",
                        Format = "TEXT"
                    },
                    new TemplateComponent
                    {
                        Type = "BODY",
                        Text = "Sevgili {{1}}, doğum gününüz kutlu olsun! 🎉 Bu özel günde sizin için hazırladığımız sürpriz hediyeyi kaçırmayın. Size özel %{{2}} indirim fırsatı!"
                    },
                    new TemplateComponent
                    {
                        Type = "FOOTER",
                        Text = "Mutlu yıllar dileriz ❤️"
                    },
                    new TemplateComponent
                    {
                        Type = "BUTTONS",
                        Buttons = new List<TemplateButton>
                        {
                            new TemplateButton
                            {
                                Type = "URL",
                                Text = "Hediyemi Al",
                                Url = "https://example.com/birthday-gift"
                            },
                            new TemplateButton
                            {
                                Type = "QUICK_REPLY",
                                Text = "Teşekkürler"
                            }
                        }
                    }
                },
                VariableMapping = new Dictionary<string, string>
                {
                    {"1", "Müşteri Adı"},
                    {"2", "İndirim Oranı"}
                }
            },
            new WhatsAppTemplateSample
            {
                Id = "gift_wheel_winner",
                Name = "Hediye Çarkı Kazanan Bildirimi",
                Description = "Hediye çarkından kazanan müşteriler için bildirim mesajı",
                Category = "MARKETING",
                Language = "tr",
                Components = new List<TemplateComponent>
                {
                    new TemplateComponent
                    {
                        Type = "HEADER",
                        Text = "🎉 Tebrikler! Hediye Kazandınız!",
                        Format = "TEXT"
                    },
                    new TemplateComponent
                    {
                        Type = "BODY",
                        Text = "Merhaba {{1}}, hediye çarkımızdan {{2}} kazandınız! 🎁 Hediye kodunuz: {{3}} Bu fırsat sadece {{4}} saat geçerlidir."
                    },
                    new TemplateComponent
                    {
                        Type = "FOOTER",
                        Text = "İyi alışverişler dileriz!"
                    },
                    new TemplateComponent
                    {
                        Type = "BUTTONS",
                        Buttons = new List<TemplateButton>
                        {
                            new TemplateButton
                            {
                                Type = "URL",
                                Text = "Alışverişe Başla",
                                Url = "https://example.com/shop"
                            },
                            new TemplateButton
                            {
                                Type = "QUICK_REPLY",
                                Text = "Teşekkürler"
                            }
                        }
                    }
                },
                VariableMapping = new Dictionary<string, string>
                {
                    {"1", "Müşteri Adı"},
                    {"2", "Kazanılan Hediye"},
                    {"3", "Hediye Kodu"},
                    {"4", "Geçerlilik Süresi (Saat)"}
                }
            },
            // Order Confirmation
            new WhatsAppTemplateSample
            {
                Id = "order_confirmation_v2",
                Name = "Sipariş Onay Bildirimi",
                Description = "Müşteriye siparişinin alındığını bildiren mesaj",
                Category = "UTILITY",
                Language = "tr",
                Components = new List<TemplateComponent>
                {
                    new TemplateComponent
                    {
                        Type = "HEADER",
                        Text = "Siparişiniz Alındı ✅",
                        Format = "TEXT"
                    },
                    new TemplateComponent
                    {
                        Type = "BODY",
                        Text = "Sayın {{1}}, {{2}} numaralı siparişiniz başarıyla alınmıştır. Sipariş tutarınız {{3}} TL'dir. Bizi tercih ettiğiniz için teşekkür ederiz."
                    },
                    new TemplateComponent
                    {
                        Type = "FOOTER",
                        Text = "Sipariş durumunu web sitemizden takip edebilirsiniz."
                    },
                    new TemplateComponent
                    {
                        Type = "BUTTONS",
                        Buttons = new List<TemplateButton>
                        {
                            new TemplateButton
                            {
                                Type = "URL",
                                Text = "Siparişi Görüntüle",
                                Url = "https://example.com/orders/{{2}}"
                            }
                        }
                    }
                },
                VariableMapping = new Dictionary<string, string>
                {
                    {"1", "Müşteri Adı"},
                    {"2", "Sipariş Numarası"},
                    {"3", "Sipariş Tutarı"}
                }
            },
            // Cargo Tracking
            new WhatsAppTemplateSample
            {
                Id = "cargo_tracking",
                Name = "Kargo Takip Bilgisi",
                Description = "Sipariş kargoya verildiğinde gönderilecek bildirim",
                Category = "UTILITY",
                Language = "tr",
                Components = new List<TemplateComponent>
                {
                    new TemplateComponent
                    {
                        Type = "HEADER",
                        Format = "IMAGE"
                    },
                    new TemplateComponent
                    {
                        Type = "BODY",
                        Text = "Merhaba {{1}}, {{2}} numaralı siparişiniz kargoya verilmiştir! \n\n📦Takip No: {{3}}"
                    },
                    new TemplateComponent
                    {
                        Type = "FOOTER",
                        Text = "Tahmini teslimat: 1-3 iş günü"
                    },
                    new TemplateComponent
                    {
                        Type = "BUTTONS",
                        Buttons = new List<TemplateButton>
                        {
                            new TemplateButton
                            {
                                Type = "URL",
                                Text = "Kargom Nerede?",
                                Url = "https://kargo-tracking.com?id={{3}}"
                            }
                        }
                    }
                },
                VariableMapping = new Dictionary<string, string>
                {
                    {"1", "Müşteri Adı"},
                    {"2", "Sipariş Numarası"},
                    {"3", "Takip Numarası"}
                }
            },
            // 2FA / OTP Code
            new WhatsAppTemplateSample
            {
                Id = "otp_verification",
                Name = "Giriş Doğrulama Kodu",
                Description = "Kullanıcı girişi veya işlem onayı için gönderilen tek kullanımlık şifre",
                Category = "AUTHENTICATION",
                Language = "tr",
                Components = new List<TemplateComponent>
                {
                    new TemplateComponent
                    {
                        Type = "BODY",
                        Text = "{{1}} doğrulama kodunuzdur. Güvenliğiniz için bu kodu kimseyle paylaşmayınız."
                    },
                    new TemplateComponent
                    {
                        Type = "FOOTER",
                        Text = "Geçerlilik süresi: 3 dakika"

                    }
                    
                },
                VariableMapping = new Dictionary<string, string>
                {
                    {"1", "Doğrulama Kodu"}
                }
            }
        };
    }

    public WhatsAppTemplateSample? GetSampleTemplate(string id)
    {
        return GetSampleTemplates().FirstOrDefault(t => t.Id == id);
    }
}

public class WhatsAppTemplateSample
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "MARKETING";
    public string Language { get; set; } = "tr";
    public List<TemplateComponent> Components { get; set; } = new();
    
    // Custom mapping for friendly variable names
    // Key: Variable Index (e.g. "1"), Value: Friendly Name (e.g. "Müşteri Adı")
    public Dictionary<string, string> VariableMapping { get; set; } = new();
}
