using Backend.BLL.ParserModule;
using Backend.BLL.ParserModule.AI;

namespace Backend.BLL.QuestionGenerationModule;

public class QuizOrchestrator : IDisposable
{
    public readonly PdfToImageConverter PdfConverter;
    
    public readonly OllamaVisionOcr VisionOcr;
    public readonly CloudContentProcessor CloudProcessor;
    public readonly MoodleXmlGenerator XmlGenerator;

    public QuizOrchestrator(
        string laptopOllamaUrl, 
        string cloudOllamaUrl, 
        string apiKey, 
        
        string cloudModel,
        string ocrModel
        )
    {
        PdfConverter = new PdfToImageConverter();
        
        VisionOcr = new OllamaVisionOcr(laptopOllamaUrl, ocrModel); 
        CloudProcessor = new CloudContentProcessor(cloudOllamaUrl, apiKey, cloudModel);
        XmlGenerator = new MoodleXmlGenerator(cloudOllamaUrl, apiKey, cloudModel);
    }

    /// <summary>
    /// Основной рабочий процесс для обработки N файлов.
    /// </summary>
    public async Task<(string TextResult, string XmlResult)> ProcessMultiplePdfsAsync(
        string[] filePaths, 
        int mcq, int mq, int pt)
    {
        List<string> aggregatedTexts = new List<string>();

        foreach (var path in filePaths)
        {
            if (!File.Exists(path)) continue;

            Console.WriteLine($"\n[Orchestrator] Обработка файла: {Path.GetFileName(path)}");
            
            byte[] pdfBytes = await File.ReadAllBytesAsync(path);
            var images = PdfConverter.GetImages(pdfBytes, dpi: 96);

            var pageTexts = await VisionOcr.ProcessImagesAsync(images);
            
            aggregatedTexts.AddRange(pageTexts);
        }

        if (aggregatedTexts.Count == 0)
            throw new Exception("Не удалось извлечь текст ни из одного файла.");

        Console.WriteLine("\n[Orchestrator] Генерация вопросов по всем материалам...");

        string rawQuestions = await CloudProcessor.AnalyzeOcrResultsAsync(aggregatedTexts, mcq, mq, pt);

        string? finalXml = await XmlGenerator.GenerateMoodleXmlAsync(rawQuestions, "Combined Study Quiz");

        return (rawQuestions, finalXml);
    }

    public void Dispose()
    {
        CloudProcessor.Dispose();
        XmlGenerator.Dispose();
    }
}