# Stato Progetto

Data aggiornamento: 2026-04-19

## Branch di lavoro
- `dev_iteration_4` (iterazione 4 - cinema multisala e ticketing)

## Stato Iterazione 4

| Fase | Stato | Data | Note |
| --- | --- | --- | --- |
| FASE 1 - Modello dati v2 e compat layer | **Completata** | 2026-04-16 | - |
| FASE 2 - Migration, seed e data migration legacy | **Completata** | 2026-04-16 | Migration `AddMultisalaTicketing` applicata con data migration legacy, seed dev esteso, conflitti sala gestiti |
| FASE 3 - Backend catalogo pubblico, scheda film e cinema preferito | **Completata** | 2026-04-16 | DTO film estesi, endpoint programmazione, scheda film, my-cinemas, cinema preferito, 20 test integrazione |
| FASE 4 - Backend sale e piantina posti | **Completata** | 2026-04-17 | DTO, service, endpoint CRUD Sala, gestione piantina SalaPosto, validazioni, 20 test integrazione |
| FASE 5 - Backend show e bridge legacy proiezioni | **Completata** | 2026-04-17 | DTO, service, endpoint ShowsEndpoints, anti-overlap, bridge proiezioni, 38 test |
| FASE 6 - Backend seat map, hold posti e ordine pendente | **Completata** | 2026-04-17 | DTO checkout, SeatHoldService su ShowPostoStato, CheckoutService, 7 endpoint, background cleanup, limite 10 posti, 20 test (inclusi concorrenza) |
| FASE 7 - Backend pagamento, credito piattaforma e finalizzazione checkout | **Completata** | 2026-04-18 | Stripe.net integrato, finalizzazione ordine idempotente, webhook replay-safe, credito piattaforma e liste ordini/ticket |
| FASE 8 - Backend ticketing digitale, PDF/email e validazione biglietti | **Completata** | 2026-04-18 | Ticketing digitale backend completato: emissione ticket, PDF multipagina, email SMTP provider-agnostic, download PDF ordine, validazione biglietti, 6 test integrazione e smoke test SMTP reale |
| FASE 9 - Frontend `programmazione.html` v2 + modale scelta cinema | **Completata** | 2026-04-19 | Pagina film-centric con tabs, search, filtro categoria, modale cinema, persistenza cinema preferito, affinamenti UX/performance (geolocalizzazione non bloccante, caroselli, load more) e paginazione backend |
| FASE 9.1 - Progetto `FilmApiSeeder` e seed realistico database | **Completata** | 2026-04-18 | Creato progetto console standalone sotto `backend/scripts/FilmApiSeeder`, integrato con `backend/.env`, TMDB, reset sicuri, 64 film, 20 cinema, 83 sale, show e piantine posti verificati |
| FASE 10 - Frontend `scheda-film.html` + `my-cinemas.html` | **Completata** | 2026-04-19 | Rail date orizzontale riusabile, show raggruppati per tipologia sala, bottoni orario auth-aware, route guard e template loader aggiornati |
| FASE 11 - Frontend `acquista.html`, `pagamento.html`, `esito-acquisto.html`, `profilo.html` v2 | **Completata** | 2026-04-19 | Seat-map interattiva con countdown, keep-alive, layout desktop compatto e zoom avanzato; Stripe Elements embedded con pagamento credito/carta/misto, finalizzazione backend post-Stripe, annullamento ordine pendente, prezzi coerenti lato backend e profilo evoluto al nuovo dominio |
| FASE 11.1 - Migrazione da Stripe Elements a Stripe Checkout hosted | **Completata** | 2026-04-19 | Checkout Session hosted con webhook come source of truth, supporto robusto a credito piattaforma, pagamento misto con credito riservato/rilasciato, cleanup automatico ordini hosted scaduti, riconciliazione backend al ritorno da Stripe e 13 test integrazione dedicati |
| FASE 12 - Frontend admin: sale, show, ricarica credito, validazione ticket | Pending | - | - |
| FASE 13 - Test finali, cleanup legacy, hardening e documentazione | Pending | - | - |

## Stato generale
- Iterazione 2.1 media upload: **completata**
- Iterazione 2.2 UI redesign (Stitch design system): **completata**
- Iterazione 3 Fase 1 (Modello Dati, JWT, Migration, Seed): **completata**
- Iterazione 3 Fase 2 (Categorie CRUD + Film many-to-many): **completata**
- Iterazione 3 Fase 3 (Auth Service e endpoint autenticazione): **completata**
- Iterazione 3 Fase 4 (Enforcement RBAC globale): **completata**
- Iterazione 3 Fase 5 (Area Personale, Prenotazioni, Gestione Utenti Admin): **completata**
- Iterazione 3 Fase 6 (Aggiornamento e ampliamento test backend): **completata**
- Iterazione 3 Fase 7 (Frontend Auth reale e token lifecycle): **completata**
- Iterazione 3 Fase 8 (Route guard e navigazione per ruolo): **completata**
- Iterazione 3 Fase 9 (Programmazione pubblica + gestione categorie admin): **completata**
- Iterazione 3 Fase 10 (Area Personale utente - profilo + prenotazioni): **completata**
- Iterazione 3 Fase 11 (Verifica finale, hardening e documentazione): **completata**
- Backend API: **stabile**, 231/231 test automatici verdi
- Backend pagamento, credito e ticketing digitale: **stabile**, PDF/email e validazione biglietti operativi, invio SMTP reale collaudato
- Frontend: **stabile**, dual-theme light/dark funzionante, responsive mobile, auth reale con token lifecycle, route guard attivi e navigazione role-aware, programmazione pubblica film-centric con caroselli e paginazione, pagine `scheda-film.html` e `my-cinemas.html` operative, flusso acquisto completo (`acquista.html` → `pagamento.html` → `esito-acquisto.html`) operativo con seat-map interattiva, countdown, keep-alive, sidebar sticky, zoom `+`/`-`/`Reset`, supporto `Ctrl + wheel` o pinch-trackpad, Stripe Checkout hosted per carta e misto, pagamento solo credito lato backend, riconciliazione ordine al ritorno da Stripe, annullamento ordine pendente e riepilogo ordine/ticket, `profilo.html` evoluto al nuovo dominio ordini/biglietti/credito/cinema preferito, listing admin con ricerca + paginazione server-side
- RBAC backend/frontend: **verificato**, tutti i redirect coerenti con matrice permessi
- Prossima fase pianificata: **FASE 12**, completamento frontend admin per sale, show, ricarica credito e validazione ticket

## Completato in sessione 2026-04-19 (Iterazione 4 — Fase 11.1: Stripe Checkout hosted, credito riservato e hardening finale)

### Backend — Checkout hosted e riconciliazione
- `backend/FilmAPI/Model/OrdineState.cs`: aggiunto stato `CheckoutInProgress`
- `backend/FilmAPI/Model/Ordine.cs`: aggiunti `StripeCheckoutSessionId`, `CheckoutExpiresAtUtc`, `CheckoutCompletedAtUtc`, `LastPaymentError`, `CreditoRiservato`
- `backend/FilmAPI/Services/StripeGateway.cs`: creazione/lettura `Checkout Session` hosted e parsing webhook `checkout.session.*`
- `backend/FilmAPI/Services/PagamentoService.cs`: creazione sessione Stripe hosted, pagamento misto con credito riservato, riconciliazione backend, finalizzazione idempotente da webhook/session status, rilascio credito su expire/cancel, fix handling `payment_intent.failed/canceled` durante sessione hosted aperta
- `backend/FilmAPI/Services/CreditoService.cs`: aggiunti metodi `ReserveOrderCreditAsync` e `ReleaseReservedOrderCreditAsync`
- `backend/FilmAPI/Services/ExpiredHoldCleanupService.cs`: esteso per scadere automaticamente anche ordini `CheckoutInProgress` e rilasciare il credito riservato
- `backend/FilmAPI/Endpoints/CheckoutEndpoints.cs`: aggiunti `POST /checkout/orders/{orderId}/stripe-checkout-session`, `GET /checkout/orders/{orderId}/checkout-status`, `POST /checkout/orders/{orderId}/reconcile-checkout-session`

### Frontend — Pagamento ed esito
- `frontend/CineBase.Web/wwwroot/js/pages/pagamento.js`: rimosso flusso `Stripe Elements`; carta e misto ora creano `Checkout Session` e reindirizzano a Stripe hosted, solo credito resta backend-only
- `frontend/CineBase.Web/wwwroot/pagamento.html`: rimossa form embedded Stripe, aggiunta UX informativa sul redirect hosted
- `frontend/CineBase.Web/wwwroot/js/pages/esito-acquisto.js`: riconciliazione automatica al ritorno da Stripe con `?success=true`, polling backend fino a convergenza ordine, fix listener duplicati sul download PDF
- `frontend/CineBase.Web/wwwroot/js/api.js`: aggiunti metodi API `createStripeCheckoutSession`, `getCheckoutStatus`, `reconcileCheckoutSession`
- `frontend/CineBase.Web/wwwroot/pagamento.html` e `esito-acquisto.html`: aggiunto cache-busting querystring sugli asset JS coinvolti nel nuovo flusso

### Test e verifiche
- `tests/backend/Integration/CheckoutHostedIntegrationTests.cs`: aggiunti 13 test dedicati per sessione hosted carta/misto, webhook completed/expired, riconciliazione, duplicate webhook, cancel, rilascio credito, no double debit e regressioni `payment_intent.failed/canceled`
- `tests/backend/Integration/CustomWebApplicationFactory.cs`: fake Stripe esteso con `Checkout Session` hosted
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **231/231 PASS**
- verifica manuale reale del flusso hosted: **OK** per solo carta e per misto credito + carta

## Pianificato in sessione 2026-04-19 (Iterazione 4 — Fase 11.1: migrazione da Stripe Elements a Stripe Checkout hosted)

### Ambito pianificato
- migrazione del flusso pagamento carta da embedded `Stripe Elements` a pagina hosted `Stripe Checkout`
- mantenimento del backend come source of truth per importi, stato ordine, stato posti e finalizzazione economica
- pagamento solo credito senza invocare Stripe quando il saldo è sufficiente
- pagamento misto con quota credito riservata e quota carta residua su Stripe Checkout

### Vincoli pianificati
- il redirect browser di ritorno da Stripe non varrà come prova di pagamento riuscito; la conferma principale arriverà da webhook verificato e riconciliazione backend
- i posti dovranno restare bloccati durante il checkout hosted tramite lock d'ordine con scadenza backend, senza dipendere dal keep-alive frontend
- cancel, expire e webhook duplicati dovranno essere gestiti in modo idempotente con rilascio posti e restore credito riservato

### Documentazione pianificata
- `docs/project/dev_iteration/4/PianoDiLavoro.md`: aggiunta `FASE 11.1` con attività, vincoli implementativi e criteri di accettazione prescrittivi
- `docs/tutorials/TUTORIAL_STRIPE_ELEMENTS_VS_CHECKOUT_CINEBASE.md`: nuovo tutorial comparativo tra flusso attuale `Stripe Elements` e flusso target `Stripe Checkout` con snippet completi frontend/backend

## Completato in sessione 2026-04-19 (Iterazione 4 — Fase 11 refinement: checkout reale, Stripe runtime config e fix prezzi)

