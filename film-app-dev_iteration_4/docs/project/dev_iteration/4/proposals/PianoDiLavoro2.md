# Piano di Lavoro - Iterazione 4 (Cinema distribuiti, Programmazione Film, Ticketing)

Autore: OpenCode con GPT-5.3-Codex
Data: 2026-04-12
Branch target suggerito: `dev_iteration_4`

---

## Stato Avanzamento Fasi

| Fase | Stato | Data | Note |
| --- | --- | --- | --- |
| FASE 1 - Modello dati sale/show/ticketing e migration | Pending | - | |
| FASE 2 - API catalogo film/cinema/show (pubbliche + profilo cinema preferito) | Pending | - | |
| FASE 3 - Backend prenotazione posti con lock anti-race | Pending | - | |
| FASE 4 - Backend pagamento (Stripe, credito, misto) e ricariche credito | Pending | - | |
| FASE 5 - Ticketing digitale (email, PDF, barcode/QR) + validazione biglietti | Pending | - | |
| FASE 6 - Frontend `programmazione.html` v2 + modale scelta cinema | Pending | - | |
| FASE 7 - Frontend `scheda-film.html` + frontend `my-cinemas.html` | Pending | - | |
| FASE 8 - Frontend `acquista.html` + frontend `pagamento.html` | Pending | - | |
| FASE 9 - Frontend admin: sale, show, ricarica credito, validazione ticket | Pending | - | |
| FASE 10 - Test backend (inclusa concorrenza posti e pagamenti) | Pending | - | |
| FASE 11 - Test frontend/e2e, hardening, documentazione finale | Pending | - | |

## 1) Obiettivo Iterazione

Con questa iterazione portiamo CineBase da gestione CRUD + prenotazioni virtuali a piattaforma operativa per:

- gestione cinema distribuiti sul territorio nazionale
- discovery film in programmazione con UX moderna e centrata sul cinema preferito
- acquisto biglietti con selezione posti, pagamenti reali e validazione ticket in ingresso sala

## 1.1 Contesto attuale (da `status.md` e `changelog.md`)

- Iterazione 3 completata al 100%, backend stabile con `103/103` test verdi
- auth JWT, refresh token e RBAC attivi
- `programmazione.html` esiste ma espone card per proiezione (duplicazioni film su cinema/date diverse)
- backend ancora modellato su `Proiezione` legata a cinema+film+data+ora (ipotesi implicita: una sola sala per cinema)
- non esistono ancora: sale, piantina posti, acquisto reale, pagamenti, ticketing, validazione ticket

## 1.2 Gap da colmare

1. Modello dati insufficiente per multi-sala per cinema
2. UX programmazione non orientata al film e al cinema preferito
3. Nessun flusso reale di acquisto/pagamento/ticket
4. Mancano strumenti operativi per cinema staff (ricariche e validazione)

---

## 2) Requisiti Funzionali Consolidati

### 2.1 Programmazione pubblica (`/programmazione.html`)

- una card per film (non piu una card per show)
- filtro per categoria
- search per titolo film
- tabs/tag minimi:
  - `In evidenza` (priorita film con piu show nei prossimi 7 giorni)
  - `In uscita` (film non subito in sala ma in arrivo entro 14 giorni)
  - `Tutti i film`
- modale scelta cinema, ordinata per vicinanza geografica
- persistenza cinema selezionato:
  - anonimo: local storage
  - autenticato: profilo utente su backend (con sincronizzazione frontend/backend)
- in card: stato visivo film nel proprio cinema (`disponibile` / `non disponibile`) con icona

### 2.2 Scheda film (`/scheda-film.html`)

- dati film: immagine, titolo, durata, data rilascio, genere, regista, cast
- nuova descrizione lunga film (fino a 2000 caratteri)
- CTA `Vai agli show`
- lista date orizzontale con frecce dx/sx
- per data selezionata:
  - cinema selezionato (nome, citta, indirizzo)
  - raggruppamento per tipologia sala (ISENSE, XL, 3D, 2D)
  - bottoni orari show per ogni tipologia
- click orario:
  - autenticato: vai a `acquista.html`
  - anonimo: redirect login con callback

