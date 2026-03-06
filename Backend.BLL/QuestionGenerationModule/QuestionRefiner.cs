using System.Text;
using OllamaSharp;
using OllamaSharp.Models;

namespace Backend.BLL.QuestionGenerationModule;

public class QuestionRefiner
{
    private readonly OllamaApiClient _ollamaClient;
    private readonly string _modelName;

    public QuestionRefiner(OllamaApiClient client, string modelName)
    {
        _ollamaClient = client;
        _modelName = modelName;
    }

    public async Task<string> RefineQuestionsAsync(string sourceText, string currentQuestions, string userFeedback)
    {
        string prompt = $@"
You are an expert Educational Architect. 
I have already generated some exam questions based on the provided study materials, but they need improvements based on user feedback.

### SOURCE STUDY MATERIALS:
{sourceText}

### CURRENT GENERATED QUESTIONS:
{currentQuestions}

### USER FEEDBACK / INSTRUCTIONS FOR CHANGE:
""{userFeedback}""

### TASK:
- Rewrite or adjust the questions according to the user feedback.
- Maintain the SAME strict output format (Multiple Choice, Matching, Problem-solving).
- Ensure all pedagogical rules (no recall, plausible distractors, Bloom's taxonomy) are still followed.
- DO NOT include commentary, just the updated questions.

### OUTPUT FORMAT (STRICT):
(Same as before: 1. MULTIPLE CHOICE QUESTIONS, 2. MATCHING QUESTIONS, 3. PROBLEM-SOLVING TASKS)";

        var request = new GenerateRequest
        {
            Model = _modelName,
            Prompt = prompt,
            Stream = true
        };

        var sb = new StringBuilder();
        await foreach (var chunk in _ollamaClient.GenerateAsync(request))
        {
            if (chunk?.Response != null) sb.Append(chunk.Response);
        }

        return sb.ToString();
    }
}