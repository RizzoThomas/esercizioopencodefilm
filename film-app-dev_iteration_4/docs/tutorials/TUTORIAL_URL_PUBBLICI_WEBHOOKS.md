# Tutorial completo: esporre applicazioni locali con URL pubblici per webhook

**Autore:** OpenCode  
**Progetto di riferimento:** CineBase  
**Ambito:** strumenti e strategie per rendere raggiungibile un backend locale da servizi esterni come Stripe  

---

## Indice

- [Tutorial completo: esporre applicazioni locali con URL pubblici per webhook](#tutorial-completo-esporre-applicazioni-locali-con-url-pubblici-per-webhook)
  - [Indice](#indice)
  - [1. Obiettivo del tutorial](#1-obiettivo-del-tutorial)
  - [2. Perché un URL pubblico è necessario](#2-perché-un-url-pubblico-è-necessario)
  - [3. Quando serve davvero un tunnel pubblico](#3-quando-serve-davvero-un-tunnel-pubblico)
    - [3.1 Casi in cui non serve subito](#31-casi-in-cui-non-serve-subito)
    - [3.2 Casi in cui serve](#32-casi-in-cui-serve)
  - [4. Soluzioni principali](#4-soluzioni-principali)
  - [5. ngrok: concetti, vantaggi e limiti](#5-ngrok-concetti-vantaggi-e-limiti)
    - [5.1 Esempio concettuale](#51-esempio-concettuale)
    - [5.2 Vantaggi](#52-vantaggi)
    - [5.3 Limiti](#53-limiti)
  - [6. VS Code Dev Tunnels: concetti, vantaggi e limiti](#6-vs-code-dev-tunnels-concetti-vantaggi-e-limiti)
    - [6.1 Vantaggi](#61-vantaggi)
    - [6.2 Limiti](#62-limiti)
    - [6.3 Quando possono avere senso](#63-quando-possono-avere-senso)
  - [7. Altre soluzioni equivalenti](#7-altre-soluzioni-equivalenti)
    - [7.1 Cloudflare Tunnel](#71-cloudflare-tunnel)
    - [7.2 localtunnel](#72-localtunnel)
    - [7.3 Tailscale Funnel](#73-tailscale-funnel)
    - [7.4 Regola pratica](#74-regola-pratica)
  - [8. Workflow pratico con ngrok](#8-workflow-pratico-con-ngrok)
    - [8.1 Installazione](#81-installazione)
    - [8.2 Esempio di avvio tunnel HTTP](#82-esempio-di-avvio-tunnel-http)
    - [8.3 Uso con Stripe](#83-uso-con-stripe)
    - [8.4 Limite operativo](#84-limite-operativo)
  - [9. Workflow pratico con Dev Tunnels](#9-workflow-pratico-con-dev-tunnels)
    - [9.1 Concetto operativo](#91-concetto-operativo)
    - [9.2 Sequenza pratica generale](#92-sequenza-pratica-generale)
    - [9.3 Considerazione importante](#93-considerazione-importante)
  - [10. Come collegare il tunnel a Stripe](#10-come-collegare-il-tunnel-a-stripe)
    - [10.1 Esempio con endpoint CineBase](#101-esempio-con-endpoint-cinebase)
    - [10.2 Eventi minimi consigliati](#102-eventi-minimi-consigliati)
    - [10.3 Verifica operativa](#103-verifica-operativa)
  - [11. Confronto tra tunnel pubblico e Stripe CLI](#11-confronto-tra-tunnel-pubblico-e-stripe-cli)
    - [11.1 Regola pratica per CineBase](#111-regola-pratica-per-cinebase)
  - [12. Buone pratiche di sicurezza e diagnostica](#12-buone-pratiche-di-sicurezza-e-diagnostica)
    - [12.1 Non trasformare un tunnel in un ambiente stabile di produzione](#121-non-trasformare-un-tunnel-in-un-ambiente-stabile-di-produzione)
    - [12.2 Tenere separati secret e ambienti](#122-tenere-separati-secret-e-ambienti)
    - [12.3 Loggare gli errori in modo utile](#123-loggare-gli-errori-in-modo-utile)
    - [12.4 Progettare per duplicati e retry](#124-progettare-per-duplicati-e-retry)
  - [13. Conclusione](#13-conclusione)

---

## 1. Obiettivo del tutorial

Questo tutorial spiega come e perché esporre un'applicazione locale tramite un URL pubblico, così da consentire a servizi esterni come Stripe di inviare richieste webhook verso la macchina di sviluppo.

Il documento chiarisce:

- che cosa risolve un tunnel pubblico
- quando conviene usare `ngrok`
- quando può essere sufficiente `VS Code Dev Tunnels`
- quali sono i vantaggi e i limiti delle varie opzioni
- come applicare questi strumenti al progetto `CineBase`

---

## 2. Perché un URL pubblico è necessario

Un webhook è una chiamata HTTP che parte dal server di un fornitore esterno e arriva all'endpoint dell'applicazione.

Se l'applicazione locale gira su:

```text
http://localhost:5000
```

questo indirizzo è visibile solo alla macchina locale.

Un server esterno, come Stripe, non può raggiungerlo direttamente.

Per questo motivo serve una mediazione che trasformi temporaneamente il servizio locale in un endpoint pubblico raggiungibile da Internet.

---

## 3. Quando serve davvero un tunnel pubblico

Non sempre serve.

### 3.1 Casi in cui non serve subito

- sviluppo della logica business interna
- test del flusso sincrono ordine-pagamento
- test delle API interne tra frontend e backend

### 3.2 Casi in cui serve

- test reali di webhook Stripe
- integrazioni con sistemi terzi che richiedono callback HTTP
- demo condivise su ambiente ancora non deployato

Nel progetto `CineBase`, il tunnel pubblico serve soprattutto quando si vuole testare davvero:

- `POST /payments/stripe/webhook`

---

## 4. Soluzioni principali

Le opzioni più comuni sono:

- `Stripe CLI`
- `ngrok`
- `VS Code Dev Tunnels`
- servizi equivalenti come `Cloudflare Tunnel`, `localtunnel`, `Tailscale Funnel`

Osservazione importante:

- `Stripe CLI` è specializzato per Stripe e spesso è la scelta più semplice per i webhook Stripe
- `ngrok` e strumenti simili sono più generici e funzionano per molti tipi di callback HTTP

---

## 5. ngrok: concetti, vantaggi e limiti

`ngrok` crea un tunnel tra una porta locale e un URL pubblico temporaneo.

### 5.1 Esempio concettuale

Applicazione locale:

```text
http://localhost:5000
```

URL pubblico generato da ngrok:

```text
https://abc123.ngrok-free.app
```

Richiesta esterna:

```text
https://abc123.ngrok-free.app/payments/stripe/webhook
```

viene inoltrata a:

```text
http://localhost:5000/payments/stripe/webhook
```

### 5.2 Vantaggi

- semplice da capire
- funziona con molti servizi esterni, non solo Stripe
- molto utile per demo e debugging HTTP

### 5.3 Limiti

- URL spesso temporaneo nel piano gratuito
- richiede configurazione esterna rispetto a Stripe
- per il solo caso Stripe webhook, Stripe CLI è spesso più diretta

---

## 6. VS Code Dev Tunnels: concetti, vantaggi e limiti

I Dev Tunnels permettono di esporre una porta locale tramite un endpoint pubblico, integrando l'esperienza con gli strumenti Microsoft.

### 6.1 Vantaggi

- buona integrazione con ecosistema VS Code
- adatti a demo, collaborazione e accesso remoto a servizi locali
- possono essere più naturali in ambienti già centrati su strumenti Microsoft

### 6.2 Limiti

- meno diffusi di ngrok in molti tutorial generici
- la procedura concreta può variare in base a estensioni, login e configurazione del client
- per soli webhook Stripe possono introdurre una complessità non necessaria rispetto a Stripe CLI

### 6.3 Quando possono avere senso

Nel caso di `CineBase`, i Dev Tunnels possono essere utili se il team vuole:

- esporre contemporaneamente backend e frontend
- fare demo condivise oltre al semplice test webhook
- evitare l'uso di più strumenti distinti

---

## 7. Altre soluzioni equivalenti

### 7.1 Cloudflare Tunnel

Molto valido per esposizione pubblica con buone capacità infrastrutturali. Più orientato a scenari evoluti.

Installazione e configurazione di base:

1. creare un account Cloudflare
2. aggiungere un dominio a Cloudflare
3. configurare i nameserver del dominio verso Cloudflare
4. scaricare `cloudflared` dalla documentazione ufficiale
5. su Windows, rinominare l'eseguibile in `cloudflared.exe` e verificare l'installazione con:

```powershell
.\cloudflared.exe --version
```

6. autenticare il client con:

```powershell
cloudflared tunnel login
```

7. creare un tunnel nominato, ad esempio:

```powershell
cloudflared tunnel create cinebase-local
```

8. creare il file di configurazione `config.yml` nella cartella `.cloudflared`, ad esempio:

```yaml
url: http://localhost:5000
tunnel: <Tunnel-UUID>
credentials-file: C:/Users/<utente>/.cloudflared/<Tunnel-UUID>.json
```

9. associare un hostname pubblico al tunnel:

```powershell
cloudflared tunnel route dns cinebase-local webhook-cinebase.example.com
```

10. avviare il tunnel:

```powershell
cloudflared tunnel run cinebase-local
```

11. usare poi in Stripe un endpoint del tipo:

```text
https://webhook-cinebase.example.com/payments/stripe/webhook
```

Osservazione pratica:

- questa soluzione è potente, ma richiede più prerequisiti delle alternative didattiche iniziali
- è consigliata soprattutto quando esiste già un dominio gestito in Cloudflare

### 7.2 localtunnel

Molto semplice in alcuni contesti, ma spesso meno stabile o meno ricco di funzionalità.

Installazione e configurazione di base:

1. installare `Node.js`, perché `localtunnel` si usa tramite ecosistema `npm`
2. per un uso immediato senza installazione globale, eseguire:

```powershell
npx localtunnel --port 5000
```

3. in alternativa, installare il client globalmente:

```powershell
npm install -g localtunnel
```

4. dopo l'installazione globale, avviare il tunnel con:

```powershell
lt --port 5000
```

5. se si desidera provare a richiedere un sottodominio leggibile, usare:

```powershell
lt --port 5000 --subdomain cinebase-demo
```

6. copiare l'URL pubblico restituito dal tool e configurare in Stripe:

```text
https://<subdomain>.localtunnel.me/payments/stripe/webhook
```

Osservazione pratica:

- `localtunnel` non richiede in genere account o token iniziali
- è molto rapido da provare
- è meno consigliato quando serve la massima stabilità del collegamento

### 7.3 Tailscale Funnel

Interessante in ambienti in cui Tailscale è già adottato. Meno immediato come scelta didattica iniziale.

Installazione e configurazione di base:

1. installare il client Tailscale dalla documentazione ufficiale
2. verificare l'installazione con:

```powershell
tailscale version
```

3. autenticare il nodo locale:

```powershell
tailscale login
```

4. verificare che nella tailnet siano attivi:
   - `MagicDNS`
   - HTTPS per il dominio `ts.net`
   - permessi per usare `Funnel`

5. avviare la pubblicazione del servizio locale, ad esempio sulla porta `5000`:

```powershell
tailscale funnel 5000
```

6. annotare l'URL pubblico generato, tipicamente nel dominio `*.ts.net`
7. configurare poi in Stripe l'endpoint webhook aggiungendo il path applicativo:

```text
https://<device>.<tailnet>.ts.net/payments/stripe/webhook
```

Osservazioni pratiche:

- `Tailscale Funnel` è attualmente una soluzione più adatta a team che già usano Tailscale
- richiede prerequisiti specifici su DNS, HTTPS e policy della tailnet
- è meno immediato di `ngrok` o `Stripe CLI` per chi parte da zero

### 7.4 Regola pratica

Per una guida introduttiva e didattica:

- Stripe webhook soltanto -> preferenza a `Stripe CLI`
- webhook generici o demo multi-servizio -> `ngrok`
- ecosistema Microsoft e condivisione di ambienti -> `Dev Tunnels`
- dominio già gestito in Cloudflare -> `Cloudflare Tunnel`
- prova rapida senza account aggiuntivi complessi -> `localtunnel`
- team che già usa rete Tailscale -> `Tailscale Funnel`

---

## 8. Workflow pratico con ngrok

### 8.1 Installazione

La documentazione ufficiale è disponibile su:

```text
https://ngrok.com/docs
```

Installazione e configurazione iniziale consigliata:

1. creare un account `ngrok`
2. recuperare dal dashboard il proprio `authtoken`
3. installare il client `ngrok`

Su Windows, i percorsi più lineari sono:

- installazione tramite Windows App Store
- download diretto dal sito ufficiale

4. verificare che il client sia disponibile nel terminale:

```powershell
ngrok help
```

5. collegare il client all'account con:

```powershell
ngrok config add-authtoken <TOKEN>
```

6. a questo punto il client è configurato e pronto a esporre la porta locale dell'applicazione

### 8.2 Esempio di avvio tunnel HTTP

Se il backend di `CineBase` gira su porta `5000`:

```powershell
ngrok http 5000
```

ngrok restituisce un URL pubblico, ad esempio:

```text
https://abc123.ngrok-free.app
```

### 8.3 Uso con Stripe

Nel dashboard Stripe si crea un endpoint webhook come:

```text
https://abc123.ngrok-free.app/payments/stripe/webhook
```

Stripe genererà un `whsec_...` per quell'endpoint.

Questo valore va configurato nel backend locale.

### 8.4 Limite operativo

Se l'URL cambia a ogni sessione, ogni volta occorre:

1. aggiornare l'endpoint webhook in Stripe
2. verificare il relativo webhook secret

---

## 9. Workflow pratico con Dev Tunnels

### 9.1 Concetto operativo

Il backend locale viene esposto su una porta pubblica attraverso un tunnel gestito dal servizio Dev Tunnels.

Osservazione importante:

- in Visual Studio Code il supporto al port forwarding tramite Microsoft Dev Tunnels è integrato
- non è richiesta un'estensione dedicata solo per il forwarding di porte locali

### 9.2 Sequenza pratica generale

1. installare o aggiornare Visual Studio Code
2. avviare il backend locale, ad esempio su `http://localhost:5000`
3. aprire in VS Code la vista `Ports` nel pannello inferiore
4. se richiesto, effettuare l'accesso con account GitHub o Microsoft
5. selezionare `Forward a Port`
6. inserire la porta del backend, ad esempio `5000`
7. attendere la generazione dell'indirizzo inoltrato pubblico
8. cambiare la visibilità della porta in `Public` se il servizio esterno deve raggiungerla senza autenticazione interattiva
9. copiare l'indirizzo pubblico generato
10. configurare in Stripe un endpoint del tipo:

```text
https://<dev-tunnel-domain>/payments/stripe/webhook
```

Osservazioni pratiche:

- per default la porta inoltrata può essere `Private`
- per i webhook Stripe serve normalmente una visibilità pubblica
- i Dev Tunnels di VS Code espongono servizi eseguiti localmente, non sono pensati come sostituto di un vero deployment

### 9.3 Considerazione importante

L'idea architetturale è identica a `ngrok`: cambia lo strumento, non il principio.

Anche in questo caso, il backend dovrà usare il `whsec_...` del webhook configurato in Stripe per quell'URL pubblico.

---

## 10. Come collegare il tunnel a Stripe

Qualunque sia lo strumento scelto, il processo concettuale è sempre questo:

1. il backend locale viene esposto pubblicamente
2. Stripe riceve un endpoint pubblico valido
3. Stripe invia un test webhook
4. il backend verifica la firma con `STRIPE_WEBHOOK_SECRET`

### 10.1 Esempio con endpoint CineBase

Endpoint applicativo:

```text
/payments/stripe/webhook
```

URL pubblico finale:

```text
https://example-tunnel-domain/payments/stripe/webhook
```

### 10.2 Eventi minimi consigliati

- `payment_intent.succeeded`
- `payment_intent.payment_failed`
- `payment_intent.canceled`

### 10.3 Verifica operativa

Una volta configurato il webhook, conviene controllare:

- che il backend risponda `200 OK` sugli eventi validi
- che i log mostrino la verifica firma riuscita
- che l'ordine sia aggiornato in modo idempotente

---

## 11. Confronto tra tunnel pubblico e Stripe CLI

| Aspetto | Stripe CLI | ngrok / Dev Tunnels |
| --- | --- | --- |
| Focus | Stripe | generico |
| Complessità iniziale | molto bassa | bassa o media |
| Utile per webhook Stripe | eccellente | buono |
| Utile per altri callback HTTP | limitato | eccellente |
| Configurazione dashboard Stripe | non sempre necessaria per test locale | spesso necessaria |

### 11.1 Regola pratica per CineBase

- per sviluppare i webhook Stripe in locale: `Stripe CLI`
- per esporre un backend locale a più servizi o per demo condivise: `ngrok` o `Dev Tunnels`

---

## 12. Buone pratiche di sicurezza e diagnostica

### 12.1 Non trasformare un tunnel in un ambiente stabile di produzione

Un tunnel serve per sviluppo, test o demo controllate, non come soluzione definitiva di deployment.

### 12.2 Tenere separati secret e ambienti

- `sk_test_...` in locale o test
- `sk_live_...` solo in produzione
- `whsec_...` coerente con l'endpoint realmente in uso

### 12.3 Loggare gli errori in modo utile

Il backend dovrebbe loggare almeno:

- ricezione webhook
- esito verifica firma
- ID evento Stripe
- ID ordine o `PaymentIntent`
- esito finale del processing

### 12.4 Progettare per duplicati e retry

Stripe può reinviare un evento. Il backend deve essere idempotente anche quando il tunnel funziona correttamente.

---

## 13. Conclusione

Gli strumenti di tunneling pubblico risolvono un problema preciso: rendere accessibile un servizio locale da Internet per ricevere callback e webhook.

Nel contesto di `CineBase`, la scelta più semplice per i webhook Stripe è spesso `Stripe CLI`, mentre `ngrok` o `Dev Tunnels` diventano utili quando il progetto ha bisogno di un URL pubblico più generale, ad esempio per demo, debugging multi-servizio o integrazioni non limitate a Stripe.
