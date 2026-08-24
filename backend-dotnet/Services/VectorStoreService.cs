using System.Collections.Concurrent;

namespace AiCareerCopilot.Api.Services;

public interface IVectorStoreService
{
    Task IndexResumeAsync(string sessionId, string resumeText);
    Task<List<string>> QueryResumeAsync(string sessionId, string query, int topK = 3);
}

public class VectorStoreService : IVectorStoreService
{
    private readonly IGeminiClientService _geminiClient;
    private readonly ConcurrentDictionary<string, SessionVectorData> _sessions = new();

    private const int ChunkSize = 500;
    private const int ChunkOverlap = 100;

    public VectorStoreService(IGeminiClientService geminiClient)
    {
        _geminiClient = geminiClient;
    }

    public record SessionVectorData(List<string> Chunks, List<float[]> Embeddings);

    public static List<string> ChunkText(string text, int chunkSize = ChunkSize, int overlap = ChunkOverlap)
    {
        var words = text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return new List<string>();

        var chunks = new List<string>();
        int start = 0;
        while (start < words.Length)
        {
            int end = Math.Min(start + chunkSize, words.Length);
            var chunk = string.Join(" ", words.Skip(start).Take(end - start));
            chunks.Add(chunk);

            if (end >= words.Length) break;
            start += Math.Max(1, chunkSize - overlap);
        }

        return chunks;
    }

    public async Task IndexResumeAsync(string sessionId, string resumeText)
    {
        var chunks = ChunkText(resumeText);
        if (chunks.Count == 0)
        {
            _sessions[sessionId] = new SessionVectorData(new List<string>(), new List<float[]>());
            return;
        }

        var embeddings = new List<float[]>();
        foreach (var chunk in chunks)
        {
            var emb = await _geminiClient.GetEmbeddingAsync(chunk, "RETRIEVAL_DOCUMENT");
            embeddings.Add(Normalize(emb));
        }

        _sessions[sessionId] = new SessionVectorData(chunks, embeddings);
    }

    public async Task<List<string>> QueryResumeAsync(string sessionId, string query, int topK = 3)
    {
        if (!_sessions.TryGetValue(sessionId, out var sessionData) || sessionData.Chunks.Count == 0)
        {
            return new List<string>();
        }

        var queryEmbedding = await _geminiClient.GetEmbeddingAsync(query, "RETRIEVAL_QUERY");
        var normalizedQuery = Normalize(queryEmbedding);

        var scoredChunks = new List<(string Chunk, float Similarity)>();
        for (int i = 0; i < sessionData.Chunks.Count; i++)
        {
            float sim = CosineSimilarity(normalizedQuery, sessionData.Embeddings[i]);
            scoredChunks.Add((sessionData.Chunks[i], sim));
        }

        return scoredChunks
            .OrderByDescending(x => x.Similarity)
            .Take(topK)
            .Select(x => x.Chunk)
            .ToList();
    }

    private static float[] Normalize(float[] vector)
    {
        double sumSquares = 0;
        for (int i = 0; i < vector.Length; i++) sumSquares += vector[i] * vector[i];
        float norm = (float)Math.Sqrt(sumSquares);

        if (norm == 0) return vector;

        var normalized = new float[vector.Length];
        for (int i = 0; i < vector.Length; i++) normalized[i] = vector[i] / norm;
        return normalized;
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dotProduct = 0;
        int len = Math.Min(a.Length, b.Length);
        for (int i = 0; i < len; i++)
        {
            dotProduct += a[i] * b[i];
        }
        return dotProduct;
    }
}
