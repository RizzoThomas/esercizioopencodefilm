# Piano di Lavoro - Iterazione 4

Autore: OpenCode

Documento unificato costruito a partire dall'analisi comparativa dei piani in `docs/project/dev_iteration/4/proposals/`, con base principale su `PianoDiLavoro2.md` e integrazione delle parti migliori di `PianoDiLavoro1.md` e `PianoDiLavoro3.md`.

Branch target suggerito: `dev_iteration_4`

---

## Stato Avanzamento Fasi

| Fase | Stato | Data | Note |
| --- | --- | --- | --- |
| FASE 1 - Modello dati v2 e compat layer | **Completata** | 2026-04-16 | Enum, entità, FilmDbContext aggiornato, Proiezione/Prenotazione mantenute |
| FASE 2 - Migration, seed e data migration legacy | **Completata** | 2026-04-16 | Migration applicata, seed dev e data migration legacy con gestione conflitti |
| FASE 3 - Backend catalogo pubblico, scheda film e cinema preferito | **Completata** | 2026-04-16 | DTO film estesi, endpoint programmazione, scheda film, my-cinemas, cinema preferito, 20 test integrazione |
| FASE 4 - Backend sale e piantina posti | **Completata** | 2026-04-17 |  DTO sala e piantina, Services, Endpoints e 20 test integrazione per CRUD sale, piantina, validazioni |
| FASE 5 - Backend show e bridge legacy proiezioni | **Completata** | 2026-04-17 | DTO, service, endpoint ShowsEndpoints, anti-overlap, bridge proiezioni, 38 test |
| FASE 6 - Backend seat map, hold posti e ordine pendente | **Completata** | 2026-04-17 | DTO checkout, SeatHoldService su ShowPostoStato, CheckoutService, 7 endpoint, background cleanup, limite 10 posti, 20 test (inclusi concorrenza) |
| FASE 7 - Backend pagamento, credito piattaforma e finalizzazione checkout | **Completata** | 2026-04-18 | Stripe.net integrato con gateway astratto, Pagamento/Credito services, finalizzazione idempotente ordine, webhook replay-safe, liste ordini/ticket e 6 test integrazione dedicati |
| FASE 8 - Backend ticketing digitale, PDF/email e validazione biglietti | **Completata** | 2026-04-18 | `BigliettoService`, `PdfService`, `EmailService`, `ValidazioneBigliettoService`, endpoint PDF/validazione, 6 test integrazione dedicati e smoke test SMTP reale |
| FASE 9 - Frontend `programmazione.html` v2 + modale scelta cinema | **Completata** | 2026-04-19 | Pagina film-centric con tabs, search, filtro categoria, modale cinema, persistenza cinema preferito, affinamenti UX/performance (geolocalizzazione non bloccante, caroselli, load more) e paginazione backend |
| FASE 9.1 - Progetto `FilmApiSeeder` e seed realistico database | **Completata** | 2026-04-18 | Progetto console standalone in `backend/scripts/FilmApiSeeder`, integrazione TMDB, seed film/cinema/sale/posti/show, reset sicuri e configurazione condivisa via `backend/.env` |
| FASE 10 - Frontend `scheda-film.html` + `my-cinemas.html` | **Completata** | 2026-04-19 | Rail date orizzontale riusabile, show raggruppati per tipologia sala, bottoni orario auth-aware, route guard e template loader aggiornati |
| FASE 11 - Frontend `acquista.html`, `pagamento.html`, `esito-acquisto.html`, `profilo.html` v2 | **Completata** | 2026-04-19 | Seat-map interattiva con countdown, keep-alive, layout desktop compatto e zoom avanzato; Stripe Elements embedded con finalizzazione sincrona, annullamento ordine pendente, configurazione Stripe runtime dal backend e prezzi confermati lato backend |
| FASE 11.1 - Migrazione da Stripe Elements a Stripe Checkout hosted | **Completata** | 2026-04-19 | Checkout Session hosted con webhook come source of truth, supporto robusto a credito piattaforma, pagamento misto con credito riservato/rilasciato, cleanup automatico ordini hosted scaduti, riconciliazione backend al ritorno da Stripe e 13 test integrazione |
| FASE 12 - Frontend admin: sale, show, ricarica credito, validazione ticket | Pending | - | - |
| FASE 13 - Test finali, cleanup legacy, hardening e documentazione | Pending | - | - |

---

## 1) Obiettivo Iterazione

Portare CineBase da piattaforma con proiezioni semplici e prenotazioni virtuali a piattaforma operativa per cinema multisala, con:

- gestione dei cinema distribuiti sul territorio nazionale
- gestione delle sale di ciascun cinema con tipologia e piantina posti
- nuova programmazione pubblica film-centric, non più show-centric
- pagina dettaglio film orientata all'acquisto
- pagina elenco cinema e programmazione per singolo cinema
- selezione posti con protezione da race condition
- acquisto reale con carta, credito piattaforma o pagamento misto
- ticket digitale con PDF, barcode, QR code, email e validazione in ingresso
- strumenti operativi per PowerUser/Admin: gestione sale, show, ricarica credito, validazione biglietti

## 1.1 Contesto attuale (da `status.md` e `changelog.md`)

- branch di lavoro corrente: `dev_iteration_3`
- iterazione 3 completata al 100%
- backend stabile con `103/103` test automatici verdi
- auth JWT, refresh token, RBAC e route guard frontend già operativi
- `programmazione.html` esiste già ma è costruita su card per proiezione, con duplicazione del medesimo film su cinema/date/orari diversi
- backend attuale ancora modellato su `Proiezione(CinemaId, FilmId, Data, Ora)`
- `Prenotazione` attuale è solo una prenotazione virtuale con `NumeroPosti`, senza selezione di posti reali e senza pagamento
- non esistono ancora sale, piantine, ordine di acquisto, ticket digitali, pagamento Stripe, credito piattaforma, validazione ticket

## 1.2 Scope dell'iterazione

### In scope

- evoluzione del modello dati da cinema monosala implicito a cinema multisala esplicito
- redesign completo delle esperienze pubbliche `programmazione.html`, `scheda-film.html`, `my-cinemas.html`
- introduzione del flusso di acquisto reale con selezione posti, ordine, pagamento, ticket e validazione
- aggiornamento dell'area admin per sale, show, credito e validazione
- evoluzione di `profilo.html` da prenotazioni virtuali a biglietti, ordini, credito e cinema preferito

### Out of scope per questa iterazione

- rimborsi automatici post-pagamento
- loyalty, coupon, gift card o promozioni avanzate
- reportistica BI e dashboard economiche
- integrazione con tornelli fisici o hardware dedicato di validazione oltre al browser su smartphone/tablet
- pricing dinamico per singolo posto o settore oltre al prezzo base show + supplemento sala

## 1.3 Architettura repository

```text
repo-root/
|- backend/FilmAPI/          (API .NET 9 Minimal API + MariaDB)
|- frontend/CineBase.Web/    (MPA statico, vanilla JS + Tailwind)
|- tests/backend/            (xUnit + integration)
|- docs/
```

## 1.4 Ordine di esecuzione raccomandato

- Le fasi 1-5 sono propedeutiche e rendono coerente il dominio multisala/show.
- Le fasi 6-8 rendono il backend realmente acquistabile e validabile.
- Le fasi 9-12 completano il frontend pubblico e operativo.
- La fase 13 chiude l'iterazione con test estesi, verifica end-to-end, hardening e cleanup legacy.
- Il branch non deve essere considerato deployable prima del completamento almeno delle fasi 1-8, perché nel mezzo esisterà una compatibilità temporanea tra dominio legacy e dominio nuovo.

## 1.5 Nomenclatura approvata

Questa sezione fissa la nomenclatura ufficiale da usare nel codice e nella documentazione dell'iterazione 4.

### Regole approvate

- il termine canonico del nuovo dominio è `Show`
- `Proiezione` resta solo come termine legacy/compat layer
- i file `Endpoints` del nuovo dominio usano il plurale della risorsa principale quando la route è plurale
- i nomi business restano coerenti con il progetto: `Pagamento`, `Biglietto`, `ValidazioneBiglietto`, `Credito`
- `my-cinemas.html` resta tale per aderire al requisito utente, ma non va propagato come prefisso nel backend
- la pagina staff di validazione si chiama ufficialmente `validazione-biglietti.html`

### Nomi approvati

| Area | Nomi approvati |
| --- | --- |
| Model | `Sala`, `SalaPosto`, `Show`, `ShowPostoStato`, `Ordine`, `Biglietto`, `MovimentoCredito` |
| DTO file | `SalaDTO.cs`, `ShowDTO.cs`, `ProgrammazioneDTO.cs`, `FilmSchedaDTO.cs`, `CheckoutDTO.cs`, `OrdineDTO.cs`, `BigliettoDTO.cs`, `CreditoDTO.cs`, `ProfiloDTO.cs` |
| Services | `SalaService`, `ShowService`, `ProgrammazioneService`, `SeatHoldService`, `CheckoutService`, `PagamentoService`, `CreditoService`, `BigliettoService`, `PdfService`, `EmailService`, `ValidazioneBigliettoService` |
| Endpoint file | `SaleEndpoints.cs`, `ShowsEndpoints.cs`, `ProgrammazioneEndpoints.cs`, `CheckoutEndpoints.cs`, `PagamentoEndpoints.cs`, `CreditoEndpoints.cs`, `ValidazioneBigliettiEndpoints.cs` |
| Public pages | `programmazione.html`, `scheda-film.html`, `my-cinemas.html`, `acquista.html`, `pagamento.html`, `esito-acquisto.html` |
| Admin pages | `sale.html`, `ricarica-credito.html`, `validazione-biglietti.html` |
| Page JS | `programmazione.js`, `scheda-film.js`, `my-cinemas.js`, `acquista.js`, `pagamento.js`, `esito-acquisto.js`, `sale.js`, `ricarica-credito.js`, `validazione-biglietti.js` |
| Route nuove | `/shows`, `/checkout/...`, `/payments/...`, `/admin/tickets/validate/...`, `/admin/credito/...` |

Nota DTO:

- `CheckoutDTO.cs` conterrà i DTO di seat map, hold e richieste checkout
- `OrdineDTO.cs` conterrà i DTO relativi a ordini e riepiloghi ordine
- `BigliettoDTO.cs` conterrà i DTO relativi a biglietti e validazione
- `ProfiloDTO.cs` conterrà i DTO di profilo evoluti
- `ProfiloPrenotazioniAdminDTO.cs` non deve essere ulteriormente esteso per il nuovo dominio

## 1.6 Convenzioni implementative

Questa sezione fissa la convenzione operativa da usare durante l'implementazione del codice.

### Service file layout

- per i service CRUD o application service semplici si adotta la convenzione già presente nel repository: interfaccia e implementazione nello stesso file
- esempi target di questa convenzione: `SalaService.cs`, `ShowService.cs`, `CreditoService.cs`
- per i service orchestrativi o infrastrutturali, con maggiore probabilità di crescita o mocking mirato, si preferiscono file separati per interfaccia e implementazione
- esempi target di questa convenzione: `Checkout`, `Pagamento`, `Email`, `Pdf`, `ValidazioneBiglietto`

### Regola pratica

- se un service contiene principalmente CRUD, query, mapping e validazioni locali del dominio, può restare in un unico file `IService + Service`
- se un service orchestra più dipendenze esterne o più sottodomini, va preferita la separazione in file distinti

### Obiettivo della convenzione

- minimizzare boilerplate dove non serve
- mantenere il codice leggibile e coerente con il repository attuale
- evitare che i service più complessi diventino file monolitici difficili da testare e manutenere

## 1.7 Riferimenti tutorial Stripe e strategia adottata per la Fase 7

Per la preparazione, l'implementazione e il collaudo della `FASE 7`, i seguenti documenti diventano riferimenti operativi espliciti del piano:

- `docs/tutorials/TUTORIAL_STRIPE_GATEWAY_PAGAMENTI.md`
  - spiega il modello Stripe, il ruolo di `PaymentIntent`, la differenza tra `publishable key`, `secret key`, `client_secret` e `webhook secret`, e la distinzione tra flusso sincrono e flusso asincrono
- `docs/tutorials/TUTORIAL_STRIPE_CLI.md`
  - spiega installazione, autenticazione, listener locale, trigger e workflow di debugging con `Stripe CLI`
- `docs/tutorials/TUTORIAL_URL_PUBBLICI_WEBHOOKS.md`
  - documenta `ngrok`, `Dev Tunnels`, `Cloudflare Tunnel`, `localtunnel` e `Tailscale Funnel`, da considerare come opzioni secondarie rispetto a `Stripe CLI` per il caso d'uso locale di questa iterazione
- `docs/tutorials/TUTORIAL_STRIPE_STRATEGIA_INTEGRAZIONE_CINEBASE.md`
  - definisce la strategia ufficiale adottata per `CineBase`, incluse la configurazione manuale della dashboard Stripe e la scelta operativa della strategia locale di test webhook

