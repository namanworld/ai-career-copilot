from typing import List, Dict
import numpy as np
import faiss
import google.generativeai as genai
from app.config import settings

if settings.GEMINI_API_KEY:
    genai.configure(api_key=settings.GEMINI_API_KEY)

# In-memory session store for vector indices and chunks: session_id -> {"index": faiss_index, "chunks": [...]}
_vector_sessions: Dict[str, dict] = {}

def get_embedding(text: str) -> List[float]:
    """Generates an embedding vector using Gemini."""
    result = genai.embed_content(
        model=settings.EMBEDDING_MODEL,
        content=text,
        task_type="retrieval_document"
    )
    return result["embedding"]

def get_query_embedding(query: str) -> List[float]:
    """Generates a query embedding vector using Gemini."""
    result = genai.embed_content(
        model=settings.EMBEDDING_MODEL,
        content=query,
        task_type="retrieval_query"
    )
    return result["embedding"]

def chunk_text(text: str, chunk_size: int = 500, overlap: int = 100) -> List[str]:
    """Sliding-window word-level chunking."""
    words = text.split()
    if not words:
        return []
    chunks = []
    start = 0
    while start < len(words):
        end = start + chunk_size
        chunk = " ".join(words[start:end])
        chunks.append(chunk)
        if end >= len(words):
            break
        start += max(1, chunk_size - overlap)
    return chunks

def index_resume(session_id: str, resume_text: str):
    """Chunks the resume, computes embeddings, and builds a FAISS index for the session."""
    chunks = chunk_text(resume_text, settings.CHUNK_SIZE, settings.CHUNK_OVERLAP)
    if not chunks:
        _vector_sessions[session_id] = {"index": None, "chunks": []}
        return

    embeddings = []
    for chunk in chunks:
        emb = get_embedding(chunk)
        embeddings.append(emb)

    emb_matrix = np.array(embeddings, dtype=np.float32)
    # Normalize for cosine similarity
    faiss.normalize_L2(emb_matrix)
    dim = emb_matrix.shape[1]

    index = faiss.IndexFlatIP(dim)
    index.add(emb_matrix)

    _vector_sessions[session_id] = {
        "index": index,
        "chunks": chunks
    }

def query_resume(session_id: str, query: str, top_k: int = 3) -> List[str]:
    """Retrieves top-k matching resume chunks for a given query."""
    session_data = _vector_sessions.get(session_id)
    if not session_data or not session_data.get("index") or not session_data.get("chunks"):
        return []

    query_emb = np.array([get_query_embedding(query)], dtype=np.float32)
    faiss.normalize_L2(query_emb)

    index = session_data["index"]
    chunks = session_data["chunks"]

    k = min(top_k, len(chunks))
    distances, indices = index.search(query_emb, k)

    retrieved = []
    for idx in indices[0]:
        if 0 <= idx < len(chunks):
            retrieved.append(chunks[idx])

    return retrieved

# Alias for backwards compatibility
create_embeddings = index_resume
