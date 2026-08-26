using AiCareerCopilot.Api.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AiCareerCopilot.Tests;

public class KnowledgeBaseAndVectorTests
{
    [Fact]
    public void DynamicVectorService_BuildsVocabulary_AndComputesSimilarity()
    {
        var vectorService = new DynamicCorpusVectorService();

        var corpus = new[]
        {
            "Built distributed microservices in C# and .NET with Docker and Kubernetes.",
            "Developed React frontend with TailwindCSS and TypeScript.",
            "Implemented REST APIs in Python using FastAPI and PostgreSQL."
        };

        // 1. Learn vocabulary dynamically
        vectorService.BuildVocabulary(corpus);
        Assert.True(vectorService.VocabularySize >= 10);

        // 2. Generate embeddings
        var vecCsharp = vectorService.CreateEmbedding("C# .NET microservices Docker");
        var vecDotnet = vectorService.CreateEmbedding(".NET Docker Kubernetes");
        var vecReact = vectorService.CreateEmbedding("React TypeScript UI");

        // 3. Compare similarities
        float simBackend = vectorService.CalculateSimilarity(vecCsharp, vecDotnet);
        float simCrossDomain = vectorService.CalculateSimilarity(vecCsharp, vecReact);

        Assert.True(simBackend > simCrossDomain, "C# backend should be more similar to .NET than to React frontend");
        Assert.True(simBackend > 0.4f, "Backend terms should have high cosine similarity");
    }

    [Fact]
    public void KnowledgeBaseService_LoadsChunks_AndSearchesRelevantRules()
    {
        var vectorService = new DynamicCorpusVectorService();

        // Point to the backend-dotnet directory containing Data/ResumeKnowledgeBase.json
        var mockEnv = new Mock<IHostEnvironment>();
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "backend-dotnet"));
        mockEnv.Setup(e => e.ContentRootPath).Returns(projectRoot);

        var kbService = new KnowledgeBaseService(
            vectorService,
            mockEnv.Object,
            NullLogger<KnowledgeBaseService>.Instance);

        var allChunks = kbService.GetAllChunks();
        Assert.Equal(50, allChunks.Count);

        // Search for rule on fabrication / inventing metrics

        var results = kbService.SearchKnowledgeBase("inventing metrics and numbers without baseline", topK: 3);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Id == "rule-001" || r.Tags.Contains("fabrication") || r.Tags.Contains("metrics"));

        // Search for Google XYZ pattern
        var xyzResults = kbService.SearchKnowledgeBase("Google XYZ accomplished X measured by Y", topK: 3);
        Assert.NotEmpty(xyzResults);
        Assert.Contains(xyzResults, r => r.Id.StartsWith("xyz-"));
    }
}