Strategia ufficiale approvata per l'iterazione:

- applicazione di test Stripe di riferimento: `CineBase_Demo`
- modello di integrazione: flusso applicativo principale sincrono con verifica backend del `PaymentIntent`, più webhook Stripe implementato fin da subito
- strategia locale di test webhook adottata: `Scenario B - Test webhook in locale con Stripe CLI`, come descritto in `docs/tutorials/TUTORIAL_STRIPE_STRATEGIA_INTEGRAZIONE_CINEBASE.md`, sezione `9.6`, passo `5`
- `Stripe CLI` viene usata come strumento di collaudo e debugging locale dei webhook, non come dipendenza runtime dell'applicazione
- il progetto deve comunque poter completare il flusso sincrono principale anche quando `Stripe CLI` non è attiva, fermo restando che in tale condizione non è possibile validare end-to-end la parte webhook

---

## 2) Requisiti Funzionali Consolidati

## 2.1 Programmazione pubblica (`/programmazione.html`)

La nuova pagina deve:

- mostrare una card per film, non una card per show
- avere UX e layout simili al comportamento di un portale cinema moderno come UCI, ma coerenti con il design system CineBase già in uso
- avere tabs/tag minime:
  - `In evidenza`
  - `In uscita`
  - `Tutti i film`
- permettere il filtro per categoria
- permettere la ricerca per titolo film
- permettere la selezione del cinema tramite modale
- ordinare i cinema nel modale per prossimità geografica quando la geolocalizzazione browser è disponibile
- salvare il cinema selezionato:
  - in `localStorage` per anonimo
  - nel profilo backend per utente autenticato
- sincronizzare coerentemente `localStorage` e backend quando l'utente effettua login
- evidenziare in pagina il cinema selezionato
- indicare in ogni card se il film è disponibile o meno nel cinema selezionato
- navigare alla scheda film cliccando la card

## 2.2 Scheda film (`/scheda-film.html`)

La pagina deve mostrare:

- immagine copertina
- titolo
- durata
- data di rilascio
- categorie/genere
- descrizione lunga fino a 2000 caratteri
- regista
- cast
- pulsante `Vai agli show`
- rail orizzontale con date scorrevoli e frecce destra/sinistra
- per la data selezionata:
  - cinema selezionato con nome, città e indirizzo
  - gruppi per tipologia sala
  - bottoni orario di inizio show

Regole aggiuntive:

- se esistono più show allo stesso orario per la stessa tipologia ma su sale diverse, i bottoni devono restare distinti con badge o label `Sala #N`
- il click su un orario porta a `acquista.html` se autenticato
- il click su un orario porta a `login.html?redirect=<url-target>` se anonimo

## 2.3 Pagina cinema per utenti finali (`/my-cinemas.html`)

La pagina deve supportare due viste:

### Vista elenco cinema

- card con nome cinema, città, indirizzo, tipologie di sala presenti
- click su card verso `my-cinemas.html?IdCinema=<id>`

### Vista dettaglio cinema

- header con nome cinema, città e indirizzo
- rail date orizzontale con frecce
- per la data selezionata, elenco dei film in programma nel cinema
- per ogni film:
  - immagine
  - titolo
  - estratto descrizione
  - gruppi per tipologia sala
  - bottoni orario
- bottoni orario auth-aware come in `scheda-film.html`

## 2.4 Modello show multi-sala e area admin

Il nuovo modello deve garantire che:

- uno show sia definito da `film + cinema + sala + data/ora inizio`
- una sala sia identificata univocamente all'interno del proprio cinema da un progressivo
- le tipologie minime supportate siano almeno `2D`, `3D`, `ISENSE`, `XL`
- sia impossibile creare show sovrapposti nella stessa sala
- siano invece consentiti show contemporanei in sale diverse anche con la stessa tipologia
- `proiezioni.html` venga rifatta come workspace di gestione show multi-sala
- esista una nuova pagina `sale.html` per CRUD sale e gestione piantina posti

## 2.5 Acquisto biglietti (`/acquista.html`) e pagamento (`/pagamento.html`)

L'esperienza utente deve prevedere:

- riepilogo show acquistato: film, cinema, sala, data, ora, numero posti, totale
- piantina posti con stati distinti almeno per:
  - disponibile
  - occupato/venduto
  - in hold da altro utente
  - selezionato dall'utente corrente
- massimo 10 posti per singolo acquisto
- countdown visibile dal primo posto selezionato
- passaggio a `pagamento.html` solo con almeno un posto valido in hold
- supporto ai metodi di pagamento:
  - carta di credito con Stripe
  - credito piattaforma
  - pagamento misto credito + carta

## 2.6 Credito piattaforma

Deve esistere una pagina protetta per `PowerUser` e `Admin` che permetta:

- ricerca utente per email
- visualizzazione saldo attuale
- ricarica di credito
- registrazione audit completa con:
  - importo
  - data/ora
  - utente operatore che ha eseguito la ricarica
  - eventuale cinema operativo
  - note opzionali

L'utente finale deve poter vedere in profilo:

- saldo attuale
- storico movimenti rilevanti

## 2.7 Ticket digitale e validazione in ingresso

Dopo un pagamento completato con successo, il sistema deve:

- mostrare una pagina di esito acquisto
- emettere un biglietto per ogni posto acquistato
- generare un PDF multipagina con un biglietto per pagina
- inviare email con riepilogo acquisto e PDF allegato
- includere in ogni biglietto almeno:
  - titolo film
  - data/ora show
  - nome cinema, città, indirizzo, codice locale
  - sala, settore, fila, posto
  - prezzo, supplemento, totale
  - barcode
  - codice biglietto in chiaro
  - QR code che punta alla pagina di validazione

La validazione deve supportare:

- inserimento manuale del codice
- scansione QR/barcode da smartphone/tablet
- apertura diretta di URL del tipo `validazione-biglietti.html?codice=...`
- vincolo del cinema operativo selezionato dall'operatore per evitare validazioni fuori sede
- tracciamento di data/ora di validazione e utente operatore

## 2.8 Compatibilità e migrazione

Poiché il repository attuale usa ancora `Proiezione` e `Prenotazione`, il piano deve essere implementato in modo graduale:

- non si rimuovono subito modelli ed endpoint legacy in Fase 1
- si introduce il nuovo dominio affiancandolo a quello esistente
- si migra il dato storico in modo non distruttivo
- si mantiene un bridge temporaneo per `proiezioni` fino a quando admin e frontend pubblici non saranno completati
- `Prenotazione` legacy non viene convertita automaticamente in `Biglietto`, perché manca il dettaglio di posto e di pagamento

## 2.9 Requisiti non funzionali e di robustezza

Sono obbligatori:

- prevenzione race condition su posti/show
- calcolo prezzi solo lato backend
- idempotenza su creazione ordine e pagamento
- verifica firma webhook Stripe
- sanitizzazione dei redirect post-login per evitare open redirect
- comportamento responsive su desktop e mobile
- test automatici estesi al nuovo dominio
- manual verification end-to-end dei principali flussi utente e staff

---

## 3) Decisioni Architetturali e Default Operativi

## 3.1 Terminologia di dominio

- Business term principale: `Show`
- Nella UI amministrativa e pubblica si usano i termini `show` o `spettacolo` in base al contesto
- `Proiezione` resta solo come compat layer temporaneo fino alla chiusura della migrazione
- Il path pagina `proiezioni.html` viene mantenuto per non rompere navigazione e route guard, ma la pagina diventa concettualmente la gestione show

## 3.2 Strategia di compatibilità legacy

- `Proiezione` e `Prenotazione` non vengono rimossi in Fase 1
- si aggiunge il nuovo schema senza rompere il codice corrente
- si migra il dato storico da `Proiezione` a `Show`
- appena disponibile `ShowService`, gli endpoint legacy `proiezioni` vengono reindirizzati internamente al nuovo dominio tramite adapter DTO
- `Prenotazione` legacy resta disponibile solo fino a quando `profilo.html`, `acquista.html` e `pagamento.html` non saranno operativi sul nuovo dominio
- la rimozione definitiva del legacy e spostata alla Fase 13, e solo se tutta la suite e i flussi end-to-end risultano stabili

## 3.3 Timezone e gestione date/ore

- Persistenza backend: UTC (`StartAtUtc`, `CreatedAtUtc`, `PaidAtUtc`, `ValidatoAtUtc`)
- Rendering frontend: timezone locale Italia (`Europe/Rome`)
- Logiche `In evidenza`, `In uscita`, TTL hold, cleanup e validazione usano tempo normalizzato backend

## 3.4 Selezione e sincronizzazione cinema preferito

Strategia raccomandata:

1. Anonimo:
   - leggere/scrivere `cb_selected_cinema` in `localStorage`
2. Utente autenticato al caricamento pagina:
   - chiamare `GET /profilo/cinema-preferito`
   - se backend ha un cinema preferito, usarlo come source of truth e aggiornare `localStorage`
   - se backend non ha un cinema preferito ma `localStorage` ne ha uno valido, salvarlo con `PUT /profilo/cinema-preferito`
   - se nessuna sorgente ha un cinema valido, lasciare stato non selezionato e invitare alla scelta
3. Cambio cinema da modale:
   - anonimo: aggiorna solo `localStorage`
   - autenticato: aggiorna backend e poi `localStorage`

## 3.5 Logica delle tabs in programmazione

### `In evidenza`

- film con almeno 1 show nei prossimi 7 giorni nella rete cinema
- ordinamento principale per numero show decrescente nei prossimi 7 giorni
- tie-breaker consigliati:
  - disponibilità nel cinema selezionato
  - data del prossimo show più vicina
  - titolo ASC

### `In uscita`

- film con `DataRilascio` compresa tra oggi e oggi + 14 giorni
- non devono essere già in programmazione immediata nel cinema selezionato o, in assenza di cinema selezionato, non devono avere show attivi nel giorno corrente
- se un film ha già show oggi, non deve stare in `In uscita`

### `Tutti i film`

- include i film rilevanti per la discovery pubblica:
  - film con almeno 1 show futuro
  - oppure film con `DataRilascio` entro 14 giorni
- non deve necessariamente coincidere con l'intero catalogo storico del DB

## 3.6 Strategia anti-race per i posti

Soluzione raccomandata: tabella dedicata per stato posto-show con unique su `ShowId + SalaPostoId`.

Flusso raccomandato:

1. L'utente seleziona il primo posto in `acquista.html`
2. Il frontend richiede la creazione di un `holdToken`
3. Il backend, in transazione atomica:
   - pulisce i hold scaduti per quello show
   - verifica che i posti richiesti non siano già in stato `Hold` attivo o `Sold`
   - inserisce/aggiorna record `ShowPostoStato` con stato `Hold`
4. Dal primo hold parte un countdown frontend
5. Il frontend invia keep-alive periodici per estendere il TTL del hold finché l'utente resta attivo in acquisto/pagamento
6. Al pagamento completato, in una transazione unica:
   - si verifica che il hold sia ancora valido e appartenga all'utente
   - si crea/aggiorna l'ordine
   - si converte lo stato dei posti da `Hold` a `Sold`
   - si emettono i biglietti
7. Se il hold scade o l'utente abbandona, i posti tornano disponibili

Dettagli operativi:

- TTL raccomandato: 10 minuti
- cleanup doppio:
  - lazy cleanup su endpoint hold/seat-map
  - background cleanup periodico
- risposta di conflitto: `409 Conflict` con elenco posti non acquisibili

## 3.7 Strategia pagamento e idempotenza

- Il frontend non calcola mai il totale definitivo come source of truth; il totale viene sempre ricalcolato dal backend.
- L'ordine nasce in stato `Pending` a partire da un hold valido.
- Ogni richiesta di creazione ordine o pagamento deve accettare una `Idempotency-Key` client-generated.
- Pagamento con credito:
  - backend verifica saldo attuale al momento della finalizzazione
  - se saldo insufficiente, rifiuta e chiede nuova scelta split
- Pagamento con carta:
  - Stripe `PaymentIntent`
  - conferma solo dopo stato coerente del payment intent
- Pagamento misto:
  - il backend valida lo split richiesto
  - il backend ricalcola il residuo carta
  - la finalizzazione resta atomica lato ordine/ticket

Nota strategica approvata per Stripe:

- il flusso applicativo locale e didattico resta inizialmente sincrono dal punto di vista del business, con verifica backend dello stato reale del `PaymentIntent`
- il webhook Stripe viene comunque implementato nella stessa fase, con verifica firma e gestione replay-safe
- per il collaudo locale del webhook si adotta `Scenario B - Test webhook in locale con Stripe CLI`
- durante i test webhook locali, il valore di `STRIPE_WEBHOOK_SECRET` deve provenire dal listener di `Stripe CLI`
- il listener `Stripe CLI` non è necessario per il solo happy path sincrono, ma è necessario quando si vogliono collaudare inoltro webhook, firma, replay e diagnostica end-to-end

## 3.8 Emissione ticket, PDF ed email

