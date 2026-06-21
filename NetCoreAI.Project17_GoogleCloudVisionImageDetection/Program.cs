using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

class Program
{
    private static readonly string googleApiKey = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build()["GoogleVisionApiKey"]!;
    private static readonly string imagePath = "C:\\Users\\e_741\\source\\repos\\NetCoreAI\\NetCoreAI.Project17_GoogleCloudVisionImageDetection\\2.png";

    static async Task Main()
    {
        Console.WriteLine("Google Vision Apı ile Görsel Nesne Tespiti Yapılıyor...");
        string response = await DetectObjects(imagePath);

        Console.WriteLine("----Tespit Edilen Nesneler----\n");
        Console.WriteLine(response);

    }
    static async Task<string> DetectObjects(string path)
    {
        using var client = new HttpClient();
        string apiUrl = $"https://vision.googleapis.com/v1/images:annotate?key={googleApiKey}";

        byte[] imageBytes = File.ReadAllBytes(path);
        string base64Image = Convert.ToBase64String(imageBytes);

        var requestBody = new
        {
            requests = new[]
            {
                    new
                    {
                        image = new { content = base64Image },
                        features = new[] { new { type = "LABEL_DETECTION", maxResults = 10 } }
                    }
                }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(apiUrl, jsonContent);
        string responseContent = await response.Content.ReadAsStringAsync();

        return responseContent;
    }
}