import React, { useState } from "react";
import "./App.css";

const API_BASE = "http://localhost:8000";

export default function App() {
  const [file, setFile] = useState(null);
  const [jd, setJd] = useState("");
  const [status, setStatus] = useState("idle"); // idle, analyzing, ready, error
  const [errorMessage, setErrorMessage] = useState("");

  const [sessionId, setSessionId] = useState(null);
  const [analysis, setAnalysis] = useState(null);
  const [interviewQuestions, setInterviewQuestions] = useState([]);
  const [loadingQuestions, setLoadingQuestions] = useState(false);

  // RAG Chat
  const [ragQuery, setRagQuery] = useState("");
  const [ragAnswer, setRagAnswer] = useState(null);
  const [askingRag, setAskingRag] = useState(false);

  const handleAnalyze = async (e) => {
    e.preventDefault();
    if (!file || !jd.trim()) {
      alert("Please upload a PDF resume and paste a Job Description.");
      return;
    }

    try {
      setStatus("analyzing");
      setErrorMessage("");
      setAnalysis(null);
      setInterviewQuestions([]);
      setRagAnswer(null);

      const formData = new FormData();
      formData.append("resume", file);
      formData.append("job_description", jd);

      const res = await fetch(`${API_BASE}/api/analyze`, {
        method: "POST",
        body: formData,
      });

      if (!res.ok) {
        const err = await res.json().catch(() => ({ detail: "Analysis request failed." }));
        throw new Error(err.detail || "Analysis failed.");
      }

      const data = await res.json();
      setSessionId(data.session_id);
      setAnalysis(data.analysis);
      setStatus("ready");
    } catch (err) {
      setStatus("error");
      setErrorMessage(err.message);
    }
  };

  const handleFetchQuestions = async () => {
    if (!sessionId) return;
    try {
      setLoadingQuestions(true);
      const res = await fetch(`${API_BASE}/api/interview-questions/${sessionId}`, {
        method: "POST",
      });
      if (!res.ok) {
        const err = await res.json();
        throw new Error(err.detail || "Failed to generate interview questions.");
      }
      const data = await res.json();
      setInterviewQuestions(data.questions || []);
    } catch (err) {
      alert("Error: " + err.message);
    } finally {
      setLoadingQuestions(false);
    }
  };

  const handleAskRag = async (e) => {
    e.preventDefault();
    if (!ragQuery.trim() || !sessionId) return;
    try {
      setAskingRag(true);
      const res = await fetch(`${API_BASE}/api/rag-query`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ query: ragQuery, session_id: sessionId }),
      });
      if (!res.ok) {
        const err = await res.json();
        throw new Error(err.detail || "RAG query failed.");
      }
      const data = await res.json();
      setRagAnswer(data);
    } catch (err) {
      alert("Error querying copilot: " + err.message);
    } finally {
      setAskingRag(false);
    }
  };

  return (
    <div className="container">
      <header>
        <div className="logo-badge">PROD READY</div>
        <h1>AI Career Copilot</h1>
        <p>Grounded RAG Resume Analysis • Gemini 3.6 Flash • FAISS Vector Search</p>
      </header>

      {/* Upload & JD Form */}
      <div className="card">
        <form onSubmit={handleAnalyze}>
          <div className="form-group">
            <label>1. Upload Resume (PDF)</label>
            <input
              type="file"
              accept=".pdf"
              onChange={(e) => setFile(e.target.files[0])}
            />
          </div>

          <div className="form-group">
            <label>2. Paste Job Description</label>
            <textarea
              placeholder="Paste the target job description or requirements here..."
              value={jd}
              onChange={(e) => setJd(e.target.value)}
            />
          </div>

          <button type="submit" disabled={status === "analyzing"}>
            {status === "analyzing" ? "Analyzing Fit & Indexing RAG..." : "Analyze Match"}
          </button>
        </form>
      </div>

      {/* Pipeline Status Progress */}
      {status !== "idle" && (
        <div className="progress-card">
          <div className="progress-header">
            <strong>Pipeline Status:</strong>
            <span>{status === "analyzing" ? "Processing with Gemini..." : status === "ready" ? "Complete" : "Error"}</span>
          </div>
          <div className="progress-steps">
            <span className={`step-badge ${status === "analyzing" ? "active" : "done"}`}>1. Parse PDF & Sanitize</span>
            <span className={`step-badge ${status === "analyzing" ? "active" : "done"}`}>2. Embed & FAISS Index</span>
            <span className={`step-badge ${status === "ready" ? "done" : ""}`}>3. Structured Fit Evaluation</span>
          </div>
          {errorMessage && <p className="error-text">{errorMessage}</p>}
        </div>
      )}

      {/* Analysis Results */}
      {analysis && (
        <>
          <div className="card">
            <h2>Match Analysis</h2>
            <div className="score-container">
              <div className="score-badge">{analysis.match_score}%</div>
              <div>
                <strong>Executive Summary:</strong>
                <p>{analysis.summary}</p>
              </div>
            </div>

            <div className="section-block">
              <h3>Matching Skills</h3>
              <div className="badge-list">
                {analysis.matching_skills.map((skill, i) => (
                  <span key={i} className="badge match">{skill}</span>
                ))}
              </div>
            </div>

            <div className="section-block">
              <h3>Missing Skills & Gaps</h3>
              <div className="badge-list">
                {analysis.missing_skills.map((gap, i) => (
                  <span key={i} className="badge missing" title={gap.context}>
                    {gap.skill} ({gap.importance})
                  </span>
                ))}
              </div>
            </div>

            {analysis.relevant_experience && analysis.relevant_experience.length > 0 && (
              <div className="section-block">
                <h3>Relevant Experience Highlights</h3>
                <ul>
                  {analysis.relevant_experience.map((exp, i) => (
                    <li key={i}>
                      <strong>{exp.role_or_project}</strong> ({exp.match_level}): {exp.relevance_summary}
                    </li>
                  ))}
                </ul>
              </div>
            )}

            <div className="section-block">
              <h3>Resume Improvement Suggestions</h3>
              <ul className="suggestions-list">
                {analysis.improvement_suggestions.map((sug, i) => (
                  <li key={i}>{sug}</li>
                ))}
              </ul>
            </div>
          </div>

          {/* Interview Questions Generator */}
          <div className="card">
            <h2>Targeted Interview Preparation</h2>
            {interviewQuestions.length === 0 ? (
              <button onClick={handleFetchQuestions} disabled={loadingQuestions}>
                {loadingQuestions ? "Generating Tailored Questions..." : "Generate Interview Questions"}
              </button>
            ) : (
              <div>
                {interviewQuestions.map((q, idx) => (
                  <div key={idx} className="question-box">
                    <div className="question-header">
                      <span className="q-tag">{q.category}</span>
                      <strong>Q{idx + 1}: {q.question}</strong>
                    </div>
                    <p className="q-rationale"><em>Why it matters:</em> {q.rationale}</p>
                    <p className="q-focus"><strong>Suggested Focus:</strong> {q.suggested_focus}</p>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Grounded RAG Chat */}
          <div className="card">
            <h2>Ask Resume & JD (Grounded RAG)</h2>
            <form onSubmit={handleAskRag} className="rag-form">
              <input
                type="text"
                placeholder="e.g. What databases or cloud tools has the candidate used in production?"
                value={ragQuery}
                onChange={(e) => setRagQuery(e.target.value)}
              />
              <button type="submit" disabled={askingRag}>
                {askingRag ? "Searching..." : "Ask Copilot"}
              </button>
            </form>

            {ragAnswer && (
              <div className="chat-box">
                <p><strong>Answer:</strong> {ragAnswer.answer}</p>
                <div className="rag-grounding-tag">
                  <span className={ragAnswer.is_grounded ? "grounded-yes" : "grounded-no"}>
                    {ragAnswer.is_grounded ? "✓ Verified & Grounded in Resume" : "⚠ Not Mentioned in Candidate Background"}
                  </span>
                </div>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
}
