# Tutorial Frontend CineBase: `index.html` e `programmazione.html`

**Autore:** OpenCode  
**Data:** Aprile 2026  
**Progetto di riferimento:** CineBase  
**Ambito:** frontend pubblico, caricamento componenti, home page, programmazione film-centric  

---

## 1. Obiettivo del tutorial

Questo tutorial descrive in modo dettagliato il funzionamento di due pagine centrali del frontend pubblico di CineBase:

- `frontend/CineBase.Web/wwwroot/index.html`
- `frontend/CineBase.Web/wwwroot/programmazione.html`

L’obiettivo è mostrare come le diverse parti del codice collaborano tra loro per costruire l’esperienza utente. In particolare, il documento spiega:

- come viene composta la home page pubblica;
- come vengono mostrati i film in evidenza nella `index.html`;
- come la pagina `programmazione.html` gestisce selezione cinema, geolocalizzazione, filtri, caroselli, paginazione e sincronizzazione dello stato;
- come il frontend interagisce con il backend attraverso `api.js`.

Il linguaggio adottato è volutamente formale e preciso, ma pensato per restare accessibile a studenti che stanno studiando architetture web basate su HTML, JavaScript vanilla e API REST.

---

## 2. Struttura generale del frontend pubblico

Le due pagine analizzate seguono una struttura comune:

1. il file HTML definisce lo scheletro della pagina;
2. `template-loader.js` carica dinamicamente navbar e footer;
3. `navbar.js` inizializza i comportamenti generali della barra di navigazione;
4. `api.js` centralizza tutte le chiamate HTTP verso il backend;
5. uno script di pagina dedicato implementa la logica specifica:
   - `js/pages/home.js` per `index.html`
   - `js/pages/programmazione.js` per `programmazione.html`

Il risultato è una separazione netta tra:

- struttura HTML;
- componenti condivisi;
- logica di pagina;
- comunicazione con il backend.

Questa separazione rende il progetto più leggibile e facilita manutenzione, debugging e refactoring.

---

## 3. Il ruolo dei file condivisi

Prima di entrare nelle singole pagine, è utile chiarire il ruolo dei moduli condivisi.

### 3.1 `template-loader.js`

Il file `frontend/CineBase.Web/wwwroot/js/template-loader.js` gestisce il caricamento dinamico di navbar e footer.

Il flusso è il seguente:

1. alla `DOMContentLoaded`, viene eseguita `loadLayoutComponents()`;
2. il loader decide se la pagina appartiene al gruppo pubblico oppure all’area amministrativa;
3. in base al path, sceglie il componente corretto:
   - `navbar-landing.html` / `footer-landing.html` per le pagine pubbliche;
   - `navbar-admin.html` / `footer-admin.html` per le pagine amministrative;
4. i componenti vengono recuperati via `fetch()`;
5. il contenuto HTML viene inserito nel DOM;
6. eventuali script inline presenti nel componente vengono rieseguiti;
7. infine viene emesso l’evento `components:loaded`.

Il file usa anche una cache in memoria (`templateCache`) per evitare fetch ripetuti dello stesso componente durante il ciclo di vita della pagina.

### 3.2 `navbar-landing.html`

La navbar pubblica non si limita a mostrare link statici. Essa implementa anche:

- stato di autenticazione utente;
- visualizzazione del cinema selezionato;
- link role-aware per l’area admin;
- menu mobile;
- sincronizzazione con eventi applicativi.

In particolare, la funzione `updateCinemaDisplay(cinemaName)` aggiorna il badge del cinema nella navbar. Questa funzione viene esposta come `window.updateNavbarCinema` e usata dalle pagine pubbliche quando cambia il cinema selezionato.

### 3.3 `navbar.js`

Il file `frontend/CineBase.Web/wwwroot/js/navbar.js` si occupa di:

- evidenziare il link attivo nella navbar;
- aprire e chiudere il menu mobile;
- inizializzare la navbar quando i componenti sono pronti.

Questo comportamento viene attivato sia su `components:loaded` sia, come fallback, su `DOMContentLoaded` se il `nav` è già presente.

### 3.4 `api.js`

Il file `frontend/CineBase.Web/wwwroot/js/api.js` rappresenta il client HTTP del frontend.

Per quanto riguarda le pagine pubbliche trattate in questo tutorial, i metodi più importanti sono:

- `API.getFilms(...)`
- `API.getProiezioni()`
- `API.getProgrammazioneFilms(...)`
- `API.getProgrammazioneCinemas(...)`
- `API.getCinemaPreferito()`
- `API.setCinemaPreferito(...)`

`api.js` incapsula inoltre:

- gestione dell’`Authorization` header;
- refresh token automatico su `401`;
- parsing uniforme delle risposte;
- costruzione dei query parameter per filtri e paginazione.

---

## 4. Funzionamento di `index.html`

### 4.1 Struttura della pagina

Il file `frontend/CineBase.Web/wwwroot/index.html` definisce due sezioni principali:

- una **hero section** introduttiva;
- una sezione **“In Evidenza Questa Settimana”**.

La home pubblica non è pensata come pagina operativa di ricerca. La sua funzione è soprattutto di discovery e presentazione.

Per questo motivo:

- la hero introduce il progetto CineBase;
- la sezione film mostra una selezione editoriale/algoritmica dei titoli più rilevanti;
- il bottone principale rimanda a `programmazione.html`, che è la pagina operativa vera e propria.

### 4.2 Script associato: `home.js`

Il comportamento della home è implementato in `frontend/CineBase.Web/wwwroot/js/pages/home.js`.

Alla `DOMContentLoaded`, lo script esegue due attività:

1. legge l’eventuale query parameter `forbidden=true` per mostrare un toast di accesso negato;
2. richiama `loadFeaturedFilms()`.

### 4.3 Caricamento dei film in evidenza

La funzione `loadFeaturedFilms()` esegue una `Promise.all()` su due endpoint:

- `API.getFilms({ page: 1, pageSize: 100 })`
- `API.getProiezioni()`

L’idea è semplice ma didatticamente molto importante:

- l’elenco dei film fornisce i metadati del catalogo;
- l’elenco delle proiezioni consente di capire quali film sono realmente rilevanti nella settimana corrente.

Lo script normalizza entrambe le risposte perché il backend, in diverse parti del progetto, può restituire dati sotto forma di:

- array semplice;
- proprietà `items`;
- proprietà `$values`.

Questa normalizzazione è un esempio concreto di adattamento del frontend a contratti leggermente differenti senza duplicare la logica di parsing in più punti.

### 4.4 Algoritmo di selezione dei film in evidenza

La funzione `buildFeaturedSelection(films, proiezioni)` implementa la logica di ranking.

Il processo è il seguente:

1. calcola l’intervallo dei prossimi 7 giorni;
2. filtra le proiezioni future comprese in quell’intervallo;
3. conta quante proiezioni ha ogni film;
4. costruisce una lista di film con un punteggio (`score`);
5. ordina i film:
   - prima per numero di proiezioni decrescente;
   - poi per data più recente;
6. seleziona i primi 5 elementi.

Questo schema è interessante perché separa il concetto di **catalogo** dal concetto di **rilevanza temporale**.

Un film non viene mostrato come “in evidenza” soltanto perché esiste nel database, ma perché ha una presenza significativa nella programmazione della settimana.

### 4.5 Rendering della sezione evidenza

Il rendering è organizzato su due livelli:

- `renderHeroCard(...)` per la card principale grande;
- `renderCompactCard(...)` per le card laterali compatte.

La funzione `updateFeaturedDisplay(activeIndex)` costruisce il layout combinato:

- una card hero per il film attivo;
- una colonna laterale con gli altri film.

Il codice mostra un pattern molto utile da studiare:

- la vista non è statica;
- la sezione si aggiorna in base a `currentFeaturedIndex`;
- il dataset sorgente resta in memoria (`featuredEntries`), mentre il DOM viene ricostruito a ogni cambio.

### 4.6 Rotazione automatica e controllo utente

La home implementa anche una rotazione automatica dei contenuti.

Le funzioni coinvolte sono:

- `initFeaturedFilms(entries)`
- `window.setActiveFeatured(index)`

Il meccanismo è il seguente:

1. viene inizializzato un intervallo (`setInterval`) di 6 secondi;
2. a ogni intervallo cambia l’indice attivo;
3. il layout viene aggiornato;
4. se l’utente clicca una card laterale, l’indice attivo cambia immediatamente;
5. il timer viene riavviato.

Questo comportamento dimostra una tecnica classica di interazione frontend: combinare **rotazione automatica** e **controllo manuale** senza perdere coerenza di stato.

