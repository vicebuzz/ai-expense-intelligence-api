"""FastAPI microservice: rule-based categorisation with optional ML extension."""

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field

from rules import categorize

app = FastAPI(
    title="Expense Categorization Service",
    description="Keyword rules engine for transaction categorisation.",
    version="1.0.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)


class CategorizeRequest(BaseModel):
    description: str = Field(..., min_length=1, max_length=500)
    is_expense: bool = True


class CategorizeResponse(BaseModel):
    category: str
    source: str = "rules"
    confidence: float


@app.get("/health")
def health():
    return {"status": "healthy", "service": "categorization"}


@app.post("/categorize", response_model=CategorizeResponse)
def categorize_transaction(body: CategorizeRequest):
    category, confidence = categorize(body.description, body.is_expense)
    return CategorizeResponse(category=category, confidence=confidence)
