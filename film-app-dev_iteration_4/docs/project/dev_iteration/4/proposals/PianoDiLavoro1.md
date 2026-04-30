# Piano di Lavoro - Iterazione 4

Autore: OpenCode con GLM-5.1
Data: 2026-04-12
Branch target suggerito: `dev_iteration_4`

---

## Stato Avanzamento Fasi

| Fase | Stato | Data | Note |
| --- | --- | --- | --- |
| FASE 1 - Modello Dati, Nuove Entita, Migration | - | - | - |
| FASE 2 - Backend: CRUD Sale + Piantina Posti | - | - | - |
| FASE 3 - Backend: CRUD Show + Validazione Sovrapposizioni | - | - | - |
| FASE 4 - Backend: Film Detail + Cinema Preferito + Geolocazione | - | - | - |
| FASE 5 - Backend: Seat Hold + Acquisto + Biglietto + Pagamento | - | - | - |
| FASE 6 - Backend: PDF Biglietto + Email + Validazione | - | - | - |
| FASE 7 - Frontend: Redesign Programmazione + Selettore Cinema | - | - | - |
| FASE 8 - Frontend: Scheda Film + My Cinemas | - | - | - |
| FASE 9 - Frontend: Acquisto + Pagamento | - | - | - |
| FASE 10 - Frontend: Admin (Sale, Show, Validazione, Credito) + Profilo | - | - | - |
| FASE 11 - Test e2e + Verifica Finale | - | - | - |

---

## 1) Obiettivo Iterazione

Trasformare CineBase da piattaforma con proiezioni semplici (1 cinema = 1 sala implicita) a sistema completo di gestione multisala con prenotazione posti, acquisto biglietti e pagamento:

- **Gestione cinema multisala**: ogni cinema ha N sale identificate da numero progressivo e tipologia (2D, 3D, ISENSE, XL)
- **Programmazione film-centric**: la pagina programmazione mostra film (non singole proiezioni) con indicazione disponibilita nel cinema scelto, simile a ucicinemas.it/film
- **Prenotazione posti specifici**: piantina interattiva della sala con selezione posti e protezione da race condition (seat hold con scadenza)
- **Acquisto biglietti**: pagamento con carta di credito (Stripe) e/o credito piattaforma, con supporto pagamento misto
- **Biglietto elettronico**: PDF con QR code, barcode, invio email, validazione al cinema da parte di operatori PowerUser/Admin
- **Cinema preferito**: salvataggio cinema selezionato in profilo (se loggato) o localStorage (se anonimo), con ordinamento per prossimita geografica
- **Pagina my-cinemas**: elenco cinema gestiti con programmazione per data, simile a ucicinemas.it/cinema

## 1.1 Contesto attuale (da `status.md` e `changelog.md`)

- Iterazione 3 completata; backend stabile con **103/103 test verdi**
- Proiezione = associazione diretta Film-Cinema-DataOra (nessun concetto di sala)
- Prenotazione = virtuale (numero posti, nessun posto specifico)
- Programmazione (`programmazione.html`) mostra card per ogni proiezione (duplicati per stesso film in cinema diversi)
- Nessun sistema di pagamento
- Nessun concetto di biglietto/PDF/email/QR code

## 1.2 Architettura repository

```text
repo-root/
|- backend/FilmAPI/          (API .NET 9 Minimal API + MariaDB)
|- frontend/CineBase.Web/    (MPA statico, vanilla JS + Tailwind)
|- tests/backend/            (xUnit + integration)
|- docs/
```

---

## 2) Ruoli, Permessi e Redirect

## 2.1 Definizione ruoli (invariata dall'iterazione 3)

| Ruolo | Enum | Descrizione |
| --- | --- | --- |
| Admin | `2` | Massimo privilegio: CRUD su tutto, gestione utenti/ruoli, validazione biglietti, ricarica credito |
| PowerUser | `1` | CRUD su Film/Show/Registi/Categorie/Sale; Read su Cinema; validazione biglietti; ricarica credito |
| User | `0` | Programmazione, profilo, acquisto biglietti, cinema preferito; niente area admin |
| Anonimo | - | Accesso pubblico a index/programmazione/my-cinemas/scheda-film in sola lettura |

## 2.2 Matrice permessi API (aggiornamento iterazione 4)

| Endpoint | Anonimo | User | PowerUser | Admin |
| --- | --- | --- | --- | --- |
| **Auth** (register/login/refresh) | SI | - | - | - |
| **Auth** (logout/me) | - | SI | SI | SI |
| **Film** GET | SI | SI | SI | SI |
| **Film** CUD | - | - | SI | SI |
| **Cinema** GET | SI | SI | SI | SI |
| **Cinema** CUD | - | - | - | SI |
| **Categorie** GET | SI | SI | SI | SI |
| **Categorie** CUD | - | - | SI | SI |
| **Registi** GET | - | - | SI | SI |
| **Registi** CUD | - | - | SI | SI |
| **Sale** GET | SI | SI | SI | SI |
| **Sale** CUD | - | - | SI | SI |
| **Show** GET | SI | SI | SI | SI |
| **Show** CUD | - | - | SI | SI |
| **Posti** GET (per sala) | SI | SI | SI | SI |
| **Posti** CUD (piantina) | - | - | SI | SI |
| **SeatHold** POST/DELETE | - | SI | - | SI |
| **Acquisto** CRUD | - | SI (propri) | - | SI (tutti) |
| **Biglietto** GET (propri) | - | SI | - | SI |
| **Biglietto** validazione | - | - | SI | SI |
| **CreditoUtente** GET (proprio) | - | SI | - | SI |
| **RicaricaCredito** POST | - | - | SI | SI |
| **RicaricaCredito** GET (storico) | - | - | SI | SI |
| **Profilo** GET/PUT | - | SI | SI | SI |
| **Cinema Preferito** GET/PUT | - | SI | SI | SI |
| **Media** upload | - | - | SI | SI |
| **Admin Utenti** | - | - | - | SI |
| **Pagamento** Stripe intent | - | SI | - | SI |
| **Pagamento** Stripe webhook | SI* | - | - | - |

*Stripe webhook usa firma per autenticazione, non JWT.

## 2.3 Matrice permessi pagine frontend (aggiornamento iterazione 4)

| Pagina | Anonimo | User | PowerUser | Admin |
| --- | --- | --- | --- | --- |
| `index.html` | SI | SI | SI | SI |
| `programmazione.html` | SI | SI | SI | SI |
| `my-cinemas.html` (nuova) | SI | SI | SI | SI |
| `scheda-film.html` (nuova) | SI | SI | SI | SI |
| `login.html` | SI | - | - | - |
| `registrazione.html` | SI | - | - | - |
| `profilo.html` | - | SI | SI | SI |
| `acquista.html` (nuova) | - | SI | SI | SI |
| `pagamento.html` (nuova) | - | SI | SI | SI |
| `esito-acquisto.html` (nuova) | - | SI | SI | SI |
| `dashboard.html` | - | - | SI | SI |
| `films.html` | - | - | SI | SI |
| `registi.html` | - | - | SI | SI |
| `cinemas.html` | - | - | - | SI |
| `sale.html` (nuova) | - | - | SI | SI |
| `proiezioni.html` (aggiornata) | - | - | SI | SI |
| `categorie.html` | - | - | SI | SI |
| `valida-biglietto.html` (nuova) | - | - | SI | SI |
| `ricarica-credito.html` (nuova) | - | - | SI | SI |

## 2.4 Regole di redirect obbligatorie (aggiornamento)

- utente non loggato su pagina protetta -> redirect `login.html?redirect=<pagina>`
- utente non loggato che clicca orario show -> redirect `login.html?redirect=<pagina_acquisto>`
- utente loggato senza ruolo sufficiente -> redirect `index.html?forbidden=true`
- utente loggato che apre `login.html` o `registrazione.html` -> redirect `index.html`
- utente anonimo che clicca "Acquista" -> redirect login; dopo login ritorno alla pagina di acquisto

---

## 3) Design Tecnico

## 3.1 Nuove entita

### TipologiaSala (enum)

```csharp
public enum TipologiaSala
{
    DueD = 0,    // 2D
    TreD = 1,    // 3D
    ISENSE = 2,
    XL = 3
}
```

### Sala

```text
Sala(
  Id int PK,
  CinemaId int FK required,
  Numero int required,            -- progressivo univoco nel cinema (1, 2, 3...)
  Nome string required max 50,    -- es. "SALA 1", "SALA 10"
  Tipologia TipologiaSala required,
  Supplemento decimal default 0   -- supplemento per tipologia sala
)
Navigation: Cinema, Posti, Show
Unique: (CinemaId, Numero)
```

### Posto

```text
Posto(
  Id int PK,
  SalaId int FK required,
  Fila int required,              -- numero fila (1, 2, 3...)
  Numero int required,            -- numero poltrona nella fila (1, 2, 3...)
  Settore string? max 50,         -- es. "PLATEA", "GALLERIA"
)
Navigation: Sala
Unique: (SalaId, Fila, Numero)
```

### Show (sostituisce Proiezione)

```text
Show(
  Id int PK,
  SalaId int FK required,
  FilmId int FK required,
  Data date required,
  OraInizio time required,
  Prezzo decimal required
)
Navigation: Sala, Film, Biglietti, SeatHolds
Unique: (SalaId, Data, OraInizio)
Derived: CinemaId via Sala.CinemaId
```