### 4.7 Immagini, fallback e robustezza

La funzione `getCoverImage(copertinaPath)` normalizza il path dell’immagine di copertina.

Essa gestisce quattro casi:

- path nullo: usa una cover di fallback;
- path locale sotto `/media/...`;
- nome file semplice da risolvere nel backend;
- URL assoluto remoto.

Questo approccio evita che il rendering della home dipenda da una sola forma di storage delle immagini.

---

## 5. Funzionamento di `programmazione.html`

Se la home è una pagina di discovery, `programmazione.html` è invece una pagina operativa complessa. Essa implementa molte responsabilità contemporaneamente.

### 5.1 Struttura della pagina

Il file `frontend/CineBase.Web/wwwroot/programmazione.html` contiene le seguenti aree principali:

1. **Cinema Header**
   - mostra il cinema selezionato;
   - consente di aprire la modale di cambio cinema.

2. **Tabs**
   - `In evidenza`
   - `In uscita`
   - `Tutti i film`

3. **Ricerca e filtro categoria**
   - campo testuale per il titolo;
   - select per la categoria.

4. **Films Section**
   - intestazione dinamica della sezione;
   - controlli del carosello;
   - area contenuti film;
   - bottone `Carica altri film`.

5. **Stati speciali**
   - empty state;
   - stato “nessun cinema selezionato”.

6. **Modale cinema**
   - ricerca interna;
   - lista ordinabile dei cinema;
   - nota relativa alla geolocalizzazione.

### 5.2 Stato applicativo di `programmazione.js`

Il file `frontend/CineBase.Web/wwwroot/js/pages/programmazione.js` usa un insieme di variabili modulo per rappresentare lo stato della pagina.

Le più importanti sono:

- `currentTab`
- `currentSearch`
- `currentCategoriaId`
- `selectedCinemaId`
- `allCategorie`
- `allCinemas`
- `userLocation`
- `currentFilms`
- `currentPage`
- `currentPagedResult`

Si tratta di un approccio di stato locale molto comune nel JavaScript vanilla: non esiste uno store centralizzato formale, ma la pagina mantiene in memoria il minimo indispensabile per poter ricalcolare la UI.

---

## 6. Cinema preferito e sincronizzazione dello stato

### 6.1 Il ruolo di `CinemaManager`

Uno degli oggetti più importanti della pagina è `CinemaManager`.

Esso incapsula la logica di persistenza del cinema preferito e fornisce tre comportamenti fondamentali:

- lettura del cinema da `localStorage`;
- sincronizzazione con il backend per utenti autenticati;
- aggiornamento del cinema attivo con notifica al resto della pagina.

### 6.2 Persistenza per utenti anonimi e autenticati

Il sistema segue una strategia ibrida:

- utente anonimo: il cinema viene salvato solo in `localStorage`;
- utente autenticato: il cinema viene letto e scritto anche tramite backend.

La funzione `syncCinemaPreferito()` fa da ponte tra le due sorgenti. Se il backend possiede un valore, esso diventa la fonte di verità. Se il backend non ha ancora un cinema salvato ma il browser sì, il valore locale viene inviato al server.

Questo meccanismo consente una UX coerente anche quando l’utente passa da stato anonimo a stato autenticato.

### 6.3 Evento `cinema:changed`

Quando il cinema cambia, `CinemaManager.setCinema(cinemaId)` emette un evento custom:

```javascript
window.dispatchEvent(new CustomEvent('cinema:changed', {
  detail: { cinemaId, cinemaName: cinema?.nome || null }
}));
```

Questo evento è importante perché disaccoppia i componenti:

- la pagina aggiorna header e film;
- la navbar aggiorna il badge del cinema;
- eventuali altre parti dell’applicazione possono reagire senza dipendere direttamente da `CinemaManager`.

---

## 7. Geolocalizzazione e ordinamento dei cinema per distanza

### 7.1 Geolocalizzazione non bloccante

La pagina `programmazione.html` richiede la geolocalizzazione, ma non blocca il rendering iniziale in attesa della risposta del browser.

La funzione `requestUserLocationInBackground()` viene eseguita in background dopo l’avvio del caricamento principale.

Questa scelta è significativa perché evita che l’interfaccia resti inutilizzabile mentre il browser:

- chiede il permesso all’utente;
- attende una risposta;
- oppure fallisce per timeout.

