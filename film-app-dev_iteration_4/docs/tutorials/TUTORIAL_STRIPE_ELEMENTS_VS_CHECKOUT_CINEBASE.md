# Tutorial Stripe Elements vs Stripe Checkout in CineBase

Autore: OpenCode

## Obiettivo del documento

Questo documento descrive in modo dettagliato il flusso di pagamento attuale di CineBase basato su `Stripe Elements` e il flusso target basato su `Stripe Checkout` hosted.

L'obiettivo è chiarire:

- come funziona oggi il pagamento con carta embedded nella pagina `pagamento.html`
- perché un passaggio a `Stripe Checkout` può migliorare la fiducia percepita dell'utente finale
- cosa deve essere implementato nel backend e nel frontend per supportare in modo solido:
  - pagamento solo credito piattaforma
  - pagamento solo carta tramite pagina Stripe hosted
  - pagamento misto credito piattaforma + carta Stripe
- come gestire correttamente lock posti, webhook, riconciliazione stato e rilascio del credito riservato

Il documento è scritto come riferimento tecnico-operativo per la futura `FASE 11.1` del piano di lavoro.

---

## 1. Contesto di partenza

Nel flusso attuale, l'utente:

1. seleziona i posti in `acquista.html`
2. arriva in `pagamento.html`
3. inserisce i dati carta dentro il sito tramite `Stripe Elements`
4. il frontend conferma il pagamento con Stripe
5. il backend finalizza l'ordine, genera i biglietti e avvia l'invio email

Dal punto di vista tecnico, questo approccio è corretto e sicuro, perché i dati carta non transitano nel backend applicativo. Tuttavia, dal punto di vista della fiducia percepita, un utente può sentirsi più tranquillo vedendo una pagina di pagamento hosted da Stripe.

Per questa ragione, la variante futura prevede l'uso di `Stripe Checkout`, cioè una pagina di pagamento ospitata da Stripe, con ritorno finale su CineBase.

---

## 2. Flusso attuale: Stripe Elements embedded

## 2.1 Sequenza logica

Nel modello attuale, il flusso funziona così:

1. il frontend crea o recupera un `Ordine` in stato `Pending`
2. il backend calcola totale, quota credito e quota carta
3. se è presente una quota carta, il backend prepara il `PaymentIntent`
4. il frontend conferma il pagamento con `stripe.confirmCardPayment(...)`
5. una volta ottenuto esito positivo da Stripe, il frontend richiama di nuovo il backend
6. il backend verifica il pagamento e finalizza l'ordine
7. il backend converte i posti da `Hold` a `Sold`, emette i biglietti, genera PDF e invia email

## 2.2 Punti di forza

- esperienza utente integrata nella pagina del sito
- nessun redirect esterno durante il pagamento
- ottimo controllo del layout e dell'interfaccia

## 2.3 Limiti percepiti

- l'utente inserisce i dati carta “dentro” il sito e questo può ridurre la fiducia, specialmente se il brand non è ancora conosciuto
- il frontend deve partecipare in modo esplicito alla finalizzazione post-Stripe
- il flusso embedded è meno adatto quando si vuole comunicare chiaramente che il pagamento è gestito da Stripe su pagina riconoscibile

---

## 3. Snippet di riferimento del flusso attuale con Stripe Elements

Quello che segue è uno snippet didattico completo e semplificato, coerente con l'architettura attuale.

### Backend .NET - finalizzazione pagamento con `PaymentIntent`

