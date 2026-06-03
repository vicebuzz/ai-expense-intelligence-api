#!/usr/bin/env bash
# Recreate venv after a broken reload (mpmath/sympy import errors)
set -euo pipefail
cd "$(dirname "$0")"
rm -rf .venv
python3.11 -m venv .venv
source .venv/bin/activate
python -m pip install --upgrade pip
pip install -r requirements.txt
python -c "from ml_model import classifier; classifier.load(); print('ML service imports OK, ready=', classifier.is_ready)"