### 7.2 Uso delle coordinate

Se la geolocalizzazione ha successo, `loadCinemas()` richiama:

- `API.getProgrammazioneCinemas({ lat, lng })`

Il backend può quindi restituire i cinema ordinati per distanza.

Questo passaggio è importante: il frontend non calcola localmente la distanza tra utente e cinema. Il suo compito è soltanto raccogliere le coordinate del browser e inviarle al backend come query parameter.

### 7.2.1 Come viene calcolata la distanza

Nel backend, il metodo `GetCinemasAsync(double? lat, double? lng)` di `ProgrammazioneService` legge per ogni cinema:

- latitudine del cinema;
- longitudine del cinema;
- latitudine dell’utente;
- longitudine dell’utente.

Se tutti questi valori sono disponibili, il service richiama `CalculateDistanceKm(...)`.

Questa funzione applica la formula di Haversine, cioè una formula geometrica usata per stimare la distanza tra due punti sulla superficie terrestre a partire da latitudine e longitudine.

Nel codice:

- il raggio terrestre è fissato a `6371.0` chilometri;
- le differenze tra coordinate vengono convertite da gradi a radianti;
- viene calcolato il valore intermedio `a`;
- da `a` viene ricavato `c`;
- la distanza finale è `R * c`.

In forma concettuale, il flusso è questo:

```text
coordinate utente + coordinate cinema
-> conversione in radianti
-> formula di Haversine
-> distanza in chilometri
-> arrotondamento a 2 decimali
-> assegnazione a DistanzaKm
```

Il valore finale viene salvato nel DTO `CinemaCardDTO` dentro la proprietà `DistanzaKm`, con arrotondamento a due decimali. Per questo motivo, nella modale il valore mostrato all’utente è già pronto per la presentazione e non richiede ulteriori trasformazioni nel frontend.

Se la geolocalizzazione non è disponibile, la lista resta comunque funzionante, ma si basa sull’ordinamento di fallback previsto dal backend.

### 7.3 Modale cinema e ordinamento

L’ordinamento viene deciso direttamente nel backend secondo due regole molto chiare:

1. se `lat` e `lng` sono presenti:
   - i cinema vengono ordinati prima per `DistanzaKm` crescente;
   - in caso di pari distanza, viene usato `Nome` come tie-breaker alfabetico;
   - i cinema senza coordinate valide finiscono in fondo, perché il codice assegna loro un valore equivalente a `double.MaxValue` durante l’ordinamento;
2. se `lat` e `lng` non sono presenti:
   - i cinema vengono ordinati semplicemente per nome.

Questa scelta ha un vantaggio didattico importante: la logica di ordinamento resta deterministica anche quando i dati geografici sono incompleti.

La modale cinema usa la lista `allCinemas`, che viene popolata una volta e poi filtrata lato client tramite il campo di ricerca interno.

Ogni card cinema mostra:

- nome;
- città e indirizzo;
- distanza, se disponibile;
- tipologie di sala presenti.

Il fatto che il calcolo della distanza e l’ordinamento per prossimità siano demandati al backend mentre il filtro testuale sia svolto nel frontend rappresenta un buon esempio di divisione dei compiti:

- il backend fa il lavoro che dipende dai dati geospaziali;
- il frontend fa il lavoro di rifinitura sulla vista corrente.

---

## 8. Caricamento iniziale e priorità dei contenuti

Alla `DOMContentLoaded`, la pagina avvia diverse operazioni:

- setup dei tab;
- setup della ricerca;
- setup del filtro categoria;
- setup della modale cinema;
- setup del carosello;
- setup del bottone di paginazione.

Successivamente avvia in parallelo:

- caricamento film;
- caricamento categorie;
- caricamento cinema.

Questa scelta è importante dal punto di vista prestazionale: i contenuti visibili più importanti vengono richiesti subito, mentre altri aspetti dell’interfaccia si popolano in parallelo.

---

## 9. Tabs e filtri

### 9.1 Gestione dei tab

La funzione `setupTabs()` associa a ogni bottone della tab bar un listener che:

1. aggiorna lo stato visivo del tab attivo;
2. aggiorna `currentTab`;
3. reimposta `currentPage = 1`;
4. richiama `loadFilms()`.

Questo significa che il cambio tab viene trattato come una nuova query logica sul catalogo.

