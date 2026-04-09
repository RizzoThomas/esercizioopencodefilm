# Stato Progetto

Data aggiornamento: 2026-03-18

## Branch di lavoro
- `dev_iteration_2_frontend`

## Stato generale
- Iterazione 2 frontend: **in avanzamento, stabile per test manuale**
- Backend API: **stabile**, endpoint allineati e test automatici verdi
- Iterazione 2.1 media upload: **pianificata** (documento pronto per implementazione AI-guidata)

## Completato in questa sessione
- Creato progetto `CineBase.Web` (ASP.NET Core) con pagine statiche e moduli JS.
- Definite porte fisse:
  - Backend `FilmAPI` su `http://localhost:5000`
  - Frontend `CineBase.Web` su `http://localhost:5001`
- Configurato CORS backend per il frontend locale.
- Risolto conflitto di compilazione multi-progetto escludendo i sorgenti `CineBase.Web` dal `FilmAPI.csproj`.
- Corretto mapping endpoint per gruppi Minimal API (`""` al posto di `"/"` nei route builder di gruppo).
- Risolti errori JS bloccanti nelle pagine CRUD (`Invalid left-hand side in assignment`).
- Allineato il frontend ai DTO backend per CRUD:
  - `films`: campi `dataProduzione`, `copertinaPath`, `filmatoPath`, `registaId`
  - `cinemas`: escluso dal payload il campo `telefono` (non supportato dal backend)
  - `proiezioni`: payload coerente con `cinemaId`, `filmId`, `data`, `ora`
- Migliorata gestione errori lato frontend (`api.js`, `utils.js`).
- Refactor struttura repository:
  - `frontend/CineBase.Web` per la web app
  - `backend/FilmAPI` per API, configurazioni e variabili ambiente
  - `tests/backend/` per il progetto test backend
  - `docs/` mantenuta top-level
- Rimossa configurazione hardcoded delle porte in codice (`Program.cs`) per frontend e backend.
- Configurazione porta via environment:
  - Backend: `backend/FilmAPI/.env` (`ASPNETCORE_URLS=http://localhost:5000`)
  - Frontend: `frontend/CineBase.Web/.env` (`ASPNETCORE_URLS=http://localhost:5001`)
- Aggiornata solution con i nuovi path dei progetti (`backend/FilmAPI`, `frontend/CineBase.Web`, `tests/backend`).
- Sistemata navbar frontend con stato attivo corretto per pagina corrente (films/registi/cinemas/proiezioni) anche con componenti caricati async.
- Uniformata terminologia UI in italiano nelle pagine admin (dashboard, films, registi, cinemas, proiezioni, footer).
- Rimossi dai modali frontend i campi non supportati dal backend:
  - `Telefono` (cinema)
  - `Posti Totali` (proiezioni)
  - `Data di Morte` (registi)
- Creato piano dettagliato Iterazione 2.1:
  - `docs/project/dev_iteration/2.1/PianoLavoro.md`
  - include upload copertina backend + trailer URL esterno + checklist test/accettazione.

## Verifiche eseguite
- Smoke test API backend CRUD: **OK** (create/delete cinema, film, proiezione).
- Test backend (`tests/backend/FilmAPI.Tests.csproj`): **66/66 PASS**.
- Verifica manuale frontend:
  - `registi.html`: caricamento elenco registi ripristinato.
  - `films.html`: errore JS bloccante risolto.
  - stato attivo navbar: corretto su tutte le pagine admin.
  - testi UI: coerenza lingua italiana verificata.

## Prossimi passi suggeriti
- Avviare Iterazione 2.1 implementando upload copertine immagini su backend (`POST /media/covers`).
- Mantenere trailer come URL esterno in `filmatoPath` con validazione URL lato backend.
- Aggiungere test integration backend dedicati all'upload media e ai nuovi vincoli.
