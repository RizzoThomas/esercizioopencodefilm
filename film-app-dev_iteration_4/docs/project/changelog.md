# Changelog Progetto

## 2026-04-19

### Iterazione 4 - Fase 11.1: migrazione da Stripe Elements a Stripe Checkout hosted

#### Added
- `backend/FilmAPI/Model/OrdineState.cs`: aggiunto stato `CheckoutInProgress = 5` per distinguere il checkout hosted dal pending tradizionale
- `backend/FilmAPI/Model/Ordine.cs`: nuovi campi `StripeCheckoutSessionId`, `CheckoutExpiresAtUtc`, `CheckoutCompletedAtUtc`, `LastPaymentError`, `CreditoRiservato`
- `backend/FilmAPI/Services/StripeGateway.cs`: metodi `CreateCheckoutSessionAsync`, `GetCheckoutSessionAsync`, snapshot `StripeCheckoutSessionSnapshot`, parsing webhook per `checkout.session.*`
- `backend/FilmAPI/Services/IPagamentoService.cs` + `Services/PagamentoService.cs`: `CreateCheckoutSessionAsync`, `GetCheckoutStatusAsync`, `ReconcileCheckoutSessionAsync`, handler webhook `checkout.session.completed`, `checkout.session.expired`, `payment_intent.*` aggiornati per supportare `CheckoutInProgress`
- `backend/FilmAPI/Endpoints/CheckoutEndpoints.cs`: nuovi endpoint `POST /checkout/orders/{orderId}/stripe-checkout-session`, `GET /checkout/orders/{orderId}/checkout-status`, `POST /checkout/orders/{orderId}/reconcile-checkout-session`
- `backend/FilmAPI/DTO/OrdineDTO.cs`: DTO `CreateCheckoutSessionRequestDTO`, `CreateCheckoutSessionResponseDTO`, `CheckoutStatusDTO`
- `backend/FilmAPI/DTO/CheckoutDTO.cs`: `OrdineSummaryDTO` esteso con `StripeCheckoutSessionId`, `CheckoutExpiresAtUtc`, `CheckoutCompletedAtUtc`, `CreditoRiservato`, `LastPaymentError`
- `backend/FilmAPI/Migrations/20260419152609_AddStripeCheckoutFieldsToOrdine.cs`: migration per nuovi campi Ordine
- `frontend/CineBase.Web/wwwroot/js/pages/pagamento.js`: riscritto per flusso hosted — solo credito finalizza direttamente, carta e misto creano sessione Stripe e redirect a pagina hosted, rimossi Stripe Elements embedded
- `frontend/CineBase.Web/wwwroot/js/pages/esito-acquisto.js`: polling stato ordine ogni 3s per `Pending` e `CheckoutInProgress`, header distinti per annullato/scaduto, badge `CheckoutInProgress`, riconciliazione automatica al ritorno da Stripe e fix listener duplicati sul download PDF
- `frontend/CineBase.Web/wwwroot/pagamento.html`: rimosso Stripe Elements card form, semplificato a scelta metodo di pagamento
- `tests/backend/Integration/CheckoutHostedIntegrationTests.cs`: 13 test integrazione per checkout hosted (creazione sessione carta e mista, stato, webhook completed, webhook expired, riconciliazione, duplicati webhook, cancellazione checkout in corso, rilascio credito, finalizzazione senza doppio addebito e regressioni `payment_intent.failed/canceled`)
- `tests/backend/Integration/CustomWebApplicationFactory.cs`: `FakeStripePaymentGateway` esteso con `CreateCheckoutSessionAsync`, `GetCheckoutSessionAsync`, `SetCheckoutSessionStatus`, `CreateCheckoutWebhook`

#### Changed
- `backend/FilmAPI/Services/PagamentoService.cs`: `HandleStripeWebhookAsync` ora gestisce sia `PaymentIntent` che `CheckoutSession` come source of truth, transizioni stato `CheckoutInProgress -> Paid/Cancelled/Expired/Failed`, validazione hold rilassata per `CheckoutInProgress` (nessun TTL check durante checkout hosted), supporto reale al pagamento misto hosted con credito riservato/rilasciato e `CheckoutCompletedAtUtc`, `CancelPendingOrdineAsync` gestisce anche `CheckoutInProgress` con rilascio credito riservato, fix regressioni su `payment_intent.failed/canceled` durante sessione hosted aperta e corretto rilascio posti nel ramo legacy `canceled`
- `backend/FilmAPI/Services/CreditoService.cs`: aggiunti metodi di riserva e rilascio credito per checkout hosted (`ReserveOrderCreditAsync`, `ReleaseReservedOrderCreditAsync`)
- `backend/FilmAPI/Services/ExpiredHoldCleanupService.cs`: oltre agli hold scaduti ora scade automaticamente anche gli ordini hosted `CheckoutInProgress`, rilascia i posti e ripristina il credito riservato
- `backend/FilmAPI/Services/CheckoutService.cs`: `MapToSummary` esteso con nuovi campi checkout
- `frontend/CineBase.Web/wwwroot/js/api.js`, `pagamento.html`, `esito-acquisto.html`: aggiunti metodi hosted e cache-busting querystring sugli asset per evitare runtime con vecchie versioni cached del browser; `CheckoutEndpoints` ora restituisce `400` corretto sui payload hosted invalidi

#### Verified
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **231/231 PASS** (218 esistenti + 13 nuovi)
- Build frontend `CineBase.Web`: **OK**
- verifica manuale reale del flusso Stripe Checkout hosted: **OK** per solo carta e per pagamento misto credito + carta

### Iterazione 4 - Pianificazione Fase 11.1: migrazione da Stripe Elements a Stripe Checkout hosted

#### Added
- `docs/tutorials/TUTORIAL_STRIPE_ELEMENTS_VS_CHECKOUT_CINEBASE.md`: nuovo tutorial in italiano che confronta il flusso attuale `Stripe Elements` con il flusso target `Stripe Checkout`, includendo snippet completi frontend/backend per solo credito, solo carta e pagamento misto

#### Changed
- `docs/project/dev_iteration/4/PianoDiLavoro.md`: aggiunta `FASE 11.1` dedicata alla migrazione verso `Stripe Checkout` hosted
- `docs/project/dev_iteration/4/PianoDiLavoro.md`: fase resa prescrittiva con decisioni già fissate, vincoli implementativi obbligatori, transizioni stato ordine, criteri di accettazione vincolanti e riferimento esplicito al caso solo credito senza Stripe e al caso misto con credito riservato
- `docs/project/status.md`: registrata come pianificata la prossima fase `11.1`, con sintesi dell'ambito e dei vincoli principali del nuovo flusso hosted

#### Notes
- la pianificazione della `FASE 11.1` stabilisce che il redirect di ritorno da Stripe non sia fonte sufficiente di verità, che il backend resti l'unica source of truth economica e che cancel, expire, webhook duplicati e ritardo webhook siano coperti come requisiti di completamento della fase

### Iterazione 4 - Fase 11 refinement: checkout reale, Stripe runtime config e fix prezzi backend

#### Added
- `backend/FilmAPI/Services/TicketPriceNormalizer.cs`: nuovo normalizzatore centralizzato per prezzi ticket/show, usato per gestire correttamente seed e valori espressi in centesimi senza correggere nulla nel frontend
- `backend/FilmAPI/Endpoints/CheckoutEndpoints.cs`: nuovo endpoint `POST /checkout/orders/{orderId}/cancel` per annullare ordini `Pending` e rilasciare i posti bloccati
- `frontend/CineBase.Web/wwwroot/acquista.html`, `pagamento.html`, `esito-acquisto.html`: nuove pagine del flusso acquisto/pagamento/esito
- `frontend/CineBase.Web/wwwroot/js/pages/acquista.js`, `pagamento.js`, `esito-acquisto.js`: logica completa per seat-map, pagamento embedded Stripe ed esito ordine

#### Changed
- `backend/FilmAPI/Program.cs`: corretto il bootstrap `.env` per caricare `backend/.env` dai path reali del repository e aggiunta la configurazione runtime `GET /config/frontend` per la publishable key Stripe
- `backend/FilmAPI/Services/PagamentoService.cs` + `IPagamentoService.cs`: estesi per supportare l'annullamento di ordini `Pending`, il rilascio posti e la finalizzazione coerente del pagamento nel flusso reale
- `backend/FilmAPI/Data/DataSeeder.cs` + `backend/scripts/FilmApiSeeder/Program.cs`: parsing di `DEFAULT_TICKET_PRICE` reso robusto e riallineato ai prezzi decimali attesi dal backend
- `backend/FilmAPI/Services/ShowService.cs`, `SeatHoldService.cs`, `CheckoutService.cs`, `BigliettoService.cs`: allineati a usare prezzi normalizzati lato server come source of truth economica
- `frontend/CineBase.Web/wwwroot/js/pages/pagamento.js`: rimosse fonti duplicate della Stripe publishable key; ora il frontend legge solo `/config/frontend` e richiama il backend dopo `stripe.confirmCardPayment(...)` per chiudere davvero l'ordine
- `frontend/CineBase.Web/wwwroot/js/pages/acquista.js` + `frontend/CineBase.Web/wwwroot/css/styles.css`: seat-map rifinita per desktop con layout piu compatto, sidebar sticky, controlli zoom `+`/`-`/`Reset`, range piu ampio e supporto `Ctrl + wheel` o pinch-trackpad
- `frontend/CineBase.Web/wwwroot/profilo.html` + `wwwroot/js/pages/profilo.js`: profilo migrato da prenotazioni legacy a ordini, biglietti, credito e cinema preferito
- `frontend/CineBase.Web/wwwroot/js/api.js`, `route-guard.js`, `template-loader.js`: integrazione delle nuove pagine protette e dei nuovi endpoint checkout

#### Fixed
- `pagamento.html`: risolto il caso in cui il pagamento carta risultava confermato su Stripe ma l'ordine restava `Pending` per mancanza di una finalizzazione backend successiva alla conferma client
- checkout reale: risolto il blocco dei posti quando l'utente annulla il pagamento e torna alla seat-map
- configurazione Stripe locale: eliminata la dipendenza da una `STRIPE_PUBLISHABLE_API_KEY` duplicata nel frontend; la chiave e ora gestita una sola volta nel backend
- seed/show pricing: corretti i casi in cui `PrezzoBase` veniva interpretato come `850` invece di `8.50`