### Backend — Checkout, pagamento e configurazione runtime
- `backend/FilmAPI/Program.cs`: bootstrap `.env` corretto per caricare in modo affidabile `backend/.env` e aggiunto endpoint `GET /config/frontend` per esporre al frontend la publishable key Stripe runtime
- `backend/FilmAPI/Services/PagamentoService.cs` + `backend/FilmAPI/Services/IPagamentoService.cs`: aggiunta cancellazione ordine `Pending` con rilascio posti e consolidata la finalizzazione del pagamento nel flusso reale post-Stripe
- `backend/FilmAPI/Endpoints/CheckoutEndpoints.cs`: aggiunto `POST /checkout/orders/{orderId}/cancel` per annullare un ordine pendente e liberare i posti

### Backend — Prezzi coerenti lato server
- `backend/FilmAPI/Services/TicketPriceNormalizer.cs`: nuovo helper centralizzato per normalizzare prezzi show/ticket espressi in formato valido anche quando i seed arrivano in centesimi
- `backend/FilmAPI/Data/DataSeeder.cs` + `backend/scripts/FilmApiSeeder/Program.cs`: parsing robusto di `DEFAULT_TICKET_PRICE` e correzione del seed prezzi, evitando workaround lato frontend
- `backend/FilmAPI/Services/ShowService.cs`, `SeatHoldService.cs`, `CheckoutService.cs`, `BigliettoService.cs`: allineati a usare prezzi backend coerenti come source of truth

### Frontend — Acquisto, pagamento ed esperienza seat-map
- `frontend/CineBase.Web/wwwroot/acquista.html` + `wwwroot/js/pages/acquista.js`: nuova pagina acquisto con seat-map a blocchi, countdown, keep-alive, polling, sidebar sticky e controlli zoom desktop `+`/`-`/`Reset` con supporto `Ctrl + wheel` o pinch-trackpad
- `frontend/CineBase.Web/wwwroot/pagamento.html` + `wwwroot/js/pages/pagamento.js`: Stripe Elements embedded, pagamento solo credito, solo carta o misto, recupero publishable key solo da `/config/frontend`, finalizzazione backend dopo `stripe.confirmCardPayment(...)` e annullamento ordine pendente dal bottone di ritorno
- `frontend/CineBase.Web/wwwroot/esito-acquisto.html` + `wwwroot/js/pages/esito-acquisto.js`: esito finale ordine con riepilogo ticket
- `frontend/CineBase.Web/wwwroot/profilo.html` + `wwwroot/js/pages/profilo.js`: area personale aggiornata a ordini, biglietti, credito e cinema preferito
- `frontend/CineBase.Web/wwwroot/js/api.js`, `route-guard.js`, `template-loader.js`, `css/styles.css`: supporto alle nuove pagine e al nuovo layout checkout
- `frontend/CineBase.Web/.env.example`: rimosso per evitare duplicazione della configurazione Stripe lato frontend

### Test e verifiche
- `tests/backend/Integration/ShowIntegrationTests.cs`: aggiunto test di normalizzazione prezzo show (`SH8B`)
- `tests/backend/Integration/PagamentoCreditoIntegrationTests.cs`: aggiunto test annullamento ordine pendente con rilascio posti (`PG7`)
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **218/218 PASS**
- Build frontend `CineBase.Web`: **OK**

## Completato in sessione 2026-04-19 (Iterazione 4 — Fase 10: Frontend `scheda-film.html` + `my-cinemas.html`)

### Frontend — Nuove pagine pubbliche
- `wwwroot/scheda-film.html`: pagina dettaglio film con hero, metadati, descrizione, cast, rail date orizzontale e gruppi show per tipologia sala
- `wwwroot/js/pages/scheda-film.js`: caricamento scheda via `GET /films/{id}/scheda?cinemaId=`, gestione cinema selezionato, date locali e bottoni orario auth-aware con redirect a login o acquisto
- `wwwroot/my-cinemas.html`: pagina cinema-centric con vista elenco cinema e vista dettaglio `?IdCinema=`
- `wwwroot/js/pages/my-cinemas.js`: caricamento cinema da `GET /my-cinemas`, dettaglio giornaliero da `GET /my-cinemas/{cinemaId}/schedule?date=`, rail date e rendering programmazione per film e tipologia sala
- `wwwroot/js/date-rail.js`: componente riusabile per rail date orizzontale condiviso tra scheda film e my-cinemas

### Frontend — Routing e integrazioni
- `wwwroot/js/api.js`: aggiunti metodi `getFilmScheda`, `getMyCinemas`, `getCinemaSchedule`
- `wwwroot/js/route-guard.js`: aggiunte `/scheda-film.html` e `/my-cinemas.html` come pagine pubbliche
- `wwwroot/js/template-loader.js`: aggiunte le nuove pagine ai landing paths
- `wwwroot/css/styles.css`: aggiunti stili per rail date, badge tipologia sala, bottoni orario e schedule cards

### Verifiche
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **216/216 PASS**
- Build frontend `CineBase.Web`: **OK**

## Completato in sessione 2026-04-19 (Iterazione 4 — Fase 9 refinement: programmazione, paginazione e tutorial)

### Backend — Programmazione pubblica
- `backend/FilmAPI/DTO/ProgrammazioneDTO.cs`: introdotto `ProgrammazioneFilmPagedResultDTO` per il payload paginato del listing film
- `backend/FilmAPI/Endpoints/ProgrammazioneEndpoints.cs`: `GET /programmazione/films` esteso con `page` e `pageSize`
- `backend/FilmAPI/Services/IProgrammazioneService.cs` + `backend/FilmAPI/Services/ProgrammazioneService.cs`: listing film reso realmente paginato e logica `In uscita` corretta per escludere film con show già attivi oggi o già disponibili nel cinema selezionato

### Frontend — Programmazione UX/performance
- `wwwroot/programmazione.html` + `wwwroot/js/pages/programmazione.js`: tabs rifinite con caroselli orizzontali per `In evidenza` e `In uscita`, caricamento incrementale, `Carica altri film` per `Tutti i film`, contatore elementi e frecce contestuali
- geolocalizzazione resa non bloccante rispetto al caricamento iniziale dei film
- caricamento film allineato alla paginazione backend con page size differenziata per grid e caroselli

### Test e documentazione
- `tests/backend/Integration/ProgrammazioneIntegrationTests.cs`: aggiunti test per paginazione `GET /programmazione/films` e per la logica corretta della tab `In uscita`
- `docs/tutorials/TUTORIAL_INDEX_PROGRAMMAZIONE_FRONTEND_CINEBASE.md`: nuovo tutorial dettagliato in italiano su `index.html`, `home.js`, `programmazione.html`, geolocalizzazione, cinema preferito, caroselli, card film, filtri e paginazione

### Verifiche
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **216/216 PASS**

## Completato in sessione 2026-04-18 (Iterazione 4 — Fase 9: Frontend programmazione.html v2 + modale scelta cinema)

### Frontend — Programmazione v2 (film-centric)
- `wwwroot/programmazione.html`: completamente riprogettata con:
  - header cinema selezionato (nome, citta, indirizzo) con bottone "Cambia cinema"
  - tabs "In evidenza", "In uscita", "Tutti i film" con stato attivo
  - search input per titolo con debounce 300ms
  - filtro categoria dropdown
  - griglia card film responsive (1-4 colonne)
  - stati empty: nessun cinema selezionato, nessun film trovato, caricamento
  - modale selezione cinema con search, lista cinema con tipologie e distanza
- `wwwroot/js/pages/programmazione.js`: riscritto completamente con:
  - `CinemaManager` per persistenza cinema preferito:
    - anonimo: `localStorage` chiave `cb_selected_cinema`
    - autenticato: `GET/PUT /profilo/cinema-preferito`
    - sincronizzazione bidirezionale al caricamento pagina
  - caricamento film da `GET /programmazione/films` con tab/search/categoria/cinemaId
  - rendering card film con copertina, titolo, durata, categorie, indicatore disponibilita
  - navigazione a `scheda-film.html?id=X&cinema=Y` al click su card
  - geolocalizzazione browser per ordinamento cinema per distanza
  - evento custom `cinema:changed` per sincronizzazione componenti
- `wwwroot/components/navbar-landing.html`: aggiunto indicatore cinema selezionato (desktop badge + mobile section)
- `wwwroot/js/api.js`: aggiunti metodi `getProgrammazioneFilms`, `getProgrammazioneCinemas`, `getCinemaPreferito`, `setCinemaPreferito`
- `wwwroot/css/styles.css`: aggiunti stili per tabs, line-clamp, modal scrollbar

### Verifiche
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **213/213 PASS**
- Build frontend `CineBase.Web`: **OK**

## Completato in sessione 2026-04-18 (Iterazione 4 — Fase 9.1: Progetto `FilmApiSeeder` e seed realistico database)

### Backend — Seeder standalone condiviso con `FilmAPI`
- `backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj`: creato progetto console dedicato al popolamento database, referenziato a `FilmAPI`
- `backend/scripts/FilmApiSeeder/Program.cs`: implementato orchestratore di seed con supporto opzioni CLI, reset sicuri (`--reset-shows`, `--reset-all`, `--force`) e lettura config da `backend/.env`
- `backend/scripts/FilmApiSeeder/TmdbClient.cs`: integrazione con API TMDB per ricerca film, dettagli, crediti, registi e URL copertine
- `backend/scripts/FilmApiSeeder/SeedCatalog.cs`: catalogo seed per film target, categorie, cinema italiani e tipologie sala
- `backend/scripts/FilmApiSeeder/README.md`: documentazione operativa del progetto, configurazione, comandi e finalita

### Configurazione e struttura repository
- `backend/.env.example`: consolidato come file environment condiviso tra backend API e seeder
- `backend/FilmAPI/Program.cs`: aggiornato per caricare `backend/.env`
- progetto `FilmApiSeeder` spostato in `backend/scripts/FilmApiSeeder` per allineamento con il backend e aggiunto alla solution Visual Studio

### Dati seedati e verifiche operative
- esecuzione reale del seeder completata con successo: **64 film**, **20 cinema**, **83 sale**
- verificate copertine TMDB reali su tutti i film seedati
- verificate piantine `SalaPosti` persistite con coordinate, settori e posti wheelchair
- verificata generazione programmazione show e reset della sola programmazione con `--reset-shows --force`
- verificata esposizione dei dati via endpoint backend usati da home e programmazione pubblica

### Verifiche
- `dotnet run --project backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj`: **OK**
- `dotnet run --project backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj -- --reset-shows --force`: **OK**
- `dotnet build backend/FilmAPI/FilmAPI.csproj /p:UseAppHost=false`: **OK**
- query manuali su database e smoke test HTTP su frontend/backend: **OK**

## Completato in sessione 2026-04-18 (Iterazione 4 — Fase 8: Backend ticketing digitale, PDF/email e validazione biglietti)

### Backend — Ticketing digitale
- `backend/FilmAPI/Services/IBigliettoService.cs` + `backend/FilmAPI/Services/BigliettoService.cs`: emissione ticket per ordine pagato, codici `CB-...` univoci, read model ordine/ticket per PDF, email e validazione
- `backend/FilmAPI/Services/IPdfService.cs` + `backend/FilmAPI/Services/PdfService.cs`: PDF multipagina con un biglietto per pagina, QR code, barcode grafico e formattazione importi `it-IT`
- `backend/FilmAPI/Services/IEmailService.cs` + `backend/FilmAPI/Services/EmailService.cs`: invio SMTP provider-agnostic con corpo HTML/text e PDF allegato, usando `SMTP_*`
- `backend/FilmAPI/Services/IValidazioneBigliettoService.cs` + `backend/FilmAPI/Services/ValidazioneBigliettoService.cs`: lookup ticket, validazione auditata, blocco doppia validazione e mismatch cinema operativo