### 2.3 Modello show multi-sala

- show (ex proiezioni) ora legati a `film + sala + data/ora`
- ogni sala identificata univocamente da progressivo interno al cinema
- vincolo unique show: `CinemaId + SalaId + StartAt`
- regola anti-overlap stessa sala:
  - impossibile inserire show in intervallo sovrapposto ad altro show della stessa sala
  - validazione su durata film (o durata snapshot dello show)
- consentiti show contemporanei su sale diverse anche con stesso tipo sala

### 2.4 Admin show/sale

- rifacimento `proiezioni.html` per gestione show multi-sala
- nuova pagina admin sale con CRUD e editor piantina posti

### 2.5 Acquisto (`/acquista.html`) e pagamento (`/pagamento.html`)

- riepilogo acquisto (film, cinema, sala, data, ora, numero biglietti, totale)
- piantina posti con stati chiari:
  - occupato
  - prenotabile
  - selezionato
- max 10 posti per singolo acquisto
- metodi pagamento:
  - carta di credito (Stripe)
  - credito piattaforma
  - pagamento misto (parte credito, parte carta)

### 2.6 Credito piattaforma

- pagina protetta per `PowerUser` e `Admin` per ricaricare credito utente
- input ricerca utente per email
- audit obbligatorio: importo, data/ora, utente operatore che ha effettuato la ricarica

### 2.7 Concorrenza posti (best practice)

- evitare race condition su stesso posto/show tra utenti diversi
- implementare lock temporaneo server-side con TTL (prenotazione non definitiva)
- scadenza lock -> posto torna disponibile automaticamente

### 2.8 Ticket digitale e validazione in ingresso

- al pagamento completato:
  - pagina esito
  - email con riepilogo
  - PDF allegato (1 pagina per ticket/posto)
  - barcode + codice acquisto + QR code
- pagina protetta `PowerUser/Admin` per validazione ticket:
  - inserimento manuale codice
  - scanner barcode
  - scanner QR da smartphone/tablet
  - controllo cinema operativo selezionato per evitare validazioni fuori sede
- registrare su ticket: validato SI/NO + data/ora validazione

### 2.9 Pagina cinema per utenti finali (`/my-cinemas.html`)

- elenco cinema con card (nome, citta, indirizzo, tipologie sala)
- dettaglio `?IdCinema=` con barra date orizzontale e programmazione giornaliera
- per ogni film del giorno: card con immagine, titolo, descrizione breve, gruppi tipologia sala, bottoni orario
- click orario: auth-aware (acquisto o login con callback)

---

## 3) Decisioni Architetturali e Default Operativi

## 3.1 Nomenclatura show/proiezione

- business term: `Show`
- compatibilita tecnica iniziale: manteniamo endpoint/page storici `proiezioni` dove utile, ma payload e UI parlano di show
- roadmap: alias API temporanei + deprecazione controllata

## 3.2 Timezone e data/ora

- persistenza orari in UTC
- rendering frontend in timezone locale Italia (`Europe/Rome`)
- tutte le logiche featured/upcoming e scheduler lock usano tempo normalizzato backend

## 3.3 Logica tab programmazione

- `In evidenza`: film con almeno 1 show nei prossimi 7 giorni, ordinati per numero show decrescente
- `In uscita`: film senza show "immediato" nel cinema selezionato (oggi/domani) ma con almeno 1 show entro 14 giorni
- `Tutti`: catalogo completo con filtri attivi

## 3.4 Ordinamento cinema per vicinanza

- primaria: geolocalizzazione browser (con consenso)
- fallback: ultimo cinema selezionato / citta profilo / ordinamento alfabetico
- backend supporta calcolo distanza su lat/lng

## 3.5 Sicurezza redirect callback login

- consentire solo redirect relativi interni (`/path?...`)
- rifiutare URL assoluti esterni

## 3.6 Strategia anti-race per posti (raccomandata)

- lock pessimista a livello DB su `(ShowId, SalaPostoId)` con indice univoco
- tabella stato posto show con stati `Hold` / `Sold`
- lock con TTL (es. 10 minuti)
- conferma pagamento atomica in transazione: hold -> sold + emissione ticket
- endpoint idempotenti con `Idempotency-Key`

