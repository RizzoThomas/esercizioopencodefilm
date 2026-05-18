# Modello Dati

## Panoramica

Il database si basa su Entity Framework Core con MySQL. Il modello dati è suddiviso in tre domini principali e comprende **37 entità**, **6 enumerazioni** e **9 vincoli di unicità**.

---

## Tabella Riepilogativa Entità

| Dominio | Entità | Descrizione | Relazioni Chiave |
|---------|--------|-------------|-------------------|
| **Cinema Multisala** | `Film` | Film cinematografico | 1 regista, N proiezioni, N show, N categorie |
| | `Regista` | Regista film | N film |
| | `Categoria` | Categoria film | N film (many-to-many) |
| | `Cinema` | Sede cinematografica | N sale, N proiezioni, N show |
| | `Sala` | Sala all'interno del cinema | N posti, N show |
| | `SalaPosto` | Posto singolo con coordinate | 1 sala |
| | `Show` | Spettacolo programmato | 1 film, 1 cinema, 1 sala |
| | `Proiezione` | Entità legacy (da migrare) | Bridge verso Show |
| **Ticketing** | `ShowPostoStato` | Stato posto per show | Hold/Sold per utente |
| | `Ordine` | Ordine di acquisto | N biglietti, 1 user, 1 show |
| | `Biglietto` | Biglietto digitale emesso | 1 ordine, 1 posto |
| | `Prenotazione` | Entità legacy pre-ticketing | Mantenuta per compatibilità |
| **Utenti & Pagamenti** | `User` | Utente della piattaforma | N ordini, N biglietti, N refresh token |
| | `MovimentoCredito` | Transazione credito | 1 utente, opzionale 1 ordine |
| | `RefreshToken` | JWT refresh token | 1 utente, device-aware |
| | `UserExternalLogin` | Social login link | 1 utente, 1 provider |

---

## Diagramma Entità-Relazioni Completo

```mermaid
classDiagram
    class Film {
        +int Id
        +string Titolo
        +int Durata
        +string? CopertinaPath
        +string? DescrizioneLunga
        +string? CastText
        +DateOnly? DataRilascio
        +int? TmdbId
        +double? VoteAverage
        +int RegistaId
    }
    
    class Regista {
        +int Id
        +string Nome
        +string Cognome
    }
    
    class Categoria {
        +int Id
        +string Nome
    }
    
    class FilmCategoria {
        +int FilmId
        +int CategoriaId
    }
    
    class Cinema {
        +int Id
        +string Nome
        +string Citta
        +string Indirizzo
        +double? Latitudine
        +double? Longitudine
    }
    
    class Sala {
        +int Id
        +int CinemaId
        +int NumeroProgressivo
        +TipoSala TipoSala
        +decimal Supplemento
        +bool IsAttiva
    }
    
    class SalaPosto {
        +int Id
        +int SalaId
        +string Settore
        +int Fila
        +int Numero
        +int? PosX, PosY
        +bool IsWheelchair
    }
    
    class Show {
        +int Id
        +int CinemaId
        +int SalaId
        +int FilmId
        +DateTime StartAtUtc
        +int DurataMinutiSnapshot
        +decimal PrezzoBase
        +decimal SupplementoSala
    }
    
    class ShowPostoStato {
        +int Id
        +int ShowId
        +int SalaPostoId
        +int UserId
        +ShowPostoState Stato
        +string? HoldToken
        +DateTime? ScadeAtUtc
        +int? OrdineId
    }
    
    class Ordine {
        +int Id
        +string CodiceOrdine
        +int UserId
        +int ShowId
        +int NumeroBiglietti
        +decimal TotaleLordo
        +decimal ImportoCredito
        +decimal ImportoCarta
        +OrdineState Stato
        +string? StripeCheckoutSessionId
        +decimal CreditoRiservato
    }
    
    class Biglietto {
        +int Id
        +int OrdineId
        +int ShowId
        +int SalaPostoId
        +int UserId
        +string CodiceBiglietto
        +string BarcodeValue
        +BigliettoState Stato
        +DateTime? ValidatoAtUtc
    }
    
    class User {
        +int Id
        +string Email
        +string? PasswordHash
        +string Nome, Cognome
        +UserRole Ruolo
        +decimal CreditoResiduo
        +int? CinemaPreferitoId
        +bool TwoFactorEnabled
        +int AuthVersion
        +bool IsDisabled
    }
    
    class MovimentoCredito {
        +int Id
        +int UserId
        +MovimentoCreditoTipo Tipo
        +decimal Importo
        +decimal SaldoPre, SaldoPost
        +int? OrdineId
    }

    Film "1" --> "many" FilmCategoria
    Categoria "1" --> "many" FilmCategoria
    Film "1" --> "many" Show
    Film "many" --> "1" Regista
    Cinema "1" --> "many" Sala
    Cinema "1" --> "many" Show
    Sala "1" --> "many" SalaPosto
    Sala "1" --> "many" Show
    Show "1" --> "many" ShowPostoStato
    Show "1" --> "many" Biglietto
    Show "1" --> "many" Ordine
    Ordine "1" --> "many" Biglietto
    User "1" --> "many" Ordine
    User "1" --> "many" Biglietto
    User "1" --> "many" MovimentoCredito
    User "1" --> "many" RefreshToken
    User "1" --> "0..1" Cinema : preferito
```

