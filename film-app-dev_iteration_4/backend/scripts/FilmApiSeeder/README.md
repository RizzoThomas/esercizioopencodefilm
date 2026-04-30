# FilmApiSeeder

`FilmApiSeeder` e un progetto console standalone pensato per inizializzare rapidamente un ambiente locale di `CineBase` con dati realistici e credibili, senza dover inserire manualmente film, cinema, sale e programmazione.

Lo scopo del progetto e duplice:

- popolare il database con un catalogo film molto piu ricco rispetto al seed demo minimale del backend
- creare una base dati utile per sviluppo, demo e verifica end-to-end del frontend ticketing multisala

In particolare il seeder:

- film reali recuperati da TMDB
- aggiorna o crea registi reali associati ai film
- assegna categorie coerenti ai film in base ai genre TMDB
- valorizza le copertine film con URL reali TMDB
- crea 20 cinema distribuiti sul territorio italiano
- crea sale multi-tipologia (`2D`, `3D`, `XL`, `ISENSE`)
- genera piantine posti persistite in `SalaPosti`, con coordinate, settori e posti wheelchair
- genera show realistici nei giorni successivi alla data di esecuzione

Il progetto riusa i model e `FilmDbContext` di `FilmAPI`, quindi scrive dati coerenti con lo schema reale dell'applicazione e con gli endpoint usati dal frontend.

## Quando usarlo

Usa `FilmApiSeeder` quando vuoi:

- inizializzare un database locale vuoto o quasi vuoto
- rigenerare la programmazione show partendo dai film gia presenti
- avere dati realistici per provare `programmazione.html`, catalogo film, cinema, sale e seat map
- fare demo interne o verifiche manuali dei flussi di ticketing

Non e pensato come seed di produzione e non sostituisce una pipeline editoriale reale.

## Configurazione

1. Copia `backend/.env.example` in `backend/.env`.
2. Valorizza almeno `TMDB_BEARER_TOKEN`.
3. Se necessario, adatta i parametri `DB_*` del file `backend/.env` al database usato da `FilmAPI`.

Esempio:

```env
TMDB_BEARER_TOKEN=<tmdb_bearer_token>
DB_HOST=localhost
DB_PORT=3306
DB_NAME=film-api-db
DB_USER=root
DB_PASSWORD=root
DB_USE_AUTODETECT=true
DB_SERVER_VERSION=10.11.0-mariadb
DEFAULT_TICKET_PRICE=8.50
```

Variabili rilevanti per il seeder:

- `TMDB_BEARER_TOKEN`: obbligatoria, usata per interrogare le API di The Movie Database
- `DB_*`: usate per connettersi allo stesso database del backend
- `DEFAULT_TICKET_PRICE`: prezzo base di fallback per gli show generati

## Esecuzione

Seed standard senza reset:

```bash
dotnet run --project backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj
```

Reset della sola programmazione e rigenerazione show:

```bash
dotnet run --project backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj -- --reset-shows --force
```

Reset completo del seed e rigenerazione totale:

```bash
dotnet run --project backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj -- --reset-all --force
```

Help:

```bash
dotnet run --project backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj -- --help
```

## Opzioni

- `--reset-shows`: elimina programmazione e dati ticketing collegati, poi rigenera gli show lasciando invariati film, registi, cinema, sale e posti.
- `--reset-all`: elimina anche film, registi, cinema, sale e posti seedati, poi rigenera tutto da zero.
- `--force`: conferma esplicita obbligatoria per le modalità di reset.
- `--help`: mostra l'help del comando.

## Cosa viene generato

Output tipico di una run completa:

- almeno 50 film reali da TMDB
- copertine film reali
- decine di registi distinti associati ai film
- 20 cinema italiani
- decine di sale distribuite tra le tipologie supportate
- migliaia di posti in `SalaPosti`
- programmazione show sui prossimi 7 giorni

Il numero esatto di film e show puo variare in base ai dati restituiti da TMDB e allo stato iniziale del database.

## Sicurezza operativa

- Le modalità di reset richiedono `--force` per evitare cancellazioni accidentali.
- `--reset-shows` e `--reset-all` sono mutuamente esclusivi.
- Il seeder usa `backend/.env` come unica sorgente condivisa con `FilmAPI`, evitando duplicazione di configurazione.

## Note

- Il seeder legge `backend/.env` come sorgente condivisa con `FilmAPI`.
- `--reset-shows` e `--reset-all` sono mutuamente esclusivi.
- Senza `TMDB_BEARER_TOKEN` il seed non parte.
- Il progetto e presente anche nella solution `claude-code-test.sln`, quindi puo essere eseguito direttamente da Visual Studio.
