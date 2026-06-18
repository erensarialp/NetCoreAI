using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Title = "Gemini Text to Speech";

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
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

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Gemini Text to Speech Uygulaması ===");
        Console.ResetColor();

        Console.Write("Metni Giriniz: ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Metin boş olamaz.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Ses dosyası oluşturuluyor...");
        Console.ResetColor();

        var success = await GenerateSpeechAsync(input, apiKey);

        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Ses dosyası 'output.wav' olarak kaydedildi.");
            Console.ResetColor();

            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "output.wav",
                    UseShellExecute = true
                }
            );
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Ses dosyası oluşturulamadı.");
            Console.ResetColor();
        }

        Console.WriteLine("\nProgramı kapatmak için bir tuşa basın...");
        Console.ReadKey();
    }

    static async Task<bool> GenerateSpeechAsync(string text, string apiKey)
    {
        using var httpClient = new HttpClient();

        var endpoint =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-tts-preview:generateContent";

        httpClient.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = $"Say clearly and naturally in Turkish: {text}"
                        }
                    }
                }
            },
            generationConfig = new
            {
                responseModalities = new[]
                {
                    "AUDIO"
                },
                speechConfig = new
                {
                    voiceConfig = new
                    {
                        prebuiltVoiceConfig = new
                        {
                            voiceName = "Kore"
                        }
                    }
                }
            },
            model = "gemini-3.1-flash-tts-preview"
        };

        try
        {
            var json = JsonSerializer.Serialize(requestBody);

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync(endpoint, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"API Hatası: {(int)response.StatusCode} - {response.StatusCode}");
                Console.WriteLine(responseString);
                Console.ResetColor();
                return false;
            }

            using var jsonDocument = JsonDocument.Parse(responseString);
            var root = jsonDocument.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates) ||
                candidates.GetArrayLength() == 0)
            {
                Console.WriteLine("Gemini herhangi bir ses çıktısı döndürmedi.");
                return false;
            }

            var part = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0];

            JsonElement inlineData;

            if (part.TryGetProperty("inlineData", out var inlineDataCamel))
            {
                inlineData = inlineDataCamel;
            }
            else if (part.TryGetProperty("inline_data", out var inlineDataSnake))
            {
                inlineData = inlineDataSnake;
            }
            else
            {
                Console.WriteLine("Ses verisi bulunamadı.");
                Console.WriteLine(responseString);
                return false;
            }

            var base64Audio = inlineData.GetProperty("data").GetString();

            if (string.IsNullOrWhiteSpace(base64Audio))
            {
                Console.WriteLine("Base64 ses verisi boş geldi.");
                return false;
            }

            var pcmBytes = Convert.FromBase64String(base64Audio);

            var wavBytes = CreateWavFile(
                pcmBytes,
                sampleRate: 24000,
                channels: 1,
                bitsPerSample: 16
            );

            await File.WriteAllBytesAsync("output.wav", wavBytes);

            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Beklenmeyen hata: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    static byte[] CreateWavFile(
        byte[] pcmData,
        int sampleRate,
        short channels,
        short bitsPerSample)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);

        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;
        var subChunk2Size = pcmData.Length;
        var chunkSize = 36 + subChunk2Size;

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(chunkSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write(bitsPerSample);

        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(subChunk2Size);
        writer.Write(pcmData);

        return memoryStream.ToArray();
    }
}