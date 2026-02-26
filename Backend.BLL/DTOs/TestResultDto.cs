namespace Backend.BLL.DTOs;

public record TestResultDto(
    Guid Id, 
    string RawGeneratedText, 
    string MoodleXmlContent, 
    DateTime CreatedAt);
    
    
    