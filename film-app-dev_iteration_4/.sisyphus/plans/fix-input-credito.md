# Fix: Input visibility + Ricarica Credito

## Problemi

### 1. Input invisibili
`.input-ferrari` ha `background: var(--ferrari-canvas)` (#181818) — stesso colore della pagina. L'input si confonde con lo sfondo.

**Fix**: Cambiare background in `var(--ferrari-canvas-elevated)` (#303030)

### 2. Bottone Ricarica Credito non funziona
- `profilo.js` riga 177 usa `btn btn-gold` (classe rimossa)
- `profilo.js` ha ancora ~50 classi `brand-*` nelle string template HTML
- Manca il modal HTML per la ricarica in `profilo.html`

---

## TODOs

- [x] 1. **Fix input background in styles.css**

  **Cosa fare**: In `frontend/CineBase.Web/wwwroot/css/styles.css`, cambiare `.input-ferrari` background da `var(--ferrari-canvas)` a `var(--ferrari-canvas-elevated)`.

  **File**: `frontend/CineBase.Web/wwwroot/css/styles.css`
  **Categoria**: `quick`

  **Verifica**: grep per `.input-ferrari` → background usa `canvas-elevated`

---

- [x] 2. **Fix profilo.js — brand-* classi + btn-gold**

  **Cosa fare**: In `frontend/CineBase.Web/wwwroot/js/pages/profilo.js`:
  1. Sostituire `btn btn-gold` con `btn-primary` (riga 177)
  2. Sostituire TUTTE le classi `brand-*` nelle string template:
     - `text-brand-on-surface-variant` → `text-body`
     - `text-brand-on-surface` → `text-ink`
     - `text-brand-gold` → `text-ferrari-primary`
     - `bg-brand-gold/15` → `bg-ferrari-primary/20`
     - `border-brand-outline-variant/20` → `border-hairline`
     - `border-brand-outline-variant/10` → `border-hairline`
     - `brand-brand-gold` → `border-ferrari-primary`
     - `rounded-xl` → rimuovere
     - `btn-ghost` → `btn-tertiary`
     - `btn-gold-sm` → `btn-primary`
     - `text-brand-error` → `text-ferrari-semantic-warning`
     - `hover:bg-brand-surface-container-high/50` → `hover:bg-canvas-elevated`
     - `bg-brand-on-surface-variant/15` → `bg-body/15`

  **File**: `frontend/CineBase.Web/wwwroot/js/pages/profilo.js`
  **Categoria**: `unspecified-high`

  **Verifica**: grep per `brand-` in profilo.js → 0 matches

---

- [x] 3. **Aggiungere topup modal a profilo.html**

  **Cosa fare**: Aggiungere il modal HTML per la ricarica credito PRIMA della chiusura del `</main>` (riga 140 circa). Il modal deve contenere:
  - Un overlay sfondo scuro
  - Un pannello con:
    - Titolo "Ricarica Credito"
    - Bottoni importi predefiniti (5€, 10€, 20€, 50€) con classe `topup-amount-btn` e attributo `data-amount`
    - Input personalizzato con id `custom-topup-amount`
    - Testo "Importo: <span id="selected-topup-amount">0,00 €</span>"
    - Bottone paga con id `btn-topup-pay` e classe `btn-primary`, onclick `payTopup()`
    - Bottone annulla con classe `btn-outline`, onclick `closeTopupModal()`

  Template HTML del modal:
  ```html
  <!-- Topup Modal -->
  <div id="topup-modal" class="fixed inset-0 z-50 hidden">
    <div class="fixed inset-0 bg-black/70" onclick="closeTopupModal()"></div>
    <div class="fixed inset-0 z-10 overflow-y-auto">
      <div class="flex min-h-full items-center justify-center p-4">
        <div class="bg-canvas-elevated p-8 w-full max-w-md">
          <h3 class="text-xl font-bold text-ink mb-6">Ricarica Credito</h3>
          
          <p class="text-sm text-body mb-4">Scegli un importo o inserisci un valore personalizzato</p>
          
          <div class="grid grid-cols-2 gap-3 mb-4">
            <button onclick="selectTopupAmount(5)" class="topup-amount-btn border border-hairline px-4 py-3 text-center text-ink hover:border-ferrari-primary transition-colors" data-amount="5">5 €</button>
            <button onclick="selectTopupAmount(10)" class="topup-amount-btn border border-hairline px-4 py-3 text-center text-ink hover:border-ferrari-primary transition-colors" data-amount="10">10 €</button>
            <button onclick="selectTopupAmount(20)" class="topup-amount-btn border border-hairline px-4 py-3 text-center text-ink hover:border-ferrari-primary transition-colors" data-amount="20">20 €</button>
            <button onclick="selectTopupAmount(50)" class="topup-amount-btn border border-hairline px-4 py-3 text-center text-ink hover:border-ferrari-primary transition-colors" data-amount="50">50 €</button>
          </div>
          
          <div class="mb-4">
            <label class="block text-sm text-body mb-2">Importo personalizzato</label>
            <input type="number" id="custom-topup-amount" class="input-ferrari w-full" placeholder="Inserisci importo" min="1" step="0.50" oninput="onCustomTopupChange()">
          </div>
          
          <div class="flex items-center justify-between mb-6 p-4 border border-hairline">
            <span class="text-body">Totale:</span>
            <span id="selected-topup-amount" class="text-2xl font-bold text-ink">0,00 €</span>
          </div>
          
          <div class="flex gap-3">
            <button onclick="closeTopupModal()" class="btn-outline flex-1">Annulla</button>
            <button id="btn-topup-pay" onclick="payTopup()" class="btn-primary flex-1" disabled>Aggiungi Credito</button>
          </div>
        </div>
      </div>
    </div>
  </div>
  ```

  **File**: `frontend/CineBase.Web/wwwroot/profilo.html`
  **Categoria**: `visual-engineering`

  **Verifica**: Aprire profilo.html, caricare credito, cliccare "Ricarica credito" → modal appare

---

## Final Verification

- [x] F1. **Verifica input visibili**: Aprire una pagina con input (login, registrazione, profilo), controllare che il campo input sia visivamente distinguibile dallo sfondo
- [x] F2. **Verifica Ricarica**: Aprire profilo.html, aspettare caricamento credito, cliccare "Ricarica credito" → modal appare con importi predefiniti
- [x] F3. **Scan brand-**: grep per `brand-` in `profilo.js` → 0 matches
