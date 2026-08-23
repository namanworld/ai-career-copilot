from app.services.embedding import query_resume
from app.services.llm import llm_service
from app.schemas import RagQueryResponse

RAG_SYSTEM_PROMPT = """
You are a strict, grounded AI Career Assistant answering questions regarding a candidate's resume and job description.
Instructions:
1. Grounding: Answer strictly and only based on the provided RETRIEVED RESUME CONTEXT.
2. Anti-Hallucination: If the answer is not supported or mentioned in the retrieved context, you MUST state clearly: "Based on the provided resume, this information is not mentioned."
3. Never invent candidate qualifications, dates, projects, or certifications.
4. Keep answers concise, factual, and helpful.
"""

def answer_resume_question(session_id: str, query: str, job_description: str = "") -> RagQueryResponse:
    retrieved_chunks = query_resume(session_id, query, top_k=3)
    context = "\n---\n".join(retrieved_chunks) if retrieved_chunks else "NO_CONTEXT_FOUND"

    user_prompt = f"""
RETRIEVED CONTEXT FROM CANDIDATE RESUME:
{context}

OPTIONAL TARGET JOB DESCRIPTION:
{job_description}

USER QUESTION:
{query}
"""
    raw_answer = llm_service.generate_chat_response(RAG_SYSTEM_PROMPT, user_prompt)
    is_grounded = "not mentioned" not in raw_answer.lower() and context != "NO_CONTEXT_FOUND"

    return RagQueryResponse(
        answer=raw_answer,
        sources=retrieved_chunks,
        is_grounded=is_grounded
    )

class RAGService:
    @staticmethod
    def answer(session_id: str, query: str, job_description: str = "") -> RagQueryResponse:
        return answer_resume_question(session_id, query, job_description)

