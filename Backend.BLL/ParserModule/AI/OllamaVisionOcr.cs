using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace Backend.BLL.ParserModule.AI;

public class OllamaVisionOcr
{
    private readonly OllamaApiClient _client;
    private readonly string _modelName;

    public OllamaVisionOcr(OllamaApiClient client, string modelName)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
    }

    public async Task<List<string>> ProcessImagesAsync(List<byte[]> images)
    {
        var results = new List<string>();
        int count = 1;

        foreach (var imageBytes in images)
        {
            Console.WriteLine($"[OllamaSharp] Обработка страницы {count++}...");

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
                if (response?.Message?.Content != null)
                    fullResponse += response.Message.Content;
            }

            results.Add(fullResponse.Trim());
        }

        return results;
    }
}