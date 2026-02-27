using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using OllamaSharp;
using OllamaSharp.Models;

namespace Backend.BLL.QuestionGenerationModule;

public class MoodleXmlGenerator
{
    private readonly OllamaApiClient _ollamaClient;
    private readonly string _modelName;

    public MoodleXmlGenerator(OllamaApiClient client, string modelName)
    {
        _ollamaClient = client ?? throw new ArgumentNullException(nameof(client));
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
    }

    public async Task<string?> GenerateMoodleXmlAsync(string aiGeneratedText, string quizTitle = "Auto-generated quiz")
    {
        if (string.IsNullOrWhiteSpace(aiGeneratedText))
            throw new ArgumentException("aiGeneratedText is empty", nameof(aiGeneratedText));

        string prompt = $@"You are an expert in creating Moodle-compatible XML for Moodle quiz import.
INPUT: the provided text contains questions already generated (Multiple Choice, Matching, Problem tasks).
TASK: Parse the input and produce a valid Moodle XML quiz document (ONLY XML — no explanations, no extra text).

### REQUIREMENTS (VERY IMPORTANT):
- The output MUST be a valid XML document with <?xml version=""1.0"" encoding=""UTF-8""?> header and a single root <quiz> element.
- For Multiple Choice: generate <question type=""multichoice""> elements, include <name>, <questiontext> (format=""html""), four <answer> elements A..D, and mark the correct one with <fraction>100. Use shuffleanswers=""1"".
- For Matching: use <question type=""matching""> with <subquestion> items and proper <answer> nodes (Moodle matching schema).
- For Problem/numeric tasks: use <question type=""numerical""> or <question type=""shortanswer""> if appropriate; set exact answers and tolerances if needed.
- Use the provided quiz title: {SecurityElement.Escape(quizTitle)}
- Preserve language and wording from the input; do not invent external facts or references.
- Do NOT output anything except the XML document (no backticks, no commentary).

### INPUT TEXT START
{aiGeneratedText}
### INPUT TEXT END

If you cannot parse a specific question, omit it rather than outputting invalid XML."; // оставляем твой prompt для XML

        var request = new GenerateRequest
        {
            Model = _modelName,
            Prompt = prompt.ToString(),
            Stream = true
        };

        var sb = new StringBuilder();
        await foreach (var chunk in _ollamaClient.GenerateAsync(request))
        {
            if (chunk?.Response != null)
            {
                sb.Append(chunk.Response);
                Console.Write(chunk.Response);
            }
        }

        var rawOutput = sb.ToString();
        var quizXml = ExtractQuizXml(rawOutput);

        if (string.IsNullOrEmpty(quizXml))
            return "[XML Error]: model returned no <quiz> block.";

        return quizXml;
    }

    private static string? ExtractQuizXml(string text) => 
        Regex.Match(text, @"<\s*quiz\b[^>]*>.*?<\s*/\s*quiz\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase).Value;
}