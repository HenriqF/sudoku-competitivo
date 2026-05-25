@echo off
docker compose -f "%~dp0/src/services/bd/docker-compose.yml" up -d --build --wait
dotnet run --project "%~dp0\src\services\bd" pop