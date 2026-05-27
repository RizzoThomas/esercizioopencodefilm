FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source
COPY ["frontend/CineBase.Web/CineBase.Web.csproj", "frontend/CineBase.Web/"]
RUN cd frontend/CineBase.Web && dotnet restore
COPY frontend/CineBase.Web/ frontend/CineBase.Web/
RUN cd frontend/CineBase.Web && dotnet build -c Release -o /app/build
RUN cd frontend/CineBase.Web && dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
LABEL org.opencontainers.image.title="CineBase Frontend"
LABEL org.opencontainers.image.version="iteration-6"
HEALTHCHECK --interval=10s --timeout=5s --retries=10 CMD curl -f http://localhost:5001/ || exit 1
ENTRYPOINT ["dotnet", "CineBase.Web.dll"]