---

## 4) Design Tecnico - Modello Dati

## 4.1 Nuove entita

### Sala

```text
Sala(
  Id int PK,
  CinemaId int FK,
  NumeroProgressivo int required,
  TipoSala enum required,      -- ISENSE, XL, 3D, 2D
  Nome string? max 100,
  IsAttiva bool required
)
UNIQUE(CinemaId, NumeroProgressivo)
```

### SalaPosto

```text
SalaPosto(
  Id int PK,
  SalaId int FK,
  Settore string required default "PLATEA",
  Fila int required,
  Numero int required,
  PosX int?,
  PosY int?,
  IsAttivo bool required
)
UNIQUE(SalaId, Settore, Fila, Numero)
```

### Show (evoluzione di Proiezione)

```text
Show(
  Id int PK,
  CinemaId int FK,
  SalaId int FK,
  FilmId int FK,
  StartAtUtc datetime required,
  DurataMinutiSnapshot int required,
  PrezzoBase decimal(10,2) required,
  SupplementoSala decimal(10,2) required default 0
)
UNIQUE(CinemaId, SalaId, StartAtUtc)
```

### FilmCastMember

```text
FilmCastMember(
  Id int PK,
  FilmId int FK,
  Nome string required max 100,
  Cognome string required max 100,
  Ordine int required
)
```

### ShowPostoStato (core concorrenza posti)

```text
ShowPostoStato(
  Id int PK,
  ShowId int FK,
  SalaPostoId int FK,
  UserId int FK,
  Stato enum required,          -- Hold, Sold
  HoldToken string? max 120,
  ScadeAtUtc datetime?,
  OrdineId int FK?,
  UpdatedAtUtc datetime required
)
UNIQUE(ShowId, SalaPostoId)
INDEX(HoldToken)
INDEX(ScadeAtUtc)
```

### Ordine

```text
Ordine(
  Id int PK,
  CodiceOrdine string required unique,
  UserId int FK,
  ShowId int FK,
  CinemaId int FK,
  SalaId int FK,
  FilmId int FK,
  Stato enum required,          -- Pending, Paid, Failed, Cancelled, Expired
  NumeroBiglietti int required,
  TotaleLordo decimal(10,2) required,
  ImportoCredito decimal(10,2) required,
  ImportoCarta decimal(10,2) required,
  StripePaymentIntentId string? max 120,
  CreatedAtUtc datetime required,
  PaidAtUtc datetime?
)
```

### Biglietto

```text
Biglietto(
  Id int PK,
  OrdineId int FK,
  ShowId int FK,
  SalaPostoId int FK,
  UserId int FK,
  CodiceAcquisto string required unique,
  PrezzoBase decimal(10,2) required,
  Supplemento decimal(10,2) required,
  PrezzoTotale decimal(10,2) required,
  Stato enum required,          -- Emesso, Validato, Annullato
  QrToken string required unique,
  BarcodeValue string required,
  ValidatoAtUtc datetime?,
  ValidatoDaUserId int FK?,
  ValidatoCinemaId int FK?
)
UNIQUE(ShowId, SalaPostoId)
```

### MovimentoCredito

```text
MovimentoCredito(
  Id int PK,
  UserId int FK,
  Tipo enum required,           -- TopUp, DebitOrder, Refund
  Importo decimal(10,2) required,
  SaldoPre decimal(10,2) required,
  SaldoPost decimal(10,2) required,
  OperatoreUserId int FK?,      -- valorizzato in ricarica manuale
  CinemaId int FK?,
  OrdineId int FK?,
  CreatedAtUtc datetime required,
  Note string? max 500
)
```

## 4.2 Modifiche entita esistenti

- `Film`
  - `DescrizioneLunga` (max 2000)
  - `DataRilascio` (date)
  - relazione 1-N con `FilmCastMember`
- `Cinema`
  - `Latitudine`, `Longitudine` (per distanza)
- `User`
  - `CinemaPreferitoId` nullable
  - `CreditoResiduo` decimal(10,2) default 0
- `Proiezione` (compat layer)
  - evoluzione verso `Show` con riferimento a sala e start datetime