- L'ordine pagato e il biglietto emesso sono la source of truth.
- PDF ed email sono attività post-pagamento e non devono annullare un ordine già pagato se SMTP o PDF falliscono.
- Si raccomanda di registrare nello `Ordine` almeno stato invio email/timestamp/ultimo errore, per consentire retry e download manuale PDF.
- In profilo utente deve esistere un fallback di consultazione anche se l'email non viene consegnata.

## 3.9 Validazione biglietti e cinema operativo

- L'operatore PowerUser/Admin deve selezionare o confermare il cinema operativo all'inizio della sessione di validazione
- Il cinema operativo può essere memorizzato in `sessionStorage` lato browser
- Il backend riceve il `cinemaId` operativo e blocca la validazione se il biglietto appartiene ad altro cinema
- In modalità scanner QR, se l'URL contiene `?codice=` e il cinema operativo è già noto, il sistema può entrare in modalità `validazione rapida` con un numero minimo di tap

## 3.10 Sicurezza e affidabilità aggiuntive

- consentire solo redirect relativi interni su `login.html?redirect=...`
- applicare rate limiting sugli endpoint di validazione biglietti
- verificare sempre coerenza `Show.CinemaId == Sala.CinemaId`
- verificare sempre ownership per ordini, ticket, hold e profilo utente
- verificare firma webhook Stripe e gestire replay idempotenti

---

## 4) Design Tecnico - Modello Dati

## 4.1 Nuovi enum suggeriti

```text
TipoSala
- DueD = 0
- TreD = 1
- ISENSE = 2
- XL = 3

ShowPostoState
- Hold = 0
- Sold = 1

OrdineState
- Pending = 0
- Paid = 1
- Failed = 2
- Cancelled = 3
- Expired = 4

BigliettoState
- Issued = 0
- Validated = 1
- Cancelled = 2

MovimentoCreditoTipo
- TopUp = 0
- DebitOrder = 1
- Refund = 2
- Adjustment = 3
```

## 4.2 Nuove entità

### Sala

```text
Sala(
  Id int PK,
  CinemaId int FK,
  NumeroProgressivo int required,
  TipoSala TipoSala required,
  Nome string? max 100,
  Supplemento decimal(10,2) required default 0,
  IsAttiva bool required default true
)
UNIQUE(CinemaId, NumeroProgressivo)
```

Note:

- `Nome` può essere valorizzato automaticamente come `Sala <NumeroProgressivo>` se non fornito
- `Supplemento` permette di modellare la differenza di prezzo per sala `ISENSE`, `XL`, `3D`, ecc.

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
  IsWheelchair bool required default false,
  IsAttivo bool required default true
)
UNIQUE(SalaId, Settore, Fila, Numero)
```

Note:

- `PosX/PosY` servono per rendering e anteprima editor piantina
- la pagina admin sale usa `SalaPosto` come persistenza reale, non un semplice JSON opaco

### Show

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

Note:

- `DurataMinutiSnapshot` evita ambiguità se in futuro la durata del film cambia dopo la schedulazione
- `CinemaId` è ridondante rispetto a `SalaId`, ma utile per migrazione, query e compatibilità con il dominio attuale; il service deve imporre coerenza con `Sala.CinemaId`

### ShowPostoStato

```text
ShowPostoStato(
  Id int PK,
  ShowId int FK,
  SalaPostoId int FK,
  UserId int FK,
  Stato ShowPostoState required,
  HoldToken string? max 120,
  ScadeAtUtc datetime?,
  OrdineId int? FK,
  UpdatedAtUtc datetime required
)
UNIQUE(ShowId, SalaPostoId)
INDEX(HoldToken)
INDEX(ScadeAtUtc)
```

Note:

- se `Stato = Hold`, `ScadeAtUtc` e obbligatorio
- se `Stato = Sold`, `ScadeAtUtc` può essere `NULL`
- i record scaduti in `Hold` vengono cancellati da lazy cleanup e background cleanup
- questo modello e preferito rispetto a un semplice `SeatLock` con unique su `(ShowId, Fila, Posto, ExpiresAt)`, che non garantirebbe l'esclusione reale dei conflitti

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
  HoldToken string required max 120,
  NumeroBiglietti int required,
  TotaleLordo decimal(10,2) required,
  ImportoCredito decimal(10,2) required default 0,
  ImportoCarta decimal(10,2) required default 0,
  StripePaymentIntentId string? max 120,
  IdempotencyKey string? max 120,
  Stato OrdineState required,
  CreatedAtUtc datetime required,
  PaidAtUtc datetime?,
  TicketEmailSentAtUtc datetime?,
  TicketEmailLastError string? max 1000
)
UNIQUE(CodiceOrdine)
UNIQUE(IdempotencyKey)
```

Note:

- `IdempotencyKey` e opzionale ma raccomandata per robustezza di creazione ordine e pagamento
- `TicketEmailSentAtUtc` e `TicketEmailLastError` evitano di legare il successo dell'ordine alla sola consegna email

### Biglietto

```text
Biglietto(
  Id int PK,
  OrdineId int FK,
  ShowId int FK,
  SalaPostoId int FK,
  UserId int FK,
  CodiceBiglietto string required unique,
  BarcodeValue string required,
  PrezzoBase decimal(10,2) required,
  Supplemento decimal(10,2) required default 0,
  PrezzoTotale decimal(10,2) required,
  Stato BigliettoState required,
  ValidatoAtUtc datetime?,
  ValidatoDaUserId int? FK,
  ValidatoCinemaId int? FK
)
UNIQUE(ShowId, SalaPostoId)
```

Note:

- `CodiceBiglietto` e il codice mostrato in chiaro e usato da barcode/QR
- il QR può codificare direttamente un URL che trasporta `CodiceBiglietto`

### MovimentoCredito

```text
MovimentoCredito(
  Id int PK,
  UserId int FK,
  Tipo MovimentoCreditoTipo required,
  Importo decimal(10,2) required,
  SaldoPre decimal(10,2) required,
  SaldoPost decimal(10,2) required,
  OperatoreUserId int? FK,
  CinemaId int? FK,
  OrdineId int? FK,
  CreatedAtUtc datetime required,
  Note string? max 500
)
```

Note:

- `OperatoreUserId` e valorizzato nelle ricariche manuali eseguite da PowerUser/Admin
- `OrdineId` e valorizzato quando il credito e speso per un acquisto

## 4.3 Modifiche entità esistenti

### Film

Da aggiungere:

- `DescrizioneLunga string? max 2000`
- `CastText string? max 2000`
- `DataRilascio DateOnly?` oppure `DateTime?` normalizzato a data
- navigation `ICollection<Show> Shows`

Nota:

- `CastText` può essere gestito come testo separato da newline o virgole in storage e trasformato in `CastList` nei DTO, evitando una normalizzazione più pesante in questa iterazione

### Cinema

Da aggiungere:

- `Latitudine double?`
- `Longitudine double?`
- `Telefono string? max 20`
- `CodiceLocale string? max 50`
- navigation `ICollection<Sala> Sale`

### User

Da aggiungere:

- `CinemaPreferitoId int? FK`
- `CreditoResiduo decimal(10,2) required default 0`
- navigation `ICollection<Ordine> Ordini`
- navigation `ICollection<Biglietto> Biglietti`

## 4.4 Entità legacy da mantenere temporaneamente

### Proiezione

- resta mappata durante la transizione
- diventa progressivamente un adapter sul nuovo dominio `Show`
- i vecchi endpoint e la vecchia UI vengono sostituiti gradualmente senza rimozione immediata

### Prenotazione

- resta mappata finché `profilo.html` non è stato migrato a `Ordine` + `Biglietto`
- i record esistenti non possono essere convertiti automaticamente in ticket per mancanza di informazioni di posto e pagamento
- a fine iterazione si decidera se:
  - mantenerla solo come storico legacy in sola lettura
  - oppure rimuoverla dopo export/documentazione, se non più necessaria

## 4.5 Relazioni e delete behavior raccomandati

| Relazione | Behavior consigliato |
| --- | --- |
| Cinema -> Sala | Restrict |
| Sala -> SalaPosto | Cascade |
| Sala -> Show | Restrict |
| Film -> Show | Restrict |
| Show -> ShowPostoStato | Cascade |
| Show -> Biglietto | Restrict |
| Ordine -> Biglietto | Cascade |
| User -> Ordine | Restrict |
| User -> Biglietto | Restrict |
| User -> MovimentoCredito (beneficiario) | Restrict |
| User -> MovimentoCredito (operatore) | Restrict |
| User -> CinemaPreferito | SetNull |

## 4.6 Vincoli business obbligatori

1. `UNIQUE(CinemaId, NumeroProgressivo)` su `Sala`
2. `UNIQUE(SalaId, Settore, Fila, Numero)` su `SalaPosto`
3. `UNIQUE(CinemaId, SalaId, StartAtUtc)` su `Show`
4. `UNIQUE(ShowId, SalaPostoId)` su `ShowPostoStato`
5. `UNIQUE(ShowId, SalaPostoId)` su `Biglietto`
6. `UNIQUE(CodiceOrdine)` su `Ordine`
7. `UNIQUE(CodiceBiglietto)` su `Biglietto`
8. massimo 10 posti per ordine/acquisto
9. nessuna sovrapposizione temporale tra show nella stessa sala
10. un biglietto può essere validato una sola volta

## 4.7 Strategia di data migration da dominio legacy a dominio nuovo

Migrazione raccomandata in più passi:

1. aggiungere nuove tabelle e nuovi campi senza eliminare `Proiezione` e `Prenotazione`
2. per ogni cinema esistente:
   - creare almeno una sala default `Sala 1`, tipo `2D`, supplemento `0`
3. migrare ogni `Proiezione` legacy in uno `Show`
4. assegnazione sala durante la migrazione:
   - provare ad assegnare la `Sala 1`
   - se nella stessa sala si genera conflitto di sovrapposizione o unique, creare automaticamente una nuova sala default `Sala auto-migrata N` e assegnare li lo show
5. valorizzare `PrezzoBase` con `DEFAULT_TICKET_PRICE` se il dato non esiste nello storico
6. non migrare automaticamente `Prenotazione` in `Biglietto`
7. mantenere gli endpoint legacy fino al completamento del refactor frontend/admin

Nota importante:

- questo algoritmo evita che dati legacy potenzialmente incoerenti o sovrapposti blocchino la migration dell'intero database

## 4.8 Read models / DTO chiave per il frontend pubblico

### `ProgrammazioneFilmDTO`

Campi consigliati:

- `Id`
- `Titolo`
- `CopertinaPath`
- `Durata`
- `Categorie`
- `DataRilascio`
- `InEvidenza`
- `InUscita`
- `ShowCountNext7Days`
- `DisponibileNelCinemaSelezionato`
- `ProssimoShowNelCinemaSelezionato`

### `FilmSchedaDTO`

Campi consigliati:

- metadati film completi
- `CastList`
- `CinemaSelezionato`
- `ShowCalendar` raggruppato per `Data -> TipoSala -> lista show`

### `CinemaCardDTO`

Campi consigliati:

- `Id`, `Nome`, `Citta`, `Indirizzo`
- `TipologieSalePresenti`
- `DistanzaKm?`

### `CinemaScheduleDayDTO`

Campi consigliati:

- `Cinema`
- `Data`
- `Films[]`
  - ogni film contiene summary e gruppi `TipoSala -> show[]`

### `SeatMapDTO`

Campi consigliati:

- summary show
- summary sala
- `ScadeAtUtc` del hold corrente se presente
- elenco posti con stato `Available`, `HeldByOther`, `HeldByMe`, `Sold`

### `OrdineDetailDTO`

Campi consigliati:

- riepilogo ordine
- film/cinema/sala/show
- importi credito/carta/totale
- elenco ticket generati
- stato email/PDF

---

## 5) API e Permessi (delta Iterazione 4)

Nota: gli endpoint già definiti in Iterazione 3 restano validi salvo adattamenti interni; qui sono elencati solo gli endpoint nuovi o significativamente evoluti.

## 5.1 Endpoint pubblici

| Endpoint | Scopo |
| --- | --- |
| `GET /programmazione/films?tab=&search=&categoriaId=&cinemaId=` | listing film per `programmazione.html` |
| `GET /programmazione/cinemas?lat=&lng=` | elenco cinema ordinato per prossimità o fallback |
| `GET /films/{id}/scheda?cinemaId=` | scheda film completa con show calendar |
| `GET /my-cinemas` | elenco cinema per pagina cinema-centric |
| `GET /my-cinemas/{cinemaId}/schedule?date=` | programmazione giornaliera di un cinema |
| `GET /shows` | listing show pubblico per compatibilità/uso admin futuro |
| `GET /shows/{id}` | dettaglio show |
| `GET /cinemas/{cinemaId}/sale` | lista sale di un cinema, almeno in read |
| `GET /sale/{salaId}` | dettaglio sala e metadati |

## 5.2 Endpoint autenticati (`Authenticated`)