### Backend — Endpoint e integrazione ordine
- `backend/FilmAPI/Endpoints/CheckoutEndpoints.cs`: nuovo `GET /checkout/orders/{orderId}/pdf`
- `backend/FilmAPI/Endpoints/ValidazioneBigliettiEndpoints.cs`: `GET /admin/tickets/validate/{code}` e `POST /admin/tickets/validate`
- `backend/FilmAPI/Services/PagamentoService.cs`: emissione ticket e tentativo invio email come attività post-pagamento senza rollback dell'ordine pagato
- `backend/FilmAPI/DTO/BigliettoDTO.cs` e `backend/FilmAPI/DTO/CheckoutDTO.cs`: estesi con dati validazione, PDF ed esito invio email
- `backend/FilmAPI/Program.cs`: registrati nuovi service, endpoint ticket validation e licenza `QuestPDF` community
- `backend/FilmAPI/FilmAPI.csproj`: aggiunti `MailKit`, `QRCoder`, `QuestPDF`, `ZXing.Net`

### Test
- `tests/backend/Integration/TicketIntegrationTests.cs`: emissione ticket, invio email fake con PDF allegato, download PDF con contenuti richiesti e ownership check
- `tests/backend/Integration/ValidazioneTicketIntegrationTests.cs`: lookup ticket, doppia validazione bloccata, validazione con cinema errato bloccata
- `tests/backend/Integration/CustomWebApplicationFactory.cs` + `tests/backend/FilmAPI.Tests.csproj`: fake `IEmailService` e verifica contenuti PDF tramite `PdfPig`

### Verifiche
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet build tests/backend/FilmAPI.Tests.csproj`: **OK**
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **213/213 PASS**
- smoke test SMTP reale con configurazione locale `SMTP_*` e invio ticket finto: **OK**

## Completato in sessione 2026-04-18 (Documentazione Fase 8 — ticketing digitale, email SMTP Google e Twilio SendGrid)

### Documentazione — Nuovi tutorial
- `docs/tutorials/TUTORIAL_FASE8_STRATEGIA_TICKETING_EMAIL_PDF_VALIDAZIONE.md`: strategia tecnica e didattica della Fase 8, con flussi, servizi coinvolti e diagrammi Mermaid
- `docs/tutorials/TUTORIAL_EMAIL_MAILKIT_BIGLIETTI_PDF_QRCODE.md`: tutorial didattico su `MailKit`, email HTML con allegati, `QuestPDF`, `QRCoder` e ciclo completo del biglietto digitale
- `docs/tutorials/TUTORIAL_SMTP_GOOGLE_TWILIO_SENDGRID_SETUP_E_TROUBLESHOOTING.md`: guida operativa passo passo per configurazione SMTP Google e `Twilio SendGrid`, collaudo e troubleshooting

### Documentazione — File aggiornati
- `docs/project/dev_iteration/4/PianoDiLavoro.md`: Fase 8 estesa con riferimenti operativi, strategia approvata Google SMTP baseline + compatibilità `Twilio SendGrid`, e sezione environment aggiornata
- `backend/.env.example`: aggiunti esempi commentati e parametri guida per Google SMTP e `Twilio SendGrid` SMTP relay

### Configurazione e strategia
- baseline operativa approvata per la Fase 8: server SMTP di Google
- requisito architetturale esplicitato: servizio email non accoppiato rigidamente a Google e pronto a funzionare anche con `Twilio SendGrid`
- documentazione aggiornata con riferimenti ufficiali Google, Microsoft e Twilio verificati manualmente durante la sessione

### Verifiche
- controllo manuale di coerenza tra tutorial, piano di lavoro e `backend/.env.example`: **OK**
- nessun test automatico eseguito, perché le modifiche della sessione sono documentali e di configurazione esempio

## Completato in sessione 2026-04-18 (Iterazione 4 — Fase 7: Backend pagamento, credito piattaforma e finalizzazione checkout)

### Backend — Pagamento e credito
- `Services/PagamentoService.cs`: finalizzazione ordine con pagamento carta, credito o misto, ricalcolo totale lato backend e idempotenza
- `Services/StripeGateway.cs`: astrazione su `Stripe.net` per creazione/verifica `PaymentIntent`
- `Services/CreditoService.cs`: saldo utente, ricariche admin e audit tramite `MovimentoCredito`

### Backend — Endpoint
- `Endpoints/PagamentoEndpoints.cs`: `POST /checkout/orders/{orderId}/pay` e `POST /payments/stripe/webhook`
- `Endpoints/CreditoEndpoints.cs`: `GET /credito/me`, `GET/POST /admin/credito/...`
- `Endpoints/CheckoutEndpoints.cs`: liste ordini e ticket per profilo utente

### Test
- `tests/backend/Integration/PagamentoCreditoIntegrationTests.cs`: carta, credito, misto, saldo insufficiente, webhook replay-safe, ricarica credito admin

### Verifiche
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **207/207 PASS**
- Pagamento carta reale in test mode verificato con `tok_visa`
- Webhook reale verificato con `Stripe CLI`

## Completato in sessione 2026-04-16 (Iterazione 4 — Fase 1: Modello dati v2 e compat layer)

### Backend — Nuovi enum (5 file)
- `Model/TipoSala.cs`: enum con DueD=0, TreD=1, ISENSE=2, XL=3
- `Model/ShowPostoState.cs`: enum con Hold=0, Sold=1
- `Model/OrdineState.cs`: enum con Pending=0, Paid=1, Failed=2, Cancelled=3, Expired=4
- `Model/BigliettoState.cs`: enum con Issued=0, Validated=1, Cancelled=2
- `Model/MovimentoCreditoTipo.cs`: enum con TopUp=0, DebitOrder=1, Refund=2, Adjustment=3

### Backend — Nuove entità (7 file)
- `Model/Sala.cs`: sala cinema con CinemaId, NumeroProgressivo, TipoSala, Nome, Supplemento, IsAttiva + navigazioni Posti, Shows
- `Model/SalaPosto.cs`: posto con SalaId, Settore, Fila, Numero, PosX, PosY, IsWheelchair, IsAttivo
- `Model/Show.cs`: spettacolo con CinemaId, SalaId, FilmId, StartAtUtc, DurataMinutiSnapshot, PrezzoBase, SupplementoSala + navigazioni
- `Model/ShowPostoStato.cs`: stato posto-show con ShowId, SalaPostoId, UserId, Stato, HoldToken, ScadeAtUtc, OrdineId, UpdatedAtUtc
- `Model/Ordine.cs`: ordine con CodiceOrdine, UserId, ShowId, CinemaId, SalaId, FilmId, HoldToken, NumeroBiglietti, TotaleLordo, ImportoCredito, ImportoCarta, StripePaymentIntentId, IdempotencyKey, Stato, CreatedAtUtc, PaidAtUtc, TicketEmailSentAtUtc, TicketEmailLastError + navigazione Biglietti
- `Model/Biglietto.cs`: biglietto con OrdineId, ShowId, SalaPostoId, UserId, CodiceBiglietto, BarcodeValue, PrezzoBase, Supplemento, PrezzoTotale, Stato, ValidatoAtUtc, ValidatoDaUserId, ValidatoCinemaId
- `Model/MovimentoCredito.cs`: movimento credito con UserId, Tipo, Importo, SaldoPre, SaldoPost, OperatoreUserId, CinemaId, OrdineId, CreatedAtUtc, Note

### Backend — Entità estese
- `Film.cs`: aggiunto DescrizioneLunga, CastText, DataRilascio (DateOnly?), navigazione ICollection<Show> Shows
- `Cinema.cs`: aggiunto Latitudine, Longitudine, Telefono, CodiceLocale, navigazione ICollection<Sala> Sale
- `User.cs`: aggiunto CinemaPreferitoId (FK nullable), CreditoResiduo, navigazioni ICollection<Ordine> Ordini, ICollection<Biglietto> Biglietti

### Backend — FilmDbContext aggiornato
- Aggiunti DbSet: Sale, SalaPosti, Shows, ShowPostiStato, Ordini, Biglietti, MovimentiCredito
- Configurati indici unici:
  - Sala: (CinemaId, NumeroProgressivo)
  - SalaPosto: (SalaId, Settore, Fila, Numero)
  - Show: (CinemaId, SalaId, StartAtUtc)
  - ShowPostoStato: (ShowId, SalaPostoId), HoldToken, ScadeAtUtc
  - Ordine: CodiceOrdine, IdempotencyKey
  - Biglietto: (ShowId, SalaPostoId), CodiceBiglietto
- Configurati delete behavior secondo matrice del piano
- `Proiezione` e `Prenotazione` mantenute (non rimosse)

### Backend — Configurazione aggiornata
- `.env.example`: aggiunti placeholder per Stripe (STRIPE_API_KEY, STRIPE_WEBHOOK_SECRET), SMTP (SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASSWORD, SMTP_FROM_EMAIL, SMTP_FROM_NAME), ticketing (DEFAULT_TICKET_PRICE, HOLD_TTL_MINUTES, MAX_SEATS_PER_ORDER), URL applicativo (FRONTEND_BASE_URL, TICKET_VALIDATION_BASE_URL)

### Verifiche
- Build backend `FilmAPI`: **OK**
- Build test `FilmAPI.Tests`: **OK**
- Proiezione e Prenotazione legacy: **ancora presenti** (non rimosse)

## Completato in sessione 2026-04-17 (Refactor backend: separazione interfacce/classi nei Services)

### Backend — Refactor struttura Services
- Applicata best practice "one type per file" a tutti i service che avevano interfaccia e classe nello stesso file
- Creati 8 nuovi file interfaccia:
  - `Services/IFilmService.cs`
  - `Services/ICategoriaService.cs`
  - `Services/ICinemaService.cs`
  - `Services/IMediaService.cs`
  - `Services/IProgrammazioneService.cs`
  - `Services/IProiezioneService.cs`
  - `Services/IRegistaService.cs`
  - `Services/ISalaService.cs`
- Rimossi dalle classi le definizioni interfaccia inline:
  - `Services/FilmService.cs`
  - `Services/CategoriaService.cs`
  - `Services/CinemaService.cs`
  - `Services/MediaService.cs`
  - `Services/ProgrammazioneService.cs`
  - `Services/ProiezioneService.cs`
  - `Services/RegistaService.cs`
  - `Services/SalaService.cs`
- Eccezioni valide mantenute (nessuna interfaccia custom):
  - `Services/RefreshTokenCleanupService.cs` (estende `BackgroundService`)
- File gia conformi (nessuna modifica necessaria):
  - `AuthService.cs` / `IAuthService.cs`
  - `PrenotazioneService.cs` / `IPrenotazioneService.cs`
  - `ProfiloService.cs` / `IProfiloService.cs`
  - `UserAdminService.cs` / `IUserAdminService.cs`

### Verifiche
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **143/143 PASS** (nessuna regressione)

## Completato in sessione 2026-04-17 (Iterazione 4 — Fase 4: Backend sale e piantina posti)

### Backend — DTO sala e piantina
- `backend/FilmAPI/DTO/SalaDTO.cs`: nuovi DTO per gestione sale e piantina:
  - `SalaDTO`: sala con lista posti
  - `SalaCreateDTO`: creazione sala con validazione numero progressivo
  - `SalaUpdateDTO`: aggiornamento sala (tipo, nome, supplemento, attiva)
  - `SalaPostoDTO`: singolo posto con settore, fila, numero, coordinate
  - `SalaLayoutSaveDTO`: lista posti per salvataggio piantina completa

### Backend — Nuovo service Sala
- `backend/FilmAPI/Services/SalaService.cs`:
  - `GetByCinemaAsync`: lista sale di un cinema ordinate per numero
  - `GetByIdAsync`: dettaglio sala con posti
  - `CreateAsync`: crea sala con validazione unicità numero progressivo nel cinema
  - `UpdateAsync`: aggiorna proprieta sala
  - `DeleteAsync`: elimina sala con blocco se esistono show futuri o biglietti emessi
  - `GetPostiAsync`: leggi piantina posti
  - `SavePostiAsync`: salva piantina completa (replace-all)

### Backend — Endpoint sale
- `backend/FilmAPI/Endpoints/SaleEndpoints.cs`:
  - `GET /cinemas/{cinemaId}/sale` [AllowAnonymous]
  - `POST /cinemas/{cinemaId}/sale` [PowerUserOrAdmin]
  - `GET /sale/{salaId}` [AllowAnonymous]
  - `PUT /sale/{salaId}` [PowerUserOrAdmin]
  - `DELETE /sale/{salaId}` [PowerUserOrAdmin]
  - `GET /sale/{salaId}/posti` [AllowAnonymous]
  - `PUT /sale/{salaId}/posti` [PowerUserOrAdmin]

### Backend — Program.cs aggiornato
- Registrato `ISalaService` / `SalaService` in DI
- Mappati `SaleEndpoints`

### Test — Integrazione sale (20 nuovi test)
- `tests/backend/Integration/SalaIntegrationTests.cs`:
  - `S1`: lista sale vuota
  - `S2`: lista sale per cinema
  - `S3`: dettaglio sala per ID
  - `S4`: sala non trovata
  - `S5`: crea sala con successo
  - `S6`: conflitto numero sala duplicato
  - `S7`: cinema non trovato
  - `S8`: forbidden per utente senza ruolo
  - `S9`: update sala
  - `S10`: update sala non trovata
  - `S11`: delete sala con successo
  - `S12`: delete sala non trovata
  - `S13`: delete bloccata da show futuri
  - `S14`: posti vuoti
  - `S15`: posti sala non trovata
  - `S16`: salva piantina
  - `S17`: salva piantina replace existing
  - `S18`: salva piantina sala non trovata
  - `S19`: forbidden save posti
  - `S20`: delete bloccata da biglietti emessi

### Verifiche
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet build tests/backend/FilmAPI.Tests.csproj`: **OK**
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **143/143 PASS** (123 esistenti + 20 nuovi)

