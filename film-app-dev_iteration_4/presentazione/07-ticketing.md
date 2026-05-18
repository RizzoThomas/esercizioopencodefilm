# Ticketing Digitale

## Panoramica

Il sistema di ticketing digitale copre l'intero ciclo di vita del biglietto: emissione automatica al pagamento, generazione PDF multipagina con QR code, invio email e validazione all'ingresso.

---

## Ciclo di Vita del Biglietto

```mermaid
stateDiagram-v2
    [*] --> Issued: Pagamento confermato
    Issued --> Validated: Operatore valida all'ingresso
    Issued --> Cancelled: Rimborso/annullamento
    Validated --> [*]: Ingresso consentito
    Cancelled --> [*]: Biglietto non valido
```

### Tabella Stati Biglietto

| Stato | Valore | Descrizione | Azioni Possibili |
|-------|--------|-------------|------------------|
| Issued | 0 | Emesso, in attesa di utilizzo | Validazione, download PDF |
| Validated | 1 | Già utilizzato per l'ingresso | Solo visualizzazione |
| Cancelled | 2 | Annullato o rimborsato | Nessuna |

---

## Flusso di Emissione Completo

```mermaid
sequenceDiagram
    participant B as Backend
    participant DB as Database
    participant PDF as PdfService
    participant EM as EmailService
    participant U as Utente

    Note over B: Pagamento confermato (stato = Paid)
    
    B->>B: BigliettoService.EmittiBigliettiAsync(ordineId)
    
    loop Per ogni posto nell'ordine
        B->>B: Genera codice univoco CB-XXXXXXXX
        B->>B: Genera barcode value
        B->>DB: INSERT Biglietto (Issued)
        B->>DB: UPDATE ShowPostoStato → Sold
    end

    B->>PDF: GeneraPdfOrdineAsync(ordineId)
    PDF->>PDF: Crea documento QuestPDF
    PDF->>PDF: 1 pagina per biglietto
    PDF->>PDF: QR code dati biglietto
    PDF->>PDF: Barcode grafico
    PDF->>PDF: Dettagli: film, cinema, sala, data, posto
    PDF-->>B: byte[] pdfBytes

    B->>EM: InviaTicketEmailAsync(ordine, pdfBytes)
    EM->>EM: Costruisce corpo HTML
    EM->>EM: Allega PDF
    EM->>EM: Invia via SMTP (MailKit)

    Alt Email inviata con successo
        B->>DB: Ordine.TicketEmailSentAtUtc = now
    Else Email fallita
        B->>DB: Ordine.TicketEmailLastError = errore
        Note right of B: Nessun rollback dell'ordine pagato
    End

    U->>B: GET /checkout/orders/{orderId}/pdf
    B-->>U: Scarica biglietti-{codice}.pdf
```

---

## Struttura del PDF (QuestPDF)

### Contenuto di Ogni Pagina

| Sezione | Contenuto | Tecnologia |
|---------|-----------|------------|
| Intestazione | Logo CineBase, titolo "Biglietto Cinema" | QuestPDF layout |
| QR Code | Dati biglietto codificati | QRCoder |
| Informazioni film | Titolo, durata, descrizione breve | QuestPDF text |
| Informazioni cinema | Nome cinema, indirizzo, sala | QuestPDF text |
| Data e ora | Data spettacolo formattata it-IT | QuestPDF text |
| Posto | Settore, fila, numero | QuestPDF text |
| Codice biglietto | CB-XXXXXXXX (font monospace) | QuestPDF text |
| Prezzo | Formattato €X.XX | QuestPDF text |
| Barcode | Barcode grafico 1D | ZXing.Net |
| Piè di pagina | Condizioni di validità | QuestPDF text |

---

## Invio Email (MailKit)

### Tabella Provider Supportati

| Provider | Host | Porta | TLS | Autenticazione |
|----------|------|-------|-----|----------------|
| Google SMTP | smtp.gmail.com | 587 | Sì | OAuth2 / App Password |
| Twilio SendGrid | smtp.sendgrid.net | 587 | Sì | API Key |

