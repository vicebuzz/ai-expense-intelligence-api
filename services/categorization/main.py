"""FastAPI categorization: transformer ML with rules fallback."""

from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field

from ml_model import classifier, _normalize_allowed


@asynccontextmanager
async def lifespan(_app: FastAPI):
    classifier.load()
    yield


app = FastAPI(
    title="Expense Categorization Service",
    description="Sentence-transformer (MiniLM) + logistic regression for transaction categories.",
    version="2.1.0",
    lifespan=lifespan,
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
    allowed_expense_categories: list[str] | None = None
    allowed_income_categories: list[str] | None = None


class CategorizeResponse(BaseModel):
    category: str
    source: str = "rules"
    confidence: float


class BatchItem(BaseModel):
    description: str = Field(..., min_length=1, max_length=500)
    is_expense: bool = True


class BatchRequest(BaseModel):
    items: list[BatchItem]
    allowed_expense_categories: list[str] | None = None
    allowed_income_categories: list[str] | None = None


class BatchResponse(BaseModel):
    results: list[CategorizeResponse]


class TrainSample(BaseModel):
    description: str
    category: str


class TrainRequest(BaseModel):
    samples: list[TrainSample]
    allowed_categories: list[str] | None = None


def _allowed_for_item(
    is_expense: bool,
    expense: list[str] | None,
    income: list[str] | None,
):
    return _normalize_allowed(expense if is_expense else income)


@app.get("/health")
def health():
    return {
        "status": "healthy",
        "service": "categorization",
        "ml_ready": classifier.is_ready,
        "model": "sentence-transformers/all-MiniLM-L6-v2",
    }


@app.post("/categorize", response_model=CategorizeResponse)
def categorize_transaction(body: CategorizeRequest):
    allowed = _allowed_for_item(
        body.is_expense,
        body.allowed_expense_categories,
        body.allowed_income_categories,
    )
    category, confidence, source = classifier.predict(
        body.description, body.is_expense, allowed
    )
    return CategorizeResponse(category=category, source=source, confidence=confidence)


@app.post("/categorize/batch", response_model=BatchResponse)
def categorize_batch(body: BatchRequest):
    allowed_expense = _normalize_allowed(body.allowed_expense_categories)
    allowed_income = _normalize_allowed(body.allowed_income_categories)
    items = [(i.description, i.is_expense) for i in body.items]
    results = [
        CategorizeResponse(category=c, source=s, confidence=conf)
        for c, conf, s in classifier.predict_batch(
            items, allowed_expense, allowed_income
        )
    ]
    return BatchResponse(results=results)


@app.post("/train")
def train_model(body: TrainRequest):
    descriptions = [s.description for s in body.samples]
    categories = [s.category for s in body.samples]
    count = classifier.train(descriptions, categories, body.allowed_categories)
    return {"trained": count, "ml_ready": classifier.is_ready}
