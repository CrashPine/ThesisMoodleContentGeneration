using Backend.BLL.DTOs;
using Backend.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Backend.DAL;

namespace Backend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuizController(ITestService quizService, ApplicationDbContext context) : ControllerBase
{
    [HttpPost("generate")]
    [Consumes("multipart/form-data")] // Важно для Swagger
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
}