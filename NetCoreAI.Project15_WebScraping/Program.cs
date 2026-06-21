using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var apiKey = config["GeminiApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("HATA: appsettings.json içinde 'GeminiApiKey' bulunamadı.");
            Console.ResetColor();
            return;
        }

        Console.Write("Lütfen analiz yapmak istediğiniz web sayfa URL'ini giriniz: ");
        string? inputUrl = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(inputUrl))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("URL boş olamaz.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Web sayfası içeriği okunuyor...");

        string webContent = ExtractTextFromWeb(inputUrl);

        if (string.IsNullOrWhiteSpace(webContent) ||
            webContent == "Sayfa içeriği okunamadı.")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Sayfa içeriği okunamadı.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine("Web sayfası Gemini AI tarafından analiz ediliyor...");
        Console.WriteLine();

        await AnalyzeWithAI(webContent, "Web Sayfası İçeriği", apiKey);

        Console.WriteLine("\nProgramı kapatmak için bir tuşa basın...");
        Console.ReadKey();
    }

    static string ExtractTextFromWeb(string url)
    {
        try
        {
            var web = new HtmlWeb();

            var doc = web.Load(url);

            var bodyNode = doc.DocumentNode.SelectSingleNode("//body");

            if (bodyNode == null)
            {
                return "Sayfa içeriği okunamadı.";
            }

            string bodyText = bodyNode.InnerText;

            bodyText = HtmlEntity.DeEntitize(bodyText);

            bodyText = Regex.Replace(bodyText, @"\s+", " ").Trim();

            if (bodyText.Length > 15000)
            {
                bodyText = bodyText.Substring(0, 15000);
            }

            return bodyText;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Web sayfası okunurken hata oluştu: {ex.Message}");
            Console.ResetColor();

            return "Sayfa içeriği okunamadı.";
        }
    }

    static async Task AnalyzeWithAI(string text, string sourceType, string apiKey)
    {
        using HttpClient client = new HttpClient();

        var endpoint =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new
                        {
                            text =
                                "Sen bir yapay zeka asistanısın. " +
                                "Kullanıcının gönderdiği web sayfası içeriğini Türkçe olarak analiz et ve özetle. " +
                                "Yanıtını sadece Türkçe ver. " +
                                "Cevabında şu başlıkları kullan:\n" +
                                "1. Genel Özet\n" +
                                "2. Ana Konular\n" +
                                "3. Öne Çıkan Bilgiler\n" +
                                "4. Kısa Değerlendirme\n\n" +
                                $"{sourceType}:\n{text}"
                        }
                    }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = 3000,
                temperature = 0.3,
                thinkingConfig = new
                {
                    thinkingBudget = 0
                }
            }
        };

        try
        {
            string json = JsonSerializer.Serialize(requestBody);

            using HttpContent content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response = await client.PostAsync(endpoint, content);

            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("API hatası oluştu:");
                Console.WriteLine($"{(int)response.StatusCode} - {response.StatusCode}");
                Console.WriteLine(responseJson);
                Console.ResetColor();

                return;
            }

            using JsonDocument result = JsonDocument.Parse(responseJson);

            JsonElement root = result.RootElement;

            if (!root.TryGetProperty("candidates", out JsonElement candidates))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini cevabında 'candidates' alanı bulunamadı.");
                Console.WriteLine(responseJson);
                Console.ResetColor();

                return;
            }

            if (candidates.GetArrayLength() == 0)
            {
                Console.WriteLine("Gemini cevap döndürmedi.");
                return;
            }

            JsonElement firstCandidate = candidates[0];

            if (firstCandidate.TryGetProperty("finishReason", out JsonElement finishReason))
            {
                string? reason = finishReason.GetString();

                if (reason == "MAX_TOKENS")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Token limiti yetersiz geldi. maxOutputTokens değerini artırın.");
                    Console.ResetColor();

                    return;
                }
            }

            if (!firstCandidate.TryGetProperty("content", out JsonElement contentElement))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini cevabında 'content' alanı bulunamadı.");
                Console.WriteLine(responseJson);
                Console.ResetColor();

                return;
            }

            if (!contentElement.TryGetProperty("parts", out JsonElement parts))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini cevabında 'parts' alanı bulunamadı.");
                Console.WriteLine(responseJson);
                Console.ResetColor();

                return;
            }

            if (parts.GetArrayLength() == 0)
            {
                Console.WriteLine("Cevap metni bulunamadı.");
                return;
            }

            if (!parts[0].TryGetProperty("text", out JsonElement textElement))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini cevabında 'text' alanı bulunamadı.");
                Console.WriteLine(responseJson);
                Console.ResetColor();

                return;
            }

            string? analysis = textElement.GetString();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nAI Analizi ({sourceType}):");
            Console.ResetColor();

            Console.WriteLine();
            Console.WriteLine(analysis?.Trim() ?? "Sonuç alınamadı.");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Beklenmeyen hata oluştu:");
            Console.WriteLine(ex.Message);
            Console.ResetColor();
        }
    }
}