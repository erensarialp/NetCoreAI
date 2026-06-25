using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

class Program
{
    private static readonly string apiKey = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build()["GeminiApiKey"]!;

    static async Task Main()
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("HATA: appsettings.json içinde 'GeminiApiKey' bulunamadı.");
            Console.ResetColor();
            return;
        }

        Console.Write("Hikaye Türünü Seçiniz (Macera, Korku, Bilim Kurgu, Fantastik, Komedi): ");
        string? genre = Console.ReadLine();

        Console.Write("Ana karakteriniz kim: ");
        string? character = Console.ReadLine();

        Console.Write("Hikaye nerede geçiyor: ");
        string? setting = Console.ReadLine();

        Console.Write("Hikayenin uzunluğu (kısa/orta/uzun): ");
        string? length = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(genre) ||
            string.IsNullOrWhiteSpace(character) ||
            string.IsNullOrWhiteSpace(setting) ||
            string.IsNullOrWhiteSpace(length))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Lütfen tüm alanları doldurun.");
            Console.ResetColor();
            return;
        }

        string prompt =
            $"{genre} türünde bir hikaye yaz. " +
            $"Baş karakterin adı {character}. " +
            $"Hikaye {setting} bölgesinde geçiyor. " +
            $"{length} bir hikaye olsun. " +
            "Giriş, gelişme ve sonuç içermeli. " +
            "Hikayeyi Türkçe yaz.";

        Console.WriteLine();
        Console.WriteLine("Hikaye Gemini AI tarafından oluşturuluyor...");

        string story = await GenerateStory(prompt);

        Console.WriteLine();
        Console.WriteLine("--- Gemini AI Tarafından Oluşturulan Hikaye ---\n");
        Console.WriteLine(story);

        Console.WriteLine("\nProgramı kapatmak için bir tuşa basın...");
        Console.ReadKey();
    }

    static async Task<string> GenerateStory(string prompt)
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
                                "Sen yaratıcı bir hikaye yazarısın. " +
                                "Kullanıcının verdiği bilgilere göre Türkçe, akıcı ve özgün bir hikaye yaz. " +
                                "Hikayede giriş, gelişme ve sonuç bölümleri belirgin olsun.\n\n" +
                                prompt
                        }
                    }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = 1500,
                temperature = 0.8,
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

            string? story = textElement.GetString();

            return story?.Trim() ?? "Hikaye oluşturulamadı.";
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