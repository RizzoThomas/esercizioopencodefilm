# syntax=docker/dockerfile:1.7
# CineBase Seeder - Console app per popolamento database da TMDB
# Build: docker build -t cinebase-seeder -f backend/scripts/FilmApiSeeder/Dockerfile backend

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Layer cache: restore con solo il csproj
COPY scripts/FilmApiSeeder/FilmApiSeeder.csproj scripts/FilmApiSeeder/
RUN dotnet restore scripts/FilmApiSeeder/FilmApiSeeder.csproj

# Build e publish
COPY FilmAPI/ FilmAPI/
COPY scripts/FilmApiSeeder/ scripts/FilmApiSeeder/
RUN dotnet publish scripts/FilmApiSeeder/FilmApiSeeder.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage: runtime (non aspnet) perché è una console app
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Labels per tracciabilità
LABEL org.opencontainers.image.title="CineBase Seeder" \
      org.opencontainers.image.description="Console app that seeds CineBase database from TMDB API" \
      org.opencontainers.image.version="1.0.0" \
      org.opencontainers.image.source="https://github.com/ThomasRizzo5IA/claude-code-test"

# Security: esegui come utente non-root
USER app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FilmApiSeeder.dll"]
