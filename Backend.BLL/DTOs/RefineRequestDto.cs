namespace Backend.BLL.DTOs;

public record RefineRequestDto(
    string SourceContext,
    string CurrentQuestions,
    string UserFeedback
);