```csharp
public sealed class PayOrdineRequest
{
    public decimal ImportoCreditoRichiesto { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? IdempotencyKey { get; set; }
}

public sealed class OrdinePaymentResultDto
{
    public int OrdineId { get; set; }
    public string Stato { get; set; } = string.Empty;
    public bool RequiresCardPayment { get; set; }
    public string? ClientSecret { get; set; }
    public string? PaymentIntentId { get; set; }
}

public async Task<OrdinePaymentResultDto> PayOrdineAsync(
    int ordineId,
    int userId,
    PayOrdineRequest request,
    CancellationToken cancellationToken)
{
    var ordine = await _db.Ordini
        .Include(o => o.Show)
        .FirstOrDefaultAsync(o => o.Id == ordineId && o.UserId == userId, cancellationToken);

    if (ordine is null)
    {
        throw new InvalidOperationException("Ordine non trovato.");
    }

    if (ordine.Stato == OrdineState.Paid)
    {
        return new OrdinePaymentResultDto
        {
            OrdineId = ordine.Id,
            Stato = "paid",
            RequiresCardPayment = false,
            PaymentIntentId = ordine.StripePaymentIntentId
        };
    }

    var totale = ordine.TotaleLordo;
    var importoCredito = Math.Min(request.ImportoCreditoRichiesto, totale);
    var importoCarta = totale - importoCredito;

    if (importoCarta == 0)
    {
        await FinalizeOrdineAsync(ordine, importoCredito, null, cancellationToken);

        return new OrdinePaymentResultDto
        {
            OrdineId = ordine.Id,
            Stato = "paid",
            RequiresCardPayment = false
        };
    }

    if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
    {
        var paymentIntent = await _stripeGateway.CreatePaymentIntentAsync(new CreateStripePaymentIntentRequest
        {
            Amount = importoCarta,
            Metadata = new Dictionary<string, string>
            {
                ["ordineId"] = ordine.Id.ToString(),
                ["userId"] = userId.ToString()
            }
        }, cancellationToken);

        ordine.ImportoCredito = importoCredito;
        ordine.ImportoCarta = importoCarta;
        ordine.StripePaymentIntentId = paymentIntent.PaymentIntentId;
        await _db.SaveChangesAsync(cancellationToken);

        return new OrdinePaymentResultDto
        {
            OrdineId = ordine.Id,
            Stato = "awaiting_card_confirmation",
            RequiresCardPayment = true,
            ClientSecret = paymentIntent.ClientSecret,
            PaymentIntentId = paymentIntent.PaymentIntentId
        };
    }

    var paymentStatus = await _stripeGateway.GetPaymentIntentStatusAsync(request.PaymentIntentId, cancellationToken);

    if (!paymentStatus.IsSucceeded)
    {
        throw new InvalidOperationException("Il pagamento Stripe non risulta completato.");
    }

    await FinalizeOrdineAsync(ordine, importoCredito, request.PaymentIntentId, cancellationToken);

    return new OrdinePaymentResultDto
    {
        OrdineId = ordine.Id,
        Stato = "paid",
        RequiresCardPayment = false,
        PaymentIntentId = request.PaymentIntentId
    };
}
```

### Frontend - conferma con `Stripe Elements`

```javascript
async function submitWithStripeElements(orderId, importoCreditoRichiesto) {
  const bootstrap = await API.payOrdine(orderId, {
    importoCreditoRichiesto,
    idempotencyKey: crypto.randomUUID()
  });

  if (!bootstrap.requiresCardPayment) {
    window.location.assign(`/esito-acquisto.html?orderId=${orderId}`);
    return;
  }

  const stripe = Stripe(window.RUNTIME_CONFIG.stripePublishableKey);
  const card = elements.getElement('card');

  const result = await stripe.confirmCardPayment(bootstrap.clientSecret, {
    payment_method: {
      card
    }
  });

  if (result.error) {
    throw new Error(result.error.message || 'Pagamento non riuscito.');
  }

  await API.payOrdine(orderId, {
    importoCreditoRichiesto,
    paymentIntentId: result.paymentIntent.id,
    idempotencyKey: crypto.randomUUID()
  });

  window.location.assign(`/esito-acquisto.html?orderId=${orderId}`);
}
```

Questo approccio funziona, ma il browser resta parte attiva della conferma di business dopo il pagamento carta.

---

## 4. Flusso target: Stripe Checkout hosted

## 4.1 Obiettivo architetturale

Nel modello target, CineBase non raccoglie più il dato carta dentro `pagamento.html`.

Il frontend:

- mostra il riepilogo ordine
- permette di scegliere solo credito, solo carta o pagamento misto
- chiama il backend per avviare il checkout
- se serve Stripe, effettua un redirect verso una `Checkout Session` hosted

Il backend:

- calcola sempre il totale reale
- decide se serve Stripe oppure no
- riserva l'eventuale quota credito
- crea la `Checkout Session` soltanto per la quota carta residua
- finalizza l'ordine solo quando Stripe conferma davvero il pagamento

## 4.2 Principio fondamentale

Nel flusso hosted, il ritorno browser da Stripe non è una prova sufficiente di pagamento riuscito.

La prova forte è data da:

- webhook Stripe verificato lato backend
- riconciliazione esplicita della sessione se necessario

---

## 5. Requisiti business del nuovo flusso

Il nuovo modello deve supportare correttamente tre scenari.

