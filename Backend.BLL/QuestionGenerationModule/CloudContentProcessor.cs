using System.Text;
using OllamaSharp;
using OllamaSharp.Models;

namespace Backend.BLL.QuestionGenerationModule;

public class CloudContentProcessor
{
    private readonly OllamaApiClient _ollamaClient;
    private readonly string _modelName;

    public CloudContentProcessor(OllamaApiClient client, string modelName)
    {
        _ollamaClient = client ?? throw new ArgumentNullException(nameof(client));
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
    }

    public async Task<string> AnalyzeOcrResultsAsync(List<string> ocrResults, int mcqCount, int matchingCount, int problemCount)
    {
        var fullContext = new StringBuilder();
        for (int i = 0; i < ocrResults.Count; i++)
        {
            fullContext.AppendLine($"--- СТРАНИЦА {i + 1} ---\n{ocrResults[i]}\n");
        }

        string prompt = $@"You are an expert Educational Architect and Assessment Specialist. 
Your goal is to design a high-stakes exam that measures deep conceptual understanding, analytical skills, and the ability to apply knowledge, rather than simple recall.

### QUANTITY REQUIREMENTS:
- Create EXACTLY {mcqCount} Multiple Choice Questions (MCQ).
- Create EXACTLY {matchingCount} Matching Questions (MQ).
- Create EXACTLY {problemCount} Problem-solving Tasks (PT).

### COGNITIVE STRATEGY (Bloom's Taxonomy):
1. NO RECALL: Avoid questions like 'What is the definition of...?' or 'What is the formula for...?'.
2. APPLY & ANALYZE: Focus on 'What happens if...?', 'Why does this occur?', 'Which of the following is a logical consequence of...?', 'Compare X and Y'.
3. CONCEPTUAL ERRORS: Design questions that target common student misconceptions or 'traps' (e.g., confusing sign, units, or cause-effect vs correlation).
4. MULTI-STEP REASONING: Questions should require combining at least two concepts from the text to find the answer.

### RULES FOR QUESTIONS:
- MCQ DISTRACTORS: All wrong options must be 'plausible'. They should represent typical mistakes: a sign error, a misinterpretation of a term, or a partially correct but incomplete answer. Avoid 'none of the above'.
- MATCHING: Do not match terms to definitions. Match 'Concepts to Scenarios', 'Causes to Effects', or 'Principles to Real-world Examples'.
- PROBLEM-SOLVING: Provide a specific case study or numerical scenario. The student must derive the solution using the logic provided in the text.

### CORE INSTRUCTIONS:
1. LANGUAGE: Same as the provided materials.
2. SOURCE FIDELITY: Use ONLY the provided text, but extrapolate logic and applications from it.
3. OCR CORRECTION: Fix typos logically.

### OUTPUT FORMAT (STRICT):

1. MULTIPLE CHOICE QUESTIONS ({mcqCount} items):
Question: [Scenario-based or analytical question]
A) [Plausible distractor]
B) [Correct answer]
C) [Common misconception]
D) [Related but incorrect concept]
Correct Answer: [Letter]
Explanation: [Briefly explain why the correct answer is right and why the main distractor is a common mistake]

2. MATCHING QUESTIONS ({matchingCount} items):
List 1: [Contextual items/scenarios/logical steps]
List 2: [Outcomes/principles/interpretations]
Correct Pairs: [1-B, 2-A...]

3. PROBLEM-SOLVING TASKS ({problemCount} items):
Task: [Comprehensive problem requiring analysis]
Final Result: [Concise answer]
Step-by-step Logic: [Brief explanation of the solution path]

---
STUDY MATERIALS (OCR DATA):
{fullContext}"; // оставляем твой большой prompt без изменений

        var request = new GenerateRequest
        {
            Model = _modelName,
            Prompt = prompt,
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

        return sb.ToString();
    }
}