## Completato in sessione 2026-04-17 (Iterazione 4 — Fase 5: Backend show e bridge legacy proiezioni)

### Backend — DTO show
- `backend/FilmAPI/DTO/ShowDTO.cs`: nuovi DTO per gestione show:
  - `ShowDTO`: show con campi derivati FilmTitolo, CinemaNome, SalaNome, SalaTipo
  - `ShowPagedResultDTO`: risultato paginato con metadati
  - `ShowCreateDTO`: creazione show con DurataMinutiSnapshot e PrezzoBase opzionali
  - `ShowUpdateDTO`: aggiornamento parziale show

### Backend — Nuovo service Show
- `backend/FilmAPI/Services/IShowService.cs` + `Services/ShowService.cs`:
  - `GetAllAsync`: lista completa show ordinati per StartAtUtc
  - `GetPagedAsync`: paginazione con filtri cinemaId, filmId, date
  - `GetByIdAsync`: dettaglio show con join film/cinema/sala
  - `CreateAsync`: crea show con validazione completa (esistenza entita, coerenza sala-cinema, anti-overlap temporale, fallback durata/prezzo)
  - `UpdateAsync`: aggiorna show con validazione anti-overlap (exclude show corrente)
  - `DeleteAsync`: elimina show con blocco se esistono biglietti emessi
  - `GetByCinemaAsync`, `GetByFilmAsync`, `GetByDateAsync`: query specializzate

### Validazione anti-overlap
- Controlla che `[NuovoStart, NuovoEnd)` non intersechi nessuna finestra `[Start, Start+Durata)` esistente della stessa sala
- Permette show consecutivi senza overlap (boundary esatto consentito)
- Permette show contemporanei in sale diverse
- Applicata sia in create che in update

### Backend — Endpoint show
- `backend/FilmAPI/Endpoints/ShowsEndpoints.cs`:
  - `GET /shows` [AllowAnonymous] — lista completa o paginata con filtri
  - `GET /shows/{id}` [AllowAnonymous] — dettaglio
  - `POST /shows` [PowerUserOrAdmin] — crea
  - `PUT /shows/{id}` [PowerUserOrAdmin] — aggiorna
  - `DELETE /shows/{id}` [PowerUserOrAdmin] — elimina

### Backend — Bridge legacy proiezioni
- `backend/FilmAPI/Services/ProiezioneService.cs`: riscritto per usare internamente `IShowService`:
  - read: projection da Shows verso ProiezioneDTO
  - write: bridge temporaneo verso ShowService con assegnazione sala default
  - update: gestisce cambio cinema con assegnazione nuova sala default
- `backend/FilmAPI/DTO/ProiezioneDTO.cs`: `ProiezioneUpdateDTO` reso nullable per partial updates

### Test — Integrazione show (28 nuovi test) + compat legacy (10 test)
- `tests/backend/Integration/ShowIntegrationTests.cs`: 28 test CRUD, overlap, RBAC, filtri
- `tests/backend/Integration/ProiezioneCompatIntegrationTests.cs`: 10 test compatibilita bridge
- Test esistenti adattati: `ProiezioneServiceTests.cs`, `ApiIntegrationTests.cs`

### Verifiche
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **181/181 PASS** (143 esistenti + 38 nuovi)

## Completato in sessione 2026-04-17 (Iterazione 4 — Fase 6: Backend seat map, hold posti e ordine pendente)

### Backend — DTO checkout e seat selection
- `backend/FilmAPI/DTO/CheckoutDTO.cs`: nuovi DTO per checkout:
  - `SeatStatus`: enum con `Available`, `HeldByOther`, `HeldByMe`, `Sold`
  - `SeatMapDTO`: piantina posti con summary show/sala, `ScadeAtUtc` hold corrente, lista `SeatInfoDTO`
  - `SeatInfoDTO`: singolo posto con `SalaPostoId`, `Settore`, `Fila`, `Numero`, `IsWheelchair`, `Stato`
  - `SeatHoldRequestDTO`: richiesta hold con `ShowId` e `SalaPostoIds`
  - `SeatHoldResponseDTO`: risposta hold con `HoldToken`, `ScadeAtUtc`, `SalaPostoIds`, `Conflitti`
  - `CreateOrdineRequestDTO`: richiesta ordine con `HoldToken` e `IdempotencyKey` opzionale
  - `OrdineSummaryDTO`: riepilogo ordine con film/cinema/sala, importi, stato

### Backend — Servizio hold posti
- `backend/FilmAPI/Services/ISeatHoldService.cs` + `Services/SeatHoldService.cs`:
  - `GetSeatMapAsync`: seat map con stati aggiornati + lazy cleanup hold scaduti
  - `CreateHoldAsync`: crea hold in transazione atomica con validazione completa:
    - cleanup hold scaduti per lo show
    - validazione appartenenza posti alla sala dello show
    - controllo conflitti (posti venduti o holdati da altri)
    - generazione `HoldToken` univoco
    - TTL configurabile via `HOLD_TTL_MINUTES` (default 10 min)
  - `RefreshHoldAsync`: estende TTL hold corrente (keep-alive)
  - `ReleaseHoldAsync`: rilascia esplicitamente hold
  - `CleanupExpiredHoldsAsync`: cleanup globale hold scaduti
  - validazione limite massimo 10 posti per ordine

### Backend — Servizio checkout
- `backend/FilmAPI/Services/ICheckoutService.cs` + `Services/CheckoutService.cs`:
  - `CreateOrdineAsync`: crea ordine `Pending` da hold valido con:
    - verifica ownership hold e validita temporale
    - idempotenza su stesso `HoldToken` (ritorna ordine esistente)
    - idempotenza su `IdempotencyKey` client-generated
    - calcolo totale lato backend (`PrezzoBase + SupplementoSala` × numero posti)
    - linking `OrdineId` su record `ShowPostoStato`
  - `GetOrdiniByUserAsync`: lista ordini utente con ownership check
  - `GetOrdineByIdAsync`: dettaglio ordine con ownership check

### Backend — Endpoint checkout
- `backend/FilmAPI/Endpoints/CheckoutEndpoints.cs`: 7 endpoint (tutti `Authenticated`):
  - `GET /checkout/shows/{showId}/seat-map` — piantina posti con stati
  - `POST /checkout/holds` — crea hold posti (409 Conflict se posti non disponibili)
  - `POST /checkout/holds/{holdToken}/refresh` — estendi TTL hold
  - `DELETE /checkout/holds/{holdToken}` — rilascia hold
  - `POST /checkout/orders` — crea ordine pendente da hold valido
  - `GET /checkout/orders` — lista ordini utente
  - `GET /checkout/orders/{orderId}` — dettaglio ordine

### Backend — Background cleanup
- `backend/FilmAPI/Services/ExpiredHoldCleanupService.cs`: hosted service per cleanup periodico:
  - intervallo configurabile con `HOLD_CLEANUP_INTERVAL_MINUTES` (default 5 min)
  - rimozione record `ShowPostoStato` con `Stato=Hold` e `ScadeAtUtc` scaduto

### Backend — Program.cs aggiornato
- Registrati `ISeatHoldService`/`SeatHoldService`, `ICheckoutService`/`CheckoutService`
- Registrato hosted service `ExpiredHoldCleanupService`
- Mappati `CheckoutEndpoints`

### Test — Integrazione checkout (20 nuovi test)
- `tests/backend/Integration/CheckoutIntegrationTests.cs`:
  - `CH1`: seat map con posti disponibili
  - `CH2`: seat map show non trovato → NotFound
  - `CH3`: crea hold con successo
  - `CH4`: hold supera max 10 posti → BadRequest
  - `CH5`: hold stesso posto da altro utente → Conflict
  - `CH6`: stesso utente puo estendere hold esistente
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

### Verifiche
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK** (0 warning, 0 errori)
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **201/201 PASS** (181 esistenti + 20 nuovi)

## Completato in sessione 2026-04-16 (Iterazione 4 — Fase 3: Backend catalogo pubblico, scheda film e cinema preferito)

### Backend — DTO film estesi
- `DTO/FilmDTO.cs`: aggiunto `DescrizioneLunga`, `CastText`, `DataRilascio` a `FilmDTO`, `FilmCreateDTO`, `FilmUpdateDTO`
- `DTO/ProgrammazioneDTO.cs`: nuovi DTO per il catalogo pubblico:
  - `ProgrammazioneFilmDTO`: film con `InEvidenza`, `InUscita`, `ShowCountNext7Days`, `DisponibileNelCinemaSelezionato`
  - `FilmSchedaDTO`: scheda film completa con `CastList`, `ShowCalendar` raggruppato per `Data -> TipoSala -> shows`
  - `CinemaCardDTO`: card cinema con `TipologieSalePresenti`, `DistanzaKm`
  - `CinemaScheduleDayDTO`: programmazione giornaliera cinema con film raggruppati per tipologia sala
  - `CinemaSintesiDTO`, `CinemaPreferitoDTO`: DTO sintetici per cinema e cinema preferito

