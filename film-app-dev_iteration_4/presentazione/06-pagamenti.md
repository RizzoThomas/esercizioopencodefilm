# Pagamenti

## Panoramica

CineBase supporta tre modalità di pagamento: solo credito interno, solo carta tramite Stripe Checkout hosted, o pagamento misto che combina credito e carta.

---

## Tabella Comparativa Metodi di Pagamento

| Caratteristica | Solo Credito | Solo Carta | Misto (Credito + Carta) |
|----------------|-------------|------------|------------------------|
| Richiede Stripe | No | Sì | Sì (solo per la parte carta) |
| Redirect a Stripe | No | Sì | Sì |
| Tempo di completamento | Immediato | 1-3 minuti | 1-3 minuti |
| Commissioni Stripe | Nessuna | Sì | Solo sulla quota carta |
| Addebito immediato | Sì | No (attesa webhook) | No (attesa webhook) |
| Rischio doppio pagamento | Nessuno | Gestito da idempotenza | Gestito da idempotenza |
| Rimborsabile | Sì (admin) | Sì (tramite Stripe) | Sì (credito + Stripe) |

---

## Architettura dei Pagamenti

```mermaid
graph TB
    subgraph "Frontend (pagamento.html)"
        UI[Scelta metodo]
        CRED[Pulsante: Paga con credito]
        CARD[Pulsante: Paga con carta]
        MIX[Slider credito + Paga resto]
    end

    subgraph "Backend Pagamenti"
        PS[PagamentoService]
        SG[StripeGateway]
        CS[CreditoService]
        WH[Webhook Handler]
    end

    subgraph "Stripe"
        CHK[Checkout Session]
        PI[Payment Intent]
        WH_EVT[Webhook Events]
    end

    subgraph "Database"
        ORD[Ordine]
        CRD[CreditoResiduo]
        MOV[MovimentoCredito]
    end

    UI --> CRED
    UI --> CARD
    UI --> MIX

    CRED --> PS
    PS --> CS
    CS --> CRD
    CS --> MOV
    PS --> ORD

    CARD --> SG
    MIX --> CS
    MIX --> SG
    CS -->|Riserva credito| CRD
    SG -->|Crea| CHK
    CHK -->|Redirect| UI

    WH -->|checkout.session.completed| PS
    WH -->|checkout.session.expired| PS
    PS -->|Finalizza| ORD
    PS -->|Addebita credito| CS
    PS -->|Emetti biglietti| ORD
```

---

## Flusso Pagamento Completo

```mermaid
sequenceDiagram
    participant U as Utente
    participant F as Frontend
    participant B as Backend
    participant S as Stripe
    participant DB as Database

    U->>F: Apre pagamento.html?orderId=X
    F->>B: GET /checkout/orders/{orderId}
    F->>B: GET /credito/me
    F->>B: GET /config/frontend

    F->>F: Render riepilogo ordine
    F->>F: Mostra opzioni pagamento

    U->>F: Seleziona metodo
    
    Alt Metodo: Solo Credito
        F->>B: POST /checkout/orders/{id}/pay (credito)
        B->>B: Verifica saldo sufficiente
        B->>DB: Addebita credito, stato→Paid
        B->>DB: Emetti biglietti
        B->>DB: Tenta invio email (best effort)
        B-->>F: { success: true }
        F->>U: Redirect a esito.html?success=true

    Else Metodo: Solo Carta
        F->>B: POST /checkout/orders/{id}/stripe-checkout-session
        B->>DB: Ordine→CheckoutInProgress
        B->>S: CheckoutSession.create(params)
        S-->>B: { sessionId, url }
        B-->>F: { stripeCheckoutUrl }
        F->>U: Redirect a checkout.stripe.com
        U->>S: Inserisce dati carta
        S-->>U: Redirect a esito.html?success=true
        S->>B: Webhook: checkout.session.completed
        B->>B: Finalizza ordine, biglietti, email
        B-->>S: 200 OK

    Else Metodo: Misto (Credito + Carta)
        F->>B: Slider: importoCreditoRichiesto
        F->>B: POST /checkout/orders/{id}/stripe-checkout-session (con credito)
        B->>DB: Riserva importoCreditoRichiesto
        B->>S: CheckoutSession.create(importoResiduo)
        S-->>B: { url }
        B-->>F: { stripeCheckoutUrl }
        F->>U: Redirect a Stripe
        U->>S: Paga importo residuo con carta
        S->>B: Webhook: checkout.session.completed
        B->>DB: Addebita credito riservato + finalizza
    End

    U->>F: esito-acquisto.html
    F->>B: Polling ogni 3s fino a stato Paid
    B-->>F: Stato → Paid
    F->>U: Mostra conferma + download PDF
```

