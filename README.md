# AI Career Copilot 🚀 (.NET & C# Edition)

> A production-ready, grounded **AI Career Copilot** built 100% in **C# / ASP.NET Core** and **React**, performing deterministic resume-to-job-description matching, tailored technical & behavioral interview generation, and strict grounded RAG Q&A.

Built to demonstrate modern AI/LLM engineering in the .NET ecosystem: structured JSON schemas, in-memory Cosine Similarity vector search, prompt-injection defense, and xUnit test suites.

---

## 🌟 Highlights

- **Dual-Index Grounded RAG & Custom Vectorizer:** Combines a session-scoped candidate resume index with a 50-rule hiring rubric knowledge base (`ResumeKnowledgeBase.json`), powered by our custom in-memory `DynamicCorpusVectorService` and Cosine Similarity search.
- **100% C# / ASP.NET Core Minimal APIs:** Built with native dependency injection, concurrent session management, and `System.Text.Json` deserialization.
- **Structured Output Contracts:** Strongly-typed C# `record` models with `System.Text.Json` serialization guaranteeing deterministic API responses for the frontend.
- **Prompt Injection Defense:** Regex-based sanitization layer in `PdfParserService.cs` neutralizing adversarial prompt override patterns (e.g., `ignore previous instructions`).
- **Zero API Secrets in Source Code:** Configured strictly via `appsettings.json` / environment variables with a comprehensive `.gitignore`.
- **Automated xUnit Testing:** Unit tests verifying vectorization, vocabulary learning, knowledge base retrieval, and prompt sanitization.

---

## 🏗 Architecture

```
┌────────────────────────────────────────────────────────┐
│               React + Vite Frontend                    │
│      (PDF Upload • Progress Tracking • RAG Chat)       │
└───────────────────────────┬────────────────────────────┘
                            │ REST API (JSON / Multipart)
                            ▼
┌────────────────────────────────────────────────────────┐
│             ASP.NET Core Minimal API Backend           │
│ ┌────────────────────────────────────────────────────┐ │
│ │  1. PdfParserService (PdfPig + Injection Defense)  │ │
│ │  2. DynamicCorpusVectorService (In-memory TF + L2) │ │
│ │  3. KnowledgeBaseService (50 Curated Hiring Rules) │ │
│ │  4. VectorStoreService (Gemini Embeddings + Cosine)│ │
│ │  5. AnalysisService (Structured Gemini Output)     │ │
│ │  6. RagService (Dual-Index Candidate + KB RAG)     │ │
│ └────────────────────────────────────────────────────┘ │
└───────────────────────────┬────────────────────────────┘
                            ▼
           Google Gemini 3.6 Flash & Embeddings
```

---

## 📂 Project Structure

```text
ai-career-copilot/
├── backend-dotnet/
│   ├── Data/
│   │   └── ResumeKnowledgeBase.json # 50 curated rules (Google XYZ, banned words, metrics)
│   ├── Models/
│   │   └── Schemas.cs             # C# record models for API & LLM contracts
│   ├── Services/
│   │   ├── DynamicCorpusVectorService.cs # Pure C# dynamic vocabulary & vectorizer
│   │   ├── KnowledgeBaseService.cs# In-memory knowledge base RAG indexer
│   │   ├── AnalysisService.cs     # Match scoring & rubric-calibrated gap analysis
│   │   ├── GeminiClientService.cs # HTTP Gemini integration & embeddings
│   │   ├── InterviewService.cs    # Tailored interview question generator
│   │   ├── PdfParserService.cs    # PdfPig extraction & prompt injection defense
│   │   ├── RagService.cs          # Dual-Index grounded RAG answer generator
│   │   └── VectorStoreService.cs  # Sliding-window chunker & Cosine Similarity
│   ├── appsettings.example.json   # Template settings (NO SECRETS)
│   ├── appsettings.json          # Local config (ignored in git)
│   ├── Program.cs                 # ASP.NET Core endpoints & DI registration
│   └── AiCareerCopilot.Api.csproj # .NET 7/8/10 project definition
├── backend-dotnet.Tests/
│   ├── KnowledgeBaseAndVectorTests.cs # xUnit tests for vectorizer & knowledge retrieval
│   ├── SecurityAndChunkingTests.cs# xUnit tests for sanitization & chunking
│   └── backend-dotnet.Tests.csproj
├── frontend/
│   ├── src/
│   │   ├── App.css                # Clean, modern UI styling
│   │   ├── App.jsx                # React single-page copilot app
│   │   └── main.jsx               # React entry point
│   ├── index.html
│   ├── package.json
│   └── vite.config.js
├── .gitignore                     # Security rules ignoring appsettings.json, bin/, obj/
└── README.md
```


---

## 🚀 Quick Start Guide