| Endpoint | Scopo |
| --- | --- |
| `GET /profilo/cinema-preferito` | recupero cinema preferito |
| `PUT /profilo/cinema-preferito` | update cinema preferito |
| `GET /checkout/shows/{showId}/seat-map` | piantina posti con stati show-specifici |
| `POST /checkout/holds` | crea/aggiorna hold posti |
| `POST /checkout/holds/{holdToken}/refresh` | estende TTL hold corrente |
| `DELETE /checkout/holds/{holdToken}` | rilascio esplicito hold |
| `POST /checkout/orders` | crea ordine pendente da hold valido |
| `GET /checkout/orders` | lista ordini utente corrente |
| `GET /checkout/orders/{orderId}` | dettaglio ordine con ownership check |
| `POST /checkout/orders/{orderId}/pay` | finalizzazione pagamento carta/credito/misto |
| `GET /checkout/tickets` | lista biglietti utente corrente |
| `GET /checkout/tickets/{ticketId}` | dettaglio biglietto con ownership check |
| `GET /checkout/orders/{orderId}/pdf` | download PDF ordine |
| `GET /credito/me` | saldo e movimenti utente corrente |

## 5.3 Endpoint `PowerUserOrAdmin`

| Endpoint | Scopo |
| --- | --- |
| `POST /cinemas/{cinemaId}/sale` | crea sala |
| `PUT /sale/{salaId}` | modifica sala |
| `DELETE /sale/{salaId}` | elimina sala |
| `PUT /sale/{salaId}/posti` | salva piantina completa della sala |
| `GET /sale/{salaId}/posti` | legge piantina sala |
| `POST /shows` | crea show |
| `PUT /shows/{showId}` | modifica show |
| `DELETE /shows/{showId}` | elimina show |
| `POST /admin/credito/ricariche` | ricarica credito utente |
| `GET /admin/credito/ricariche?email=` | storico ricariche |
| `GET /admin/credito/users?email=` | ricerca utente per ricarica |
| `GET /admin/tickets/validate/{code}` | lookup ticket per validazione |
| `POST /admin/tickets/validate` | validazione ticket |

## 5.4 Endpoint `AdminOnly`

Gli endpoint di amministrazione utenti e di CRUD cinema restano quelli dell'iterazione 3.

In particolare:

- `GET /admin/utenti`
- `PUT /admin/utenti/{id}/ruolo`
- `POST/PUT/DELETE /cinemas`

## 5.5 Webhook e callback esterni

| Endpoint | Auth |
| --- | --- |
| `POST /payments/stripe/webhook` | `AllowAnonymous` + verifica firma Stripe |

## 5.6 Matrice permessi pagine frontend aggiornata

| Pagina | Anonimo | User | PowerUser | Admin |
| --- | --- | --- | --- | --- |
| `index.html` | SI | SI | SI | SI |
| `programmazione.html` | SI | SI | SI | SI |
| `scheda-film.html` | SI | SI | SI | SI |
| `my-cinemas.html` | SI | SI | SI | SI |
| `login.html` | SI | - | - | - |
| `registrazione.html` | SI | - | - | - |
| `profilo.html` | - | SI | SI | SI |
| `acquista.html` | - | SI | SI | SI |
| `pagamento.html` | - | SI | SI | SI |
| `esito-acquisto.html` | - | SI | SI | SI |
| `dashboard.html` | - | - | SI | SI |
| `films.html` | - | - | SI | SI |
| `registi.html` | - | - | SI | SI |
| `cinemas.html` | - | - | - | SI |
| `proiezioni.html` | - | - | SI | SI |
| `sale.html` | - | - | SI | SI |
| `categorie.html` | - | - | SI | SI |
| `ricarica-credito.html` | - | - | SI | SI |
| `validazione-biglietti.html` | - | - | SI | SI |

## 5.7 Regole di redirect obbligatorie

1. pagina protetta e utente anonimo -> `login.html?redirect=<relative-url>`
2. `login.html` e `registrazione.html` restano anonimo-only
3. utente loggato senza ruolo sufficiente -> `index.html?forbidden=true`
4. i redirect devono accettare solo path relativi interni (`/pagina?...`)
5. click su orario show da pagina pubblica:
   - autenticato -> `acquista.html?...`
   - anonimo -> `login.html?redirect=<url acquista>`

---

## 6) Frontend UX Plan

## 6.1 `programmazione.html` v2

Elementi principali:

- area header con cinema selezionato ben visibile
- bottone `Cambia cinema`
- modale cinema con search opzionale, ordinamento per distanza, card cinema essenziali
- tabs `In evidenza`, `In uscita`, `Tutti i film`
- search input per titolo
- filtro categoria
- griglia card film

Contenuto minimo card film:

- copertina
- titolo
- durata
- categorie
- indicatore disponibilità nel cinema selezionato
- eventuale data rilascio se il film e in `In uscita`
- CTA implicita sul click card verso `scheda-film.html`

## 6.2 `scheda-film.html`

Layout consigliato:

- hero con copertina a sinistra e metadata a destra
- descrizione lunga in area dedicata
- cast come lista leggibile o pill lineari
- pulsante `Vai agli show` con scroll alla sezione show
- rail orizzontale date riusabile anche altrove
- gruppi show per tipologia sala e orari

Dettagli UX importanti:

- se non c'e cinema selezionato, la pagina deve invitare a sceglierne uno prima di mostrare gli show
- gli show vanno filtrati sul cinema selezionato
- se non ci sono show per la data selezionata, mostrare empty state esplicito

## 6.3 `my-cinemas.html`

Comportamento:

- senza query param: lista cinema
- con `?IdCinema=`: dettaglio cinema con rail date e lista film del giorno

Dettagli UX importanti:

- le tipologie sala presenti in lista cinema devono derivare dai dati reali delle sale
- nella vista dettaglio, il raggruppamento deve essere `film -> tipologia sala -> orari`
- se più show hanno lo stesso orario ma sale diverse, mostrare `Sala #N`

## 6.4 `acquista.html`

Elementi principali:

- riepilogo show e prezzo in card laterale o header sticky
- piantina posti responsive
- legenda colori/stati
- countdown hold
- lista posti selezionati
- CTA `Continua`

Dettagli UX importanti:

- refresh periodico seat-map per mostrare hold altrui aggiornati
- keep-alive hold automatico mentre la pagina e attiva
- messaggi espliciti se il hold scade o un posto diventa indisponibile

## 6.5 `pagamento.html` ed `esito-acquisto.html`

`pagamento.html` deve mostrare:

- riepilogo ordine
- saldo credito disponibile
- opzioni pagamento:
  - solo carta
  - solo credito
  - misto
- calcolo split in tempo reale
- form Stripe Elements per la sola quota carta

`esito-acquisto.html` deve mostrare:

- esito pagamento
- riepilogo ordine/ticket
- stato invio email se disponibile
- CTA verso profilo e programmazione

## 6.6 `sale.html`

Workspace admin con:

- filtro cinema
- tabella sale del cinema selezionato
- modale crea/modifica sala
- editor visuale piantina posti
- anteprima piantina
- salvataggio completo configurazione posti

L'editor visuale deve consentire almeno:

- definizione numero file
- numero posti per fila
- settore per gruppi di file
- preview dei posti come bottoni

## 6.7 `proiezioni.html` rifatta come gestione show

La pagina deve diventare un workspace show multi-sala con:

- colonne: film, cinema, sala, tipologia, start date/time, prezzo, stato
- cascading dropdown cinema -> sala nel form
- filtri per cinema, sala, film, data
- supporto a errori di sovrapposizione mostrati chiaramente

## 6.8 `validazione-biglietti.html`

Elementi principali:

- selettore cinema operativo
- input manuale codice
- bottone scanner QR/barcode
- supporto a query string `?codice=`
- card esito con dettagli ticket
- modalità validazione rapida per staff su mobile/tablet

## 6.9 `ricarica-credito.html`

Elementi principali:

- ricerca utente per email
- saldo attuale
- input importo e note
- bottone ricarica
- storico ricariche recenti

## 6.10 `profilo.html` v2

Evoluzione prevista:

- mantenere sezione dati personali
- sostituire progressivamente la sezione prenotazioni con:
  - cinema preferito
  - saldo credito e movimenti
  - ordini effettuati
  - biglietti acquistati
  - download PDF ordine

Nota importante:

- la rimozione definitiva della UI prenotazioni legacy deve avvenire solo quando gli endpoint `checkout/orders` e `checkout/tickets` sono stabili e coperti da test

---

## 7) Fasi di Implementazione (incrementale)

### FASE 1 - Modello dati v2 e compat layer

**Obiettivo**: introdurre il nuovo dominio multisala/ticketing senza rompere subito il codice legacy.

**Attivita**:

1. Creare i nuovi enum e modelli:
   - `TipoSala`
   - `ShowPostoState`
   - `OrdineState`
   - `BigliettoState`
   - `MovimentoCreditoTipo`
   - `Sala`
   - `SalaPosto`
   - `Show`
   - `ShowPostoStato`
   - `Ordine`
   - `Biglietto`
   - `MovimentoCredito`
2. Estendere `Film`, `Cinema`, `User` con i campi nuovi.
3. Aggiornare `FilmDbContext` con nuovi `DbSet`, relazioni, indici unici e delete behavior.
4. NON rimuovere ancora `Proiezione` e `Prenotazione`.
5. Preparare DTO minimi e config class condivise per la fase successiva.
6. Aggiornare `.env.example` e configurazione applicativa con le chiavi necessarie ma senza ancora integrare Stripe/SMTP.
7. Verificare compilazione e adattare eventuali test baseline solo dove il nuovo modello ha impatto diretto sulla build.

**Verifica fase**:

- compilazione backend OK
- nuove entità disponibili nel codice
- nessuna rimozione distruttiva del legacy

**Checklist fase**:

- [x] Nuovi enum creati
- [x] Nuove entità create
- [x] `Film`, `Cinema`, `User` estesi
- [x] `FilmDbContext` aggiornato con nuovi `DbSet`, relazioni e vincoli
- [x] `Proiezione` e `Prenotazione` ancora presenti
- [x] Config e placeholder env aggiunti
- [x] Build backend verde

---

### FASE 2 - Migration, seed e data migration legacy

**Obiettivo**: applicare lo schema nuovo e migrare lo storico senza perdita dati.

**Attivita**:

1. Creare migration `AddMultisalaTicketing`.
2. Aggiornare `DataSeeder` per supportare, in ambiente dev, sale e show di esempio.
3. Implementare data migration da `Proiezione` a `Show`.
4. Creare almeno una sala default per ogni cinema esistente.
5. Gestire i conflitti di migrazione creando sale auto-migrate aggiuntive quando necessario.
6. Inizializzare `CreditoResiduo = 0` per utenti esistenti se non valorizzato.
7. Non migrare automaticamente `Prenotazione` in `Biglietto`.
8. Applicare la migration e verificare lo stato del DB.

**Verifica fase**:

- migration applicata correttamente
- show legacy migrati
- nessuna perdita dati sulle proiezioni storiche
- gestione conflitti sala automatica verificata

**Checklist fase**:

- [x] Migration creata
- [x] Migration applicata
- [x] Sale default create per i cinema esistenti
- [x] Proiezioni legacy migrate a show
- [x] Gestione conflitti di migrazione verificata
- [x] Seed dev aggiornato
- [x] Test baseline ancora verdi o adattati con esito documentato

---

### FASE 3 - Backend catalogo pubblico, scheda film e cinema preferito

**Obiettivo**: fornire al frontend pubblico le API aggregate necessarie per `programmazione.html`, `scheda-film.html` e `my-cinemas.html`.

**Attivita**:

1. Estendere `FilmDTO`, `FilmCreateDTO`, `FilmUpdateDTO` con:
   - `DescrizioneLunga`
   - `CastText` o `CastList`
   - `DataRilascio`
2. Implementare servizio/endpoint per `GET /programmazione/films` con logiche `In evidenza`, `In uscita`, `Tutti`.
3. Implementare `GET /programmazione/cinemas?lat=&lng=` con distanza e fallback ordinamento.
4. Implementare `GET /films/{id}/scheda?cinemaId=`.
5. Implementare `GET /my-cinemas` e `GET /my-cinemas/{cinemaId}/schedule?date=`.
6. Estendere `ProfiloService` o servizio dedicato per `GET/PUT /profilo/cinema-preferito`.
7. Implementare DTO read-model aggregati per discovery pubblica.
8. Aggiungere test integrazione per:
   - tabs featured/upcoming/all
   - filtro categoria + search
   - cinema preferito
   - ordinamento per distanza

**Verifica fase**:

- payload pubblici coerenti con le nuove pagine
- film detail completo disponibile da API
- cinema preferito leggibile/scrivibile

**Checklist fase**:

- [x] DTO film estesi
- [x] Endpoint `GET /programmazione/films` disponibile
- [x] Endpoint cinema nearby/discovery disponibile
- [x] Endpoint `GET /films/{id}/scheda` disponibile
- [x] Endpoint `my-cinemas` disponibili
- [x] `GET/PUT /profilo/cinema-preferito` implementati
- [x] Test integrazione catalogo pubblico verdi