---

## Enumerazioni

| Enum | Valori | Valore Int | Descrizione |
|------|--------|-----------|-------------|
| `TipoSala` | DueD, TreD, ISENSE, XL | 0-3 | Tipologia sala cinematografica |
| `OrdineState` | Pending, Paid, Failed, Cancelled, Expired, CheckoutInProgress | 0-5 | Ciclo di vita dell'ordine |
| `ShowPostoState` | Hold, Sold | 0-1 | Stato posto per uno show |
| `BigliettoState` | Issued, Validated, Cancelled | 0-2 | Ciclo di vita del biglietto |
| `MovimentoCreditoTipo` | TopUp, DebitOrder, Refund, Adjustment | 0-3 | Tipo transazione credito |
| `UserRole` | User, PowerUser, Admin | 0-2 | Ruoli utente (RBAC) |

---

## Ciclo di Vita dell'Ordine

```mermaid
stateDiagram-v2
    [*] --> Pending: Crea ordine da hold
    Pending --> CheckoutInProgress: Crea sessione Stripe
    Pending --> Paid: Pagamento solo credito
    Pending --> Failed: Errore pagamento
    Pending --> Cancelled: Utente annulla
    Pending --> Expired: TTL scaduto

    CheckoutInProgress --> Paid: Webhook checkout.completed
    CheckoutInProgress --> Cancelled: Utente annulla
    CheckoutInProgress --> Expired: TTL checkout scaduto

    Paid --> [*]: Biglietti emessi, email inviata
    Cancelled --> [*]: Posti rilasciati, credito ripristinato
    Expired --> [*]: Posti rilasciati, credito ripristinato
    Failed --> [*]: Posti rilasciati
```

### Tabella Transizioni di Stato

| Stato Iniziale | Evento | Stato Finale | Azioni |
|----------------|--------|-------------|--------|
| (nessuno) | Utente seleziona posti + click continua | Pending | Hold token consumato, ordine creato |
| Pending | Pagamento con credito riuscito | Paid | Credito addebitato, biglietti emessi |
| Pending | Stripe Checkout avviato | CheckoutInProgress | Sessione Stripe creata, credito riservato |
| Pending | Utente clicca annulla | Cancelled | Posti rilasciati, hold rimosso |
| Pending | TTL hold superato | Expired | Cleanup automatico, posti liberati |
| CheckoutInProgress | Webhook checkout.completed | Paid | Credito riservato addebitato, biglietti emessi |
| CheckoutInProgress | Webhook checkout.expired | Expired | Rilascio posti, credito ripristinato |
| CheckoutInProgress | Utente annulla | Cancelled | Rilascio posti, credito ripristinato |

---

## Ciclo di Vita del Biglietto

```mermaid
stateDiagram-v2
    [*] --> Issued: Pagamento confermato
    Issued --> Validated: Operatore valida all'ingresso
    Issued --> Cancelled: Ordine rimborsato
    Validated --> [*]: Ingresso consentito
    Cancelled --> [*]: Biglietto non valido
```

---

## Indici Unici

| Tabella | Colonne | Descrizione |
|---------|---------|-------------|
| `Sala` | `(CinemaId, NumeroProgressivo)` | Unicità numero sala per cinema |
| `SalaPosto` | `(SalaId, Settore, Fila, Numero)` | Unicità posto nella sala |
| `Show` | `(CinemaId, SalaId, StartAtUtc)` | Due show non possono iniziare nello stesso momento nella stessa sala |
| `ShowPostoStato` | `(ShowId, SalaPostoId)` | Un solo stato per posto per show |
| `Ordine` | `CodiceOrdine` | Codice ordine univoco |
| `Ordine` | `IdempotencyKey` | Evita duplicati in caso di rete |

---

## Migration EF Core

| Migration | Data | Modifiche |
|-----------|------|-----------|
| InitialCreate | Iterazione 2 | Schema base: Film, Regista, Cinema, Proiezione, Prenotazione |
| AddRefreshTokenDeviceId | 2026-04-13 | Colonna DeviceId su RefreshToken per auth device-aware |
| AddMultisalaTicketing | 2026-04-16 | 7 nuove tabelle (Sala, SalaPosto, Show, ShowPostoStato, Ordine, Biglietto, MovimentoCredito) + data migration legacy |
| AddStripeCheckoutFieldsToOrdine | 2026-04-19 | Campi Stripe Checkout su Ordine (SessionId, CheckoutExpiresAt, CreditoRiservato) |

### Comandi

```bash
# Creare una migration
dotnet ef migrations add NomeMigration --project backend/FilmAPI

# Applicare al database
dotnet ef database update --project backend/FilmAPI
```