#### Verified
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **218/218 PASS**
- Build frontend `CineBase.Web`: **OK**

### Iterazione 4 - Fase 9 refinement: `programmazione.html`, paginazione backend e tutorial frontend

#### Added
- `backend/FilmAPI/DTO/ProgrammazioneDTO.cs`: nuovo `ProgrammazioneFilmPagedResultDTO` per il listing film paginato di `GET /programmazione/films`
- `docs/tutorials/TUTORIAL_INDEX_PROGRAMMAZIONE_FRONTEND_CINEBASE.md`: tutorial dettagliato in terza persona su `index.html`, `home.js`, `programmazione.html`, geolocalizzazione, ordinamento cinema per distanza, cinema preferito, caroselli, card film, filtri categoria e paginazione

#### Changed
- `backend/FilmAPI/Endpoints/ProgrammazioneEndpoints.cs`: endpoint `GET /programmazione/films` esteso con query `page` e `pageSize`
- `backend/FilmAPI/Services/IProgrammazioneService.cs` + `backend/FilmAPI/Services/ProgrammazioneService.cs`: `GetFilmsAsync(...)` aggiornato per restituire un payload paginato e per correggere la logica `In uscita` in presenza di show attivi oggi o disponibilita nel cinema selezionato
- `wwwroot/programmazione.html` + `wwwroot/js/pages/programmazione.js`: UX evoluta con caroselli orizzontali per `In evidenza` e `In uscita`, caricamento incrementale automatico, `Carica altri film` per `Tutti i film`, contatore elementi e uso coerente della paginazione backend
- `wwwroot/js/api.js`: `getProgrammazioneFilms` aggiornato per inviare `page` e `pageSize`
- `wwwroot/css/styles.css`: aggiunti stili per caroselli, controlli di navigazione e stati della nuova programmazione pubblica

#### Fixed
- `programmazione.html`: geolocalizzazione browser resa non bloccante rispetto al bootstrap iniziale della pagina
- `scheda-film.html` e `programmazione.html`: risolto il disallineamento delle date causato da conversioni UTC/local time basate su `toISOString()`

#### Verified
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **216/216 PASS**
- Build frontend `CineBase.Web`: **OK**

### Iterazione 4 - Fase 10: Frontend `scheda-film.html` + `my-cinemas.html`

#### Added
- `wwwroot/scheda-film.html`: nuova pagina dettaglio film con copertina, metadati, descrizione, cast, regista, rail date orizzontale e show raggruppati per tipologia sala
- `wwwroot/js/pages/scheda-film.js`: logica completa per scheda film con:
  - caricamento dati da `GET /films/{id}/scheda?cinemaId=`
  - rail date orizzontale riusabile con frecce e selezione data
  - raggruppamento show per `TipoSala -> orari` con ordinamento 2D/3D/ISENSE/XL
  - bottoni orario auth-aware (autenticato -> acquista, anonimo -> login con redirect)
  - badge `Sala #N` per show allo stesso orario su sale diverse
  - modale selezione cinema con sincronizzazione cinema preferito
- `wwwroot/my-cinemas.html`: nuova pagina cinema-centric con due viste:
  - vista lista cinema: card con nome, citta, indirizzo, tipologie sala, distanza
  - vista dettaglio cinema: header con info cinema, rail date, lista film del giorno con show raggruppati
- `wwwroot/js/pages/my-cinemas.js`: logica completa per lista e dettaglio cinema con:
  - caricamento cinema da `GET /my-cinemas`
  - caricamento programmazione da `GET /my-cinemas/{cinemaId}/schedule?date=`
  - rail date con caricamento dinamico programmazione per data selezionata
  - bottoni orario auth-aware come scheda-film
- `wwwroot/js/date-rail.js`: componente riusabile per rail date orizzontale con:
  - generazione dinamica giorni (oggi + N giorni)
  - frecce sinistra/destra per scroll
  - selezione data con evento callback
  - stile coerente con design system CineBase
- `wwwroot/js/api.js`: aggiunti metodi `getFilmScheda`, `getMyCinemas`, `getCinemaSchedule`
- `wwwroot/css/styles.css`: aggiunti stili per date rail, show time buttons, tipo sala badges, film schedule cards

#### Changed
- `wwwroot/js/route-guard.js`: aggiunte `/scheda-film.html` e `/my-cinemas.html` come pagine pubbliche (anonimo/user/poweruser/admin)
- `wwwroot/js/template-loader.js`: aggiunte nuove pagine ai landing paths per caricamento navbar/footer landing

#### Verified
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet build frontend/CineBase.Web/CineBase.Web.csproj`: **OK**
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **216/216 PASS**

## 2026-04-18

### Iterazione 4 - Fase 9.1: Progetto `FilmApiSeeder` e seed realistico database

#### Added
- `backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj`: nuovo progetto console standalone per seed dati reali e credibili del dominio cinema multisala
- `backend/scripts/FilmApiSeeder/Program.cs`: orchestrazione completa del seed con supporto CLI, reset sicuri e connessione allo stesso DB di `FilmAPI`
- `backend/scripts/FilmApiSeeder/TmdbClient.cs`: client TMDB con ricerca film, dettagli, crediti, registi e risoluzione URL copertine
- `backend/scripts/FilmApiSeeder/SeedCatalog.cs`: catalogo seed di film target, cinema italiani, categorie e supplementi sala
- `backend/scripts/FilmApiSeeder/README.md`: documentazione locale del seeder con configurazione, opzioni e casi d'uso

#### Changed
- `backend/.env.example`: consolidato come sorgente environment condivisa tra backend API e seeder, con placeholder `TMDB_BEARER_TOKEN`
- `backend/FilmAPI/Program.cs`: caricamento variabili ambiente spostato a `backend/.env`
- struttura repository aggiornata: seeder spostato in `backend/scripts/FilmApiSeeder`
- `claude-code-test.sln`: aggiunto progetto `backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj`
- documentazione progetto aggiornata ai nuovi path `backend/.env` e `backend/.env.example`

#### Verified
- esecuzione completa seeder: **64 film**, **20 cinema**, **83 sale** generate/aggiornate
- reset programmazione con `--reset-shows --force`: **OK**
- query DB su `Films`, `Registi`, `Shows`, `Sale`, `SalaPosti`: **OK**
- endpoint backend usati da home e programmazione pubblica con dati seedati reali: **OK**

### Iterazione 4 - Fase 9: Frontend `programmazione.html` v2 + modale scelta cinema

#### Added
- `wwwroot/programmazione.html`: pagina completamente riprogettata come esperienza film-centric con:
  - header cinema selezionato con nome, citta e indirizzo
  - bottone "Cambia cinema" che apre modale selezione
  - tabs "In evidenza", "In uscita", "Tutti i film"
  - search input per titolo film con debounce
  - filtro categoria
  - griglia card film con copertina, titolo, durata, categorie, indicatore disponibilita
  - stati empty: nessun cinema selezionato, nessun film trovato
- `wwwroot/js/pages/programmazione.js`: riscritto completamente con:
  - `CinemaManager` per persistenza cinema preferito (localStorage per anonimo, backend per autenticato)
  - sincronizzazione cinema preferito al caricamento pagina (backend -> localStorage o viceversa)
  - modale selezione cinema con search, ordinamento per distanza (geolocalizzazione), card cinema
  - caricamento film da `GET /programmazione/films` con tab/search/categoria/cinemaId
  - rendering card film con indicatore disponibilita nel cinema selezionato
  - navigazione a `scheda-film.html` al click su card
  - evento `cinema:changed` per sincronizzazione navbar
- `wwwroot/components/navbar-landing.html`: aggiunto indicatore cinema selezionato (desktop + mobile)
- `wwwroot/js/api.js`: aggiunti metodi `getProgrammazioneFilms`, `getProgrammazioneCinemas`, `getCinemaPreferito`, `setCinemaPreferito`
- `wwwroot/css/styles.css`: aggiunti stili per tabs, line-clamp, modal scrollbar

#### Changed
- `wwwroot/programmazione.html`: da pagina proiezione-centric a pagina film-centric
- `wwwroot/js/pages/programmazione.js`: da logica basata su proiezioni/films/cinemas separati a logica film-centric con API aggregata

#### Verified
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **213/213 PASS**
- Build frontend `CineBase.Web`: **OK**

### Iterazione 4 - Fase 8: Backend ticketing digitale, PDF/email e validazione biglietti

#### Added
- `backend/FilmAPI/Services/IBigliettoService.cs` + `backend/FilmAPI/Services/BigliettoService.cs`: emissione ticket idempotente per ordine/posto, codici univoci `CB-...`, read model documento ordine
- `backend/FilmAPI/Services/IPdfService.cs` + `backend/FilmAPI/Services/PdfService.cs`: PDF multipagina con QR code, barcode grafico e dati completi biglietto
- `backend/FilmAPI/Services/IEmailService.cs` + `backend/FilmAPI/Services/EmailService.cs`: invio email SMTP provider-agnostic con allegato PDF
- `backend/FilmAPI/Services/IValidazioneBigliettoService.cs` + `backend/FilmAPI/Services/ValidazioneBigliettoService.cs`: lookup e validazione ticket con controllo cinema operativo
- `backend/FilmAPI/Endpoints/ValidazioneBigliettiEndpoints.cs`: endpoint `GET /admin/tickets/validate/{code}` e `POST /admin/tickets/validate`
- `tests/backend/Integration/TicketIntegrationTests.cs` e `tests/backend/Integration/ValidazioneTicketIntegrationTests.cs`: 6 test integrazione dedicati per ticket, PDF, email e validazione

#### Changed
- `backend/FilmAPI/Services/PagamentoService.cs`: ticket emission ed email spostate in servizi dedicati; PDF/email restano attività post-pagamento senza rollback dell'ordine pagato
- `backend/FilmAPI/Endpoints/CheckoutEndpoints.cs`: aggiunto `GET /checkout/orders/{orderId}/pdf`
- `backend/FilmAPI/Services/CheckoutService.cs`: ordini e ticket esposti con stato invio email e dati di validazione
- `backend/FilmAPI/DTO/BigliettoDTO.cs` e `backend/FilmAPI/DTO/CheckoutDTO.cs`: aggiunti DTO per validazione e documento PDF ordine
- `backend/FilmAPI/Program.cs`: registrati nuovi servizi ticketing, mappati endpoint validazione, configurata licenza `QuestPDF` community
- `backend/FilmAPI/FilmAPI.csproj` e `tests/backend/FilmAPI.Tests.csproj`: aggiunti `MailKit`, `QRCoder`, `QuestPDF`, `ZXing.Net` e `PdfPig`
- `tests/backend/Integration/CustomWebApplicationFactory.cs`: sostituito `IEmailService` con fake testabile per evitare dipendenza da SMTP reale

#### Verified
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet build tests/backend/FilmAPI.Tests.csproj`: **OK**
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **213/213 PASS**
- smoke test SMTP reale eseguito con configurazione locale `SMTP_*`, email inviata a `gennaro.malafronte@issgreppi.it` con PDF ticket finto allegato: **OK**

