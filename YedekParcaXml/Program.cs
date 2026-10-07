using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// XmlWriter için
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

builder.Services.AddHttpClient("BasbugApi", client =>
{
    client.BaseAddress = new Uri("https://api.basbug.com.tr/");
    client.Timeout = TimeSpan.FromSeconds(120);
});

var app = builder.Build();

app.MapGet("/urunler", async (HttpContext ctx, string kod, string? depolar, IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrWhiteSpace(kod))
        return Results.BadRequest("Kod gereklidir.");

    var depoList = ParseDepolar(depolar);

    try
    {
        var httpClient = httpClientFactory.CreateClient("BasbugApi");

        // LOGIN
        var loginPayload = new
        {
            KullaniciAdi = "MS11906",
            Parola = "wW8WuyyıkVbIibIh7d",
            ClientSecret = "W2wOU8V6w3eSWfo6sxi2CThf1g9EYmZ3",
            ClientID = "materialApi"
        };

        string? token;

        using (var loginContent = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json"))
        using (var loginResponse = await httpClient.PostAsync("auth/Login", loginContent))
        {
            loginResponse.EnsureSuccessStatusCode();
            await using var loginStream = await loginResponse.Content.ReadAsStreamAsync();
            using var loginDoc = await JsonDocument.ParseAsync(loginStream);
            token = loginDoc.RootElement.GetProperty("token").GetString();
        }

        if (string.IsNullOrWhiteSpace(token))
            return Results.Problem("Token alınamadı.");

        var urunList = new List<UrunItem>();
        var stokList = new List<StokItem>();

        // ÜRÜNLER
        var urunUrl = $"material/MalzemeleriGetir?FirmaAdi=BASBUG&ListeGrubu={kod}";
        using (var req = new HttpRequestMessage(HttpMethod.Get, urunUrl))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var res = await httpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            foreach (var x in doc.RootElement.GetProperty("malzemeListesi").EnumerateArray())
            {
                urunList.Add(new UrunItem(
                    x.GetProperty("no").GetString(),
                    x.GetProperty("ac").GetString(),
                    KategoriGrupla(x.GetProperty("ac").GetString()),
                    x.TryGetProperty("ac2", out var ac2) ? ac2.GetString() : null,
                    x.TryGetProperty("mkk", out var mkk) ? mkk.GetString() : null,
                    x.TryGetProperty("oe", out var oe) ? oe.GetString() : null,
                    x.TryGetProperty("uk", out var uk) ? uk.GetString() : null,
                    x.TryGetProperty("lgk", out var lgk) ? lgk.GetString() : null,
                    x.TryGetProperty("m", out var m) ? m.GetString() : null,
                    x.TryGetProperty("mo", out var mo) ? mo.GetString() : null,
                    x.TryGetProperty("y", out var y) ? y.GetString() : null,
                    x.TryGetProperty("b", out var b) ? b.GetString() : null,
                    x.TryGetProperty("dc", out var dc) ? dc.GetString() : null,
                    x.TryGetProperty("lf", out var lf) && lf.ValueKind == JsonValueKind.Number ? lf.GetDouble() : 0,
                    x.TryGetProperty("nf", out var nf) ? nf.GetString() : null,
                    "",
                    ""
                ));
            }
        }

        // STOK
        foreach (var depo in depoList)
        {
            var stokUrl = $"material/StokGetir?FirmaAdi=BASBUG&ListeGrubu={kod}&Depo={depo}";
            using var req = new HttpRequestMessage(HttpMethod.Get, stokUrl);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var res = await httpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            foreach (var x in doc.RootElement.GetProperty("stokListesi").EnumerateArray())
            {
                stokList.Add(new StokItem(
                    x.GetProperty("no").GetString(),
                    x.GetProperty("stok").GetInt32(),
                    depo
                ));
            }
        }

        var stokDict = stokList
            .GroupBy(s => s.No)
            .ToDictionary(g => g.Key, g => new
            {
                Stok = g.Sum(x => x.Stok),
                Depo = string.Join(",", g.Where(x => x.Stok > 0).Select(x => x.SDepo).Distinct())
            });

        var joined = urunList
            .Select(u =>
            {
                stokDict.TryGetValue(u.No, out var s);
                return new JoinedItem(
                    u.No, u.Ac, u.AcGrup, u.Ac2, u.Mkk, u.Oe, u.Uk, u.Lgk,
                    u.M, u.Mo, u.Y, u.B, u.Dc, u.Lf,
                    s?.Stok ?? 0,
                    s?.Depo,
                    u.Nf, u.Ackl, u.Anht
                );
            })
            .Where(x => x.Stok > 0)
            .ToList();

        ctx.Response.ContentType = "text/xml; charset=utf-8";

        using var writer = XmlWriter.Create(ctx.Response.Body);

        writer.WriteStartDocument();
        writer.WriteStartElement("Urunler");

        foreach (var j in joined)
        {
            var modeller = ModelParcala(j.M);

            if (modeller.Count == 0)
                modeller.Add(j.M ?? "");

            foreach (var model in modeller)
            {
                writer.WriteStartElement("Urun");

                writer.WriteElementString("No", j.No);
                writer.WriteElementString("Lgk", j.Lgk);
                writer.WriteElementString("M", model); // 🔥 PARÇALANMIŞ MODEL
                writer.WriteElementString("Ac", j.AcGrup);
                writer.WriteElementString("AcOrijinal", j.Ac);
                writer.WriteElementString("Stok", j.Stok.ToString());
                writer.WriteElementString("Depo", j.SDepo);

                writer.WriteEndElement();
            }
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();

        return Results.Empty;
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();

// MODEL PARÇALAMA
static List<string> ModelParcala(string? m)
{
    if (string.IsNullOrWhiteSpace(m))
        return new List<string>();

    return m
        .Split(new[] { '-', '/' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim())
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct()
        .ToList();
}

// KATEGORİ
static string KategoriGrupla(string? ac)
{
    if (string.IsNullOrWhiteSpace(ac)) return "DİĞER";

    var s = ac.ToUpper();

    if (s.Contains("FREN")) return "FREN SİSTEMİ";
    if (s.Contains("YAKIT")) return "YAKIT SİSTEMİ";
    if (s.Contains("YAĞ")) return "YAĞ SİSTEMİ";

    return ac;
}

// HELPERS
static string[] ParseDepolar(string? depolarQuery)
{
    var defaults = new[] { "MRK", "IAN", "TRK", "IZM", "KYS" };
    if (string.IsNullOrWhiteSpace(depolarQuery)) return defaults;

    return depolarQuery.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

// RECORDS
record UrunItem(string? No, string? Ac, string? AcGrup, string? Ac2, string? Mkk, string? Oe, string? Uk, string? Lgk, string? M, string? Mo, string? Y, string? B, string? Dc, double Lf, string? Nf, string? Ackl, string? Anht);
record StokItem(string? No, int Stok, string? SDepo);
record JoinedItem(string? No, string? Ac, string? AcGrup, string? Ac2, string? Mkk, string? Oe, string? Uk, string? Lgk, string? M, string? Mo, string? Y, string? B, string? Dc, double Lf, int Stok, string? SDepo, string? Nf, string? Ackl, string? Anht);