## 4.3 Vincoli business obbligatori

1. Max 10 posti per ordine
2. Nessuna sovrapposizione show nella stessa sala
3. Un posto non puo essere venduto due volte nello stesso show
4. Hold scaduti rimossi automaticamente
5. Ticket validabile una sola volta
6. Validazione ticket vincolata al cinema operativo selezionato

---

## 5) API e Permessi (Delta Iterazione 4)

## 5.1 Endpoint pubblici

- `GET /programmazione/films?tab=&search=&categoriaId=&cinemaId=`
- `GET /programmazione/cinemas?lat=&lng=`
- `GET /films/{id}/scheda?cinemaId=`
- `GET /my-cinemas`
- `GET /my-cinemas/{cinemaId}/schedule?date=`

## 5.2 Endpoint autenticati (`Authenticated`)

- `GET /profilo/cinema-preferito`
- `PUT /profilo/cinema-preferito`
- `GET /checkout/shows/{showId}/seat-map`
- `POST /checkout/holds` (acquisizione lock posti)
- `DELETE /checkout/holds/{holdToken}`
- `POST /checkout/orders`
- `POST /checkout/orders/{orderId}/pay` (credito/carta/misto)
- `GET /checkout/orders/{orderId}`

## 5.3 Endpoint PowerUser/Admin

- `POST /admin/credito/ricariche`
- `GET /admin/credito/ricariche?email=`
- `POST /admin/tickets/validate`
- `GET /admin/tickets/validate/{code}`
- CRUD sale e posti:
  - `GET/POST/PUT/DELETE /cinemas/{cinemaId}/sale`
  - `GET/PUT /sale/{salaId}/posti`

## 5.4 Endpoint webhook

- `POST /payments/stripe/webhook` (`AllowAnonymous` con signature verification)

## 5.5 Route guard frontend (nuove pagine)

| Pagina | Anonimo | User | PowerUser | Admin |
| --- | --- | --- | --- | --- |
| `programmazione.html` | SI | SI | SI | SI |
| `scheda-film.html` | SI | SI | SI | SI |
| `my-cinemas.html` | SI | SI | SI | SI |
| `acquista.html` | - | SI | SI | SI |
| `pagamento.html` | - | SI | SI | SI |
| `sale.html` (admin sale) | - | - | SI | SI |
| `ricarica-credito.html` | - | - | SI | SI |
| `validazione-biglietti.html` | - | - | SI | SI |

---

## 6) Frontend UX Plan

## 6.1 `programmazione.html` v2

- header con cinema corrente ben evidenziato
- pulsante `Cambia cinema` -> modale ordinata per distanza
- tabs `In evidenza`, `In uscita`, `Tutti`
- search titolo + filtro categoria
- card film unica con:
  - immagine
  - titolo
  - categorie
  - indicatore presenza film nel cinema selezionato
  - CTA verso `scheda-film.html?idFilm=...`

## 6.2 `scheda-film.html`

- hero film con metadata completi
- sezione descrizione lunga
- cast come lista nominativi
- rail date orizzontale con frecce
- sezione show per tipologia sala e orari cliccabili
- se stesso orario su piu sale stessa tipologia: bottoni separati con badge `Sala #`

## 6.3 `acquista.html`

- riepilogo show e prezzo in sidebar/card
- piantina posti responsive desktop/mobile
- legenda stati posti
- selezione max 10 posti
- timer hold con countdown
- CTA `Continua` verso `pagamento.html`

## 6.4 `pagamento.html`

- opzioni:
  - solo carta
  - solo credito
  - misto (credito + carta)
- calcolo in tempo reale importi
- gestione errori pagamento e retry idempotente

## 6.5 `my-cinemas.html`

- lista cinema a card
- dettaglio cinema con date orizzontali
- righe film del giorno con blocchi orario per tipologia sala

## 6.6 Admin UX

- `proiezioni.html` diventa workspace show per sala
- `sale.html`: CRUD sale + editor posti
- `ricarica-credito.html`: ricerca utente per email + storico ricariche
- `validazione-biglietti.html`: input manuale + scanner barcode/QR

---

## 7) Sicurezza, Concorrenza e Affidabilita

