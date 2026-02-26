using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace Backend.BLL.ParserModule.AI;

public class OllamaVisionOcr
{
    private readonly OllamaApiClient _client;
    private readonly string _modelName;

    public OllamaVisionOcr(string ollamaUrl = "url", string modelName = "model")
    {
        _modelName = modelName;
        _client = new OllamaApiClient(ollamaUrl);
    }

    public async Task<List<string>> ProcessImagesAsync(List<byte[]> images)
    {
        var results = new List<string>();
        int count = 1;

        foreach (var imageBytes in images)
        {
            Console.WriteLine($"[OllamaSharp 5.x] Обработка страницы {count++}...");

            string base64Image = Convert.ToBase64String(imageBytes);

            var request = new ChatRequest
            {
                Model = _modelName,
                Messages = new List<Message>
                {
                    new Message
                    {
                        Role = "user",
                        Content = "ocr the image",
                        Images = new[] { base64Image } 
                    }
                },
                Stream = false 
            };

            string fullResponse = "";

            await foreach (var response in _client.ChatAsync(request))
            {
                fullResponse += response?.Message.Content;
            }

            results.Add(fullResponse.Trim());
        }

        return results;
    }
}