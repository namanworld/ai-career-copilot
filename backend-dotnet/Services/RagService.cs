using AiCareerCopilot.Api.Models;

namespace AiCareerCopilot.Api.Services;

public interface IRagService
{
    Task<RagQueryResponse> AnswerQuestionAsync(string sessionId, string query, string jobDescription = "");
}

public class RagService : IRagService
{
    private readonly IVectorStoreService _vectorStore;
    private readonly IGeminiClientService _geminiClient;

    private const string SystemPrompt = @"
You are a strict, grounded AI Career Assistant answering questions regarding a candidate's resume and job description.
Instructions:
1. Grounding: Answer strictly and only based on the provided RETRIEVED RESUME CONTEXT.
2. Anti-Hallucination: If the answer is not supported or mentioned in the retrieved context, you MUST state clearly: ""Based on the provided resume, this information is not mentioned.""
3. Never invent candidate qualifications, dates, projects, or certifications.
4. Keep answers concise, factual, and helpful.
";

    public RagService(IVectorStoreService vectorStore, IGeminiClientService geminiClient)
    {
        _vectorStore = vectorStore;
        _geminiClient = geminiClient;
    }

    public async Task<RagQueryResponse> AnswerQuestionAsync(string sessionId, string query, string jobDescription = "")
    {
        var retrievedChunks = await _vectorStore.QueryResumeAsync(sessionId, query, topK: 3);
        var context = retrievedChunks.Count > 0 ? string.Join("\n---\n", retrievedChunks) : "NO_CONTEXT_FOUND";

        string userPrompt = $@"
RETRIEVED CONTEXT FROM CANDIDATE RESUME:
{context}

OPTIONAL TARGET JOB DESCRIPTION:
{jobDescription}

USER QUESTION:
{query}
";
        var rawAnswer = await _geminiClient.GenerateChatResponseAsync(SystemPrompt, userPrompt);
        bool isGrounded = !rawAnswer.Contains("not mentioned", StringComparison.OrdinalIgnoreCase) && context != "NO_CONTEXT_FOUND";

        return new RagQueryResponse(
            Answer: rawAnswer,
            Sources: retrievedChunks,
            IsGrounded: isGrounded
        );
    }
}
