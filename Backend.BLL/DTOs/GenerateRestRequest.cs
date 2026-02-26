namespace Backend.BLL.DTOs;

public record GenerateRequestDto(
    string MoodleUserId, 
    int McqCount, 
    int MatchingCount, 
    int ProblemCount);
    
    