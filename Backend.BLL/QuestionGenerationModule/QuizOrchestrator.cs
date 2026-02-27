using Backend.BLL.ParserModule;
using Backend.BLL.ParserModule.AI;

namespace Backend.BLL.QuestionGenerationModule;

public class QuizOrchestrator
{
    public readonly PdfToImageConverter PdfConverter;
    public readonly OllamaVisionOcr VisionOcr;
    public readonly CloudContentProcessor CloudProcessor;
    public readonly MoodleXmlGenerator XmlGenerator;

    public QuizOrchestrator(
        PdfToImageConverter pdfConverter,
        OllamaVisionOcr visionOcr,
        CloudContentProcessor cloudProcessor,
        MoodleXmlGenerator xmlGenerator)
    {
        PdfConverter = pdfConverter;
        VisionOcr = visionOcr;
        CloudProcessor = cloudProcessor;
        XmlGenerator = xmlGenerator;
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
}