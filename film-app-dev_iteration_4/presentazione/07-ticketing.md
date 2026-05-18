# Ticketing Digitale

## Panoramica

Il sistema di ticketing digitale copre l'intero ciclo di vita del biglietto: dall'emissione alla validazione, passando per PDF, email e QR code.

---

## Architettura del Ticketing

```mermaid
graph TB
    subgraph "Emissione"
        PAY[Pagamento confermato]
        BS[BigliettoService]
        PS[PdfService]
        ES[EmailService]
    end

    subgraph "Output"
        PDF[PDF Multipagina]
        QR[QR Code + Barcode]
        EMAIL[Email SMTP]
    end

    subgraph "Validazione"
        VS[ValidazioneBigliettoService]
        VE[Endpoint Validazione]
        SC[Scanner QR/Codice]
    end

    subgraph "Storage"
        DB[(MySQL)]
        TICK[Biglietto entity]
        ORD[Ordine entity]
    end

    PAY --> BS
    BS --> ORD
    BS --> TICK
    BS --> PS
    BS --> ES
    PS --> PDF
    PS --> QR
    ES --> EMAIL
    PDF --> EMAIL

    SC --> VE
    VE --> VS
    VS --> DB
    VS --> TICK
    VS --> ORD
```

---

## Flusso di Emissione Biglietti

```mermaid
sequenceDiagram
    participant B as Backend
    participant DB as Database
    participant PDF as PdfService
    participant EMAIL as EmailService
    participant U as Utente

    Note over B: Pagamento confermato (stato=Paid)

    B->>B: BigliettoService.EmittiBigliettiAsync()
    B->>DB: Per ogni posto:
    B->>DB: INSERT Biglietto (codice CB-XXXXXXXX)
    B->>DB: UPDATE ShowPostoStato → Sold
    B->>DB: Genera CodiceUnivoco, BarcodeValue

    B->>PDF: GeneraPdfOrdineAsync(ordineId)
    PDF->>PDF: Crea documento QuestPDF multipagina
    PDF->>PDF: 1 biglietto per pagina
    PDF->>PDF: QR Code + Barcode
    PDF->>PDF: Dati: film, cinema, sala, data, posto, prezzo
    PDF-->>B: byte[] PDF

    B->>EMAIL: InviaTicketEmailAsync(ordine, pdfBytes)
    EMAIL->>EMAIL: Costruisce HTML corpo email
    EMAIL->>EMAIL: Allega PDF
    EMAIL->>EMAIL: Invia via SMTP (MailKit)
    alt Email inviata
        EMAIL-->>B: Successo
        B->>DB: Ordine.TicketEmailSentAtUtc = now
    else Email fallita
        EMAIL-->>B: Errore
        B->>DB: Ordine.TicketEmailLastError = error
    end

    U->>B: GET /checkout/orders/{orderId}/pdf
    B-->>U: PDF bytes
    U->>U: Download / biglietti-{codice}.pdf
```

---

## Generazione PDF (QuestPDF)

