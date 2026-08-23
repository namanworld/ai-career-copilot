import os
from pathlib import Path
from dotenv import load_dotenv

# Load .env file from backend root if present
env_path = Path(__file__).resolve().parent.parent / ".env"
load_dotenv(dotenv_path=env_path)

class Settings:
    GEMINI_API_KEY: str = os.getenv("GEMINI_API_KEY", "")
    GEMINI_MODEL: str = os.getenv("GEMINI_MODEL", "models/gemini-3.6-flash")
    EMBEDDING_MODEL: str = os.getenv("EMBEDDING_MODEL", "models/gemini-embedding-001")
    CORS_ORIGINS: list[str] = ["*"]
    MAX_RESUME_SIZE_MB: int = int(os.getenv("MAX_RESUME_SIZE_MB", "5"))
    CHUNK_SIZE: int = int(os.getenv("CHUNK_SIZE", "500"))
    CHUNK_OVERLAP: int = int(os.getenv("CHUNK_OVERLAP", "100"))

settings = Settings()
