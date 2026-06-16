using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(
                "appsettings.json",
                optional: false,
                reloadOnChange: true
            )
            .Build();

        var apiKey = config["GeminiApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                "HATA: appsettings.json içinde 'GeminiApiKey' bulunamadı."
            );
            Console.WriteLine(
                "Lütfen API anahtarınızı appsettings.json dosyasına ekleyin."
            );
            Console.ResetColor();
            return;
        }

        using var httpClient = new HttpClient();

        var endpoint =
            "https://generativelanguage.googleapis.com/v1beta/" +
            "models/gemini-2.5-flash:generateContent";

        httpClient.DefaultRequestHeaders.Add(
            "x-goog-api-key",
            apiKey
        );

        var chatHistory =
            new List<(string Sender, string Message)>();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Gemini Chatbot'a hoş geldiniz!");
        Console.WriteLine(
            "Çıkmak için 'çık' veya 'exit' yazabilirsiniz.\n"
        );
        Console.ResetColor();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Siz: ");
            Console.ResetColor();

            var prompt = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(prompt))
            {
                continue;
            }

            var normalizedPrompt = prompt.Trim().ToLower();

            if (normalizedPrompt == "çık" ||
                normalizedPrompt == "exit")
            {
                break;
            }

            chatHistory.Add(("Siz", prompt));

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
                                text = prompt
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = 500,
                    temperature = 0.7
                }
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await httpClient.PostAsync(
                    endpoint,
                    content
                );

                var responseString =
                    await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var result =
                        JsonDocument.Parse(responseString);

                    var root = result.RootElement;

                    if (!root.TryGetProperty(
                            "candidates",
                            out var candidates
                        ) ||
                        candidates.GetArrayLength() == 0)
                    {
                        Console.ForegroundColor =
                            ConsoleColor.Red;

                        Console.WriteLine(
                            "Gemini cevap üretmedi."
                        );

                        Console.WriteLine(responseString);
                        Console.ResetColor();
                        continue;
                    }

                    var parts = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts");

                    var answerBuilder =
                        new StringBuilder();

                    foreach (
                        var part in parts.EnumerateArray()
                    )
                    {
                        if (part.TryGetProperty(
                                "text",
                                out var textElement
                            ))
                        {
                            answerBuilder.Append(
                                textElement.GetString()
                            );
                        }
                    }

                    var answer =
                        answerBuilder.ToString();

                    chatHistory.Add(
                        ("Gemini", answer)
                    );

                    Console.Clear();

                    Console.ForegroundColor =
                        ConsoleColor.Cyan;

                    Console.WriteLine(
                        "Gemini Chatbot'a hoş geldiniz!"
                    );

                    Console.WriteLine(
                        "Çıkmak için 'çık' veya " +
                        "'exit' yazabilirsiniz.\n"
                    );

                    Console.ResetColor();

                    foreach (
                        var (sender, message)
                        in chatHistory
                    )
                    {
                        if (sender == "Siz")
                        {
                            Console.ForegroundColor =
                                ConsoleColor.Yellow;

                            Console.Write("Siz: ");
                        }
                        else
                        {
                            Console.ForegroundColor =
                                ConsoleColor.Green;

                            Console.Write("Gemini: ");
                        }

                        Console.ResetColor();
                        Console.WriteLine(message);
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.ForegroundColor =
                        ConsoleColor.Red;

                    Console.WriteLine(
                        $"Hata kodu: " +
                        $"{(int)response.StatusCode}"
                    );

                    Console.WriteLine(
                        $"Hata türü: " +
                        $"{response.StatusCode}"
                    );

                    Console.WriteLine(
                        "Gemini API hata ayrıntısı:"
                    );

                    Console.WriteLine(responseString);
                    Console.ResetColor();
                }
            }
            catch (HttpRequestException ex)
            {
                Console.ForegroundColor =
                    ConsoleColor.Red;

                Console.WriteLine(
                    $"Bağlantı hatası: {ex.Message}"
                );

                Console.ResetColor();
            }
            catch (JsonException ex)
            {
                Console.ForegroundColor =
                    ConsoleColor.Red;

                Console.WriteLine(
                    $"JSON okuma hatası: {ex.Message}"
                );

                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor =
                    ConsoleColor.Red;

                Console.WriteLine(
                    $"Beklenmeyen hata: {ex.Message}"
                );

                Console.ResetColor();
            }
        }

        Console.WriteLine(
            "\nProgram kapatıldı."
        );
    }
}