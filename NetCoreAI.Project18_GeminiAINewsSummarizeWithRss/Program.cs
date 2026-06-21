using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

class Program
{
    private static readonly string apiKey = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build()["GeminiApiKey"]!;

    private static readonly string rssFeedUrl = "https://www.sozcu.com.tr/rss/tum-haberler.xml";

    static async Task Main()
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("HATA: appsettings.json içinde 'GeminiApiKey' bulunamadı.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Haberler sistemden alınıyor...");
        Console.ResetColor();

        List<string> articles = await FetchLatestNews(10);

        foreach (var article in articles)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Haber özeti oluşturuluyor...");
            Console.ResetColor();

            string summary = await SummarizeArticle(article);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("--- Gemini AI tarafından özetlenen haber ---\n");
            Console.ResetColor();

            Console.WriteLine(summary);
            Console.WriteLine("-------------------------------------------------\n");
        }

        Console.WriteLine("Programı kapatmak için bir tuşa basın...");
        Console.ReadKey();
    }

    static async Task<List<string>> FetchLatestNews(int count)
    {
        using var client = new HttpClient();

        string rssContent = await client.GetStringAsync(rssFeedUrl);

        XDocument doc = XDocument.Parse(rssContent);

        var items = doc.Descendants("item").Take(count);

        List<string> articles = items.Select(item =>
        {
            string title = item.Element("title")?.Value ?? "";
            string description = item.Element("description")?.Value ?? "";

            return $"{title}. {description}";
        }).ToList();

        return articles;
    }

    static async Task<string> SummarizeArticle(string articleText)
    {
        using var client = new HttpClient();

        string endpoint =
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
                                "Sen uzman bir haber özetleyicisisin. " +
                                "Verilen haberi Türkçe olarak 3 cümlede özetle. " +
                                "Gereksiz açıklama ekleme, sadece haber özetini yaz.\n\n" +
                                "Haber metni:\n" +
                                articleText
                        }
                    }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = 500,
                temperature = 0.3,
                thinkingConfig = new
                {
                    thinkingBudget = 0
                }
            }
        };

        try
        {
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response = await client.PostAsync(endpoint, jsonContent);

            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini API hatası:");
                Console.WriteLine($"{(int)response.StatusCode} - {response.StatusCode}");
                Console.WriteLine(responseContent);
                Console.ResetColor();

                return "Hata!";
            }

            using JsonDocument doc = JsonDocument.Parse(responseContent);

            JsonElement root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out JsonElement candidates))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini cevabında 'candidates' alanı bulunamadı.");
                Console.WriteLine(responseContent);
                Console.ResetColor();

                return "Hata!";
            }

            if (candidates.GetArrayLength() == 0)
            {
                return "Gemini cevap döndürmedi.";
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

                    return "Hata!";
                }
            }

            if (!firstCandidate.TryGetProperty("content", out JsonElement contentElement))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini cevabında 'content' alanı bulunamadı.");
                Console.WriteLine(responseContent);
                Console.ResetColor();

                return "Hata!";
            }

            if (!contentElement.TryGetProperty("parts", out JsonElement parts))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini cevabında 'parts' alanı bulunamadı.");
                Console.WriteLine(responseContent);
                Console.ResetColor();

                return "Hata!";
            }

            if (parts.GetArrayLength() == 0)
            {
                return "Cevap metni bulunamadı.";
            }

            if (!parts[0].TryGetProperty("text", out JsonElement textElement))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini cevabında 'text' alanı bulunamadı.");
                Console.WriteLine(responseContent);
                Console.ResetColor();

                return "Hata!";
            }

            string? summary = textElement.GetString();

            return summary?.Trim() ?? "Özet alınamadı.";
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Beklenmeyen hata oluştu:");
            Console.WriteLine(ex.Message);
            Console.ResetColor();

            return "Hata!";
        }
    }
}