---

### FASE 4 - Backend sale e piantina posti

**Obiettivo**: implementare il dominio sala e la persistenza reale dei posti.

**Attivita**:

1. Creare DTO sala e piantina:
   - `SalaDTO`
   - `SalaCreateDTO`
   - `SalaUpdateDTO`
   - `SalaPostoDTO`
   - `SalaLayoutSaveDTO`
2. Implementare `ISalaService/SalaService` con CRUD sale.
3. Implementare salvataggio completo piantina tramite lista `SalaPosto`.
4. Validare unicità numero sala nel cinema.
5. Bloccare delete sala se esistono show futuri o ticket emessi.
6. Esporre endpoint read e CUD coerenti con RBAC `PowerUserOrAdmin`.
7. Aggiungere test integrazione per sale e layout.

**Verifica fase**:

- sale CRUD funzionante
- piantina posti salvabile/leggibile
- validazioni di coerenza attive

**Checklist fase**:

- [x] DTO sala/piantina creati
- [x] `ISalaService`/`SalaService` implementati
- [x] Endpoint sale mappati
- [x] Persistenza piantina `SalaPosto` implementata
- [x] Delete sala protetto da vincoli show/ticket
- [x] Test integrazione sale verdi

---

### FASE 5 - Backend show e bridge legacy proiezioni

**Obiettivo**: rendere `Show` il nuovo dominio operativo e mantenere compatibilità temporanea con `proiezioni`.

**Attivita**:

1. Creare DTO show:
   - `ShowDTO`
   - `ShowCreateDTO`
   - `ShowUpdateDTO`
   - `ShowPagedResultDTO`
2. Implementare `IShowService/ShowService`.
3. Implementare validazione completa anti-overlap:
   - la nuova finestra `[NuovoStart, NuovoEnd)` non deve intersecare nessuna finestra esistente della stessa sala
4. Esporre endpoint `GET/POST/PUT/DELETE /shows` e query per cinema/data/film.
5. Implementare adapter temporaneo per gli endpoint `proiezioni` esistenti:
   - read: projection da show verso `ProiezioneDTO`
   - write: bridge temporaneo verso `ShowService` dove sensato
6. Preparare il backend ai nuovi campi film usati da admin show/film.
7. Aggiungere test integrazione per show e compat layer `proiezioni`.

**Verifica fase**:

- show CRUD funzionante
- anti-overlap robusto verificato
- endpoint legacy `proiezioni` ancora utilizzabili durante la transizione

**Checklist fase**:

- [x] DTO show creati
- [x] `IShowService`/`ShowService` implementati
- [x] Validazione anti-overlap implementata
- [x] Endpoint show mappati
- [x] Bridge legacy `proiezioni` implementato/documentato
- [x] Test show e compatibilità verdi

---

### FASE 6 - Backend seat map, hold posti e ordine pendente

**Obiettivo**: abilitare la selezione posti reale e la creazione dell'ordine pendente prima del pagamento.

**Attivita**:

1. Creare DTO checkout/seat-map/hold.
2. Implementare `SeatHoldService` o `SeatLockService` sul modello `ShowPostoStato`.
3. Implementare endpoint:
   - `GET /checkout/shows/{showId}/seat-map`
   - `POST /checkout/holds`
   - `POST /checkout/holds/{holdToken}/refresh`
   - `DELETE /checkout/holds/{holdToken}`
4. Implementare background cleanup hold scaduti.
5. Implementare `CheckoutService` per creazione `Ordine` in stato `Pending` a partire da un hold valido.
6. Bloccare massimo 10 posti per ordine.
7. Aggiungere test integrazione e test di concorrenza con richieste parallele.

**Verifica fase**:

- seat map disponibile
- hold posti con TTL funzionante
- ordine pendente creato solo da hold valido
- doppio hold sullo stesso posto impossibile

**Checklist fase**:

- [x] DTO seat-map e hold creati
- [x] Servizio hold implementato su `ShowPostoStato`
- [x] Endpoint hold mappati
- [x] Cleanup hold scaduti attivo
- [x] Creazione ordine pendente implementata
- [x] Limite 10 posti implementato
- [x] Test concorrenza verdi

---

### FASE 7 - Backend pagamento, credito piattaforma e finalizzazione checkout

**Obiettivo**: supportare pagamento carta, credito e misto, con finalizzazione robusta dell'ordine.

**Riferimenti operativi fase**:

- `docs/tutorials/TUTORIAL_STRIPE_GATEWAY_PAGAMENTI.md`
- `docs/tutorials/TUTORIAL_STRIPE_CLI.md`
- `docs/tutorials/TUTORIAL_URL_PUBBLICI_WEBHOOKS.md`
- `docs/tutorials/TUTORIAL_STRIPE_STRATEGIA_INTEGRAZIONE_CINEBASE.md`

**Strategia approvata per la fase**:

- l'integrazione Stripe usa un modello ibrido e progressivo: flusso applicativo principale sincrono più webhook implementato fin da subito
- l'applicazione Stripe di test usata per la fase è `CineBase_Demo`
- per il collaudo locale del webhook si adotta in modo esplicito `Scenario B - Test webhook in locale con Stripe CLI`
- `Stripe CLI` serve per inoltro, verifica firma e debugging webhook locale, ma non è una dipendenza necessaria per il solo happy path sincrono del progetto didattico

**Attivita**:

1. L'operatore umano prepara l'ambiente Stripe `CineBase_Demo` in `test mode`.
   - verificare che la dashboard Stripe sia in `test mode`
   - verificare il riferimento applicativo `CineBase_Demo`
   - recuperare dalla dashboard le standard keys `pk_test_...` e `sk_test_...`
   - verificare che il metodo di pagamento `Card` sia attivo in `test mode`
   - riportare i valori corretti nei file locali di configurazione del progetto
2. L'operatore umano prepara l'ambiente locale di test webhook secondo `Scenario B - Test webhook in locale con Stripe CLI`.
   - installare `Stripe CLI`
   - eseguire `stripe login`
   - avviare un listener locale con inoltro verso `http://localhost:5000/payments/stripe/webhook`
   - usare il comando raccomandato:
     - `stripe listen --events payment_intent.succeeded,payment_intent.payment_failed,payment_intent.canceled --forward-to localhost:5000/payments/stripe/webhook`
   - copiare il `whsec_...` mostrato dalla CLI e usarlo come `STRIPE_WEBHOOK_SECRET` nel backend durante la sessione di test webhook
   - ricordare che, se la CLI non è attiva, l'applicazione deve restare comunque utilizzabile per il flusso sincrono principale, ma il webhook non può essere collaudato end-to-end
3. Integrare `Stripe.net` e configurazione chiavi.
4. Implementare `PagamentoService` con `PaymentIntent` e verifica stato pagamento.
5. Implementare finalizzazione ordine:
   - ricalcolo totale lato backend
   - verifica split credito/carta
   - eventuale addebito credito con `MovimentoCredito`
   - conversione `Hold -> Sold`
   - aggiornamento ordine `Paid`
6. Implementare endpoint `POST /checkout/orders/{orderId}/pay` con `Idempotency-Key`.
7. Implementare endpoint `GET /credito/me`.
8. Implementare endpoint admin di ricarica credito e ricerca utente per email.
9. Implementare `POST /payments/stripe/webhook` con firma verificata e replay-safe.
10. Esporre liste ordini e ticket per il futuro `profilo.html`.
11. Aggiungere test per:
    - solo carta
    - solo credito
    - misto
    - saldo insufficiente
    - webhook replay

**Verifica fase**:

- pagamento carta funzionante in test mode
- happy path sincrono carta funzionante anche senza listener `Stripe CLI` attivo
- webhook verificato in locale tramite `Stripe CLI`
- pagamento solo credito funzionante
- pagamento misto funzionante
- credito ricaricabile con audit operatore

**Checklist fase**:

- [x] Ambiente Stripe `CineBase_Demo` preparato in `test mode`
- [x] Strategia locale `Scenario B - Test webhook in locale con Stripe CLI` preparata e documentata
- [x] Stripe integrato lato backend
- [x] Finalizzazione ordine implementata
- [x] `MovimentoCredito` usato per audit saldo
- [x] Endpoint credito admin e user disponibili
- [x] Webhook Stripe verificato via `Stripe CLI`
- [x] Happy path sincrono verificato anche senza `Stripe CLI`
- [x] Liste ordini/ticket disponibili
- [x] Test pagamento/credito verdi

**Esito reale della fase**:

- pagamento carta in test mode verificato con `tok_visa`
- webhook Stripe verificato in locale con `Stripe CLI`
- ordine finalizzato correttamente con passaggio a `Paid`
- ticket generati a fine pagamento
- saldo credito aggiornato con movimento auditabile

---

### FASE 8 - Backend ticketing digitale, PDF/email e validazione biglietti

**Obiettivo**: emettere ticket digitali completi e consentirne la validazione operativa in cinema.

**Riferimenti operativi fase**:

- `docs/tutorials/TUTORIAL_FASE8_STRATEGIA_TICKETING_EMAIL_PDF_VALIDAZIONE.md`
- `docs/tutorials/TUTORIAL_EMAIL_MAILKIT_BIGLIETTI_PDF_QRCODE.md`
- `docs/tutorials/TUTORIAL_SMTP_GOOGLE_TWILIO_SENDGRID_SETUP_E_TROUBLESHOOTING.md`

**Strategia approvata per la fase**:

- l'implementazione della fase usa come baseline operativa i server SMTP di Google, per semplicità didattica e rapidità di collaudo locale
- l'architettura della fase deve però essere pronta a funzionare anche con una soluzione SMTP `Twilio SendGrid`
- in questa fase è accettabile predisporre il supporto configurativo e architetturale per `Twilio SendGrid` senza usarlo come provider principale di collaudo
- il servizio email non deve essere accoppiato a Google in modo rigido; deve restare compatibile con provider SMTP equivalenti
- i file `.env.example` e la documentazione devono già coprire in modo esplicito sia il caso Google SMTP sia il caso `Twilio SendGrid`

**Attivita**:

1. Integrare `QuestPDF`, `QRCoder`, `MailKit`.
2. Implementare `BigliettoService` per generazione `Biglietto` e codici univoci.
3. Implementare `PdfService` per PDF multipagina.
4. Implementare `EmailService` per invio conferma con allegato PDF.
5. Implementare download PDF ordine.
6. Implementare `ValidazioneBigliettoService`.
7. Implementare endpoint:
   - `GET /admin/tickets/validate/{code}`
   - `POST /admin/tickets/validate`
8. Registrare e storicizzare `ValidatoAtUtc`, `ValidatoDaUserId`, `ValidatoCinemaId`.
9. Aggiungere test per:
   - emissione singola ticket
   - PDF contenente dati richiesti
   - doppia validazione bloccata
   - validazione con cinema errato bloccata

**Verifica fase**:

- ticket emessi correttamente
- PDF scaricabile e inviabile via email
- validazione staff funzionante e auditata

**Checklist fase**:

- [x] BigliettoService implementato
- [x] PDF multipagina implementato
- [x] Email invio implementata
- [x] Endpoint download PDF disponibile
- [x] ValidazioneBigliettoService implementato
- [x] Endpoint validazione mappati
- [x] Test ticket/validazione verdi

**Esito reale della fase**:

- emissione ticket spostata in `BigliettoService` e resa idempotente per ordine/posto
- PDF multipagina scaricabile da `GET /checkout/orders/{orderId}/pdf`, con QR code e barcode grafico
- email SMTP provider-agnostic inviata come attività post-pagamento, con tracciamento `TicketEmailSentAtUtc` e `TicketEmailLastError`
- validazione backend con blocco doppia validazione e mismatch cinema operativo, audit su `ValidatoAtUtc`, `ValidatoDaUserId`, `ValidatoCinemaId`
- suite backend verificata verde con `213/213 PASS`
- smoke test SMTP reale eseguito con biglietto finto e PDF allegato verso `gennaro.malafronte@issgreppi.it`

---

### FASE 9 - Frontend `programmazione.html` v2 + modale scelta cinema

**Obiettivo**: rilasciare la nuova UX pubblica film-centric di programmazione.

**Attivita**:

1. Aggiornare `api.js` con i nuovi metodi di catalogo pubblico.
2. Rifare `programmazione.html` con tabs, search, filtro categoria e header cinema selezionato.
3. Implementare `programmazione.js` con filtri combinati e rendering card film.
4. Implementare modale selezione cinema con geolocalizzazione e fallback ordinamento.
5. Implementare sincronizzazione `localStorage <-> backend` per il cinema preferito.
6. Aggiornare navbar landing con link `I Nostri Cinema`.
7. Aggiornare `index.html` / `home.js` per i nuovi link.

**Verifica fase**:

- una sola card per film
- tabs e filtri funzionanti
- cinema selezionato persistito correttamente

**Checklist fase**:

- [x] `api.js` aggiornato
- [x] `programmazione.html` ridisegnata
- [x] `programmazione.js` implementato
- [x] Modale cinema implementato
- [x] Persistenza cinema preferito implementata
- [x] Navbar/home aggiornate

---

### FASE 9.1 - Progetto `FilmApiSeeder` e seed realistico database

**Obiettivo**: dotare il repository di uno strumento standalone per inizializzare rapidamente un ambiente locale con dati realistici, aggiornati e utili per verifiche end-to-end del dominio multisala.

**Attivita**:

1. Creare un progetto console dedicato `backend/scripts/FilmApiSeeder` separato dal runtime principale di `FilmAPI`.
2. Riutilizzare `FilmDbContext` e i model del backend per evitare script SQL fragili e seed disallineati dallo schema reale.
3. Integrare TMDB via bearer token (`TMDB_BEARER_TOKEN`) per recuperare:
   - film reali
   - registi reali
   - copertine reali
4. Generare un dataset locale credibile composto da:
   - almeno 50 film
   - circa 20 cinema distribuiti in Italia
   - sale multi-tipologia (`2D`, `3D`, `XL`, `ISENSE`)
   - piantine posti persistite in `SalaPosti`
   - show sui giorni successivi alla data corrente
5. Centralizzare la configurazione environment in `backend/.env` e `backend/.env.example`, condividendola tra API e seeder.
6. Aggiungere modalità operative sicure:
   - esecuzione standard idempotente
   - `--reset-shows`
   - `--reset-all`
   - `--force` per prevenire reset accidentali
7. Documentare lo strumento in `backend/scripts/FilmApiSeeder/README.md` e renderlo visibile nella solution.

**Verifica fase**:

- esecuzione completa del seed con dati reali da TMDB
- copertine film valorizzate
- cinema, sale e piantine posti presenti nel DB
- programmazione show rigenerabile in modo controllato
- progetto eseguibile anche da Visual Studio tramite solution

**Checklist fase**:

- [x] Progetto `FilmApiSeeder` creato in `backend/scripts/FilmApiSeeder`
- [x] Integrazione TMDB con bearer token implementata
- [x] Seed realistico film/registi/copertine implementato
- [x] Seed cinema/sale/posti/show implementato
- [x] Modalità `--reset-shows`, `--reset-all`, `--force` implementate
- [x] Config condivisa via `backend/.env` allineata
- [x] README del seeder scritto e progetto aggiunto alla solution

---

### FASE 10 - Frontend `scheda-film.html` + `my-cinemas.html`

**Obiettivo**: completare la discovery pubblica film-centric e cinema-centric.

**Attivita**:

1. Creare `scheda-film.html` e `scheda-film.js`.
2. Creare componente/helper riusabile per rail date orizzontale con frecce.
3. Implementare raggruppamento `Data -> TipoSala -> lista orari`.
4. Gestire auth-aware redirect per i bottoni show.
5. Creare `my-cinemas.html` e `my-cinemas.js`.
6. Implementare vista lista cinema e vista dettaglio cinema.
7. Aggiornare `route-guard.js` e `template-loader.js` per le nuove pagine pubbliche.

**Verifica fase**:

- scheda film completa e usabile
- my-cinemas completa e coerente con il prompt
- rail date utilizzabile su desktop/mobile

**Checklist fase**:

- [x] `scheda-film.html` creata
- [x] `scheda-film.js` implementato
- [x] Rail date riusabile implementata
- [x] `my-cinemas.html` creata
- [x] `my-cinemas.js` implementato
- [x] Route guard/template loader aggiornati

---

### FASE 11 - Frontend `acquista.html`, `pagamento.html`, `esito-acquisto.html`, `profilo.html` v2

**Obiettivo**: completare il flusso utente di acquisto e la nuova area personale.

**Attivita**:

1. Creare `acquista.html` e `acquista.js` con seat-map, polling, keep-alive e countdown.
2. Creare `pagamento.html` e `pagamento.js` con Stripe Elements e split credito/carta.
3. Creare `esito-acquisto.html` e `esito-acquisto.js`.
4. Aggiornare `profilo.html` e `profilo.js` per mostrare:
   - cinema preferito
   - saldo credito
   - movimenti credito essenziali
   - ordini
   - biglietti
   - download PDF
5. Disattivare gradualmente la UI di creazione prenotazioni legacy.
6. Aggiornare `route-guard.js`, `template-loader.js`, `api.js` per le nuove pagine protette.

**Scostamenti implementativi gestiti in collaudo**:

- la publishable key Stripe resta solo nel backend ed e esposta al frontend via endpoint runtime `GET /config/frontend`
- dopo `stripe.confirmCardPayment(...)` il frontend richiama esplicitamente il backend per finalizzare in modo sincrono l'ordine e non lasciare lo stato `Pending`
- il flusso di annullamento da `pagamento.html` cancella l'ordine pendente e rilascia i posti tramite endpoint dedicato
- i prezzi restano source of truth del backend; eventuali valori seed espressi in centesimi vengono normalizzati lato server e nel seeder, non nel frontend
- la seat-map desktop e stata rifinita con sidebar sticky, pulsanti posto piu compatti, controlli `+`/`-`/`Reset` e supporto `Ctrl + wheel` o pinch-trackpad

**Verifica fase**:

- selezione posti funzionante con timer
- pagamento completo funzionante con finalizzazione backend post-Stripe e annullamento ordine pendente
- profilo mostra ordini, ticket e credito
- configurazione Stripe frontend risolta via backend senza variabili duplicate lato web
- calcolo prezzi coerente tra show, checkout, biglietti e seed dati

**Checklist fase**:

- [x] `acquista.html`/`acquista.js` completati
- [x] `pagamento.html`/`pagamento.js` completati
- [x] `esito-acquisto.html`/`esito-acquisto.js` completati
- [x] `profilo.html`/`profilo.js` evoluti al nuovo dominio
- [x] Route guard/template loader/api aggiornati
- [x] UI prenotazioni legacy dismessa o marcata come deprecata
- [x] Configurazione Stripe frontend centralizzata sul backend via `GET /config/frontend`
- [x] Finalizzazione ordine post-Stripe e annullamento ordine pendente coperti dal flusso reale
- [x] Affinamenti UX seat-map desktop e controlli zoom completati

---

### FASE 11.1 - Migrazione da Stripe Elements a Stripe Checkout hosted

**Obiettivo**: sostituire il pagamento carta embedded con un flusso hosted Stripe Checkout, mantenendo il backend come source of truth economica e garantendo supporto solido a solo credito, solo carta e pagamento misto credito + carta.

**Decisioni già fissate e non negoziabili**:

- il flusso target ufficiale per il pagamento carta è `Stripe Checkout`, non `Stripe Elements`
- `Stripe Elements` può restare solo come fallback tecnico temporaneo dietro feature flag durante la migrazione, non come destinazione finale della fase
- il backend resta l'unica source of truth per totale ordine, quota credito, quota carta, stato ordine e stato posti
- il frontend non deve mai marcare un ordine come pagato sulla base del solo redirect browser di ritorno da Stripe
- il pagamento con solo credito sufficiente deve bypassare completamente Stripe
- il pagamento misto deve riservare la quota credito prima del redirect e consolidarla solo a pagamento carta confermato
- se checkout Stripe fallisce, viene annullato o scade, i posti devono essere rilasciati e il credito riservato deve essere restituito in modo idempotente
- la fase non è considerata completata se il flusso hosted funziona solo in happy path senza copertura di cancel, expire, webhook duplicato e ritardo webhook

**Riferimenti operativi fase**:

- `docs/tutorials/TUTORIAL_STRIPE_GATEWAY_PAGAMENTI.md`
- `docs/tutorials/TUTORIAL_STRIPE_STRATEGIA_INTEGRAZIONE_CINEBASE.md`
- `docs/tutorials/TUTORIAL_STRIPE_CLI.md`
- `docs/tutorials/TUTORIAL_STRIPE_ELEMENTS_VS_CHECKOUT_CINEBASE.md`

**Strategia approvata per la fase**:

- il pagamento con sola carta non usa più `stripe.confirmCardPayment(...)` nel browser, ma una `Checkout Session` hosted da Stripe
- il pagamento con solo credito resta interamente interno al backend: se il credito è sufficiente non viene avviata alcuna procedura verso Stripe e si passa direttamente a finalizzazione ordine, biglietti, PDF ed email
- il pagamento misto usa il backend per riservare la quota credito e crea una `Checkout Session` Stripe soltanto per il residuo carta
- il redirect di ritorno da Stripe non è considerato prova di pagamento riuscito; la conferma reale arriva dal backend tramite webhook verificato e, in seconda battuta, tramite riconciliazione esplicita della sessione
- mentre l'utente si trova sulla pagina hosted Stripe, i posti non dipendono più dal keep-alive frontend ma da un lock d'ordine temporaneo lato backend con scadenza controllata
- il backend deve implementare cleanup automatico degli ordini hosted non finalizzati entro TTL, con rilascio posti e restore credito
- la pagina `esito-acquisto.html` deve diventare pagina di riconciliazione stato, non pagina che deduce l'esito dal query string di ritorno

**Attività**:

1. Formalizzare il nuovo stato ordine per il checkout hosted.
   - introdurre obbligatoriamente uno stato esplicito `CheckoutInProgress`, evitando di riusare `Pending` con semantica ambigua
   - aggiungere a `Ordine` i campi necessari, almeno:
      - `StripeCheckoutSessionId`
      - `CheckoutExpiresAtUtc`
      - `CreditoRiservato`
      - `CheckoutCompletedAtUtc` opzionale
      - `LastPaymentError` opzionale
   - definire in modo prescrittivo le transizioni ammesse:
     - `Pending -> CheckoutInProgress`
     - `Pending -> Paid` per solo credito
     - `CheckoutInProgress -> Paid`
     - `CheckoutInProgress -> Cancelled`
     - `CheckoutInProgress -> Expired`
     - `CheckoutInProgress -> Failed`
2. Evolvere il backend per creare una `Checkout Session` Stripe hosted.
   - aggiungere endpoint autenticato tipo `POST /checkout/orders/{orderId}/stripe-checkout-session`
   - calcolare lato backend il residuo carta reale a partire da totale ordine e credito richiesto
   - se la quota carta è zero, non creare alcuna sessione Stripe e finalizzare direttamente l'ordine
   - se la quota carta è maggiore di zero, creare la sessione con `mode=payment`, `success_url`, `cancel_url`, metadata ordine obbligatori (`orderId`, `orderCode`, `userId`, `showId`), idempotenza coerente e importo rigorosamente allineato al residuo backend
3. Implementare la riserva credito per il pagamento misto.
   - verificare il saldo nel momento di avvio checkout hosted
   - riservare la quota credito senza considerarla subito definitivamente spesa
   - confermare l'addebito del credito solo quando il backend finalizza l'ordine come pagato
   - ripristinare la quota credito riservata se la sessione Stripe viene annullata, scade o fallisce
4. Rendere il webhook Stripe la source of truth del pagamento carta hosted.
   - gestire almeno `checkout.session.completed`, `checkout.session.expired`, `payment_intent.payment_failed` ed eventuali eventi duplicati in modo replay-safe
   - verificare sempre firma webhook, importo, valuta, `metadata.orderId` e ownership logica dell'ordine
   - finalizzare l'ordine una sola volta in maniera idempotente
5. Introdurre una riconciliazione backend esplicita del ritorno da Stripe.
   - aggiungere endpoint tipo `GET /checkout/orders/{orderId}/checkout-status`
   - al ritorno su `esito-acquisto.html`, il frontend mostra stato di attesa e interroga il backend finché l'ordine non risulta `Paid`, `Cancelled`, `Expired` o `Failed`
   - aggiungere un endpoint di riconciliazione manuale della sessione Stripe da usare quando il webhook non è ancora arrivato o è in ritardo
   - il ritorno su `cancel_url` non deve annullare automaticamente l'ordine; il backend deve decidere se mantenerlo in `CheckoutInProgress`, marcarlo `Cancelled` o lasciarlo scadere secondo stato reale della sessione
6. Adeguare la logica di lock posti all'uscita temporanea dal sito.
   - convertire il semplice hold frontend-driven in un lock d'ordine con scadenza backend `CheckoutExpiresAtUtc`
   - evitare che i posti tornino disponibili mentre l'utente sta completando il pagamento hosted
   - estendere cleanup automatico per rilasciare posti e credito riservato sugli ordini scaduti o annullati
7. Aggiornare `pagamento.html` e `pagamento.js` per il nuovo comportamento.
   - mantenere la scelta `solo credito` / `solo carta` / `misto`
   - rimuovere la raccolta dati carta embedded quando il flusso Checkout hosted sarà attivo
   - per la quota carta, chiamare il backend per creare la sessione e fare redirect a Stripe
   - per il solo credito, finalizzare direttamente via backend e portare l'utente a `esito-acquisto.html`
   - per il pagamento misto, mostrare chiaramente quota credito riservata e quota carta da pagare su Stripe
