using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;

namespace NetCoreAI.Project20_RecipeSuggestionWithGeminiAI.Models
{
    public class GeminiAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        private const string GeminiUrl =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        public GeminiAIService()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _apiKey = configuration["GeminiApiKey"];

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new Exception("appsettings.json içinde 'GeminiApiKey' bulunamadı.");
            }

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
        }

        public async Task<string> GetRecipeAsync(string ingredients)
        {
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
                                    "Sen profesyonel bir aşçısın. " +
                                    "Kullanıcının elindeki malzemelere göre Türkçe yemek tarifi öner. " +
                                    "Tarifte şu başlıklar olsun: Yemek Adı, Malzemeler, Hazırlanışı, Pişirme Süresi ve Tavsiye.\n\n" +
                                    $"Elimde şu malzemeler var: {ingredients}. Ne yapabilirim?"
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 1000,
                    thinkingConfig = new
                    {
                        thinkingBudget = 0
                    }
                }
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);

            var response = await _httpClient.PostAsync(
                GeminiUrl,
                new StringContent(jsonRequest, Encoding.UTF8, "application/json")
            );

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Gemini API hatası: {(int)response.StatusCode} - {response.StatusCode}\n{responseBody}";
            }

            using var doc = JsonDocument.Parse(responseBody);

            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out JsonElement candidates) ||
                candidates.GetArrayLength() == 0)
            {
                return "Gemini cevap döndürmedi.";
            }

            var firstCandidate = candidates[0];

            if (!firstCandidate.TryGetProperty("content", out JsonElement content))
            {
                return "Gemini cevabında 'content' alanı bulunamadı.";
            }

            if (!content.TryGetProperty("parts", out JsonElement parts) ||
                parts.GetArrayLength() == 0)
            {
                return "Gemini cevabında 'parts' alanı bulunamadı.";
            }

            if (!parts[0].TryGetProperty("text", out JsonElement textElement))
            {
                return "Gemini cevabında 'text' alanı bulunamadı.";
            }

            return textElement.GetString() ?? "Tarif oluşturulamadı.";
        }
    }
}