### Iterazione 4 - Documentazione Fase 8: ticketing digitale, SMTP Google e Twilio SendGrid

#### Added
- `docs/tutorials/TUTORIAL_FASE8_STRATEGIA_TICKETING_EMAIL_PDF_VALIDAZIONE.md`: documento strategico per la Fase 8 con architettura servizi, flusso ticketing, PDF, email e validazione
- `docs/tutorials/TUTORIAL_EMAIL_MAILKIT_BIGLIETTI_PDF_QRCODE.md`: tutorial didattico su `MailKit`, email HTML con allegati, `QuestPDF`, `QRCoder` e gestione completa del biglietto digitale
- `docs/tutorials/TUTORIAL_SMTP_GOOGLE_TWILIO_SENDGRID_SETUP_E_TROUBLESHOOTING.md`: guida operativa per configurazione SMTP Google e `Twilio SendGrid`, collaudo manuale e troubleshooting

#### Changed
- `docs/project/dev_iteration/4/PianoDiLavoro.md`: Fase 8 aggiornata con riferimenti operativi espliciti, strategia approvata `Google SMTP` come baseline e predisposizione architetturale per `Twilio SendGrid`
- `docs/project/dev_iteration/4/PianoDiLavoro.md`: sezione environment aggiornata con variabili coerenti al repository e template commentati per Google SMTP e `Twilio SendGrid` SMTP relay
- `backend/.env.example`: esteso con esempi commentati per i due scenari SMTP supportati dalla documentazione
- `docs/tutorials/TUTORIAL_FASE8_STRATEGIA_TICKETING_EMAIL_PDF_VALIDAZIONE.md`: arricchito con riferimenti ufficiali provider email, variabili `SMTP_*` del repository e strategia provider-agnostic
- `docs/tutorials/TUTORIAL_EMAIL_MAILKIT_BIGLIETTI_PDF_QRCODE.md`: approfondito con procedure passo passo, URL ufficiali, blocchi `.env` pronti all'uso e chiarimenti di compatibilità architetturale

#### Verified
- riferimenti ufficiali Google, Microsoft e `Twilio SendGrid` verificati manualmente durante la redazione dei tutorial
- coerenza tra tutorial, piano di lavoro e `backend/.env.example` verificata manualmente
- nessun test automatico eseguito, perché le modifiche sono limitate a documentazione e configurazione esempio

### Iterazione 4 - Fase 7: Pagamento, credito piattaforma e finalizzazione checkout

#### Added
- `backend/FilmAPI/DTO/OrdineDTO.cs`, `BigliettoDTO.cs`, `CreditoDTO.cs`: DTO per ordini, ticket e credito
- `backend/FilmAPI/Services/IPagamentoService.cs` + `Services/PagamentoService.cs`: finalizzazione checkout con pagamento carta, credito e misto
- `backend/FilmAPI/Services/CreditoService.cs`: consultazione credito utente e ricariche admin con audit `MovimentoCredito`
- `backend/FilmAPI/Services/StripeGateway.cs`: gateway astratto per `Stripe.net`
- `backend/FilmAPI/Endpoints/PagamentoEndpoints.cs`: endpoint pagamento e webhook Stripe
- `backend/FilmAPI/Endpoints/CreditoEndpoints.cs`: endpoint user/admin per credito e ricariche
- `tests/backend/Integration/PagamentoCreditoIntegrationTests.cs`: test integrazione per carta, credito, misto, saldo insufficiente e replay webhook

#### Changed
- `backend/FilmAPI/Services/CheckoutService.cs`: esposizione liste ordini e ticket per il profilo utente
- `backend/FilmAPI/Endpoints/CheckoutEndpoints.cs`: completato il flusso checkout con endpoint ordine e dettaglio ordine
- `backend/FilmAPI/Program.cs`: registrati i servizi pagamento/credito e i nuovi endpoint
- `backend/FilmAPI/Model/Ordine.cs` e `backend/FilmAPI/Model/Biglietto.cs`: estesi per supportare finalizzazione e tracciamento economico
- `backend/FilmAPI/Data/FilmDbContext.cs`: aggiornato con vincoli e relazioni per il dominio pagamento/credito
- `.env.example` backend e frontend: aggiunti placeholder Stripe e URL applicativi necessari al flusso

#### Verified
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **207/207 PASS**
- Pagamento carta in test mode verificato con `tok_visa`
- Webhook Stripe verificato in locale con `Stripe CLI`

## 2026-04-17

### Iterazione 4 - Fase 6: Backend seat map, hold posti e ordine pendente

#### Added
- `backend/FilmAPI/DTO/CheckoutDTO.cs`: nuovi DTO per checkout e seat selection:
  - `SeatStatus`: enum con `Available`, `HeldByOther`, `HeldByMe`, `Sold`
  - `SeatMapDTO`: piantina posti con summary show/sala, `ScadeAtUtc` hold corrente, lista `SeatInfoDTO`
  - `SeatInfoDTO`: singolo posto con `SalaPostoId`, `Settore`, `Fila`, `Numero`, `IsWheelchair`, `Stato`
  - `SeatHoldRequestDTO`: richiesta hold con `ShowId` e `SalaPostoIds`
  - `SeatHoldResponseDTO`: risposta hold con `HoldToken`, `ScadeAtUtc`, `SalaPostoIds`, `Conflitti`
  - `CreateOrdineRequestDTO`: richiesta ordine con `HoldToken` e `IdempotencyKey` opzionale
  - `OrdineSummaryDTO`: riepilogo ordine con film/cinema/sala, importi, stato
- `backend/FilmAPI/Services/ISeatHoldService.cs` + `Services/SeatHoldService.cs`: servizio hold posti su `ShowPostoStato`:
  - `GetSeatMapAsync`: restituisce seat map con stati aggiornati (lazy cleanup scaduti incluso)
  - `CreateHoldAsync`: crea hold in transazione atomica con:
    - cleanup hold scaduti per lo show
    - validazione appartenenza posti alla sala dello show
    - controllo conflitti (posti gia venduti o holdati da altri)
    - generazione `HoldToken` univoco (`{userId}_{showId}_{guid}`)
    - TTL configurabile via `HOLD_TTL_MINUTES` (default 10 min)
  - `RefreshHoldAsync`: estende TTL hold corrente (keep-alive)
  - `ReleaseHoldAsync`: rilascia esplicitamente hold (rimuove record)
  - `CleanupExpiredHoldsAsync`: cleanup globale hold scaduti
  - validazione limite massimo 10 posti per ordine
- `backend/FilmAPI/Services/ICheckoutService.cs` + `Services/CheckoutService.cs`: servizio checkout:
  - `CreateOrdineAsync`: crea ordine `Pending` da hold valido con:
    - verifica ownership hold e validita temporale
    - idempotenza su stesso `HoldToken` (ritorna ordine esistente)
    - idempotenza su `IdempotencyKey` client-generated
    - calcolo totale lato backend (`PrezzoBase + SupplementoSala` × numero posti)
    - linking `OrdineId` su record `ShowPostoStato`
  - `GetOrdiniByUserAsync`: lista ordini utente con ownership check
  - `GetOrdineByIdAsync`: dettaglio ordine con ownership check
- `backend/FilmAPI/Endpoints/CheckoutEndpoints.cs`: 7 endpoint checkout (tutti `Authenticated`):
  - `GET /checkout/shows/{showId}/seat-map` — piantina posti con stati
  - `POST /checkout/holds` — crea hold posti (409 Conflict con dettagli se posti non disponibili)
  - `POST /checkout/holds/{holdToken}/refresh` — estendi TTL hold
  - `DELETE /checkout/holds/{holdToken}` — rilascia hold
  - `POST /checkout/orders` — crea ordine pendente da hold valido
  - `GET /checkout/orders` — lista ordini utente
  - `GET /checkout/orders/{orderId}` — dettaglio ordine
- `backend/FilmAPI/Services/ExpiredHoldCleanupService.cs`: hosted service per cleanup periodico hold scaduti:
  - intervallo configurabile con `HOLD_CLEANUP_INTERVAL_MINUTES` (default 5 min)
  - rimozione record `ShowPostoStato` con `Stato=Hold` e `ScadeAtUtc` scaduto
- `tests/backend/Integration/CheckoutIntegrationTests.cs`: 20 test integrazione:
  - `CH1`: seat map con posti disponibili
  - `CH2`: seat map show non trovato
  - `CH3`: crea hold con successo
  - `CH4`: hold supera max 10 posti → BadRequest
  - `CH5`: hold stesso posto da altro utente → Conflict
  - `CH6`: stesso utente puo estendere hold
  - `CH7`: refresh hold estende scadenza
  - `CH8`: release hold rende posti disponibili
  - `CH9`: crea ordine da hold valido → Pending
  - `CH10`: idempotenza su stesso holdToken
  - `CH11`: lista ordini utente
  - `CH12`: dettaglio ordine con ownership
  - `CH13`: dettaglio ordine altro utente → NotFound
  - `CH14`: seat map mostra HeldByMe dopo hold
  - `CH15`: seat map mostra HeldByOther dopo hold altrui
  - `CH16`: concorrenza hold stesso posto → solo 1 vince
  - `CH17`: concorrenza hold posti sovrapposti → gestione conflitto
  - `CH18`: idempotenza con IdempotencyKey
  - `CH19`: ordine con holdToken vuoto → BadRequest
  - `CH20`: ordine con holdToken inesistente → Conflict

