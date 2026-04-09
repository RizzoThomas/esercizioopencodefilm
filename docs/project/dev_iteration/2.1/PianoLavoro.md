# Piano di Lavoro - Iterazione 2.1: Media Upload (Copertine) + Trailer URL

## 1) Obiettivo Iterazione

Implementare una gestione media robusta per i film, mantenendo complessita controllata:

- **Copertina film**: upload file immagine gestito dal backend API
- **Trailer/filmato**: URL esterno (YouTube/Vimeo o altro URL pubblico)

Questa iterazione non include upload video file su server.

---

## 2) Contesto Architetturale (repo attuale)

Struttura progetto da rispettare:

```
repo-root/
├── backend/
│   └── FilmAPI/
├── frontend/
│   └── CineBase.Web/
├── tests/
│   └── backend/
└── docs/
```

Porte attese:

- Backend: `http://localhost:5000`
- Frontend: `http://localhost:5001`

Configurazione porte via env/launch settings, non hardcoded nei `Program.cs`.

---

## 3) Scope Funzionale

### 3.1 In scope

1. Endpoint backend per upload copertina immagine (`multipart/form-data`)
2. Salvataggio file in storage locale backend (cartella media)
3. Restituzione URL pubblico della copertina
4. Form film frontend aggiornato:
   - upload file copertina (opzione principale)
   - campo trailer URL esterno
5. Flusso create/update film:
   - upload copertina (se file presente)
   - invio payload film con `copertinaPath` e `filmatoPath`
6. Validazioni base file e URL
7. Test backend dedicati (unit + integration)

### 3.2 Out of scope

- Upload video file lato backend
- Transcoding video
- CDN/media proxy
- Cancellazione fisica automatica file orfani (opzionale futura)

---

## 4) Design Tecnico - Backend

### 4.1 Nuovo endpoint media upload

Creare gruppo endpoint media, ad esempio:

- `POST /media/covers`

Request:

- `multipart/form-data`
- field file: `file` (`IFormFile`)

Response (200 OK):

```json
{
  "path": "/media/covers/<filename>",
  "fileName": "<filename>",
  "contentType": "image/jpeg",
  "size": 123456
}
```

Errori:

- `400` se file mancante/non valido
- `413` se troppo grande (se configurato)
- `415` content type non supportato

### 4.2 Regole validazione upload

- MIME consentiti: `image/jpeg`, `image/png`, `image/webp`
- estensioni consentite: `.jpg`, `.jpeg`, `.png`, `.webp`
- dimensione massima: es. `5 MB`
- nome file sicuro:
  - niente nome originale diretto
  - usare GUID + estensione validata

### 4.3 Storage locale

Percorso suggerito:

- fisico: `backend/FilmAPI/wwwroot/media/covers/`
- URL pubblico: `/media/covers/<file>`

Configurare static files in backend per servire `wwwroot` se non gia abilitato.

### 4.4 Configurazione CORS

Verificare che CORS consenta `POST` multipart dal frontend su `http://localhost:5001`.

### 4.5 DTO Film

Mantenere i DTO esistenti:

- `copertinaPath`: string URL/path
- `filmatoPath`: URL esterno trailer

Aggiungere validazione applicativa su `filmatoPath`:

- opzionale
- se presente deve essere URL assoluto valido (`http/https`)

---

## 5) Design Tecnico - Frontend

### 5.1 Aggiornamento modal film

In `frontend/CineBase.Web/wwwroot/films.html`:

- sostituire campo testo `Copertina URL` con input file:
  - `type="file"`
  - `accept="image/png,image/jpeg,image/webp"`
- rinominare/etichettare `Filmato URL` come `Trailer URL`
- mantenere compatibilita edit:
  - se non carico nuova copertina, preservare `copertinaPath` corrente

### 5.2 Flusso submit film

In `js/pages/films.js`:

1. leggere file copertina (se selezionato)
2. chiamare upload endpoint (`POST /media/covers`)
3. prendere `path` risposta upload
4. inviare create/update film con:
   - `copertinaPath` = path upload o valore esistente
   - `filmatoPath` = URL trailer (se valorizzato)

### 5.3 Nuovo metodo API frontend

In `js/api.js` aggiungere metodo helper upload:

- `uploadCover(file)` con `FormData`
- non impostare header `Content-Type` manualmente per multipart

### 5.4 UX minima

- spinner/stato "caricamento" durante upload
- disabilitare submit mentre upload in corso
- toast errore chiaro in caso upload fallito

---

## 6) Sicurezza e Robustezza

1. Non fidarsi del filename client
2. Validare estensione + content type
3. Imporre limite dimensione
4. Evitare path traversal
5. Restituire solo path relativo pubblico, non path fisico

---

## 7) Impatto su Test

Cartella test backend:

- `tests/backend/`

### 7.1 Nuovi test integration consigliati

1. Upload cover valido -> `200` + path valorizzato
2. Upload senza file -> `400`
3. Upload MIME non supportato -> `415`
4. Create film con `copertinaPath` da upload -> `201`
5. Create/Update film con `filmatoPath` URL invalido -> `400`

### 7.2 Test regressione

Eseguire test suite completa:

```bash
dotnet test tests/backend/FilmAPI.Tests.csproj
```

---

## 8) Checklist Implementazione

### Fase A - Backend media upload
- [ ] Creare endpoint `POST /media/covers`
- [ ] Aggiungere validazioni file (MIME/estensione/size)
- [ ] Salvare file in `wwwroot/media/covers`
- [ ] Restituire DTO risposta upload
- [ ] Verificare static file serving backend

### Fase B - Backend film validation
- [ ] Validare `filmatoPath` come URL assoluto se presente
- [ ] Mantenere fallback copertina default se assente

### Fase C - Frontend films modal
- [ ] Sostituire `Copertina URL` con input file
- [ ] Aggiornare label `Trailer URL`
- [ ] Gestire upload prima di create/update
- [ ] Gestire caso edit senza nuova copertina

### Fase D - Test e verifica
- [ ] Aggiungere/aggiornare integration test upload
- [ ] Eseguire `dotnet test tests/backend/FilmAPI.Tests.csproj`
- [ ] Verifica manuale frontend `films.html` (create/edit)

---

## 9) Criteri di Accettazione

L'iterazione e completata quando:

1. Admin puo creare film caricando una copertina file dal browser
2. Copertina salvata e visibile nel catalogo film
3. Admin puo inserire trailer URL esterno valido
4. URL trailer invalido produce errore utente chiaro
5. Nessun campo non supportato compare nel modal
6. Test backend passano tutti

---

## 10) Prompt Guida per AI (consigliato)

Usare un prompt operativo esplicito (adattabile):

"Implementa Iterazione 2.1 nel repo corrente rispettando la struttura `backend/FilmAPI`, `frontend/CineBase.Web`, `tests/backend`. Aggiungi upload copertina immagini lato backend (`POST /media/covers`, multipart), mantieni trailer come URL esterno in `filmatoPath`, aggiorna modal film frontend per file upload + trailer URL, valida input lato backend, aggiungi test integration dedicati, esegui `dotnet test tests/backend/FilmAPI.Tests.csproj` e riporta esito. Non introdurre porte hardcoded in Program.cs."
