namespace Backend.API.Configuration;

public class QuizOrchestratorSettings
{
    public string Url { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string OcrModel { get; set; } = string.Empty;
    public string SummaryModel { get; set; } = string.Empty;
    public string XmlModel { get; set; } = string.Empty;
}