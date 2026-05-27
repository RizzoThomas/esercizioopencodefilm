FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy both project files (seeder references FilmAPI)
COPY ["backend/FilmAPI/FilmAPI.csproj", "backend/FilmAPI/"]
COPY ["backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj", "backend/scripts/FilmApiSeeder/"]

# Restore seeder (restores FilmAPI as dependency)
RUN dotnet restore backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj

# Copy all source code for both projects
COPY backend/ backend/

# Build and publish only the seeder
RUN dotnet build backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj -c Release -o /app/build
RUN dotnet publish backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish .
LABEL org.opencontainers.image.title="CineBase Film API Seeder"
LABEL org.opencontainers.image.version="iteration-6"
ENTRYPOINT ["dotnet", "FilmApiSeeder.dll"]
