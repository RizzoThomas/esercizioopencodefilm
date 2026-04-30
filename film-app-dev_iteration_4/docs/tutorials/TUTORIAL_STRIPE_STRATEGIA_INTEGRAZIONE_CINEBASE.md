# Strategia di integrazione Stripe per CineBase

**Autore:** OpenCode  
**Progetto di riferimento:** CineBase  
**Ambito:** strategia tecnica e didattica per integrare Stripe in Iterazione 4  

---

## Indice

1. [Obiettivo del documento](#1-obiettivo-del-documento)
2. [Direzione ufficiale del progetto](#2-direzione-ufficiale-del-progetto)
3. [Vincoli e principi architetturali](#3-vincoli-e-principi-architetturali)
4. [Perché CineBase sceglie Stripe Checkout hosted](#4-perche-cinebase-sceglie-stripe-checkout-hosted)
5. [Strategia raccomandata per il prodotto](#5-strategia-raccomandata-per-il-prodotto)
6. [Flussi supportati: solo credito, solo carta, pagamento misto](#6-flussi-supportati-solo-credito-solo-carta-pagamento-misto)
7. [Ruolo dei webhook e della riconciliazione](#7-ruolo-dei-webhook-e-della-riconciliazione)
8. [Decisione operativa su Stripe CLI](#8-decisione-operativa-su-stripe-cli)
9. [Configurazione guidata della dashboard Stripe per CineBase_Demo](#9-configurazione-guidata-della-dashboard-stripe-per-cinebase_demo)
10. [Configurazione ambienti e secrets](#10-configurazione-ambienti-e-secrets)
11. [Metadati, idempotenza e lock d'ordine](#11-metadati-idempotenza-e-lock-dordine)
12. [Piano incrementale di implementazione](#12-piano-incrementale-di-implementazione)
13. [Stripe Elements: approccio documentato ma non supportato](#13-stripe-elements-approccio-documentato-ma-non-supportato)
14. [Cosa non implementare subito](#14-cosa-non-implementare-subito)
15. [Checklist di conformità](#15-checklist-di-conformita)
16. [Procedura replicabile di collaudo locale](#16-procedura-replicabile-di-collaudo-locale)
17. [Conclusione operativa](#17-conclusione-operativa)

---

## 1. Obiettivo del documento

Questo documento definisce la strategia Stripe ufficiale per `CineBase` dopo l'introduzione della `FASE 11.1`, cioè la migrazione da `Stripe Elements` a `Stripe Checkout` hosted.

Lo scopo è fissare in modo chiaro:

- quale approccio è ufficialmente raccomandato per il prodotto
- quali vincoli architetturali non devono essere violati
- come devono funzionare i casi `solo credito`, `solo carta`, `misto`
- quale ruolo hanno webhook, riconciliazione backend e `Stripe CLI`
- perché `Stripe Elements` resta documentato ma non è più considerato il flusso supportato per la direzione del prodotto

---

## 2. Direzione ufficiale del progetto

La direzione ufficiale di `CineBase` è la seguente:

- per il pagamento carta si usa `Stripe Checkout` hosted
- il backend resta la sola source of truth per importi, stato ordine, stato posti, credito e biglietti
- il pagamento `solo credito` non deve chiamare Stripe se il saldo è sufficiente
- il pagamento `misto` deve riservare il credito e inviare a Stripe soltanto il residuo carta
- il redirect browser di ritorno da Stripe non vale come prova sufficiente di pagamento riuscito

Conclusione operativa:

- `Stripe Checkout` è il flusso raccomandato e supportato
- `Stripe Elements` non è il flusso target del prodotto

---

## 3. Vincoli e principi architetturali

Dal piano di lavoro e dalle decisioni già approvate emergono questi principi.

### 3.1 Stripe gestisce il pagamento esterno, CineBase gestisce il business

Stripe non deve diventare la source of truth dell'ordine applicativo.

La source of truth resta `CineBase` per:

- ordine
- posti venduti
- movimenti credito
- biglietti emessi
- PDF ed email associate all'ordine

### 3.2 Il backend resta autorevole

Il backend decide sempre:

- il totale reale da pagare
- la quota copribile da credito piattaforma
- la quota carta residua
- quando un ordine può essere considerato davvero pagato

### 3.3 L'idempotenza non è opzionale

La soluzione deve tollerare:

- retry frontend
- retry webhook
- doppio click utente
- refresh pagina
- ritorno tardivo del webhook dopo redirect browser già completato

### 3.4 Il lock posti deve sopravvivere al redirect hosted

Con `Stripe Checkout` l'utente esce temporaneamente dal sito. Per questo motivo il semplice keep-alive frontend non è sufficiente.

Serve quindi un lock d'ordine temporaneo lato backend, con scadenza e cleanup.

---

## 4. Perché CineBase sceglie Stripe Checkout hosted

La scelta non nasce da un problema di sicurezza tecnica di `Stripe Elements`, ma da un obiettivo di fiducia percepita verso l'utente finale.

Per un prodotto ancora poco conosciuto, una pagina hosted Stripe offre questi vantaggi:

- maggiore riconoscibilità del brand Stripe nel momento sensibile dell'inserimento carta
- minore diffidenza nel digitare i dati carta
- migliore chiarezza percettiva sul fatto che la carta è gestita da un provider di pagamento noto

Questa motivazione è perfettamente coerente con il contesto di `CineBase`.

---

## 5. Strategia raccomandata per il prodotto

La strategia raccomandata è un modello hosted, webhook-driven e backend-authoritative.

### 5.1 Modello raccomandato

1. il frontend prepara o recupera l'ordine
2. il backend ricalcola totale, quota credito e quota carta
3. se la quota carta è zero, il backend finalizza direttamente
4. se la quota carta è maggiore di zero, il backend crea una `Checkout Session`
5. il frontend reindirizza l'utente alla pagina Stripe hosted
6. Stripe gestisce il pagamento carta
7. il backend riceve il webhook verificato e finalizza l'ordine in modo idempotente
8. `esito-acquisto.html` interroga il backend finché lo stato non è coerente

### 5.2 Regole non derogabili

- nessun ordine viene marcato `Paid` dal frontend
- nessun addebito credito definitivo avviene prima della conferma reale del pagamento carta nel caso misto
- nessun posto viene rilasciato prematuramente mentre la sessione hosted è ancora valida

---

## 6. Flussi supportati: solo credito, solo carta, pagamento misto

## 6.1 Caso solo credito piattaforma

Flusso ufficiale:

1. il backend ricalcola il totale ordine
2. verifica il saldo disponibile
3. se il saldo è sufficiente, finalizza direttamente l'ordine
4. crea il movimento credito auditabile
5. converte i posti da `Hold` o lock ordine a `Sold`
6. emette i biglietti

In questo caso Stripe non deve entrare nel flusso.

## 6.2 Caso solo carta

Flusso ufficiale:

1. il backend ricalcola il totale carta dovuto
2. crea una `Checkout Session` Stripe
3. salva su ordine almeno `StripeCheckoutSessionId` e `CheckoutExpiresAtUtc`
4. il frontend reindirizza l'utente a Stripe
5. il backend riceve `checkout.session.completed`
6. il backend finalizza l'ordine in modo idempotente

## 6.3 Caso pagamento misto credito più carta

Flusso ufficiale:

1. il backend ricalcola il totale
2. verifica il saldo disponibile
3. determina la quota credito ammissibile
4. riserva quella quota su ordine come `CreditoRiservato`
5. crea una `Checkout Session` solo per il residuo carta
6. al webhook di successo consolida il credito e finalizza l'ordine
7. in caso di cancel, expire o failure rilascia posti e credito riservato

### 6.4 Nota fondamentale

Nel caso misto il credito non deve essere definitivamente consumato al momento dell'avvio della sessione hosted.

Deve prima essere riservato, poi consolidato o rilasciato.

---

## 7. Ruolo dei webhook e della riconciliazione

## 7.1 Webhook come fonte primaria di verità

Nel modello hosted di `CineBase`, il webhook ha un ruolo centrale:

- confermare il successo reale del checkout carta
- finalizzare l'ordine in modo idempotente
- coprire i casi in cui il redirect browser arrivi prima o senza stato consolidato

### 7.2 Eventi minimi raccomandati

- `checkout.session.completed`
- `checkout.session.expired`
- `payment_intent.payment_failed`

### 7.3 Riconciliazione backend

Poiché il redirect browser può arrivare prima del webhook, `CineBase` deve esporre almeno un endpoint di stato, per esempio:

```text
GET /checkout/orders/{orderId}/checkout-status
```

È inoltre raccomandato un endpoint di riconciliazione manuale della sessione hosted quando il webhook non è ancora stato elaborato.

### 7.4 Regola fondamentale sul `cancel_url`

Il ritorno su `cancel_url` non è prova sufficiente di annullamento finale del pagamento.

È soltanto un ritorno applicativo. Sarà il backend a stabilire se l'ordine deve:

- restare in `CheckoutInProgress`
- essere annullato
- scadere

---

## 8. Decisione operativa su Stripe CLI

Nel progetto `CineBase`, `Stripe CLI` viene usata come strumento locale per testare e debuggare i webhook.

### 8.1 Risposta breve

- non è una dipendenza runtime dell'applicazione
- è fortemente raccomandata per testare in locale il webhook hosted
- in deployment non viene usata, perché Stripe chiamerà direttamente l'endpoint pubblico

### 8.2 Cosa significa in pratica

Durante lo sviluppo locale:

- il backend crea la `Checkout Session`
- il frontend effettua il redirect alla pagina hosted
- `Stripe CLI` inoltra gli eventi webhook a `localhost`
- il backend verifica firma, eventi e idempotenza

---

## 9. Configurazione guidata della dashboard Stripe per CineBase_Demo

Questa sezione descrive, passo dopo passo, che cosa un operatore umano deve configurare nella dashboard Stripe per preparare l'ambiente `CineBase_Demo`.

### 9.1 Risultato finale atteso

Al termine della configurazione manuale, l'operatore dovrebbe avere:

- una `publishable key` di test `pk_test_...`
- una `secret key` di test `sk_test_...`
- il metodo carta disponibile in `test mode`
- una decisione chiara sul webhook:
  - locale con `Stripe CLI`
  - endpoint pubblico reale in dashboard per tunnel o deployment

### 9.2 Passo 1 - Verificare `test mode`

1. aprire `https://dashboard.stripe.com/`
2. effettuare l'accesso
3. verificare che la dashboard sia in `test mode`

### 9.3 Passo 2 - Recuperare le chiavi API

Percorso consigliato:

1. aprire `Developers`
2. aprire `API keys`
3. copiare la `Publishable key` di test `pk_test_...`
4. copiare la `Secret key` di test `sk_test_...`

### 9.4 Passo 3 - Verificare il metodo carta

1. aprire `Payment methods`
2. verificare che `Card` sia attivo in `test mode`

### 9.5 Passo 4 - Decidere il canale webhook

#### Scenario A - Sviluppo locale con Stripe CLI

- il backend espone `POST /payments/stripe/webhook`
- `Stripe CLI` inoltra gli eventi a `localhost`
- il backend usa il `whsec_...` restituito dalla CLI

#### Scenario B - Tunnel pubblico o deployment

1. aprire `Developers`
2. aprire `Webhooks`
3. aggiungere endpoint:

```text
https://<dominio-pubblico>/payments/stripe/webhook
```

4. selezionare almeno:
   - `checkout.session.completed`
   - `checkout.session.expired`
   - `payment_intent.payment_failed`
5. copiare il `whsec_...` dell'endpoint creato

### 9.6 Cosa non serve configurare adesso

Per evitare complessità prematura, non serve configurare subito:

- `Products`
- `Prices`
- `Payment Links`
- `Subscriptions`
- `Billing` avanzato
- `restricted API keys`
- `live mode`

Nota importante:

- il fatto che `CineBase` usi `Stripe Checkout` non obbliga a costruire un catalogo prodotti Stripe persistente; la sessione hosted può essere creata dinamicamente dal backend con importi e descrizioni runtime

---

## 10. Configurazione ambienti e secrets

### 10.1 Backend

In `backend/.env`:

```text
STRIPE_SECRET_API_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
STRIPE_PAYMENT_FLOW=checkout
FRONTEND_BASE_URL=http://localhost:5001
```

### 10.2 Frontend

Il frontend non deve mantenere una chiave Stripe duplicata in un `.env` locale dedicato.

La publishable key deve essere esposta runtime dal backend.

### 10.3 Regole di separazione

- `pk_test_...` esposta al frontend tramite backend runtime config
- `sk_test_...` solo nel backend
- `whsec_...` solo nel backend
- `live` soltanto quando il progetto sarà pronto per ambienti pubblici

---

## 11. Metadati, idempotenza e lock d'ordine

### 11.1 Metadati obbligatori della Checkout Session

Si raccomanda di includere almeno:

```text
orderId
orderCode
userId
showId
```

Questi metadati servono a:

- riconciliare rapidamente la sessione
- validare coerenza ordine
- facilitare debug e audit tecnico

### 11.2 Idempotenza applicativa

La logica deve proteggere almeno questi casi:

- se ordine è già `Paid`, non rieseguire finalizzazione
- se i biglietti esistono già, non riemetterli
- se il credito è già stato consolidato, non addebitarlo di nuovo
- se i posti sono già `Sold`, non ripetere la conversione

### 11.3 Lock d'ordine

Con checkout hosted è necessario almeno:

- `CheckoutInProgress`
- `CheckoutExpiresAtUtc`
- cleanup ordini scaduti
- rilascio posti e credito riservato su cancel o expire

---

## 12. Piano incrementale di implementazione

### 12.1 Step 1

Estendere `Ordine` con i campi necessari a checkout hosted, scadenza e credito riservato.

### 12.2 Step 2

Implementare il gateway backend che crea la `Checkout Session`.

### 12.3 Step 3

Implementare la logica di riserva credito e lock d'ordine.

### 12.4 Step 4

Implementare webhook hosted e riconciliazione backend.

### 12.5 Step 5

Migrare `pagamento.html` dal flusso embedded a redirect hosted.

### 12.6 Step 6

Aggiornare `esito-acquisto.html` con polling stato backend.

### 12.7 Step 7

Scrivere test per:

- solo credito
- solo carta hosted
- misto
- saldo insufficiente
- webhook duplicato
- cancel ed expire
- ritardo webhook rispetto al redirect browser

---

## 13. Stripe Elements: approccio documentato ma non supportato

`Stripe Elements` resta un approccio tecnicamente valido e viene mantenuto in documentazione per completezza storica e comparativa.

Tuttavia, per la direzione del prodotto `CineBase`, `Stripe Elements` è da considerare **non supportato come soluzione finale**.

### 13.1 Perché non è la direzione scelta

Le motivazioni non sono legate a una presunta insicurezza intrinseca di `Stripe Elements`.

Le motivazioni sono di prodotto e fiducia percepita:

- il team vuole che l'utente veda una pagina Stripe riconoscibile nel momento dell'inserimento carta
- il prodotto vuole minimizzare la diffidenza di utenti che non conoscono ancora il brand CineBase
- il team preferisce una UX hosted che comunichi in modo più diretto la presenza del provider di pagamento

### 13.2 Quando può restare nel codice

Solo come:

- fallback tecnico temporaneo durante la migrazione
- feature flag di rollback controllato
- riferimento storico nei tutorial e nel confronto architetturale

Non deve però essere presentato come direzione ufficiale del checkout del prodotto.

---

## 14. Cosa non implementare subito

Per mantenere il progetto sotto controllo, conviene non introdurre subito:

- rimborsi automatici completi
- dispute e chargeback workflow avanzati
- salvataggio carte per riuso futuro
- orchestrazioni multi-provider
- logiche di billing ricorrente

---

## 15. Checklist di conformità

La strategia hosted proposta è corretta se risultano veri tutti i punti seguenti:

- il backend ricalcola sempre il totale
- il backend decide sempre la quota credito e la quota carta
- il caso solo credito non chiama Stripe
- il caso misto riserva e poi consolida o rilascia credito
- il webhook verifica la firma
- la finalizzazione è idempotente
- il redirect browser non è considerato fonte sufficiente di verità
- il lock d'ordine protegge i posti durante il checkout hosted
- il cleanup rilascia automaticamente ordini scaduti

---

## 16. Procedura replicabile di collaudo locale

### 16.1 Prerequisiti minimi

1. `STRIPE_SECRET_API_KEY` presente nel backend
2. `STRIPE_WEBHOOK_SECRET` presente nel backend
3. `Stripe CLI` installata e autenticata con `stripe login`
4. backend avviato su `http://localhost:5000`
5. frontend avviato su `http://localhost:5001`
6. almeno uno `Show` con posti disponibili
7. almeno un utente con cui autenticarsi

### 16.2 Listener locale

```powershell
stripe listen --events checkout.session.completed,checkout.session.expired,payment_intent.payment_failed --forward-to localhost:5000/payments/stripe/webhook
```

### 16.3 Sequenza pratica hosted

1. autenticarsi nel frontend
2. creare hold posti
3. creare ordine
4. avviare `POST /checkout/orders/{orderId}/stripe-checkout-session`
5. reindirizzarsi a Stripe hosted
6. completare il pagamento in `test mode`
7. tornare su `esito-acquisto.html`
8. verificare che il backend porti l'ordine a `Paid`
9. verificare che i biglietti siano stati generati

### 16.4 Verifica casi negativi

È necessario testare anche:

- ritorno su `cancel_url`
- sessione scaduta
- webhook duplicato
- webhook in ritardo rispetto al redirect browser

---

## 17. Conclusione operativa

La strategia più adatta per `CineBase` è la seguente:

1. usare `Stripe Checkout` hosted come flusso ufficiale per il pagamento carta
2. mantenere il backend come unica fonte autorevole per importi, ordini, credito e biglietti
3. gestire il caso `solo credito` completamente fuori da Stripe
4. gestire il caso `misto` con riserva credito e finalizzazione webhook-driven
5. usare `Stripe CLI` in locale per collaudare seriamente i webhook
6. mantenere `Stripe Elements` solo come fallback tecnico temporaneo e come riferimento documentale, non come strategia supportata del prodotto

Questa soluzione è coerente con l'obiettivo di aumentare la fiducia percepita degli utenti finali senza rinunciare a rigore tecnico, idempotenza e controllo del dominio applicativo.