### Configurazione .env

```env
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=mittente@gmail.com
SMTP_PASSWORD=password_app_16_caratteri
SMTP_FROM_EMAIL=noreply@cinebase.it
SMTP_FROM_NAME=CineBase
```

### Architettura Provider-Agnostic

```csharp
public interface IEmailService {
    Task SendTicketEmailAsync(
        string to,              // Email destinatario
        string subject,         // Oggetto email
        string htmlBody,        // Corpo HTML
        byte[] pdfAttachment,   // PDF allegato
        string pdfFileName      // Nome file PDF
    );
}

// Implementazione reale: EmailService (MailKit)
// Fake per test: FakeEmailService (sostituito in DI)
```

---

## Validazione Biglietti

```mermaid
sequenceDiagram
    participant O as Operatore
    participant F as Frontend (validazione.html)
    participant B as Backend
    participant DB as Database

    O->>F: Inserisce/scansiona codice biglietto
    F->>B: GET /admin/tickets/validate/{codice}
    B->>DB: Cerca Biglietto per CodiceBiglietto
    
    Alt Biglietto non trovato
        B-->>F: 404 Not Found
        F-->>O: "Biglietto non trovato"
    Else Biglietto valido
        B-->>F: { film, cinema, sala, data, posto, stato }
        F-->>O: Mostra dettagli biglietto
        
        O->>F: Click "Valida"
        F->>B: POST /admin/tickets/validate { codice, cinemaId }
        B->>B: ValidazioneBigliettoService.ValidaBigliettoAsync
        
        Alt Già validato
            B-->>F: 409 Conflict
            F-->>O: "Biglietto già validato il DD/MM/YYYY"
        Else Cinema errato
            B-->>F: 400 Bad Request
            F-->>O: "Questo biglietto non è per questo cinema"
        Else Successo
            B->>DB: Biglietto.Stato = Validated
            B->>DB: ValidatoAtUtc, ValidatoDaUserId, ValidatoCinemaId
            B-->>F: 200 OK { validato: true }
            F-->>O: "Ingresso consentito"
        End
    End
```

### Regole di Validazione

| Condizione | Codice HTTP | Messaggio |
|------------|-------------|-----------|
| Codice inesistente | 404 | Biglietto non trovato |
| Già validato | 409 | Biglietto già validato il [data] |
| Cinema non corrispondente | 400 | Questo biglietto non è per questo cinema |
| Biglietto annullato | 400 | Biglietto annullato |
| Validazione riuscita | 200 | Ingresso consentito |

---

## Endpoint Backend Ticketing

| Metodo | Endpoint | Auth | Descrizione |
|--------|----------|------|-------------|
| GET | `/checkout/orders/{orderId}/pdf` | Authenticated | Download PDF dell'ordine |
| GET | `/admin/tickets/validate/{code}` | PowerUserOrAdmin | Ricerca biglietto per codice |
| POST | `/admin/tickets/validate` | PowerUserOrAdmin | Convalida biglietto all'ingresso |

---

## Servizi Backend

| Servizio | Metodi Principali |
|----------|-------------------|
| `IBigliettoService` | `EmittiBigliettiAsync`, `GetBigliettiByOrdineAsync`, `GetBigliettoByCodiceAsync` |
| `IPdfService` | `GeneraPdfOrdineAsync` |
| `IEmailService` | `SendTicketEmailAsync` |
| `IValidazioneBigliettoService` | `LookupBigliettoAsync`, `ValidaBigliettoAsync` |

---

## Librerie Utilizzate

| Libreria | Versione | Utilizzo |
|----------|----------|----------|
| QuestPDF | 2024.x | Generazione PDF multipagina |
| QRCoder | 1.x | Codici QR per ogni biglietto |
| ZXing.Net | 0.16.x | Barcode grafico 1D |
| MailKit | 4.x | Client SMTP per invio email |
| PdfPig | — | Lettura PDF nei test di integrazione |
