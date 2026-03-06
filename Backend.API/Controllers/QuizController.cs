using Backend.BLL.DTOs;
using Backend.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Backend.DAL;
namespace Backend.API.Controllers;

//оптимизировать
//решение распознавание рукописных конспектов

[ApiController]
[Route("api/[controller]")]
public class QuizController(ITestService quizService, ApplicationDbContext context) : ControllerBase
{
    [HttpPost("generate")]
    [Consumes("multipart/form-data")] 
    public async Task<ActionResult<TestResultDto>> Generate([FromForm] GenerateRequestDto request, List<IFormFile> files)
    {
        if (files == null || files.Count == 0) return BadRequest("Файлы не выбраны");
        
        var result = await quizService.GenerateTestFromFilesAsync(request, files);
        return Ok(result);
    }

    [HttpGet("download-xml/{id}")]
    public async Task<IActionResult> DownloadXml(Guid id)
    {
        var test = await context.Tests.FindAsync(id);
        if (test == null) return NotFound();

        var bytes = Encoding.UTF8.GetBytes(test.MoodleXmlContent);
        return File(bytes, "application/xml", $"quiz_{id}.xml");
    }
    
    /// <summary>
    /// ЭТАП 1: Загрузка файлов и генерация первого варианта вопросов (Preview).
    /// База данных на этом этапе не используется.
    /// </summary>
    [HttpPost("preview")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PreviewResponseDto>> GeneratePreview(
        [FromForm] GenerateRequestDto request, 
        List<IFormFile> files)
    {
        if (files == null || files.Count == 0) 
            return BadRequest("Файлы не выбраны");

        // Сервис парсит PDF и возвращает текст вопросов + исходный контекст
        var result = await quizService.GetTestPreviewAsync(request, files);
        return Ok(result);
    }

    /// <summary>
    /// ЭТАП 2: Итеративная доработка вопросов на основе промпта пользователя.
    /// Можно вызывать многократно.
    /// </summary>
    [HttpPost("refine")]
    public async Task<ActionResult<string>> RefineQuestions([FromBody] RefineRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.UserFeedback))
            return BadRequest("Укажите, что именно нужно исправить (Feedback)");

        // Возвращает только строку с обновленным текстом вопросов
        var updatedQuestions = await quizService.RefineTestAsync(request);
        
        return Ok(new { updatedQuestions });
    }

    /// <summary>
    /// ЭТАП 3: Финализация. Генерация XML, сохранение в БД и возврат готового результата.
    /// </summary>
    [HttpPost("finalize")]
    public async Task<ActionResult<TestResultDto>> FinalizeTest([FromBody] FinalizeRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RawQuestions))
            return BadRequest("Текст вопросов пуст");

        // Генерирует XML, сохраняет Test в БД и возвращает DTO с ID и контентом
        var result = await quizService.FinalizeAndSaveTestAsync(
            request.MoodleUserId, 
            request.RawQuestions, 
            request.QuizTitle);

        return Ok(result);
    }


    
    
    
    
    
}