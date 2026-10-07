-- Social Proof modülünü ekle
INSERT INTO "Modules" ("Name", "Description", "DetailedDescription", "Price", "IconClass", "IconColor", "BackgroundColor", "IsActive", "IsNew", "IsFeatured", "CategoryId", "CreatedAt", "Features", "DefaultSettings")
SELECT 
    'Social Proof',
    'Ürün sayfalarında sosyal kanıt göstererek satışları artırın',
    'Social Proof modülü, ürün sayfalarında "X kişi şu anda bu ürünü inceliyor" gibi mesajlar göstererek müşterilerin satın alma motivasyonunu artırır. Rastgele ama mantıklı sayılar kullanarak gerçek veri takibi gerektirmeden etkili sosyal kanıt sunar.',
    99.99,
    'fas fa-users',
    '#6366f1',
    '#f8fafc',
    true,
    true,
    false,
    mc.Id,
    CURRENT_TIMESTAMP,
    '["Rastgele sayı üretimi", "Özelleştirilebilir mesajlar", "Responsive tasarım", "Kolay kurulum", "Ticimax uyumlu", "Performans odaklı"]',
    '{"isActive": true, "viewersMin": 15, "viewersMax": 45, "followersMin": 5, "followersMax": 20, "buyersMin": 2, "buyersMax": 8, "updateInterval": 60, "displayDuration": 5, "textTemplates": {"viewersTemplate": "{count} kişi şu anda bu ürünü inceliyor", "followersTemplate": "{count} kişi bu ürünü takip ediyor", "buyersTemplate": "{count} kişi satın almaya hazırlanıyor"}, "displaySettings": {"position": "bottom-left", "primaryColor": "#007bff", "textColor": "#ffffff", "backgroundColor": "#333333", "borderRadius": 8, "fontSize": 14, "enableAnimation": true, "enableShadow": true}}'
FROM "ModuleCategories" mc 
WHERE mc."Name" = 'E-ticaret Araçları'
AND NOT EXISTS (
    SELECT 1 FROM "Modules" m WHERE m."Name" = 'Social Proof'
);

-- Eğer E-ticaret Araçları kategorisi yoksa, önce onu oluştur
INSERT INTO "ModuleCategories" ("Name", "Description", "IconClass", "IsActive", "SortOrder", "CreatedAt")
SELECT 
    'E-ticaret Araçları',
    'E-ticaret sitenizi güçlendiren araçlar',
    'fas fa-shopping-cart',
    true,
    3,
    CURRENT_TIMESTAMP
WHERE NOT EXISTS (
    SELECT 1 FROM "ModuleCategories" mc WHERE mc."Name" = 'E-ticaret Araçları'
);

-- Sonra Social Proof modülünü tekrar dene (kategori yoksa oluşturulduktan sonra)
INSERT INTO "Modules" ("Name", "Description", "DetailedDescription", "Price", "IconClass", "IconColor", "BackgroundColor", "IsActive", "IsNew", "IsFeatured", "CategoryId", "CreatedAt", "Features", "DefaultSettings")
SELECT 
    'Social Proof',
    'Ürün sayfalarında sosyal kanıt göstererek satışları artırın',
    'Social Proof modülü, ürün sayfalarında "X kişi şu anda bu ürünü inceliyor" gibi mesajlar göstererek müşterilerin satın alma motivasyonunu artırır. Rastgele ama mantıklı sayılar kullanarak gerçek veri takibi gerektirmeden etkili sosyal kanıt sunar.',
    99.99,
    'fas fa-users',
    '#6366f1',
    '#f8fafc',
    true,
    true,
    false,
    mc.Id,
    CURRENT_TIMESTAMP,
    '["Rastgele sayı üretimi", "Özelleştirilebilir mesajlar", "Responsive tasarım", "Kolay kurulum", "Ticimax uyumlu", "Performans odaklı"]',
    '{"isActive": true, "viewersMin": 15, "viewersMax": 45, "followersMin": 5, "followersMax": 20, "buyersMin": 2, "buyersMax": 8, "updateInterval": 60, "displayDuration": 5, "textTemplates": {"viewersTemplate": "{count} kişi şu anda bu ürünü inceliyor", "followersTemplate": "{count} kişi bu ürünü takip ediyor", "buyersTemplate": "{count} kişi satın almaya hazırlanıyor"}, "displaySettings": {"position": "bottom-left", "primaryColor": "#007bff", "textColor": "#ffffff", "backgroundColor": "#333333", "borderRadius": 8, "fontSize": 14, "enableAnimation": true, "enableShadow": true}}'
FROM "ModuleCategories" mc 
WHERE mc."Name" = 'E-ticaret Araçları'
AND NOT EXISTS (
    SELECT 1 FROM "Modules" m WHERE m."Name" = 'Social Proof'
);
