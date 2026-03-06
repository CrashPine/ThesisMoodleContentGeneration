using Backend.BLL.DTOs;
using Microsoft.AspNetCore.Http;

namespace Backend.BLL.Interfaces;

public interface ITestService
{
    Task<TestResultDto> GenerateTestFromFilesAsync(GenerateRequestDto request, List<IFormFile> files);
    
    Task<PreviewResponseDto> GetTestPreviewAsync(GenerateRequestDto request, List<IFormFile> files);
    
    // Второй этап: итеративная доработка
    Task<string> RefineTestAsync(RefineRequestDto request);
    
    // Третий этап: финальная сборка и сохранение в БД (если нужно)
    Task<TestResultDto> FinalizeAndSaveTestAsync(string moodleUserId, string rawQuestions, string title);
}