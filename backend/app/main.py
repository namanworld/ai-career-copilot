import uuid
from typing import Optional
from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from app.config import settings
from app.services.parser import parse_pdf, sanitize_text
from app.services.embedding import index_resume
from app.services.analysis import analyze_resume_fit
from app.services.interview import generate_interview_prep
from app.services.rag import answer_resume_question
from app.schemas import AnalysisResponse, InterviewQuestionsResponse, RagQueryRequest, RagQueryResponse

# In-memory session store
session_store = {}

app = FastAPI(title="AI Career Copilot API", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/health")
def health_check():
    return {"status": "ok", "model": settings.GEMINI_MODEL}

@app.get("/")
def root():
    return {"message": "Welcome to the AI Career Copilot API!", "status": "running"}

@app.post("/api/analyze")
async def analyze_application(
    resume: UploadFile = File(...),
    job_description: str = Form(...)
):
    if not resume.filename or not resume.filename.lower().endswith(".pdf"):
        raise HTTPException(status_code=400, detail="Only PDF resumes are supported.")

    file_bytes = await resume.read()
    if len(file_bytes) > settings.MAX_RESUME_SIZE_MB * 1024 * 1024:
        raise HTTPException(status_code=400, detail=f"File exceeds {settings.MAX_RESUME_SIZE_MB}MB limit.")

    resume_text = parse_pdf(file_bytes)
    cleaned_jd = sanitize_text(job_description)

    if len(cleaned_jd.split()) < 5:
        raise HTTPException(status_code=400, detail="Job description is too short. Please provide a complete job description.")

    session_id = str(uuid.uuid4())
    session_store[session_id] = {
        "resume_text": resume_text,
        "job_description": cleaned_jd
    }

    # Index into vector store for RAG
    index_resume(session_id, resume_text)

    # Perform structured fit analysis
    analysis_result = analyze_resume_fit(resume_text, cleaned_jd)

    return {
        "session_id": session_id,
        "analysis": analysis_result
    }

@app.post("/api/interview-questions/{session_id}", response_model=InterviewQuestionsResponse)
def get_interview_questions_by_session(session_id: str):
    session = session_store.get(session_id)
    if not session:
        raise HTTPException(status_code=404, detail="Session not found. Please upload resume first.")
    return generate_interview_prep(session["resume_text"], session["job_description"])

class InterviewDirectRequest(BaseModel):
    resume_text: Optional[str] = ""
    job_description: Optional[str] = ""

@app.post("/api/interview-questions", response_model=InterviewQuestionsResponse)
def get_interview_questions_direct(payload: InterviewDirectRequest):
    return generate_interview_prep(payload.resume_text or "", payload.job_description or "")

@app.post("/api/rag-query", response_model=RagQueryResponse)
def query_copilot(payload: RagQueryRequest):
    session = session_store.get(payload.session_id, {})
    jd = session.get("job_description", "")
    return answer_resume_question(payload.session_id or "default", payload.query, jd)

class QaLegacyRequest(BaseModel):
    session_id: Optional[str] = "default"
    question: str

@app.post("/api/qa", response_model=RagQueryResponse)
def qa_legacy(payload: QaLegacyRequest):
    session = session_store.get(payload.session_id, {})
    jd = session.get("job_description", "")
    return answer_resume_question(payload.session_id or "default", payload.question, jd)