#### Changed
- `backend/FilmAPI/Program.cs`: registrati `ISeatHoldService`/`SeatHoldService`, `ICheckoutService`/`CheckoutService`, `ExpiredHoldCleanupService`, mappati `CheckoutEndpoints`

#### Verified
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK** (0 warning, 0 errori)
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **201/201 PASS** (181 esistenti + 20 nuovi)

## 2026-04-17

### Iterazione 4 - Fase 5: Backend show e bridge legacy proiezioni

#### Added
- `backend/FilmAPI/DTO/ShowDTO.cs`: DTO per gestione show (`ShowDTO`, `ShowCreateDTO`, `ShowUpdateDTO`, `ShowPagedResultDTO`)
- `backend/FilmAPI/Services/IShowService.cs` + `Services/ShowService.cs`: service CRUD show con:
  - validazione esistenza film, cinema, sala
  - validazione coerenza sala appartiene al cinema
  - validazione anti-overlap temporale completa nella stessa sala (`[NuovoStart, NuovoEnd)` vs `[Start, Start+Durata)`)
  - fallback durata da film se non specificata
  - fallback prezzo base a 10m
  - supplemento sala copiato da Sala.Supplemento
  - blocco delete se esistono biglietti emessi
  - query specializzate per cinema, film, data
- `backend/FilmAPI/Endpoints/ShowsEndpoints.cs`: 5 endpoint show:
  - `GET /shows` [AllowAnonymous] — lista completa o paginata con filtri cinemaId/filmId/date
  - `GET /shows/{id}` [AllowAnonymous] — dettaglio
  - `POST /shows` [PowerUserOrAdmin] — crea
  - `PUT /shows/{id}` [PowerUserOrAdmin] — aggiorna
  - `DELETE /shows/{id}` [PowerUserOrAdmin] — elimina
- `tests/backend/Integration/ShowIntegrationTests.cs`: 28 test integrazione per CRUD show, anti-overlap, RBAC, filtri
- `tests/backend/Integration/ProiezioneCompatIntegrationTests.cs`: 10 test compatibilita bridge legacy

#### Changed
- `backend/FilmAPI/Services/ProiezioneService.cs`: riscritto come adapter sul dominio Show:
  - read: projection da Shows verso ProiezioneDTO
  - write: bridge temporaneo verso ShowService con assegnazione sala default del cinema
  - update: gestisce cambio cinema con assegnazione nuova sala default
- `backend/FilmAPI/DTO/ProiezioneDTO.cs`: `ProiezioneUpdateDTO` reso nullable per partial updates
- `backend/FilmAPI/Program.cs`: registrato `IShowService`/`ShowService`, mappati `ShowsEndpoints`
- `tests/backend/Unit/ProiezioneServiceTests.cs`: aggiunto IShowService + SalaService al DI container, seed sala
- `tests/backend/Integration/ApiIntegrationTests.cs`: CreateProiezioneAsync ora crea sala automaticamente se mancante

#### Verified
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **181/181 PASS** (143 esistenti + 38 nuovi)

## 2026-04-17

### Refactor backend: separazione interfacce/classi nei Services

#### Changed
- Applicata best practice "one type per file" a tutti i service con interfaccia e classe nello stesso file
- Creati 8 nuovi file interfaccia:
  - `backend/FilmAPI/Services/IFilmService.cs`
  - `backend/FilmAPI/Services/ICategoriaService.cs`
  - `backend/FilmAPI/Services/ICinemaService.cs`
  - `backend/FilmAPI/Services/IMediaService.cs`
  - `backend/FilmAPI/Services/IProgrammazioneService.cs`
  - `backend/FilmAPI/Services/IProiezioneService.cs`
  - `backend/FilmAPI/Services/IRegistaService.cs`
  - `backend/FilmAPI/Services/ISalaService.cs`
- Rimosse definizioni interfaccia inline dai file delle classi:
  - `backend/FilmAPI/Services/FilmService.cs`
  - `backend/FilmAPI/Services/CategoriaService.cs`
  - `backend/FilmAPI/Services/CinemaService.cs`
  - `backend/FilmAPI/Services/MediaService.cs`
  - `backend/FilmAPI/Services/ProgrammazioneService.cs`
  - `backend/FilmAPI/Services/ProiezioneService.cs`
  - `backend/FilmAPI/Services/RegistaService.cs`
  - `backend/FilmAPI/Services/SalaService.cs`
- Nessun file di test o endpoint modificato (interfacce pubbliche invariate)

#### Verified
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **143/143 PASS**

## 2026-04-17

### Iterazione 4 - Fase 4: Backend sale e piantina posti

#### Added
- `backend/FilmAPI/DTO/SalaDTO.cs`: DTO per gestione sale e piantina (`SalaDTO`, `SalaCreateDTO`, `SalaUpdateDTO`, `SalaPostoDTO`, `SalaLayoutSaveDTO`)
- `backend/FilmAPI/Services/SalaService.cs`: service CRUD sale con validazioni:
  - unicità numero progressivo nel cinema
  - blocco delete con show futuri o biglietti emessi
  - salvataggio piantina completa (replace-all)
- `backend/FilmAPI/Endpoints/SaleEndpoints.cs`: 7 endpoint sale/piantina:
  - `GET /cinemas/{cinemaId}/sale` [AllowAnonymous]
  - `POST /cinemas/{cinemaId}/sale` [PowerUserOrAdmin]
  - `GET /sale/{salaId}` [AllowAnonymous]
  - `PUT /sale/{salaId}` [PowerUserOrAdmin]
  - `DELETE /sale/{salaId}` [PowerUserOrAdmin]
  - `GET /sale/{salaId}/posti` [AllowAnonymous]
  - `PUT /sale/{salaId}/posti` [PowerUserOrAdmin]
- `tests/backend/Integration/SalaIntegrationTests.cs`: 20 test integrazione per CRUD sale, piantina, validazioni

#### Changed
- `backend/FilmAPI/Program.cs`: registrato `ISalaService`/`SalaService`, mappati `SaleEndpoints`

#### Verified
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **143/143 PASS** (123 esistenti + 20 nuovi)

## 2026-04-16

### Iterazione 4 - Fase 3: Backend catalogo pubblico, scheda film e cinema preferito

#### Added
- `backend/FilmAPI/DTO/ProgrammazioneDTO.cs`: nuovi DTO per catalogo pubblico (`ProgrammazioneFilmDTO`, `FilmSchedaDTO`, `CinemaCardDTO`, `CinemaScheduleDayDTO`, `CinemaSintesiDTO`, `CinemaPreferitoDTO`)
- `backend/FilmAPI/Services/IProgrammazioneService.cs` + `Services/ProgrammazioneService.cs`: service completo per programmazione pubblica, scheda film, my-cinemas, cinema preferito
- `backend/FilmAPI/Endpoints/ProgrammazioneEndpoints.cs`: 5 endpoint pubblici (`/programmazione/films`, `/programmazione/cinemas`, `/films/{id}/scheda`, `/my-cinemas`, `/my-cinemas/{cinemaId}/schedule`)
- `tests/backend/Integration/ProgrammazioneIntegrationTests.cs`: 20 test integrazione per tabs, search, categoria, cinema preferito, ordinamento distanza

#### Changed
- `backend/FilmAPI/DTO/FilmDTO.cs`: aggiunto `DescrizioneLunga`, `CastText`, `DataRilascio` a `FilmDTO`, `FilmCreateDTO`, `FilmUpdateDTO`
- `backend/FilmAPI/Services/FilmService.cs`: tutte le proiezioni ora includono i nuovi campi film
- `backend/FilmAPI/Services/IProfiloService.cs` + `Services/ProfiloService.cs`: aggiunti `GetCinemaPreferitoAsync` e `SetCinemaPreferitoAsync`
- `backend/FilmAPI/Endpoints/ProfiloEndpoints.cs`: aggiunti `GET/PUT /profilo/cinema-preferito`
- `backend/FilmAPI/Program.cs`: registrato `IProgrammazioneService` e mappati `ProgrammazioneEndpoints`

#### Verified
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **123/123 PASS** (103 esistenti + 20 nuovi)

### Iterazione 4 - Fase 2: Migration, seed e data migration legacy

#### Added
- `backend/FilmAPI/Migrations/20260416171534_AddMultisalaTicketing.cs`: migration schema + data migration legacy per multisala/ticketing
- `backend/FilmAPI/Migrations/20260416171534_AddMultisalaTicketing.Designer.cs`: designer EF della migration
- `backend/FilmAPI/Data/DataSeeder.cs`: seed dev esteso con cinema, registi, film, sale e show di esempio

#### Changed
- Data migration legacy in `AddMultisalaTicketing` aggiornata per aderire al piano Fase 2:
  - `CreditoResiduo` inizializzato a `0` solo se `NULL`
  - creazione `Sala 1` default per cinema senza sale
  - migrazione `Proiezione -> Show` con `StartAtUtc` calcolato da `Data + Ora`
  - gestione conflitti per sovrapposizione temporale con creazione di `Sala auto-migrata N`
  - valorizzazione `PrezzoBase` con `DEFAULT_TICKET_PRICE` (fallback `8.50`)
  - nessuna conversione automatica `Prenotazione -> Biglietto`

#### Verified
- `dotnet ef database drop --project backend/FilmAPI/FilmAPI.csproj --force`: **OK**
- `dotnet ef database update --project backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet ef migrations list --project backend/FilmAPI/FilmAPI.csproj`: migration applicata fino a `AddMultisalaTicketing`
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **103/103 PASS**

### Iterazione 4 - Fase 1: Modello dati v2 e compat layer

#### Added
- Nuovi enum per il dominio multisala/ticketing:
  - `backend/FilmAPI/Model/TipoSala.cs`: DueD=0, TreD=1, ISENSE=2, XL=3
  - `backend/FilmAPI/Model/ShowPostoState.cs`: Hold=0, Sold=1
  - `backend/FilmAPI/Model/OrdineState.cs`: Pending=0, Paid=1, Failed=2, Cancelled=3, Expired=4
  - `backend/FilmAPI/Model/BigliettoState.cs`: Issued=0, Validated=1, Cancelled=2
  - `backend/FilmAPI/Model/MovimentoCreditoTipo.cs`: TopUp=0, DebitOrder=1, Refund=2, Adjustment=3
