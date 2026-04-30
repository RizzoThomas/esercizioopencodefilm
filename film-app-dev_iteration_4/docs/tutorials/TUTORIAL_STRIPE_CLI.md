# Tutorial completo: Stripe CLI per sviluppo, test e webhook

**Autore:** OpenCode  
**Progetto di riferimento:** CineBase  
**Ambito:** uso pratico di Stripe CLI in contesto locale e semi-produttivo  

---

## Indice

- [Tutorial completo: Stripe CLI per sviluppo, test e webhook](#tutorial-completo-stripe-cli-per-sviluppo-test-e-webhook)
  - [Indice](#indice)
  - [1. Obiettivo del tutorial](#1-obiettivo-del-tutorial)
  - [2. Che cos'è Stripe CLI](#2-che-cosè-stripe-cli)
  - [3. Perché è utile in un progetto come CineBase](#3-perché-è-utile-in-un-progetto-come-cinebase)
  - [4. Installazione e login](#4-installazione-e-login)
    - [4.1 Installazione](#41-installazione)
    - [4.2 Verifica installazione](#42-verifica-installazione)
    - [4.3 Login](#43-login)
  - [5. Comando fondamentale: ascoltare e inoltrare webhook](#5-comando-fondamentale-ascoltare-e-inoltrare-webhook)
  - [6. Workflow locale tipico per CineBase](#6-workflow-locale-tipico-per-cinebase)
    - [6.1 Preparazione](#61-preparazione)
    - [6.2 Sequenza pratica](#62-sequenza-pratica)
    - [6.3 Carte di test](#63-carte-di-test)
  - [7. Eventi da ascoltare per il progetto](#7-eventi-da-ascoltare-per-il-progetto)
  - [8. Come generare eventi di test](#8-come-generare-eventi-di-test)
    - [8.1 Esempio: pagamento riuscito](#81-esempio-pagamento-riuscito)
    - [8.2 Esempio: pagamento fallito](#82-esempio-pagamento-fallito)
    - [8.3 Limite da comprendere](#83-limite-da-comprendere)
  - [9. Come leggere il webhook secret corretto](#9-come-leggere-il-webhook-secret-corretto)
  - [10. Workflow principali oltre a CineBase](#10-workflow-principali-oltre-a-cinebase)
    - [10.1 Test di un e-commerce classico](#101-test-di-un-e-commerce-classico)
    - [10.2 Test di abbonamenti](#102-test-di-abbonamenti)
    - [10.3 Test di rimborsi](#103-test-di-rimborsi)
    - [10.4 Debug di endpoint webhook](#104-debug-di-endpoint-webhook)
  - [11. Diagnostica e troubleshooting](#11-diagnostica-e-troubleshooting)
    - [11.1 Il backend riceve 400 sul webhook](#111-il-backend-riceve-400-sul-webhook)
    - [11.2 Il backend non riceve nulla](#112-il-backend-non-riceve-nulla)
    - [11.3 Il pagamento funziona ma il webhook non finalizza l'ordine](#113-il-pagamento-funziona-ma-il-webhook-non-finalizza-lordine)
    - [11.4 L'evento arriva più volte](#114-levento-arriva-più-volte)
  - [12. Buone pratiche operative](#12-buone-pratiche-operative)
    - [12.1 Tenere separati i ruoli](#121-tenere-separati-i-ruoli)
    - [12.2 Non dipendere da trigger simulati come unico test](#122-non-dipendere-da-trigger-simulati-come-unico-test)
    - [12.3 Mettere metadati utili nei PaymentIntent](#123-mettere-metadati-utili-nei-paymentintent)
    - [12.4 Annotare sempre quale secret è in uso](#124-annotare-sempre-quale-secret-è-in-uso)
  - [13. Workflow replicabile per test assistiti o automatici](#13-workflow-replicabile-per-test-assistiti-o-automatici)
  - [14. Conclusione](#14-conclusione)

---

## 1. Obiettivo del tutorial

Questo tutorial documenta come usare Stripe CLI in modo pratico, con particolare attenzione ai workflow utili durante lo sviluppo di `CineBase`.

L'obiettivo è chiarire:

- a cosa serve davvero Stripe CLI
- come usarla per testare webhook in locale
- quali comandi sono più utili nella pratica
- come riutilizzare gli stessi concetti anche in altri progetti

---

## 2. Che cos'è Stripe CLI

Stripe CLI è uno strumento da riga di comando ufficiale che aiuta a interagire con Stripe senza passare sempre dalla dashboard.

Le funzioni più importanti sono:

- autenticarsi sul proprio account Stripe
- ascoltare eventi webhook e inoltrarli a un endpoint locale
- generare eventi di test
- ispezionare alcune risorse o operazioni utili durante lo sviluppo

In ambito didattico, il valore principale di Stripe CLI è che elimina il bisogno immediato di esporre il backend locale con strumenti esterni solo per ricevere webhook.

---

## 3. Perché è utile in un progetto come CineBase

Nel progetto `CineBase`, l'endpoint webhook previsto è:

```text
POST /payments/stripe/webhook
```

In locale il backend gira tipicamente su:

```text
http://localhost:5000
```

Stripe non può raggiungere direttamente `localhost`, ma Stripe CLI può fare da ponte:

1. ascolta gli eventi dai server Stripe
2. li inoltra alla macchina locale
3. fornisce anche il `whsec_...` da configurare per verificare la firma

---

## 4. Installazione e login

### 4.1 Installazione

La documentazione ufficiale di riferimento è disponibile su:

```text
https://docs.stripe.com/stripe-cli
```

In ambiente Windows, l'installazione può avvenire in modi diversi. I più comuni sono:

- installer dedicato
- package manager come `winget`
- download manuale del binario

Esempio con `winget`:

```powershell
winget install Stripe.StripeCLI
```

### 4.2 Verifica installazione

```powershell
stripe version
```

### 4.3 Login

```powershell
stripe login
```

Questo comando:

- apre il browser
- richiede autorizzazione sull'account Stripe
- collega la CLI all'account corrente

---

## 5. Comando fondamentale: ascoltare e inoltrare webhook

Il comando più importante per `CineBase` è:

```powershell
stripe listen --forward-to localhost:5000/payments/stripe/webhook
```

Cosa fa:

- si mette in ascolto degli eventi Stripe
- inoltra ciascun evento all'endpoint locale indicato
- mostra a video un `webhook signing secret` temporaneo

Output tipico:

```text
Ready! Your webhook signing secret is whsec_xxxxxxxxxxxxx
```

Questo valore va copiato nel backend locale:

```text
STRIPE_WEBHOOK_SECRET=whsec_xxxxxxxxxxxxx
```

Nota importante:

- questo `whsec_...` appartiene alla sessione CLI o al listener configurato
- non coincide necessariamente con un secret creato manualmente dal dashboard

---

## 6. Workflow locale tipico per CineBase

### 6.1 Preparazione

1. Avviare il backend `FilmAPI` su `http://localhost:5000`.
2. Verificare che l'endpoint `/payments/stripe/webhook` esista.
3. Configurare `STRIPE_SECRET_API_KEY` nel backend.
4. Avviare il listener Stripe CLI.

### 6.2 Sequenza pratica

Terminale 1:

```powershell
dotnet run --project backend/FilmAPI/FilmAPI.csproj
```

Terminale 2:

```powershell
stripe listen --forward-to localhost:5000/payments/stripe/webhook
```

Terminale 3 o browser:

- eseguire il flusso di acquisto in `CineBase`
- confermare un pagamento di test

Il backend riceverà i webhook inoltrati da Stripe CLI come se fosse già pubblicamente raggiungibile.

### 6.3 Carte di test

Esempio noto per pagamento riuscito:

```text
4242 4242 4242 4242
```

Per lo sviluppo occorre sempre usare carte di test documentate da Stripe.

---

## 7. Eventi da ascoltare per il progetto

Per la direzione hosted di `CineBase`, gli eventi minimi consigliati sono:

- `checkout.session.completed`
- `checkout.session.expired`
- `payment_intent.payment_failed`

Listener filtrato:

```powershell
stripe listen --events checkout.session.completed,checkout.session.expired,payment_intent.payment_failed --forward-to localhost:5000/payments/stripe/webhook
```

Vantaggio del filtro:

- riduce il rumore
- rende più leggibili i log
- aiuta durante il debugging mirato

---

## 8. Come generare eventi di test

Stripe CLI permette di generare eventi simulati senza dover completare ogni volta un pagamento reale di test.

### 8.1 Esempio: pagamento riuscito

```powershell
stripe trigger payment_intent.succeeded
```

### 8.2 Esempio: pagamento fallito

```powershell
stripe trigger payment_intent.payment_failed
```

### 8.3 Limite da comprendere

Gli eventi generati con `trigger` sono utilissimi per testare:

- parsing del payload
- verifica firma
- comportamento idempotente del webhook

Non sostituiscono completamente il test del flusso reale ordine-pagamento, perché potrebbero non corrispondere esattamente ai metadati o agli ID generati dall'applicazione.

Per `CineBase`, il test migliore resta:

1. creare davvero l'ordine `Pending`
2. creare davvero la `Checkout Session`
3. completare il checkout hosted in `test mode`
4. osservare il webhook inoltrato da Stripe CLI

---

## 9. Come leggere il webhook secret corretto

Questo punto è fondamentale.

Quando Stripe CLI avvia un listener, mostra un `whsec_...` che deve essere usato dal backend per validare le firme degli eventi inoltrati dalla CLI.

Se il backend usa un altro secret, ad esempio uno preso dal dashboard per un endpoint differente, la verifica firma fallirà.

Regola pratica:

- webhook inoltrati da Stripe CLI -> usare il `whsec_...` mostrato dalla CLI
- webhook ricevuti da un endpoint pubblico reale creato in dashboard -> usare il `whsec_...` di quell'endpoint

---

## 10. Workflow principali oltre a CineBase

Stripe CLI è utile anche in altri contesti.

### 10.1 Test di un e-commerce classico

Uso tipico:

- listener webhook locale
- trigger di `checkout.session.completed`
- verifica di aggiornamento ordine

### 10.2 Test di abbonamenti

Uso tipico:

- ascolto di `invoice.paid`
- ascolto di `customer.subscription.updated`
- verifica aggiornamento stato abbonamento nel backend

### 10.3 Test di rimborsi

Uso tipico:

- esecuzione rimborso da dashboard o API
- osservazione evento di rimborso nel backend

### 10.4 Debug di endpoint webhook

Uso tipico:

- inviare eventi controllati
- verificare log e status code HTTP
- correggere parsing, firma o idempotenza

---

## 11. Diagnostica e troubleshooting

### 11.1 Il backend riceve 400 sul webhook

Cause frequenti:

- `STRIPE_WEBHOOK_SECRET` errato
- body letto in modo incompatibile con la verifica firma
- payload non processato correttamente

### 11.2 Il backend non riceve nulla

Cause frequenti:

- listener CLI non attivo
- URL `--forward-to` errato
- backend non in ascolto sulla porta corretta

### 11.3 Il pagamento funziona ma il webhook non finalizza l'ordine

Cause frequenti:

- metadati `orderId` mancanti nella `Checkout Session`
- idempotenza non implementata correttamente
- logica business del webhook non allineata a quella del flusso hosted e della riconciliazione backend

### 11.4 L'evento arriva più volte

Non è necessariamente un errore. Il backend deve essere progettato per tollerare duplicati.

---

## 12. Buone pratiche operative

### 12.1 Tenere separati i ruoli

- Stripe CLI serve per test e forwarding
- il backend resta responsabile della business logic
- il frontend resta responsabile solo della UX di pagamento

### 12.2 Non dipendere da trigger simulati come unico test

I trigger aiutano, ma il flusso reale con `Checkout Session` hosted e ritorno applicativo va comunque provato.

### 12.3 Mettere metadati utili nelle Checkout Session

Per `CineBase` è utile includere almeno:

```text
orderId
orderCode
userId
showId
```

### 12.4 Annotare sempre quale secret è in uso

Durante lo sviluppo è facile confondere:

- secret key API
- webhook secret della CLI
- webhook secret del dashboard

Una documentazione chiara nel progetto evita molti errori.

---

## 13. Workflow replicabile per test assistiti o automatici

Questa sezione descrive un workflow concreto che può essere eseguito da un docente, da uno studente oppure da un assistente automatico con accesso locale al workspace.

### 13.1 Obiettivo del workflow

Verificare davvero tutti i punti seguenti:

- il backend crea la `Checkout Session`
- Stripe completa il checkout in `test mode`
- il frontend torna su CineBase e interroga lo stato ordine
- il webhook inoltrato da Stripe CLI finalizza o riconcilia l'ordine correttamente

### 13.2 Controlli preliminari

Verificare:

```powershell
stripe --version
stripe whoami
curl http://localhost:5000/swagger
```

Poi verificare che almeno uno show abbia posti reali disponibili chiamando la `seat-map`.

### 13.3 Procedura sincrona assistita

1. autenticarsi sul backend con un utente reale del DB locale
2. leggere la `seat-map` di uno show con posti disponibili
3. creare un hold
4. creare un ordine `Pending`
5. chiamare `POST /checkout/orders/{orderId}/stripe-checkout-session`
6. leggere la URL di checkout restituita
7. completare il checkout hosted in `test mode`
8. tornare su `esito-acquisto.html`
9. verificare che l'ordine sia `Paid`
10. verificare che i ticket siano stati generati

### 13.4 Procedura webhook assistita

1. avviare il backend locale
2. avviare il listener:

```powershell
stripe listen --events checkout.session.completed,checkout.session.expired,payment_intent.payment_failed --forward-to localhost:5000/payments/stripe/webhook
```

3. leggere il secret corretto con:

```powershell
stripe listen --events checkout.session.completed,checkout.session.expired,payment_intent.payment_failed --forward-to localhost:5000/payments/stripe/webhook --print-secret
```

4. assicurarsi che il backend usi proprio quel `whsec_...`
5. creare hold e ordine come sopra
6. chiamare `POST /checkout/orders/{orderId}/stripe-checkout-session`
7. completare il checkout hosted in `test mode`
8. attendere il webhook
9. verificare che l'ordine passi a `Paid` senza finalizzazione manuale lato browser

### 13.5 Regola didattica importante: non usare raw card data

Durante i test assistiti o automatici non è opportuno usare comandi che passano numeri carta grezzi alle API Stripe.

Esempio da evitare:

- creazione diretta di `payment_method` con numero `4242...`

Motivi:

- Stripe può rifiutare la richiesta
- può inviare email di warning sull'uso di raw card data
- non rappresenta il flusso corretto che in produzione passa da Stripe.js / Elements

Per i test automatici o da terminale usare invece:

- `tok_visa`

### 13.6 Problemi reali che gli studenti possono incontrare

#### A. `seat-map` vuota

Cause:

- show presenti nel DB ma sala senza `SalaPosti`

Soluzione:

- popolare la sala con `PUT /sale/{salaId}/posti`

#### B. Webhook con firma valida ma errore interno

Caso reale emerso:

- evento Stripe con API version diversa da quella attesa dalla libreria backend

Effetto:

- il webhook arriva
- ma il backend non finalizza l'ordine

#### C. `dotnet build` fallisce dopo il test

Cause:

- backend ancora in esecuzione
- binari lockati dal processo locale

Soluzione:

- fermare i processi `dotnet` o `FilmAPI` e poi rilanciare build e test

### 13.7 Quando un assistente automatico può davvero eseguire questi test

Un assistente può replicare questo workflow solo se ha accesso reale a:

- file `.env`
- terminale locale
- processi locali
- backend locale
- database locale

In assenza di questi accessi, l'assistente può aiutare a progettare il test, ma non può garantire il collaudo reale end-to-end.

---

## 14. Conclusione

Stripe CLI è lo strumento più semplice e lineare per testare i webhook di Stripe in locale.

Per `CineBase` rappresenta il ponte ideale tra:

- semplicità di sviluppo locale
- necessità didattica di comprendere i webhook
- allineamento progressivo verso un deployment corretto

Per questo motivo, anche se il progetto può partire con una prima integrazione sincrona, Stripe CLI è lo strumento più naturale per fare il passo successivo verso una integrazione completa.
