# Changelog Progetto

## 2026-03-18

### Added
- Nuovo progetto frontend `CineBase.Web` con struttura `wwwroot` completa.
- Pagine frontend: `index.html`, `dashboard.html`, `registi.html`, `films.html`, `cinemas.html`, `proiezioni.html`.
- Componenti riusabili: navbar/footer admin e landing.
- Moduli JS base: `api.js`, `utils.js`, `template-loader.js`, `form-handlers.js`, `navbar.js`.
- File environment frontend: `frontend/CineBase.Web/.env` e `frontend/CineBase.Web/.env.example`.
- File environment backend: `backend/FilmAPI/.env` e `backend/FilmAPI/.env.example`.
- Piano Iterazione 2.1: `docs/project/dev_iteration/2.1/PianoLavoro.md` con specifiche complete per media upload copertine + trailer URL.

### Changed
- Repository riorganizzato in cartelle top-level `frontend/`, `backend/`, `tests/`, con `docs/` mantenuta top-level.
- Progetto backend annidato in `backend/FilmAPI/` per simmetria con `frontend/CineBase.Web/`.
- Progetto test backend spostato in `tests/backend/`.
- Configurazione porte rimossa dal codice (`Program.cs`) e gestita via environment (`ASPNETCORE_URLS`) + launch settings.
- Aggiornata configurazione CORS in backend per richieste da `http://localhost:5001`.
- Corretto `FilmAPI.csproj` per evitare inclusione dei file C# del progetto frontend annidato.
- Uniformato routing endpoint Minimal API su route di gruppo (`MapGet("")`, `MapPost("")`).
- Allineamento payload frontend ai DTO backend:
  - Film: `dataProduzione`, `copertinaPath`, `filmatoPath`, `registaId`
  - Cinema: rimosso `telefono` dal payload
  - Proiezione: formato e campi coerenti con DTO (`cinemaId`, `filmId`, `data`, `ora`)

### Fixed
- Risolti errori JavaScript bloccanti (`Invalid left-hand side in assignment`) nelle pagine CRUD.
- Ripristinato caricamento elenco registi in `registi.html`.
- Migliorata gestione errori API nel frontend con messaggi espliciti in caso di backend non raggiungibile o risposta non valida.
- Corretto stato attivo navbar nelle pagine admin con componenti caricati async.
- Uniformata UI frontend in italiano nelle pagine admin e footer.
- Rimossi dai modali frontend i campi non supportati dal backend (`telefono`, `postiTotali`, `dataDiMorte`).

### Verified
- Smoke test API CRUD backend: OK (create/delete su cinema/film/proiezione).
- Test suite backend: `66` test passati su `66` (`tests/backend/FilmAPI.Tests.csproj`).