## 5.1 Solo credito piattaforma

Se il saldo utente è sufficiente:

- il backend non crea nessuna sessione Stripe
- l'ordine viene finalizzato direttamente
- i posti diventano `Sold`
- i biglietti vengono emessi
- PDF ed email vengono avviati come nel flusso standard

Se il saldo non è sufficiente:

- il backend rifiuta il pagamento solo credito
- il frontend invita a usare il pagamento misto o solo carta

## 5.2 Solo carta

Se l'utente non usa credito:

- il backend crea una `Checkout Session`
- il frontend reindirizza l'utente a Stripe
- Stripe gestisce carta, 3DS e conferma
- il backend riceve webhook e finalizza l'ordine

## 5.3 Pagamento misto credito + carta

Se l'utente decide di usare parte del credito:

- il backend calcola la quota credito accettabile
- il backend riserva quella quota
- il backend crea la `Checkout Session` solo per il residuo carta
- quando Stripe conferma il pagamento, il backend consolida l'addebito credito e finalizza l'ordine
- se il pagamento viene annullato o scade, il backend rilascia i posti e restituisce la quota credito riservata

---

## 6. Problema chiave: i posti durante il redirect verso Stripe

Con `Stripe Elements`, il frontend può fare keep-alive del hold mentre l'utente resta su CineBase.

Con `Stripe Checkout`, durante il redirect hosted questo non è più affidabile.

Per questo motivo il nuovo modello deve introdurre un vero lock d'ordine temporaneo lato backend.

## 6.1 Strategia consigliata

Quando l'utente avvia il checkout hosted:

1. il backend verifica che l'ordine sia ancora valido
2. il backend converte il normale hold in un lock associato all'ordine
3. il backend imposta `CheckoutExpiresAtUtc`
4. fino a scadenza o finalizzazione, i posti non vengono rimessi in disponibilità
5. se il checkout va a buon fine, i posti diventano `Sold`
6. se il checkout viene annullato, fallisce o scade, i posti vengono rilasciati

Questo approccio evita che i posti si liberino mentre l'utente sta completando il pagamento sulla pagina Stripe.

---

## 7. Estensioni consigliate al modello `Ordine`

Per supportare il flusso hosted in modo robusto, è consigliabile aggiungere o formalizzare questi campi:

```text
Ordine
- StripeCheckoutSessionId string? max 120
- CheckoutExpiresAtUtc datetime?
- CreditoRiservato decimal(10,2) required default 0
- CheckoutCompletedAtUtc datetime?
- LastPaymentError string? max 1000
```

È inoltre utile chiarire gli stati dominio. Una possibile evoluzione è:

```text
OrdineState
- Pending
- CheckoutInProgress
- Paid
- Failed
- Cancelled
- Expired
```

Se non si vuole introdurre subito un nuovo enum value, si può temporaneamente mantenere `Pending` con campi di checkout più espliciti, ma il modello con `CheckoutInProgress` è più leggibile.

---

## 8. Backend target: endpoint principali

Il flusso target richiede tipicamente questi endpoint.

## 8.1 Creazione sessione hosted

```text
POST /checkout/orders/{orderId}/stripe-checkout-session
```

Scopo:

- validare lo stato ordine
- calcolare totale reale
- riservare credito se richiesto
- creare la `Checkout Session` solo se la quota carta è maggiore di zero
- ritornare URL di redirect o esito diretto per il solo credito

## 8.2 Stato checkout

```text
GET /checkout/orders/{orderId}/checkout-status
```

Scopo:

- permettere a `esito-acquisto.html` di interrogare il backend dopo il ritorno da Stripe
- restituire stati come:
  - `paid`
  - `processing`
  - `cancelled`
  - `expired`
  - `failed`

## 8.3 Cancel esplicito ordine hosted

```text
POST /checkout/orders/{orderId}/cancel
```

Scopo:

- annullare un ordine non pagato
- restituire credito riservato
- rilasciare posti

---

## 9. Backend target: esempio completo di creazione `Checkout Session`

Il seguente esempio mostra una possibile implementazione didattica lato backend in .NET.

