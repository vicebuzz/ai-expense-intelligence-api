"""Transformer embeddings (MiniLM) + logistic regression for expense categories."""

from __future__ import annotations

import json
import logging
from pathlib import Path

import joblib
import numpy as np
from sentence_transformers import SentenceTransformer
from sklearn.linear_model import LogisticRegression
from sklearn.preprocessing import LabelEncoder

from rules import categorize as rules_categorize

logger = logging.getLogger(__name__)

EXCLUDED_CATEGORY = "Other"

DATA_DIR = Path(__file__).parent / "data"
MODEL_DIR = Path(__file__).parent / "models"
LABELED_SAMPLES = DATA_DIR / "labeled_samples.json"
CLASSIFIER_PATH = MODEL_DIR / "classifier.joblib"
ENCODER_PATH = MODEL_DIR / "label_encoder.joblib"

EMBEDDING_MODEL = "sentence-transformers/all-MiniLM-L6-v2"


def _is_included(category: str) -> bool:
    return category.strip().lower() != EXCLUDED_CATEGORY.lower()


def _normalize_allowed(allowed: list[str] | None) -> set[str] | None:
    if not allowed:
        return None
    return {c.strip() for c in allowed if c.strip() and _is_included(c)}


class TransformerExpenseClassifier:
    def __init__(self) -> None:
        self._embedder: SentenceTransformer | None = None
        self._classifier: LogisticRegression | None = None
        self._label_encoder: LabelEncoder | None = None
        self._ready = False

    @property
    def is_ready(self) -> bool:
        return self._ready and self._classifier is not None

    def _get_embedder(self) -> SentenceTransformer:
        if self._embedder is None:
            logger.info("Loading embedding model %s", EMBEDDING_MODEL)
            self._embedder = SentenceTransformer(EMBEDDING_MODEL)
        return self._embedder

    def load(self) -> None:
        MODEL_DIR.mkdir(parents=True, exist_ok=True)
        if CLASSIFIER_PATH.exists() and ENCODER_PATH.exists():
            self._classifier = joblib.load(CLASSIFIER_PATH)
            self._label_encoder = joblib.load(ENCODER_PATH)
            self._ready = True
            logger.info("Loaded ML classifier from disk")
        elif LABELED_SAMPLES.exists():
            self.train_from_file(LABELED_SAMPLES)

    def train(
        self,
        descriptions: list[str],
        categories: list[str],
        allowed_categories: list[str] | None = None,
    ) -> int:
        allowed = _normalize_allowed(allowed_categories)
        pairs = [
            (d, c)
            for d, c in zip(descriptions, categories)
            if _is_included(c) and (allowed is None or c.strip() in allowed)
        ]
        if len(pairs) < 5:
            logger.warning("Need at least 5 non-Other samples to train; got %d", len(pairs))
            return 0

        descriptions, categories = zip(*pairs)
        descriptions = list(descriptions)
        categories = list(categories)

        embedder = self._get_embedder()
        X = embedder.encode(descriptions, show_progress_bar=False, normalize_embeddings=True)

        self._label_encoder = LabelEncoder()
        y = self._label_encoder.fit_transform(categories)

        self._classifier = LogisticRegression(
            max_iter=2000,
            class_weight="balanced",
        )
        self._classifier.fit(X, y)

        MODEL_DIR.mkdir(parents=True, exist_ok=True)
        joblib.dump(self._classifier, CLASSIFIER_PATH)
        joblib.dump(self._label_encoder, ENCODER_PATH)
        self._ready = True
        logger.info(
            "Trained on %d samples, %d categories",
            len(descriptions),
            len(self._label_encoder.classes_),
        )
        return len(descriptions)

    def train_from_file(self, path: Path) -> int:
        with open(path, encoding="utf-8") as f:
            data = json.load(f)
        samples = data.get("samples", data)
        descriptions = [s["description"] for s in samples]
        categories = [s["category"] for s in samples]
        return self.train(descriptions, categories)

    def _best_ml_category(
        self,
        description: str,
        allowed: set[str] | None,
    ) -> tuple[str, float] | None:
        assert self._classifier is not None
        assert self._label_encoder is not None

        embedder = self._get_embedder()
        X = embedder.encode([description], normalize_embeddings=True)
        proba = self._classifier.predict_proba(X)[0]

        best: tuple[str, float] | None = None
        for idx in np.argsort(proba)[::-1]:
            category = str(self._label_encoder.inverse_transform([int(idx)])[0])
            if not _is_included(category):
                continue
            if allowed is not None and category not in allowed:
                continue
            return category, float(proba[idx])

        return best

    def predict(
        self,
        description: str,
        is_expense: bool,
        allowed: set[str] | None = None,
    ) -> tuple[str, float, str]:
        if self.is_ready:
            ml_result = self._best_ml_category(description, allowed)
            if ml_result is not None:
                category, confidence = ml_result
                if confidence >= 0.35:
                    return category, confidence, "transformer"

        category, confidence = rules_categorize(
            description, is_expense, list(allowed) if allowed else None
        )
        if _is_included(category) and (allowed is None or category in allowed):
            return category, confidence, "rules"

        if self.is_ready:
            ml_result = self._best_ml_category(description, allowed)
            if ml_result is not None:
                return ml_result[0], ml_result[1], "transformer-low-confidence"

        if allowed:
            return next(iter(allowed)), 0.4, "catalog-default"

        return category, confidence, "rules-unresolved"

    def predict_batch(
        self,
        items: list[tuple[str, bool]],
        allowed_expense: set[str] | None = None,
        allowed_income: set[str] | None = None,
    ) -> list[tuple[str, float, str]]:
        results = []
        for desc, is_exp in items:
            allowed = allowed_expense if is_exp else allowed_income
            results.append(self.predict(desc, is_exp, allowed))
        return results


classifier = TransformerExpenseClassifier()
