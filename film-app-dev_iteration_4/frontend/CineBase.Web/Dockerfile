# syntax=docker/dockerfile:1.7
# CineBase Web - Frontend ASP.NET Core (Static Files MPA)
# Build: docker build -t cinebase-web -f frontend/CineBase.Web/Dockerfile frontend/CineBase.Web

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Layer cache: restore con solo il csproj
COPY CineBase.Web.csproj ./
RUN dotnet restore ./CineBase.Web.csproj

# Build e publish
COPY . ./
RUN dotnet publish ./CineBase.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Labels per tracciabilità
LABEL org.opencontainers.image.title="CineBase Web" \
      org.opencontainers.image.description="Frontend for CineBase film management application" \
      org.opencontainers.image.version="1.0.0" \
      org.opencontainers.image.source="https://github.com/ThomasRizzo5IA/claude-code-test"

# Security: esegui come utente non-root
USER app

# Supporto proxy/load balancer (ACA, Azure Front Door, etc.)
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl --fail http://localhost:8080/index.html || exit 1

ENTRYPOINT ["dotnet", "CineBase.Web.dll"]
