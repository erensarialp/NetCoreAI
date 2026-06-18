using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
class Program
{
    // Kullanıcıya gösterilecek dil listesi
    private static readonly Dictionary<string, string> SupportedLanguages = new()
    {
        { "1", "Turkish (Türkçe)" },
        { "2", "English (İngilizce)" },
        { "3", "German (Almanca)" },
        { "4", "French (Fransızca)" },
        { "5", "Spanish (İspanyolca)" },
        { "6", "Italian (İtalyanca)" },
        { "7", "Portuguese (Portekizce)" }
    };

    // API'ye gönderilecek dil kodları - Gemini AI tarafından anlaşılacak format
    private static readonly Dictionary<string, string> LanguageCodes = new()
    {
        { "1", "Turkish" },
        { "2", "English" },
        { "3", "German" },
        { "4", "French" },
        { "5", "Spanish" },
        { "6", "Italian" },
        { "7", "Portuguese" }
    };

    static async Task Main(string[] args)
    {
        Console.Title = "Gemini AI Translator";
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("========================================");
        Console.WriteLine("  Gemini AI Destekli Çeviri Uygulaması");
        Console.WriteLine("========================================\n");
        Console.ResetColor();

        // appsettings.json dosyasından API anahtarını oku
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // API anahtarını al ve kontrol et
        var apiKey = config["GeminiApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // API anahtarı yoksa kullanıcıyı uyar ve çık
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("HATA: appsettings.json içinde 'GeminiApiKey' bulunamadı.");
            Console.WriteLine("Lütfen API anahtarınızı appsettings.json dosyasına ekleyin.");
            Console.ResetColor();
            Console.WriteLine("\nÇıkmak için bir tuşa basın...");
            Console.ReadKey();
            return;
        }

        // Ana uygulama döngüsü - kullanıcı çıkana kadar çalışır
        while (true)
        {
            try
            {
                ShowMainMenu();
                string choice = Console.ReadLine()?.Trim() ?? "";

                // Kullanıcı seçimine göre işlem yap
                switch (choice.ToLower())
                {
                    case "1":
                        // Hızlı çeviri - Sadece Türkçe -> İngilizce
                        await QuickTranslateAsync(apiKey);
                        break;
                    case "2":
                        // Özel dil seçimi - kullanıcı kaynak ve hedef dili seçer
                        await CustomTranslateAsync(apiKey);
                        break;
                    case "3":
                    case "exit":
                    case "çık":
                        // Uygulamadan çık
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("\nÇeviri uygulamasından çıkılıyor. İyi günler!");
                        Console.ResetColor();
                        return;
                    default:
                        // Geçersiz seçim için uyarı
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\nGeçersiz seçim! Lütfen 1, 2 veya 3 seçin.");
                        Console.ResetColor();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nBir hata oluştu: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("\nDevam etmek için bir tuşa basın...");
                Console.ReadKey();
                Console.Clear();
            }
        }
    }

    // Ana menüyü ekranda gösteren fonksiyon
    private static void ShowMainMenu()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("=============== ANA MENÜ ===============");
        Console.ResetColor();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[1] Hızlı Çeviri (Türkçe -> İngilizce)");
        Console.WriteLine("[2] Özel Dil Seçimi ile Çeviri");
        Console.WriteLine("[3] Çıkış");
        Console.ResetColor();
        Console.WriteLine();
        Console.Write("Seçiminizi yapın (1-3): ");
    }

    // Hızlı çeviri fonksiyonu - Sadece Türkçe'den İngilizce'ye çeviri yapar
    private static async Task QuickTranslateAsync(string apiKey)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Hızlı Çeviri (Türkçe -> İngilizce)");
        Console.WriteLine("===================================");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Çıkmak için 'q' yazın\n");
        Console.ResetColor();