Il PDF viene generato con **QuestPDF** (libreria C# per PDF, licenza community).

### Struttura del PDF Multipagina

```
┌──────────────────────────────────────┐
│          BIGLIETTO CINEMA            │
│  ┌──────────────────────────────┐    │
│  │         QR CODE              │    │
│  │     ██ ▄▄▄ ██ ▄▄▄           │    │
│  │     ▄▄█ █ ▄▄▄█▄▄            │    │
│  └──────────────────────────────┘    │
│                                      │
│  CineBase - La Tua Rete Cinematogr.  │
│                                      │
│  Film:      Il Padrino               │
│  Cinema:    Roma Moderno             │
│  Sala:      Sala 3 - ISENSE          │
│  Data:      mer 25/05/2026           │
│  Orario:    21:00                    │
│                                      │
│  Posto:     PLATEA CENTRO - Fila 8   │
│  Posto n:   12                       │
│                                      │
│  Codice:    CB-A1B2C3D4              │
│  Prezzo:    14,50 €                  │
│  ──────────────────────────────      │
│  ▌▌▌▌▌▌ BARCODE ▌▌▌▌▌▌             │
└──────────────────────────────────────┘
        (una pagina per biglietto)
```

### Dipendenze

```xml
<PackageReference Include="QuestPDF" Version="2024.*" />
<PackageReference Include="QRCoder" Version="1.*" />
<PackageReference Include="ZXing.Net" Version="0.16.*" />
```

---

## Invio Email (MailKit)

### Provider Supportati

| Provider | Configurazione | Tipo |
|----------|---------------|------|
| **Google SMTP** | `smtp.gmail.com:587`, TLS, OAuth2/App Password | Baseline operativa |
| **Twilio SendGrid** | `smtp.sendgrid.net:587`, TLS, API Key | Alternativa documentata |

### Configurazione `.env`

```env
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your-email@gmail.com
SMTP_PASSWORD=your-app-password
SMTP_FROM_EMAIL=noreply@cinebase.it
SMTP_FROM_NAME=CineBase
```

### Architettura Provider-Agnostic

```csharp
public interface IEmailService {
    Task SendTicketEmailAsync(string to, string subject, string htmlBody, byte[] pdfAttachment, string pdfFileName);
}

// Implementazione concreta: EmailService (MailKit)
// Fake per test: sostituito in CustomWebApplicationFactory
```

---

## Validazione Biglietti

```mermaid
sequenceDiagram
    participant O as Operatore
    participant F as Frontend (validazione.html)
    participant B as Backend
    participant DB as Database

    O->>F: Inserisce codice biglietto (o scannerizza QR)
    F->>B: GET /admin/tickets/validate/{code}
    B->>DB: Lookup Biglietto per CodiceBiglietto
    B->>B: ValidazioneBigliettoService.ValidaBigliettoAsync

    alt Biglietto trovato
        B-->>F: { film, cinema, sala, data, posto, stato }
        F-->>O: Mostra dettagli biglietto
    else Biglietto non trovato
        B-->>F: 404
        F-->>O: "Biglietto non trovato"
    end

    O->>F: Click "Valida" (conferma ingresso)
    F->>B: POST /admin/tickets/validate { codice, cinemaId }
    B->>B: ValidazioneBigliettoService.ValidaBigliettoAsync

    alt Già validato
        B-->>F: 409 Conflict
        F-->>O: "Biglietto già validato il DD/MM/YYYY"
    else Cinema errato
        B-->>F: 400 BadRequest
        F-->>O: "Questo biglietto non è per questo cinema"
    else Successo
        B->>DB: Biglietto.Stato = Validated
        B->>DB: ValidatoAtUtc, ValidatoDaUserId, ValidatoCinemaId
        B-->>F: { successo, timestamp }
        F-->>O: "✅ Ingresso consentito"
    end
```

### Regole di Validazione

| Regola | Comportamento |
|--------|---------------|
| Biglietto non trovato | 404 Not Found |
| Biglietto già validato | 409 Conflict + data validazione |
| Cinema non corrispondente | 400 Bad Request |
| Biglietto annullato | 400 Bad Request |
| Biglietto emesso e valido | ✅ Validazione OK |

---

## Endpoint Backend Ticketing

| Metodo | Endpoint | Auth | Descrizione |
|--------|----------|------|-------------|
| `GET` | `/checkout/orders/{orderId}/pdf` | Authenticated | Download PDF ordine |
| `GET` | `/admin/tickets/validate/{code}` | PowerUserOrAdmin | Lookup biglietto per codice |
| `POST` | `/admin/tickets/validate` | PowerUserOrAdmin | Valida biglietto (conferma ingresso) |

### Servizi Backend

| Servizio | Metodi Principali |
|----------|-------------------|
| `IBigliettoService` | `EmittiBigliettiAsync`, `GetBigliettiByOrdineAsync`, `GetBigliettoByCodiceAsync` |
| `IPdfService` | `GeneraPdfOrdineAsync` |
| `IEmailService` | `SendTicketEmailAsync` |
| `IValidazioneBigliettoService` | `LookupBigliettoAsync`, `ValidaBigliettoAsync` |
