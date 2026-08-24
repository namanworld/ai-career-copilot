using AiCareerCopilot.Api.Models;

namespace AiCareerCopilot.Api.Services;

public interface IInterviewService
{
    Task<InterviewQuestionsResponse> GenerateInterviewQuestionsAsync(string resumeText, string jobDescription);
}

public class InterviewService : IInterviewService
{
    private readonly IGeminiClientService _geminiClient;

    private const string SystemPrompt = @"
You are a Principal Technical Interviewer and Hiring Manager.
Generate 5 targeted, high-signal interview questions tailored to:
1. Gaps or transition areas between the resume and Job Description.
2. Deep-dive technical scenarios on projects explicitly listed in the resume.
3. Behavioral and leadership expectations mentioned in the JD.

For each question, provide:
- category: ""Technical"", ""Behavioral"", or ""System Design""
- question: The interview question text
- rationale: Why this question matters given the candidate's background & JD
- suggested_focus: Key talking points the candidate should highlight

Expected JSON structure:
{
  ""questions"": [
    {
      ""category"": ""Technical"",
      ""question"": ""..."",
      ""rationale"": ""..."",
      ""suggested_focus"": ""...""
    }
  ]
}
";

    public InterviewService(IGeminiClientService geminiClient)
    {
        _geminiClient = geminiClient;
    }

    public async Task<InterviewQuestionsResponse> GenerateInterviewQuestionsAsync(string resumeText, string jobDescription)
    {
        string userPrompt = $@"
TARGET JOB DESCRIPTION:
{jobDescription}

CANDIDATE RESUME:
{resumeText}
";
        return await _geminiClient.GenerateStructuredOutputAsync<InterviewQuestionsResponse>(SystemPrompt, userPrompt);
    }
}
