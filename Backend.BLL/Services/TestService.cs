using AutoMapper;
using Backend.BLL.DTOs;
using Backend.BLL.Interfaces;
using Backend.BLL.QuestionGenerationModule;
using Backend.DAL;
using Backend.DAL.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Backend.BLL.Services;

public class TestService(
    QuizOrchestrator orchestrator, 
    ApplicationDbContext context, 
    IMapper mapper) : ITestService
{
    public async Task<TestResultDto> GenerateTestFromFilesAsync(GenerateRequestDto request, List<IFormFile> files)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.MoodleUserId == request.MoodleUserId)
                   ?? new User { MoodleUserId = request.MoodleUserId };
        
        if (user.Id == Guid.Empty) context.Users.Add(user);

        List<string> allExtractedTexts = [];

        foreach (var file in files)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            byte[] fileBytes = ms.ToArray();

            var images = orchestrator.PdfConverter.GetImages(fileBytes, 96);
            var pageTexts = await orchestrator.VisionOcr.ProcessImagesAsync(images);
            
            string combinedFileText = string.Join("\n", (IEnumerable<string>)pageTexts);
            allExtractedTexts.Add(combinedFileText);

            context.TextSources.Add(new TextSource
            {
                User = user,
                FileName = file.FileName,
                RawText = combinedFileText
            });
        }

        string rawQuestions = await orchestrator.CloudProcessor.AnalyzeOcrResultsAsync(
            allExtractedTexts, 
            request.McqCount, 
            request.MatchingCount, 
            request.ProblemCount);

        string? xmlResult = await orchestrator.XmlGenerator.GenerateMoodleXmlAsync(rawQuestions, "Moodle Quiz");

        
        
        var test = new Test
        {
            User = user,
            RawGeneratedText = rawQuestions,
            MoodleXmlContent = xmlResult ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        context.Tests.Add(test);
        await context.SaveChangesAsync();

        return mapper.Map<TestResultDto>(test);
    }
    
    public async Task<PreviewResponseDto> GetTestPreviewAsync(GenerateRequestDto request, List<IFormFile> files)
    {
        List<string> allExtractedTexts = [];

        foreach (var file in files)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            byte[] fileBytes = ms.ToArray();

            var images = orchestrator.PdfConverter.GetImages(fileBytes, 96);
            var pageTexts = await orchestrator.VisionOcr.ProcessImagesAsync(images);
            allExtractedTexts.Add(string.Join("\n", (IEnumerable<string>)pageTexts));
        }

        string sourceContext = string.Join("\n", allExtractedTexts);
        
        // Генерируем только текст вопросов через оркестратор
        string rawQuestions = await orchestrator.CloudProcessor.AnalyzeOcrResultsAsync(
            allExtractedTexts, 
            request.McqCount, 
            request.MatchingCount, 
            request.ProblemCount);

        return new PreviewResponseDto(rawQuestions, sourceContext);
    }

    public async Task<string> RefineTestAsync(RefineRequestDto request)
    {
        // Просто пробрасываем запрос в оркестратор (через новый класс Refiner)
        return await orchestrator.Refiner.RefineQuestionsAsync(
            request.SourceContext, 
            request.CurrentQuestions, 
            request.UserFeedback);
    }

    public async Task<TestResultDto> FinalizeAndSaveTestAsync(string moodleUserId, string rawQuestions, string title)
    {
        // Ищем или создаем пользователя
        var user = await context.Users.FirstOrDefaultAsync(u => u.MoodleUserId == moodleUserId)
                   ?? new User { MoodleUserId = moodleUserId };
        
        if (user.Id == Guid.Empty) context.Users.Add(user);

        // Генерируем финальный XML
        string? xmlResult = await orchestrator.XmlGenerator.GenerateMoodleXmlAsync(rawQuestions, title);

        var test = new Test
        {
            User = user,
            RawGeneratedText = rawQuestions,
            MoodleXmlContent = xmlResult ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        context.Tests.Add(test);
        await context.SaveChangesAsync();

        return mapper.Map<TestResultDto>(test);
    }

}