8. Aggiornare `esito-acquisto.html` e `esito-acquisto.js`.
   - supportare il ritorno da `success_url` e `cancel_url`
   - mostrare messaggi distinti per `pagamento in verifica`, `pagamento completato`, `pagamento annullato`, `ordine scaduto`
   - guidare l'utente al retry solo quando il backend lo consente senza perdere coerenza sui posti
9. Aggiornare i servizi backend coinvolti.
   - `PagamentoService` per orchestrazione checkout hosted, finalizzazione e restore credito
   - `CheckoutService` per lock ordine, scadenza e stato di riconciliazione
   - `SeatHoldService` per l'interazione fra hold iniziale e lock d'ordine
   - eventuale gateway Stripe dedicato alla `Checkout Session` separato dalla logica `PaymentIntent` embedded attuale
10. Definire la strategia di migrazione dal flusso attuale al nuovo flusso.
    - mantenere temporaneamente compatibilità con Stripe Elements dietro feature flag o configurazione esplicita denominata in modo chiaro, per esempio `STRIPE_PAYMENT_FLOW`
    - permettere rollout graduale e rollback semplice in caso di problemi
    - aggiornare documentazione env, guida operativa locale e tutorial Stripe già presenti
11. Aggiungere test automatici completi.
    - solo credito con saldo sufficiente e nessuna chiamata a Stripe
    - solo credito con saldo insufficiente
    - solo carta con sessione creata correttamente
    - misto con credito riservato e quota carta residua corretta
    - webhook `checkout.session.completed` che finalizza ordine, vende posti ed emette biglietti
    - webhook duplicato che non duplica addebiti o biglietti
    - cancel/expire che rilasciano posti e restituiscono credito riservato
    - ritorno frontend su `success_url` prima dell'arrivo del webhook con polling di riconciliazione

**Vincoli implementativi obbligatori**:

- nessun totale monetario mostrato o usato dal frontend può essere considerato definitivo senza conferma backend
- nessun addebito credito definitivo deve avvenire prima della conferma reale del pagamento Stripe nel caso misto
- nessun posto in lock d'ordine deve essere rilasciato prima della scadenza hosted o del cancel esplicito, salvo errore applicativo che imponga rollback immediato
- ogni handler webhook deve essere replay-safe e idempotente rispetto a ordine, biglietti, movimenti credito e stato posti
- ogni endpoint di checkout hosted deve verificare ownership dell'ordine e coerenza con l'utente autenticato
- ogni `success_url` o `cancel_url` deve puntare a path interni controllati da CineBase
- ogni `cancel_url` deve essere trattato come semplice ritorno applicativo e non come prova sufficiente di annullamento del pagamento
- la fase deve riutilizzare l'infrastruttura Stripe esistente dove sensato, evitando duplicazioni non necessarie tra gateway hosted e gateway `PaymentIntent`

**Criteri di accettazione vincolanti**:

- non esiste più alcun inserimento carta dentro `pagamento.html` quando la feature flag hosted è attiva
- il caso solo credito sufficiente completa l'ordine senza creare `PaymentIntent` o `Checkout Session`
- il caso misto produce una `Checkout Session` con importo pari esattamente al residuo carta ricalcolato dal backend
- il ritorno da Stripe con webhook in ritardo non lascia l'utente in stato incoerente né genera biglietti duplicati
- la scadenza checkout hosted libera posti e credito riservato senza intervento manuale
- il cancel esplicito lato utente dopo ritorno da Stripe annulla l'ordine solo se l'ordine non è già stato pagato o finalizzato
- l'intera suite di test nuova o aggiornata deve risultare verde prima di dichiarare la fase completata

**Verifica fase**:

- l'utente che paga con sola carta viene reindirizzato a una pagina Stripe hosted e torna su CineBase con esito coerente
- l'utente che paga con solo credito non passa da Stripe e riceve direttamente ordine pagato, biglietti ed email se il saldo è sufficiente
- il pagamento misto riserva il credito, addebita su Stripe soltanto il residuo e finalizza l'ordine una sola volta
- i posti restano bloccati durante il checkout hosted e vengono rilasciati correttamente se la sessione fallisce, viene annullata o scade
- il webhook Stripe è la fonte principale di conferma e il frontend non marca mai da solo un ordine come pagato
- il ritorno da Stripe verso `esito-acquisto.html` è robusto anche in presenza di ritardo del webhook

**Checklist fase**:

- [x] Modello `Ordine` esteso per sessione hosted, scadenza checkout e credito riservato
- [x] Endpoint creazione `Checkout Session` implementato
- [x] Endpoint stato checkout / riconciliazione implementato
- [x] Pagamento solo credito senza Stripe implementato
- [x] Pagamento misto con riserva e rilascio credito implementato
- [x] Webhook Stripe Checkout replay-safe implementato
- [x] Cleanup ordini hosted scaduti con rilascio posti e credito implementato
- [x] `pagamento.html` migrata da Elements a redirect hosted
- [x] `esito-acquisto.html` aggiornata con polling stato backend
- [x] Endpoint riconciliazione sessione Stripe implementato
- [x] Feature flag o strategia di rollout graduale documentata
- [x] Test integrazione checkout hosted verdi
- [x] Tutorial comparativo Elements vs Checkout scritto e referenziato

**Esito reale della fase**:

- ordine transita a `CheckoutInProgress` alla creazione della sessione Stripe hosted
- webhook `checkout.session.completed` finalizza l'ordine, vende posti ed emette biglietti in modo idempotente
- webhook duplicati non generano biglietti o addebiti duplicati
- webhook `checkout.session.expired` rilascia posti e marca ordine come `Expired`
- nel pagamento misto il credito viene riservato all'avvio del checkout hosted, finalizzato senza doppio addebito al successo e rilasciato su expire/cancel/failure
- riconciliazione manuale tramite `POST /checkout/orders/{orderId}/reconcile-checkout-session` aggiorna stato da Stripe quando il webhook e in ritardo
- `esito-acquisto.html` mostra polling per `CheckoutInProgress` e distingue annullato/scaduto
- frontend `pagamento.html` non usa piu Stripe Elements; carta e misto creano sessione hosted, solo credito finalizza direttamente
- cleanup periodico backend gestisce anche ordini hosted scaduti senza ritorno browser
- suite backend verificata verde con `231/231 PASS`

---

### FASE 12 - Frontend admin: sale, show, ricarica credito, validazione ticket

**Obiettivo**: fornire agli operatori strumenti completi per lavorare sul nuovo dominio.

**Attivita**:

1. Creare `sale.html` e `sale.js` con editor visuale piantina.
2. Aggiornare `proiezioni.html` e `proiezioni.js` in ottica show multi-sala.
3. Creare `ricarica-credito.html` e `ricarica-credito.js`.
4. Creare `validazione-biglietti.html` e `validazione-biglietti.js` con supporto scanner QR/barcode.
5. Aggiornare `navbar-admin.html`, `dashboard.html` e `route-guard.js`.
6. Integrare una libreria scanner frontend o usare `BarcodeDetector` con fallback.

**Verifica fase**:

- gestione sale/show usabile senza workaround
- ricarica credito operativa
- validazione ticket da mobile/tablet operativa

**Checklist fase**:

- [ ] `sale.html`/`sale.js` completati
- [ ] `proiezioni.html`/`proiezioni.js` rifatti per show
- [ ] `ricarica-credito.html`/`ricarica-credito.js` completati
- [ ] `validazione-biglietti.html`/`validazione-biglietti.js` completati
- [ ] Navbar admin/dashboard/route guard aggiornati
- [ ] Scanner QR/barcode funzionante almeno in scenario browser supportato

---

### FASE 13 - Test finali, cleanup legacy, hardening e documentazione

**Obiettivo**: chiudere iterazione con base stabile, documentata e pronta per uso continuativo.

**Attivita**:

1. Aggiornare/estendere tutte le suite integration test.
2. Aggiungere test di concorrenza sui posti.
3. Verificare idempotenza pagamento e replay webhook.
4. Eseguire smoke test manuali per tutti i ruoli.
5. Valutare cleanup legacy:
   - se nessuna dipendenza residua, rimuovere `Proiezione` e `Prenotazione`
   - se resta rischio, lasciarle deprecate e documentare il debito tecnico residuo
6. Aggiornare `status.md` e `changelog.md`.
7. Allineare il piano di lavoro a eventuali scostamenti implementativi reali.

**Verifica fase**:

- suite backend verde
- flussi principali verificati end-to-end
- documentazione aggiornata
- situazione legacy chiarita e documentata

**Checklist fase**:

- [ ] Suite integration test estesa e verde
- [ ] Test concorrenza posti verde
- [ ] Verifica webhook/idempotenza completata
- [ ] Smoke test manuali per ruoli completati
- [ ] Cleanup legacy eseguito o deprecazione documentata
- [ ] `status.md` aggiornato
- [ ] `changelog.md` aggiornato
- [ ] Piano finale riallineato se necessario

---

## 8) Dipendenze e Configurazioni

## 8.1 NuGet packages backend da aggiungere

| Package | Scopo |
| --- | --- |
| `Stripe.net` | pagamento carta |
| `QuestPDF` | generazione PDF ticket |
| `QRCoder` | generazione QR code |
| `MailKit` | invio email SMTP |

## 8.2 Librerie frontend suggerite

| Libreria | Scopo |
| --- | --- |
| `Stripe.js` | carta di credito con Elements |
| `html5-qrcode` oppure `BarcodeDetector` con fallback | scansione QR/barcode su `validazione-biglietti.html` |

## 8.3 Variabili environment da aggiungere o estendere

```env
# Stripe
STRIPE_SECRET_API_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...

# La publishable key Stripe resta solo nel backend e viene esposta al frontend
# tramite configurazione runtime (`GET /config/frontend`). Nessuna chiave Stripe
# va duplicata in file `.env` del frontend.

# SMTP provider-agnostic
# Caso baseline Fase 8: Google SMTP
# SMTP_HOST=smtp.gmail.com
# SMTP_PORT=587
# SMTP_USER=cinebase.demo@gmail.com
# SMTP_PASSWORD=<google_app_password>
# SMTP_FROM_NAME=CineBase
# SMTP_FROM_EMAIL=cinebase.demo@gmail.com

# Caso alternativo previsto: Twilio SendGrid SMTP relay
# SMTP_HOST=smtp.sendgrid.net
# SMTP_PORT=587
# SMTP_USER=apikey
# SMTP_PASSWORD=<twilio_sendgrid_api_key>
# SMTP_FROM_NAME=CineBase
# SMTP_FROM_EMAIL=tickets@example.com

SMTP_HOST=<smtp_host>
SMTP_PORT=587
SMTP_USER=<smtp_user>
SMTP_PASSWORD=<smtp_password>
SMTP_FROM_NAME=CineBase
SMTP_FROM_EMAIL=noreply@cinebase.it

# Ticketing / checkout
FRONTEND_BASE_URL=http://localhost:5001
DEFAULT_TICKET_PRICE=8.50
HOLD_TTL_MINUTES=10
MAX_SEATS_PER_ORDER=10
TICKET_VALIDATION_BASE_URL=http://localhost:5001/validazione-biglietti.html
```

---

## 9) Nuovi File Previsti (sintesi)

## 9.1 Backend (`backend/FilmAPI/`)

**Model**

- `Model/TipoSala.cs`
- `Model/ShowPostoState.cs`
- `Model/OrdineState.cs`
- `Model/BigliettoState.cs`
- `Model/MovimentoCreditoTipo.cs`
- `Model/Sala.cs`
- `Model/SalaPosto.cs`
- `Model/Show.cs`
- `Model/ShowPostoStato.cs`
- `Model/Ordine.cs`
- `Model/Biglietto.cs`
- `Model/MovimentoCredito.cs`

**DTO**

- `DTO/SalaDTO.cs`
- `DTO/ShowDTO.cs`
- `DTO/ProgrammazioneDTO.cs`
- `DTO/FilmSchedaDTO.cs`
- `DTO/CheckoutDTO.cs`
- `DTO/OrdineDTO.cs`
- `DTO/BigliettoDTO.cs`
- `DTO/CreditoDTO.cs`
- `DTO/ProfiloDTO.cs`
- aggiornamento `DTO/FilmDTO.cs`
- aggiornamento `DTO/CinemaDTO.cs`

**Services**

- `Services/ISalaService.cs` + `Services/SalaService.cs`
- `Services/IShowService.cs` + `Services/ShowService.cs`
- `Services/IProgrammazioneService.cs` + `Services/ProgrammazioneService.cs`
- `Services/ISeatHoldService.cs` + `Services/SeatHoldService.cs`
- `Services/ICheckoutService.cs` + `Services/CheckoutService.cs`
- `Services/IPagamentoService.cs` + `Services/PagamentoService.cs`
- `Services/ICreditoService.cs` + `Services/CreditoService.cs`
- `Services/IBigliettoService.cs` + `Services/BigliettoService.cs`
- `Services/IPdfService.cs` + `Services/PdfService.cs`
- `Services/IEmailService.cs` + `Services/EmailService.cs`
- `Services/IValidazioneBigliettoService.cs` + `Services/ValidazioneBigliettoService.cs`

