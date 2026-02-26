using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using OllamaSharp;
using OllamaSharp.Models;

namespace Backend.BLL.QuestionGenerationModule;

/// <summary>
/// Принимает сырой текст (ответ модели — список сгенерированных вопросов),
/// посылает в модель задачу по конвертации в Moodle XML и возвращает чистый XML.
/// </summary>
public class MoodleXmlGenerator : IDisposable
{
    private readonly OllamaApiClient _ollamaClient;
    private readonly string _modelName;

    public MoodleXmlGenerator(string baseUri, string apiKey, string modelName)
    {
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUri),
            Timeout = TimeSpan.FromMinutes(10)
        };

        if (!string.IsNullOrEmpty(apiKey))
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        _ollamaClient = new OllamaApiClient(httpClient);
    }

    /// <summary>
    /// Основной метод.
    /// aiGeneratedText — строка с результатом предыдущей модели (формат: MCQ, Matching, Problem tasks — как в вашем примере).
    /// quizTitle — заголовок квиза, который будет вставлен в Moodle XML.
    /// Возвращает: строку с корректным Moodle XML или сообщение об ошибке (начинается с "[XML Error]:" / "[Cloud Error]:").
    /// </summary>
    public async Task<string?> GenerateMoodleXmlAsync(string aiGeneratedText, string quizTitle = "Auto-generated quiz")
    {
        if (string.IsNullOrWhiteSpace(aiGeneratedText))
            throw new ArgumentException("aiGeneratedText is empty", nameof(aiGeneratedText));

            
        var prompt = new StringBuilder();
        prompt.AppendLine("You are an expert in creating Moodle-compatible XML for Moodle quiz import.");
        prompt.AppendLine("INPUT: the provided text contains questions already generated (Multiple Choice, Matching, Problem tasks).");
        prompt.AppendLine("TASK: Parse the input and produce a valid Moodle XML quiz document (ONLY XML — no explanations, no extra text).");
        prompt.AppendLine();
        prompt.AppendLine("REQUIREMENTS (VERY IMPORTANT):");
        prompt.AppendLine("- The output MUST be a valid XML document with <?xml version=\"1.0\" encoding=\"UTF-8\"?> header and a single root <quiz> element.");
        prompt.AppendLine("- For Multiple Choice: generate <question type=\"multichoice\"> elements, include <name>, <questiontext> (format=\"html\"), four <answer> elements A..D, and mark the correct one with <fraction>100. Use shuffleanswers=\"1\".");
        prompt.AppendLine("- For Matching: use <question type=\"matching\"> with <subquestion> items and proper <answer> nodes (Moodle matching schema).");
        prompt.AppendLine("- For Problem/numeric tasks: use <question type=\"numerical\"> or <question type=\"shortanswer\"> if appropriate; set exact answers and tolerances if needed.");
        prompt.AppendLine("- Use the provided quiz title: " + SecurityElement.Escape(quizTitle));
        prompt.AppendLine("- Preserve language and wording from the input; do not invent external facts or references.");
        prompt.AppendLine("- Do NOT output anything except the XML document (no backticks, no commentary).");
        prompt.AppendLine();
        prompt.AppendLine("INPUT TEXT START");
        prompt.AppendLine(aiGeneratedText);
        prompt.AppendLine("INPUT TEXT END");
        prompt.AppendLine();
        prompt.AppendLine("If you cannot parse a specific question, omit it rather than outputting invalid XML.");

        var request = new GenerateRequest
        {
            Model = _modelName,
            Prompt = prompt.ToString(),
            Stream = true
        };

        var sb = new StringBuilder();

        try
        {
            await foreach (var chunk in _ollamaClient.GenerateAsync(request))
            {
                if (chunk?.Response != null)
                {
                    sb.Append(chunk.Response);
                    Console.Write(chunk.Response); 
                }
            }
        }
        catch (Exception ex)
        {
            return $"[Cloud Error]: {ex.Message}";
        }

        var rawOutput = sb.ToString();

        var quizXml = ExtractQuizXml(rawOutput);

        if (string.IsNullOrEmpty(quizXml))
        {
            return "[XML Error]: model returned no <quiz> block. Raw output:\n" + TruncateForLog(rawOutput);
        }

        if (!TryValidateXml(quizXml, out var formattedXml, out var xmlError))
        {
            return $"[XML Error]: parsing failed: {xmlError}\nModel output (truncated):\n{TruncateForLog(rawOutput)}";
        }

        if (formattedXml != null && !formattedXml.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
        {
            formattedXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + formattedXml;
        }

        return formattedXml;
    }

    /// <summary>
    /// Ищет первый <quiz>...</quiz> блок в тексте.
    /// </summary>
    private static string? ExtractQuizXml(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var regex = new Regex(@"<\s*quiz\b[^>]*>.*?<\s*/\s*quiz\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var match = regex.Match(text);
        if (match.Success)
            return match.Value;

        var regex2 = new Regex(@"<\?xml[\s\S]*?\?>\s*<\s*quiz\b[^>]*>[\s\S]*?<\s*/\s*quiz\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        match = regex2.Match(text);
        if (match.Success)
            return match.Value;

        return null;
    }

    /// <summary>
    /// Проверка на well-formed XML. Возвращает отформатированную строку при успехе.
    /// </summary>
    private static bool TryValidateXml(string xml, out string? formatted, out string? error)
    {
        formatted = null;
        error = null;
        try
        {
            var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            formatted = doc.Declaration != null
                ? doc.Declaration + Environment.NewLine + doc
                : doc.ToString();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string TruncateForLog(string s, int max = 4000)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Length <= max ? s : s.Substring(0, max) + "\n...[truncated]";
    }

    public void Dispose() => _ollamaClient.Dispose();
}