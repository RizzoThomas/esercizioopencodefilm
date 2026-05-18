# Modello Dati

## Panoramica Entità

Il database si basa su Entity Framework Core con MySQL. Il modello dati è suddiviso in tre domini principali:
1. **Dominio Cinema Multisala** — Sale, Posti, Show
2. **Dominio Ticketing** — Ordini, Biglietti, Hold Posti
3. **Dominio Utente & Pagamenti** — Utenti, Credito, Movimenti, Auth

---

## Diagramma Entità-Relazioni

```mermaid
classDiagram
    class Film {
        +int Id
        +string Titolo
        +int Durata
        +string CopertinaPath
        +string? DescrizioneLunga
        +string? CastText
        +DateOnly? DataRilascio
        +int? TmdbId
        +string? ImdbId
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
        +string Indirizzo
        +string Citta
        +double? Latitudine
        +double? Longitudine
        +string? Telefono
        +string? CodiceLocale
    }
    class Sala {
        +int Id
        +int CinemaId
        +int NumeroProgressivo
        +TipoSala TipoSala
        +string? Nome
        +decimal Supplemento
        +bool IsAttiva
    }
    class SalaPosto {
        +int Id
        +int SalaId
        +string Settore
        +int Fila
        +int Numero
        +int? PosX
        +int? PosY
        +bool IsWheelchair
        +bool IsAttivo
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
        +int CinemaId
        +int SalaId
        +int FilmId
        +string HoldToken
        +int NumeroBiglietti
        +decimal TotaleLordo
        +decimal ImportoCredito
        +decimal ImportoCarta
        +string? StripePaymentIntentId
        +string? StripeCheckoutSessionId
        +string? IdempotencyKey
        +OrdineState Stato
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
        +decimal PrezzoBase
        +decimal Supplemento
        +decimal PrezzoTotale
        +BigliettoState Stato
        +DateTime? ValidatoAtUtc
        +int? ValidatoDaUserId
        +int? ValidatoCinemaId
    }
    class User {
        +int Id
        +string Email
        +string? PasswordHash
        +string Nome
        +string Cognome
        +string? Telefono
        +UserRole Ruolo
        +int? CinemaPreferitoId
        +decimal CreditoResiduo
        +bool TwoFactorEnabled
        +int AuthVersion
        +bool IsDisabled
    }
    class MovimentoCredito {
        +int Id
        +int UserId
        +MovimentoCreditoTipo Tipo
        +decimal Importo
        +decimal SaldoPre
        +decimal SaldoPost
        +int? OrdineId
    }

    Film "1" --> "many" Proiezione : ha
    Film "1" --> "many" FilmCategoria : categorizzato
    Film "1" --> "many" Show : protagonista
    Film "many" --> "1" Regista : diretto da
    Categoria "1" --> "many" FilmCategoria : include
    Cinema "1" --> "many" Sala : contiene
    Cinema "1" --> "many" Show : ospita
    Cinema "1" --> "many" Proiezione : (legacy)
    Sala "1" --> "many" SalaPosto : ha posti
    Sala "1" --> "many" Show : programma
    Show "1" --> "many" ShowPostoStato : stato posti
    Show "1" --> "many" Biglietto : venduti
    Show "1" --> "many" Ordine : generati
    ShowPostoStato "many" --> "1" Ordine : riferimento
    Ordine "1" --> "many" Biglietto : contiene
    User "1" --> "many" Ordine : possiede
    User "1" --> "many" Biglietto : possiede
    User "1" --> "many" MovimentoCredito : storico
    User "1" --> "many" RefreshToken : sessioni
    User "1" --> "many" Prenotazione : (legacy)
    User "1" --> "0..1" Cinema : preferito
```

---

## Enumerazioni

### TipoSala
```csharp
public enum TipoSala {
    DueD = 0,    // Sala 2D standard
    TreD = 1,    // Sala 3D
    ISENSE = 2,  // Sala premium ISENSE
    XL = 3       // Sala grande formato
}
```

### OrdineState (macchina a stati principale)
```csharp
public enum OrdineState {
    Pending = 0,            // Ordine creato, in attesa pagamento
    Paid = 1,               // Pagato con successo
    Failed = 2,             // Pagamento fallito
    Cancelled = 3,          // Annullato dall'utente
    Expired = 4,            // Scaduto (TTL hold superato)
    CheckoutInProgress = 5  // Checkout Stripe hosted in corso
}
```

### ShowPostoState
```csharp
public enum ShowPostoState {
    Hold = 0,  // Posto tenuto temporaneamente
    Sold = 1   // Posto venduto (biglietto emesso)
}
```

### BigliettoState
```csharp
public enum BigliettoState {
    Issued = 0,     // Emesso
    Validated = 1,  // Validato all'ingresso
    Cancelled = 2   // Annullato
}
```

### MovimentoCreditoTipo
```csharp
public enum MovimentoCreditoTipo {
    TopUp = 0,       // Ricarica credito
    DebitOrder = 1,  // Addebito per ordine
    Refund = 2,      // Rimborso
    Adjustment = 3   // Rettifica manuale (admin)
}
```

### UserRole (RBAC)
```csharp
public enum UserRole {
    User = 0,       // Utente base: acquista biglietti
    PowerUser = 1,  // Operatore: gestisce film, sale, show
    Admin = 2       // Amministratore: full access inclusi utenti e credito
}
```

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

---

## Indici Unici Configurati

| Tabella | Colonna/e | Descrizione |
|---------|-----------|-------------|
| `Sala` | `(CinemaId, NumeroProgressivo)` | Unicità numero sala per cinema |
| `SalaPosto` | `(SalaId, Settore, Fila, Numero)` | Unicità posto nella sala |
| `Show` | `(CinemaId, SalaId, StartAtUtc)` | Unicità spettacolo per sala/orario |
| `ShowPostoStato` | `(ShowId, SalaPostoId)` | Un posto, uno stato per show |
| `ShowPostoStato` | `HoldToken` | Unicità token di hold |
| `Ordine` | `CodiceOrdine` | Unicità codice ordine |
| `Ordine` | `IdempotencyKey` | Idempotenza (client-generated) |
| `Biglietto` | `(ShowId, SalaPostoId)` | Un biglietto per posto per show |
| `Biglietto` | `CodiceBiglietto` | Unicità codice biglietto |

---

## Migration EF Core

La migration principale è `AddMultisalaTicketing` che ha:
1. Creato lo schema completo multisala/ticketing (7 nuove tabelle)
2. Eseguito la data migration legacy:
   - Inizializzato `CreditoResiduo = 0` per utenti esistenti
   - Creata `Sala 1` di default per cinema senza sale
   - Migrato `Proiezione -> Show` con composizione `StartAtUtc` da `Data + Ora`
   - Gestito conflitti temporali con sale auto-migrate

Altre migrations:
- `AddRefreshTokenDeviceId`: Aggiunto `DeviceId` ai refresh token
- `AddStripeCheckoutFieldsToOrdine`: Aggiunti campi per Stripe Checkout hosted

```bash
# Comandi EF Core usati
dotnet ef migrations add NomeMigration --project backend/FilmAPI
dotnet ef database update --project backend/FilmAPI
```
