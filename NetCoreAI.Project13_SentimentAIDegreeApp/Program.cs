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

        Console.Write("Bir metin giriniz: ");
        string? input = Console.ReadLine();

        Console.WriteLine();

        if (!string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("Gelişmiş duygu analizi yapılıyor...");

            string sentiment = await AdvancedSentimentalAnalysis(input, apiKey);

            Console.WriteLine();
            Console.WriteLine("Sonuç:");
            Console.WriteLine(sentiment);
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

    static async Task<string> AdvancedSentimentalAnalysis(string text, string apiKey)
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
                                "You are an advanced AI that analyzes emotions in text. " +
                                "Analyze the following text and return ONLY a valid JSON object. " +
                                "Do not write markdown. Do not write explanations. " +
                                "Return emotion scores as numbers between 0 and 100. " +
                                "The total score should be approximately 100.\n\n" +
                                "Required JSON format:\n" +
                                "{\n" +
                                "  \"Joy\": 0,\n" +
                                "  \"Sadness\": 0,\n" +
                                "  \"Anger\": 0,\n" +
                                "  \"Fear\": 0,\n" +
                                "  \"Surprise\": 0,\n" +
                                "  \"Neutral\": 0\n" +
                                "}\n\n" +
                                $"Text: \"{text}\""
                        }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                maxOutputTokens = 300,
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

                return "Hata!";
            }

            using JsonDocument result = JsonDocument.Parse(responseJson);

            JsonElement root = result.RootElement;

            if (!root.TryGetProperty("candidates", out JsonElement candidates))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini cevabında 'candidates' alanı bulunamadı.");
                Console.WriteLine(responseJson);
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
                Console.WriteLine(responseJson);
                Console.ResetColor();

                return "Hata!";
            }

            if (!contentElement.TryGetProperty("parts", out JsonElement parts))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gemini cevabında 'parts' alanı bulunamadı.");
                Console.WriteLine(responseJson);
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
                Console.WriteLine(responseJson);
                Console.ResetColor();

                return "Hata!";
            }

            string? analysis = textElement.GetString();

            if (string.IsNullOrWhiteSpace(analysis))
            {
                return "Sonuç alınamadı.";
            }

            return FormatJson(analysis);
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

    static string FormatJson(string jsonText)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(jsonText);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(document.RootElement, options);
        }
        catch
        {
            return jsonText;
        }
    }
}