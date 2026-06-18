using Google.Cloud.Vision.V1;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Resim yolunuzu giriniz: ");
        string imagePath = Console.ReadLine();
        Console.WriteLine();



        string credentialPath = @"D:\visionapi-499817-3bf81a5201be.json";
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);

        try
        {
            var client = ImageAnnotatorClient.Create();

            var image = Image.FromFile(imagePath);
            var response = client.DetectText(image);
            Console.WriteLine("Resimdeki Metin: ");
            Console.WriteLine();

            foreach (var annotination in response)
            {
                if (!string.IsNullOrEmpty(annotination.Description))
                {
                    Console.WriteLine(annotination.Description);
                }
            }
        }
        catch (Exception ex)
        {

            Console.WriteLine($"Bir hata olustu {ex.Message}");
        }
    }
}