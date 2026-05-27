# Multi-stage: Build con SDK, Runtime con aspnet:9.0
# Usa il pattern esatto dal PianoDiLavoro sezione 4.1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source
COPY ["backend/FilmAPI/FilmAPI.csproj", "backend/FilmAPI/"]
RUN cd backend/FilmAPI && dotnet restore
COPY backend/FilmAPI/ backend/FilmAPI/
RUN cd backend/FilmAPI && dotnet build -c Release -o /app/build
RUN cd backend/FilmAPI && dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=10s --timeout=5s --retries=10 \
    CMD curl -f http://localhost:5000/health || exit 1
ENTRYPOINT ["dotnet", "FilmAPI.dll"]