```csharp
public sealed class CreateHostedCheckoutRequestDto
{
    public decimal RequestedCredito { get; set; }
    public string? IdempotencyKey { get; set; }
}

public sealed class HostedCheckoutResponseDto
{
    public int OrdineId { get; set; }
    public string Flow { get; set; } = string.Empty;
    public string Stato { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
}

public async Task<HostedCheckoutResponseDto> StartHostedCheckoutAsync(
    int ordineId,
    int userId,
    CreateHostedCheckoutRequestDto request,
    CancellationToken cancellationToken)
{
    var ordine = await _db.Ordini
        .Include(o => o.User)
        .Include(o => o.Show)
        .FirstOrDefaultAsync(o => o.Id == ordineId && o.UserId == userId, cancellationToken);

    if (ordine is null)
    {
        throw new InvalidOperationException("Ordine non trovato.");
    }

    if (ordine.Stato == OrdineState.Paid)
    {
        return new HostedCheckoutResponseDto
        {
            OrdineId = ordine.Id,
            Flow = "already_paid",
            Stato = "paid"
        };
    }

    var totale = ordine.TotaleLordo;
    var creditoDisponibile = ordine.User.CreditoResiduo;
    var creditoDaUsare = Math.Min(request.RequestedCredito, Math.Min(creditoDisponibile, totale));
    var residuoCarta = totale - creditoDaUsare;

    if (residuoCarta == 0)
    {
        await FinalizeOrdineWithCreditoOnlyAsync(ordine, creditoDaUsare, cancellationToken);

        return new HostedCheckoutResponseDto
        {
            OrdineId = ordine.Id,
            Flow = "credito_only",
            Stato = "paid"
        };
    }

    ordine.ImportoCredito = creditoDaUsare;
    ordine.ImportoCarta = residuoCarta;
    ordine.CreditoRiservato = creditoDaUsare;
    ordine.CheckoutExpiresAtUtc = DateTime.UtcNow.AddMinutes(15);
    ordine.Stato = OrdineState.Pending;

    var session = await _stripeHostedCheckoutGateway.CreateCheckoutSessionAsync(new CreateStripeCheckoutSessionRequest
    {
        Amount = residuoCarta,
        SuccessUrl = $"{_frontendBaseUrl}/esito-acquisto.html?orderId={ordine.Id}&checkout=success",
        CancelUrl = $"{_frontendBaseUrl}/esito-acquisto.html?orderId={ordine.Id}&checkout=cancel",
        Metadata = new Dictionary<string, string>
        {
            ["ordineId"] = ordine.Id.ToString(),
            ["userId"] = userId.ToString()
        },
        IdempotencyKey = request.IdempotencyKey
    }, cancellationToken);

    ordine.StripeCheckoutSessionId = session.SessionId;
    await _db.SaveChangesAsync(cancellationToken);

    return new HostedCheckoutResponseDto
    {
        OrdineId = ordine.Id,
        Flow = "stripe_checkout",
        Stato = "checkout_in_progress",
        CheckoutUrl = session.Url
    };
}
```

### Considerazioni importanti

- se `residuoCarta == 0`, il backend non deve parlare con Stripe
- il credito non va considerato definitivamente speso al momento della semplice creazione sessione: va prima riservato, poi consolidato alla finalizzazione
- `CheckoutExpiresAtUtc` deve essere usato dal cleanup backend per liberare posti e restituire credito riservato se la sessione non si chiude correttamente

---

## 10. Gateway backend di esempio per Stripe Checkout

```csharp
using Stripe.Checkout;

public sealed class CreateStripeCheckoutSessionRequest
{
    public decimal Amount { get; set; }
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public sealed class StripeCheckoutSessionResult
{
    public string SessionId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public interface IStripeHostedCheckoutGateway
{
    Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateStripeCheckoutSessionRequest request,
        CancellationToken cancellationToken);
}

public sealed class StripeHostedCheckoutGateway : IStripeHostedCheckoutGateway
{
    public async Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateStripeCheckoutSessionRequest request,
        CancellationToken cancellationToken)
    {
        var service = new SessionService();

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmountDecimal = request.Amount * 100m,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Ordine CineBase"
                        }
                    }
                }
            },
            Metadata = request.Metadata
        };

        var stripeRequestOptions = new Stripe.RequestOptions
        {
            IdempotencyKey = request.IdempotencyKey
        };

        var session = await service.CreateAsync(options, stripeRequestOptions, cancellationToken);

        return new StripeCheckoutSessionResult
        {
            SessionId = session.Id,
            Url = session.Url ?? throw new InvalidOperationException("Stripe non ha restituito una URL di checkout.")
        };
    }
}
```

---

## 11. Webhook target: finalizzazione ordine hosted

