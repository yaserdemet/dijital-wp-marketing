# PushDashboard - Multi-Tenant E-commerce Management SaaS

## 📋 Proje Genel Bakış
PushDashboard, e-ticaret işletmeleri için geliştirilmiş çok kiracılı (multi-tenant) bir yönetim platformudur. Her şirketin kendi verilerine erişebildiği, müşteri yönetimi, sepet analizi ve entegrasyon yönetimi yapabildiği kapsamlı bir sistemdir.

## 🏗️ Teknik Altyapı
- **Framework:** ASP.NET Core 8 MVC
- **Veritabanı:** PostgreSQL + Entity Framework Core
- **Authentication:** ASP.NET Core Identity
- **Frontend:** Tailwind CSS, JavaScript (AJAX)
- **Entegrasyonlar:** SOAP Web Services (Ticimax)

## ✅ Tamamlanan Özellikler

### 🔐 Kimlik Doğrulama ve Yetkilendirme
- [x] **Kullanıcı Kayıt/Giriş Sistemi:** ASP.NET Core Identity ile güvenli kimlik doğrulama
- [x] **Çok Kiracılı Yapı:** Her kullanıcı bir şirkete bağlı, veriler şirket bazında izole
- [x] **Rol Tabanlı Yetkilendirme:** User ve CompanyOwner rolleri
- [x] **İki Faktörlü Doğrulama (2FA):** TOTP tabanlı güvenlik sistemi
- [x] **Oturum Yönetimi:** Aktif oturumları görüntüleme ve sonlandırma

### 🏢 Şirket Yönetimi
- [x] **Şirket Profili:** Şirket bilgileri yönetimi
- [x] **Fatura Bilgileri:** Kurumsal/bireysel fatura bilgileri
- [x] **Kullanıcı Yönetimi:** Şirket kullanıcılarını görüntüleme, aktif/pasif yapma
- [x] **Çok Kullanıcılı Yapı:** Bir şirkette birden fazla kullanıcı

### 👥 Müşteri Yönetimi (CRM)
- [x] **Müşteri Listesi:** Sayfalama, arama, filtreleme özellikleri
- [x] **Müşteri Detayları:** Popup ile detaylı müşteri bilgileri
- [x] **Müşteri Senkronizasyonu:** E-ticaret platformundan otomatik müşteri çekme
- [x] **Müşteri İstatistikleri:** Toplam, aktif, yeni müşteri sayıları
- [x] **Sepet Entegrasyonu:** Müşteri listesinden sepetlere erişim

### 🛒 Sepet Yönetimi
- [x] **Sepet Listesi:** Tüm sepetleri görüntüleme ve yönetme
- [x] **Sepet Detayları:** Popup ile sepet içeriği ve ürün detayları
- [x] **Çoklu Para Birimi:** Farklı para birimlerinde sepet toplamları
- [x] **Sepet Senkronizasyonu:** E-ticaret platformundan otomatik sepet çekme
- [x] **Üye/Üyesiz Sepet Ayrımı:** Sepetleri üyelik durumuna göre filtreleme
- [x] **Sepet İstatistikleri:** Toplam sepet sayısı ve değerleri

### 🔗 Entegrasyon Yönetimi
- [x] **Entegrasyon Mimarisi:** Çoklu e-ticaret platformu desteği
- [x] **Ticimax Entegrasyonu:** SOAP servisleri ile müşteri ve sepet senkronizasyonu
- [x] **Entegrasyon Ayarları:** Platform bazında konfigürasyon yönetimi
- [x] **Bağlantı Testleri:** Entegrasyon bağlantılarını test etme
- [x] **Senkronizasyon Logları:** Tüm senkronizasyon işlemlerinin kaydı

### ⚙️ Sistem Yönetimi
- [x] **Ayarlar Paneli:** Kullanıcı profili, şirket bilgileri, güvenlik ayarları
- [x] **Modül Sistemi:** Dinamik modül yönetimi ve ayarları
- [x] **Bildirim Tercihleri:** E-posta, SMS, push bildirim ayarları
- [x] **Responsive Tasarım:** Mobil uyumlu arayüz

## 🚧 Geliştirme Aşamasındaki Modüller

### 📧 Pazarlama ve İletişim Modülleri
- [ ] **1- Yeni Üye Karşılama:** Sisteme yeni kayıt olan müşterilere hoş geldin mesajları ve özel teklifler otomatik gönderilir. Böylece ilk temas anında pozitif bir deneyim sağlanır.
- [ ] **2- İlk Alışverişe Teşvik:** Yeni üyelerin ilk alışverişlerini tamamlaması için indirim kuponu veya hediye çeki sunulur, alışveriş dönüşüm oranı artırılır.
- [ ] **3- Doğum Günü:** Müşterilerin doğum günlerinde kişiye özel indirim veya hediye kuponu otomatik olarak iletilir, bağlılık güçlendirilir.
- [ ] **4- Sepet Kurtarma:** Müşteri sepetine ürün ekleyip tamamlamazsa, belirli bir süre sonra (örneğin 7 gün) hediye çeki veya indirim teklifi WhatsApp mesajıyla hatırlatılır.
- [ ] **5- Toplu WhatsApp Mesajı:** Tüm müşteri segmentlerine ya da seçilen gruplara tek tuşla kişiselleştirilmiş WhatsApp mesajları gönderilir. SMS yerine WhatsApp kullanılarak daha hızlı ve etkili iletişim sağlanır.
- [ ] **6- Hediye Çarkı:** Müşteri etkileşimini artırmak için oyunlaştırma yöntemiyle hediye çarkı sunulur. Müşteri kazanma şansı ile markaya bağlılığı artırılır.
- [ ] **7- İndirim Kulakçığı & Kampanya Çanı:** Web sitesinde veya uygulamada dikkat çekici görsel bildirimlerle güncel indirim ve kampanyalar anında duyurulur.
- [ ] **8- Kampanyalar:** Özel dönemlerde veya müşteri segmentlerine göre kampanyalar düzenlenir, satışları ve müşteri sadakatini artırır.

## 🔌 Entegrasyonlar


**1- E-ticaret Altyapı Entegrasyonları:**
- [x] **Ticimax Entegrasyonu:**
    - [x] SelectSepet - Sepet verilerini çekme
    - [x] SelectUye - Müşteri verilerini çekme
    - [x] Çoklu şirket desteği
    - [x] Manuel senkronizasyon
    - [ ] Otomatik senkronizasyon
    - [x] Hata yönetimi ve loglama


**1- Bildirim Entegrasyonları:**
- [ ] **WhatsApp Business API:** Müşteri bildirimleri için
- [ ] **SMS Entegrasyonu:** En uygun maliyetli provider ile
- [ ] **E-posta Bildirimleri:** SMTP kullanarak

**2- E-ticaret Altyapı Entegrasyonları:**
- [ ] **İkas Entegrasyonu:** API tabanlı entegrasyon
- [ ] **Shopify Entegrasyonu:** REST API ile entegrasyon
- [ ] **Sipariş Yönetimi:** Tüm platformlar için sipariş çekme

**3- Pazaryeri Entegrasyonları:**
- [ ] **Trendyol Entegrasyonu:**
    - [ ] Müşteri yorumlarını çekme
    - [ ] Ürün performans analizi
    - [ ] Satış raporları


    "DefaultConnection": "Server=yamanote.proxy.rlwy.net;Port=29407;Database=pushonica;Userid=postgres;Password=CaZNDINSCdQrCTxBtBKtYfLQeknRvoeh;Timeout=300;Command Timeout=300;Connection Idle Lifetime=300;"

