using Backend.BLL.DTOs;
using Backend.BLL.ParserModule;
using Backend.BLL.ParserModule.AI;

namespace Backend.BLL.QuestionGenerationModule;

public class QuizOrchestrator
{
    public readonly PdfToImageConverter PdfConverter;
    public readonly OllamaVisionOcr VisionOcr;
    public readonly CloudContentProcessor CloudProcessor;
    public readonly MoodleXmlGenerator XmlGenerator;
    public readonly QuestionRefiner Refiner; 

    public QuizOrchestrator(
        PdfToImageConverter pdfConverter,
        OllamaVisionOcr visionOcr,
        CloudContentProcessor cloudProcessor,
        MoodleXmlGenerator xmlGenerator,
        QuestionRefiner refiner
        )
    {
        PdfConverter = pdfConverter;
        VisionOcr = visionOcr;
        CloudProcessor = cloudProcessor;
        XmlGenerator = xmlGenerator;
        Refiner = refiner;
    }

    public async Task<(string TextResult, string XmlResult)> ProcessMultiplePdfsAsync(
        string[] filePaths, int mcq, int mq, int pt)
    {
        List<string> aggregatedTexts = new();

        foreach (var path in filePaths)
        {
            if (!File.Exists(path)) continue;

            byte[] pdfBytes = await File.ReadAllBytesAsync(path);
            var images = PdfConverter.GetImages(pdfBytes, 96);
            aggregatedTexts.AddRange(await VisionOcr.ProcessImagesAsync(images));
        }

        if (!aggregatedTexts.Any())
            throw new Exception("Не удалось извлечь текст ни из одного файла.");

        string rawQuestions = await CloudProcessor.AnalyzeOcrResultsAsync(aggregatedTexts, mcq, mq, pt);
        string? finalXml = await XmlGenerator.GenerateMoodleXmlAsync(rawQuestions, "Combined Study Quiz");

        return (rawQuestions, finalXml);
    }
    
    public async Task<PreviewResponseDto> CreatePreviewAsync(string[] filePaths, int mcq, int mq, int pt)
    {
        List<string> aggregatedTexts = new();
        foreach (var path in filePaths)
        {
            byte[] pdfBytes = await File.ReadAllBytesAsync(path);
            var images = PdfConverter.GetImages(pdfBytes, 96);
            aggregatedTexts.AddRange(await VisionOcr.ProcessImagesAsync(images));
        }

        string sourceContext = string.Join("\n", aggregatedTexts);
        string rawQuestions = await CloudProcessor.AnalyzeOcrResultsAsync(aggregatedTexts, mcq, mq, pt);

        return new PreviewResponseDto(rawQuestions, sourceContext);
    }

    // Этап 2: Редактирование
    public async Task<string> RefinePreviewAsync(RefineRequestDto request)
    {
        return await Refiner.RefineQuestionsAsync(request.SourceContext, request.CurrentQuestions, request.UserFeedback);
    }
    
    
}