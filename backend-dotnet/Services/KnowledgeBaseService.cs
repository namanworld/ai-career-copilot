using System.Text.Json;
using AiCareerCopilot.Api.Models;

namespace AiCareerCopilot.Api.Services;

public interface IKnowledgeBaseService
{
    List<KnowledgeChunk> GetAllChunks();
    List<KnowledgeChunk> SearchKnowledgeBase(string query, int topK = 3);
    string FormatRubricContext(List<KnowledgeChunk> relevantChunks);
}

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly IDynamicCorpusVectorService _vectorizer;
    private readonly List<KnowledgeChunk> _chunks = new();
    private readonly List<float[]> _chunkEmbeddings = new();
    private readonly ILogger<KnowledgeBaseService> _logger;

    public KnowledgeBaseService(
        IDynamicCorpusVectorService vectorizer,
        IHostEnvironment environment,
        ILogger<KnowledgeBaseService> logger)
    {
        _vectorizer = vectorizer;
        _logger = logger;

        LoadAndIndexKnowledgeBase(environment.ContentRootPath);
    }

    private void LoadAndIndexKnowledgeBase(string rootPath)
    {
        try
        {
            var filePath = Path.Combine(rootPath, "Data", "ResumeKnowledgeBase.json");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("ResumeKnowledgeBase.json not found at: {Path}", filePath);
                return;
            }

            var jsonString = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var doc = JsonSerializer.Deserialize<KnowledgeBaseDocument>(jsonString, options);

            if (doc?.Chunks == null || doc.Chunks.Count == 0)
            {
                _logger.LogWarning("ResumeKnowledgeBase.json contained no chunks.");
                return;
            }

            _chunks.AddRange(doc.Chunks);

            // 1. Build dynamic vocabulary from all knowledge chunks (title + text + tags)
            var corpus = _chunks.Select(c => $"{c.Title} {c.Text} {string.Join(" ", c.Tags)}").ToList();
            _vectorizer.BuildVocabulary(corpus);

            // 2. Compute embeddings for all chunks in-memory
            foreach (var chunk in _chunks)
            {
                var chunkText = $"{chunk.Title}: {chunk.Text}";
                var embedding = _vectorizer.CreateEmbedding(chunkText);
                _chunkEmbeddings.Add(embedding);
            }

            _logger.LogInformation(
                "Successfully loaded and vectorized {Count} knowledge base chunks (Learned Vocab Size: {VocabSize})",
                _chunks.Count, _vectorizer.VocabularySize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load and index ResumeKnowledgeBase.json");
        }
    }

    public List<KnowledgeChunk> GetAllChunks() => _chunks;

    public List<KnowledgeChunk> SearchKnowledgeBase(string query, int topK = 3)
    {
        if (_chunks.Count == 0 || string.IsNullOrWhiteSpace(query))
        {
            return new List<KnowledgeChunk>();
        }

        var queryEmbedding = _vectorizer.CreateEmbedding(query);
        var scored = new List<(KnowledgeChunk Chunk, float Score)>();

        for (int i = 0; i < _chunks.Count; i++)
        {
            float sim = _vectorizer.CalculateSimilarity(queryEmbedding, _chunkEmbeddings[i]);
            scored.Add((_chunks[i], sim));
        }

        return scored
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Chunk)
            .ToList();
    }

    public string FormatRubricContext(List<KnowledgeChunk> relevantChunks)
    {
        if (relevantChunks.Count == 0) return string.Empty;

        var items = relevantChunks.Select(c => 
            $"[Rule: {c.Title} (Category: {c.Category}, Severity: {c.Severity})]\n{c.Text}");

        return string.Join("\n\n", items);
    }
}
