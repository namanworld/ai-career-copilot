using AiCareerCopilot.Api.Models;

namespace AiCareerCopilot.Api.Services;

public interface IRagService
{
    Task<RagQueryResponse> AnswerQuestionAsync(string sessionId, string query, string jobDescription = "");
}

public class RagService : IRagService
{
    private readonly IVectorStoreService _vectorStore;
    private readonly IKnowledgeBaseService _knowledgeBase;
    private readonly IGeminiClientService _geminiClient;

    private const string SystemPrompt = @"
You are a strict, grounded AI Career Assistant and Resume Coach answering questions regarding a candidate's resume, job description, and resume writing best practices.
Instructions:
1. Candidate Grounding: For questions about candidate history, answer strictly based on the RETRIEVED CANDIDATE RESUME CONTEXT. If not present, state clearly: ""Based on the provided resume, this information is not mentioned."" Never invent candidate credentials.
2. Knowledge Base Rubrics: For questions about improving bullets, phrasing, metrics, or interview defensibility, apply the RETRIEVED KNOWLEDGE BASE GUIDELINES (e.g. Google XYZ pattern, active verbs, removing buzzwords, metric defensibility).
3. Be clear, technical, concise, and direct.
";

    public RagService(
        IVectorStoreService vectorStore,
        IKnowledgeBaseService knowledgeBase,
        IGeminiClientService geminiClient)
    {
        _vectorStore = vectorStore;
        _knowledgeBase = knowledgeBase;
        _geminiClient = geminiClient;
    }

    public async Task<RagQueryResponse> AnswerQuestionAsync(string sessionId, string query, string jobDescription = "")
    {
        // 1. Retrieve from Candidate Resume Index
        var candidateChunks = await _vectorStore.QueryResumeAsync(sessionId, query, topK: 3);
        var candidateContext = candidateChunks.Count > 0 ? string.Join("\n---\n", candidateChunks) : "NO_CANDIDATE_RESUME_CONTEXT_FOUND";

        // 2. Retrieve from Knowledge Base Index
        var knowledgeChunks = _knowledgeBase.SearchKnowledgeBase(query, topK: 2);
        var knowledgeContext = _knowledgeBase.FormatRubricContext(knowledgeChunks);

        string userPrompt = $@"
RETRIEVED CANDIDATE RESUME CONTEXT:
{candidateContext}

RETRIEVED RESUME KNOWLEDGE BASE RUBRICS:
{(string.IsNullOrWhiteSpace(knowledgeContext) ? "NO_RUBRIC_CONTEXT_NEEDED" : knowledgeContext)}

OPTIONAL TARGET JOB DESCRIPTION:
{jobDescription}

USER QUESTION:
{query}
";
        var rawAnswer = await _geminiClient.GenerateChatResponseAsync(SystemPrompt, userPrompt);
        
        bool isCandidateFactQuery = !query.Contains("how", StringComparison.OrdinalIgnoreCase) && 
                                   !query.Contains("improve", StringComparison.OrdinalIgnoreCase) && 
                                   !query.Contains("rewrite", StringComparison.OrdinalIgnoreCase);

        bool isGrounded = candidateContext != "NO_CANDIDATE_RESUME_CONTEXT_FOUND" && 
                          !rawAnswer.Contains("not mentioned", StringComparison.OrdinalIgnoreCase);

        var allSources = new List<string>(candidateChunks);
        allSources.AddRange(knowledgeChunks.Select(k => $"[Knowledge Base: {k.Title}] {k.Text}"));

        return new RagQueryResponse(
            Answer: rawAnswer,
            Sources: allSources,
            IsGrounded: isGrounded || !isCandidateFactQuery
        );
    }
}