- Nuove entità per il dominio multisala/ticketing:
  - `backend/FilmAPI/Model/Sala.cs`: sala cinema con numero progressivo, tipo, nome, supplemento
  - `backend/FilmAPI/Model/SalaPosto.cs`: posto con settore, fila, numero, coordinate, wheelchair
  - `backend/FilmAPI/Model/Show.cs`: spettacolo con cinema/sala/film/data-ora/prezzo
  - `backend/FilmAPI/Model/ShowPostoStato.cs`: stato hold/sold per ogni posto-show
  - `backend/FilmAPI/Model/Ordine.cs`: ordine con holdToken, importi, stato, payment info
  - `backend/FilmAPI/Model/Biglietto.cs`: biglietto con codice, barcode, validazione
  - `backend/FilmAPI/Model/MovimentoCredito.cs`: movimento credito con tipo, importo, saldo pre/post
- Entità estese con nuovi campi e navigazioni:
  - `Film.cs`: DescrizioneLunga, CastText, DataRilascio, ICollection<Show>
  - `Cinema.cs`: Latitudine, Longitudine, Telefono, CodiceLocale, ICollection<Sala>
  - `User.cs`: CinemaPreferitoId, CreditoResiduo, ICollection<Ordine>, ICollection<Biglietto>
- `Data/FilmDbContext.cs`: aggiornato con 7 nuovi DbSet, relazioni complete, indici unici e delete behavior

#### Changed
- `.env.example`: aggiunti placeholder Stripe, SMTP, ticketing e URL applicativi (porte allineate a 5001 per frontend)

#### Verified
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet build tests/backend/FilmAPI.Tests.csproj`: **OK**
- Proiezione e Prenotazione legacy: **ancora presenti** (non rimosse in questa fase)

## 2026-04-13

### Iterazione 4 - Hardening refresh token lifecycle (device-aware + cleanup + route guard proattivo)

#### Added
- `backend/FilmAPI/Services/RefreshTokenCleanupService.cs`: hosted service per cleanup periodico token revocati/scaduti
- `backend/FilmAPI/Migrations/20260413200358_AddRefreshTokenDeviceId.cs` (+ `.Designer.cs`): migrazione DB con colonna `DeviceId` su `RefreshTokens`
- `RefreshToken.DeviceId` con indice composto `(UserId, DeviceId)` per supportare policy per-device
- `auth.js`: chiave `cb_device_id` in `localStorage` con generazione UUID e fallback legacy `web-default`

#### Changed
- `backend/FilmAPI/DTO/AuthDTO.cs`: aggiunto `DeviceId` a `LoginRequestDTO`, `RegisterRequestDTO`, `RefreshTokenRequestDTO`
- `backend/FilmAPI/Services/IAuthService.cs` e `AuthService.cs`:
  - `RefreshAsync`/`LogoutAsync` ora accettano `deviceId`
  - validazione refresh token vincolata al device
  - revoca token attivi preesistenti per stessa coppia `UserId+DeviceId` prima di emetterne uno nuovo
- `backend/FilmAPI/Endpoints/AuthEndpoints.cs`: propagazione `deviceId` ai metodi service
- `backend/FilmAPI/Program.cs`: registrato hosted service cleanup
- `frontend/CineBase.Web/wwwroot/js/auth.js`: invio `deviceId` su login/register/refresh/logout
- `frontend/CineBase.Web/wwwroot/js/route-guard.js`: tentativo refresh proattivo prima di redirect login

#### Fixed
- Migrazione iniziale fallita su MySQL per drop indice `IX_RefreshTokens_UserId` richiesto da FK
  - risolto mantenendo indice singolo `UserId` e aggiungendo indice composto senza drop distruttivi
- Migrazione aggiornata con default/backfill `DeviceId = 'web-default'` per compatibilita record storici

#### Verified
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet ef database update --project backend/FilmAPI/FilmAPI.csproj`: **OK** (migrazione `20260413200358_AddRefreshTokenDeviceId` applicata)
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **103/103 PASS**

### Iterazione 4 - Refactor navigazione admin + cleanup landing/mobile UX

#### Added
- `wwwroot/js/admin-shell.js`: nuova shell admin condivisa con:
  - sidebar laterale unica per tutta l'area admin
  - topbar secondaria con menu utente (profilo, prenotazioni, logout)
  - gestione link attivo, toggle sidebar mobile e backdrop
- Integrazione admin shell su pagine:
  - `dashboard.html`
  - `films.html`
  - `registi.html`
  - `cinemas.html`
  - `proiezioni.html`
  - `categorie.html`

#### Changed
- `dashboard.html`: rimosso layout sidebar inline legacy, adottata shell unificata come le altre pagine admin
- `template-loader.js`: escluso caricamento navbar/footer legacy per pagine coperte da admin shell
- `navbar-landing.html`:
  - rimossi link ridondanti `Film`/`Sale`
  - menu mobile riorganizzato in sezioni (`Navigazione`, `Account`, `Preferenze`)
  - CTA mobile allineate ai token desktop (`btn-outline-brand`, `btn-gold`) con proporzioni coerenti
  - palette mobile allineata al desktop (`glass-panel`, gerarchie colore consistenti)
- `index.html`: rimossi CTA ridondanti in hero (`Area Admin`, `Programmazione`) per ridurre duplicazione con navbar/featured
- `footer-landing.html`: rimosso testo `API Docs | Swagger`
- `styles.css` + `js/pages/home.js`: overlay hero alleggerito e visibilita immagine di sfondo aumentata

#### Fixed
- Incoerenza UX area admin: transizione da dashboard (sidebar) a CRUD (navbar orizzontale) eliminata con layout unico
- Navbar landing mobile: migliorata leggibilita e ordine dei link account/navigation
- Duplicazione controlli tema in area admin: rimosso toggle dalla topbar (presente solo in sidebar)

#### Verified
- Build frontend `CineBase.Web`: **OK**
- Verifica manuale:
  - sidebar unificata presente su tutte le pagine admin
  - topbar secondaria coerente e senza duplicazioni
  - menu mobile landing ordinato e cromaticamente allineato a desktop/tablet

## 2026-04-12

### Iterazione 3 - Fase 11: Verifica finale, hardening e documentazione

#### Verified
- Test suite backend completa: **103/103 PASS**
- RBAC backend verificato su tutti gli endpoint:
  - Endpoint pubblici (GET films/cinemas/proiezioni/categorie): `AllowAnonymous`
  - Endpoint auth (register/login/refresh): `AllowAnonymous`; (logout/me): `Authenticated`
  - CRUD registi/films/proiezioni/categorie: `PowerUserOrAdmin`
  - CRUD cinemas: GET pubblico, CUD `AdminOnly`
  - Media upload: `PowerUserOrAdmin`
  - Profilo/prenotazioni: `Authenticated` con ownership check
  - Admin utenti: `AdminOnly`
- RBAC frontend verificato (route-guard.js):
  - Pagine pubbliche: `index.html`, `programmazione.html`
  - Anonimo-only: `login.html`, `registrazione.html` (redirect se gia loggati)
  - PowerUser/Admin: `dashboard.html`, `films.html`, `registi.html`, `cinemas.html`, `proiezioni.html`, `categorie.html`
  - User/PowerUser/Admin: `profilo.html`
  - Redirect coerenti: non autenticati -> login con redirect; ruolo insufficiente -> index con forbidden
- Tutti i 11 criteri di accettazione iterazione 3 soddisfatti

## 2026-04-12

### Iterazione 3 - Fase 10 estesa: UX listing admin + paginazione backend

#### Added
- Paginazione backend server-side per endpoint lista:
  - `GET /registi?page=&pageSize=&search=`
  - `GET /cinemas?page=&pageSize=&search=`
  - `GET /proiezioni?page=&pageSize=&search=`
- Nuovi DTO paginati backend:
  - `RegistaPagedResultDTO`
  - `CinemaPagedResultDTO`
  - `ProiezionePagedResultDTO`
- Ricerca + paginazione UI su pagine admin:
  - `registi.html`
  - `cinemas.html` (inclusa nuova search bar)
  - `proiezioni.html` (inclusa nuova search bar)
- Test integrazione backend aggiunti per paginazione e compatibilita legacy:
  - `R10_GetRegisti_WithPaginationAndSearch_ReturnsPagedResult`
  - `R11_GetRegisti_WithoutPaginationParams_ReturnsLegacyArrayPayload`
  - `C6_GetCinemas_WithPaginationAndSearch_ReturnsPagedResult`
  - `C7_GetCinemas_WithoutPaginationParams_ReturnsLegacyArrayPayload`
  - `P9_GetProiezioni_WithPaginationAndSearch_ReturnsPagedResult`
  - `P10_GetProiezioni_WithoutPaginationParams_ReturnsLegacyArrayPayload`

#### Changed
- `api.js`: `getRegisti`, `getCinemas`, `getProiezioni` ora accettano params opzionali `page`, `pageSize`, `search`
- `dashboard.html`: stato proiezioni uniformato a pill arrotondata con `chip-status`
- `proiezioni.js`:
  - stato non piu hardcoded su "Pianificata", ora calcolato (`Passata` / `In programma`)
  - colonne film/cinema arricchite con nome/titolo + ID visualmente secondario
- `home.js` / `index.html`: tuning UX della sezione "In Evidenza" (hero + compact cards), bilanciamento altezze, overlay meno aggressivo, comportamento piu stabile su mobile/desktop

#### Fixed
- Home hero mobile: eliminato clipping verticale in alto/basso della sezione hero principale
- Featured card mobile: eliminato taglio laterale sinistro e migliorato scaling con viewport
- `profilo.html`: fix accessibilita/lint su campo email (label associata con `for="profilo-email"`)

#### Verified
- Build backend `FilmAPI`: **OK**
- Test suite backend completa: **103/103 PASS**
- Verifica endpoint paginati:
  - `cinemas` e `proiezioni` verificati anonimi
  - `registi` verificato autenticato admin
  - compatibilita legacy (senza query params) mantenuta

## 2026-04-12

