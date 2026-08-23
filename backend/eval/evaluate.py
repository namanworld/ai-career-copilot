import json
import os
import sys

# Ensure backend root is on Python path
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from app.services.embedding import index_resume
from app.services.rag import answer_resume_question

def run_evaluation():
    eval_file = os.path.join(os.path.dirname(__file__), "dataset.json")
    with open(eval_file, "r") as f:
        samples = json.load(f)

    passed = 0
    total = len(samples)

    print("=" * 60)
    print("AI CAREER COPILOT: RAG GROUNDEDNESS & HALLUCINATION EVALUATION")
    print("=" * 60)

    for sample in samples:
        session_id = sample["id"]
        # 1. Index test resume into vector store
        index_resume(session_id, sample["resume_text"])

        # 2. Run RAG query
        response = answer_resume_question(session_id, sample["query"])

        # 3. Check for expected grounded keywords
        passed_keywords = any(kw.lower() in response.answer.lower() for kw in sample["expected_keywords"])
        grounded_status = response.is_grounded == sample["should_be_grounded"]
        passed_all = passed_keywords and grounded_status

        status = "PASSED" if passed_all else "FAILED"
        if passed_all:
            passed += 1

        print(f"\n[{status}] Test ID: {sample['id']}")
        print(f"Query: {sample['query']}")
        print(f"Response: {response.answer.strip()}")
        print(f"Grounded: {response.is_grounded} (Expected: {sample['should_be_grounded']})")

    accuracy = (passed / total) * 100
    print("\n" + "=" * 60)
    print(f"SUMMARY: {passed}/{total} tests passed ({accuracy:.1f}% accuracy)")
    print("=" * 60)

if __name__ == "__main__":
    run_evaluation()
