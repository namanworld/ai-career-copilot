using System.Text.Json.Serialization;

namespace AiCareerCopilot.Api.Models;

public record SkillGapItem(
    [property: JsonPropertyName("skill")] string Skill,
    [property: JsonPropertyName("importance")] string Importance,
    [property: JsonPropertyName("context")] string Context
);

public record ExperienceMatch(
    [property: JsonPropertyName("role_or_project")] string RoleOrProject,
    [property: JsonPropertyName("relevance_summary")] string RelevanceSummary,
    [property: JsonPropertyName("match_level")] string MatchLevel
);

public record AnalysisResponse(
    [property: JsonPropertyName("match_score")] int MatchScore,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("matching_skills")] List<string> MatchingSkills,
    [property: JsonPropertyName("missing_skills")] List<SkillGapItem> MissingSkills,
    [property: JsonPropertyName("relevant_experience")] List<ExperienceMatch> RelevantExperience,
    [property: JsonPropertyName("improvement_suggestions")] List<string> ImprovementSuggestions
);

public record QuestionItem(
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("rationale")] string Rationale,
    [property: JsonPropertyName("suggested_focus")] string SuggestedFocus
);

public record InterviewQuestionsResponse(
    [property: JsonPropertyName("questions")] List<QuestionItem> Questions
);

public record RagQueryRequest(
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("session_id")] string? SessionId = "default"
);

public record RagQueryResponse(
    [property: JsonPropertyName("answer")] string Answer,
    [property: JsonPropertyName("sources")] List<string> Sources,
    [property: JsonPropertyName("is_grounded")] bool IsGrounded
);

public record AnalyzeEndpointResponse(
    [property: JsonPropertyName("session_id")] string SessionId,
    [property: JsonPropertyName("analysis")] AnalysisResponse Analysis
);
