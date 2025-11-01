using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtoEntegre.Api.DTOs;
using OtoEntegre.Api.Services;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Data;
using OtoEntegre.Api.Converters;
using System.Text.Json;
using OtoEntegre.Api.Repositories; // Siparisler ve TrendyolOrderDto burada olmalı
using System.Text;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.IO;

namespace OtoEntegre.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SiparislerController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly EntegrasyonService _entegrasyonService;
        private readonly TedarikService _tedarikService;
        private readonly TelegramService _telegramService;
        private readonly TrendyolService _trendyolService;

        private readonly IGenericRepository<Siparisler> _repo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _appDbContext;
        private readonly PdfLabelService _pdfLabelService;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderSyncBackgroundService> _logger;
        private readonly IHubContext<OrderHub> _orderHub;
        private readonly OtostickerService _otostickerService;
        public SiparislerController(OtostickerService otostickerService, IHubContext<OrderHub> orderHub, IConfiguration configuration, EntegrasyonService entegrasyonService, ILogger<OrderSyncBackgroundService> logger,
            IWebHostEnvironment env,
            PdfLabelService pdfLabelService,
            AppDbContext appDbContext, TelegramService telegramService, TedarikService tedarikService, TrendyolService trendyolService, IHttpClientFactory httpClientFactory, OrderService orderService, IGenericRepository<Siparisler> repo)
        {
            _repo = repo;
            _logger = logger;
            _orderHub = orderHub;
            _otostickerService = otostickerService;
            _appDbContext = appDbContext;
            _trendyolService = trendyolService;
            _tedarikService = tedarikService;
            _telegramService = telegramService;
            _orderService = orderService;
            _entegrasyonService = entegrasyonService;
            _httpClientFactory = httpClientFactory;
            _pdfLabelService = pdfLabelService;
            _env = env;
            _configuration = configuration;
        }

        [HttpGet("urun-kar-zarar")]
        public async Task<IActionResult> UrunKarZarar([FromQuery] Guid? urunId = null, [FromQuery] Guid? kullaniciId = null, [FromQuery] bool siparisBazli = false)
        {
            // ✅ Sipariş ürünlerini tek seferde çek
            var siparisUrunleriQuery = _appDbContext.SiparisUrunleri
                .Include(su => su.Urun)
                .Include(su => su.Siparis)
                .AsQueryable();

            if (urunId.HasValue)
                siparisUrunleriQuery = siparisUrunleriQuery.Where(su => su.Urun_Id == urunId.Value);

            if (kullaniciId.HasValue)
                siparisUrunleriQuery = siparisUrunleriQuery.Where(su => su.Siparis.KullaniciId == kullaniciId.Value);

            var siparisUrunleri = await siparisUrunleriQuery.ToListAsync();


            var siparisBazliList = new List<object>();
            foreach (var su in siparisUrunleri)
            {
                decimal trendyolBirimFiyat = su.Toplam_Fiyat / su.Adet;
                var keywords = ExtractKeywords(su.Urun.Ad);
                decimal otostickerFiyat = 0;
                decimal karZarar = 0;
                int eslesmeSkoru = 0;
                string eslesmeDurumu = "Eşleşmedi";

                var otostickerMatch = await FindBestMatchByCategoryAsync(su.Urun.Ad, keywords);

                if (otostickerMatch != null)
                {
                    otostickerFiyat = otostickerMatch.SalePrice;
                    eslesmeSkoru = CalculateMatchScore(su.Urun.Ad, otostickerMatch.Name);
                    eslesmeDurumu = eslesmeSkoru >= 25 ? "Mükemmel" :
                                   eslesmeSkoru >= 15 ? "İyi" :
                                   eslesmeSkoru >= 8 ? "Orta" : "Zayıf";
                }

                karZarar = (trendyolBirimFiyat - otostickerFiyat) * su.Adet;

                siparisBazliList.Add(new
                {
                    urunresmi = su.Urun.Image,
                    UrunId = su.Urun_Id,
                    UrunAdi = su.Urun.Ad,
                    su.Adet,
                    TrendyolFiyat = trendyolBirimFiyat,
                    OtostickerFiyat = otostickerFiyat,
                    KarZarar = karZarar,
                    KarZararYuzde = otostickerFiyat > 0 ? ((trendyolBirimFiyat - otostickerFiyat) / otostickerFiyat) * 100 : 0,
                    SiparisTarihi = su.Siparis.CreatedAt,
                    SiparisNo = su.Siparis.SiparisNumarasi,
                    TrendyolEslesti = true,
                    OtostickerEslesti = otostickerMatch != null,
                    EslesmeSkoru = eslesmeSkoru,
                    EslesmeDurumu = eslesmeDurumu,
                    OtostickerAdi = otostickerMatch?.Name ?? "Eşleşen ürün bulunamadı"
                });
            }
            // ✅ Sipariş Bazlı Liste - Gelişmiş


            // ✅ Ürün Bazlı Gruplanmış İstatistik - Gelişmiş
            var urunBazli = siparisUrunleri
                .GroupBy(su => new { su.Urun_Id, su.Urun.Ad })
                .Select(g =>
                {
                    int toplamAdet = g.Sum(x => x.Adet);
                    decimal toplamCiro = g.Sum(x => x.Toplam_Fiyat);
                    decimal ortalamaFiyat = toplamCiro / toplamAdet;
                    DateTime sonSatis = g.Max(x => x.Siparis.CreatedAt);

                    // Otosticker fiyatını bulmak için ilk kayıttaki anahtar kelimeleri al
                    var keywords = ExtractKeywords(g.First().Urun.Ad);
                    var otostickerProducts = GetOtostickerProductsByKeywordsAsync(keywords).Result;
                    var otostickerMatch = FindBestMatch(g.First().Urun.Ad, otostickerProducts);
                    decimal otostickerFiyat = otostickerMatch?.SalePrice ?? 0;
                    int eslesmeSkoru = otostickerMatch != null ? CalculateMatchScore(g.First().Urun.Ad, otostickerMatch.Name) : 0;

                    decimal toplamKar = (ortalamaFiyat - otostickerFiyat) * toplamAdet;
                    decimal karYuzdesi = otostickerFiyat > 0 ? ((ortalamaFiyat - otostickerFiyat) / otostickerFiyat) * 100 : 0;

                    return new
                    {
                        UrunId = g.Key.Urun_Id,
                        UrunAdi = g.Key.Ad,
                        ToplamSatilanAdet = toplamAdet,
                        ToplamSiparisSayisi = g.Count(),
                        OrtalamaTrendyolFiyati = ortalamaFiyat,
                        ToplamCiro = toplamCiro,
                        OtostickerFiyat = otostickerFiyat,
                        ToplamKarZarar = toplamKar,
                        KarYuzdesi = karYuzdesi,
                        SonSatisTarihi = sonSatis,
                        EslesmeSkoru = eslesmeSkoru,
                        OtostickerEslesti = otostickerMatch != null,
                        OtostickerAdi = otostickerMatch?.Name ?? "Eşleşen ürün bulunamadı"
                    };
                }).ToList();

            // ✅ SON JSON ÇIKTI
            if (siparisBazli)
            {
                return Ok(new
                {
                    siparisBazli = siparisBazliList.Cast<object>().ToList(),
                    urunBazli = new List<object>()
                });
            }
            else
            {
                return Ok(new
                {
                    siparisBazli = new List<object>(),
                    urunBazli = urunBazli.Cast<object>().ToList()
                });
            }

        }

        public class OtostickerUserOrderRequest
        {
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
            public OtostickerFastSaleOrderRequest Order { get; set; } = new();
        }

        public class OtostickerFastSaleOrderRequest
        {
            public List<Guid> SiparisIdList { get; set; } = new();
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] OtostickerLoginDto dto)
        {
            var result = await _otostickerService.LoginAsync(dto.Email, dto.Password);
            return Ok(result);
        }

        public class OtostickerSearchDto
        {
            [JsonPropertyName("email")]
            public string Email { get; set; } = "";

            [JsonPropertyName("password")]
            public string Password { get; set; } = "";

            [JsonPropertyName("productName")]
            public string ProductName { get; set; } = "";
        }



        [HttpPost("search")]
        public async Task<IActionResult> SearchProduct([FromBody] OtostickerSearchDto dto)
        {
            if (dto == null) return BadRequest("DTO boş geldi!");
            Console.WriteLine($"🔎 Otosticker ürün arama başladı... {dto.Email} - {dto.ProductName}");

            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password) || string.IsNullOrEmpty(dto.ProductName))
                return BadRequest("Email, şifre ve ürün adı zorunludur.");

            var result = await _otostickerService.SearchProductAsync(dto.Email, dto.Password, dto.ProductName);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result.Products);
        }

        public record OtostickerLoginDto(string Email, string Password);
        public record OtostickerLoginResponse(bool Success, string Token, string Message);

        public class OtostickerLoginSearchDto
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
        }

        // ✅ Gelişmiş normalize function
        private string NormalizeForMatch(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.ToLower()
                       .Replace("-", " ")
                       .Replace("_", " ")
                       .Replace(",", " ")
                       .Replace(".", " ")
                       .Replace("(", " ")
                       .Replace(")", " ")
                       .Replace("[", " ")
                       .Replace("]", " ")
                       .Replace("'", " ")
                       .Replace("\"", " ")
                       .Replace("  ", " ")
                       .Trim();
        }

        // ✅ Gelişmiş anahtar kelime çıkarıcı
        private List<string> ExtractKeywords(string urunAdi)
        {
            if (string.IsNullOrEmpty(urunAdi)) return new List<string>();

            var normalized = NormalizeForMatch(urunAdi);

            // Genişletilmiş ignore listesi
            var ignore = new[] {
                "ve", "ile", "bir", "set", "paket", "adet", "li", "lü", "lu", "5li", "4lü", "3lü", "2li", "1li",
                "cm", "mm", "kg", "gr", "ml", "lt", "m", "metre", "santimetre", "milimetre",
                "renk", "renkli", "siyah", "beyaz", "kırmızı", "mavi", "yeşil", "sarı", "mor", "turuncu",
                "büyük", "küçük", "orta", "xl", "l", "m", "s", "xs", "xxl", "xxxl",
                "erkek", "kadın", "çocuk", "bebek", "unisex",
                "yeni", "eski", "orijinal", "kaliteli", "premium", "standart",
                "için", "olan", "gibi", "kadar", "daha", "çok", "az", "en", "çok", "az",
                "uyumlu", "araç", "araba", "oto", "one", "size", "fc5", "sd3", "kor", "0321"
            };

            var keywords = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                           .Where(k => k.Length > 2 && !ignore.Contains(k, StringComparer.OrdinalIgnoreCase))
                           .ToList();

            // Eğer çok az kelime varsa, daha esnek ol
            if (keywords.Count <= 2)
            {
                keywords = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                           .Where(k => k.Length > 1 && !ignore.Contains(k, StringComparer.OrdinalIgnoreCase))
                           .ToList();
            }

            return keywords.Take(5).ToList(); // En fazla 5 anahtar kelime
        }

        // ✅ Ürün eşleştirme skorlaması - Gelişmiş
        private int CalculateMatchScore(string trendyolAd, string otostickerAd)
        {
            if (string.IsNullOrEmpty(trendyolAd) || string.IsNullOrEmpty(otostickerAd))
                return 0;

            var trendyolKeywords = ExtractKeywords(trendyolAd);
            var otostickerKeywords = ExtractKeywords(otostickerAd);

            if (!trendyolKeywords.Any() || !otostickerKeywords.Any())
                return 0;

            int score = 0;
            int totalKeywords = trendyolKeywords.Count;

            // Kısa ürün adları için özel işlem
            if (otostickerKeywords.Count <= 2)
            {
                // Otosticker'daki her kelime için Trendyol'da arama yap
                foreach (var otostickerKeyword in otostickerKeywords)
                {
                    // Tam eşleşme
                    if (trendyolKeywords.Any(k => k.Equals(otostickerKeyword, StringComparison.OrdinalIgnoreCase)))
                    {
                        score += 15; // Kısa ürünler için daha yüksek skor
                    }
                    // Kısmi eşleşme (içeriyor)
                    else if (trendyolKeywords.Any(k => k.Contains(otostickerKeyword, StringComparison.OrdinalIgnoreCase) ||
                                                      otostickerKeyword.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    {
                        score += 10;
                    }
                    // Levenshtein distance ile benzerlik
                    else
                    {
                        var bestMatch = trendyolKeywords
                            .Select(k => CalculateLevenshteinDistance(otostickerKeyword, k))
                            .Min();

                        if (bestMatch <= 1) // 1 karakter farkına kadar kabul et
                        {
                            score += 8;
                        }
                        else if (bestMatch <= 2)
                        {
                            score += 5;
                        }
                    }
                }
            }
            else
            {
                // Normal uzunluktaki ürünler için mevcut algoritma
                foreach (var keyword in trendyolKeywords)
                {
                    // Tam eşleşme
                    if (otostickerKeywords.Any(k => k.Equals(keyword, StringComparison.OrdinalIgnoreCase)))
                    {
                        score += 10;
                    }
                    // Kısmi eşleşme (içeriyor)
                    else if (otostickerKeywords.Any(k => k.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                                        keyword.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    {
                        score += 5;
                    }
                    // Levenshtein distance ile benzerlik
                    else
                    {
                        var bestMatch = otostickerKeywords
                            .Select(k => CalculateLevenshteinDistance(keyword, k))
                            .Min();

                        if (bestMatch <= 2) // 2 karakter farkına kadar kabul et
                        {
                            score += 3;
                        }
                    }
                }
            }

            return score;
        }

        // ✅ Levenshtein distance hesaplama
        private int CalculateLevenshteinDistance(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
            if (string.IsNullOrEmpty(s2)) return s1.Length;

            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            return d[s1.Length, s2.Length];
        }

        // ✅ Cache için static dictionary
        private static readonly Dictionary<string, List<OtostickerProductDto>> _otostickerCache = new();
        private static readonly object _cacheLock = new object();

        // ✅ Gelişmiş Otosticker API çağrısı
        private async Task<List<OtostickerProductDto>> GetOtostickerProductsByKeywordsAsync(List<string> keywords)
        {
            if (keywords == null || !keywords.Any()) return new List<OtostickerProductDto>();

            var cacheKey = string.Join("|", keywords.OrderBy(k => k));

            lock (_cacheLock)
            {
                if (_otostickerCache.ContainsKey(cacheKey))
                {
                    return _otostickerCache[cacheKey];
                }
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apiKey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apiSecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");

            var searchName = Uri.EscapeDataString(string.Join(' ', keywords));
            var url = $"https://www.otosticker.com.tr/api/v2/product/lists?name={searchName}&pageSize=50"; // pageSize arttırıldı eşleşme için

            try
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return new List<OtostickerProductDto>();

                var json = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonSerializer.Deserialize<OtostickerResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var products = data?.Result?.List ?? new List<OtostickerProductDto>();

                // Cache'e ekle
                lock (_cacheLock)
                {
                    if (_otostickerCache.Count > 200) // cache limit biraz arttırıldı
                    {
                        _otostickerCache.Clear();
                    }
                    _otostickerCache[cacheKey] = products;
                }

                return products;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Otosticker API hatası: {ex.Message}");
                return new List<OtostickerProductDto>();
            }
        }
        // ✅ En iyi eşleşmeyi bulan metod
        private async Task<OtostickerProductDto?> FindBestMatchByCategoryAsync(string trendyolAd, List<string> keywords)
        {
            if (string.IsNullOrWhiteSpace(trendyolAd)) return null;
            Console.WriteLine($"🔍 En iyi eşleşme aranıyor: {trendyolAd}");
            // 1) Önce birleşik anahtar kelimelerle ara
            var products = await GetOtostickerProductsByKeywordsAsync(keywords);

            // 2) Eğer dönüş yoksa, tek tek kelimelerle dene (daha esnek arama)
            if (!products.Any() && keywords.Any())
            {
                var accum = new List<OtostickerProductDto>();
                foreach (var k in keywords)
                {
                    var p = await GetOtostickerProductsByKeywordsAsync(new List<string> { k });
                    if (p != null && p.Any())
                    {
                        accum.AddRange(p);
                    }
                }
                products = accum.DistinctBy(p => p.Name).ToList(); // aynı isimleri tekilleştir
            }

            if (!products.Any())
            {
                // Son çare: boş döndür (eşleşme yok)
                return null;
            }

            // 3) Dönen ürünleri Category1'e göre grupla ve her kategori için "max match score" hesapla
            var groupsByCat = products
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Category1) ? "__UNKNOWN__" : p.Category1!.Trim().ToLowerInvariant())
                .Select(g => new
                {
                    Category = g.Key,
                    Products = g.ToList(),
                    MaxScore = g.Max(p => CalculateMatchScore(trendyolAd, p.Name))
                })
                .OrderByDescending(x => x.MaxScore)
                .ToList();

            // 4) En iyi kategori (en yüksek MaxScore) seç
            var bestCategoryGroup = groupsByCat.FirstOrDefault();

            List<OtostickerProductDto> candidates;
            if (bestCategoryGroup != null && bestCategoryGroup.MaxScore > 0)
            {
                // Kategori içinde en iyi ürünü seç
                candidates = bestCategoryGroup.Products;
            }
            else
            {
                // Eğer kategorilerde hiç pozitif skor yoksa yine tüm ürünleri candidate yap
                candidates = products;
            }

            // 5) Aday ürünler arasında skorlara göre sırala
            var scored = candidates
                .Select(p => new
                {
                    Product = p,
                    Score = CalculateMatchScore(trendyolAd, p.Name)
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            // 6) Eğer hiç pozitif skor yoksa, fallback: kelime-benzerliğine göre (Levenshtein) en yakını al
            var top = scored.FirstOrDefault();
            if (top != null && top.Score > 0)
                return top.Product;

            // fallback: hesapla minimal Levenshtein distance between trendyol keywords and product name keywords
            var trendyolKeywords = ExtractKeywords(trendyolAd);
            OtostickerProductDto? bestByDistance = null;
            int bestDist = int.MaxValue;

            foreach (var p in candidates)
            {
                var pKeywords = ExtractKeywords(p.Name);
                if (!pKeywords.Any()) continue;

                // en küçük mesafeyi bul (her kw çiftine bak)
                int dist = int.MaxValue;
                foreach (var tk in trendyolKeywords)
                {
                    foreach (var pk in pKeywords)
                    {
                        var d = CalculateLevenshteinDistance(tk, pk);
                        if (d < dist) dist = d;
                    }
                }

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestByDistance = p;
                }
            }

            // Eğer hala null değilse onu döndür, yoksa listedeki en kısa isimli ürünü döndür
            if (bestByDistance != null)
                return bestByDistance;

            return products.OrderBy(p => p.Name.Length).FirstOrDefault();
        }
        private OtostickerProductDto? FindBestMatch(string trendyolAd, List<OtostickerProductDto> otostickerProducts)
        {
            if (!otostickerProducts.Any()) return null;

            var scoredProducts = otostickerProducts
                .Select(p => new
                {
                    Product = p,
                    Score = CalculateMatchScore(trendyolAd, p.Name)
                })
                .Where(x => x.Score > 0) // Sadece pozitif skorlu ürünler
                .OrderByDescending(x => x.Score)
                .ToList();

            return scoredProducts.FirstOrDefault()?.Product;
        }

        // Otosticker DTO
        public class OtostickerResponse
        {
            public OtostickerResult? Result { get; set; }
        }

        public class OtostickerResult
        {
            public List<OtostickerProductDto> List { get; set; } = new();
        }

        public class OtostickerProductDto
        {
            public string Name { get; set; } = string.Empty;
            public decimal SalePrice { get; set; }

            // API döndürüyorsa kategori alanları
            public string? Category1 { get; set; }
            public string? Category2 { get; set; }
            public string? Category3 { get; set; }
            public string? Category4 { get; set; }
        }


        [HttpGet("dealer-by-user")]
        public async Task<IActionResult> GetDealerByUserEmail(string userEmail)
        {
            try
            {
                // 1. Veritabanından kullanıcı email'ini çek
                var user = await _appDbContext.Kullanicilar
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return NotFound("Kullanıcı bulunamadı");

                // 2. Otosticker bayi listesini çek
                var dealerListResponse = await _otostickerService.GetDealerListAsync();

                if (dealerListResponse?.Result?.List == null)
                    return NotFound("Bayi listesi alınamadı");

                var dealer = dealerListResponse.Result.List
                    .FirstOrDefault(d => d.Email.Trim().Equals(userEmail.Trim(), StringComparison.OrdinalIgnoreCase));

                if (dealer == null)
                    return NotFound("Bayi bulunamadı");

                // 3. Bayi bilgilerini console'a yazdır (balance dahil)
                Console.WriteLine($"Aranan email: {user.Email}, Bulunan bayi: {dealer.Email}, Balance: {dealer.Balance}");

                // 4. Response olarak balance dahil döndür
                return Ok(new
                {
                    dealer.Id,
                    dealer.Email,
                    dealer.Name,
                    dealer.Lastname,
                    dealer.Title,
                    dealer.Status,
                    dealer.Balance // <- burada balance da dahil
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        // [HttpPost("create-fast-sale")]
        // public async Task<IActionResult> CreateFastSaleByUserEmail(string userEmail)
        // {
        //     try
        //     {
        //         // 1️⃣ Kullanıcıyı veritabanında bul
        //         var user = await _appDbContext.Kullanicilar
        //             .FirstOrDefaultAsync(u => u.Email == userEmail);

        //         if (user == null)
        //             return NotFound("Kullanıcı bulunamadı");

        //         // 2️⃣ Otosticker bayi bilgilerini çek
        //         var dealerListResponse = await _otostickerService.GetDealerListAsync();
        //         var dealer = dealerListResponse?.Result?.List?
        //             .FirstOrDefault(d => d.Email.Trim().Equals(userEmail.Trim(), StringComparison.OrdinalIgnoreCase));

        //         if (dealer == null)
        //             return NotFound("Kullanıcıya ait bayi hesabı bulunamadı");

        //         // 3️⃣ Veritabanındaki siparişleri al
        //         var siparisler = await _appDbContext.Siparisler
        //             .Include(s => s.SiparisUrunleri)
        //             .Where(s => s.KullaniciId == user.Id)
        //             .ToListAsync();

        //         if (siparisler.Count == 0)
        //             return NotFound("Kullanıcının siparişi bulunamadı");

        //         foreach (var siparis in siparisler)
        //         {
        //             var fastSaleRequest = new
        //             {
        //                 customer = new
        //                 {
        //                     name = $"{user.Ad}",
        //                     email = user.Email,
        //                     phone = user.Telefon
        //                 },
        //                 order = new
        //                 {
        //                     paymentType = 0, // hızlı satış
        //                     status = 1, // yeni sipariş
        //                     products = siparis.SiparisUrunleri.Select(u => new
        //                     {
        //                         id = u.Id,
        //                         price = u.Toplam_Fiyat,
        //                         quantity = u.Adet,
        //                     }).ToList()
        //                 }
        //             };

        //             // 4️⃣ Otosticker’a gönder
        //             var result = await _otostickerService.CreateFastSaleAsync(fastSaleRequest, dealer);
        //         }

        //         return Ok("Tüm siparişler Otosticker'a gönderildi.");
        //     }
        //     catch (Exception ex)
        //     {
        //         return BadRequest(ex.Message);
        //     }
        // }


        // [HttpPost("otosticker-satin-al")]
        // public async Task<IActionResult> SatinAlOtostickerdan([FromBody] OtostickerUserOrderRequest dto)
        // {
        //     if (dto == null || dto.Order == null)
        //         return BadRequest("Eksik parametre.");

        //     var (email, password) = (dto.Email, dto.Password);
        //     Console.WriteLine($"email - şifre: {email} - {password}");

        //     // 1️⃣ Otosticker login kontrolü
        //     var loginResult = await _otostickerService.LoginAsync(email, password);
        //     if (!loginResult.Success)
        //     {
        //         Console.WriteLine($"❌ Oturum açılamadı: {loginResult}");
        //         return BadRequest($"Oturum açılamadı: {loginResult.Message}");
        //     }

        //     var siparisler = await _appDbContext.Siparisler
        //         .Include(s => s.SiparisUrunleri)
        //         .ThenInclude(su => su.Urun)
        //         .Where(s => dto.Order.SiparisIdList.Contains(s.Id))
        //         .ToListAsync();

        //     if (!siparisler.Any())
        //         return NotFound("Belirtilen sipariş(ler) bulunamadı.");

        //     var results = new List<object>();

        //     foreach (var siparis in siparisler)
        //     {
        //         foreach (var urun in siparis.SiparisUrunleri)
        //         {
        //             Console.WriteLine($"🔎 Otosticker'da ürün aranıyor: {urun.Urun.Ad}");
        //             var searchResult = await _otostickerService.SearchProductAsync(email, password, urun.Urun.Ad);

        //             if (!searchResult.Success || searchResult.Products == null || !searchResult.Products.Any())
        //             {
        //                 results.Add(new
        //                 {
        //                     SiparisNo = siparis.SiparisNumarasi,
        //                     Urun = urun.Urun.Ad,
        //                     Durum = "❌ Ürün bulunamadı"
        //                 });
        //                 continue;
        //             }

        //             // 3️⃣ En uygun eşleşmeyi seç (ilkini alıyoruz)
        //             var bestMatch = searchResult.Products.First();
        //             int productId = Convert.ToInt32(bestMatch.ProductId);

        //             // 4️⃣ Sipariş DTO’sunu oluştur
        //             var fastSaleOrder = new OtostickerFastSaleOrderDto
        //             {
        //                 Customer = new OtostickerCustomerDto
        //                 {
        //                     Name = siparis.MusteriAdSoyad ?? "Müşteri",
        //                     Lastname = "",
        //                     Email = "info@otoentegrasyon.com", // isteğe göre siparişten alabilirsin
        //                     Phone = "05555555555", // isteğe göre siparişten alabilirsin",
        //                     City = "İstanbul", // Otosticker zorunlu alanlar
        //                     Distict = "Merkez",
        //                     Address = siparis.MusteriAdres ?? "Adres belirtilmemiş"
        //                 },
        //                 Order = new OtostickerOrderDto
        //                 {
        //                     Note = $"Trendyol sipariş numarası: {siparis.SiparisNumarasi}"
        //                 },
        //                 Products = new List<OtostickerProductItemDto>
        //         {
        //             new OtostickerProductItemDto
        //             {
        //                 Id = productId,
        //                 Price = bestMatch.SalePrice,
        //                 Quantity = urun.Adet,
        //                 Variant1 = ""
        //             }
        //         }
        //             };

        //             Console.WriteLine($"🛒 Otosticker siparişi gönderiliyor: {bestMatch.Name} ({urun.Adet} adet)");

        //             // 5️⃣ Otosticker sipariş oluşturma çağrısı
        //             var orderResult = await _otostickerService.CreateOrderAsync(email, password, fastSaleOrder);

        //             // 6️⃣ Sonucu listeye ekle
        //             results.Add(new
        //             {
        //                 SiparisNo = siparis.SiparisNumarasi,
        //                 Urun = bestMatch.Name,
        //                 Fiyat = bestMatch.SalePrice,
        //                 Adet = urun.Adet,
        //                 Durum = orderResult.Success ? "✅ Başarılı" : $"❌ {orderResult.Message}"
        //             });
        //         }
        //     }

        //     return Ok(new
        //     {
        //         Success = true,
        //         Message = "Otosticker sipariş işlemi tamamlandı.",
        //         Detaylar = results
        //     });
        // }



        [HttpPost("webhook")]
        public async Task<IActionResult> TrendyolWebhook([FromBody] TrendyolWebhookRequest request)
        {
            if (request == null)
                return BadRequest("Payload deserialize edilemedi.");
            if (request.Status == "Cancelled")
            {
                Console.WriteLine("Cancelled sipariş alındı, işlem yapılmadı.");
                return Ok(new { success = false, message = "Cancelled sipariş, işlem yapılmadı." });
            }
            Console.WriteLine("TrendyolWebhook başladı");

            if (request.Lines == null || !request.Lines.Any())
                return BadRequest("Sipariş satırı bulunamadı.");

            // Log dizini
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            // Payload log
            var rawJson = System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var payloadFilePath = Path.Combine(logDir, $"trendyol_webhook_payload_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
            await System.IO.File.WriteAllTextAsync(payloadFilePath, rawJson);

            // İlk sipariş
            var firstOrder = request;
            var firstLine = firstOrder.Lines.FirstOrDefault();
            if (firstLine == null)
                return BadRequest("Sipariş satırı bulunamadı.");

            int merchantId = request.Lines.First().MerchantId;
            var entegrasyon = await _appDbContext.Entegrasyonlar.FirstOrDefaultAsync(e => e.Seller_Id == merchantId);
            if (entegrasyon == null)
                return BadRequest("Bu sipariş için entegrasyon bulunamadı.");

            // DB entity map
            var siparis = TrendyolMapping.MapToSiparis(request, entegrasyon);
            // DB entity map
            siparis.GeldigiYer = 0;

            // Sipariş zaten var mı kontrol et
            bool siparisVarMi = await _appDbContext.Siparisler
                .AnyAsync(s => s.SiparisNumarasi == siparis.SiparisNumarasi);

            if (siparisVarMi)
            {
                Console.WriteLine($"Sipariş zaten var: {siparis.SiparisNumarasi}, DB'ye eklenmedi.");
                return Ok(new { success = false, message = "Sipariş zaten mevcut", siparisNo = siparis.SiparisNumarasi });
            }
            else
            {
                await _orderHub.Clients.All.SendAsync("ReceiveOrderNotification", new
                {
                    Id = siparis.Id,
                    Siparis = siparis,
                    Message = "Yeni sipariş!"
                });

                // Sipariş dosyaya kaydet
                var siparisJson = System.Text.Json.JsonSerializer.Serialize(siparis, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                var siparisFilePath = Path.Combine(logDir, $"trendyol_webhook_siparis_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
                await System.IO.File.WriteAllTextAsync(siparisFilePath, siparisJson);
                Console.WriteLine($"MapToSiparis sonucu dosyaya kaydedildi: {siparisFilePath}");

                if (siparis.Id == Guid.Empty)
                    siparis.Id = Guid.NewGuid();

                _appDbContext.Siparisler.Add(siparis);
                try
                {
                    await _appDbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SaveChanges hatası: {ex.Message}");
                }


                // SiparisUrunleri eklenmeli
                foreach (var line in request.Lines)
                {
                    var urun = await _appDbContext.Urunler
                        .FirstOrDefaultAsync(u => u.ProductCode == line.ProductCode);

                    if (urun == null)
                    {
                        urun = new Urunler
                        {
                            Id = Guid.NewGuid(),
                            UrunTedarikBarcode = line.Barcode,
                            Ad = line.ProductName ?? "-",
                            ProductCode = line.ProductCode
                        };
                        _appDbContext.Urunler.Add(urun);
                        await _appDbContext.SaveChangesAsync();
                    }

                    // 🔹 Trendyol’dan ürün detaylarını çek — image bilgisi dahil
var trendyolService = _trendyolService;
                    var products = await trendyolService.GetProductsByBarcodesAsync(
                        entegrasyon.Seller_Id.Value,
                        entegrasyon.Api_Key,
                        entegrasyon.Api_Secret,
                        new List<string> { line.Barcode }
                    );

                    var productData = products.FirstOrDefault(p => p.ProductCode == line.ProductCode);
                    var imageUrl = productData?.Images?.FirstOrDefault()?.Url;

                    // 🔹 Eğer resim varsa kaydet
                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        urun.Image = imageUrl; // Ürün tablosuna kaydet

                        var dosya = new SiparisDosyalari
                        {
                            Id = Guid.NewGuid(),
                            Siparis_Id = siparis.Id,
                            Dosya_Turu = "image",
                            Dosya_Url = imageUrl,
                            Created_At = DateTime.UtcNow
                        };
                        _appDbContext.SiparisDosyalari.Add(dosya);
                    }

                    // 🔹 Sipariş ürün kaydı oluştur
                    var siparisUrun = new SiparisUrunleri
                    {
                        Id = Guid.NewGuid(),
                        Siparis_Id = siparis.Id,
                        Urun_Id = urun.Id,
                        Adet = line.Quantity,
                        Birim_Fiyat = line.Price,
                        Toplam_Fiyat = line.Price * line.Quantity
                    };
                    _appDbContext.SiparisUrunleri.Add(siparisUrun);
                }

                // 🔹 En sonda kaydet
                await _appDbContext.SaveChangesAsync();


                try
                {
                    await _appDbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SaveChanges hatası: {ex.Message}");
                }
            }
            return Ok(new { success = true, id = siparis.Id });
        }

        //BAYİNİN otostickerdaki SİPARİŞLERİNİ ÇEKER
        [HttpGet("by-user/{dealerId}/all")]
        public async Task<IActionResult> GetAllOrdersByUser(int dealerId)
        {
            Console.WriteLine($"GetAllOrdersByUser başladı: dealerId={dealerId}");
            var orders = await _orderService.GetAllOrdersWithDetailsByDealerIdAsync(dealerId);
            return Ok(orders);
        }

        // Sadece sayfa sayısı ile çek
        [HttpGet("by-user/{dealerId}")]
        public async Task<IActionResult> GetOrdersByUser(int dealerId, int page = 0, int pageSize = 10, string? sort = "desc", string? sortBy = "createdAt")
        {
            try
            {
                var orders = await _orderService.GetOrdersWithDetailsByDealerIdAsync(dealerId);
                if (orders == null || !orders.Any())
                    return Ok(new List<object>());

                // Dinamik sıralama
                var orderedData = sortBy?.ToLower() switch
                {
                    "code" => sort?.ToLower() == "asc"
                        ? orders.OrderBy(o => o.Order?.Code)
                        : orders.OrderByDescending(o => o.Order?.Code),

                    "overall" => sort?.ToLower() == "asc"
                        ? orders.OrderBy(o => o.Summary?.Overall)
                        : orders.OrderByDescending(o => o.Summary?.Overall),

                    "createdat" => sort?.ToLower() == "asc"
                        ? orders.OrderBy(o => o.Order?.CreatedAt)
                        : orders.OrderByDescending(o => o.Order?.CreatedAt),

                    _ => orders.OrderByDescending(o => o.Order?.CreatedAt)
                };

                // Sıralanmış verilere sayfalama uygula
                var pagedData = orderedData.Skip(page * pageSize).Take(pageSize);

                return Ok(pagedData.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOrdersByUser failed for dealerId: {DealerId}", dealerId);
                return StatusCode(500, "Siparişler alınırken bir hata oluştu");
            }
        }

        // Toplam sipariş sayısı
        [HttpGet("by-user/{dealerId}/count")]
        public async Task<IActionResult> GetOrderCountByUser(int dealerId)
        {
            var orders = await _orderService.GetAllOrdersWithDetailsByDealerIdAsync(dealerId);
            return Ok(orders.Count);
        }

        // Toplam sipariş tutarı


        [HttpGet("by-user/{dealerId}/total-amount")]
        public async Task<IActionResult> GetTotalAmountByUser(int dealerId)
        {
            try
            {
                var orders = await _orderService.GetAllOrdersWithDetailsByDealerIdAsync(dealerId);
                var totalAmount = orders?.Sum(o => o.Summary?.Overall ?? 0) ?? 0;
                return Ok(totalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTotalAmountByUser failed for dealerId: {DealerId}", dealerId);
                return StatusCode(500, "Toplam tutar hesaplanırken bir hata oluştu");
            }
        }


        [HttpGet]
        public async Task<IEnumerable<Siparisler>> GetAll([FromQuery] string? durum = null, [FromQuery] string? sort = "desc")
        {
            Console.WriteLine("GetAll servisi başladı");
            var siparisler = await _repo.GetAllAsync();

            // Durum filtresi
            if (!string.IsNullOrEmpty(durum))
                siparisler = siparisler.Where(s => s.Durum == durum);

            // Sıralama (desc = varsayılan, asc = artan)
            siparisler = sort?.ToLower() switch
            {
                "asc" => siparisler.OrderBy(s => s.CreatedAt),
                "desc" => siparisler.OrderByDescending(s => s.CreatedAt),
                _ => siparisler.OrderByDescending(s => s.CreatedAt)
            };

            return siparisler;
        }

        [HttpPost("update-product-note")]
        public async Task<IActionResult> UpdateProductNote([FromBody] ProductNoteDto dto)
        {
            Console.WriteLine($"UpdateProductNote çağrıldı: OrderId={dto.OrderId}, ProductId={dto.ProductId}, Note={dto.Note}");

            // Hem sipariş hem ürün eşleşmeli!
            var siparisUrun = await _appDbContext.SiparisUrunleri
                .FirstOrDefaultAsync(su => su.Siparis_Id == dto.OrderId && su.Urun_Id == dto.ProductId);

            if (siparisUrun == null)
                return NotFound(new { success = false, message = "Ürün bulunamadı veya siparişe ait değil" });

            // Notu güncelle
            siparisUrun.SiparisNotu = dto.Note;
            await _appDbContext.SaveChangesAsync();

            return Ok(new { success = true, message = "Not güncellendi" });
        }

        public class ProductNoteDto
        {
            public Guid OrderId { get; set; }
            public Guid ProductId { get; set; }
            public string Note { get; set; } = "";
        }

        //VERİTABANINDAN KULLANICI BAZLI SİPARİŞLERİ ÇEKER
        [HttpGet("kullanici/{kullaniciId}")]
        public async Task<IActionResult> GetOrdersByUserFromDatabase(Guid kullaniciId,
    [FromQuery] int page = 0,
    [FromQuery] int pageSize = 0,
    [FromQuery] string? durum = null,
    [FromQuery] string? sort = "desc")
        {
            try
            {
                var query = _appDbContext.Siparisler
                    .Where(s => s.KullaniciId == kullaniciId);

                if (!string.IsNullOrEmpty(durum))
                    query = query.Where(s => s.Durum == durum);

                query = sort?.ToLower() switch
                {
                    "asc" => query.OrderBy(s => s.CreatedAt),
                    "desc" => query.OrderByDescending(s => s.CreatedAt),
                    _ => query.OrderByDescending(s => s.CreatedAt)
                };

                var totalCount = await query.CountAsync();

                if (pageSize > 0)
                    query = query.Skip(page * pageSize).Take(pageSize);

                var siparisler = await query
                    .Include(s => s.SiparisUrunleri)
                        .ThenInclude(su => su.Urun)
                    .ToListAsync();

                // Sipariş ve ürün idlerini topla
                var siparisIdler = siparisler.Select(s => s.Id).ToList();
                var urunIdler = siparisler
                    .SelectMany(s => s.SiparisUrunleri.Select(su => su.Urun_Id))
                    .Distinct()
                    .ToList();

                // Sipariş dosyalarını tek seferde al
                var siparisDosyalariDict = await _appDbContext.SiparisDosyalari
                    .Where(d => siparisIdler.Contains(d.Siparis_Id))
                    .GroupBy(d => new { d.Siparis_Id, d.Dosya_Turu })
                    .Select(g => g.FirstOrDefault())
                    .ToDictionaryAsync(d => (d.Siparis_Id, d.Dosya_Turu), d => d.Dosya_Url);

                // Ürünleri tek seferde al
                var urunImageDict = await _appDbContext.Urunler
                    .Where(u => urunIdler.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.Image);

                // Her sipariş ürünü için image alanını belirle
                foreach (var siparis in siparisler)
                {
                    foreach (var urunItem in siparis.SiparisUrunleri)
                    {
                        string? imageUrl = null;

                        // Önce siparis_dosyalari tablosuna bak
                        if (siparisDosyalariDict.TryGetValue((siparis.Id, "urun"), out var dosyaUrl) && !string.IsNullOrEmpty(dosyaUrl))
                        {
                            imageUrl = dosyaUrl;
                        }
                        else
                        {
                            // Yoksa urunler tablosundaki image alanını kullan
                            imageUrl = urunImageDict.ContainsKey(urunItem.Urun_Id) ? urunImageDict[urunItem.Urun_Id] : null;
                        }

                        urunItem.Urun.Image = imageUrl;
                    }
                }

                // Her siparişe SellerId ekle
                foreach (var siparis in siparisler)
                {
                    var sellerId = await _appDbContext.Entegrasyonlar
                        .Where(e => e.Kullanici_Id == siparis.KullaniciId)
                        .Select(e => e.Seller_Id)
                        .FirstOrDefaultAsync();

                    siparis.SellerId = sellerId;
                }

                var result = new
                {
                    Data = siparisler,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Siparişler getirilirken bir hata oluştu.", error = ex.Message });
            }
        }





        [HttpPut("trendyol/picking/{orderId}")]
        public async Task<IActionResult> SetOrderPicking(Guid orderId)
        {
            try
            {
                var siparis = await _appDbContext.Siparisler
                    .Include(s => s.SiparisUrunleri)
                    .Include(s => s.Entegrasyonlar)
                    .FirstOrDefaultAsync(s => s.Id == orderId);

                if (siparis == null) return NotFound("Sipariş bulunamadı.");

                var entegrasyon = siparis.Entegrasyonlar;
                if (entegrasyon == null)
                    return BadRequest("Trendyol entegrasyonu bulunamadı.");

                var sellerId = entegrasyon.Seller_Id;
                var packageId = siparis.PaketNumarasi;

                if (sellerId == null || string.IsNullOrEmpty(packageId))
                    return BadRequest("SellerId veya PackageId eksik.");

                var payload = new
                {
                    lines = siparis.SiparisUrunleri.Select(u => new
                    {
                        lineId = u.LineId,
                        quantity = u.Adet
                    }).ToList(),
                    @params = new { },
                    status = "Picking"
                };

                var apiKey = entegrasyon.Api_Key.Trim();
                var apiSecret = entegrasyon.Api_Secret.Trim();
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));

                var httpClient = _httpClientFactory.CreateClient();
                var url = $"https://apigw.trendyol.com/integration/order/sellers/{sellerId}/shipment-packages/{packageId}";

                var request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Headers.Add("Authorization", $"Basic {auth}");
                request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                    return Ok(new
                    {
                        success = true,
                        Siparis = siparis,  // frontend’e eski yapı gibi geri dön
                        PaketNumarasi = siparis.PaketNumarasi,
                        SellerId = sellerId
                    });
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return BadRequest(new { success = false, error = errorContent });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }


        public class PickingRequest
        {
            public List<PickingLine> Lines { get; set; }
            public object Params { get; set; } = new { };
            public string Status { get; set; } = "Picking";
        }

        public class PickingLine
        {
            public long LineId { get; set; }
            public int Quantity { get; set; }
        }


        [HttpGet("{siparisId}/urunler")]
        public async Task<IActionResult> GetSiparisUrunleri(Guid siparisId)
        {
            var siparis = await _appDbContext.Siparisler
                .FirstOrDefaultAsync(s => s.Id == siparisId);

            if (siparis == null)
                return NotFound(new { message = "Sipariş bulunamadı." });

            var urunler = await (from su in _appDbContext.SiparisUrunleri
                                 join u in _appDbContext.Urunler on su.Urun_Id equals u.Id
                                 join d in _appDbContext.SiparisDosyalari
                                     on u.Image equals d.Dosya_Url into dosyalar
                                 from d in dosyalar.DefaultIfEmpty()
                                 where su.Siparis_Id == siparisId && (d == null || d.Dosya_Turu == "image")
                                 select new
                                 {
                                     u.Id,
                                     u.Ad,
                                     u.ProductCode,
                                     Image = u.Image ?? string.Empty,
                                     su.Adet,
                                     su.Toplam_Fiyat,
                                     su.SiparisNotu
                                 }).ToListAsync();

            // Aynı ürünü tekilleştir (adet ve fiyat aynı kalacak)
            var tekUrunler = urunler
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToList();

            return Ok(new
            {
                siparisId = siparis.Id,
                SiparisNo = siparis.SiparisNumarasi,
                Urunler = tekUrunler
            });
        }



        //VERİTABANINDAN KULLANICI BAZLI SİPARİŞ SAYISINI ÇEKER
        [HttpGet("kullanici/{kullaniciId}/count")]
        public async Task<IActionResult> GetOrderCountByUserFromDatabase(Guid kullaniciId, [FromQuery] string? durum = null)
        {
            Console.WriteLine($"GetOrderCountByUserFromDatabase başladı - KullaniciId: {kullaniciId}");

            try
            {
                var query = _appDbContext.Siparisler
                    .Where(s => s.KullaniciId == kullaniciId);

                // Durum filtresi
                if (!string.IsNullOrEmpty(durum))
                    query = query.Where(s => s.Durum == durum);

                var count = await query.CountAsync();

                return Ok(new { Count = count, KullaniciId = kullaniciId, Durum = durum });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetOrderCountByUserFromDatabase hatası: {ex.Message}");
                return StatusCode(500, new { message = "Sipariş sayısı getirilirken bir hata oluştu.", error = ex.Message });
            }
        }


        //BAYİNİN ORDER ID'YE GÖRE TRENDYOLDAKİ SİPARİŞİNİ  ÇEKER

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            Console.WriteLine("GetOrderDetail servisi başladı");

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();
            return Ok(order.Result); // order.Result içinde detay var
        }


        //BAYİNİN ID'YE GÖRE VERİTABANINDAKİ SİPARİŞİNİ  ÇEKER

        [HttpGet("by-guid/{id}")]
        public async Task<ActionResult<Siparisler>> GetById(Guid id)
        {
            Console.WriteLine("GetById servisi başladı");

            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }



        [HttpPost]
        public async Task<ActionResult<Siparisler>> Create(
     SiparisCreateDto dto,
     [FromServices] TedarikService tedarikService,
     [FromServices] TelegramService telegramService,
     [FromServices] IServiceScopeFactory scopeFactory)
        {
            if (dto == null)
                return BadRequest(new { message = "Sipariş verisi boş olamaz." });

            // 1️⃣ Siparişi oluştur
            var siparis = new Siparisler
            {
                Id = Guid.NewGuid(),
                SiparisNumarasi = dto.Siparis_Numarasi,
                ToplamTutar = dto.Toplam_Tutar,
                KargoUcreti = dto.Kargo_Ucreti ?? 0,
                OdemeDurumu = dto.Odeme_Durumu ?? string.Empty,
                Kod = dto.Kod ?? string.Empty,
                Durum = dto.Durum ?? string.Empty,
                KullaniciId = dto.Kullanici_Id,
                EntegrasyonId = dto.Entegrasyon_Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TelegramSent = false
            };

            // 2️⃣ DB'ye kaydet
            await _repo.AddAsync(siparis);
            await _repo.SaveAsync();
            Console.WriteLine($"Webhook -> Sipariş KullaniciId: {siparis.KullaniciId}");

            // 3️⃣ Arka planda tedarik ve telegram gönderimini başlat
            _ = Task.Run(async () =>
         {
             try
             {
                 using var scope = scopeFactory.CreateScope();

                 var scopedRepo = scope.ServiceProvider.GetRequiredService<IGenericRepository<Siparisler>>();
                 //var scopedTedarikService = scope.ServiceProvider.GetRequiredService<TedarikService>();
                 var scopedTelegramService = scope.ServiceProvider.GetRequiredService<TelegramService>();

                 // Tedarik gönder
                 //bool tedarikSent = await scopedTedarikService.SiparisiTedarikSitesineGonder(siparis.Kod);

                 // Telegram gönder
                 string imageUrl = "https://dummyimage.com/600x400/cccccc/000000&text=Sipariş+Resmi";
                 bool telegramSent = await scopedTelegramService.SendOrderMessageAsync(siparis.KullaniciId, siparis.SiparisNumarasi, imageUrl);

                 // DB güncelle
                 var siparisToUpdate = await scopedRepo.GetByIdAsync(siparis.Id);
                 //siparisToUpdate.TedarikSent = tedarikSent;
                 if (siparisToUpdate != null)
                 {
                     siparisToUpdate.TelegramSent = telegramSent;
                     siparisToUpdate.UpdatedAt = DateTime.UtcNow;
                 }

                 await scopedRepo.SaveAsync();
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"[Sipariş Otomatik Gönderim Hatası] {ex.Message}");
             }
         });

            return CreatedAtAction(nameof(GetById), new
            {
                id = siparis.Id
            }, siparis);
        }






        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, SiparisUpdateDto dto)
        {
            var siparis = await _repo.GetByIdAsync(id);
            if (siparis == null) return NotFound();

            siparis.SiparisNumarasi = dto.Siparis_Numarasi;
            siparis.ToplamTutar = dto.Toplam_Tutar;
            siparis.KargoUcreti = dto.Kargo_Ucreti;
            siparis.OdemeDurumu = dto.Odeme_Durumu ?? string.Empty;
            siparis.Kod = dto.Kod ?? string.Empty;
            siparis.Durum = dto.Durum;
            siparis.KullaniciId = dto.Kullanici_Id;
            siparis.EntegrasyonId = dto.Entegrasyon_Id;
            siparis.UpdatedAt = DateTime.UtcNow;

            _repo.Update(siparis);
            await _repo.SaveAsync();

            return NoContent();
        }






        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();
            _repo.Delete(item);
            await _repo.SaveAsync();
            return NoContent();
        }


        [HttpPost("send-siparis-tedarik/{orderCode}")]
        public async Task<IActionResult> SendSiparisTedarik(string orderCode, [FromServices] TedarikService tedarikService)
        {

            Console.WriteLine("SendSiparisTedarik servisi başladı");

            try
            {
                await tedarikService.SiparisiTedarikSitesineGonder(orderCode);
                return Ok(new { success = true, message = "Sipariş Otosticker'a gönderildi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }









        [HttpPut("siparisler/{packageId}/kargo-firmasi")]
        public async Task<IActionResult> ChangeCargoProvider(long packageId, [FromBody] ChangeCargoProviderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CargoProvider))
                return BadRequest("Kargo firması boş olamaz.");

            var entegrasyon = await _appDbContext.Entegrasyonlar
                .FirstOrDefaultAsync(e => e.Id == request.EntegrasyonId);

            if (entegrasyon == null)
                return NotFound("Entegrasyon bulunamadı.");

            if (!entegrasyon.Seller_Id.HasValue ||
                string.IsNullOrWhiteSpace(entegrasyon.Api_Key) ||
                string.IsNullOrWhiteSpace(entegrasyon.Api_Secret))
            {
                return BadRequest("Entegrasyon bilgileri eksik veya hatalı.");
            }

            var sellerId = entegrasyon.Seller_Id.Value;
            var apiKey = entegrasyon.Api_Key.Trim();
            var apiSecret = entegrasyon.Api_Secret.Trim();
            var httpClient = _httpClientFactory.CreateClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));

            // Trendyol PUT isteği
            var putUrl = $"https://apigw.trendyol.com/integration/order/sellers/{sellerId}/shipment-packages/{packageId}/cargo-providers";
            var payload = new { cargoProvider = request.CargoProvider };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var putRequest = new HttpRequestMessage(HttpMethod.Put, putUrl)
            {
                Content = jsonContent
            };
            putRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            putRequest.Headers.Add("User-Agent", "MyAppIntegration/1.0");

            try
            {
                var putResponse = await httpClient.SendAsync(putRequest);
                var putBody = await putResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Trendyol PUT status: {(int)putResponse.StatusCode}, body: {putBody}");

                if (!putResponse.IsSuccessStatusCode)
                {
                    return StatusCode((int)putResponse.StatusCode, new
                    {
                        success = false,
                        message = "Kargo firması değiştirilemedi.",
                        trendyolResponse = putBody
                    });
                }

                // 🔹 Sipariş numarasını bulalım (packageId değil)
                var siparis = await _appDbContext.Siparisler
                    .FirstOrDefaultAsync(s => s.PaketNumarasi == packageId.ToString() && s.EntegrasyonId == request.EntegrasyonId);

                if (siparis == null)
                {
                    return NotFound("Veritabanında bu pakete ait sipariş bulunamadı.");
                }

                // 🔹 Trendyol’dan güncel sipariş bilgisini çek (orderNumber ile)
                var getUrl = $"https://apigw.trendyol.com/integration/order/sellers/{sellerId}/orders?orderNumber={siparis.SiparisNumarasi}";
                var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
                getRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                getRequest.Headers.Add("User-Agent", "MyAppIntegration/1.0");

                var getResponse = await httpClient.SendAsync(getRequest);
                var getBody = await getResponse.Content.ReadAsStringAsync();

                Console.WriteLine($"Trendyol GET status: {(int)getResponse.StatusCode}, body: {getBody}");

                if (!getResponse.IsSuccessStatusCode || string.IsNullOrWhiteSpace(getBody))
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Kargo firması Trendyol'da değiştirildi fakat güncel sipariş bilgisi alınamadı.",
                        trendyolResponse = getBody
                    });
                }

                // 🔹 JSON parse (System.Text.Json)
                using var doc = JsonDocument.Parse(getBody);
                var root = doc.RootElement;

                string? cargoProviderName = null;
                string? trackingNumber = null;

                string? GetJsonString(JsonElement element)
                {
                    return element.ValueKind switch
                    {
                        JsonValueKind.String => element.GetString(),
                        JsonValueKind.Number => element.GetRawText(), // sayıyı stringe çevir
                        _ => null
                    };
                }

                if (root.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
                {
                    var first = contentArray.EnumerateArray().FirstOrDefault();
                    if (first.ValueKind == JsonValueKind.Object)
                    {
                        if (first.TryGetProperty("cargoProviderName", out var providerProp))
                            cargoProviderName = GetJsonString(providerProp);

                        if (first.TryGetProperty("cargoTrackingNumber", out var trackProp))
                            trackingNumber = GetJsonString(trackProp);
                    }
                }

                // 🔹 Veritabanını güncelle
                siparis.CargoProviderName = cargoProviderName ?? request.CargoProvider;
                siparis.KargoTakipNumarasi = trackingNumber ?? "";
                siparis.UpdatedAt = DateTime.UtcNow;

                await _appDbContext.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Kargo firması başarıyla değiştirildi ve sipariş bilgisi Trendyol'dan güncellendi.",
                    updatedCargo = siparis.CargoProviderName,
                    updatedTracking = siparis.KargoTakipNumarasi,
                    trendyolResponse = getBody
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP hatası: {ex.Message}");
                return StatusCode(500, $"Trendyol API ile iletişim kurulamadı: {ex.Message}");
            }
        }



        [HttpPost("toplu-kargo-degistir")]
        public async Task<IActionResult> TopluKargoDegistir([FromBody] TopluKargoDto dto)
        {
            if (dto == null || dto.OrderIds == null || dto.OrderIds.Count == 0)
                return BadRequest("Sipariş listesi boş olamaz.");

            if (string.IsNullOrWhiteSpace(dto.ShippingCompany))
                return BadRequest("Kargo firması boş olamaz.");

            var firstOrder = await _appDbContext.Siparisler
                .FirstOrDefaultAsync(s => dto.OrderIds.Contains(s.Id.ToString()));

            if (firstOrder == null)
                return NotFound("Sipariş bulunamadı.");

            var entegrasyon = await _appDbContext.Entegrasyonlar
                .FirstOrDefaultAsync(e => e.Id == firstOrder.EntegrasyonId);

            if (entegrasyon == null)
                return NotFound("Entegrasyon bulunamadı.");

            if (!entegrasyon.Seller_Id.HasValue ||
                string.IsNullOrWhiteSpace(entegrasyon.Api_Key) ||
                string.IsNullOrWhiteSpace(entegrasyon.Api_Secret))
            {
                return BadRequest("Entegrasyon bilgileri eksik veya hatalı.");
            }

            var sellerId = entegrasyon.Seller_Id.Value;
            var apiKey = entegrasyon.Api_Key.Trim();
            var apiSecret = entegrasyon.Api_Secret.Trim();

            var httpClient = _httpClientFactory.CreateClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));

            var orders = await _appDbContext.Siparisler
                .Where(s => dto.OrderIds.Contains(s.Id.ToString()))
                .ToListAsync();

            var results = new List<dynamic>();

            // 🔹 Yardımcı fonksiyon: JSON değeri güvenli şekilde string olarak oku
            string? GetJsonString(JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Number => element.GetRawText(), // sayıyı stringe dönüştür
                    _ => null
                };
            }

            foreach (var order in orders)
            {
                if (string.IsNullOrEmpty(order.PaketNumarasi))
                {
                    results.Add(new { order.Id, success = false, message = "Paket numarası boş, Trendyol güncellemesi atlanıyor." });
                    continue;
                }

                // 1️⃣ Trendyol’a PUT isteği gönder (kargo firması değişikliği)
                var putUrl = $"https://apigw.trendyol.com/integration/order/sellers/{sellerId}/shipment-packages/{order.PaketNumarasi}/cargo-providers";
                var payload = new { cargoProvider = dto.ShippingCompany };
                var jsonContent = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var putRequest = new HttpRequestMessage(HttpMethod.Put, putUrl);
                putRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                putRequest.Headers.Add("User-Agent", "MyAppIntegration/1.0");
                putRequest.Content = jsonContent;

                try
                {
                    var putResponse = await httpClient.SendAsync(putRequest);
                    var putBody = await putResponse.Content.ReadAsStringAsync();

                    if (!putResponse.IsSuccessStatusCode)
                    {
                        results.Add(new { order.Id, success = false, message = putBody });
                        continue;
                    }

                    // 2️⃣ Güncel sipariş bilgisini Trendyol’dan al
                    var getUrl = $"https://apigw.trendyol.com/integration/order/sellers/{sellerId}/orders?orderNumber={order.SiparisNumarasi}";
                    var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
                    getRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                    getRequest.Headers.Add("User-Agent", "MyAppIntegration/1.0");

                    var getResponse = await httpClient.SendAsync(getRequest);
                    var getBody = await getResponse.Content.ReadAsStringAsync();

                    Console.WriteLine($"Trendyol GET status: {(int)getResponse.StatusCode}, body: {getBody}");

                    if (!getResponse.IsSuccessStatusCode || string.IsNullOrWhiteSpace(getBody))
                    {
                        results.Add(new { order.Id, success = false, message = "PUT başarılı ama GET başarısız." });
                        continue;
                    }

                    using var doc = JsonDocument.Parse(getBody);
                    var root = doc.RootElement;

                    string? cargoProviderName = null;
                    string? trackingNumber = null;

                    // content -> [0] -> shipmentPackageList -> [0] -> cargoTrackingNumber
                    if (root.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
                    {
                        var first = contentArray.EnumerateArray().FirstOrDefault();
                        if (first.ValueKind == JsonValueKind.Object)
                        {
                            if (first.TryGetProperty("shipmentPackageList", out var packageList) && packageList.ValueKind == JsonValueKind.Array)
                            {
                                var firstPkg = packageList.EnumerateArray().FirstOrDefault();
                                if (firstPkg.ValueKind == JsonValueKind.Object)
                                {
                                    if (firstPkg.TryGetProperty("cargoTrackingNumber", out var trackProp))
                                        trackingNumber = GetJsonString(trackProp);

                                    if (firstPkg.TryGetProperty("cargoProviderName", out var providerProp))
                                        cargoProviderName = GetJsonString(providerProp);
                                }
                            }
                        }
                    }

                    // 3️⃣ Veritabanını güncelle
                    order.CargoProviderName = cargoProviderName ?? dto.ShippingCompany;
                    order.KargoTakipNumarasi = trackingNumber ?? "";
                    order.UpdatedAt = DateTime.UtcNow;

                    results.Add(new
                    {
                        order.Id,
                        success = true,
                        message = "Kargo firması başarıyla değiştirildi.",
                        updatedCargo = order.CargoProviderName,
                        updatedTracking = order.KargoTakipNumarasi
                    });
                }
                catch (HttpRequestException ex)
                {
                    results.Add(new { order.Id, success = false, message = $"HTTP hatası: {ex.Message}" });
                }
            }

            await _appDbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                updated = results.Count(r => r.success == true),
                results
            });
        }



        [HttpPost("bol-siparis-paketi")]
        public async Task<IActionResult> BolSiparisPaketi([FromBody] TrendyolSplitPackageRequest request)
        {
            if (request == null || request.SellerId <= 0 || request.PackageId <= 0)
                return BadRequest("Eksik veya hatalı parametreler.");

            var entegrasyon = await _appDbContext.Entegrasyonlar
                .FirstOrDefaultAsync(e => e.Seller_Id == request.SellerId);

            if (entegrasyon == null)
                return BadRequest("Entegrasyon bilgisi bulunamadı.");

            string apiKey = entegrasyon.Api_Key ?? "";
            string apiSecret = entegrasyon.Api_Secret ?? "";

            // HttpClient oluştur
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}")));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Trendyol base URL
            string baseUrl = "https://apigw.trendyol.com/integration/order/sellers";
            string endpoint = "";

            switch (request.SplitType.ToLower())
            {
                case "split":
                    endpoint = $"{baseUrl}/{request.SellerId}/shipment-packages/{request.PackageId}/split";
                    break;
                case "multi-split":
                    endpoint = $"{baseUrl}/{request.SellerId}/shipment-packages/{request.PackageId}/multi-split";
                    break;
                case "quantity-split":
                    endpoint = $"{baseUrl}/{request.SellerId}/shipment-packages/{request.PackageId}/quantity-split";
                    break;
                case "multi-package":
                    endpoint = $"{baseUrl}/{request.SellerId}/shipment-packages/{request.PackageId}/split-packages";
                    break;
                default:
                    return BadRequest("Geçersiz splitType değeri.");
            }

            var jsonBody = JsonConvert.SerializeObject(request.Payload, Formatting.None);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(endpoint, content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Paket bölme isteği gönderildi.",
                        response = responseText
                    });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        success = false,
                        message = "Trendyol API isteği başarısız oldu.",
                        error = responseText
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paket bölme isteği hatası.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        public class TrendyolSplitPackageRequest
        {
            public long SellerId { get; set; }
            public long PackageId { get; set; }
            public string SplitType { get; set; } = "split"; // split, multi-split, quantity-split, multi-package
            public object Payload { get; set; } = new(); // API'ye uygun JSON payload
        }

        public class TopluKargoDto
        {
            public List<string>? OrderIds { get; set; }
            public string? ShippingCompany { get; set; }
        }


        public class ChangeCargoProviderRequest
        {
            public string CargoProvider { get; set; } = string.Empty;
            public Guid EntegrasyonId { get; set; }

        }

    }
}