### Backend — Nuovo service Programmazione
- `Services/IProgrammazioneService.cs` + `Services/ProgrammazioneService.cs`:
  - `GetFilmsAsync`: listing film con tab (`evidenza`/`uscita`/`tutti`), search, filtro categoria, cinemaId
  - `GetCinemasAsync`: elenco cinema con ordinamento per distanza (Haversine) o fallback nome
  - `GetFilmSchedaAsync`: scheda film con show calendar raggruppato
  - `GetMyCinemasAsync`: elenco cinema per pagina cinema-centric
  - `GetCinemaScheduleAsync`: programmazione giornaliera di un cinema
  - `GetCinemaPreferitoAsync` / `SetCinemaPreferitoAsync`: gestione cinema preferito utente

### Backend — Endpoint pubblici nuovi
- `Endpoints/ProgrammazioneEndpoints.cs`:
  - `GET /programmazione/films?tab=&search=&categoriaId=&cinemaId=` [AllowAnonymous]
  - `GET /programmazione/cinemas?lat=&lng=` [AllowAnonymous]
  - `GET /films/{id}/scheda?cinemaId=` [AllowAnonymous]
  - `GET /my-cinemas` [AllowAnonymous]
  - `GET /my-cinemas/{cinemaId}/schedule?date=` [AllowAnonymous]

### Backend — Endpoint cinema preferito
- `Endpoints/ProfiloEndpoints.cs` esteso:
  - `GET /profilo/cinema-preferito` [RequireAuthorization("Authenticated")]
  - `PUT /profilo/cinema-preferito` (clear) [RequireAuthorization("Authenticated")]
  - `PUT /profilo/cinema-preferito/{cinemaId}` (set) [RequireAuthorization("Authenticated")]
- `Services/ProfiloService.cs` esteso con `GetCinemaPreferitoAsync` e `SetCinemaPreferitoAsync`

### Backend — FilmService aggiornato
- `Services/FilmService.cs`: tutti i metodi di proiezione ora includono `DescrizioneLunga`, `CastText`, `DataRilascio`

### Test — Integrazione catalogo pubblico (20 nuovi test)
- `Integration/ProgrammazioneIntegrationTests.cs`:
  - `PG1`: lista vuota
  - `PG2`: tab evidenza con film con show
  - `PG3`: tab uscita con film in arrivo
  - `PG4`: tab tutti i film rilevanti
  - `PG5`: search per titolo
  - `PG6`: filtro per categoria
  - `PG7`: cinema senza coordinate ordinati per nome
  - `PG8`: cinema con coordinate ordinati per distanza
  - `PG9`: scheda film con show calendar
  - `PG10`: scheda film filtrata per cinema
  - `PG11`: scheda film non trovato
  - `PG12`: my-cinemas con tipologie sale
  - `PG13`: cinema schedule per data
  - `PG14`: cinema schedule cinema non trovato
  - `PG15`: cinema preferito non impostato
  - `PG16`: imposta cinema preferito
  - `PG17`: cancella cinema preferito
  - `PG18`: cinema preferito cinema non trovato
  - `PG19`: disponibilità film nel cinema selezionato
  - `PG20`: ordinamento per numero show in evidenza

### Verifiche
- `dotnet build backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet build tests/backend/FilmAPI.Tests.csproj`: **OK**
- `dotnet test tests/backend/FilmAPI.Tests.csproj`: **123/123 PASS** (103 esistenti + 20 nuovi)

## Completato in sessione 2026-04-16 (Iterazione 4 — Fase 2: Migration, seed e data migration legacy)

### Backend — Migration schema + data migration legacy
- Creata migration `AddMultisalaTicketing`:
  - `Migrations/20260416171534_AddMultisalaTicketing.cs`
  - `Migrations/20260416171534_AddMultisalaTicketing.Designer.cs`
- Applicata migration su DB con creazione completa del nuovo schema multisala/ticketing
- Data migration legacy inclusa nella migration principale:
  - inizializzazione `CreditoResiduo = 0` quando `NULL`
  - creazione `Sala 1` default per cinema esistenti senza sale
  - migrazione `Proiezione -> Show` con composizione `StartAtUtc` da `Data + Ora`
  - gestione conflitti per sovrapposizione temporale con creazione automatica di `Sala auto-migrata N`
  - nessuna conversione automatica `Prenotazione -> Biglietto`
  - uso di `DEFAULT_TICKET_PRICE` (fallback a `8.50`) per `PrezzoBase`

### Backend — Seed dev aggiornato
- `Data/DataSeeder.cs` esteso con `SeedDevDataAsync()` attivo solo in ambiente Development
- Aggiunto seed demo coerente col nuovo dominio:
  - cinema (Roma/Milano/Napoli) con coordinate e codice locale
  - registi e film d'esempio (metadati estesi)
  - sale multi-tipologia (`2D`, `3D`, `ISENSE`, `XL`)
  - show distribuiti sui prossimi giorni
- Seed admin aggiornato con `CreditoResiduo = 0`

### Verifiche
- `dotnet ef database drop --project backend/FilmAPI/FilmAPI.csproj --force`: **OK**
- `dotnet ef database update --project backend/FilmAPI/FilmAPI.csproj`: **OK**
- `dotnet ef migrations list --project backend/FilmAPI/FilmAPI.csproj`: tutte applicate fino a `AddMultisalaTicketing`
- Build backend `FilmAPI`: **OK**
- Test backend: **103/103 PASS**

## Completato in sessione 2026-04-13 (Iterazione 4 — Hardening refresh token lifecycle)

### Backend — Refresh token device-aware + cleanup
- `RefreshTokens` estesa con colonna `DeviceId` (migration applicata: `20260413200358_AddRefreshTokenDeviceId`)
- Nuovi indici: `UserId` (mantenuto) + `(UserId, DeviceId)` per query/rotazione efficiente
- `AuthService` aggiornato:
  - login/register/refresh/logout ora lavorano con `deviceId`
  - validazione refresh token vincolata al device (`token.DeviceId == request.DeviceId`)
  - limite attivi per coppia `UserId+DeviceId`: revoca token attivi precedenti sullo stesso device
- Nuovo hosted service `RefreshTokenCleanupService`:
  - rimozione periodica token revocati o scaduti
  - intervallo configurabile con `REFRESH_TOKEN_CLEANUP_INTERVAL_MINUTES` (default 30)

### Frontend — Device identity + refresh proattivo
- `wwwroot/js/auth.js`:
  - introdotta chiave `cb_device_id` in localStorage
  - generazione `deviceId` con UUID e fallback legacy `web-default`
  - invio `deviceId` in `/auth/login`, `/auth/register`, `/auth/refresh`, `/auth/logout`
- `wwwroot/js/route-guard.js`:
  - refresh proattivo prima del redirect login quando l'access token e scaduto
  - mantenuto fallback sicuro a redirect se refresh non disponibile o non valido

### Documentazione aggiornata
- `docs/tutorials/FRONTEND_ARCHITECTURE.md`: aggiunta sezione dedicata all'aggiornamento auth lifecycle (deviceId, refresh reattivo/proattivo, impatti)
- `docs/tutorials/TUTORIAL_AUTENTICAZIONE_WEB.md`: aggiunta appendice implementativa Aprile 2026 con flussi aggiornati e diagrammi mermaid

### Verifiche
- Build backend `FilmAPI`: **OK**
- Migrazione DB `AddRefreshTokenDeviceId`: **OK** (corretta gestione indice FK MySQL)
- Test backend: **103/103 PASS**

## Completato in sessione 2026-04-13 (Iterazione 4 — Refactor navigazione admin + cleanup landing)

### Frontend — Shell admin unificata
- Introdotto layout admin condiviso con sidebar unica + topbar secondaria tramite `wwwroot/js/admin-shell.js`
- Shell applicata a tutte le pagine admin: `dashboard.html`, `films.html`, `registi.html`, `cinemas.html`, `proiezioni.html`, `categorie.html`
- Sidebar unificata con voci coerenti e sezione utility in basso (`Cambia tema`, `Settings`)
- Topbar admin razionalizzata: mantenuto menu profilo/logout, rimosso toggle tema duplicato
- Rimosso componente legacy `wwwroot/components/navbar-admin.html` (non piu usato)

### Frontend — Routing/layout support
- `wwwroot/js/template-loader.js`: escluso caricamento navbar/footer legacy sulle pagine che usano admin shell

### Frontend — Landing cleanup e coerenza menu
- `wwwroot/index.html`: rimossi CTA ridondanti in hero (Area Admin e Programmazione)
- `wwwroot/components/footer-landing.html`: rimosso riferimento `API Docs | Swagger`
- `wwwroot/components/navbar-landing.html`:
  - rimossi link ridondanti `Film` e `Sale` (desktop + mobile)
  - menu mobile riorganizzato in sezioni (`Navigazione`, `Account`, `Preferenze`)
  - CTA mobile allineate a desktop (`btn-outline-brand` / `btn-gold`) con proporzioni uniformi
  - colori mobile allineati al desktop/tablet (`glass-panel`, palette e gerarchie coerenti)

### Frontend — Hero visual tuning
- `wwwroot/css/styles.css` e `wwwroot/js/pages/home.js`: ridotto overlay hero e aumentata visibilita immagine di sfondo

### Verifiche
- Build frontend `CineBase.Web`: **OK**
- Verifica manuale UI: coerenza navigazione admin/landing desktop-mobile: **OK**

## Completato in sessione 2026-04-12 (Iterazione 3 — Fase 10 estesa: UX listing admin + paginazione backend)

### Frontend — Home/featured e responsive refinement
- `wwwroot/index.html`: hero landing resa piu robusta su mobile (spazio verticale e overflow), grid featured ottimizzata
- `wwwroot/js/pages/home.js`: sezione featured rifinita con rotazione hero automatica/manuale, fix proporzioni hero/compact card, fix leggibilita overlay su tema chiaro e tuning tipografico

### Frontend — Coerenza status e tabelle admin
- `wwwroot/dashboard.html`: stato proiezione aggiornato con `chip-status` (pill arrotondata coerente a disponibilita in proiezioni)
- `wwwroot/proiezioni.html` + `wwwroot/js/pages/proiezioni.js`:
  - stato proiezione calcolato correttamente (`Passata` vs `In programma`)
  - colonne film/cinema con nome/titolo + ID, con ID de-enfatizzato
  - aggiunta ricerca testuale e paginazione

### Frontend — Ricerca + paginazione sulle pagine admin
- `wwwroot/registi.html` + `wwwroot/js/pages/registi.js`: paginazione completa con controlli first/prev/next/last
- `wwwroot/cinemas.html` + `wwwroot/js/pages/cinemas.js`: aggiunte barra ricerca + paginazione
- `wwwroot/proiezioni.html` + `wwwroot/js/pages/proiezioni.js`: aggiunte barra ricerca + paginazione
- `wwwroot/js/api.js`: supporto query params `page/pageSize/search` per `getRegisti/getCinemas/getProiezioni`

### Backend — Paginazione server-side
- `Endpoints/RegistiEndpoints.cs`, `Endpoints/CinemasEndpoints.cs`, `Endpoints/ProiezioniEndpoints.cs`:
  - supporto query `page/pageSize/search`
  - fallback legacy preservato (senza query params risposta array)
- `Services/RegistaService.cs`, `Services/CinemaService.cs`, `Services/ProiezioneService.cs`:
  - nuovi metodi `GetPagedAsync(...)` con filtri search e metadati paginazione
