#!/bin/bash
# CineBase - Docker Compose Initialization Script
# Usage: ./docker-compose-init.sh [--clean]

set -e

CLEAN=false
if [ "$1" = "--clean" ]; then
  CLEAN=true
fi

echo "=== CineBase Docker Initialization ==="

if [ "$CLEAN" = true ]; then
  echo "[1/5] Cleaning previous containers and volumes..."
  docker-compose down -v 2>/dev/null || true
else
  echo "[1/5] Stopping existing containers (preserving volumes)..."
  docker-compose down 2>/dev/null || true
fi

echo "[2/5] Building Docker images..."
docker-compose build

echo "[3/5] Starting services (db, backend, frontend)..."
docker-compose up -d db backend frontend

echo "[4/5] Waiting for backend to be healthy..."
ATTEMPTS=0
MAX_ATTEMPTS=30
until docker-compose exec -T backend curl -f http://localhost:5000/health 2>/dev/null; do
  ATTEMPTS=$((ATTEMPTS + 1))
  if [ $ATTEMPTS -ge $MAX_ATTEMPTS ]; then
    echo "ERROR: Backend did not become healthy within 5 minutes"
    exit 1
  fi
  echo "  Waiting for backend health... (${ATTEMPTS}/${MAX_ATTEMPTS})"
  sleep 10
done
echo "  Backend is healthy!"

echo "[5/5] Running database seeder..."
docker-compose run --rm seeder || echo "WARNING: Seeder returned non-zero exit code, check logs"

echo ""
echo "=== Initialization Complete ==="
echo "Backend:  http://localhost:5000"
echo "Frontend: http://localhost:5001"
echo "Health:   http://localhost:5000/health"
echo ""
echo "To stop:  docker-compose down"
echo "To clean: docker-compose down -v"