1. Lock posti atomico con transazione DB
2. Cleanup hold scaduti via background job schedulato
3. Idempotency key su creazione ordine e pagamento
4. Verifica firma webhook Stripe
5. Audit trail su ricariche credito e validazioni ticket
6. Rate limiting su endpoint validazione ticket
7. Sanitizzazione callback login per prevenire open redirect
8. Validazione server-side di tutti i prezzi (mai fidarsi del frontend)

---

## 8) Fasi di Implementazione (incrementale)

### FASE 1 - Modello dati sale/show/ticketing e migration

**Obiettivo**: introdurre schema persistente completo per sale, posti, show, ordini, ticket e credito.

**Attivita**:

1. Creare modelli: `Sala`, `SalaPosto`, `FilmCastMember`, `ShowPostoStato`, `Ordine`, `Biglietto`, `MovimentoCredito`
2. Evolvere `Proiezione` in Show-v2 (sala + start datetime + durata snapshot)
3. Estendere `Film`, `Cinema`, `User` con campi nuovi
4. Aggiornare `FilmDbContext` con relazioni e vincoli univoci
5. Creare migration `AddSaleShowTicketing`
6. Data migration: creare `Sala 1 - 2D` per cinema esistenti e migrare proiezioni storiche

**Verifica fase**:

- migration applicata senza perdita dati
- show legacy migrati su una sala default
- vincoli univoci attivi

**Checklist fase**:

- [ ] Modelli e enum creati
- [ ] Relazioni DbContext aggiornate
- [ ] Migration creata/applicata
- [ ] Seed/migrazione dati legacy validata

---

### FASE 2 - API catalogo film/cinema/show

**Obiettivo**: fornire API pubbliche per programmazione moderna e scheda film.

**Attivita**:

1. Endpoint programmazione film con tab, search e categorie
2. Endpoint cinema con ordinamento distanza
3. Endpoint dettaglio film con show per data/sala tipo
4. Endpoint profilo cinema preferito (`GET/PUT`)
5. DTO aggiornati con descrizione lunga, cast e disponibilita cinema selezionato

**Verifica fase**:

- payload coerenti con UX richiesta
- featured/upcoming/tutti funzionanti

**Checklist fase**:

- [ ] Endpoint programmazione pubblici pronti
- [ ] Logica featured/upcoming implementata backend
- [ ] Persistenza cinema preferito implementata

---

### FASE 3 - Backend prenotazione posti con lock anti-race

**Obiettivo**: garantire concorrenza corretta su selezione posti.

**Attivita**:

1. Endpoint seat map con stati posto per show
2. Endpoint `POST /checkout/holds` con TTL
3. Endpoint release hold e keep-alive
4. Background job cleanup hold scaduti
5. Validazione max 10 posti per ordine

**Verifica fase**:

- due utenti non possono ottenere hold sullo stesso posto
- scadenza hold rende posto nuovamente disponibile

**Checklist fase**:

- [ ] Lock atomico implementato
- [ ] TTL e cleanup automatico implementati
- [ ] Gestione conflitti 409 con dettaglio posti occupati

---

### FASE 4 - Backend pagamento (Stripe, credito, misto) e ricariche credito

**Obiettivo**: supportare tutti i metodi pagamento richiesti con robustezza transazionale.

**Attivita**:

1. Servizio ordine/pagamento con stato ordine
2. Integrazione Stripe PaymentIntent
3. Pagamento con credito piattaforma (totale o parziale)
4. Pagamento misto credito+carta
5. Endpoint ricarica credito per `PowerUser/Admin` con audit operatore

**Verifica fase**:

- combinazioni pagamento coperte
- ordini idempotenti e consistenti

**Checklist fase**:

- [ ] Stripe flow implementato
- [ ] Credito e pagamento misto implementati
- [ ] Ricarica credito con audit operatore implementata

---

### FASE 5 - Ticketing digitale + validazione biglietti

**Obiettivo**: emettere ticket digitali e abilitarne validazione operativa in cinema.

**Attivita**:

