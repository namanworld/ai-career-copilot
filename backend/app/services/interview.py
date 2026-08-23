from app.services.llm import llm_service
from app.schemas import InterviewQuestionsResponse

INTERVIEW_SYSTEM_PROMPT = """
You are a Principal Technical Interviewer and Hiring Manager.
Generate 5 targeted, high-signal interview questions tailored to:
1. Gaps or transition areas between the resume and Job Description.
2. Deep-dive technical scenarios on projects explicitly listed in the resume.
3. Behavioral and leadership expectations mentioned in the JD.
For each question, provide the category (Technical, Behavioral, System Design), the rationale, and the suggested focus/talking points for the candidate.
"""

def generate_interview_prep(resume_text: str, job_description: str) -> InterviewQuestionsResponse:
    user_prompt = f"""
TARGET JOB DESCRIPTION:
{job_description}

CANDIDATE RESUME:
{resume_text}
"""
    return llm_service.get_structured_output(INTERVIEW_SYSTEM_PROMPT, user_prompt, InterviewQuestionsResponse)

# Alias for backwards compatibility
generate_interview_questions = generate_interview_prep
