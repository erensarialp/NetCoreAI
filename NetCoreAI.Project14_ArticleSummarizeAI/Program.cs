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

        Console.Write("Uzun metninizi veya makalenizi giriniz: ");
        string? input = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine();
            Console.WriteLine("Girmiş olduğunuz metin Gemini AI tarafından özetleniyor...");
            Console.WriteLine();

            string shortSummary = await SummarizeText(input, "short", apiKey);
            string mediumSummary = await SummarizeText(input, "medium", apiKey);
            string detailedSummary = await SummarizeText(input, "detailed", apiKey);

            Console.WriteLine("Özetler");
            Console.WriteLine("------------------------");
            Console.WriteLine($"Kısa Özet:\n{shortSummary}");
            Console.WriteLine("------------------------");
            Console.WriteLine($"Orta Uzunlukta Özet:\n{mediumSummary}");
            Console.WriteLine("------------------------");
            Console.WriteLine($"Detaylı Özet:\n{detailedSummary}");
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

    static async Task<string> SummarizeText(string text, string level, string apiKey)
    {
        using HttpClient client = new HttpClient();

        var endpoint =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

        string instruction = level switch
        {
            "short" => "Summarize the following text in 1-2 sentences.",
            "medium" => "Summarize the following text in 3-5 sentences.",
            "detailed" => "Summarize the following text in a detailed but concise manner.",
            _ => "Summarize the following text."
        };

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
                                "You are an AI assistant that summarizes texts at different detail levels. " +
                                "Answer in Turkish. Do not add unnecessary explanations.\n\n" +
                                $"{instruction}\n\n" +
                                $"Text:\n{text}"
                        }
                    }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = 1000,
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

            string? summary = textElement.GetString();

            return summary?.Trim() ?? "Sonuç alınamadı.";
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