### 9.2 Ricerca per titolo

La funzione `setupSearch()` usa un debounce di 300 ms.

Questo dettaglio è importante: non viene lanciata una richiesta HTTP a ogni singolo carattere digitato, ma soltanto quando l’utente smette di scrivere per una breve finestra temporale.

Il debounce riduce:

- numero di chiamate al backend;
- rumore in rete;
- jitter visivo nel rendering della griglia.

### 9.3 Filtro per categoria

Le categorie vengono caricate una sola volta da backend e poi usate per popolare la `select`.

Quando l’utente cambia categoria:

- `currentCategoriaId` viene aggiornato;
- `currentPage` torna a `1`;
- la lista film viene ricaricata.

Anche in questo caso il filtro è trattato come una nuova query, non come un semplice filtro client-side sui risultati già presenti.

---

## 10. Rendering dei film

### 10.1 `loadFilms()` come orchestratore

La funzione `loadFilms()` è il cuore operativo della pagina. Essa:

1. verifica se esiste un cinema selezionato;
2. mostra gli stati intermedi corretti;
3. costruisce i query parameter per il backend;
4. richiama `API.getProgrammazioneFilms(...)`;
5. aggiorna lo stato locale (`currentFilms`, `currentPagedResult`);
6. delega il rendering a `renderFilms(...)`.

### 10.2 Parametri inviati al backend

La richiesta costruita dal frontend può includere:

- `tab`
- `search`
- `categoriaId`
- `cinemaId`
- `page`
- `pageSize`

In questo modo il backend riceve già il contesto completo della vista corrente.

---

## 11. Caroselli per `In evidenza` e `In uscita`

### 11.1 Perché un carosello

Per i tab `In evidenza` e `In uscita`, la pagina usa una visualizzazione orizzontale a carosello. Questo approccio consente di dare maggiore enfasi visiva a un numero limitato di titoli, mantenendo una densità informativa più bassa rispetto alla griglia completa.

### 11.2 Rendering del carosello

La funzione `renderFilmsCarousel(films)`:

1. sostituisce il contenuto del contenitore con un track orizzontale;
2. inserisce ogni card dentro `.programmazione-carousel-card`;
3. collega il track alle funzioni di sincronizzazione del contatore e dello stato delle frecce.

Il carosello non usa librerie esterne. Tutto il comportamento si basa su:

- `overflow-x: auto`;
- `scrollBy(...)`;
- calcolo manuale della larghezza card e del gap.

### 11.3 Frecce e contatore

Le frecce sono gestite da `setupCarouselControls()`.

Il contatore è aggiornato da `updateCarouselUI(track, totalFilms)`, che calcola:

- quante card sono realmente visibili a viewport corrente;
- l’indice corrente di partenza;
- l’intervallo visualizzato, ad esempio `1-4 / 10`.

Inoltre:

- le frecce vengono disabilitate quando il track è già all’inizio o alla fine;
- l’intero blocco controlli viene nascosto se il numero di card è insufficiente per richiedere scroll.

### 11.4 Paginazione progressiva del carosello

La pagina non carica più subito tutte le card dei caroselli.

Per `In evidenza` e `In uscita` usa una `pageSize` più piccola (`CAROUSEL_PAGE_SIZE = 8`). Quando l’utente si avvicina al fondo del carosello, `handleCarouselInfiniteLoad()` richiede automaticamente la pagina successiva al backend e concatena i nuovi film.

Questo comportamento è un esempio interessante di **lazy pagination** su contenuti orizzontali.

---

## 12. Griglia e paginazione di `Tutti i film`

### 12.1 Contratto paginato con il backend

Per il tab `Tutti i film`, il frontend usa la paginazione vera dell’endpoint `GET /programmazione/films`.

Il backend restituisce un payload con:

- `items`
- `page`
- `pageSize`
- `totalCount`
- `totalPages`
- `hasNextPage`
- `hasPreviousPage`

### 12.2 Caricamento incrementale

Quando l’utente si trova nel tab `Tutti i film`:

- la pagina richiede inizialmente i primi 20 elementi;
- il bottone `Carica altri film` viene mostrato solo se `hasNextPage = true`;
- a ogni click viene incrementato `currentPage`;
- `loadFilms({ append: true })` recupera la pagina successiva;
- i nuovi elementi vengono concatenati a `currentFilms`.