### Iterazione 3 - Fase 10: Area Personale utente (profilo + prenotazioni)

#### Added
- `wwwroot/profilo.html`: pagina area personale con sezione dati personali (nome, cognome, telefono) aggiornabili e sezione prenotazioni (lista, cancellazione, creazione)
- `wwwroot/js/pages/profilo.js`: logica completa per update profilo (`PUT /profilo`), CRUD prenotazioni (`GET/POST/DELETE /prenotazioni`), creazione prenotazione da `?prenota=<proiezioneId>` e da form manuale con selettore proiezioni future
- Sezione prenotazione dedicata: carica automaticamente dettagli proiezione (film, cinema, data/ora) quando `?prenota=` presente; dropdown proiezioni future per creazione manuale
- Feedback UI: toast successo/errore, indicatore "Salvato" su update profilo, conferma cancellazione prenotazione
- Scroll automatico a sezione prenotazioni quando si accede via `#prenotazioni` anchor
- `route-guard.js`: aggiunto `/profilo.html` con `authRequired: true`, ruoli `['user', 'poweruser', 'admin']`
- `template-loader.js`: aggiunto `/profilo.html` ai landing paths

#### Changed
- `navbar-landing.html`: link "Prenotazioni" punta a `/profilo.html#prenotazioni` invece di `/prenotazioni.html` (desktop + mobile)
- `programmazione.js`: bottone "Accedi per prenotare" per anonimi ora redirecta a `/login.html?redirect=/profilo.html?prenota=<id>` per flusso diretto login->prenotazione

#### Verified
- Test backend: **97/97 PASS** (nessuna modifica backend)
- API e2e: register→login→profilo→update→crea prenotazione→lista→cancella prenotazione **OK**
- Route guard: `/profilo.html` blocca accesso anonimo, redirect login con URL completo
- Flusso programmazione→login→profilo→prenotazione verificato end-to-end

### Iterazione 3 - Home refresh: Featured section allineata a pagina Programmazione

#### Changed
- `wwwroot/index.html`: sostituita la sezione "Film in Programmazione" con "In Evidenza Questa Settimana"
  - rimossi i filtri operativi (ora presenti solo in `programmazione.html`)
  - aggiunta CTA esplicita "Vai alla Programmazione"
  - aggiornato copy hero per comunicare in modo definitivo la visione di piattaforma completa per la gestione delle sale cinematografiche
- `wwwroot/js/pages/home.js`: nuova logica featured
  - selezione film top in base al numero di proiezioni nei prossimi 7 giorni
  - fallback su nuove uscite quando non ci sono proiezioni utili
  - layout: hero card grande + card laterali compatte
  - CTA card orientate a discovery (`/programmazione.html`) invece di prenotazione diretta
- `wwwroot/components/footer-landing.html`: navigazione footer resa role-aware (anonimo/user/poweruser/admin) in coerenza con la navbar

#### Fixed
- Risolta inconsistenza di rendering non deterministico tra film/proiezioni causata da caricamento asincrono non sincronizzato
- Risolto caricamento navbar errata su `/programmazione.html` (ora usa layout landing tramite `template-loader.js`)

### Iterazione 3 - Fase 9: Programmazione pubblica + gestione categorie admin

#### Added
- `wwwroot/programmazione.html`: nuova pagina pubblica con elenco proiezioni e filtri (citta/data/categoria/orario)
- `wwwroot/js/pages/programmazione.js`: logica di caricamento proiezioni, popolamento filtri dinamici (citta da cinema, categorie da API), rendering card proiezioni con badge categorie e bottone prenota auth-aware
- `wwwroot/categorie.html`: nuova pagina CRUD categorie per admin/poweruser
- `wwwroot/js/pages/categorie.js`: create, update, delete categorie con feedback UI (toast)
- `wwwroot/js/pages/home.js`: badge categorie multiple sulle card film invece di genere singolo, bottone prenota auth-aware
- `route-guard.js`: aggiunte regole per `/programmazione.html` (accessibile a tutti) e `/categorie.html` (solo poweruser/admin)
- `api.js`: aggiunto `/categorie.html` ai path admin per enforcement accesso frontend

#### Changed
- `navbar-landing.html`: link "Programmazione" ora punta a `/programmazione.html` invece di `#programmazione`
- `index.html`: hero button "Programmazione" punta a `/programmazione.html`
- `films.html`: aggiunta colonna "Categorie" nella tabella, sostituito filtro hardcoded "genere" con select dinamico categorie, aggiunto gruppo checkbox categorie nel form
- `films.js`: caricamento e popolamento categorie, filtro per categoria funzionante, visualizzazione badge categorie in tabella, gestione categorieIds nel submit form

#### Verified
- Test backend: **97/97 PASS**
- Route guard: `/programmazione.html` accessibile ad anonimi (verificato tramite PAGE_PERMISSIONS), `/categorie.html` protetto per poweruser/admin

## 2026-04-07

### Iterazione 3 - Fase 8.2: Fix loop redirect su login con token valido

#### Fixed
- **Loop redirect login.html**: quando un utente con token valido accedeva direttamente a una pagina admin (es. `/proiezioni.html`), il route-guard nell'`<head>` non vedeva `window.Auth` (non ancora caricato) e reindirizzava al login, che a sua volta vedeva il token valido e reindirizzava alla pagina → loop infinito
  - Causa: `route-guard.js` dipendeva da `window.Auth` che non esiste quando lo script viene eseguito nell'`<head>` (prima del body dove `auth.js` e' incluso)
  - Fix: `route-guard.js` ora e' completamente self-contained — legge token da `localStorage` e fa parsing JWT internamente, senza alcuna dipendenza da `Auth`

## 2026-04-07

### Iterazione 3 - Fase 8.1: Fix Route Guard (redirect sincrono + navbar role-aware)

#### Fixed
- **Flash pagina non autorizzata**: route-guard.js riscritto come IIFE con esecuzione immediata, spostato nell'`<head>` di tutte le pagine — il redirect avviene PRIMA che il body venga parsato/renderizzato
- **Pulsante "indietro" del browser**: usato `window.location.replace()` invece di `window.location.href` per evitare che la pagina bloccata resti nella history
- **Navbar-landing mostra Film/Sale a utenti non autorizzati**: link "Film" e "Sale" ora nascosti di default (`class="hidden"`), visibili solo a `PowerUser`/`Admin` (desktop + mobile)
- Parsing JWT diretto da localStorage nel route-guard (senza dipendenza da `Auth` inizializzato) per decisione di redirect prima del DOMContentLoaded

#### Changed
- `wwwroot/js/route-guard.js`: riscritto come IIFE con `check()` eseguito immediatamente
- `wwwroot/components/navbar-landing.html`: `updateAuthUI` gestisce visibilita completa di Film/Sale oltre ad Area Admin
- Tutte le pagine HTML: `<script src="/js/route-guard.js">` spostato nell'`<head>` dopo `theme.js`

#### Verified
- Nessun flash di pagina non autorizzata su accesso diretto
- History browser non include pagina bloccata (replace invece di href)
- Navbar landing mostra solo voci permesse per ciascun ruolo
- Test backend: **97/97 PASS**

## 2026-04-07

### Iterazione 3 - Fase 8: Route guard e navigazione per ruolo

#### Added
- `wwwroot/js/route-guard.js`: route guard con mappa pagina->ruoli (`PAGE_PERMISSIONS`)
  - redirect anonimi su pagine admin -> `login.html?redirect=<pagina>`
  - redirect loggati da `login.html`/`registrazione.html` -> `index.html` (o redirect originale)
  - redirect ruolo insufficiente (`User` su pagine admin) -> `index.html?forbidden=true`
- Bottone "Area Admin" in `navbar-landing.html` (desktop + mobile), visibile solo a `PowerUser`/`Admin`
- Link "Categorie" in `navbar-admin.html` (desktop + mobile)
- Menu dropdown utente in `navbar-admin.html` con profilo e logout reale
- Avatar utente con iniziali dinamiche in navbar admin

#### Changed
- `wwwroot/components/navbar-landing.html`: `updateAuthUI` nasconde "Area Admin" per ruolo `User` e anonimi
- `wwwroot/components/navbar-admin.html`: rimosso mock "Admin" hardcoded, sostituito con dati utente reali da `Auth.getUser()`
- Tutte le pagine HTML (`index.html`, `login.html`, `registrazione.html`, `dashboard.html`, `films.html`, `registi.html`, `cinemas.html`, `proiezioni.html`): incluso `route-guard.js`

#### Verified
- Redirect URL diretto a pagina non consentita -> corretto (route-guard.js)
- Utente `User` non vede bottone/entry "Area Admin" nella navbar
- Test backend: **97/97 PASS** (nessuna modifica backend)

## 2026-04-06

### Iterazione 3 - Fase 7.1: Manutenzione frontend (lint/accessibilita + config condivisa + fix home)

#### Added
- `wwwroot/js/tailwind-config.js`: configurazione Tailwind centralizzata (theme `brand` + dark mode)

#### Changed
- Pagine HTML (`dashboard.html`, `films.html`, `index.html`, `login.html`, `registi.html`, `registrazione.html`, `cinemas.html`, `proiezioni.html`): rimosso blocco inline `tailwind.config` e sostituito con include condiviso `/js/tailwind-config.js`
- `wwwroot/js/pages/home.js`: normalizzazione payload film in ingresso (`array`, `items`, `$values`) prima del rendering

#### Fixed
- Accessibilita HTML: aggiunti `title`/`aria-label` ai pulsanti icon-only e nomi accessibili ai controlli `select`
- Form markup: aggiunti collegamenti `for`/`id` tra `label` e campi dove mancanti
- Inline CSS segnalato dal linter: rimosso e sostituito con classi utility
- Home cards: risolto fallback errato "Regista sconosciuto" usando i campi backend `registaNome`/`registaCognome` (con fallback su struttura annidata)

#### Verified
- Linter frontend sulle pagine HTML coinvolte: **No errors found**
- Coerenza backend/frontend verificata rispetto a `FilmDTO` (`RegistaNome`, `RegistaCognome`)

## 2026-04-06

### Iterazione 3 - Fase 7: Frontend Auth reale e token lifecycle