- DTO paginati introdotti:
  - `RegistaPagedResultDTO`, `CinemaPagedResultDTO`, `ProiezionePagedResultDTO`

### Test backend — Copertura nuove funzionalita
- `tests/backend/Integration/ApiIntegrationTests.cs`: aggiunti test paginazione/search e compatibilita legacy
  - `R10-R11`, `C6-C7`, `P9-P10`
- Suite completa: **103/103 PASS** (`dotnet test tests/backend/FilmAPI.Tests.csproj`)

### Frontend — Accessibilita form
- `wwwroot/profilo.html`: fix label email (`for="profilo-email"`) per eliminare warning linter HTML su campo form senza label associata

## Completato in sessione 2026-04-12 (Iterazione 3 — Fase 10: Area Personale utente - profilo + prenotazioni)

### Frontend — Nuova pagina profilo
- `wwwroot/profilo.html`: pagina area personale con sezione dati personali (nome, cognome, telefono aggiornabili) e sezione prenotazioni (lista, cancellazione, creazione)
- `wwwroot/js/pages/profilo.js`: logica completa - update profilo via `PUT /profilo`, lista/cancellazione prenotazioni via API, creazione prenotazione da `?prenota=<proiezioneId>` e da form manuale con selettore proiezioni, feedback UI (toast, indicatore salvataggio)

### Frontend — Sezione prenotazione
- Sezione dedicata visibile quando l'utente proviene da `programmazione.html` con `?prenota=<id>` o cliccando "Nuova"
- Da `?prenota=`: carica automaticamente dettagli proiezione (film, cinema, data/ora) e mostra form con numero posti e note
- Da "Nuova": mostra dropdown proiezioni future con lazy loading, form posti/note
- Dopo creazione: refresh lista, toast successo, pulizia URL

### Frontend — Integrazioni
- `wwwroot/js/route-guard.js`: aggiunto `/profilo.html` con `authRequired: true`, ruoli `['user', 'poweruser', 'admin']`
- `wwwroot/js/template-loader.js`: aggiunto `/profilo.html` ai landing paths (usa navbar/footer landing)
- `wwwroot/components/navbar-landing.html`: link "Prenotazioni" ora punta a `/profilo.html#prenotazioni` (desktop + mobile)
- `wwwroot/js/pages/programmazione.js`: bottone "Accedi per prenotare" per anonimi ora redirecta a `/login.html?redirect=/profilo.html?prenota=<id>` per flusso diretto login->prenotazione

### Verifiche
- Test backend: **97/97 PASS** (nessuna modifica backend)
- API e2e: register->login->profilo->update->crea prenotazione->lista->cancella prenotazione **tutto OK**
- Route guard: `/profilo.html` blocca accesso anonimo (redirect login con redirect URL)
- Accesso non autorizzato a `/profilo` senza token: **401 Unauthorized**

## Completato in sessione 2026-04-12 (Iterazione 3 — Fase 9: Programmazione pubblica + gestione categorie admin)

### Frontend — Nuova pagina programmazione
- `wwwroot/programmazione.html`: pagina pubblica accessibile ad anonimi con lista proiezioni
- `wwwroot/js/pages/programmazione.js`: filtri citta/data/categoria/orario, bottone prenota auth-aware
- Route guard: `/programmazione.html` accessibile a tutti (anonimo, user, poweruser, admin)

### Frontend — Nuova pagina categorie admin
- `wwwroot/categorie.html`: pagina CRUD categorie per poweruser/admin
- `wwwroot/js/pages/categorie.js`: create, update, delete categorie con validazione
- Route guard: `/categorie.html` accessibile solo a poweruser/admin

### Frontend — Aggiornamento films.html con categorie
- Tabella: aggiunta colonna "Categorie" con badge
- Form: aggiunto gruppo checkbox per selezione multipla categorie
- Filtro: sostituito filtro hardcoded "genere" con select dinamico categorie

### Frontend — Aggiornamento home.js con badge categorie
- Card film: badge categorie multipli invece di genere singolo
- Bottone "Prenota" auth-aware (redirect a login se anonimo)

### Frontend — Aggiornamenti minori
- `navbar-landing.html`: link "Programmazione" ora punta a `/programmazione.html`
- `index.html`: hero button "Programmazione" punta a `/programmazione.html`
- `api.js`: aggiunto `/categorie.html` ai path admin per enforcement accesso

### Verifiche
- Test backend: **97/97 PASS** (nessuna modifica backend)
- Route guard: permessi corretti per `/programmazione.html` (pubblico) e `/categorie.html` (power/admin)

### Frontend — Refinement landing/index
- `wwwroot/index.html`: sezione film riprogettata come "In Evidenza Questa Settimana" (no filtri operativi)
- `wwwroot/js/pages/home.js`: featured hero + mini-grid, selezione per rilevanza programmazione settimanale e fallback nuove uscite
- `wwwroot/index.html`: hero copy aggiornato per posizionare CineBase come piattaforma completa per la gestione delle sale cinematografiche
- `wwwroot/components/footer-landing.html`: sezione Navigazione resa role-aware come navbar (link dinamici per anonimo/user/admin)
- `wwwroot/js/template-loader.js`: `/programmazione.html` classificata come pagina landing per caricare navbar/footer corretti
- Separazione responsabilita UI confermata:
  - `index.html` = discovery/marketing
  - `programmazione.html` = ricerca/filtro operativo

## Completato in sessione 2026-04-07 (Iterazione 3 — Fase 8.2 Fix loop redirect login)