### SeatHold (prenotazione temporanea posto)

```text
SeatHold(
  Id int PK,
  ShowId int FK required,
  PostoId int FK required,
  UserId int FK required,
  CreatedAt DateTime required,
  ExpiresAt DateTime required
)
Navigation: Show, Posto, User
Unique: (ShowId, PostoId) per hold attivi (filtered index: ExpiresAt > now)
Index: ExpiresAt per cleanup
```

### Acquisto (ordine di acquisto)

```text
Acquisto(
  Id int PK,
  CodiceAcquisto string required unique,  -- GUID string
  UserId int FK required,
  ShowId int FK required,
  DataAcquisto DateTime required,
  ImportoTotale decimal required,
  CreditoUsato decimal required default 0,
  ImportoStripe decimal required default 0,
  StripePaymentIntentId string? max 255,
  MetodoPagamento string required,   -- "carta", "credito", "misto"
  Stato string required              -- "in_attesa", "completato", "fallito", "rimborsato"
)
Navigation: User, Show, Biglietti
```

### Biglietto

```text
Biglietto(
  Id int PK,
  AcquistoId int FK required,
  ShowId int FK required,
  PostoId int FK required,
  UserId int FK required,
  CodiceBiglietto string required unique,  -- per validazione QR/barcode
  Prezzo decimal required,
  Supplemento decimal required default 0,
  DataValidazione DateTime?,
  Validato bool default false
)
Navigation: Acquisto, Show, Posto, User
Unique: (ShowId, PostoId) -- un posto per show ha al massimo un biglietto
```

### CreditoUtente

```text
CreditoUtente(
  Id int PK,
  UserId int FK required unique,
  Saldo decimal required default 0
)
Navigation: User
```

### RicaricaCredito

```text
RicaricaCredito(
  Id int PK,
  UserId int FK required,       -- utente che riceve il credito
  OperatoreId int FK required,  -- PowerUser/Admin che effettua la ricarica
  Importo decimal required,
  DataOra DateTime required,
  Note string? max 500
)
Navigation: User (destinatario), User (operatore)
```

## 3.2 Modifiche entita esistenti

### Film: aggiungere

- `Descrizione` string? max 2000 -- descrizione testuale del film
- `Cast` string? max 1000 -- elenco nomi e cognomi separati da virgola

### User: aggiungere

- `CinemaPreferitoId` int? FK to Cinema
- Navigation: `Cinema? CinemaPreferito`

### Cinema: aggiungere

- `Latitudine` double? -- per ordinamento per prossimita
- `Longitudine` double? -- per ordinamento per prossimita
- `Telefono` string? max 20
- `CodiceLocale` string? max 50 -- codice identificativo per biglietti (es. "0131220507688")
- Navigation: `ICollection<Sala> Sale`

### Proiezione -> Show (rinominare e ristrutturare)

- Rimuovere `CinemaId` FK diretto (deriva da Sala.CinemaId)
- Aggiungere `SalaId` FK
- Aggiungere `Prezzo` decimal
- `Ora` -> `OraInizio` (solo componente orario)
- Rimuovere navigation `Cinema`
- Aggiungere navigation `Sala`, `Biglietti`, `SeatHolds`
- Unique constraint: `(SalaId, Data, OraInizio)`

### Prenotazione -> deprecata e rimossa

- L'entita Prenotazione (prenotazione virtuale con NumeroPosti) viene rimossa
- Sostituita da Biglietto (posto specifico, pagamento reale)
- Le vecchie API `/prenotazioni` vengono rimosse
- Il frontend viene aggiornato per mostrare Biglietti al posto di Prenotazioni

## 3.3 Relazioni e vincoli

```text
Regista (1) ----< (N) Film
Film    (1) ----< (N) Show >---- (1) Sala >---- (1) Cinema
Sala    (1) ----< (N) Posto
Film    (1) ----< FilmCategoria >---- (1) Categoria  (M:N)
Show    (1) ----< (N) Biglietto >---- (1) Posto
Show    (1) ----< (N) SeatHold >---- (1) Posto
Acquisto(1) ----< (N) Biglietto
User    (1) ----< (N) Biglietto
User    (1) ----< (N) Acquisto
User    (1) ----< (N) SeatHold
User    (1) ----< (1) CreditoUtente
User    (1) ----< (N) RicaricaCredito (come destinatario)
User    (1) ----< (N) RicaricaCredito (come operatore)
User    (N) ----> (1) Cinema (CinemaPreferito)
```

**Vincoli unici**:

| Tabella | Vincolo |
| --- | --- |
| Sala | `UNIQUE(CinemaId, Numero)` |
| Posto | `UNIQUE(SalaId, Fila, Numero)` |
| Show | `UNIQUE(SalaId, Data, OraInizio)` |
| Biglietto | `UNIQUE(ShowId, PostoId)` |
| SeatHold | `UNIQUE(ShowId, PostoId)` per hold attivi (filtered) |
| CreditoUtente | `UNIQUE(UserId)` |
| Acquisto | `UNIQUE(CodiceAcquisto)` |
| Biglietto | `UNIQUE(CodiceBiglietto)` |

**Delete behaviors**:

| Relazione | Behavior |
| --- | --- |
| Sala -> Cinema | Restrict |
| Show -> Sala | Restrict |
| Show -> Film | Restrict |
| Posto -> Sala | Cascade |
| Biglietto -> Acquisto | Cascade |
| Biglietto -> Show | Restrict |
| Biglietto -> Posto | Restrict |
| Biglietto -> User | Restrict |
| SeatHold -> Show | Cascade |
| SeatHold -> Posto | Cascade |
| SeatHold -> User | Cascade |
| CreditoUtente -> User | Cascade |
| RicaricaCredito -> User (destinatario) | Restrict |
| RicaricaCredito -> User (operatore) | Restrict |
| Acquisto -> User | Restrict |
| Acquisto -> Show | Restrict |

## 3.4 Strategia Seat Hold (race condition)

**Problema**: due utenti selezionano lo stesso posto contemporaneamente.

**Soluzione: Hold ottimistico con scadenza (best practice per ticketing)**

1. L'utente seleziona N posti (max 10) e il frontend chiama `POST /shows/{id}/seats/hold`
2. Il backend, in una **transazione atomica**:
   - Rimuove SeatHold scaduti per questo show (lazy cleanup)
   - Per ogni PostoId: verifica nessun Biglietto E nessun SeatHold attivo
   - Se tutti disponibili: crea SeatHold con `ExpiresAt = now + 8 min`
   - Se qualcuno non disponibile: restituisce **409 Conflict** con lista posti non disponibili
3. Il frontend mostra un **countdown di 8 minuti**
4. All'acquisto: il backend verifica SeatHold ancora attivi, crea Biglietti e rimuove SeatHold in una singola transazione
5. Se il timer scade: il frontend notifica l'utente, i posti tornano disponibili automaticamente

**Cleanup**:
- **Lazy**: alla creazione di un nuovo hold, vengono rimossi i hold scaduti per quello show
- **Background**: `BackgroundService` pulisce hold scaduti ogni 30 secondi

**Garanzie di concorrenza**:
- `UNIQUE(ShowId, PostoId)` su SeatHold previene doppio hold a livello database
- Transazione atomica per creazione hold + verifica
- Transazione atomica per creazione biglietti + rimozione hold

## 3.5 Strategia Pagamento Misto

**Flusso di acquisto**:

