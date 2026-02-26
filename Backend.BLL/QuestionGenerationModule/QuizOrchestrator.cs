using Backend.BLL.ParserModule;
using Backend.BLL.ParserModule.AI;

namespace Backend.BLL.QuestionGenerationModule;

public class QuizOrchestrator : IDisposable
{
    private readonly PdfToImageConverter _pdfConverter;
    private readonly OllamaVisionOcr _visionOcr;
    private readonly CloudContentProcessor _cloudProcessor;
    private readonly MoodleXmlGenerator _xmlGenerator;

    public QuizOrchestrator(
        string laptopOllamaUrl, 
        string cloudOllamaUrl, 
        string apiKey, 
        
        string cloudModel,
        string ocrModel
        )
    {
        _pdfConverter = new PdfToImageConverter();
        _visionOcr = new OllamaVisionOcr(laptopOllamaUrl, ocrModel); // Использует glm-ocr по умолчанию
        _cloudProcessor = new CloudContentProcessor(cloudOllamaUrl, apiKey, cloudModel);
        _xmlGenerator = new MoodleXmlGenerator(cloudOllamaUrl, apiKey, cloudModel);
    }

    /// <summary>
    /// Основной рабочий процесс для обработки N файлов.
    /// </summary>
    public async Task<(string TextResult, string XmlResult)> ProcessMultiplePdfsAsync(
        string[] filePaths, 
        int mcq, int mq, int pt)
    {
        List<string> aggregatedTexts = new List<string>();

        // Цикл обработки N файлов
        foreach (var path in filePaths)
        {
            if (!File.Exists(path)) continue;

            Console.WriteLine($"\n[Orchestrator] Обработка файла: {Path.GetFileName(path)}");
            
            // 1. PDF -> Images
            byte[] pdfBytes = await File.ReadAllBytesAsync(path);
            var images = _pdfConverter.GetImages(pdfBytes, dpi: 96);

            // 2. Images -> OCR
            var pageTexts = await _visionOcr.ProcessImagesAsync(images);
            
            // Собираем текст в общую базу данных для текущей сессии
            aggregatedTexts.AddRange(pageTexts);
        }

        if (aggregatedTexts.Count == 0)
            throw new Exception("Не удалось извлечь текст ни из одного файла.");

        Console.WriteLine("\n[Orchestrator] Генерация вопросов по всем материалам...");

        // 3. Анализ и генерация вопросов
        string rawQuestions = await _cloudProcessor.AnalyzeOcrResultsAsync(aggregatedTexts, mcq, mq, pt);

        // 4. Валидация и конвертация в Moodle XML
        string? finalXml = await _xmlGenerator.GenerateMoodleXmlAsync(rawQuestions, "Combined Study Quiz");

        return (rawQuestions, finalXml);
    }

    public void Dispose()
    {
        _cloudProcessor.Dispose();
        _xmlGenerator.Dispose();
    }
}