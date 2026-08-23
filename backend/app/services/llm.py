import json
import re
import google.generativeai as genai
from app.config import settings
from typing import Type, TypeVar
from pydantic import BaseModel
from fastapi import HTTPException

T = TypeVar("T", bound=BaseModel)

class LLMService:
    def __init__(self):
        if not settings.GEMINI_API_KEY:
            raise ValueError("GEMINI_API_KEY is not set. Please set it in backend/.env")
        genai.configure(api_key=settings.GEMINI_API_KEY)
        self.model_name = settings.GEMINI_MODEL

    def get_structured_output(self, system_prompt: str, user_prompt: str, schema: Type[T]) -> T:
        """Invokes Gemini with JSON mode and validates the output against a Pydantic model."""
        try:
            json_schema_str = json.dumps(schema.model_json_schema(), indent=2)
            full_system_prompt = (
                f"{system_prompt}\n\n"
                f"You MUST respond ONLY with valid JSON matching this exact JSON schema:\n"
                f"```json\n{json_schema_str}\n```\n"
                f"Do not include any text outside the JSON object."
            )

            model = genai.GenerativeModel(
                model_name=self.model_name,
                system_instruction=full_system_prompt,
                generation_config={
                    "response_mime_type": "application/json",
                    "temperature": 0.2,
                }
            )
            response = model.generate_content(user_prompt)
            raw_text = response.text or ""
            
            # Clean markdown codeblocks if present
            cleaned_text = re.sub(r"^```(?:json)?\s*", "", raw_text.strip(), flags=re.IGNORECASE)
            cleaned_text = re.sub(r"\s*```$", "", cleaned_text.strip())

            return schema.model_validate_json(cleaned_text)
        except Exception as e:
            raise HTTPException(status_code=500, detail=f"LLM Structured Generation Error: {str(e)}")

    def generate_chat_response(self, system_prompt: str, user_prompt: str) -> str:
        """Generates plain text chat responses."""
        try:
            model = genai.GenerativeModel(
                model_name=self.model_name,
                system_instruction=system_prompt,
                generation_config={"temperature": 0.2}
            )
            response = model.generate_content(user_prompt)
            return response.text or ""
        except Exception as e:
            raise HTTPException(status_code=500, detail=f"LLM Generation Error: {str(e)}")

llm_service = LLMService()

