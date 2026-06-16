using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

class Program
{
    // Model bilgileri
    private const string Model1Name =
    "FLUX.1 Schnell (Hugging Face)";
    private const string Model1Endpoint =
    "https://router.huggingface.co/hf-inference/models/black-forest-labs/FLUX.1-schnell";
    private const string Model2Name = "FLUX Text To Image (modelslab.com)";
    private const string Model2Endpoint = "https://modelslab.com/api/v6/images/text2img";

    static async Task Main(string[] args)
    {
        Console.Title = "AI Image Generation";
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==============================================");
        Console.WriteLine("      AI Text-to-Image Console Uygulaması      ");
        Console.WriteLine("==============================================\n");
        Console.ResetColor();

        // API anahtarlarını appsettings.json'dan oku
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        var huggingFaceApiKey = config["HuggingFaceApiKey"];
        var modelsLabApiKey = config["ModelsLabApiKey"];
        if (string.IsNullOrWhiteSpace(huggingFaceApiKey) || string.IsNullOrWhiteSpace(modelsLabApiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("API anahtar(lar)ı bulunamadı. Lütfen appsettings.json dosyasını kontrol edin.");
            Console.ResetColor();
            return;
        }

        while (true)
        {
            // Model seçimi
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Kullanılabilir Modeller:");
            Console.WriteLine($"  [1] {Model1Name}");
            Console.WriteLine($"  [2] {Model2Name}");
            Console.ResetColor();
            Console.Write("Model numarasını seçin (1-2, çıkmak için 'exit'): ");
            var modelInput = Console.ReadLine();
            if (modelInput?.Trim().ToLower() == "exit") break;
            int modelIndex = 1;
            if (!string.IsNullOrWhiteSpace(modelInput) && int.TryParse(modelInput, out int idx) && (idx == 1 || idx == 2))
                modelIndex = idx;
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Geçersiz seçim. 1 veya 2 girin.\n");
                Console.ResetColor();
                continue;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nSeçili Model: {(modelIndex == 1 ? Model1Name : Model2Name)}\n");
            Console.ResetColor();

            // Prompt döngüsü
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Çizilmesini istediğiniz içerik (çıkmak için 'exit', model değiştirmek için 'back'): ");
                Console.ResetColor();
                string prompt = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Prompt boş olamaz.\n");
                    Console.ResetColor();
                    continue;
                }
                if (prompt.Trim().ToLower() == "exit")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Programdan çıkılıyor...");
                    Console.ResetColor();
                    return;
                }
                if (prompt.Trim().ToLower() == "back")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Model seçimine dönülüyor...\n");
                    Console.ResetColor();
                    break;
                }

                if (modelIndex == 1)
                {
                    // Hugging Face Stable Diffusion XL
                    await GenerateWithHuggingFace(prompt, huggingFaceApiKey);
                }
                else
                {
                    // FLUX Text To Image (modelslab.com)
                    await GenerateWithFlux(prompt, modelsLabApiKey);
                }
            }
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\nUygulamayı kullandığınız için teşekkürler!");
        Console.ResetColor();
    }

    // Hugging Face ile görsel üretimi
    private static async Task GenerateWithHuggingFace(
    string prompt,
    string apiKey)
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                apiKey
            );

        httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(
                "image/png"
            )
        );

        var requestBody = new
        {
            inputs = prompt,
            parameters = new
            {
                guidance_scale = 7.5,
                num_inference_steps = 30
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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nGörsel üretiliyor...\n");
            Console.ResetColor();

            using var response = await httpClient.PostAsync(
                Model1Endpoint,
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent =
                    await response.Content.ReadAsStringAsync();

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    $"API Hatası: {(int)response.StatusCode} " +
                    $"{response.StatusCode}"
                );

                Console.WriteLine(errorContent);
                Console.ResetColor();
                return;
            }

            var contentType =
                response.Content.Headers.ContentType?.MediaType;

            if (contentType?.StartsWith("image/") != true)
            {
                var unexpectedResponse =
                    await response.Content.ReadAsStringAsync();

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    "API görsel yerine farklı bir yanıt döndürdü:"
                );

                Console.WriteLine(unexpectedResponse);
                Console.ResetColor();
                return;
            }

            var imageBytes =
                await response.Content.ReadAsByteArrayAsync();

            var extension = contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/webp" => ".webp",
                _ => ".png"
            };

            var outputDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                "GeneratedImages"
            );

            Directory.CreateDirectory(outputDirectory);

            var fileName =
                $"output_hf_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";

            var fullPath = Path.Combine(
                outputDirectory,
                fileName
            );

            await File.WriteAllBytesAsync(
                fullPath,
                imageBytes
            );

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                $"Görsel başarıyla kaydedildi:\n{fullPath}\n"
            );

            Console.ResetColor();
        }
        catch (TaskCanceledException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                "İstek zaman aşımına uğradı. Model yoğun olabilir.\n"
            );
            Console.ResetColor();
        }
        catch (HttpRequestException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                $"HTTP bağlantı hatası: {ex.Message}\n"
            );
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Hata: {ex.Message}\n");
            Console.ResetColor();
        }
    }

    // FLUX Text To Image (modelslab.com) ile görsel üretimi
    private static async Task GenerateWithFlux(string prompt, string apiKey)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("key", apiKey);
        // FLUX modeline uygun request body
        var requestBody = new
        {
            model_id = "flux",
            prompt = prompt,
            samples = "1",
            negative_prompt = "(worst quality:2), (low quality:2), (normal quality:2), (jpeg artifacts), (blurry), (duplicate), (morbid), (mutilated), (out of frame), (extra limbs), (bad anatomy), (disfigured), (deformed), (cross-eye), (glitch), (oversaturated), (overexposed), (underexposed), (bad proportions), (bad hands), (bad feet), (cloned face), (long neck), (missing arms), (missing legs), (extra fingers), (fused fingers), (poorly drawn hands), (poorly drawn face), (mutation), (deformed eyes), watermark, text, logo, signature, grainy, tiling, censored, nsfw, ugly, blurry eyes, noisy image, bad lighting, unnatural skin, asymmetry",
            width = "768",
            height = "1024",
            clip_skip = "1",
            enhance_prompt = (string?)null,
            guidance_scale = "7.5"
        };
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        try
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nGörsel üretiliyor, lütfen bekleyin...\n");
            Console.ResetColor();
            var response = await httpClient.PostAsync(Model2Endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"API Hatası: {response.StatusCode}\n{responseBody}\n");
                Console.ResetColor();
                return;
            }
            // Yanıtı ayrıştır: output veya proxy_links dizisinden ilk url'yi bul
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            string? url = null;
            if (root.TryGetProperty("output", out var outputArr) && outputArr.ValueKind == JsonValueKind.Array && outputArr.GetArrayLength() > 0)
            {
                url = outputArr[0].GetString();
            }
            else if (root.TryGetProperty("proxy_links", out var proxyArr) && proxyArr.ValueKind == JsonValueKind.Array && proxyArr.GetArrayLength() > 0)
            {
                url = proxyArr[0].GetString();
            }
            if (!string.IsNullOrWhiteSpace(url))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Görsel başarıyla oluşturuldu! URL: {url}\n");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Beklenmeyen yanıt: {responseBody}\n");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Hata: {ex.Message}\n");
            Console.ResetColor();
        }
    }
}