FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source
COPY ["backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj", "."]
RUN dotnet restore
COPY backend/scripts/FilmApiSeeder/ .
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish .
LABEL org.opencontainers.image.title="CineBase Film API Seeder"
LABEL org.opencontainers.image.version="iteration-6"
ENTRYPOINT ["dotnet", "FilmApiSeeder.dll"]
