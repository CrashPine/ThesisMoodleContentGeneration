using Backend.BLL.DTOs;
using Microsoft.AspNetCore.Http;

namespace Backend.BLL.Interfaces;

public interface ITestService
{
    Task<TestResultDto> GenerateTestFromFilesAsync(GenerateRequestDto request, List<IFormFile> files);

}