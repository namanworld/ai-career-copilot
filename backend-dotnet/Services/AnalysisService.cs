using AiCareerCopilot.Api.Models;

namespace AiCareerCopilot.Api.Services;

public interface IAnalysisService
{
    Task<AnalysisResponse> AnalyzeFitAsync(string resumeText, string jobDescription);
}

public class AnalysisService : IAnalysisService
{
    private readonly IGeminiClientService _geminiClient;

    private const string SystemPrompt = @"
You are an expert technical hiring manager and career coach.
Analyze the candidate's resume strictly against the target Job Description (JD).
Rules:
1. Grounding: Distinguish explicitly between verified evidence in the resume vs. missing qualifications.
2. Anti-Hallucination: DO NOT fabricate or assume tools/skills not mentioned in the resume.
3. Scoring: Provide an honest 0-100 match score (integer) based on core requirements and years of experience.
4. Missing Skills: Identify critical or nice-to-have skill gaps with importance (High, Medium, or Low) and context.
5. Suggestions: Give actionable, high-impact bullet points to improve the resume for this exact role.

Expected JSON structure:
{
  ""match_score"": 85,
  ""summary"": ""..."",
  ""matching_skills"": [""C#"", "".NET""],
  ""missing_skills"": [{""skill"": ""Kubernetes"", ""importance"": ""High"", ""context"": ""Required for cloud deployment""}],
  ""relevant_experience"": [{""role_or_project"": ""Backend Lead"", ""relevance_summary"": ""Built REST APIs with ASP.NET"", ""match_level"": ""Strong""}],
  ""improvement_suggestions"": [""Highlight container experience""]
}
";

    public AnalysisService(IGeminiClientService geminiClient)
    {
        _geminiClient = geminiClient;
    }

    public async Task<AnalysisResponse> AnalyzeFitAsync(string resumeText, string jobDescription)
    {
        string userPrompt = $@"
TARGET JOB DESCRIPTION:
{jobDescription}

CANDIDATE RESUME:
{resumeText}
";
        return await _geminiClient.GenerateStructuredOutputAsync<AnalysisResponse>(SystemPrompt, userPrompt);
    }
}