1. Emissione biglietti su ordine pagato
2. Generazione barcode + QR token
3. Generazione PDF (1 pagina per ticket)
4. Invio email riepilogo + allegato PDF
5. Endpoint/pagina validazione ticket con controllo cinema operativo

**Verifica fase**:

- ticket emesso una sola volta
- validazione singola con tracciamento data/ora/operatore

**Checklist fase**:

- [ ] Biglietti emessi correttamente
- [ ] Email + PDF inviati
- [ ] Validazione ticket protetta e auditata

---

### FASE 6 - Frontend `programmazione.html` v2 + modale scelta cinema

**Obiettivo**: rilasciare UX programmazione stile portale cinema moderno.

**Attivita**:

1. Nuovo layout tab/search/filtro categoria
2. Modale selezione cinema con ordinamento distanza
3. Persistenza locale e sync con profilo
4. Card film unica con stato disponibilita nel proprio cinema

**Verifica fase**:

- nessuna duplicazione card per show
- filtri e tabs performanti e coerenti

**Checklist fase**:

- [ ] Nuova UI programmazione completata
- [ ] Modale cinema e persistenza completate
- [ ] Redirect auth-aware mantenuto

---

### FASE 7 - Frontend `scheda-film.html` + `my-cinemas.html`

**Obiettivo**: completare discovery dettagliata film e navigazione cinema-centric.

**Attivita**:

1. Nuova pagina `scheda-film.html`
2. Rail date orizzontale e show raggruppati per tipo sala
3. Nuova pagina `my-cinemas.html` e vista `?IdCinema=`
4. CTA show -> acquista/login callback

**Verifica fase**:

- flusso programmazione -> scheda -> show operativo
- my-cinemas con date e show funzionali

**Checklist fase**:

- [ ] `scheda-film.html` completata
- [ ] `my-cinemas.html` completata

---

### FASE 8 - Frontend `acquista.html` + `pagamento.html`

**Obiettivo**: esperienza utente completa di acquisto e pagamento.

**Attivita**:

1. Nuova pagina acquisto con seat map e timer hold
2. Nuova pagina pagamento con tre modalita (carta/credito/misto)
3. Gestione esito pagamento e redirect finale

**Verifica fase**:

- UX mobile/desktop stabile
- blocco posti e pagamento coerenti

**Checklist fase**:

- [ ] `acquista.html` completata
- [ ] `pagamento.html` completata

---

### FASE 9 - Frontend admin: sale, show, ricarica credito, validazione ticket

**Obiettivo**: fornire strumenti operativi completi a PowerUser/Admin.

**Attivita**:

1. Nuova pagina admin sale con editor piantina
2. Refactor `proiezioni.html` in ottica show multi-sala
3. Nuova pagina ricarica credito
4. Nuova pagina validazione ticket con scanner

**Verifica fase**:

- operatore cinema puo gestire sale/show/credito/validazione senza workaround

**Checklist fase**:

- [ ] Pagina admin sale completata
- [ ] Refactor proiezioni/show completato
- [ ] Pagine credito e validazione completate

---

### FASE 10 - Test backend (inclusa concorrenza posti e pagamenti)

**Obiettivo**: estendere copertura test su dominio ticketing e pagamenti.

**Attivita**:

1. Nuovi integration test per sale/show e vincoli anti-overlap
2. Test concorrenza hold su stessi posti (parallel requests)
3. Test ordini pagamento credito, carta, misto
4. Test validazione ticket e blocco doppia validazione
5. Test ricarica credito con audit operatore

**Verifica fase**:

- suite backend totalmente verde
- test di concorrenza deterministici

**Checklist fase**:

- [ ] Test dominio show/sale completati
- [ ] Test concorrenza lock posti completati
- [ ] Test pagamento/credito completati
- [ ] Test ticket validation completati

---

### FASE 11 - Test frontend/e2e, hardening, documentazione finale

**Obiettivo**: chiudere iterazione con qualita verificata e docs allineate.

**Attivita**:

1. Smoke test UI completo su nuove pagine
2. E2E principali:
   - anonimo -> login callback -> acquisto
   - selezione cinema e persistenza
   - acquisto con tutti i metodi pagamento
   - validazione ticket da mobile
3. Aggiornamento `status.md` e `changelog.md`