Nel flusso hosted, il webhook deve diventare la fonte primaria di conferma del pagamento.

### Esempio backend .NET

```csharp
app.MapPost("/payments/stripe/webhook", async (
    HttpRequest request,
    IPagamentoService pagamentoService,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    var payload = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);
    var signature = request.Headers["Stripe-Signature"];
    var secret = configuration["STRIPE_WEBHOOK_SECRET"];

    var stripeEvent = Stripe.EventUtility.ConstructEvent(payload, signature, secret);

    switch (stripeEvent.Type)
    {
        case "checkout.session.completed":
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session is not null)
            {
                await pagamentoService.HandleCheckoutSessionCompletedAsync(session.Id, cancellationToken);
            }
            break;
        }

        case "checkout.session.expired":
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session is not null)
            {
                await pagamentoService.HandleCheckoutSessionExpiredAsync(session.Id, cancellationToken);
            }
            break;
        }

        case "payment_intent.payment_failed":
        {
            var paymentIntent = stripeEvent.Data.Object as Stripe.PaymentIntent;
            if (paymentIntent is not null)
            {
                await pagamentoService.HandleHostedPaymentFailedAsync(paymentIntent.Id, cancellationToken);
            }
            break;
        }
    }

    return Results.Ok();
});
```

### Esempio service di finalizzazione idempotente

```csharp
public async Task HandleCheckoutSessionCompletedAsync(string sessionId, CancellationToken cancellationToken)
{
    var ordine = await _db.Ordini
        .Include(o => o.User)
        .FirstOrDefaultAsync(o => o.StripeCheckoutSessionId == sessionId, cancellationToken);

    if (ordine is null)
    {
        throw new InvalidOperationException("Ordine associato alla sessione Stripe non trovato.");
    }

    if (ordine.Stato == OrdineState.Paid)
    {
        return;
    }

    await FinalizeOrdineHostedAsync(ordine, cancellationToken);
}
```

La finalizzazione deve essere replay-safe. Se Stripe reinvia lo stesso evento, il backend non deve emettere biglietti duplicati né addebitare due volte il credito.

---

## 12. Frontend target: avvio checkout hosted

Il frontend non raccoglie più la carta. Si limita a decidere il tipo di pagamento e a delegare al backend.

### Esempio frontend completo

```javascript
async function startHostedCheckout(orderId, requestedCredito) {
  const response = await API.createStripeCheckoutSession(orderId, {
    requestedCredito,
    idempotencyKey: crypto.randomUUID()
  });

  if (response.flow === 'credito_only') {
    window.location.assign(`/esito-acquisto.html?orderId=${orderId}`);
    return;
  }

  if (response.flow !== 'stripe_checkout' || !response.checkoutUrl) {
    throw new Error('Risposta di checkout non valida.');
  }

  window.location.assign(response.checkoutUrl);
}
```

In questo modello il frontend non decide se il pagamento è riuscito. Si limita a:

- avviare il flusso
- reindirizzare l'utente a Stripe
- interrogare il backend quando l'utente torna su CineBase

---

## 13. Frontend target: pagina `esito-acquisto.html` con polling stato backend

### Esempio frontend completo

```javascript
async function waitForHostedCheckoutResult(orderId) {
  const statusEl = document.querySelector('[data-checkout-status]');
  const maxAttempts = 20;

  for (let attempt = 1; attempt <= maxAttempts; attempt += 1) {
    const status = await API.getCheckoutStatus(orderId);

    if (status.state === 'paid') {
      statusEl.textContent = 'Pagamento completato. I biglietti sono stati emessi correttamente.';
      await loadOrderDetails(orderId);
      return;
    }

    if (status.state === 'cancelled') {
      statusEl.textContent = 'Pagamento annullato. I posti sono stati rilasciati.';
      return;
    }

    if (status.state === 'expired') {
      statusEl.textContent = 'Sessione scaduta. È necessario ripetere il checkout.';
      return;
    }

    if (status.state === 'failed') {
      statusEl.textContent = 'Il pagamento non è andato a buon fine.';
      return;
    }

    statusEl.textContent = 'Pagamento in verifica. Attendere qualche secondo...';
    await new Promise(resolve => setTimeout(resolve, 1500));
  }

  statusEl.textContent = 'Verifica ancora in corso. Aggiornare la pagina tra qualche secondo.';
}
```

Questo polling serve a coprire il caso reale in cui:

- l'utente torna dal redirect `success_url`
- il webhook Stripe non è ancora arrivato o non è ancora stato elaborato

---

## 14. Gestione robusta del credito piattaforma

## 14.1 Regola fondamentale

Nel pagamento misto il credito non deve essere “perso” se la parte Stripe non va a buon fine.

Per questo motivo conviene distinguere tre momenti:

1. verifica saldo disponibile
2. riserva credito
3. consolidamento o rilascio credito

## 14.2 Flusso consigliato

### Riserva credito

Quando l'utente avvia il checkout hosted:

- il backend verifica il saldo attuale
- decide quanto credito può usare davvero
- scrive `Ordine.CreditoRiservato`
- il saldo non viene considerato definitivamente consumato dal punto di vista business finché Stripe non conferma il pagamento della quota carta

### Consolidamento credito

Quando il webhook conferma `checkout.session.completed`:

- il backend crea il movimento credito definitivo
- aggiorna `CreditoResiduo`
- marca l'ordine come `Paid`

### Rilascio credito

Se il pagamento hosted viene annullato, fallisce o scade:

- il backend azzera `CreditoRiservato`
- non crea il movimento definitivo di addebito oppure crea un movimento tecnico di restore, secondo il modello adottato
- rilascia i posti

---

## 15. Differenze architetturali principali tra i due approcci

| Tema | Stripe Elements | Stripe Checkout hosted |
| --- | --- | --- |
| Raccolta carta | dentro `pagamento.html` | su pagina Stripe |
| Fiducia percepita | media | più alta per molti utenti |
| Controllo UX | massimo | più limitato |
| Redirect esterno | no | sì |
| Keep-alive hold browser | facile | non sufficiente |
| Webhook come source of truth | consigliato | sostanzialmente obbligatorio |
| Solo credito senza Stripe | sì | sì |
| Misto credito + carta | sì | sì, con riserva credito |

---

## 16. Strategia di migrazione consigliata per CineBase

Per minimizzare i rischi, conviene introdurre il nuovo flusso con rollout graduale.

## 16.1 Step consigliati

1. mantenere il supporto backend attuale a `PaymentIntent`
2. aggiungere in parallelo il supporto a `Checkout Session`
3. introdurre una feature flag come `STRIPE_PAYMENT_FLOW=elements|checkout`
4. migrare `pagamento.html` al nuovo flusso solo quando webhook, cleanup e riconciliazione sono stabili
5. mantenere `esito-acquisto.html` compatibile con entrambi i flussi finché la transizione non è chiusa

## 16.2 Vantaggi di questo approccio

- rollback semplice in caso di regressione
- possibilità di confrontare i due modelli in ambiente locale o staging
- minore rischio di rompere il checkout già funzionante

---

## 17. Test automatici raccomandati per Stripe Checkout

La migrazione a hosted checkout richiede una suite di test più esplicita.

## 17.1 Backend

- creazione sessione solo carta
- creazione sessione misto con quota carta corretta
- nessuna sessione Stripe per pagamento solo credito sufficiente
- errore per solo credito insufficiente
- webhook `checkout.session.completed` che finalizza una sola volta
- webhook duplicato replay-safe
- `checkout.session.expired` che rilascia posti e credito riservato
- `payment_intent.payment_failed` che porta ordine a stato coerente

## 17.2 Frontend

- redirect a Stripe soltanto quando il backend restituisce `checkoutUrl`
- redirect diretto a `esito-acquisto.html` in caso solo credito
- polling su `esito-acquisto.html` che gestisce `processing`, `paid`, `cancelled`, `expired`, `failed`

---

## 18. Conclusione operativa

Dal punto di vista tecnico, il flusso attuale con `Stripe Elements` è corretto e sicuro. Tuttavia, se CineBase vuole migliorare la fiducia percepita dell'utente finale, il passaggio a `Stripe Checkout` hosted è sensato.

Affinché la migrazione sia davvero robusta, non basta sostituire il widget frontend. È necessario implementare in modo esplicito:

- lock d'ordine temporaneo lato backend durante il redirect hosted
- webhook Stripe come fonte primaria di verità
- polling o riconciliazione lato `esito-acquisto.html`
- gestione corretta del solo credito senza toccare Stripe
- gestione del pagamento misto con quota credito riservata e rilascio in caso di fallimento o scadenza

Per questa ragione, la migrazione merita una fase dedicata nel piano di lavoro, identificata come `FASE 11.1`.
