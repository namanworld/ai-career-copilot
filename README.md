# AI Career Copilot 🚀

> A production-ready, grounded **AI Career Copilot** that performs deterministic resume-to-job-description matching, tailored technical & behavioral interview generation, and strict grounded RAG Q&A.

Built to demonstrate modern AI/LLM engineering fundamentals (structured JSON outputs, FAISS vector search, prompt-injection defense, and evaluation metrics) without framework bloat.

---

## 🌟 Highlights

- **Anti-Hallucination & Grounded RAG:** Uses sliding-window chunking ($500$ words / $100$ word overlap), normalized FAISS vector indexing, and strict context grounding with citation detection.
- **Structured Pydantic Outputs:** Reliable JSON schemas mapped directly to Pydantic models for scores, skill gaps, and interview prep.
- **Prompt Injection Defense:** Input sanitization layer that neutralizes prompt override patterns (e.g. `ignore previous instructions`).
- **Zero API Secrets in Source Code:** Configured strictly via `.env` with a comprehensive `.gitignore`.
- **Built-in Automated Evaluation:** Includes [backend/eval/evaluate.py](backend/eval/evaluate.py) testing retrieval correctness and anti-hallucination accuracy.

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
│                   FastAPI Backend                      │
│ ┌────────────────────────────────────────────────────┐ │
│ │  1. PDF Parser & Injection Defense (pypdf + regex) │ │
│ │  2. Embedding Service (Gemini Embedding + FAISS)   │ │
│ │  3. Analysis Service (Structured Gemini Output)    │ │
│ │  4. Interview Service (Tailored Questions)         │ │
│ │  5. RAG Retrieval Engine (Strict Grounded Q&A)     │ │
│ └────────────────────────────────────────────────────┘ │
└───────────────────────────┬────────────────────────────┘
                            ▼
           Google Gemini 3.6 Flash & Embeddings
```

---

## 📂 Project Structure

```text
ai-career-copilot/
├── backend/
│   ├── app/
│   │   ├── services/
│   │   │   ├── analysis.py      # Match scoring & gap analysis
│   │   │   ├── embedding.py     # Gemini embeddings + FAISS indexing
│   │   │   ├── interview.py     # Tailored interview question generator
│   │   │   ├── llm.py           # Gemini client with structured output
│   │   │   ├── parser.py        # PDF text extraction & injection defense
│   │   │   └── rag.py           # Grounded RAG retrieval & QA
│   │   ├── config.py            # Environment settings
│   │   ├── main.py              # FastAPI application routes
│   │   └── schemas.py           # Pydantic validation schemas
│   ├── eval/
│   │   ├── dataset.json         # Evaluation benchmark dataset
│   │   └── evaluate.py          # Groundedness & hallucination evaluator
│   ├── tests/
│   │   └── test_api.py          # API & sanitization unit tests
│   ├── .env.example             # Environment template (NO SECRETS)
│   ├── pytest.ini               # Test configuration
│   └── requirements.txt         # Backend Python dependencies
├── frontend/
│   ├── src/
│   │   ├── App.css              # Clean, modern UI styling
│   │   ├── App.jsx              # React single-page copilot app
│   │   └── main.jsx             # React entry point
│   ├── index.html
│   ├── package.json
│   └── vite.config.js
├── .gitignore                   # Security rules ignoring .env & node_modules
└── README.md
```

---

## 🚀 Quick Start Guide

### Prerequisites
- **Python 3.10+**
- **Node.js 18+** & npm
- A **Google Gemini API Key** ([Google AI Studio](https://aistudio.google.com/app/apikey))

---

### Step 1: Clone and Configure Environment

```bash
git clone <YOUR_GITHUB_REPO_URL>
cd ai-career-copilot

# Create backend .env from template
cp backend/.env.example backend/.env
```

Open `backend/.env` and paste your Gemini API key:
```ini
GEMINI_API_KEY=your_actual_gemini_api_key_here
GEMINI_MODEL=models/gemini-3.6-flash
EMBEDDING_MODEL=models/gemini-embedding-001
MAX_RESUME_SIZE_MB=5
CHUNK_SIZE=500
CHUNK_OVERLAP=100
```

---

### Step 2: Start the Backend Server

```bash
cd backend
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt

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