**Verifica fase**:

- criteri di accettazione tutti soddisfatti
- documentazione aggiornata

**Checklist fase**:

- [ ] Smoke + e2e completati
- [ ] Hardening sicurezza completato
- [ ] `status.md` aggiornato
- [ ] `changelog.md` aggiornato

---

## 9) Strategia di Migrazione da Proiezione Legacy a Show Multi-Sala

1. Aggiunta tabelle/campi nuovi senza rimuovere legacy
2. Creazione sala default per ogni cinema esistente
3. Migrazione record `Proiezione` su show con `SalaId` default
4. Compat layer API: endpoint legacy continuano a rispondere con formato compatibile
5. Refactor frontend graduale verso nuove API
6. Pulizia tecnica legacy solo dopo stabilizzazione test e produzione

---

## 10) Piano Test (macro)

## 10.1 Backend - nuove suite suggerite

- `SaleIntegrationTests` (CRUD sale, vincoli progressivo)
- `ShowIntegrationTests` (vincolo unique, anti-overlap)
- `SeatLockIntegrationTests` (hold, expiry, race condition)
- `CheckoutIntegrationTests` (ordine + pagamento credito/carta/misto)
- `TicketIntegrationTests` (emissione, pdf metadata, validazione)
- `CreditoIntegrationTests` (ricarica e movimenti)

## 10.2 Frontend - test funzionali

- `programmazione` con tabs/filter/search e cinema selector
- `scheda-film` con rail date + show groups
- `acquista` seat map + timer + max 10
- `pagamento` combinazioni metodi
- `my-cinemas` elenco + dettaglio date/show
- pagine admin nuove

## 10.3 Concorrenza e resilienza

- test paralleli su stesso posto
- retry/idempotency su pagamento
- webhook Stripe replay-safe

---

## 11) Criteri di Accettazione Iterazione 4

L'iterazione e completata quando tutte le condizioni seguenti sono vere:

1. `programmazione.html` mostra una card per film, non per show
2. tabs `In evidenza`, `In uscita`, `Tutti` funzionano con logica corretta
3. filtro categoria e search titolo funzionano insieme
4. cinema selezionabile da modale con ordinamento distanza
5. cinema preferito persistito e sincronizzato tra frontend/backend
6. `scheda-film.html` mostra dati estesi film (descrizione lunga + cast)
7. show raggruppati per tipologia sala con date orizzontali
8. backend impedisce sovrapposizione show nella stessa sala
9. admin gestisce sale e piantina posti
10. `acquista.html` visualizza posti liberi/occupati/hold e impone max 10 posti
11. concorrenza posti robusta: nessun doppio acquisto stesso posto/show
12. `pagamento.html` supporta carta, credito, misto
13. ricarica credito disponibile solo a PowerUser/Admin con audit operatore
14. pagamento completato genera ticket, invia email e allegato PDF
15. ticket validabile una sola volta da pagina protetta staff
16. validazione ticket bloccata su cinema non coerente
17. `my-cinemas.html` completa con card cinema + programmazione per giorno
18. route guard aggiornato per tutte le nuove pagine
19. suite backend verde dopo estensione test
20. `status.md` e `changelog.md` aggiornati con esito finale

---

## 12) Nuovi File Previsti (sintesi)

## 12.1 Backend (`backend/FilmAPI/`)

- `Model/Sala.cs`
- `Model/SalaPosto.cs`
- `Model/FilmCastMember.cs`
- `Model/ShowPostoStato.cs`
- `Model/Ordine.cs`
- `Model/Biglietto.cs`
- `Model/MovimentoCredito.cs`
- `DTO/ShowDTO.cs` (o evoluzione `ProiezioneDTO`)
- `DTO/SalaDTO.cs`
- `DTO/SeatMapDTO.cs`
- `DTO/CheckoutDTO.cs`
- `DTO/TicketDTO.cs`
- `DTO/CreditoDTO.cs`
- `Services/SalaService.cs`
- `Services/ShowService.cs`
- `Services/SeatLockService.cs`
- `Services/CheckoutService.cs`
- `Services/PaymentService.cs`
- `Services/TicketService.cs`
- `Services/CreditoService.cs`
- `Services/TicketValidationService.cs`
- `Endpoints/SaleEndpoints.cs`
- `Endpoints/CheckoutEndpoints.cs`
- `Endpoints/PaymentEndpoints.cs`
- `Endpoints/TicketEndpoints.cs`
- `Endpoints/CreditoEndpoints.cs`
- `Endpoints/ProgrammazioneEndpoints.cs`