#### Added
- `wwwroot/js/auth.js`: gestione completa token lifecycle (login, register, logout, refresh) con salvataggio in localStorage
- `wwwroot/login.html`: pagina login con supporto `?redirect=` e `?expired=true`
- `wwwroot/js/pages/login.js`: logica login con toggle password, gestione errori, redirect post-login
- `wwwroot/registrazione.html`: pagina registrazione con validazioni client-side
- `wwwroot/js/pages/registrazione.js`: validazione email, strength password, conferma password
- `wwwroot/components/navbar-landing.html` aggiornato: voci auth-aware (mostra/nasconde login/registrati vs dropdown utente)

#### Changed
- `wwwroot/js/api.js`: aggiunto Bearer token automaticamente su tutte le richieste, retry con refresh su 401, fallback logout + redirect login
- `wwwroot/js/api.js`: aggiunti metodi API per profilo, prenotazioni, categorie, utenti admin
- Rimosso mock auth da `navbar.js` (funzioni `mockLogin`/`mockLogout` e `sessionStorage`)
- `wwwroot/js/template-loader.js`: caricamento dinamico navbar/footer landing o admin in base alla pagina (inclusi `login.html` e `registrazione.html`)
- `wwwroot/js/navbar.js`: inizializzazione navbar senza dipendenze da mock auth e integrazione con `updateAuthUI`
- `wwwroot/components/navbar-landing.html`: link Programmazione corretto a `/index.html#programmazione`; logout stilisticamente allineato alle altre voci menu
- `wwwroot/css/styles.css`: fix icone native duplicate sui campi password e classe tema `nav-menu-danger` per azioni logout
- Pagine admin (`dashboard.html`, `films.html`, `registi.html`, `cinemas.html`, `proiezioni.html`): incluso `auth.js` per coerenza auth runtime
- `wwwroot/js/api.js`: hardening accesso area admin lato frontend con redirect uniforme (anonimo -> login, ruolo insufficiente -> index)
- `wwwroot/js/pages/home.js`: toast user-friendly su redirect `?forbidden=true`

#### Verified
- Backend build: **0 Errori**
- Test backend: **97/97 PASS**
- Login/register end-to-end: flusso completo dal frontend al backend con JWT
- Token salvati correttamente in localStorage (`cb_access_token`, `cb_refresh_token`, `cb_user`)
- Chiamate protette includono Bearer token automaticamente
- Refresh token lifecycle implementato con retry automatico su 401
- Login/logout/registrazione verificati manualmente da UI: **OK**
- Redirect uniforme su accesso pagine admin non autorizzato: **OK**

## 2026-04-06

### Iterazione 3 - Fase 6: Aggiornamento e ampliamento test backend

#### Added
- `Integration/AuthIntegrationTests.cs`: suite A1-A8 (register, login, refresh, logout, me)
- `Integration/RbacIntegrationTests.cs`: suite RB1-RB8 (401/403 per ruolo, endpoint pubblici/protetti)
- `Integration/CategoriaIntegrationTests.cs`: suite CAT1-CAT5 (CRUD categorie, duplicati)
- `Integration/PrenotazioneIntegrationTests.cs`: suite PR1-PR5 (create, isolamento utente, ownership delete, admin vede tutte, not found)

#### Changed
- `Integration/CustomWebApplicationFactory.cs`: aggiunto supporto user ID configurabile (`X-Test-UserId`), email (`X-Test-Email`), nome (`X-Test-Nome`) tramite header
- `Integration/CustomWebApplicationFactory.cs`: nuovo metodo `CreateAuthenticatedClient(role, userId, email, nome)` per test con identita multiple
- `TestAuthHandler`: ora legge userId, email, nome dagli header invece di valori hardcoded

#### Verified
- Test totali: **97/97 PASS** (da 71 a 97, +26 nuovi test)
- Copertura: auth (A1-A8), RBAC (RB1-RB8), categorie (CAT1-CAT5), prenotazioni (PR1-PR5)
- Isolamento prenotazioni per utente verificato
- Ownership delete prenotazioni verificata
- Admin vede tutte le prenotazioni verificato
- Nessun test esistente rotto

## 2026-04-06

### Iterazione 3 - Fase 5: Area Personale, Prenotazioni, Gestione Utenti Admin

#### Added
- `DTO/ProfiloPrenotazioniAdminDTO.cs`: ProfiloUpdateDTO, PrenotazioneCreateDTO, PrenotazioneDTO, UserAdminDTO, UpdateRuoloDTO
- `Services/IProfiloService.cs` + `Services/ProfiloService.cs`: GetProfiloAsync, UpdateProfiloAsync
- `Services/IPrenotazioneService.cs` + `Services/PrenotazioneService.cs`: GetPrenotazioniAsync (own), GetAllPrenotazioniAsync (admin), CreatePrenotazioneAsync, DeletePrenotazioneAsync
- `Services/IUserAdminService.cs` + `Services/UserAdminService.cs`: GetAllUsersAsync, UpdateUserRoleAsync con vincolo ultimo admin
- `Endpoints/ProfiloEndpoints.cs`: GET/PUT /profilo (policy: Authenticated)
- `Endpoints/PrenotazioniEndpoints.cs`: GET/POST/DELETE /prenotazioni (User vede/modifica proprie, Admin vede tutte)
- `Endpoints/AdminUtentiEndpoints.cs`: GET /admin/utenti, PUT /admin/utenti/{id}/ruolo (policy: AdminOnly)
- Ownership check su eliminazione prenotazioni (user puo eliminare solo proprie)
- Vincolo sicurezza: impedisce degradazione dell'ultimo admin

#### Changed
- `Program.cs`: registrato DI per IProfiloService, IPrenotazioneService, IUserAdminService; mappati ProfiloEndpoints, PrenotazioniEndpoints, AdminUtentiEndpoints

#### Verified
- User vede/modifica solo dati propri: **OK**
  - due utenti distinti aggiornano `/profilo` e vedono solo i propri dati
- User gestisce solo prenotazioni proprie: **OK**
  - `GET /prenotazioni` isolato per utente
  - tentativo delete su prenotazione altrui -> `404`
- Admin vede tutte le prenotazioni e gestisce ruoli: **OK**
  - admin vede entrambe le prenotazioni create da due user diversi
  - update ruolo user -> `PowerUser` riuscito
  - tentativo degradazione ultimo admin -> `400`
- Test automatici: **71/71 PASS** (nessuna regressione)

#### Notes
- Verifica manuale end-to-end completata con successo su tutte le feature Fase 5

## 2026-04-06

### Iterazione 3 - Fase 4: Enforcement RBAC globale su tutte le API

#### Added
- Middleware `UseAuthentication()` e `UseAuthorization()` attivati in `Program.cs`
- Policy autorizzazione: `AdminOnly`, `PowerUserOrAdmin`, `Authenticated`
- CORS configurato con `WithExposedHeaders("Authorization")` per header token

#### Changed
- Endpoint auth: `AllowAnonymous` su register/login/refresh; `RequireAuthorization("Authenticated")` su logout/me
- Endpoint CRUD: applicate policy RBAC secondo matrice sezione 2.2 del PianoDiLavoro
- Endpoint films/proiezioni/categorie: GET pubblico, CUD PowerUserOrAdmin
- Endpoint cinemas: GET pubblico, CUD AdminOnly
- Endpoint registi: PowerUserOrAdmin
- Endpoint media upload: PowerUserOrAdmin

#### Verified
- 401 senza token su endpoint protetti: OK
- 403 con ruolo insufficiente: OK
- Permessi allineati alla matrice documentata

## 2026-04-06

### Iterazione 3 - Fase 3: Auth Service e endpoint autenticazione

#### Added
- `DTO/LoginRequestDTO.cs`: Email, Password (required)
- `DTO/RegisterRequestDTO.cs`: Email, Password, Nome, Cognome, Telefono (required)
- `DTO/AuthResponseDTO.cs`: AccessToken, RefreshToken, ExpiresAt, User (UserInfoDTO)
- `DTO/UserInfoDTO.cs`: Id, Email, Nome, Cognome, Telefono, Ruolo, DataRegistrazione
- `DTO/RefreshTokenRequestDTO.cs`: RefreshToken (required)
- `Services/IAuthService.cs` + `Services/AuthService.cs`: register/login/refresh/logout/me
- `Endpoints/AuthEndpoints.cs`: 5 endpoint `/auth/register|login|refresh|logout|me`
- Generazione JWT HS256 con claim `sub`, `email`, `role`, `nome`
- Refresh token rotation: revoca vecchio + generazione nuovo ad ogni refresh
- Parsing manuale JWT su `/auth/me` (senza middleware auth attivo)

#### Changed
- `Program.cs`: registrato DI `IAuthService`/`AuthService`, mappati `AuthEndpoints`

#### Fixed
- **Bug critico**: aggiunto `SaveChangesAsync()` mancante dopo `GenerateRefreshToken()` in `RegisterAsync` e `LoginAsync` di `AuthService.cs`. Il refresh token veniva restituito al client ma non persistito nel database, rendendo impossibile qualsiasi operazione di refresh.

#### Verified
- Register crea utente con ruolo User: OK
- Login ritorna coppia access + refresh token: OK
- Refresh rinnova token e revoca il precedente: OK
- Credenziali errate -> 401: OK
- Vecchio refresh token dopo refresh -> 401: OK

## 2026-04-06

### Iterazione 3 - Fase 2: Categorie CRUD e integrazione Film many-to-many

#### Added
- `DTO/CategoriaDTO.cs`: CategoriaDTO, CategoriaCreateDTO, CategoriaUpdateDTO
- `Services/ICategoriaService.cs` + `Services/CategoriaService.cs`: CRUD completo con validazione duplicati (409 Conflict)
- `Endpoints/CategorieEndpoints.cs`: 5 endpoint (GET list, GET by id, POST, PUT, DELETE) con codici 201/204/404/409
- `FilmDTO`: aggiunta proprieta `List<CategoriaDTO> Categorie`
- `FilmCreateDTO`/`FilmUpdateDTO`: aggiunta proprieta `List<int>? CategorieIds`
- `FilmService`: gestione CategorieIds in create/update con sync record ponte FilmCategoria
- Query read film (GetAll, GetPaged, GetById) includono `FilmCategorie -> Categoria`