### Prerequisites
- **.NET 7 / 8 / 10 SDK** (`dotnet --version`)
- **Node.js 18+** & npm
- A **Google Gemini API Key** ([Google AI Studio](https://aistudio.google.com/app/apikey))

---

### Step 1: Configure Backend Environment

```bash
cd backend-dotnet

# Create appsettings.json from template
cp appsettings.example.json appsettings.json
```

Add your Gemini API key in `backend-dotnet/appsettings.json`:
```json
{
  "GEMINI_API_KEY": "YOUR_ACTUAL_GEMINI_API_KEY_HERE",
  "GEMINI_MODEL": "models/gemini-3.6-flash",
  "EMBEDDING_MODEL": "models/gemini-embedding-001"
}
```

---

### Step 2: Start the .NET Backend API

```bash
cd backend-dotnet
dotnet run --urls "http://127.0.0.1:8000"
```

- **Health Check:** [http://127.0.0.1:8000/health](http://127.0.0.1:8000/health)

---

### Step 3: Start the Frontend UI

Open a second terminal window:

```bash
cd frontend
npm install
npm run dev
```

- **Web Application:** [http://localhost:5173](http://localhost:5173)

---

## 🧪 Running xUnit Tests

```bash
cd backend-dotnet.Tests
dotnet test
```

---

## 🔒 Security & Privacy

- **No Secrets in Version Control:** `appsettings.json`, `.env`, and all credential files are explicitly ignored in `.gitignore`.
- **Input Sanitization:** Regex filters in `PdfParserService` neutralize prompt injection vectors before passing data to LLM context.
- **In-Memory Storage:** Candidate resumes are parsed and indexed per session in memory without persistent disk exposure.

---

## 💡 C# & AI Engineering Interview Talking Points

1. **Why C# / ASP.NET Core for GenAI backends?**
   - High throughput, native asynchronous programming (`Task`), compile-time type safety with `record` types, and modern vector calculation support.
2. **How does Cosine Similarity vector search work in C#?**
   - L2-normalized vectors mean the cosine similarity is the dot product of two embedding vectors:
     $$\text{Cosine Similarity}(A, B) = \sum_{i=1}^n A_i \cdot B_i$$
3. **Structured Outputs in C#:**
   - Prompt instructions specify exact JSON schemas matching C# `record` definitions, and `System.Text.Json.JsonSerializer.Deserialize<T>()` provides type-safe deserialization.


# Run FastAPI backend
python -m uvicorn app.main:app --reload --port 8000
```

- **Health Check:** [http://localhost:8000/health](http://localhost:8000/health)
- **Interactive Swagger Docs:** [http://localhost:8000/docs](http://localhost:8000/docs)

---

### Step 3: Start the Frontend UI

Open a second terminal window:

```bash
cd frontend
npm install
npm run dev
```

- **Web Application:** [http://localhost:5173](http://localhost:5173)

---

## 🧪 Testing & Evaluation

### 1. Run Unit Tests
```bash
cd backend
source venv/bin/activate
pytest tests/
```

### 2. Run Groundedness & Anti-Hallucination Evaluation
```bash
cd backend
source venv/bin/activate
python eval/evaluate.py
```

---

## 🔒 Security & Privacy

- **No Secrets in Version Control:** `.env` and all credential files are explicitly ignored in `.gitignore`.
- **Input Sanitization:** Built-in regex filters neutralize common prompt injection vectors before passing data to LLM context.
- **In-Memory Storage:** Candidate resumes are parsed and indexed per session in volatile memory without persistent disk exposure.

---

## 💡 AI Engineering Concepts for Interviews

1. **Grounded RAG Pipeline:** Why not pass the entire PDF directly?
   - Context window cost optimization, reduced distraction for the model, and deterministic retrieval through similarity scoring.
2. **Structured Outputs:**
   - Enforcing strict schemas at generation time eliminates JSON parsing failures in backend microservices.
3. **Anti-Hallucination Guardrails:**
   - Explicit negative constraints in system prompts combined with fallback response verification ensures the copilot never invents credentials or certifications.


---

## ⚡ Key AI Concepts & Interview Prep

### 1. Retrieval-Augmented Generation (RAG)
- **What it does:** Breaks candidate resume into sliding-window text chunks ($500$ words, $100$ word overlap), generates vector embeddings, stores them in an in-memory **FAISS** index, and performs similarity search ($\text{Cosine Similarity}$) to retrieve the top $k=3$ relevant chunks.
- **Why we need it:** Feeds only verified candidate resume context into the prompt, preventing hallucinations when answering specific career questions.
- **Key Tradeoff:** Chunk size vs. context window cost and retrieval precision.
- **Interview Question:** *How do you prevent the LLM from inventing candidate certifications not found in the resume?*
  - **Answer:** System prompt instructions require explicit citations, and fallback phrasing ("Based on the provided resume, this information is not mentioned") is enforced when semantic distance exceeds threshold.

### 2. Structured Outputs (Pydantic Schema Enforcement)
- **What it does:** Uses Gemini's native JSON schema validation mode (`response_schema=AnalysisResponse`) to enforce strict JSON structure for match score (0-100), missing skills list with importance, relevant experience items, and improvement suggestions.
- **Why we need it:** Avoids brittle regex parsing and guarantees JSON structure in production APIs.

### 3. Prompt-Injection Defense
- **What it does:** Strips non-printable ASCII characters and neutralizes injection vectors like `"ignore previous instructions"` or `"reveal system prompt"` from uploaded resumes and pasted job descriptions.

### 4. Deterministic Evaluation
- **What it does:** Runs automated groundedness and anti-hallucination checks against [backend/eval/dataset.json](backend/eval/dataset.json) to calculate accuracy and verify grounded output behavior.

---

## 🛠 Quick Start Guide

### 1. Backend Setup & Run
```bash
cd backend
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
cp .env.example .env
# Set your GEMINI_API_KEY in .env

# Run server
uvicorn app.main:app --reload --port 8000
```
API Documentation will be available at [http://localhost:8000/docs](http://localhost:8000/docs).

### 2. Frontend Setup & Run
```bash
cd frontend
npm install
npm run dev
```
Open [http://localhost:5173](http://localhost:5173) in your browser.

---

## 🧪 Running Tests & Evaluations

```bash
# In backend directory with virtual environment activated:

# 1. Run Unit Tests (FastAPI endpoints + Sanitization)
pytest tests/

# 2. Run RAG Evaluation Benchmark (Groundedness + Anti-Hallucination)
python eval/evaluate.py
```