        // Sürekli çeviri döngüsü - 'q' yazılana kadar devam eder
        while (true)
        {
            // Kullanıcıdan çevrilecek metni al
            Console.Write("Türkçe metin: ");
            string inputText = Console.ReadLine()?.Trim() ?? "";

            if (inputText.ToLower() == "q")
            {
                Console.Clear();
                break;
            }

            // Metin boş mu kontrol et
            if (string.IsNullOrEmpty(inputText))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Metin girmediniz! Tekrar deneyin.\n");
                Console.ResetColor();
                continue;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("İngilizce'ye çevriliyor... ");
            Console.ResetColor();

            // API'ye çeviri isteği gönder
            string translatedText = await TranslateTextAsync(inputText, "English", apiKey);

            // Çeviri sonucu
            if (!string.IsNullOrEmpty(translatedText))
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"İngilizce: {translatedText}\n");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Çeviri başarısız oldu.\n");
                Console.ResetColor();
            }
        }
    }

    // Özel dil seçimi ile çeviri yapan fonksiyon
    private static async Task CustomTranslateAsync(string apiKey)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Özel Dil Seçimi ile Çeviri");
        Console.WriteLine("===========================\n");
        Console.ResetColor();

        // Kullanıcıdan kaynak dili seç
        string? sourceLanguage = SelectLanguage("Kaynak dili seçin (metnin şu anki dili):");
        if (sourceLanguage == null) return;

        // Kullanıcıdan hedef dili seç
        string? targetLanguage = SelectLanguage("Hedef dili seçin (çevrilecek dil):");
        if (targetLanguage == null) return;

        if (sourceLanguage == targetLanguage)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Kaynak ve hedef dil aynı olamaz!");
            Console.ResetColor();
            Console.WriteLine("\nDevam etmek için bir tuşa basın...");
            Console.ReadKey();
            Console.Clear();
            return;
        }

        // Dil isimlerini bul
        string sourceLangDisplay = SupportedLanguages.FirstOrDefault(x => LanguageCodes[x.Key] == sourceLanguage).Value;
        string targetLangDisplay = SupportedLanguages.FirstOrDefault(x => LanguageCodes[x.Key] == targetLanguage).Value;

        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Çeviri: {sourceLangDisplay} -> {targetLangDisplay}");
        Console.WriteLine("=====================================");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Çıkmak için 'q' yazın\n");
        Console.ResetColor();

        // Sürekli çeviri döngüsü - 'q' yazılana kadar devam eder
        while (true)
        {
            // Kullanıcıdan çevrilecek metni al
            Console.Write($"{sourceLangDisplay} metin: ");
            string inputText = Console.ReadLine()?.Trim() ?? "";

            // Çıkış kontrolü
            if (inputText.ToLower() == "q")
            {
                Console.Clear();
                break; // Ana menüye dön
            }

            // Metin boş mu kontrol et
            if (string.IsNullOrEmpty(inputText))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Metin girmediniz! Tekrar deneyin.\n");
                Console.ResetColor();
                continue;
            }

            // Çeviri işleminin başladığını bildir
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{targetLangDisplay} diline çevriliyor... ");
            Console.ResetColor();

            // API'ye çeviri isteği gönder
            string translatedText = await TranslateTextAsync(inputText, targetLanguage, apiKey);

            // Çeviri sonucu
            if (!string.IsNullOrEmpty(translatedText))
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{targetLangDisplay}: {translatedText}\n");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Çeviri başarısız oldu.\n");
                Console.ResetColor();
            }
        }
    }

    // Dil seçimi için kullanıcıya liste gösterir ve seçimini alır
    private static string? SelectLanguage(string prompt)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n{prompt}");
        Console.ResetColor();
        Console.WriteLine();

        // Desteklenen dilleri listele
        foreach (var lang in SupportedLanguages)
        {
            Console.WriteLine($"[{lang.Key}] {lang.Value}");
        }

        // Kullanıcıdan seçim al
        Console.Write("\nDil numarasını seçin (1-7): ");
        string choice = Console.ReadLine()?.Trim() ?? "";

        // Seçim geçerli mi kontrol et
        if (LanguageCodes.ContainsKey(choice))
        {
            return LanguageCodes[choice];
        }

        // Geçersiz seçim uyarısı
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Geçersiz seçim!");
        Console.ResetColor();
        return null;
    }

    // Google AI Studio (Gemini) API'sine çeviri isteği gönderen ana fonksiyon
    private static async Task<string> TranslateTextAsync(string text, string targetLanguage, string apiKey)
    {
        // HTTP istemcisi oluştur
        using var httpClient = new HttpClient();

        // Gemini API endpoint URL'i
        var endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        // API anahtarını header'a ekle
        httpClient.DefaultRequestHeaders.Add("X-goog-api-key", apiKey);

        // API'ye gönderilecek istek gövdesini hazırla
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        // Gemini'ye çeviri talimatı ver
                        new { text = $"Please translate the following text to {targetLanguage}. Only return the translated text, no explanations or additional text: {text}" }
                    }
                }
            }
        };

        try
        {
            // İstek gövdesini JSON formatına çevir
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // API'ye POST isteği gönder
            var response = await httpClient.PostAsync(endpoint, content);
            var responseString = await response.Content.ReadAsStringAsync();

            // İstek başarılı mı kontrol et
            if (response.IsSuccessStatusCode)
            {
                // JSON yanıtını parse et
                var responseJson = JsonDocument.Parse(responseString);
                var candidates = responseJson.RootElement.GetProperty("candidates");

                // Yanıtta sonuç var mı kontrol et
                if (candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    var content_prop = firstCandidate.GetProperty("content");
                    var parts = content_prop.GetProperty("parts");

                    // Çeviri metnini al ve döndür
                    if (parts.GetArrayLength() > 0)
                    {
                        var translatedText = parts[0].GetProperty("text").GetString();
                        return translatedText?.Trim() ?? "";
                    }
                }
            }
            else
            {
                // API hatası durumunda sadece hata durumunu döndür, konsola yazdırma
                return "";
            }
        }
        catch (Exception)
        {
            // Bağlantı hatası durumunda sadece hata durumunu döndür
            return "";
        }

        return ""; // Hata durumunda boş string döndür
    }
}