---

## Tabella Webhook Stripe Gestiti

| Evento Stripe | Azione Backend | Stato Ordine Finale |
|---------------|----------------|---------------------|
| `checkout.session.completed` | Finalizza ordine, addebita credito riservato, emetti biglietti, invia email | Paid |
| `checkout.session.expired` | Rilascia posti, ripristina credito riservato | Expired |
| `payment_intent.payment_failed` | Logga errore su LastPaymentError | CheckoutInProgress (se hosted) o Failed |
| `payment_intent.canceled` | Logga cancellazione | CheckoutInProgress o Cancelled |

---

## Gestione Transizioni di Stato Ordine

```mermaid
stateDiagram-v2
    [*] --> Pending: Ordine creato da hold
    
    Pending --> CheckoutInProgress: Sessione Stripe creata
    Pending --> Paid: Pagamento solo credito
    Pending --> Cancelled: Utente annulla
    
    CheckoutInProgress --> Paid: Webhook completed + riconciliazione
    CheckoutInProgress --> Cancelled: Utente annulla
    CheckoutInProgress --> Expired: Sessione Stripe scaduta
    
    Paid --> [*]: Biglietti emessi
    
    note right of CheckoutInProgress
        Pulizia automatica:
        - ExpiredHoldCleanupService
        - Rilascio posti
        - Ripristino credito riservato
    end note
```

---

## Endpoint Backend Pagamento

| Metodo | Endpoint | Auth | Descrizione |
|--------|----------|------|-------------|
| POST | `/checkout/orders/{orderId}/pay` | Authenticated | Paga ordine con credito |
| POST | `/checkout/orders/{orderId}/stripe-checkout-session` | Authenticated | Crea sessione Stripe Checkout |
| GET | `/checkout/orders/{orderId}/checkout-status` | Authenticated | Stato sessione Stripe |
| POST | `/checkout/orders/{orderId}/reconcile-checkout-session` | Authenticated | Riconcilia ordine dopo Stripe |
| POST | `/payments/stripe/webhook` | AllowAnonymous | Webhook Stripe (firmato) |
| GET | `/credito/me` | Authenticated | Saldo e movimenti credito |
| GET | `/config/frontend` | AllowAnonymous | Publishable key Stripe |

---

## Tabella Sicurezza Pagamenti

| Rischio | Mitigazione |
|---------|-------------|
| Doppio pagamento | `IdempotencyKey` su tutta la pipeline, stato `Paid` bloccante |
| Redirect di ritorno non affidabile | Backend come source of truth, webhook come conferma principale |
| Sessione Stripe abbandonata | `ExpiredHoldCleanupService` pulisce ordini CheckoutInProgress scaduti |
| Credito non addebitato | Riservato al momento della creazione sessione, addebitato solo a webhook completed |
| Concorrenza pagamenti | Lock d'ordine impedisce pagamenti concorrenti |
| Webhook duplicati | Idempotenza sull'evento Stripe (event ID) |
| Back button da Stripe | Riconciliazione automatica, se ancora CheckoutInProgress → cancella ordine |
