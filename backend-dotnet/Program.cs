using System.Collections.Concurrent;
using AiCareerCopilot.Api.Models;
using AiCareerCopilot.Api.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configure CORS for frontend access
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IPdfParserService, PdfParserService>();
builder.Services.AddSingleton<IGeminiClientService, GeminiClientService>();
builder.Services.AddSingleton<IVectorStoreService, VectorStoreService>();
builder.Services.AddSingleton<IAnalysisService, AnalysisService>();
builder.Services.AddSingleton<IInterviewService, InterviewService>();
builder.Services.AddSingleton<IRagService, RagService>();

// In-memory active session store
var sessionStore = new ConcurrentDictionary<string, (string ResumeText, string JobDescription)>();

var app = builder.Build();

app.UseCors();

// Health Check
app.MapGet("/health", (IConfiguration config) =>
{
    var model = config["GEMINI_MODEL"] ?? Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "models/gemini-3.6-flash";
    return Results.Ok(new { status = "ok", model = model, runtime = ".NET / ASP.NET Core" });
});

app.MapGet("/", () => Results.Ok(new { message = "Welcome to the AI Career Copilot .NET API!", status = "running" }));

// 1. Analyze Endpoint (Multipart PDF + Form JD)
app.MapPost("/api/analyze", async (
    HttpRequest request,
    IPdfParserService parser,
    IVectorStoreService vectorStore,
    IAnalysisService analysisService) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { detail = "Request must be multipart/form-data." });
    }

    var form = await request.ReadFormAsync();
    var resumeFile = form.Files.GetFile("resume");
    var jobDescription = form["job_description"].ToString();

    if (resumeFile == null || !resumeFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { detail = "Only PDF resumes are supported." });
    }

    if (resumeFile.Length > 5 * 1024 * 1024)
    {
        return Results.BadRequest(new { detail = "File exceeds 5MB limit." });
    }

    string resumeText;
    using (var stream = resumeFile.OpenReadStream())
    {
        resumeText = parser.ExtractText(stream);
    }

    var cleanedJd = parser.SanitizeText(jobDescription);
    if (cleanedJd.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 5)
    {
        return Results.BadRequest(new { detail = "Job description is too short. Please provide a complete job description." });
    }

    var sessionId = Guid.NewGuid().ToString();
    sessionStore[sessionId] = (resumeText, cleanedJd);

    // Index into vector store for RAG
    await vectorStore.IndexResumeAsync(sessionId, resumeText);

    // Perform structured analysis
    var analysisResult = await analysisService.AnalyzeFitAsync(resumeText, cleanedJd);

    return Results.Ok(new AnalyzeEndpointResponse(sessionId, analysisResult));
});

// 2. Interview Questions (by session_id)
app.MapPost("/api/interview-questions/{sessionId}", async (
    string sessionId,
    IInterviewService interviewService) =>
{
    if (!sessionStore.TryGetValue(sessionId, out var session))
    {
        return Results.NotFound(new { detail = "Session not found. Please upload resume first." });
    }

    var result = await interviewService.GenerateInterviewQuestionsAsync(session.ResumeText, session.JobDescription);
    return Results.Ok(result);
});

// 3. Grounded RAG Query
app.MapPost("/api/rag-query", async (
    [FromBody] RagQueryRequest payload,
    IRagService ragService) =>
{
    string sessionId = payload.SessionId ?? "default";
    string jobDescription = string.Empty;

    if (sessionStore.TryGetValue(sessionId, out var session))
    {
        jobDescription = session.JobDescription;
    }

    var result = await ragService.AnswerQuestionAsync(sessionId, payload.Query, jobDescription);
    return Results.Ok(result);
});

app.Run();

