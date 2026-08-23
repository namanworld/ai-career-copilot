from pydantic import BaseModel
from typing import List, Optional

class SkillGapItem(BaseModel):
    skill: str
    importance: str  # High, Medium, or Low
    context: str

class ExperienceMatch(BaseModel):
    role_or_project: str
    relevance_summary: str
    match_level: str  # Strong, Moderate, or Weak

class AnalysisResponse(BaseModel):
    match_score: int
    summary: str
    matching_skills: List[str]
    missing_skills: List[SkillGapItem]
    relevant_experience: List[ExperienceMatch]
    improvement_suggestions: List[str]

class QuestionItem(BaseModel):
    category: str  # Technical, Behavioral, or System Design
    question: str
    rationale: str
    suggested_focus: str

class InterviewQuestionsResponse(BaseModel):
    questions: List[QuestionItem]

class RagQueryRequest(BaseModel):
    query: str
    session_id: Optional[str] = "default"

class RagQueryResponse(BaseModel):
    answer: str
    sources: List[str]
    is_grounded: bool

