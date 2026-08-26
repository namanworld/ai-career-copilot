using System.Text.RegularExpressions;

namespace AiCareerCopilot.Api.Services;

public interface IDynamicCorpusVectorService
{
    void BuildVocabulary(IEnumerable<string> corpusDocuments);
    float[] CreateEmbedding(string text);
    float CalculateSimilarity(float[] vectorA, float[] vectorB);
    int VocabularySize { get; }
}

/// <summary>
/// A pure C# in-memory vectorization and embedding engine.
/// Learns vocabulary dynamically from documents without hardcoded dictionaries,
/// generates L2-normalized Term-Frequency feature vectors, and computes Cosine Similarity.
/// </summary>
public class DynamicCorpusVectorService : IDynamicCorpusVectorService
{
    private readonly Dictionary<string, int> _vocabulary = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    // Standard high-frequency stopwords that carry minimal semantic signal
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "in", "on", "at", "to", "for", "with", "by", 
        "from", "of", "is", "was", "are", "were", "be", "been", "being", "have", "has", 
        "had", "do", "does", "did", "but", "if", "then", "else", "when", "up", "out", "so"
    };

    public int VocabularySize => _vocabulary.Count;

    /// <summary>
    /// 1. Tokenizes and extracts all unique terms across the document corpus to build vocabulary.
    /// </summary>
    public void BuildVocabulary(IEnumerable<string> corpusDocuments)
    {
        lock (_lock)
        {
            foreach (var doc in corpusDocuments)
            {
                var tokens = Tokenize(doc);
                foreach (var token in tokens)
                {
                    if (!_vocabulary.ContainsKey(token))
                    {
                        _vocabulary[token] = _vocabulary.Count;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 2. Converts any input text into an L2-normalized dense feature vector based on learned vocabulary.
    /// </summary>
    public float[] CreateEmbedding(string text)
    {
        float[] vector;
        int vocabSize;

        lock (_lock)
        {
            vocabSize = _vocabulary.Count;
            if (vocabSize == 0) return Array.Empty<float>();

            vector = new float[vocabSize];
            var tokens = Tokenize(text);

            if (tokens.Count == 0) return vector;

            // Compute term frequencies (TF)
            foreach (var token in tokens)
            {
                if (_vocabulary.TryGetValue(token, out int index))
                {
                    vector[index] += 1.0f;
                }
            }
        }

        // Apply sub-linear frequency scaling: tf_weight = 1 + ln(tf)
        for (int i = 0; i < vector.Length; i++)
        {
            if (vector[i] > 0)
            {
                vector[i] = 1.0f + (float)Math.Log(vector[i]);
            }
        }

        // Normalize to unit sphere: ||vector||_2 = 1.0
        return NormalizeL2(vector);
    }

    /// <summary>
    /// 3. Computes Cosine Similarity between two normalized embedding vectors.
    /// Since both vectors are L2-normalized, CosineSimilarity(A, B) = DotProduct(A, B).
    /// </summary>
    public float CalculateSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length == 0 || vectorB.Length == 0) return 0f;

        float dotProduct = 0f;
        int length = Math.Min(vectorA.Length, vectorB.Length);

        for (int i = 0; i < length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
        }

        return Math.Max(0f, dotProduct);
    }

    /// <summary>
    /// Extracts clean, alphanumeric tokens and tech symbols (e.g. c#, .net, c++).
    /// </summary>
    public static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        var matches = Regex.Matches(text.ToLowerInvariant(), @"\b[a-z0-9+#.-]+\b");
        var tokens = new List<string>(matches.Count);

        foreach (Match match in matches)
        {
            var word = match.Value.Trim('-', '.', ' ');
            if (word.Length > 1 && !StopWords.Contains(word))
            {
                tokens.Add(word);
            }
        }

        return tokens;
    }

    /// <summary>
    /// Divides each element by Euclidean L2-norm: sqrt(sum(x_i^2)).
    /// </summary>
    private static float[] NormalizeL2(float[] vector)
    {
        double sumSquares = 0.0;
        for (int i = 0; i < vector.Length; i++)
        {
            sumSquares += vector[i] * vector[i];
        }

        float norm = (float)Math.Sqrt(sumSquares);
        if (norm > 0)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] /= norm;
            }
        }

        return vector;
    }
}
