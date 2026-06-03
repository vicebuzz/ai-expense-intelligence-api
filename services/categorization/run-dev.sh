#!/usr/bin/env bash
# Run from services/categorization with venv activated
set -euo pipefail
cd "$(dirname "$0")"

# Only watch project Python files — never .venv (avoids reload storms + corrupt imports)
exec uvicorn main:app \
  --host 127.0.0.1 \
  --port 8000 \
  --reload \
  --reload-include 'main.py' \
  --reload-include 'ml_model.py' \
  --reload-include 'rules.py' \
  --reload-exclude '.venv' \
  --reload-exclude 'models' \
  --reload-exclude 'data'
