# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY scripts/FilmApiSeeder/FilmApiSeeder.csproj scripts/FilmApiSeeder/
RUN dotnet restore scripts/FilmApiSeeder/FilmApiSeeder.csproj

COPY FilmAPI/ FilmAPI/
COPY scripts/FilmApiSeeder/ scripts/FilmApiSeeder/
RUN dotnet publish scripts/FilmApiSeeder/FilmApiSeeder.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FilmApiSeeder.dll"]
