import io
import re
from pypdf import PdfReader
from fastapi import HTTPException

# Prompt injection patterns to neutralize malicious resumes/JDs
INJECTION_PATTERNS = [
    r"ignore\s+(all\s+)?(previous|prior)\s+instructions",
    r"disregard\s+(all\s+)?(previous|prior)\s+instructions",
    r"system\s+prompt\s+override",
    r"you\s+are\s+now\s+in\s+developer\s+mode",
    r"reveal\s+(system\s+)?(prompt|secret|key)",
    r"output\s+raw\s+system\s+prompt"
]

def sanitize_text(text: str) -> str:
    """Sanitizes text by removing non-printable control characters and neutralizing injection attempts."""
    cleaned = re.sub(r"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "", text)
    for pattern in INJECTION_PATTERNS:
        cleaned = re.sub(pattern, "[FLAGGED_INJECTION_REMOVED]", cleaned, flags=re.IGNORECASE)
    return cleaned.strip()

def parse_pdf(file_input: bytes | str) -> str:
    """Extracts and sanitizes text from PDF byte stream or file path."""
    try:
        if isinstance(file_input, bytes):
            reader = PdfReader(io.BytesIO(file_input))
        else:
            reader = PdfReader(file_input)

        extracted_text = []
        for page in reader.pages:
            text = page.extract_text()
            if text:
                extracted_text.append(text)

        full_text = "\n".join(extracted_text)
        if not full_text.strip():
            raise HTTPException(
                status_code=400,
                detail="Could not extract readable text from PDF. Ensure the PDF is not an un-OCR'd scanned image."
            )
        return sanitize_text(full_text)
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=400, detail=f"PDF parsing error: {str(e)}")

# Alias for backwards compatibility
parse_resume = parse_pdf
extract_text_from_pdf = parse_pdf