#### Changed
- `Program.cs`: registrato DI `ICategoriaService`/`CategoriaService`, mappati `CategorieEndpoints`
- `FilmService`: riscritto con metodi helper `MapToDTO`, `MapToDTOAsync`, `SyncFilmCategorieAsync`

#### Verified
- CRUD categorie funzionante con validazione duplicati
- Film creati/aggiornati con categorie multiple
- Output film include categorie
- Test regressione: **71/71 PASS**

## 2026-04-06

### Iterazione 3 - Fase 2: Categorie CRUD e integrazione Film many-to-many

#### Added
- `DTO/CategoriaDTO.cs`: CategoriaDTO, CategoriaCreateDTO, CategoriaUpdateDTO
- `Services/ICategoriaService.cs` + `Services/CategoriaService.cs`: CRUD completo con validazione duplicati (409 Conflict)
- `Endpoints/CategorieEndpoints.cs`: 5 endpoint (GET list, GET by id, POST, PUT, DELETE) con codici 201/204/404/409
- `FilmDTO`: aggiunta proprieta `List<CategoriaDTO> Categorie`
- `FilmCreateDTO`/`FilmUpdateDTO`: aggiunta proprieta `List<int>? CategorieIds`
- `FilmService`: gestione CategorieIds in create/update con sync record ponte FilmCategoria
- Query read film (GetAll, GetPaged, GetById) includono `FilmCategorie -> Categoria`

#### Changed
- `Program.cs`: registrato DI `ICategoriaService`/`CategoriaService`, mappati `CategorieEndpoints`
- `FilmService`: riscritto con metodi helper `MapToDTO`, `MapToDTOAsync`, `SyncFilmCategorieAsync`

#### Verified
- CRUD categorie funzionante con validazione duplicati
- Film creati/aggiornati con categorie multiple
- Output film include categorie
- Test regressione: **71/71 PASS**

## 2026-04-06

### Iterazione 3 - Fase 1: Modello Dati, JWT Infrastructure, Migration e Seed

#### Added
- Package NuGet `Microsoft.AspNetCore.Authentication.JwtBearer` 9.0.11
- Package NuGet `BCrypt.Net-Next` 4.1.0
- Entita `User` con campi: Email, PasswordHash, Nome, Cognome, Telefono, Ruolo, DataRegistrazione
- Entita `RefreshToken` con campi: Token, UserId, ExpiresAt, CreatedAt, RevokedAt, computed property `IsActive`
- Entita `Prenotazione` con campi: UserId, ProiezioneId, NumeroPosti, Note, DataPrenotazione
- Entita `Categoria` con campo Nome (unique, max 100)
- Entita `FilmCategoria` con PK composita (FilmId, CategoriaId) per relazione many-to-many
- Enum `UserRole` con valori: User=0, PowerUser=1, Admin=2
- Navigation property `FilmCategorie` su entita `Film`
- `DataSeeder` per seed automatico admin e 12 categorie iniziali
- Configurazione JWT in `Program.cs` (Authentication + JwtBearer con HS256)
- Variabili environment JWT: `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_ACCESS_TOKEN_EXPIRY_MINUTES`, `JWT_REFRESH_TOKEN_EXPIRY_DAYS`
- Variabili environment admin seed: `ADMIN_SEED_EMAIL`, `ADMIN_SEED_PASSWORD`
- Migration `AddCategorieAndAuth` con tabelle: Categorie, Users, RefreshTokens, Prenotazioni, FilmCategorie
- Indici unici su: `Categoria.Nome`, `User.Email`, `RefreshToken.Token`

#### Changed
- `FilmDbContext`: aggiunti 5 nuovi DbSet, configurate relazioni con delete behaviors (Cascade/Restrict)
- `Film.cs`: aggiunta navigation property `ICollection<FilmCategoria> FilmCategorie`
- `Program.cs`: aggiunta configurazione JWT senza middleware auth attivi (preparazione per Fase 4)
- `.env` e `.env.example`: aggiunte 7 variabili per JWT e admin seed

#### Verified
- Migration `AddCategorieAndAuth` applicata con successo
- Seed admin e 12 categorie verificato su DB
- Test regressione: **71/71 PASS** (nessuna regressione)

## 2026-04-06

### Added

- Design system "Cinema Graphite" documentato in `docs/project/dev_iteration/2.2/DesignSystem.md`
- File `theme.js`: gestione tema light/dark con localStorage + system preference (`prefers-color-scheme`)
- Toggle tema nella sidebar della dashboard e nelle navbar landing/admin
- Classi CSS brand: `glass-panel`, `sidebar-glass`, `card-elevated`, `ghost-input`, `btn-gold`, `btn-gold-lg`, `btn-gold-sm`, `btn-outline-brand`, `btn-outline-brand-light`, `chip-active`, `chip-past`, `row-hover`, `btn-page`, `label-caps`, `hero-overlay`, `modal-backdrop`, `theme-toggle-btn`, `sidebar-theme-toggle`
- Sidebar flottante mobile per dashboard con hamburger button e backdrop
- `overflow-x-auto` su tutte le tabelle per scroll orizzontale mobile
- Colonna ID alla tabella proiezioni della dashboard (allineata a proiezioni.html)

### Changed

- Riscritto completamente `styles.css` con CSS custom properties per 30+ token brand
- Tema LIGHT in `:root`, tema DARK in `.dark` con palette completa
- `index.html`: hero overlay dark per leggibilità testo, testo bianco in light mode, `btn-outline-brand-light` per Programmazione
- `dashboard.html`: header responsive con bottone "+ Proiezione" abbreviato su mobile, tabella proiezioni con 6 colonne
- `films.html`, `registi.html`, `cinemas.html`, `proiezioni.html`: classi brand su card, tabelle, bottoni, modali
- `navbar-landing.html`, `navbar-admin.html`: glass-panel, theme toggle, bottoni brand
- `footer-landing.html`, `footer-admin.html`: brand surface colors
- `js/navbar.js`: active links con `text-brand-gold`
- `js/pages/home.js`, `films.js`, `registi.js`, `cinemas.js`, `proiezioni.js`: template con brand tokens
- Rimossi `bg-white` hardcoded da tbody e modali, sostituiti con brand surface tokens
- Bottoni "Salva" modali: rimosso padding override `px-4 py-2`, usano padding integrato di `btn-gold`

### Fixed

- Select dropdown dark mode: frecce duplicate rimosse con `appearance: none !important`
- Hero overlay light mode: da near-white a dark per leggibilità testo
- Hero text accent "Gestita in un Click": colore oro brillante `#D4AF37` in dark mode con `!important`
- Dashboard mobile: sidebar fixed fuori dal document flow, contenuto full-width
- Dashboard mobile: hamburger menu funzionante con `initializeNavbar()` chiamato dopo caricamento componente
- Dashboard mobile: tabella proiezioni scrollabile orizzontalmente
- `btn-gold` dark mode: testo da bianco a `#0A0A12` per contrasto su oro
- `btn-outline-brand-light`: nuova variante con bordo e testo bianco per uso su overlay scuri

## 2026-03-29

### Added

- Endpoint backend `POST /media/covers` per upload copertine immagini
- Servizio `MediaService` con validazioni MIME, estensione, dimensione (max 5MB)
- DTO `MediaUploadResultDTO` per risposta upload (path, fileName, contentType, size)
- Metodo `uploadCover(file)` in `api.js` per multipart upload lato frontend
- Test integration per upload media:
  - `M1_UploadCover_ReturnsOk_WithValidImage`
  - `M2_UploadCover_ReturnsBadRequest_WhenNoFile`
  - `M3_UploadCover_ReturnsBadRequest_WhenUnsupportedMimeType`
- Test integration per validazione filmatoPath:
  - `F9_PostFilms_ReturnsBadRequest_WhenFilmatoPathIsInvalidUrl`
  - `F10_PostFilms_AcceptsValidFilmatoUrl`

### Changed

- Configurato static file serving in backend (`app.UseStaticFiles()`)
- Aggiunta validazione `filmatoPath` come URL assoluto http/https in `FilmService`
- Aggiornato `films.html`: campo input file per copertina + campo Trailer URL
- Aggiornato `films.js`: upload copertina integrato nel flusso create/update film
- Aggiornato `CustomWebApplicationFactory` per supportare WebRoot nei test
- Aggiornati test esistenti per rimuovere `filmatoPath` con URL non validi

### Fixed

- Rendering immagini copertina caricati da backend (path `/media/*` risolti verso `http://localhost:5000`)
- Gestione file mancante in endpoint upload con messaggio BadRequest appropriato
- Fallback WebRoot in `MediaService` quando non configurato nell'ambiente

## 2026-03-18

### Added

- Nuovo progetto frontend `CineBase.Web` con struttura `wwwroot` completa.
- Pagine frontend: `index.html`, `dashboard.html`, `registi.html`, `films.html`, `cinemas.html`, `proiezioni.html`.
- Componenti riusabili: navbar/footer admin e landing.
- Moduli JS base: `api.js`, `utils.js`, `template-loader.js`, `form-handlers.js`, `navbar.js`.
- File environment frontend: `frontend/CineBase.Web/.env` e `frontend/CineBase.Web/.env.example`.
- File environment backend: `backend/.env` e `backend/.env.example`.
- Piano Iterazione 2.1: `docs/project/dev_iteration/2.1/PianoLavoro.md` con specifiche complete per media upload copertine + trailer URL.
- Modal proiezione aggiunto alla dashboard.

### Changed

- Repository riorganizzato in cartelle top-level `frontend/`, `backend/`, `tests/`, con `docs/` mantenuta top-level.
- Progetto backend annidato in `backend/FilmAPI/` per simmetria con `frontend/CineBase.Web/`.
- Progetto test backend spostato in `tests/backend/`.
- Configurazione porte rimossa dal codice (`Program.cs`) e gestita via environment (`ASPNETCORE_URLS`) + launch settings.
- Aggiornata configurazione CORS in backend per richieste da `http://localhost:5001`.
- Corretto `FilmAPI.csproj` per evitare inclusione dei file C# del progetto frontend annidato.
- Uniformato routing endpoint Minimal API su route di gruppci (`MapGet("")`, `MapPost("")`).
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
- Test suite backend: `71` test passati su `71` (`tests/backend/FilmAPI.Tests.csproj`).