**Endpoints**

- `Endpoints/SaleEndpoints.cs`
- `Endpoints/ShowsEndpoints.cs`
- `Endpoints/ProgrammazioneEndpoints.cs`
- `Endpoints/CheckoutEndpoints.cs`
- `Endpoints/PagamentoEndpoints.cs`
- `Endpoints/CreditoEndpoints.cs`
- `Endpoints/ValidazioneBigliettiEndpoints.cs`
- aggiornamento `Endpoints/ProfiloEndpoints.cs`
- aggiornamento temporaneo `Endpoints/ProiezioniEndpoints.cs`

**Background**

- `Background/SeatHoldCleanupService.cs`

## 9.2 Frontend (`frontend/CineBase.Web/wwwroot/`)

**Nuove pagine**

- `scheda-film.html`
- `my-cinemas.html`
- `acquista.html`
- `pagamento.html`
- `esito-acquisto.html`
- `sale.html`
- `ricarica-credito.html`
- `validazione-biglietti.html`

**Nuovi JS**

- `js/pages/scheda-film.js`
- `js/pages/my-cinemas.js`
- `js/pages/acquista.js`
- `js/pages/pagamento.js`
- `js/pages/esito-acquisto.js`
- `js/pages/sale.js`
- `js/pages/ricarica-credito.js`
- `js/pages/validazione-biglietti.js`
- eventuale helper riusabile per rail date e/o scanner QR

**File aggiornati**

- `programmazione.html`
- `js/pages/programmazione.js`
- `profilo.html`
- `js/pages/profilo.js`
- `proiezioni.html`
- `js/pages/proiezioni.js`
- `films.html`
- `js/pages/films.js`
- `cinemas.html`
- `js/pages/cinemas.js`
- `dashboard.html`
- `js/api.js`
- `js/route-guard.js`
- `js/template-loader.js`
- `components/navbar-landing.html`
- `components/navbar-admin.html`

## 9.3 Test (`tests/backend/`)

- `Integration/SaleIntegrationTests.cs`
- `Integration/ShowIntegrationTests.cs`
- `Integration/ProgrammazioneIntegrationTests.cs`
- `Integration/CinemaPreferitoIntegrationTests.cs`
- `Integration/SeatHoldIntegrationTests.cs`
- `Integration/CheckoutIntegrationTests.cs`
- `Integration/CreditoIntegrationTests.cs`
- `Integration/TicketIntegrationTests.cs`
- `Integration/ValidazioneTicketIntegrationTests.cs`

---

## 10) Piano Test (macro)

## 10.1 Backend - nuove suite suggerite

- `SaleIntegrationTests`
  - CRUD sale
  - vincolo progressivo univoco nel cinema
  - salvataggio piantina
  - blocco delete con show futuri
- `ShowIntegrationTests`
  - CRUD show
  - validazione anti-overlap completa
  - query per cinema/data
  - query per film/cinema
  - compatibilità endpoint `proiezioni`
- `ProgrammazioneIntegrationTests`
  - featured/upcoming/all
  - search + categoria
  - disponibilità nel cinema selezionato
  - film scheda raggruppati per data/tipo sala
- `CinemaPreferitoIntegrationTests`
  - GET/PUT profilo cinema preferito
  - ownership e validazione cinema esistente
- `SeatHoldIntegrationTests`
  - hold singolo e multiplo
  - scadenza hold
  - refresh hold
  - conflitto concorrente su stesso posto
- `CheckoutIntegrationTests`
  - ordine pendente da hold valido
  - ordine rifiutato se hold scaduto
  - max 10 posti
- `CreditoIntegrationTests`
  - ricarica manuale
  - audit operatore
  - saldo utente
  - pagamento con credito
- `TicketIntegrationTests`
  - emissione ticket
  - download PDF
  - codici univoci
- `ValidazioneTicketIntegrationTests`
  - validazione corretta
  - doppia validazione bloccata
  - cinema operativo errato bloccato

## 10.2 Frontend - smoke test / test manuali guidati

- `programmazione.html`
  - tabs funzionanti
  - filtro categoria + search
  - selezione cinema e persistenza
- `scheda-film.html`
  - dettagli film completi
  - rail date
  - bottoni orario auth-aware
- `my-cinemas.html`
  - lista cinema
  - dettaglio programmazione giorno
- `acquista.html`
  - seat map responsive
  - countdown
  - max 10 posti
- `pagamento.html`
  - carta
  - credito
  - misto
- `validazione-biglietti.html`
  - input manuale
  - query string `?codice=`
  - scanner su mobile/tablet

## 10.3 Scenari end-to-end obbligatori

1. anonimo -> sceglie cinema -> entra in scheda film -> click show -> login -> acquista -> paga con carta -> riceve ticket
2. utente con credito -> seleziona posti -> paga tutto con credito
3. utente con credito parziale -> paga credito + carta
4. PowerUser -> ricarica credito utente per email
5. PowerUser -> valida ticket da QR su smartphone/tablet
6. due utenti concorrenti -> tentano lo stesso posto -> uno solo ottiene il hold/pagamento

---

## 11) Criteri di Accettazione Iterazione 4

L'iterazione e completata quando tutte le condizioni seguenti sono vere:

1. ogni cinema può avere N sale con numero progressivo univoco, tipologia e piantina posti
2. gli show sono modellati come `film + cinema + sala + data/ora`
3. il backend impedisce sovrapposizioni temporali nella stessa sala
4. `programmazione.html` mostra una sola card per film, non per show
5. tabs `In evidenza`, `In uscita`, `Tutti i film` funzionano con logica coerente
6. filtro categoria e search titolo funzionano insieme
7. il cinema selezionato può essere scelto da modale e ordinato per prossimità quando possibile
8. il cinema preferito e sincronizzato tra `localStorage` e backend profilo
9. ogni card film indica disponibilità nel cinema selezionato
10. `scheda-film.html` mostra descrizione lunga, cast e show raggruppati per data/tipo sala
11. `my-cinemas.html` mostra sia la lista cinema sia la programmazione giornaliera del cinema selezionato
12. `acquista.html` visualizza posti disponibili, occupati, hold altrui e posti dell'utente corrente
13. non è possibile acquistare più di 10 posti in un solo ordine
14. il sistema impedisce a due utenti di acquistare lo stesso posto nello stesso show
15. `pagamento.html` supporta carta, credito e pagamento misto
16. PowerUser/Admin possono ricaricare credito utente con audit operatore
17. un ordine pagato genera ticket univoci, PDF e email
18. ogni ticket include barcode, QR code, codice ticket e dettagli show/posto/cinema/prezzo
19. PowerUser/Admin possono validare ticket manualmente o via QR da pagina protetta
20. la validazione fallisce se il cinema operativo non coincide con quello del ticket
21. `profilo.html` mostra cinema preferito, credito, ordini e biglietti
22. tutta la suite backend e verde dopo l'estensione dei test
23. `status.md` e `changelog.md` sono aggiornati
24. il debito tecnico legacy (`Proiezione`/`Prenotazione`) e stato eliminato oppure documentato esplicitamente

---

## 12) Prompt Guida (fase-by-fase)

Regola comune per tutti i prompt fase:

- implementare solo la fase richiesta
- al termine aggiornare la tabella `Stato Avanzamento Fasi`
- spuntare la checklist della fase con `[x]`
- se una parte resta incompleta, segnalarla nelle note con impatto e workaround

### Prompt Fase 1

```text
Implementa la FASE 1 del PianoDiLavoro Iterazione 4 (`docs/project/dev_iteration/4/PianoDiLavoro.md`).
Crea il nuovo modello dati v2: enum e entità `Sala`, `SalaPosto`, `Show`, `ShowPostoStato`, `Ordine`, `Biglietto`, `MovimentoCredito`, estendi `Film`, `Cinema`, `User`, aggiorna `FilmDbContext` con nuovi DbSet, relazioni e vincoli.
NON rimuovere ancora `Proiezione` e `Prenotazione`.
Aggiorna i placeholder di configurazione e verifica compilazione backend.
```

### Prompt Fase 2

```text
Implementa la FASE 2 del PianoDiLavoro Iterazione 4.
Crea e applica la migration `AddMultisalaTicketing`, aggiorna il seed dev, crea sale default per i cinema esistenti e migra le `Proiezione` legacy a `Show`, gestendo eventuali conflitti con sale auto-migrate aggiuntive.
Non convertire automaticamente `Prenotazione` in `Biglietto`.
Verifica DB e test baseline.
```

### Prompt Fase 3

```text
Implementa la FASE 3 del PianoDiLavoro Iterazione 4.
Estendi i DTO film con `DescrizioneLunga`, `CastText`, `DataRilascio`, crea gli endpoint pubblici per `programmazione/films`, `programmazione/cinemas`, `films/{id}/scheda`, `my-cinemas` e `my-cinemas/{cinemaId}/schedule`, e implementa `GET/PUT /profilo/cinema-preferito`.
Aggiungi test integrazione per tabs featured/upcoming/all, search, categoria, cinema preferito e ordinamento distanza.
```

### Prompt Fase 4

```text
Implementa la FASE 4 del PianoDiLavoro Iterazione 4.
Crea DTO, service ed endpoint per CRUD `Sala` e gestione completa della piantina tramite `SalaPosto`.
Implementa validazioni di numero sala univoco, blocco delete con show futuri o ticket emessi, e aggiungi test integrazione.
```

### Prompt Fase 5

```text
Implementa la FASE 5 del PianoDiLavoro Iterazione 4.
Crea DTO, service ed endpoint `Show` usando la nomenclatura approvata (`ShowsEndpoints`, route `/shows`), con validazione anti-overlap completa per la stessa sala.
Prepara un compat layer per gli endpoint `proiezioni` esistenti, mappandoli internamente al nuovo dominio `Show` dove possibile.
Aggiungi test integrazione per CRUD show, overlap e compatibilità legacy.
```

### Prompt Fase 6

```text
Implementa la FASE 6 del PianoDiLavoro Iterazione 4.
Implementa seat map, hold posti con TTL su `ShowPostoStato`, keep-alive, release, cleanup background e creazione ordine pendente da hold valido.
Verifica il limite massimo di 10 posti e aggiungi test di concorrenza su richieste parallele allo stesso posto/show.
```

### Prompt Fase 7

```text
Implementa la FASE 7 del PianoDiLavoro Iterazione 4.
Integra Stripe, pagamento credito piattaforma e pagamento misto, finalizzazione ordine idempotente, aggiornamento `MovimentoCredito`, endpoint di ricarica credito per PowerUser/Admin e liste ordini/ticket per il profilo utente.
Aggiungi test per carta, credito, misto, saldo insufficiente e webhook replay-safe.
```

### Prompt Fase 8

```text
Implementa la FASE 8 del PianoDiLavoro Iterazione 4.
Implementa emissione ticket, PDF multipagina, invio email, download PDF ordine e validazione ticket con controllo cinema operativo.
Aggiungi test per emissione ticket, PDF, doppia validazione e mismatch cinema.
```

### Prompt Fase 9

```text
Implementa la FASE 9 del PianoDiLavoro Iterazione 4.
Riprogetta `programmazione.html` come pagina film-centric con tabs, search, filtro categoria, modale selezione cinema, persistenza e sincronizzazione del cinema preferito, e aggiornamento navbar/home.
```

### Prompt Fase 10

```text
Implementa la FASE 10 del PianoDiLavoro Iterazione 4.
Crea `scheda-film.html` e `my-cinemas.html`, con rail date orizzontale riusabile, show raggruppati per tipologia sala, bottoni orario auth-aware e aggiornamento di route guard/template loader.
```

### Prompt Fase 11

```text
Implementa la FASE 11 del PianoDiLavoro Iterazione 4.
Crea `acquista.html`, `pagamento.html`, `esito-acquisto.html` e aggiorna `profilo.html` al nuovo dominio ordini/biglietti/credito/cinema preferito.
Implementa seat-map interattiva con countdown e keep-alive, Stripe Elements, pagamento misto e riepilogo finale ordine/ticket.
```

### Prompt Fase 12

```text
Implementa la FASE 12 del PianoDiLavoro Iterazione 4.
Crea `sale.html` con editor visuale piantina, aggiorna `proiezioni.html` in workspace show multi-sala, crea `ricarica-credito.html` e `validazione-biglietti.html` con supporto scanner QR/barcode, e aggiorna navbar admin/dashboard/route guard.
```

### Prompt Fase 13

```text
Implementa la FASE 13 del PianoDiLavoro Iterazione 4.
Estendi tutte le suite di test, esegui smoke test ed end-to-end per anonimo/user/poweruser/admin, verifica hardening sicurezza (redirect, rate limit, idempotenza, webhook), e aggiorna `status.md` e `changelog.md`.
Se il legacy `Proiezione`/`Prenotazione` non è più usato, rimuovilo; altrimenti lascialo deprecato e documenta chiaramente il debito tecnico residuo.
```