### Frontend — Route guard self-contained (no dipendenza da Auth nel head)
- `wwwroot/js/route-guard.js`: rimosso ogni riferimento a `window.Auth` (non ancora caricato quando lo script gira nell'`<head>`)
  - lettura token direttamente da `localStorage` (`cb_access_token`)
  - parsing JWT interno con `parseJwt()` invece di delegare ad `Auth.parseJwt()`
  - controllo validita token e ruolo completamente autonomo
- Risultato: nessun loop redirect quando utente con token valido accede direttamente a pagina admin

## Completato in sessione 2026-04-07 (Iterazione 3 — Fase 8.1 Fix Route Guard)

### Frontend — Route guard sincrono nell'head (no flash pagina non autorizzata)
- `wwwroot/js/route-guard.js`: riscritto come IIFE con esecuzione immediata (no `DOMContentLoaded`)
  - parsing JWT diretto da localStorage (senza dipendenza da `Auth` inizializzato) per controllo ruolo prima del render
  - uso di `window.location.replace()` invece di `window.location.href` per evitare che il browser torni alla pagina bloccata col pulsante "indietro"
  - script spostato nell'`<head>` di tutte le pagine, prima di qualsiasi contenuto HTML
- Risultato: se non hai permesso, il browser viene reindirizzato PRIMA che il corpo della pagina venga renderizzato

### Frontend — Navbar role-aware (Film/Sale nascosti a chi non ha permessi)
- `wwwroot/components/navbar-landing.html`: link "Film" e "Sale" nascosti di default (`class="hidden"`), mostrati solo a `PowerUser`/`Admin`
  - desktop: `#nav-films-link`, `#nav-cinemas-link`
  - mobile: `#mobile-nav-films-link`, `#mobile-nav-cinemas-link`
- `updateAuthUI()` gestisce visibilita completa: anonimo vede solo Programmazione + Login/Registrati; User vede solo Programmazione + Profilo/Prenotazioni; PowerUser/Admin vedono anche Film, Sale, Area Admin

### Verifiche
- Nessun flash di pagina non autorizzata: redirect avviene nel `<head>` prima del body parsing
- Pulsante "indietro" del browser non torna alla pagina bloccata (`location.replace`)
- Navbar landing mostra solo voci permesse in base a stato auth e ruolo
- Test backend: **97/97 PASS** (nessuna modifica backend)

## Completato in sessione 2026-04-07 (Iterazione 3 — Fase 8)

### Frontend — Route Guard e navigazione per ruolo
- `wwwroot/js/route-guard.js`: creato con mappa pagina->ruoli ammessi (`PAGE_PERMISSIONS`)
  - redirect non autenticati su pagine admin -> `login.html?redirect=<pagina>`
  - redirect utenti loggati su `login.html`/`registrazione.html` -> `index.html` (o redirect originale)
  - redirect ruolo insufficiente (`User` su pagine admin) -> `index.html?forbidden=true`
- `wwwroot/components/navbar-landing.html`: aggiunto bottone "Area Admin" nascosto di default, visibile solo a `PowerUser`/`Admin` (desktop + mobile)
- `wwwroot/components/navbar-admin.html`: aggiunto link "Categorie" nella navbar, menu dropdown utente con profilo/logout reale (no mock), avatar con iniziali utente
- Tutte le pagine (`index.html`, `login.html`, `registrazione.html`, `dashboard.html`, `films.html`, `registi.html`, `cinemas.html`, `proiezioni.html`): incluso `route-guard.js`

### Verifiche
- URL diretto a pagina non consentita -> redirect corretto (verifica logica route-guard)
- utente `User` non vede bottone "Area Admin" nella navbar (classe `hidden` gestita da `updateAuthUI`)
- Test backend: **97/97 PASS** (nessuna modifica backend)

## Completato in sessione 2026-04-06 (Iterazione 3 — Fase 7.1 Manutenzione Frontend)

### Frontend — Qualita HTML e accessibilita
- Risolti warning linter su pagine admin e auth (`dashboard.html`, `films.html`, `index.html`, `login.html`, `registi.html`, `registrazione.html`, `cinemas.html`, `proiezioni.html`)
- Aggiunti attributi di accessibilita per controlli icon-only (`title`, `aria-label`) e nomi accessibili su `select`
- Collegati `label`/campi con `for` + `id` dove mancanti
- Rimossi inline style segnalati dal linter (sostituiti con classi utility)

### Frontend — Riduzione duplicazione configurazione Tailwind
- Creata configurazione condivisa `wwwroot/js/tailwind-config.js`
- Sostituiti i blocchi inline `tailwind.config` nelle pagine HTML con include unico `<script src="/js/tailwind-config.js"></script>`

### Frontend — Home page fix dati regista
- `wwwroot/js/pages/home.js`: corretto rendering regista nelle card film usando i campi backend `registaNome`/`registaCognome`
- Aggiunto fallback compatibile anche con payload annidato (`film.regista.nome`/`film.regista.cognome`)
- Normalizzazione robusta risposta film (`array`, `items`, `$values`) in caricamento home

### Verifiche
- Linter frontend: nessun errore residuo sui file HTML coinvolti
- Fix home verificato a livello codice con allineamento al DTO backend (`FilmDTO.RegistaNome`, `FilmDTO.RegistaCognome`)

## Completato in sessione 2026-04-06 (Iterazione 3 — Fase 7)

### Frontend — Auth reale e lifecycle token
- `wwwroot/js/auth.js`: gestione token in localStorage (`cb_access_token`, `cb_refresh_token`, `cb_user`), login/register/logout/refresh, parsing JWT e ruolo
- `wwwroot/js/api.js`: Bearer automatico, retry su 401 con refresh token, fallback sessione scaduta con clear auth + redirect login
- `wwwroot/login.html` + `wwwroot/js/pages/login.js`: pagina login con `?redirect=` e `?expired=true`, validazioni e messaggi utente non tecnici
- `wwwroot/registrazione.html` + `wwwroot/js/pages/registrazione.js`: registrazione con validazioni client-side e password strength

### Frontend — Navbar/layout e UX
- `wwwroot/components/navbar-landing.html`: navbar auth-aware (login/registrati vs dropdown utente con profilo/prenotazioni/logout)
- Link `Programmazione` aggiornato a `/index.html#programmazione` (desktop + mobile)
- `wwwroot/js/template-loader.js`: scelta automatica layout landing/admin in base alla pagina (inclusi login/registrazione)
- `wwwroot/js/navbar.js`: rimossa logica mock auth (`sessionStorage`), inizializzazione pulita e compatibile con `updateAuthUI`

### Frontend — Coerenza accesso pagine admin (hardening)
- In `wwwroot/js/api.js` introdotto controllo accesso area admin lato frontend:
  - non autenticato -> redirect `login.html?redirect=<pagina>`
  - ruolo insufficiente (`User`) -> redirect `index.html?forbidden=true`
- `wwwroot/js/pages/home.js`: toast su redirect `forbidden` con cleanup querystring

### Frontend — Fix grafici auth/navbar
- `wwwroot/css/styles.css`: rimosse icone native browser duplicate sui campi password
- `wwwroot/login.html`: placeholder password reso esplicito (`Inserisci la password`)
- `wwwroot/components/navbar-landing.html` + `wwwroot/css/styles.css`: stile logout uniforme con voci menu (base rossa, hover coerente light/dark)

### Verifiche
- Test backend: **97/97 PASS**
- Smoke test frontend: `index.html`, `login.html`, `registrazione.html` -> **200 OK**
- Flussi manuali confermati: login, registrazione, logout; gestione token; redirect accesso non autorizzato con feedback utente

## Completato in sessione 2026-04-06 (Iterazione 3 — Fase 6)

### Test — Auth (A1-A8)
- `A1_Register_ReturnsAuthResponse_WithValidData`: register crea utente con token
- `A2_Register_ReturnsConflict_WhenEmailAlreadyExists`: email duplicata -> 409
- `A3_Login_ReturnsAuthResponse_WithValidCredentials`: login con credenziali valide
- `A4_Login_ReturnsUnauthorized_WithInvalidCredentials`: password errata -> 401
- `A5_Refresh_ReturnsNewTokens_AndRevokesOldRefreshToken`: refresh con rotazione
- `A6_Refresh_ReturnsUnauthorized_WithInvalidToken`: token invalido -> 401
- `A7_Logout_RevokesRefreshToken`: logout revoca token
- `A8_Me_ReturnsUserInfo_WhenAuthenticated`: /me ritorna dati utente

### Test — RBAC (RB1-RB8)
- `RB1_Anonymous_OnProtectedEndpoint_ReturnsUnauthorized`: anonimo -> 401
- `RB2_User_OnAdminOnlyEndpoint_ReturnsForbidden`: User su admin -> 403
- `RB3_User_OnPowerUserOrAdminEndpoint_ReturnsForbidden`: User su power/admin -> 403
- `RB4_PowerUser_OnAdminOnlyEndpoint_ReturnsForbidden`: PowerUser su admin -> 403
- `RB5_PowerUser_OnPowerUserOrAdminEndpoint_ReturnsSuccess`: PowerUser CRUD -> OK
- `RB6_Admin_OnAdminOnlyEndpoint_ReturnsSuccess`: Admin su admin -> OK
- `RB7_Admin_OnPowerUserOrAdminEndpoint_ReturnsSuccess`: Admin su power/admin -> OK
- `RB8_Anonymous_OnPublicGetEndpoint_ReturnsSuccess`: GET pubblici -> OK

### Test — Categorie (CAT1-CAT5)
- `CAT1_GetCategorie_ReturnsAllCategories`: lista categorie
- `CAT2_CreateCategoria_ReturnsCreated_WithValidData`: crea -> 201
- `CAT3_CreateCategoria_ReturnsConflict_WhenDuplicateName`: duplicato -> 409
- `CAT4_UpdateCategoria_UpdatesName_WhenExists`: update nome
- `CAT5_DeleteCategoria_DeletesEntity_WhenExists`: delete -> 204

### Test — Prenotazioni (PR1-PR5)
- `PR1_CreatePrenotazione_ReturnsCreated_WithValidData`: crea prenotazione
- `PR2_User_SeesOnlyOwnPrenotazioni`: isolamento prenotazioni per utente
- `PR3_User_CannotDeleteAnotherUsersPrenotazione`: ownership delete
- `PR4_Admin_SeesAllPrenotazioni`: admin vede tutte
- `PR5_DeletePrenotazione_ReturnsNotFound_WhenNonExistent`: delete inesistente -> 404

### CustomWebApplicationFactory
- Aggiunto supporto user ID configurabile via header `X-Test-UserId`
- Aggiunti header `X-Test-Email` e `X-Test-Nome` per identita multiple
- Nuovo metodo `CreateAuthenticatedClient(role, userId, email, nome)`
- `TestAuthHandler` legge userId/email/nome dagli header invece di hardcoded

### Verifiche
- Test totali: **97/97 PASS** (da 71 a 97, +26 nuovi)
- Copertura minima raggiunta: auth, RBAC, categorie, prenotazioni
- Nessun test esistente rotto

## Completato in sessione 2026-04-06 (Iterazione 3 — Fase 5)

### Backend — DTO Profilo/Prenotazioni/Admin
- `DTO/ProfiloPrenotazioniAdminDTO.cs`: ProfiloUpdateDTO, PrenotazioneCreateDTO, PrenotazioneDTO, UserAdminDTO, UpdateRuoloDTO

### Backend — Servizi Profilo
- `Services/IProfiloService.cs` + `Services/ProfiloService.cs`:
  - `GetProfiloAsync`: recupera profilo utente per ID
  - `UpdateProfiloAsync`: aggiorna Nome, Cognome, Telefono

### Backend — Servizi Prenotazioni
- `Services/IPrenotazioneService.cs` + `Services/PrenotazioneService.cs`:
  - `GetPrenotazioniAsync`: recupera prenotazioni proprie dell'utente (filter by userId)
  - `GetAllPrenotazioniAsync`: recupera tutte le prenotazioni (per admin)
  - `CreatePrenotazioneAsync`: crea prenotazione con validazione proiezione esistente
  - `DeletePrenotazioneAsync`: elimina prenotazione con ownership check
  - `PrenotazioneDTO` include campi derivati: TitoloFilm, NomeCinema, DataProiezione, OraProiezione

### Backend — Servizi Admin Utenti
- `Services/IUserAdminService.cs` + `Services/UserAdminService.cs`:
  - `GetAllUsersAsync`: recupera tutti gli utenti
  - `UpdateUserRoleAsync`: aggiorna ruolo con vincolo "ultimo admin"
  - Vincolo sicurezza: se l'utente e admin e si tenta di degradarlo a non-admin, verifica che ci sia almeno un altro admin

### Backend — Endpoint Profilo
- `Endpoints/ProfiloEndpoints.cs`:
  - `GET /profilo` -> 200 OK / 401
  - `PUT /profilo` -> 200 OK / 401
  - Policy: Authenticated

### Backend — Endpoint Prenotazioni
- `Endpoints/PrenotazioniEndpoints.cs`:
  - `GET /prenotazioni` -> 200 OK (User: proprie, Admin: tutte)
  - `POST /prenotazioni` -> 201 Created / 400 / 401
  - `DELETE /prenotazioni/{id}` -> 204 / 404 (solo proprie per User, tutte per Admin)
  - Policy: Authenticated

### Backend — Endpoint Admin Utenti
- `Endpoints/AdminUtentiEndpoints.cs`:
  - `GET /admin/utenti` -> 200 OK (lista tutti gli utenti)
  - `PUT /admin/utenti/{id}/ruolo` -> 200 OK / 400 (vincolo ultimo admin) / 404
  - Policy: AdminOnly

### Verifiche
- User vede/modifica solo dati propri: **OK** (2 utenti distinti testati)
- User gestisce solo prenotazioni proprie: **OK** (`GET` isolato, `DELETE` altrui -> `404`)
- Admin vede tutte le prenotazioni e gestisce ruoli: **OK**
- Vincolo ultimo admin: **OK** (`PUT /admin/utenti/1/ruolo` -> `400`)
- Test backend: **71/71 PASS**

## Completato in sessione 2026-04-06 (Iterazione 3 — Fase 4)

### Backend — DTO Auth (5 file)
- `DTO/LoginRequestDTO.cs`: Email, Password (required)
- `DTO/RegisterRequestDTO.cs`: Email, Password, Nome, Cognome, Telefono (required)
- `DTO/AuthResponseDTO.cs`: AccessToken, RefreshToken, ExpiresAt, User (UserInfoDTO)
- `DTO/UserInfoDTO.cs`: Id, Email, Nome, Cognome, Telefono, Ruolo, DataRegistrazione
- `DTO/RefreshTokenRequestDTO.cs`: RefreshToken (required)

### Backend — Servizi Auth
- `Services/IAuthService.cs` + `Services/AuthService.cs`:
  - `RegisterAsync`: BCrypt hash, ruolo default User, genera coppia token
  - `LoginAsync`: verifica credenziali con BCrypt, genera coppia token
  - `RefreshAsync`: validazione refresh token, rotazione (revoca vecchio + nuovo)
  - `LogoutAsync`: revoca refresh token
  - `GetUserByIdAsync`: recupero info utente da ID
  - Helper `GenerateAccessToken` (JWT HS256, claims: sub/email/role/nome)
  - Helper `GenerateRefreshToken` (stringa opaca Base64, expiry 7 giorni)

### Backend — Endpoint Auth
- `Endpoints/AuthEndpoints.cs`: 5 endpoint
  - `POST /auth/register` -> 200 OK / 409 Conflict (email duplicata)
  - `POST /auth/login` -> 200 OK / 401 Unauthorized (credenziali errate)
  - `POST /auth/refresh` -> 200 OK / 401 Unauthorized (token invalido/scaduto)
  - `POST /auth/logout` -> 200 OK / 404 Not Found
  - `GET /auth/me` -> 200 OK / 401 Unauthorized (parsing JWT manuale)

### Verifiche manuali
- Register crea utente con ruolo User: **OK**
- Login ritorna coppia access + refresh token: **OK**
- Refresh rinnova token e revoca il precedente: **OK**
- Credenziali errate -> 401: **OK**
- Vecchio refresh token dopo refresh -> 401: **OK**

### Bug fix critico
- `AuthService.cs`: aggiunto `SaveChangesAsync()` mancante dopo `GenerateRefreshToken()` in `RegisterAsync` e `LoginAsync`. Senza questa chiamata il refresh token veniva restituito al client ma non persistito nel DB, rendendo impossibile il refresh.

## Completato in sessione 2026-04-06 (Iterazione 3 — Fase 2)

### Backend — DTO Categorie
- `DTO/CategoriaDTO.cs`: CategoriaDTO, CategoriaCreateDTO, CategoriaUpdateDTO

### Backend — Servizi Categorie
- `Services/ICategoriaService.cs` + `CategoriaService.cs`: CRUD completo
- Validazione duplicati su nome (solleva `InvalidOperationException` -> 409 Conflict)
- Normalizzazione nome con trim

### Backend — Aggiornamento FilmService
- `FilmDTO`: aggiunta `List<CategoriaDTO> Categorie`
- `FilmCreateDTO`/`FilmUpdateDTO`: aggiunta `List<int>? CategorieIds`
- GetAll/GetPaged/GetById: includono `FilmCategorie -> Categoria`
- Create/Update: sync record ponte `FilmCategoria` (aggiunta/rimozione selettiva)
- Refactoring con metodi helper `MapToDTO`, `MapToDTOAsync`, `SyncFilmCategorieAsync`

### Backend — Endpoint Categorie
- `Endpoints/CategorieEndpoints.cs`: 5 endpoint
  - `GET /categorie` -> 200 OK (lista)
  - `GET /categorie/{id}` -> 200 OK / 404
  - `POST /categorie` -> 201 Created / 400 / 409
  - `PUT /categorie/{id}` -> 200 OK / 400 / 404 / 409
  - `DELETE /categorie/{id}` -> 204 NoContent / 404

### Verifiche
- Test backend: **71/71 PASS** (nessuna regressione)
- CRUD categorie funzionante con validazione duplicati
- Film con categorie multiple in create/update

## Completato in sessione 2026-04-06 (Iterazione 3 — Fase 1)

### Backend — Nuovi modelli (6 file)
- `Model/UserRole.cs`: enum con User=0, PowerUser=1, Admin=2
- `Model/User.cs`: entita utente con Email (unique), PasswordHash, Nome, Cognome, Telefono, Ruolo, DataRegistrazione
- `Model/RefreshToken.cs`: token opaco con UserId, ExpiresAt, CreatedAt, RevokedAt, computed `IsActive`
- `Model/Prenotazione.cs`: prenotazione con UserId, ProiezioneId, NumeroPosti, Note, DataPrenotazione
- `Model/Categoria.cs`: categoria con Nome (unique, max 100)
- `Model/FilmCategoria.cs`: tabella ponte con PK composita (FilmId, CategoriaId)

### Backend — Data e Seed
- `Data/FilmDbContext.cs`: aggiunti DbSet Users, RefreshTokens, Prenotazioni, Categorie, FilmCategorie
- Relazioni configurate: User 1-N RefreshToken (Cascade), User 1-N Prenotazione (Cascade), Proiezione 1-N Prenotazione (Restrict), Film-Categoria many-to-many (Cascade)
- Indici unici: Categoria.Nome, User.Email, RefreshToken.Token
- `Data/DataSeeder.cs`: seed automatico admin da env + 12 categorie (Drammatico, Commedia, Avventura, Fantasy, Horror, Azione, Fantascienza, Thriller, Animazione, Documentario, Romantico, Storico)
- Migration `AddCategorieAndAuth` creata e applicata

### Backend — Configurazione
- `Program.cs`: aggiunta configurazione JWT (HS256) con `AddAuthentication().AddJwtBearer()` — middleware NON ancora attivi (preparazione per Fase 4)
- `.env` / `.env.example`: aggiunte variabili JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE, JWT_ACCESS_TOKEN_EXPIRY_MINUTES, JWT_REFRESH_TOKEN_EXPIRY_DAYS, ADMIN_SEED_EMAIL, ADMIN_SEED_PASSWORD
- Package installati: `Microsoft.AspNetCore.Authentication.JwtBearer` 9.0.11, `BCrypt.Net-Next` 4.1.0

### Verifiche
- Test backend: **71/71 PASS** (nessuna regressione)
- Migration applicata correttamente
- Seed admin e 12 categorie verificato su DB

### Design System
- Estratti design tokens dal progetto Stitch "Modern CineBase Style" (ID: `3151130682396165519`)
- Creato `DesignSystem.md` con documentazione completa di Cinema Graphite
- Token: colori (gold/indigo/cyan), surface hierarchy, tipografia Inter, roundness, ombre

### Frontend — CSS (`styles.css`)
- Riscritto completamente con CSS custom properties per 30+ token brand
- Tema LIGHT in `:root`, tema DARK in `.dark`
- Classi create: `glass-panel`, `sidebar-glass`, `ambient-shadow`, `card-elevated`, `ghost-input` (con dark mode), `btn-gold`/`btn-gold-lg`/`btn-gold-sm`/`btn-outline-brand`/`btn-outline-brand-light`/`btn-ghost`, `chip-active`/`chip-past`, `row-hover`, `btn-page`, `label-caps`, `hero-overlay`, `modal-backdrop`, `theme-toggle-btn`, `sidebar-theme-toggle`
- Fix select dropdown: `appearance: none !important` per evitare frecce duplicate in dark mode
- Fix overlay hero: da near-white a dark per leggibilità testo in light mode

### Frontend — Nuovo file `theme.js`
- Gestione tema light/dark con localStorage + system preference (`prefers-color-scheme`)
- Default: system preference
- Espone `window.CineBaseTheme` con metodi `init()`, `toggle()`, `set()`, `get()`
- Toggle icon: luna/sole con transizione

### Frontend — Pagine HTML modificate (16 file)
- `index.html`: hero overlay dark, testo bianco, `btn-outline-brand-light` per Programmazione, `btn-gold-lg` per Area Admin
- `dashboard.html`: sidebar flottante mobile con hamburger, `overflow-x-auto` per tabelle, bottoni Salva fixati, toggle tema nella sidebar
- `films.html`, `registi.html`, `cinemas.html`, `proiezioni.html`: card-elevated, label-caps, ghost-input, btn-gold, row-hover, pagination btn-page, modali senza bg-white
- `navbar-landing.html`, `navbar-admin.html`: glass-panel, theme toggle, btn-gold-lg/sm
- `footer-landing.html`, `footer-admin.html`: brand surface colors
- `js/navbar.js`: active links con text-brand-gold
- `js/pages/home.js`, `films.js`, `registi.js`, `cinemas.js`, `proiezioni.js`: template classes con brand tokens

### Frontend — Fix mobile responsive
- Dashboard sidebar: `fixed` su mobile con backdrop, `translate-x` toggle, contenuto full-width
- Dashboard header: hamburger button, titolo responsive, bottone "+ Proiezione" con testo abbreviato su mobile
- Tutte le tabelle: `overflow-x-auto` per scroll orizzontale su mobile
- Dashboard proiezioni: tabella aggiornata con colonne ID, Film, Cinema, Data, Ora, Status (allineata a proiezioni.html)

### Verifiche
- Grep `bg-white` residui: **0 occorrenze** nei file frontend
- Grep `bg-brand-dark*`, `text-brand-orange` residui: **0 occorrenze**
- Test backend: **71/71 PASS** (invariato, nessuna modifica funzionale)
- File NON modificati: `api.js`, `template-loader.js`, `form-handlers.js`

## Completato in sessione 2026-03-29 (Iterazione 2.1)

### Backend (FilmAPI)
- Creato endpoint `POST /media/covers` per upload copertine (multipart/form-data)
- Implementato `MediaService` con validazioni:
  - MIME consentiti: `image/jpeg`, `image/png`, `image/webp`
  - Estensioni consentite: `.jpg`, `.jpeg`, `.png`, `.webp`
  - Dimensione massima: 5 MB
  - Nome file sicuro con GUID
- Salvataggio file in `wwwroot/media/covers/`
- Configurato static file serving (`app.UseStaticFiles()`)
- Aggiunta validazione `filmatoPath` come URL assoluto http/https in `FilmService`
- Creati DTO: `MediaUploadResultDTO`, `MediaDTO`
- Aggiornato `CustomWebApplicationFactory` per supportare WebRoot nei test

### Frontend (CineBase.Web)
- Aggiornato `films.html`: campo input file per copertina + campo Trailer URL
- Aggiornato `films.js`: gestione upload copertina prima del submit film, spinner durante upload
- Aggiunto metodo `uploadCover(file)` in `api.js` per multipart upload
- Corretto rendering immagini copertina: path `/media/*` risolti verso backend (`http://localhost:5000`)
- Aggiornato `home.js` per compatibilità con path copertine caricati

### Test
- 71 test passati (5 nuovi test integration):
  - `M1_UploadCover_ReturnsOk_WithValidImage`
  - `M2_UploadCover_ReturnsBadRequest_WhenNoFile`
  - `M3_UploadCover_ReturnsBadRequest_WhenUnsupportedMimeType`
  - `F9_PostFilms_ReturnsBadRequest_WhenFilmatoPathIsInvalidUrl`
  - `F10_PostFilms_AcceptsValidFilmatoUrl`
- Aggiornati test esistenti per rimuovere `filmatoPath` con URL non validi

## Completato in sessione 2026-03-18 (commit 2eb4fe4)
- Aggiunto modal proiezione alla dashboard
- Risolti errori CRUD nelle pagine admin

## Verifiche eseguite
- Test backend (`tests/backend/FilmAPI.Tests.csproj`): **71/71 PASS**
- Verifica manuale frontend:
  - Upload copertina film funzionante
  - Visualizzazione copertine su `films.html` e `index.html`
  - Validazione URL trailer
  - Edit film senza nuova copertina (preserva valore esistente)
  - Tema chiaro e scuro funzionanti su tutte le pagine
  - Responsive mobile su dashboard

## Completato in sessione 2026-04-12 (Iterazione 3 — Fase 11: Verifica finale, hardening e documentazione)

### Test backend
- Suite completa: **103/103 PASS** (`dotnet test tests/backend/FilmAPI.Tests.csproj`)
- Nessuna regressione rispetto a Fase 10

### Verifica RBAC backend
- Endpoint pubblici (GET films/cinemas/proiezioni/categorie): `AllowAnonymous` confermato
- Endpoint auth (register/login/refresh): `AllowAnonymous` confermato
- Endpoint auth (logout/me): `RequireAuthorization("Authenticated")` confermato
- Endpoint CRUD registi/films/proiezioni/categorie: `RequireAuthorization("PowerUserOrAdmin")` confermato
- Endpoint CRUD cinemas: GET pubblico, CUD `RequireAuthorization("AdminOnly")` confermato
- Endpoint media upload: `RequireAuthorization("PowerUserOrAdmin")` confermato
- Endpoint profilo/prenotazioni: `RequireAuthorization("Authenticated")` con ownership check confermato
- Endpoint admin utenti: `RequireAuthorization("AdminOnly")` confermato

### Verifica RBAC frontend (route-guard.js)
- `index.html`, `programmazione.html`: accessibili a tutti (anonimo/user/poweruser/admin)
- `login.html`, `registrazione.html`: solo anonimi, redirect a `index.html` se gia loggati
- `dashboard.html`, `films.html`, `registi.html`, `cinemas.html`, `proiezioni.html`, `categorie.html`: solo poweruser/admin
- `profilo.html`: user/poweruser/admin, redirect login se anonimi
- Redirect ruolo insufficiente: `index.html?forbidden=true`
- Redirect non autenticati: `login.html?redirect=<pagina_corrente>`

### Criteri di accettazione iterazione 3
1. Anonimo accede a index/programmazione ma non prenota: **OK**
2. Registrazione/login producono token JWT validi: **OK**
3. Refresh token rinnova access token senza nuovo login: **OK**
4. User gestisce profilo e prenotazioni proprie: **OK**
5. User non accede a pagine admin e non vede bottone area admin: **OK**
6. PowerUser fa CRUD su Film/Proiezioni/Registi/Categorie e solo Read su Cinema: **OK**
7. Admin fa tutto e gestisce ruoli utenti: **OK**
8. Categorie associate ai film, visualizzate e filtrabili: **OK**
9. API rispondono con 401/403 coerenti: **OK**
10. Redirect frontend coerenti per tutti i casi non autorizzati: **OK**
11. Suite backend totalmente verde (103/103): **OK**

## Prossimi passi suggeriti
- Implementare cancellazione file copertina orfani (opzionale)
- Aggiungere preview immagine prima dell'upload
- Valutare CDN per file media in produzione
- Verifica manuale completa di tutte le pagine in entrambi i temi
