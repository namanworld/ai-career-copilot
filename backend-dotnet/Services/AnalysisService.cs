using AiCareerCopilot.Api.Models;

namespace AiCareerCopilot.Api.Services;

public interface IAnalysisService
{
    Task<AnalysisResponse> AnalyzeFitAsync(string resumeText, string jobDescription);
}

public class AnalysisService : IAnalysisService
{
    private readonly IGeminiClientService _geminiClient;
    private readonly IKnowledgeBaseService _knowledgeBase;

    private const string SystemPrompt = @"
You are an expert technical hiring manager and career coach.
Analyze the candidate's resume strictly against the target Job Description (JD).
Rules:
1. Grounding: Distinguish explicitly between verified evidence in the resume vs. missing qualifications.
2. Anti-Hallucination: DO NOT fabricate or assume tools/skills not mentioned in the resume.
3. Scoring: Provide an honest 0-100 match score (integer) based on core requirements and years of experience.
4. Missing Skills: Identify critical or nice-to-have skill gaps with importance (High, Medium, or Low) and context.
5. Suggestions: Give actionable, high-impact suggestions that strictly follow the provided Knowledge Base Rubrics (e.g., active engineering verbs, Google XYZ pattern where metrics are real, avoiding empty buzzwords like 'leveraged' or 'successfully', and ending on technical mechanism when numbers are missing).

Expected JSON structure:
{
  ""match_score"": 85,
  ""summary"": ""..."",
  ""matching_skills"": [""C#"", "".NET""],
  ""missing_skills"": [{""skill"": ""Kubernetes"", ""importance"": ""High"", ""context"": ""Required for cloud deployment""}],
  ""relevant_experience"": [{""role_or_project"": ""Backend Lead"", ""relevance_summary"": ""Built REST APIs with ASP.NET"", ""match_level"": ""Strong""}],
  ""improvement_suggestions"": [""Rewrite bullet using Google XYZ formula: Built X, measured by Y, by doing Z.""]
}
";

    public AnalysisService(IGeminiClientService geminiClient, IKnowledgeBaseService knowledgeBase)
    {
        _geminiClient = geminiClient;
        _knowledgeBase = knowledgeBase;
    }

    public async Task<AnalysisResponse> AnalyzeFitAsync(string resumeText, string jobDescription)
    {
        // Retrieve the most relevant writing & evaluation rubrics from knowledge base
        var relevantRubrics = _knowledgeBase.SearchKnowledgeBase($"{jobDescription} {resumeText}", topK: 4);
        var rubricContext = _knowledgeBase.FormatRubricContext(relevantRubrics);

        string userPrompt = $@"
KNOWLEDGE BASE EVALUATION & REWRITING RUBRICS:
{rubricContext}

TARGET JOB DESCRIPTION:
{jobDescription}

CANDIDATE RESUME:
{resumeText}
";
        return await _geminiClient.GenerateStructuredOutputAsync<AnalysisResponse>(SystemPrompt, userPrompt);
    }
}

