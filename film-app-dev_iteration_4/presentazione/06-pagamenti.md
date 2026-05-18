# Pagamenti

## Panoramica

CineBase supporta tre modalità di pagamento:
1. **Solo credito** — pagamento interno alla piattaforma (nessun addebito esterno)
2. **Solo carta** — Stripe Checkout Session hosted
3. **Misto (credito + carta)** — quota credito + residuo su Stripe

L'architettura è evoluta da Stripe Elements (embedded) a **Stripe Checkout hosted** per maggiore sicurezza e minore complessità PCI.

---

## Metodi di Pagamento

```mermaid
flowchart TD
    PAG[pagamento.html] --> M{Metodo scelto}
    
    M -->|Solo Credito| CRED[Paga con credito]
    CRED --> POST[POST /checkout/orders/{id}/pay]
    POST --> VER{Saldo sufficiente?}
    VER -->|Sì| OK[Ordine → Paid]
    VER -->|No| ERR[Mostra errore]
    OK --> ESITO[esito-acquisto.html]

    M -->|Solo Carta| CARTA[POST /checkout/orders/{id}/stripe-checkout-session]
    CARTA --> STS[Crea sessione Stripe Checkout]
    STS --> RED[Redirect a checkout.stripe.com]
    RED --> WEB{Webhook completed?}
    WEB -->|Sì| FIN[Finalizza ordine]
    WEB -->|No| POLL[Polling riconciliazione]
    FIN --> ESITO2[esito-acquisto.html]

    M -->|Misto| MISTO[Riserva credito + crea sessione Stripe]
    MISTO --> RES[CreditoRiservato = importo]
    RES --> STS2[Crea sessione Stripe per residuo]
    STS2 --> RED2[Redirect a Stripe]
    RED2 --> WEB2{Webhook completed?}
    WEB2 -->|Sì| FIN2[Addebita credito riservato + finalizza]
    WEB2 -->|No| POLL2[Polling + rilascio credito se scade]
```

---

## Flusso Stripe Checkout Hosted

```mermaid
sequenceDiagram
    participant U as Utente
    participant F as Frontend
    participant B as Backend
    participant STR as Stripe
    participant DB as Database

    U->>F: Seleziona "Paga con carta" o "Misto"
    F->>B: GET /config/frontend (publishable key)
    B-->>F: { stripePublishableKey }

    alt Misto
        F->>B: POST /checkout/orders/{id}/stripe-checkout-session
        B->>B: CreditoService.ReserveOrderCreditAsync
        B->>DB: Ordine.CreditoRiservato = importoCredito
        B->>DB: Ordine.Stato = CheckoutInProgress
        B->>DB: Ordine.StripeCheckoutSessionId = sessionId
        B->>B: StripeGateway.CreateCheckoutSessionAsync
        B->>STR: Crea Checkout Session (importo = residuo)
        STR-->>B: { sessionId, url }
        B-->>F: { stripeCheckoutUrl, sessionId }
    end

    F->>U: Redirect a Stripe Checkout hosted
    U->>STR: Inserisce dati carta
    STR-->>U: Conferma pagamento

    U->>F: Redirect a esito-acquisto.html?success=true
    F->>B: POST /checkout/orders/{id}/reconcile-checkout-session
    B->>STR: Sessione completata?
    STR-->>B: Sì
    B->>B: PagamentoService.ReconcileCheckoutSessionAsync
    B->>DB: Ordine → Paid, addebita credito riservato
    B->>DB: Emetti biglietti, invia email

    STR->>B: Webhook: checkout.session.completed
    B->>B: Idempotent handler
    B->>DB: Finalizza se non già fatto

    F->>B: Polling ogni 3s fino a stato Paid
    B-->>F: Ordine → Paid
    F->>U: Mostra successo + PDF biglietti
```

---

## Pagina Pagamento (`pagamento.html`) — `pagamento.js`

### Logica

```mermaid
flowchart TD
    A[DOMContentLoaded] --> B{Autenticato?}
    B -->|No| C[Redirect login]
    B -->|Sì| D[Carica ordine, credito, config in parallelo]

    D --> E{Stato ordine?}
    E -->|CheckoutInProgress| F[Tenta reconcile]
    F --> G{Ancora CheckoutInProgress?}
    G -->|Sì| H[Cancella ordine + redirect esito]
    G -->|No| I[Riprova]

    E -->|Pending| J[renderOrderSummary]
    J --> K[setupPaymentOptions]
    K --> L[Credito sufficiente? → abilita option-credito]
    K --> M[Saldo > 0? → abilita option-misto]

    L --> N[Attendi click Paga]
    M --> N

    N --> O{Metodo?}
    O -->|Credito| P[POST /checkout/orders/{id}/pay]
    O -->|Carta| Q[POST /checkout/orders/{id}/stripe-checkout-session]
    O -->|Misto| R[POST con importoCreditoRichiesto]
    Q --> S[Redirect a Stripe]
    R --> S
```

### Scelta Metodo

L'interfaccia mostra tre opzioni radio:

