using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

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

        Console.Write("Lütfen metni giriniz: ");
        string? input = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine();
            Console.WriteLine("Duygu analizi yapılıyor...");

            string sentiment = await AnalyzeSentiment(input, apiKey);

            Console.WriteLine();
            Console.WriteLine($"Sonuç: {sentiment}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Metin boş olamaz.");
            Console.ResetColor();
        }

        Console.WriteLine("\nProgramı kapatmak için bir tuşa basın...");
        Console.ReadKey();
    }

    static async Task<string> AnalyzeSentiment(string text, string apiKey)
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
                            "Analyze the sentiment of the following text. " +
                            "Return only one word: Positive, Negative, or Neutral.\n\n" +
                            $"Text: \"{text}\""
                    }
                }
            }
        },
            generationConfig = new
            {
                maxOutputTokens = 100,
                temperature = 0.1,
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

                return "Hata";
            }

            using JsonDocument result = JsonDocument.Parse(responseJson);

            JsonElement root = result.RootElement;

            if (!root.TryGetProperty("candidates", out JsonElement candidates))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini cevabında 'candidates' alanı bulunamadı.");
                Console.WriteLine(responseJson);
                Console.ResetColor();

                return "Hata";
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

                    return "Hata";
                }
            }

            if (!firstCandidate.TryGetProperty("content", out JsonElement contentElement))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini cevabında 'content' alanı bulunamadı.");
                Console.WriteLine(responseJson);
                Console.ResetColor();

                return "Hata";
            }

            if (!contentElement.TryGetProperty("parts", out JsonElement parts))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini cevabında 'parts' alanı bulunamadı.");
                Console.WriteLine(responseJson);
                Console.ResetColor();

                return "Hata";
            }

            if (parts.GetArrayLength() == 0)
            {
                return "Cevap metni bulunamadı.";
            }

            if (!parts[0].TryGetProperty("text", out JsonElement textElement))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini cevabında 'text' alanı bulunamadı.");
                Console.WriteLine(responseJson);
                Console.ResetColor();

                return "Hata";
            }

            string? answer = textElement.GetString();

            return answer?.Trim() ?? "Sonuç alınamadı.";
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Beklenmeyen hata oluştu:");
            Console.WriteLine(ex.Message);
            Console.ResetColor();

            return "Hata";
        }
    }
}