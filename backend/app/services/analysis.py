from app.services.llm import llm_service
from app.schemas import AnalysisResponse

ANALYSIS_SYSTEM_PROMPT = """
You are an expert technical hiring manager and career coach.
Analyze the candidate's resume strictly against the target Job Description (JD).
Rules:
1. Grounding: Distinguish explicitly between verified evidence in the resume vs. missing qualifications.
2. Anti-Hallucination: DO NOT fabricate or assume tools/skills not mentioned in the resume.
3. Scoring: Provide an honest 0-100 match score based on core requirements and years of experience.
4. Missing Skills: Identify critical or nice-to-have skill gaps with importance and context.
5. Suggestions: Give actionable, high-impact bullet points to improve the resume for this exact role.
"""

def analyze_resume_fit(resume_text: str, job_description: str) -> AnalysisResponse:
    user_prompt = f"""
TARGET JOB DESCRIPTION:
{job_description}

CANDIDATE RESUME:
{resume_text}
"""
    return llm_service.get_structured_output(ANALYSIS_SYSTEM_PROMPT, user_prompt, AnalysisResponse)

# Alias for backwards compatibility
analyze_resume = analyze_resume_fit
