# CognomeNomeAPI

Backend ASP.NET Core Minimal API per esercitazione Film/Registi/Cinema/Proiezioni usando MariaDB ed EF Core.

Prerequisiti
- .NET 9 SDK
- MariaDB (o Docker)
- dotnet-ef tool (per migration)

Setup rapido
1. Avviare MariaDB (locale o con Docker): `docker-compose up -d`
2. Aggiungere tool ef: `dotnet tool install --global dotnet-ef --version 9.0.11`
3. Restore/build: `dotnet restore` && `dotnet build`
4. Migrazione e aggiornamento DB:
   - `dotnet ef migrations add InitialCreate`
   - `dotnet ef database update`
5. Eseguire l'app: `dotnet run`

Swagger UI (sviluppo): `http://localhost:5000/swagger`
