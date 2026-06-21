using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;
using UglyToad.PdfPig;

class Program
{
    private static string apiKey = "";

    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        apiKey = config["GeminiApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("HATA: appsettings.json içinde 'GeminiApiKey' bulunamadı.");
            Console.ResetColor();
            return;
        }

        Console.Write("PDF Dosya Yolunu Giriniz: ");
        string pdfPath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(pdfPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("PDF dosya yolu boş olamaz.");
            Console.ResetColor();
            return;
        }

        if (!File.Exists(pdfPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("PDF dosyası bulunamadı.");
            Console.WriteLine($"Girilen yol: {pdfPath}");
            Console.ResetColor();
            return;
        }

        Console.WriteLine("Pdf Analizi AI tarafından yapılıyor...");
        Console.WriteLine();

        string pdfText = ExtractTextFromPdf(pdfPath);

        if (string.IsNullOrWhiteSpace(pdfText))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("PDF içeriği okunamadı veya PDF metin içermiyor.");
            Console.ResetColor();
            return;
        }

        await AnalyzeWithAI(pdfText, "Pdf İçeriği");

        Console.WriteLine();
        Console.WriteLine("Programı kapatmak için bir tuşa basın...");
        Console.ReadKey();

        static string ExtractTextFromPdf(string filePath)
        {
            StringBuilder text = new StringBuilder();

            using (PdfDocument pdf = PdfDocument.Open(filePath))
            {
                foreach (var page in pdf.GetPages())
                {
                    text.AppendLine(page.Text);
                }
            }

            return text.ToString();
        }

        static async Task AnalyzeWithAI(string text, string sourceType)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

                var endpoint =
                    "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

                if (text.Length > 20000)
                {
                    text = text.Substring(0, 20000);
                }

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
                                        "Kullanıcının gönderdiği PDF içeriğini analiz et ve Türkçe olarak özetle. " +
                                        "Yanıtlarını sadece Türkçe ver. " +
                                        "Cevabında şu başlıkları kullan:\n" +
                                        "1. Genel Özet\n" +
                                        "2. Ana Konular\n" +
                                        "3. Öne Çıkan Bilgiler\n" +
                                        "4. Kısa Değerlendirme\n\n" +
                                        $"Analyze and summarize the following {sourceType}:\n\n{text}"
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

                string json = JsonConvert.SerializeObject(requestBody);

                HttpContent content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

                HttpResponseMessage response = await client.PostAsync(endpoint, content);
                string responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    dynamic result = JsonConvert.DeserializeObject<dynamic>(responseJson);

                    try
                    {
                        string analysis = result.candidates[0].content.parts[0].text.ToString();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\nAI Analizi ({sourceType}):");
                        Console.ResetColor();

                        Console.WriteLine();
                        Console.WriteLine(analysis);
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Gemini cevabı beklenen formatta dönmedi.");
                        Console.WriteLine(responseJson);
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Hata:");
                    Console.WriteLine($"{(int)response.StatusCode} - {response.StatusCode}");
                    Console.WriteLine(responseJson);
                    Console.ResetColor();
                }
            }
        }
    }
}