```html
<input type="radio" name="payment-method" value="carta">   <!-- Sempre attivo -->
<input type="radio" name="payment-method" value="credito"> <!-- Solo se saldo >= totale -->
<input type="radio" name="payment-method" value="misto">   <!-- Solo se saldo > 0 -->
```

Per il metodo **misto**, uno slider permette di scegliere quanto credito usare:

```javascript
const slider = document.getElementById('credit-slider');
slider.max = Math.min(saldo, Math.max(0, totale - 0.01)); // Almeno 0.01 su carta
slider.value = Math.min(saldo, Math.max(0, totale - 0.01)); // Default: massimo credito
```

### Gestione Back Button da Stripe

```javascript
window.addEventListener('pageshow', async (e) => {
    if (e.persisted) {
        // Utente tornato da Stripe con back button
        if (ordine?.stato === 'CheckoutInProgress' || ordine?.stato === 'Pending') {
            await API.cancelOrdine(orderId);
            showToast('Pagamento non completato. Ordine annullato.', 'warning');
            // Redirect a acquista.html
        }
    }
});
```

---

## Webhook Stripe

```mermaid
sequenceDiagram
    participant STR as Stripe
    participant B as Backend
    participant DB as Database

    STR->>B: POST /payments/stripe/webhook
    Note over STR,B: Con firma Stripe-Signature

    B->>B: Verifica firma webhook
    B->>B: Parsing evento

    alt checkout.session.completed
        B->>B: PagamentoService.HandleCheckoutCompletedAsync
        B->>DB: Ordine → Paid
        B->>DB: Emetti biglietti
        B->>DB: Invia email (best effort)
        B-->>STR: 200 OK

    else checkout.session.expired
        B->>B: PagamentoService.HandleCheckoutExpiredAsync
        B->>DB: Ordine → Expired
        B->>DB: Rilascia posti (se ancora in hold)
        B->>DB: Rilascia credito riservato
        B-->>STR: 200 OK

    else payment_intent.payment_failed
        B->>B: Logga errore
        B->>DB: Ordine.LastPaymentError = error
        B-->>STR: 200 OK

    else payment_intent.canceled
        B->>B: Gestione cancellazione
        B-->>STR: 200 OK
    end
```

---

## Servizi Backend Pagamento

| Servizio | Metodi Principali |
|----------|-------------------|
| `IPagamentoService` | `PayCreditoAsync`, `CreateCheckoutSessionAsync`, `GetCheckoutStatusAsync`, `ReconcileCheckoutSessionAsync`, `HandleStripeWebhookAsync`, `CancelPendingOrdineAsync` |
| `StripeGateway` | `CreateCheckoutSessionAsync`, `GetCheckoutSessionAsync`, `ConstructWebhookEvent`, `ParsePaymentIntent` |
| `CreditoService` | `GetSaldoAsync`, `AddCreditoAsync` (admin), `ReserveOrderCreditAsync`, `ReleaseReservedOrderCreditAsync` |
| `ICheckoutService` | `GetOrdineByIdAsync`, `MapToSummary` |

---

## Endpoint Backend Pagamento

| Metodo | Endpoint | Auth | Descrizione |
|--------|----------|------|-------------|
| `POST` | `/checkout/orders/{orderId}/pay` | Authenticated | Paga ordine (credito/ticket) |
| `POST` | `/checkout/orders/{orderId}/stripe-checkout-session` | Authenticated | Crea sessione Stripe Checkout |
| `GET` | `/checkout/orders/{orderId}/checkout-status` | Authenticated | Stato sessione Stripe |
| `POST` | `/checkout/orders/{orderId}/reconcile-checkout-session` | Authenticated | Riconcilia ordine dopo Stripe |
| `POST` | `/payments/stripe/webhook` | AllowAnonymous | Webhook Stripe (verificato) |
| `GET` | `/credito/me` | Authenticated | Saldo e movimenti credito |
| `POST` | `/admin/credito/ricarica` | AdminOnly | Ricarica credito admin |
| `POST` | `/admin/credito/crea-ricarica-stripe` | Authenticated | Crea sessione Stripe per topup |
| `GET` | `/config/frontend` | AllowAnonymous | Config pubblica (Stripe key) |

---

## Sicurezza Pagamenti

| Aspetto | Implementazione |
|---------|----------------|
| **Idempotenza** | `IdempotencyKey` su tutta la pipeline pagamento |
| **Source of Truth** | Backend, non il client. Il redirect di ritorno da Stripe non è considerato prova |
| **Doppio addebito** | Prevenuto da stato `Paid` e idempotenza |
| **Webhook** | Firmati con `STRIPE_WEBHOOK_SECRET`, elaborati in modo idempotente |
| **Credito** | Riservato al momento della sessione Stripe, addebitato solo a pagamento confermato |
| **Scadenza** | Ordini `CheckoutInProgress` scadono automaticamente, credito rilasciato |
| **Concorrenza** | Lock d'ordine impedisce pagamenti concorrenti |