1. L'utente seleziona posti -> il sistema crea SeatHold
2. L'utente sceglie metodo pagamento su `/pagamento.html`:
   - **Credito piattaforma**: usa il saldo CreditoUtente (se sufficiente per coprire l'intero importo)
   - **Carta di credito**: pagamento intero via Stripe
   - **Misto**: parte con credito, parte con carta
3. Backend:
   - Calcola `importoTotale = (prezzo + supplemento) x numeroPosti`
   - Se `creditoUsato > 0`: verifica `CreditoUtente.Saldo >= creditoUsato` e sottrai
   - Se `rimanente > 0`: crea Stripe PaymentIntent
   - Se solo credito e sufficiente: completa direttamente senza Stripe
4. Stripe PaymentIntent:
   - Backend crea intent con `stripe.PaymentIntentCreate()`
   - Restituisce `clientSecret` al frontend
   - Frontend conferma con Stripe.js
   - Backend verifica stato e finalizza
5. Finalizzazione:
   - Crea Acquisto e Biglietti
   - Rimuovi SeatHold
   - Genera PDF e invia email
   - Aggiorna credito se usato

**Gestione fallimenti**:
- Se il pagamento Stripe fallisce: l'Acquisto viene marcato "fallito", il credito non viene addebitato, i SeatHold restano attivi fino a scadenza
- Se l'utente abbandona: i SeatHold scadono automaticamente, nessun addebito

## 3.6 NuGet packages

| Package | Versione | Scopo |
| --- | --- | --- |
| `Stripe.net` | 47.x | Pagamenti carta di credito |
| `QuestPDF` | 2024.x | Generazione PDF biglietti |
| `QRCoder` | 1.6.x | Generazione QR code |
| `MailKit` | 4.x | Invio email con allegato PDF |

## 3.7 Variabili environment da aggiungere

```env
# Stripe
STRIPE_SECRET_KEY=sk_test_...
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...

# SMTP
SMTP_HOST=smtp.example.com
SMTP_PORT=587
SMTP_USER=
SMTP_PASSWORD=
SMTP_FROM=noreply@cinebase.it
SMTP_FROM_NAME=CineBase

# Configurazione biglietti
DEFAULT_TICKET_PRICE=8.00
SEAT_HOLD_DURATION_MINUTES=8
CINEMA_ORGANIZZATORE=CineBase S.r.l.
BASE_URL=http://localhost:5001
```

## 3.8 Schema URL validazione biglietto

Il QR code su ogni biglietto codifica l'URL:

```
{BASE_URL}/valida-biglietto.html?codice={CodiceBiglietto}
```

Quando un operatore PowerUser/Admin scansiona il QR code:
1. Accede alla pagina di validazione (gia autenticato)
2. La pagina estrae il `codice` dal query param
3. Se l'operatore non ha ancora selezionato il proprio cinema, viene richiesto
4. Il sistema verifica: biglietto esistente, non gia validato, show appartenente al cinema dell'operatore
5. Se tutto OK: marca il biglietto come validato con timestamp
6. Mostra esito all'operatore (successo o errore con motivo)

---

## 4) Fasi di Implementazione (incrementale)

### FASE 1 - Modello Dati, Nuove Entita, Migration

**Obiettivo**: introdurre tutte le nuove entita, aggiornare quelle esistenti, creare la migration, aggiornare la suite di test esistente.

**Attivita**:

1. Installare i package NuGet richiesti (`Stripe.net`, `QuestPDF`, `QRCoder`, `MailKit`).
2. Creare file modello:
   - `Model/TipologiaSala.cs` (enum)
   - `Model/Sala.cs`
   - `Model/Posto.cs`
   - `Model/Show.cs` (sostituisce Proiezione)
   - `Model/SeatHold.cs`
   - `Model/Acquisto.cs`
   - `Model/Biglietto.cs`
   - `Model/CreditoUtente.cs`
   - `Model/RicaricaCredito.cs`
3. Aggiornare modelli esistenti:
   - `Model/Film.cs`: aggiungere `Descrizione` e `Cast`
   - `Model/Cinema.cs`: aggiungere `Latitudine`, `Longitudine`, `Telefono`, `CodiceLocale`, `Sale`
   - `Model/User.cs`: aggiungere `CinemaPreferitoId` e navigation `CinemaPreferito`
   - Rimuovere `Model/Proiezione.cs` (sostituito da Show)
   - Rimuovere `Model/Prenotazione.cs` (sostituito da Biglietto/Acquisto)
4. Aggiornare `Data/FilmDbContext.cs`:
   - Rimuovere `DbSet<Proiezione> Proiezioni`
   - Rimuovere `DbSet<Prenotazione> Prenotazioni`
   - Aggiungere DbSet per tutte le nuove entita
   - Configurare relazioni, vincoli unique, delete behaviors
   - Aggiornare Film-Categoria (invariato)
5. Aggiornare `.env` e `.env.example` con le nuove variabili.
6. Rimuovere vecchi DTO/servizi/endpoint relativi a Proiezione e Prenotazione:
   - Rimuovere `DTO/ProiezioneDTO.cs`
   - Rimuovere `DTO/ProfiloPrenotazioniAdminDTO.cs` (parzialmente: tenere ProfiloUpdateDTO e UserAdminDTO/UpdateRuoloDTO)
   - Rimuovere `Services/IProiezioneService.cs` + `ProiezioneService.cs`
   - Rimuovere `Services/IPrenotazioneService.cs` + `PrenotazioneService.cs`
   - Rimuovere `Endpoints/ProiezioniEndpoints.cs`
   - Rimuovere `Endpoints/PrenotazioniEndpoints.cs`
7. Creare DTO placeholder per le nuove entita (verranno popolati nelle fasi successive):
   - `DTO/SalaDTO.cs` (vuoto, da completare in Fase 2)
   - `DTO/ShowDTO.cs` (vuoto, da completare in Fase 3)
8. Aggiornare `Program.cs`:
   - Rimuovere registrazione ProiezioneService e PrenotazioneService
   - Rimuovere mapping ProiezioniEndpoints e PrenotazioniEndpoints
9. Aggiornare i test esistenti che referenziano Proiezione e Prenotazione:
   - Aggiornare `Integration/ApiIntegrationTests.cs`: rimuovere o commentare test Proiezione (P1-P10)
   - Aggiornare `Integration/PrenotazioneIntegrationTests.cs`: rimuovere (PR1-PR5)
   - Aggiornare `CustomWebApplicationFactory`: aggiornare reset DB per nuove tabelle
10. Creare migration: `dotnet ef migrations add AddMultisalaAndTickets`
11. Applicare migration e verificare schema.

**Verifica fase**:

- migration applicata correttamente
- tutte le nuove tabelle presenti nel DB
- test esistenti (esclusi quelli rimossi) ancora verdi
- compilazione backend senza errori

**Checklist fase**:

- [ ] Package NuGet installati e compilazione OK
- [ ] Nuove entita create (TipologiaSala, Sala, Posto, Show, SeatHold, Acquisto, Biglietto, CreditoUtente, RicaricaCredito)
- [ ] Film aggiornato con Descrizione e Cast
- [ ] Cinema aggiornato con Latitudine, Longitudine, Telefono, CodiceLocale, Sale
- [ ] User aggiornato con CinemaPreferitoId
- [ ] Proiezione e Prenotazione rimossi
- [ ] FilmDbContext aggiornato con nuovi DbSet, relazioni, vincoli
- [ ] Vecchi DTO/servizi/endpoint Proiezione e Prenotazione rimossi
- [ ] `.env` e `.env.example` aggiornati
- [ ] Test esistenti aggiornati (Proiezione/Prenotazione rimossi, altri OK)
- [ ] Migration `AddMultisalaAndTickets` creata e applicata
- [ ] Compilazione backend senza errori

---

### FASE 2 - Backend: CRUD Sale + Piantina Posti

**Obiettivo**: implementare gestione completa delle sale e della piantina posti.

**Attivita**:

1. Creare DTO:
   - `DTO/SalaDTO.cs`: SalaDTO, SalaDetailDTO (con posti), SalaCreateDTO, SalaUpdateDTO, PiantinaPostiDTO (definizione griglia per generazione posti)
2. Creare `Services/ISalaService.cs` + `Services/SalaService.cs`:
   - `GetAllAsync()` / `GetPagedAsync()` (con search)
   - `GetByIdAsync(id)` (con posti)
   - `GetByCinemaAsync(cinemaId)` (sale di un cinema)
   - `CreateAsync(SalaCreateDTO)` (validazione Cinema esistente, Numero univoco nel cinema)
   - `UpdateAsync(id, SalaUpdateDTO)`
   - `DeleteAsync(id)` (solo se non ha show futuri)
   - `GeneraPiantinaAsync(salaId, PiantinaPostiDTO)` -- genera posti da definizione griglia
   - `GetPostiAsync(salaId)` -- lista posti di una sala
   - `DeletePiantinaAsync(salaId)` -- rimuovi tutti i posti (solo se nessun biglietto)
3. Creare `Endpoints/SaleEndpoints.cs`:
   - `GET /sale` (paginato, pubblico)
   - `GET /sale/{id}` (dettaglio con posti, pubblico)
   - `GET /cinemas/{cinemaId}/sale` (sale di un cinema, pubblico)
   - `POST /sale` (PowerUserOrAdmin)
   - `PUT /sale/{id}` (PowerUserOrAdmin)
   - `DELETE /sale/{id}` (PowerUserOrAdmin)
   - `POST /sale/{id}/posti` (genera piantina, PowerUserOrAdmin)
   - `GET /sale/{id}/posti` (lista posti, pubblico)
   - `DELETE /sale/{id}/posti` (rimuovi piantina, PowerUserOrAdmin)
4. Registrare DI e mapping in `Program.cs`.
5. Aggiornare `CinemaDTO` per includere le sale nel dettaglio cinema.
6. Aggiornare `CinemaService.GetByIdAsync` per includere le sale.
7. Aggiungere test di integrazione per Sale (CRUD + piantina).

**Verifica fase**:

- CRUD sale funzionante
- Piantina posti generabile e consultabile
- Sale visibili nel dettaglio cinema
- Test verdi

**Checklist fase**:

- [ ] DTO Sale creati (SalaDTO, SalaDetailDTO, SalaCreateDTO, SalaUpdateDTO, PiantinaPostiDTO)
- [ ] `ISalaService`/`SalaService` implementati con CRUD + piantina
- [ ] `SaleEndpoints` mappati con policy RBAC corrette
- [ ] CinemaDTO/CinemaService aggiornati per includere sale
- [ ] Test integrazione Sale aggiunti e verdi

---

### FASE 3 - Backend: CRUD Show + Validazione Sovrapposizioni

**Obiettivo**: implementare gestione degli show (ex-proiezioni) con validazione temporale e struttura multisala.

**Attivita**:

1. Creare DTO:
   - `DTO/ShowDTO.cs`: ShowDTO (con Sala.Cinema info, Film info, Prezzo), ShowCreateDTO, ShowUpdateDTO, ShowPagedResultDTO
2. Creare `Services/IShowService.cs` + `Services/ShowService.cs`:
   - `GetAllAsync()` / `GetPagedAsync(page, pageSize, search)`
   - `GetByIdAsync(id)`
   - `CreateAsync(ShowCreateDTO)`: validazioni
     - Sala esistente
     - Film esistente
     - **Validazione sovrapposizione**: per la stessa sala nella stessa data, la nuova OraInizio non deve essere anteriore a (OraInizio show precedente + durata film precedente)
     - Unique constraint (SalaId, Data, OraInizio)
   - `UpdateAsync(id, ShowUpdateDTO)`: stesse validazioni di create
   - `DeleteAsync(id)` (solo se nessun biglietto venduto)
   - `GetByCinemaAndDateAsync(cinemaId, date)`: tutti gli show di un cinema per una data
   - `GetByFilmAsync(filmId, cinemaId?, data?)`: show di un film, filtrabili
   - `GetShowWithAvailabilityAsync(id)`: show con mappa posti disponibili/occupati/hold
3. Creare `Endpoints/ShowEndpoints.cs`:
   - `GET /show` (paginato, pubblico)
   - `GET /show/{id}` (pubblico)
   - `POST /show` (PowerUserOrAdmin)
   - `PUT /show/{id}` (PowerUserOrAdmin)
   - `DELETE /show/{id}` (PowerUserOrAdmin)
   - `GET /cinemas/{cinemaId}/show?data=` (pubblico, show per cinema e data)
   - `GET /films/{filmId}/show?cinemaId=&data=` (pubblico, show per film)
   - `GET /show/{id}/availability` (pubblico, posti disponibili)
4. Aggiornare `FilmService` per includere show nel dettaglio film.
5. Aggiornare `DataSeeder` per creare cinema con sale e alcuni show di esempio.
6. Registrare DI e mapping in `Program.cs`.
7. Aggiungere test di integrazione per Show (CRUD + validazione sovrapposizioni + query per cinema/film).

**Verifica fase**:

- CRUD show funzionante con validazione sovrapposizioni
- Query show per cinema/data e per film funzionanti
- Show con info sala e cinema nel DTO
- Test verdi

**Checklist fase**:

- [ ] DTO Show creati (ShowDTO, ShowCreateDTO, ShowUpdateDTO, ShowPagedResultDTO)
- [ ] `IShowService`/`ShowService` implementati con validazione sovrapposizioni
- [ ] `ShowEndpoints` mappati con policy RBAC corrette
- [ ] Endpoint query show per cinema/data e per film funzionanti
- [ ] FilmService aggiornato per includere show
- [ ] DataSeeder aggiornato con sale e show di esempio
- [ ] Test integrazione Show aggiunti e verdi

---

### FASE 4 - Backend: Film Detail + Cinema Preferito + Geolocazione

**Obiettivo**: completare le API per la visualizzazione film-centric e la gestione del cinema preferito.

**Attivita**:

1. Aggiornare `DTO/FilmDTO.cs`:
   - `FilmDTO`: aggiungere `Descrizione`, `Cast`
   - `FilmDTO`: aggiungere `InEvidenza` (bool, calcolato: >= N show nei prossimi 7 giorni)
   - `FilmDTO`: aggiungere `InUscita` (bool, calcolato: DataProduzione nei prossimi 14 giorni e non ancora in programmazione)
   - `FilmDetailDTO`: nuovo DTO con tutti i campi + show disponibili raggruppati per data
   - `FilmCreateDTO`/`FilmUpdateDTO`: aggiungere `Descrizione`, `Cast`
2. Aggiornare `FilmService`:
   - Gestione `Descrizione` e `Cast` in create/update
   - Nuovo metodo `GetFilmDetailAsync(id)`: film completo con show raggruppati per data
   - Calcolo `InEvidenza` e `InUscita` nel mapping DTO
   - Metodo `GetFilmsWithShowInfoAsync(cinemaId?)`: lista film con indicazione disponibilita nel cinema
3. Creare endpoint film detail:
   - `GET /films/{id}/detail` (pubblico): FilmDetailDTO con show raggruppati per data
4. Aggiornare `ProfiloService`:
   - `GetProfiloAsync`: includere CinemaPreferito info
   - Aggiungere `SetCinemaPreferitoAsync(userId, cinemaId)`: validazione cinema esistente
   - Aggiungere `GetCinemaPreferitoAsync(userId)`: restituisce cinema preferito
5. Creare endpoint cinema preferito:
   - `GET /profilo/cinema-preferito` (Authenticated): restituisce il cinema preferito
   - `PUT /profilo/cinema-preferito` (Authenticated): imposta cinema preferito
6. Aggiornare `ProfiloUpdateDTO`: aggiungere `CinemaPreferitoId`
7. Creare `DTO/CinemaPreferitoDTO.cs`: CinemaPreferitoDTO (con info cinema)
8. Aggiornare endpoint cinema per geolocazione:
   - `GET /cinemas/nearby?lat=&lng=` (pubblico): cinema ordinati per distanza dal punto dato
   - Implementare formula di Haversine nel servizio
9. Aggiornare DTO Cinema per includere le tipologie di sale disponibili.
10. Aggiornare test di integrazione.

**Verifica fase**:

- Film detail con descrizione, cast, show per data
- Film con tag InEvidenza/InUscita calcolati
- Cinema preferito salvabile e recuperabile
- Cinema ordinabili per prossimita geografica
- Test verdi

**Checklist fase**:

- [ ] FilmDTO aggiornato con Descrizione, Cast, InEvidenza, InUscita
- [ ] FilmDetailDTO creato con show raggruppati per data
- [ ] Endpoint `GET /films/{id}/detail` funzionante
- [ ] CinemaPreferito salvabile e recuperabile da profilo
- [ ] Endpoint cinema nearby con ordinamento distanza funzionante
- [ ] CinemaDTO include tipologie sale
- [ ] Test integrazione aggiunti e verdi

---

### FASE 5 - Backend: Seat Hold + Acquisto + Biglietto + Pagamento

**Obiettivo**: implementare il flusso completo di selezione posti, hold temporaneo, acquisto, generazione biglietti e pagamento (Stripe + credito).

**Attivita**:

1. Creare DTO:
   - `DTO/SeatHoldDTO.cs`: SeatHoldRequestDTO (ShowId, lista PostoIds), SeatHoldResponseDTO (holdId, expiresAt, heldPosti), SeatHoldStatusDTO
   - `DTO/AcquistoDTO.cs`: AcquistoCreateDTO, AcquistoDTO, AcquistoDetailDTO (con biglietti)
   - `DTO/BigliettoDTO.cs`: BigliettoDTO, BigliettoDetailDTO (con info show/posto/cinema)
   - `DTO/PagamentoDTO.cs`: PaymentIntentDTO (clientSecret, importo), ConfermaPagamentoDTO, CreditoDTO, RicaricaCreditoCreateDTO, RicaricaCreditoDTO
2. Creare `Services/ISeatHoldService.cs` + `Services/SeatHoldService.cs`:
   - `HoldSeatsAsync(showId, userId, postoIds)`: crea hold con scadenza, lazy cleanup, validazione posti
   - `ReleaseHoldAsync(holdId, userId)`: rilascia hold (solo proprietario)
   - `ReleaseExpiredHoldsAsync()`: cleanup hold scaduti (per background service)
   - `ValidateHoldAsync(holdId, userId)`: verifica hold ancora attivo
   - `GetHeldSeatsForShowAsync(showId)`: posti attualmente in hold per uno show
3. Creare `BackgroundService/SeatHoldCleanupService.cs`:
   - Esegue ogni 30 secondi
   - Chiama `ReleaseExpiredHoldsAsync()`
4. Creare `Services/ICreditoService.cs` + `Services/CreditoService.cs`:
   - `GetCreditoAsync(userId)`: restituisce saldo
   - `DeductCreditoAsync(userId, importo)`: sottrae importo (con validazione saldo)
   - `AddCreditoAsync(userId, importo)`: aggiunge importo
5. Creare `Services/IRicaricaCreditoService.cs` + `Services/RicaricaCreditoService.cs`:
   - `RicaricaAsync(userId, operatoreId, importo, note)`: registra ricarica e aggiorna credito
   - `GetStoricoRicaricheAsync(filters)`: storico ricariche
6. Creare `Services/IAcquistoService.cs` + `Services/AcquistoService.cs`:
   - `CreateAcquistoAsync(userId, showId, postoIds, metodoPagamento, creditoUsato)`:
     - Verifica SeatHold attivi
     - Se creditoUsato > 0: verifica e sottrai da CreditoUtente
     - Se rimanente > 0: crea Stripe PaymentIntent
     - Crea Acquisto (stato "in_attesa" o "completato" se solo credito)
     - Se "completato": crea Biglietti, rimuovi SeatHold
   - `ConfirmStripePaymentAsync(acquistoId, paymentIntentId)`: verifica pagamento Stripe e finalizza
   - `GetAcquistiAsync(userId)`: lista acquisti utente
   - `GetAcquistoDetailAsync(acquistoId, userId)`: dettaglio con biglietti (ownership check)
   - `GetBigliettiAsync(userId)`: lista biglietti utente
   - `GetBigliettoDetailAsync(bigliettoId)`: dettaglio biglietto (per validazione)
7. Creare `Services/IPaymentService.cs` + `Services/PaymentService.cs`:
   - `CreatePaymentIntentAsync(importo, metadata)`: Stripe PaymentIntent
   - `VerifyPaymentAsync(paymentIntentId)`: verifica stato pagamento
   - `HandleWebhookAsync(payload, signature)`: gestisce webhook Stripe
8. Creare `Endpoints/SeatHoldEndpoints.cs`:
   - `POST /shows/{showId}/seats/hold` (Authenticated): hold posti
   - `DELETE /seats-hold/{holdId}` (Authenticated): rilascia hold
   - `GET /shows/{showId}/seats-hold` (Authenticated, admin): lista hold per show
9. Creare `Endpoints/AcquistoEndpoints.cs`:
   - `POST /acquisti` (Authenticated): crea acquisto
   - `POST /acquisti/{id}/confirm-stripe` (Authenticated): conferma pagamento Stripe
   - `GET /acquisti` (Authenticated): lista propri acquisti (Admin: tutti)
   - `GET /acquisti/{id}` (Authenticated): dettaglio acquisto (ownership check)
   - `GET /biglietti` (Authenticated): lista propri biglietti
   - `GET /biglietti/{id}` (Authenticated): dettaglio biglietto
10. Creare `Endpoints/PagamentoEndpoints.cs`:
    - `POST /pagamento/create-intent` (Authenticated): crea Stripe PaymentIntent
    - `POST /pagamento/stripe-webhook` (AllowAnonymous, firma verificata): webhook Stripe
11. Creare `Endpoints/CreditoEndpoints.cs`:
    - `GET /credito` (Authenticated): saldo credito proprio
    - `POST /credito/ricarica` (PowerUserOrAdmin): ricarica credito utente
    - `GET /credito/ricariche` (PowerUserOrAdmin): storico ricariche
12. Configurare Stripe in `Program.cs` (legge STRIPE_SECRET_KEY da env).
13. Registrare tutti i servizi e mapping in `Program.cs`.
14. Aggiungere test di integrazione per:
    - SeatHold (creazione, conflitto, scadenza)
    - Acquisto (creazione, pagamento credito, pagamento misto)
    - Credito (ricarica, saldo, validazione)

**Verifica fase**:

- Hold posti funzionante con protezione race condition
- Acquisto con credito funzionante
- Stripe PaymentIntent creato correttamente
- Pagamento misto (credito + carta) funzionante
- Cleanup hold scaduti automatico
- Test verdi

**Checklist fase**:

- [ ] DTO SeatHold, Acquisto, Biglietto, Pagamento, Credito creati
- [ ] `ISeatHoldService`/`SeatHoldService` implementati con hold/release/cleanup
- [ ] `SeatHoldCleanupService` background service attivo
- [ ] `ICreditoService`/`CreditoService` implementati
- [ ] `IRicaricaCreditoService`/`RicaricaCreditoService` implementati
- [ ] `IAcquistoService`/`AcquistoService` implementati con flusso completo
- [ ] `IPaymentService`/`PaymentService` con Stripe integrato
- [ ] Endpoints SeatHold, Acquisto, Pagamento, Credito mappati
- [ ] Configurazione Stripe in Program.cs
- [ ] Test integrazione SeatHold, Acquisto, Credito verdi

---

### FASE 6 - Backend: PDF Biglietto + Email + Validazione Biglietto

**Obiettivo**: implementare generazione PDF del biglietto, invio email e validazione biglietto al cinema.

**Attivita**:

1. Installare e configurare QuestPDF (licenza Community).
2. Creare `Services/IPdfService.cs` + `Services/PdfService.cs`:
   - `GeneraBigliettoPdfAsync(bigliettoId)`: genera PDF con:
     - Titolo film, data/ora, sala, settore, fila, posto
     - Nome cinema, codice locale, indirizzo, citta
     - Tipo evento, organizzatore (da env)
     - Prezzo, supplemento, totale
     - Codice a barre (Code128) come immagine
     - Codice biglietto in chiaro
     - QR code con URL di validazione
   - `GeneraBigliettiPdfAsync(acquistoId)`: PDF multi-pagina (un biglietto per pagina)
3. Creare `Services/IQrCodeService.cs` + `Services/QrCodeService.cs`:
   - `GeneraQrCodeAsync(testo)`: genera immagine QR code
   - `GeneraBarcodeAsync(codice)`: genera immagine codice a barre Code128
4. Creare `Services/IEmailService.cs` + `Services/EmailService.cs`:
   - `InviaBigliettiAsync(email, acquistoId)`: invia email con PDF allegato
   - Configurazione SMTP da env
   - Template email HTML con riepilogo acquisto
5. Aggiornare `AcquistoService`:
   - Dopo creazione biglietti: chiamare `IPdfService.GeneraBigliettiPdfAsync` e `IEmailService.InviaBigliettiAsync`
6. Creare `Services/IValidazioneService.cs` + `Services/ValidazioneService.cs`:
   - `ValidaBigliettoAsync(codiceBiglietto, cinemaIdOperatore)`:
     - Cerca biglietto per CodiceBiglietto
     - Verifica: esistente, non gia validato, show appartenente al cinema dell'operatore
     - Se OK: marca come validato con DataValidazione = now
     - Restituisce esito con dettagli
   - `GetInfoBigliettoAsync(codiceBiglietto)`: info biglietto per preview prima di validazione
7. Creare `Endpoints/ValidazioneEndpoints.cs`:
   - `POST /validazione/valida` (PowerUserOrAdmin): valida biglietto
   - `GET /validazione/info?codice=` (PowerUserOrAdmin): info biglietto per codice
8. Configurare MailKit in `Program.cs`.
9. Registrare servizi e mapping in `Program.cs`.
10. Aggiungere test di integrazione per validazione biglietto.
11. Test manuale generazione PDF e invio email.

**Verifica fase**:

- PDF biglietto generato correttamente con QR code e barcode
- Email inviata con PDF allegato
- Validazione biglietto funzionante (con controllo cinema operatore)
- Biglietto gia validato -> errore
- Biglietto cinema sbagliato -> errore
- Test verdi

**Checklist fase**:

- [ ] `IPdfService`/`PdfService` implementati con QuestPDF
- [ ] `IQrCodeService`/`QrCodeService` implementati con QRCoder
- [ ] `IEmailService`/`EmailService` implementati con MailKit
- [ ] `IValidazioneService`/`ValidazioneService` implementati con controllo cinema
- [ ] `ValidazioneEndpoints` mappati con policy PowerUserOrAdmin
- [ ] Email invio automatico dopo acquisto
- [ ] PDF generato con QR code, barcode, dati biglietto
- [ ] Configurazione SMTP in Program.cs
- [ ] Test integrazione validazione aggiunti e verdi

---

### FASE 7 - Frontend: Redesign Programmazione + Selettore Cinema

**Obiettivo**: ridisegnare la pagina programmazione come pagina film-centric con tag, filtri e selettore cinema.

**Attivita**:

1. Aggiornare `js/api.js` con nuovi metodi:
   - `getFilmDetail(id)`, `getFilmsWithShowInfo(cinemaId)`, `getCinemaPreferito()`, `setCinemaPreferito(cinemaId)`, `getCinemasNearby(lat, lng)`, `getShow(id)`, `getShowsByFilm(filmId, cinemaId, data)`, `getShowsByCinema(cinemaId, data)`
2. Riscrivere `programmazione.html`:
   - Layout simile a ucicinemas.it/film
   - **Barra superiore**: selettore cinema (bottone che apre modale) + barra ricerca titolo + filtro categoria
   - **Sezione tag**: "In evidenza" / "In uscita" / "Tutti i film" (tab switch)
   - **Griglia film**: card film con immagine, titolo, categorie, badge "Disponibile nel tuo cinema" / "Non disponibile"
   - Card cliccabile -> `scheda-film.html?id={filmId}`
3. Riscrivere `js/pages/programmazione.js`:
   - Caricamento film con show-info per cinema selezionato
   - Logica tag: InEvidenza, InUscita, Tutti
   - Barra ricerca: filtro client-side per titolo
   - Filtro categoria: dropdown categorie
   - **Modale selettore cinema**:
     - Elenco cinema con nome, citta, indirizzo
     - Se browser supporta geolocation: ordinamento per distanza
     - Click su cinema -> seleziona e chiudi modale
     - Salvataggio: se loggato -> API profilo; se anonimo -> localStorage (`cb_cinema_preferito`)
     - Coerenza: se loggato, cinema da API; se anonimo, da localStorage
   - Badge disponibilita: per ogni film, verifica se ci sono show nel cinema selezionato
   - Cinema selezionato evidenziato in pagina (nome + citta)
4. Aggiornare `route-guard.js`: `programmazione.html` resta pubblico.
5. Aggiornare `template-loader.js`: nessuna modifica (gia landing).
6. Aggiornare `navbar-landing.html`: aggiungere link "I Nostri Cinema" -> `/my-cinemas.html`.
7. Aggiornare `index.html` / `js/pages/home.js`: link programmazione punta a nuova pagina.

**Verifica fase**:

- Pagina programmazione mostra film (non proiezioni)
- Tag In evidenza/In uscita/Tutti funzionanti
- Barra ricerca per titolo funzionante
- Filtro categoria funzionante
- Modale selettore cinema con geolocazione
- Cinema preferito salvato e recuperato correttamente
- Badge disponibilita per cinema selezionato
- Anonimo può usare la pagina e salvare cinema in localStorage

**Checklist fase**:

- [ ] `api.js` aggiornato con nuovi metodi
- [ ] `programmazione.html` ridisegnata con layout film-centric
- [ ] Tag In evidenza/In uscita/Tutti funzionanti
- [ ] Barra ricerca e filtro categoria funzionanti
- [ ] Modale selettore cinema con geolocazione
- [ ] Salvataggio cinema preferito (profilo se loggato, localStorage se anonimo)
- [ ] Badge disponibilita film nel cinema selezionato
- [ ] Navbar aggiornata con link "I Nostri Cinema"
- [ ] Home page link aggiornati

---

### FASE 8 - Frontend: Scheda Film + My Cinemas

**Obiettivo**: implementare la pagina di dettaglio film e la pagina elenco cinema.

**Attivita**:

1. Creare `scheda-film.html`:
   - **Sezione info film**: immagine copertina, titolo, durata, data rilascio, genere/categorie, descrizione, regista, cast
   - **Pulsante "Vai agli show"**: apre sezione show sotto le info
   - **Sezione show**:
     - Barra date orizzontale scrollabile (oggi, lun 13 apr, mar 14 apr, ...) con frecce sinistra/destra
     - Per la data selezionata: nome cinema + citta + indirizzo
     - Per ogni sala (raggruppata per tipologia): bottoni orari (es. 16:00, 18:30, 21:00)
     - Bottoni orario: se utente loggato -> `acquista.html?idCinema=...&idFilm=...&idSala=...&idShow=...`; se anonimo -> `login.html?redirect=...`
2. Creare `js/pages/scheda-film.js`:
   - Caricamento film detail da API
   - Rendering sezione info
   - Logica barra date: generazione date (da oggi a +14 giorni), scroll orizzontale, frecce
   - Caricamento show per film + data + cinema selezionato
   - Raggruppamento show per tipologia sala
   - Rendering bottoni orario per tipologia
   - Auth-aware: redirect a login con callback se non autenticato
3. Creare `my-cinemas.html`:
   - **Elenco cinema**: card con nome, citta, indirizzo, tipologie sale (badge)
   - Click su cinema -> `my-cinemas.html?idCinema={id}` (vista dettaglio)
   - **Vista dettaglio cinema**:
     - Barra date orizzontale scrollabile (come scheda-film)
     - Per data selezionata: per ogni film in programma, card con immagine, titolo, descrizione parziale, elenco tipologie sale con bottoni orari
   - Bottoni orario: stesso comportamento auth-aware di scheda-film
4. Creare `js/pages/my-cinemas.js`:
   - Caricamento lista cinema
   - Caricamento programmazione cinema per data
   - Rendering card cinema e dettaglio
   - Logica barra date e show
5. Aggiornare `route-guard.js`: aggiungere `/scheda-film.html` (pubblico) e `/my-cinemas.html` (pubblico).
6. Aggiornare `template-loader.js`: aggiungere `/scheda-film.html` e `/my-cinemas.html` ai landing paths.

**Verifica fase**:

- Scheda film mostra info complete (descrizione, cast, regista)
- Sezione show con barra date scrollabile funzionante
- Bottoni orario raggruppati per tipologia sala
- Bottoni orario reindirizzano correttamente (login se anonimo, acquista se loggato)
- My Cinemas mostra elenco cinema e programmazione per data
- Entrambe le pagine accessibili ad anonimi

**Checklist fase**:

- [ ] `scheda-film.html` creata con sezione info + show
- [ ] `scheda-film.js` con barra date, raggruppamento tipologia sala, auth-aware
- [ ] `my-cinemas.html` creata con elenco cinema e dettaglio
- [ ] `my-cinemas.js` con programmazione cinema per data
- [ ] Route-guard aggiornato per nuove pagine
- [ ] Template-loader aggiornato per nuove pagine

---

### FASE 9 - Frontend: Acquisto + Pagamento

**Obiettivo**: implementare le pagine di selezione posti, acquisto e pagamento.

**Attivita**:

1. Creare `acquista.html`:
   - **Card riepilogo**: titolo film, tipologia sala, orario inizio, data estesa, nome cinema, numero biglietti, totale prezzo
   - **Piantina sala**: griglia di bottoni disposti su righe (file) che rappresentano la piantina
     - Ogni bottone = un posto (identificato da fila + numero)
     - Posti occupati (biglietto venduto): colore rosso, non cliccabili
     - Posti in hold da altro utente: colore arancione, non cliccabili
     - Posti disponibili: colore verde, cliccabili
     - Posti selezionati dall'utente: colore blu, cliccabili per deselezionare
     - Max 10 posti selezionabili
   - **Countdown timer**: visibile, mostra tempo rimanente per il hold
   - **Elenco posti selezionati**: "Fila 7, Posto 5", "Fila 8, Posto 3", etc.
   - **Bottone "Continua"**: visibile solo se almeno 1 posto selezionato, porta a pagamento
2. Creare `js/pages/acquista.js`:
   - Lettura parametri URL: `idShow`
   - Caricamento show + sala + posti + disponibilita
   - Rendering piantina sala interattiva
   - Selezione/deselezione posti (max 10)
   - Chiamata `POST /shows/{id}/seats/hold` alla prima selezione
   - Aggiornamento hold quando si aggiunge/rimuove un posto
   - Countdown timer: avvia al primo hold, avvisa a scadenza
   - Bottone "Continua": redirect a `pagamento.html?idShow=...&holdId=...`
   - Se utente non loggato: redirect a login con callback
3. Creare `pagamento.html`:
   - **Riepilogo ordine**: film, sala, data, ora, posti, importo totale
   - **Saldo credito**: visibile se utente ha credito residuo
   - **Scelta metodo pagamento**:
     - Opzione 1: "Paga con credito piattaforma" (disabilitata se credito insufficiente)
     - Opzione 2: "Paga con carta di credito"
     - Opzione 3: "Pagamento misto" (se credito insufficiente per tutto):
       - Slider/input per importo da pagare con credito
       - Rimanente con carta
   - **Form carta di credito**: Stripe Elements (integrato con Stripe.js)
   - **Bottone "Paga"**: invia pagamento
4. Creare `js/pages/pagamento.js`:
   - Caricamento info show + hold + credito utente
   - Calcolo importi (totale, credito usabile, rimanente)
   - Integrazione Stripe.js (carica da CDN con STRIPE_PUBLISHABLE_KEY)
   - Flusso pagamento:
     - Se solo credito: chiama direttamente API acquisto
     - Se carta: crea PaymentIntent, conferma con Stripe, finalizza
     - Se misto: sottrae credito, paga rimanente con Stripe
   - Redirect a `esito-acquisto.html?idAcquisto=...` dopo successo
5. Creare `esito-acquisto.html`:
   - Esito pagamento (successo/fallimento)
   - Riepilogo biglietti acquistati
   - Messaggio "Biglietti inviati via email"
   - Bottone "Vai ai miei biglietti" -> profilo
   - Bottone "Torna alla programmazione"
6. Creare `js/pages/esito-acquisto.js`:
   - Caricamento dettaglio acquisto
   - Rendering esito e biglietti
7. Aggiornare `route-guard.js`: aggiungere `/acquista.html`, `/pagamento.html`, `/esito-acquisto.html` (Authenticated).
8. Aggiornare `template-loader.js`: aggiungere le nuove pagine ai landing paths.
9. Aggiornare `api.js` con metodi:
   - `holdSeats(showId, postoIds)`, `releaseHold(holdId)`, `getShowAvailability(showId)`
   - `createAcquisto(data)`, `confirmStripePayment(acquistoId, paymentIntentId)`
   - `getCredito()`, `ricaricaCredito(data)`

**Verifica fase**:

- Piantina sala interattiva con posti occupati/disponibili/selezionati
- Hold posti con countdown funzionante
- Pagamento con credito funzionante
- Pagamento con Stripe funzionante (test con chiave test)
- Pagamento misto funzionante
- Esito acquisto mostrato correttamente
- Redirect login per anonimi con callback

**Checklist fase**:

- [ ] `acquista.html` con piantina sala interattiva e countdown
- [ ] `acquista.js` con hold, selezione, timer
- [ ] `pagamento.html` con scelta metodo (credito/carta/misto)
- [ ] `pagamento.js` con Stripe.js e flusso pagamento completo
- [ ] `esito-acquisto.html` con esito e riepilogo biglietti
- [ ] `esito-acquisto.js` implementato
- [ ] Route-guard e template-loader aggiornati
- [ ] `api.js` aggiornato con nuovi metodi
- [ ] Pagamento Stripe test con chiave di test funzionante

---

### FASE 10 - Frontend: Admin (Sale, Show, Validazione, Credito) + Profilo

**Obiettivo**: completare le pagine amministrative e aggiornare il profilo utente.

**Attivita**:

1. Creare `sale.html` (admin: gestione sale):
   - Tabella sale con colonne: Nome, Cinema, Tipologia, Supplemento, Capienza, Azioni
   - Filtro per cinema
   - Modale crea/modifica sala con:
     - Select cinema, nome, numero, tipologia, supplemento
     - Sezione piantina: definizione griglia (numero file, posti per fila, settori)
     - Anteprima piantina
   - Bottone "Genera piantina" per creare i posti
   - Bottone "Elimina piantina" per rimuovere posti
2. Creare `js/pages/sale.js`:
   - CRUD sale con piantina
   - Validazione form
   - Anteprima piantina
3. Aggiornare `proiezioni.html` -> ridisegnata per gestione show:
   - Rinominare concettualmente in "Gestione Show"
   - Tabella show con colonne: Film, Cinema, Sala, Data, Ora Inizio, Prezzo, Azioni
   - Filtro per cinema, data, film
   - Modale crea/modifica show con:
     - Select film, select cinema (aggiorna sale), select sala, data, ora, prezzo
     - Validazione sovrapposizioni lato client (warning, il backend fa enforcement)
   - Vista batch: possibilità di creare show multipli in un cinema per date diverse
4. Aggiornare `js/pages/proiezioni.js`:
   - Adattato per Show al posto di Proiezione
   - Filtri cinema/sala/data
   - Validazione lato client
5. Creare `valida-biglietto.html` (PowerUser/Admin):
   - **Selettore cinema**: l'operatore deve identificare la sede del cinema dove sta operando
   - **Form validazione manuale**: input per codice biglietto + bottone "Valida"
   - **Validazione da QR code**: se URL contiene `?codice=`, pre-compila e valida automaticamente
   - **Scanner QR/barcode**: accesso camera per scansione (su mobile/tablet)
   - **Risultato validazione**:
     - Successo: biglietto validato, mostra info (film, sala, posto, orario)
     - Errore: gia validato (con data validazione) / cinema sbagliato / codice non trovato
   - **Storico validazioni** della sessione
6. Creare `js/pages/valida-biglietto.js`:
   - Selettore cinema (salvato in sessionStorage per sessione)
   - Validazione manuale e automatica (da URL param)
   - Accesso camera per scanner (se supportato)
   - Storico sessione
7. Creare `ricarica-credito.html` (PowerUser/Admin):
   - Form: input email utente, importo, note
   - Ricerca utente per email
   - Mostra saldo attuale
   - Bottone "Ricaria"
   - Feedback operazione
   - Storico ricariche recenti
8. Creare `js/pages/ricarica-credito.js`:
   - Ricerca utente per email
   - Chiamata API ricarica
   - Storico ricariche
9. Aggiornare `profilo.html`:
   - Rimuovere sezione prenotazioni (sostituita da biglietti)
   - Aggiungere sezione **Cinema Preferito**: visualizzazione e cambio cinema
   - Aggiungere sezione **Credito**: saldo e cronologia
   - Aggiungere sezione **I Miei Biglietti**: elenco biglietti acquistati
     - Per ogni biglietto: film, cinema, sala, data/ora, posto, codice, stato (validato/non validato)
     - Click su biglietto -> dettaglio con codice QR visibile
   - Aggiungere sezione **Storico Acquisti**: elenco acquisti con riepilogo
10. Aggiornare `js/pages/profilo.js`:
    - Rimuovere logica prenotazioni
    - Aggiungere cinema preferito, credito, biglietti, storico acquisti
11. Aggiornare `route-guard.js`:
    - Aggiungere `/sale.html` (poweruser/admin)
    - Aggiungere `/valida-biglietto.html` (poweruser/admin)
    - Aggiungere `/ricarica-credito.html` (poweruser/admin)
12. Aggiornare `template-loader.js`:
    - `/valida-biglietto.html` e `/ricarica-credito.html` come admin pages
    - `/sale.html` come admin page
13. Aggiornare `navbar-admin.html`:
    - Aggiungere link "Sale" e "Valida Biglietto" e "Ricarica Credito"
14. Aggiornare `api.js` con metodi:
    - CRUD sale, show
    - `validaBiglietto(codice, cinemaId)`, `getInfoBiglietto(codice)`
    - `ricaricaCredito(userId, importo, note)`, `getStoricoRicariche()`
    - `getBiglietti()`, `getAcquisti()`, `getCredito()`

**Verifica fase**:

- CRUD sale con piantina funzionante
- Gestione show (ex-proiezioni) aggiornata con sale
- Validazione biglietto funzionante (manuale e da QR)
- Ricarica credito funzionante
- Profilo aggiornato con cinema preferito, credito, biglietti
- Tutte le nuove pagine accessibili solo ai ruoli corretti

**Checklist fase**:

- [ ] `sale.html`/`sale.js` con CRUD e piantina
- [ ] `proiezioni.html`/`proiezioni.js` aggiornati per show con sale
- [ ] `valida-biglietto.html`/`valida-biglietto.js` con validazione manuale e QR
- [ ] `ricarica-credito.html`/`ricarica-credito.js` con ricerca utente e ricarica
- [ ] `profilo.html`/`profilo.js` aggiornati con cinema preferito, credito, biglietti
- [ ] Route-guard, template-loader, navbar aggiornati
- [ ] `api.js` aggiornato con tutti i nuovi metodi

---

### FASE 11 - Test e2e + Verifica Finale

**Obiettivo**: chiudere iterazione con qualita verificata e documentazione aggiornata.

**Attivita**:

1. Eseguire test backend completi (`dotnet test tests/backend/FilmAPI.Tests.csproj`).
2. Verifica manuale completa per ruolo:
   - **Anonimo**: index, programmazione, my-cinemas, scheda-film (sola lettura); click acquisto -> login
   - **User**: programmazione, acquisto biglietti, pagamento, profilo con biglietti e credito
   - **PowerUser**: CRUD film/show/sale/categorie, validazione biglietti, ricarica credito
   - **Admin**: tutto + CRUD cinemas + gestione utenti
3. Verifica flussi end-to-end:
   - Anonimo -> login -> acquisto biglietto -> pagamento -> email
   - PowerUser -> validazione biglietto via QR code
   - PowerUser -> ricarica credito utente
   - User -> pagamento misto (credito + carta)
   - Race condition: due utenti selezionano stesso posto
4. Verifica redirect URL diretti non autorizzati.
5. Verifica PDF biglietto (contenuto, QR code, barcode).
6. Verifica email con PDF allegato.
7. Aggiornare `docs/project/status.md`.
8. Aggiornare `docs/project/changelog.md`.
9. Aggiornare tabella Stato Avanzamento Fasi.

**Verifica fase**:

- Test verdi
- RBAC e redirect coerenti
- Flussi e2e completati
- Documentazione aggiornata

**Checklist fase**:

- [ ] Suite test backend completa e verde
- [ ] Verifica manuale completata per Admin/PowerUser/User/Anonimo
- [ ] Flusso acquisto biglietto e2e verificato
- [ ] Flusso validazione biglietto e2e verificato
- [ ] Flusso ricarica credito verificato
- [ ] Race condition posti verificata
- [ ] PDF e email verificati
- [ ] Redirect su URL diretti non autorizzati verificati
- [ ] `docs/project/status.md` aggiornato
- [ ] `docs/project/changelog.md` aggiornato
- [ ] Tabella Stato Avanzamento Fasi aggiornata

---

## 5) Nuovi File Previsti

## 5.1 Backend (`backend/FilmAPI/`)

**Modelli**:
- `Model/TipologiaSala.cs`
- `Model/Sala.cs`
- `Model/Posto.cs`
- `Model/Show.cs` (sostituisce Proiezione)
- `Model/SeatHold.cs`
- `Model/Acquisto.cs`
- `Model/Biglietto.cs`
- `Model/CreditoUtente.cs`
- `Model/RicaricaCredito.cs`

**DTO**:
- `DTO/SalaDTO.cs`
- `DTO/ShowDTO.cs` (sostituisce ProiezioneDTO)
- `DTO/SeatHoldDTO.cs`
- `DTO/AcquistoDTO.cs`
- `DTO/BigliettoDTO.cs`
- `DTO/PagamentoDTO.cs`
- `DTO/CreditoDTO.cs`
- `DTO/ValidazioneDTO.cs`
- `DTO/CinemaPreferitoDTO.cs`

**Servizi**:
- `Services/ISalaService.cs` + `Services/SalaService.cs`
- `Services/IShowService.cs` + `Services/ShowService.cs`
- `Services/ISeatHoldService.cs` + `Services/SeatHoldService.cs`
- `Services/IAcquistoService.cs` + `Services/AcquistoService.cs`
- `Services/IPaymentService.cs` + `Services/PaymentService.cs`
- `Services/ICreditoService.cs` + `Services/CreditoService.cs`
- `Services/IRicaricaCreditoService.cs` + `Services/RicaricaCreditoService.cs`
- `Services/IValidazioneService.cs` + `Services/ValidazioneService.cs`
- `Services/IPdfService.cs` + `Services/PdfService.cs`
- `Services/IQrCodeService.cs` + `Services/QrCodeService.cs`
- `Services/IEmailService.cs` + `Services/EmailService.cs`

**Endpoint**:
- `Endpoints/SaleEndpoints.cs`
- `Endpoints/ShowEndpoints.cs`
- `Endpoints/SeatHoldEndpoints.cs`
- `Endpoints/AcquistoEndpoints.cs`
- `Endpoints/PagamentoEndpoints.cs`
- `Endpoints/CreditoEndpoints.cs`
- `Endpoints/ValidazioneEndpoints.cs`

**Background Services**:
- `BackgroundService/SeatHoldCleanupService.cs`

**File rimossi**:
- `Model/Proiezione.cs`
- `Model/Prenotazione.cs`
- `DTO/ProiezioneDTO.cs`
- `Services/IProiezioneService.cs` + `ProiezioneService.cs`
- `Services/IPrenotazioneService.cs` + `PrenotazioneService.cs`
- `Endpoints/ProiezioniEndpoints.cs`
- `Endpoints/PrenotazioniEndpoints.cs`

## 5.2 Frontend (`frontend/CineBase.Web/wwwroot/`)

**Nuove pagine**:
- `scheda-film.html`
- `my-cinemas.html`
- `acquista.html`
- `pagamento.html`
- `esito-acquisto.html`
- `sale.html`
- `valida-biglietto.html`
- `ricarica-credito.html`

**Nuovi JS**:
- `js/pages/scheda-film.js`
- `js/pages/my-cinemas.js`
- `js/pages/acquista.js`
- `js/pages/pagamento.js`
- `js/pages/esito-acquisto.js`
- `js/pages/sale.js`
- `js/pages/valida-biglietto.js`
- `js/pages/ricarica-credito.js`

**File aggiornati**:
- `js/api.js` (nuovi metodi)
- `js/route-guard.js` (nuove pagine e permessi)
- `js/template-loader.js` (nuovi landing/admin paths)
- `programmazione.html` + `js/pages/programmazione.js` (ridisegno completo)
- `proiezioni.html` + `js/pages/proiezioni.js` (aggiornamento per show)
- `profilo.html` + `js/pages/profilo.js` (biglietti, credito, cinema preferito)
- `index.html` + `js/pages/home.js` (link aggiornati)
- `components/navbar-landing.html` (link "I Nostri Cinema")
- `components/navbar-admin.html` (link Sale, Valida Biglietto, Ricarica Credito)
- `films.html` + `js/pages/films.js` (aggiungere Descrizione, Cast)
- `cinemas.html` + `js/pages/cinemas.js` (aggiungere lat/lng, telefono, codice locale)

---

## 6) Criteri di Accettazione

L'iterazione e completata quando tutte le seguenti condizioni sono vere:

1. ogni cinema puo avere N sale con tipologia diversa (2D, 3D, ISENSE, XL)
2. la pagina programmazione mostra film (non proiezioni) con tag In Evidenza/In Uscita/Tutti
3. l'utente puo selezionare il cinema preferito (profilo se loggato, localStorage se anonimo) con ordinamento per prossimita
4. ogni film card indica se il film e disponibile nel cinema selezionato
5. la scheda film mostra descrizione, cast, show per data con orari raggruppati per tipologia sala
6. la pagina my-cinemas mostra l'elenco cinema con programmazione per data
7. la pagina acquista mostra la piantina sala interattiva con posti occupati/disponibili/selezionati
8. la selezione posti e protetta da race condition (seat hold con scadenza)
9. l'utente puo pagare con carta di credito (Stripe), credito piattaforma o pagamento misto
10. il PowerUser/Admin puo ricaricare il credito di un utente, registrando operatore e data
11. dopo l'acquisto, l'utente riceve un'email con PDF allegato contenente i biglietti
12. il PDF contiene QR code, barcode, dati biglietto, dati cinema, prezzo
13. il QR code codifica un URL che permette la validazione automatica del biglietto
14. il PowerUser/Admin puo validare il biglietto (manualmente o via QR) con controllo che il biglietto appartenga al suo cinema
15. il biglietto validato riporta data e ora di vidimazione
16. gli show non si sovrappongono nella stessa sala (validazione backend)
17. la gestione admin delle proiezioni e aggiornata per supportare sale e show
18. esiste una pagina admin per la gestione delle sale con piantina posti
19. il profilo utente mostra biglietti acquistati, credito e cinema preferito
20. utenti non autenticati vengono rediretti al login con callback quando tentano azioni riservate
21. la suite backend e totalmente verde

---

## 7) Prompt Guida (per esecuzione fase-by-fase)

Regola comune per **tutti** i prompt fase:

- implementare solo la fase richiesta
- al termine aggiornare la tabella `Stato Avanzamento Fasi`:
  - `Stato`: `Completata` (oppure `In corso` / `Bloccata`)
  - `Data`: data corrente
  - `Note`: breve esito (test, blocchi, deviazioni)
- spuntare la `Checklist fase` relativa con `[x]` sugli item completati
- se restano attivita parziali, lasciare check non spuntati e indicare motivo nelle note

**Fase 1**
"Implementa Fase 1 del piano di iterazione 4: crea tutte le nuove entita (TipologiaSala, Sala, Posto, Show, SeatHold, Acquisto, Biglietto, CreditoUtente, RicaricaCredito); aggiorna Film (Descrizione, Cast), Cinema (Latitudine, Longitudine, Telefono, CodiceLocale, Sale), User (CinemaPreferitoId); rimuovi Proiezione e Prenotazione; aggiorna FilmDbContext; rimuovi vecchi DTO/servizi/endpoint Proiezione e Prenotazione; aggiorna .env; crea DTO placeholder per Sale e Show; aggiorna test esistenti; crea migration AddMultisalaAndTickets; verifica compilazione e test superstiti verdi. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 1."

**Fase 2**
"Implementa Fase 2 del piano: crea ISalaService/SalaService con CRUD e piantina posti; crea SalaDTO; crea SaleEndpoints con policy RBAC; aggiorna CinemaDTO/CinemaService per includere sale; aggiungi test integrazione. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 2."

**Fase 3**
"Implementa Fase 3 del piano: crea IShowService/ShowService con CRUD e validazione sovrapposizioni temporali (OraInizio >= OraInizio show precedente + durata film precedente nella stessa sala); crea ShowDTO; crea ShowEndpoints con policy RBAC; aggiungi endpoint query per cinema/data e per film; aggiorna FilmService e DataSeeder; aggiungi test integrazione. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 3."

**Fase 4**
"Implementa Fase 4 del piano: aggiungi Descrizione e Cast a Film/FilmDTO; crea FilmDetailDTO con show raggruppati per data; aggiungi calcolo InEvidenza e InUscita; crea endpoint GET /films/{id}/detail; implementa cinema preferito (GET/PUT /profilo/cinema-preferito); implementa endpoint cinema nearby con Haversine; aggiorna CinemaDTO con tipologie sale; aggiungi test. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 4."

**Fase 5**
"Implementa Fase 5 del piano: crea ISeatHoldService con hold/release/cleanup e SeatHoldCleanupService background; crea ICreditoService e IRicaricaCreditoService; crea IAcquistoService con flusso completo (hold -> pagamento -> biglietti); crea IPaymentService con Stripe PaymentIntent e webhook; crea tutti gli endpoint (SeatHold, Acquisto, Pagamento, Credito); configura Stripe; aggiungi test integrazione. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 5."

**Fase 6**
"Implementa Fase 6 del piano: crea IPdfService con QuestPDF per generazione PDF biglietto (con QR code, barcode, dati); crea IQrCodeService con QRCoder; crea IEmailService con MailKit per invio email con PDF; crea IValidazioneService con controllo cinema operatore; crea ValidazioneEndpoints; aggiorna AcquistoService per generazione PDF e email automatiche; configura SMTP; aggiungi test. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 6."

**Fase 7**
"Implementa Fase 7 del piano: ridisegna programmazione.html come pagina film-centric con tag (In evidenza/In uscita/Tutti), barra ricerca titolo, filtro categoria; implementa modale selettore cinema con geolocazione e salvataggio (profilo o localStorage); implementa badge disponibilita film nel cinema selezionato; aggiorna api.js, route-guard, navbar, home. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 7."

**Fase 8**
"Implementa Fase 8 del piano: crea scheda-film.html con dettaglio film e sezione show (barra date orizzontale, raggruppamento per tipologia sala, bottoni orario auth-aware); crea my-cinemas.html con elenco cinema e programmazione per data; aggiorna route-guard e template-loader. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 8."

**Fase 9**
"Implementa Fase 9 del piano: crea acquista.html con piantina sala interattiva, countdown hold, selezione posti (max 10); crea pagamento.html con scelta metodo (credito/carta/misto) e Stripe.js; crea esito-acquisto.html; aggiorna route-guard, template-loader, api.js. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 9."

**Fase 10**
"Implementa Fase 10 del piano: crea sale.html con CRUD sale e piantina; aggiorna proiezioni.html per gestione show con sale; crea valida-biglietto.html con validazione manuale e QR; crea ricarica-credito.html con ricerca utente e ricarica; aggiorna profilo.html con cinema preferito, credito, biglietti; aggiorna route-guard, template-loader, navbar-admin, api.js. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 10."

**Fase 11**
"Implementa Fase 11 del piano: esegui verifica finale con test verdi; verifica manuale completa per tutti i ruoli; verifica flussi e2e (acquisto, validazione, ricarica credito, race condition); verifica PDF e email; verifica redirect; aggiorna status.md e changelog.md. A fine fase aggiorna tabella Stato Avanzamento Fasi e Checklist fase 11."