## 12.2 Frontend (`frontend/CineBase.Web/wwwroot/`)

- `scheda-film.html`
- `acquista.html`
- `pagamento.html`
- `my-cinemas.html`
- `sale.html` (admin)
- `ricarica-credito.html` (admin/power)
- `validazione-biglietti.html` (admin/power)
- `js/pages/scheda-film.js`
- `js/pages/acquista.js`
- `js/pages/pagamento.js`
- `js/pages/my-cinemas.js`
- `js/pages/sale.js`
- `js/pages/ricarica-credito.js`
- `js/pages/validazione-biglietti.js`
- aggiornamento `js/pages/programmazione.js`, `js/pages/proiezioni.js`, `js/api.js`, `js/route-guard.js`, navbar/footer

## 12.3 Test (`tests/backend/` + eventuali e2e)

- `Integration/SaleIntegrationTests.cs`
- `Integration/ShowIntegrationTests.cs`
- `Integration/SeatLockIntegrationTests.cs`
- `Integration/CheckoutIntegrationTests.cs`
- `Integration/TicketIntegrationTests.cs`
- `Integration/CreditoIntegrationTests.cs`

---

## 13) Prompt Guida (fase-by-fase)

Regola comune per ogni fase:

- implementare solo la fase richiesta
- al termine aggiornare tabella `Stato Avanzamento Fasi`
- spuntare checklist fase con `[x]`
- se bloccata, indicare causa e workaround

**Fase 1**
"Implementa Fase 1: crea modello dati sale/show/ticketing, aggiorna DbContext e migration con data migration da proiezioni legacy a show multi-sala default. Aggiorna checklist e stato fase."

**Fase 2**
"Implementa Fase 2: crea API catalogo programmazione (featured/upcoming/all, search, categoria), endpoint cinema preferito utente e endpoint scheda film/show raggruppati per sala tipo. Aggiorna checklist e stato fase."

**Fase 3**
"Implementa Fase 3: aggiungi seat map + lock temporaneo anti-race con TTL, release e cleanup job. Verifica conflitti concorrenti. Aggiorna checklist e stato fase."

**Fase 4**
"Implementa Fase 4: pagamento carta con Stripe, credito piattaforma e pagamento misto, con ordini idempotenti e ricariche credito da PowerUser/Admin con audit. Aggiorna checklist e stato fase."

**Fase 5**
"Implementa Fase 5: emissione ticket, invio email con PDF per ticket, barcode/QR e endpoint validazione ticket con controllo cinema operativo. Aggiorna checklist e stato fase."

**Fase 6**
"Implementa Fase 6: rifai programmazione.html con tabs/search/categoria, modale scelta cinema, card unica per film e stato disponibilita nel cinema preferito. Aggiorna checklist e stato fase."

**Fase 7**
"Implementa Fase 7: crea scheda-film.html con rail date/show per sala tipo e crea my-cinemas.html con dettaglio programmazione per giorno. Aggiorna checklist e stato fase."

**Fase 8**
"Implementa Fase 8: crea acquista.html con piantina posti e timer hold, crea pagamento.html con metodi carta/credito/misto. Aggiorna checklist e stato fase."

**Fase 9**
"Implementa Fase 9: crea pagine admin sale, ricarica credito e validazione ticket; aggiorna proiezioni.html in gestione show multi-sala. Aggiorna checklist e stato fase."

**Fase 10**
"Implementa Fase 10: aggiungi test backend per sale/show/lock/pagamento/ticket/credito e verifica suite verde. Aggiorna checklist e stato fase."

**Fase 11**
"Implementa Fase 11: esegui smoke/e2e frontend, hardening sicurezza, aggiorna status.md e changelog.md, chiudi iterazione. Aggiorna checklist e stato fase."