Si tratta quindi di un meccanismo di tipo **“load more”** supportato da paginazione reale lato backend.

### 12.3 Perché questa soluzione è migliore di una paginazione solo client-side

Con una paginazione solo client-side, il backend invierebbe comunque tutto il catalogo e il frontend si limiterebbe a mostrarne una parte alla volta. Questa soluzione sarebbe poco efficiente.

Con la paginazione backend:

- si riduce il payload iniziale;
- si riduce il tempo di parsing lato browser;
- si migliora la scalabilità al crescere del numero di film.

---

## 13. Card film e informazioni mostrate

La funzione `renderFilmCard(film)` costruisce la card base usata sia nella griglia sia nei caroselli.

Ogni card mostra:

- immagine di copertina;
- titolo;
- durata;
- fino a tre badge categoria;
- indicatore di disponibilità.

L’indicatore finale cambia in base al caso:

- **Disponibile** nel cinema selezionato, con eventuale prossimo show;
- **In uscita**, con data di rilascio;
- **Non disponibile in questo cinema**.

Questo è un buon esempio di rendering condizionale che usa lo stesso template visivo ma modifica il contenuto semantico in base ai dati ricevuti.

---

## 14. Gestione delle immagini

Le immagini sono risolte tramite `getCoverImage(copertinaPath)`.

Nelle card della programmazione vengono usati attributi importanti dal punto di vista prestazionale:

- `loading="lazy"`
- `decoding="async"`
- `fetchpriority="low"`
- `referrerpolicy="no-referrer"`

Questi dettagli permettono di ridurre l’impatto delle copertine sul caricamento iniziale della pagina, delegando il download ai momenti in cui l’immagine è effettivamente necessaria.

---

## 15. Interazione tra frontend e backend nella programmazione

La pagina `programmazione.html` è un ottimo esempio di cooperazione tra frontend e backend.

Il backend si occupa di:

- filtrare il catalogo in base al tab;
- applicare ricerca e filtro categoria;
- calcolare disponibilità nel cinema selezionato;
- ordinare e paginare i risultati;
- ordinare i cinema per distanza.

Il frontend si occupa di:

- gestire l’interazione utente;
- mantenere lo stato locale della vista;
- renderizzare card, caroselli e modale;
- decidere quando richiedere nuove pagine;
- sincronizzare il cinema scelto con navbar e storage locale.

Questa distribuzione delle responsabilità è didatticamente corretta perché evita di duplicare nel browser logiche di dominio che devono restare centralizzate lato server.

---

## 16. Conclusioni didattiche

Lo studio congiunto di `index.html` e `programmazione.html` permette di osservare due livelli diversi della stessa applicazione frontend.

La home page mostra un caso di **selezione e presentazione editoriale**:

- caricamento di dataset multipli;
- ranking dei contenuti;
- rotazione hero + card compatte.

La pagina programmazione mostra invece un caso di **interfaccia applicativa ricca**, in cui convivono:

- stato locale;
- sincronizzazione con backend;
- geolocalizzazione;
- persistenza preferenze;
- filtri;
- caroselli;
- paginazione incrementale;
- rendering adattivo.

Per uno studente, queste due pagine rappresentano un esempio molto utile di come un progetto JavaScript vanilla possa crescere in complessità senza perdere completamente chiarezza, a condizione che:

- le responsabilità siano separate;
- il codice sia suddiviso in moduli;
- il backend e il frontend collaborino attraverso contratti espliciti.

---

## 17. File di riferimento

Per studiare direttamente il codice descritto in questo tutorial, conviene consultare in parallelo i seguenti file:

- `frontend/CineBase.Web/wwwroot/index.html`
- `frontend/CineBase.Web/wwwroot/js/pages/home.js`
- `frontend/CineBase.Web/wwwroot/programmazione.html`
- `frontend/CineBase.Web/wwwroot/js/pages/programmazione.js`
- `frontend/CineBase.Web/wwwroot/js/template-loader.js`
- `frontend/CineBase.Web/wwwroot/js/navbar.js`
- `frontend/CineBase.Web/wwwroot/components/navbar-landing.html`
- `frontend/CineBase.Web/wwwroot/js/api.js`

Questa lettura incrociata consente di comprendere non solo il comportamento delle singole funzioni, ma soprattutto l’interazione tra i moduli che compongono il frontend pubblico di CineBase.
