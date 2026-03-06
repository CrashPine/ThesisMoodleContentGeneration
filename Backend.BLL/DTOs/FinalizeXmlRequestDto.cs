namespace Backend.BLL.DTOs;

public record FinalizeRequestDto(
    string MoodleUserId,
    string RawQuestions,
    string QuizTitle = "Moodle Quiz"
);