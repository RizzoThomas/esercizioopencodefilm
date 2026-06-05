/**
 * CineBase — Validazione Biglietti
 * QR/Barcode camera scanning + manual entry
 * Uses html5-qrcode library for camera scanning
 */
(function () {
  'use strict';

  // Variabile Validazione: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const Validazione = {
    scanner: null,
    scanning: false,
    cooldown: false,
    history: [],
    cinemaId: null,

    /* ================================================================
       INIT
       ================================================================ */
    init() {
      this.loadHistory();
      this.loadCinemaId();
      this.bindEvents();
      this.populateCameraList();
    },

    loadCinemaId() {
      // Try to get cinema ID from Auth user profile
      const user = window.Auth?.getUser?.();
      this.cinemaId = user?.cinemaId || null;
      // If user is admin, they need to specify cinema — we'll prompt or use null
    },

    /* ================================================================
       EVENT BINDING
       ================================================================ */
    bindEvents() {
      // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
      document.getElementById('start-scan-btn')?.addEventListener('click', () => this.startScan());
      // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
      document.getElementById('stop-scan-btn')?.addEventListener('click', () => this.stopScan());
      // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
      document.getElementById('manual-form')?.addEventListener('submit', (e) => {
        e.preventDefault();
        this.handleManualValidation();
      });
      // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
      document.getElementById('codice-input')?.addEventListener('paste', () => {
        // Auto-validate on paste after short delay
        setTimeout(() => this.handleManualValidation(), 100);
      });
      // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
      document.getElementById('clear-history-btn')?.addEventListener('click', () => this.clearHistory());
      // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
      document.getElementById('camera-select')?.addEventListener('change', (e) => {
        if (this.scanning) {
          this.stopScan();
          setTimeout(() => this.startScan(e.target.value), 500);
        }
      });
    },

    /* ================================================================
       CAMERA — Populate device list
       ================================================================ */
    async populateCameraList() {
      try {
        // Variabile devices: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const devices = await Html5Qrcode.getCameras();
        // Variabile select: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const select = document.getElementById('camera-select');
        if (!devices.length) {
          document.getElementById('no-camera-msg')?.classList.remove('hidden');
          return;
        }
        select.innerHTML = devices.map((d, i) =>
          `<option value="${d.id}">${d.label || `Fotocamera ${i + 1}`}</option>`
        ).join('');
        select.classList.remove('hidden');
        document.getElementById('no-camera-msg')?.classList.add('hidden');
      } catch {
        document.getElementById('no-camera-msg')?.classList.remove('hidden');
      }
    },

    /* ================================================================
       CAMERA — Start scanning
       ================================================================ */
    async startScan(cameraId = null) {
      try {
        if (!this.scanner) {
          this.scanner = new Html5Qrcode('reader');
        }

        // Variabile config: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const config = {
          fps: 10,
          qrbox: { width: 250, height: 250 },
          aspectRatio: 1.0,
          formatsToSupport: [
            Html5QrcodeSupportedFormats.QR_CODE,
            Html5QrcodeSupportedFormats.CODE_128,
            Html5QrcodeSupportedFormats.CODE_39,
            Html5QrcodeSupportedFormats.EAN_13,
            Html5QrcodeSupportedFormats.EAN_8,
            Html5QrcodeSupportedFormats.UPC_A,
            Html5QrcodeSupportedFormats.UPC_E,
            Html5QrcodeSupportedFormats.CODABAR,
            Html5QrcodeSupportedFormats.ITF,
            Html5QrcodeSupportedFormats.DATA_MATRIX,
            Html5QrcodeSupportedFormats.AZTEC,
            Html5QrcodeSupportedFormats.PDF_417,
          ],
        };

        // Variabile deviceId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const deviceId = cameraId || undefined;
        // Use the first available camera if none specified
        const targetCamera = deviceId ? { deviceId: { exact: deviceId } } : { facingMode: 'environment' };

        await this.scanner.start(
          targetCamera,
          config,
          (decodedText) => this.onScanSuccess(decodedText),
          (errorMessage) => { /* scanning in progress, ignore */ }
        );

        this.scanning = true;
        this.updateUIState('scanning');
        showToast('Fotocamera avviata — inquadra un codice', 'info');
      } catch (err) {
        console.error('Camera error:', err);
        showToast('Impossibile accedere alla fotocamera. Verifica i permessi.', 'danger');
        document.getElementById('no-camera-msg')?.classList.remove('hidden');
      }
    },

    /* ================================================================
       CAMERA — Stop scanning
       ================================================================ */
    async stopScan() {
      try {
        if (this.scanner && this.scanning) {
          await this.scanner.stop();
        }
      } catch (err) {
        console.error('Stop scan error:', err);
      }
      this.scanning = false;
      this.updateUIState('idle');
    },

    /* ================================================================
       SCAN SUCCESS — Code detected by camera
       ================================================================ */
    onScanSuccess(decodedText) {
      // Cooldown check — prevent rapid re-scans
      if (this.cooldown) return;
      this.cooldown = true;

      // Parse the scanned text — handle URLs, QR codes with parameters, etc.
      let codice = decodedText.trim();

      // If it's a URL, extract the 'codice' query parameter
      if (codice.startsWith('http://') || codice.startsWith('https://')) {
        try {
          // Variabile url: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const url = new URL(codice);
          // Variabile codeParam: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const codeParam = url.searchParams.get('codice') || url.searchParams.get('code') || url.searchParams.get('ticket');
          if (codeParam) {
            codice = codeParam;
          } else {
            // If no query param, use the last path segment as a fallback
            const segments = url.pathname.split('/').filter(s => s);
            if (segments.length > 0) {
              codice = segments[segments.length - 1];
            }
          }
        } catch {
          // Not a valid URL, use as-is after stripping protocol
          codice = decodedText.replace(/^https?:\/\/[^/]+\/?/, '').trim();
        }
      }

      // Also handle plain query strings like "codice=ABC123"
      if (codice.includes('=') && !codice.includes('/')) {
        // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const params = new URLSearchParams(codice);
        // Variabile codeParam: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const codeParam = params.get('codice') || params.get('code') || params.get('ticket');
        if (codeParam) codice = codeParam;
      }

      // Flash effect
      const flash = document.getElementById('scan-flash');
      if (flash) {
        flash.classList.remove('hidden');
        flash.style.background = 'rgba(3,144,74,0.15)';
        setTimeout(() => flash.classList.add('hidden'), 300);
      }

      // Auto-fill and validate
      document.getElementById('codice-input').value = codice;
      this.handleManualValidation();

      // Cooldown: pause camera briefly, then resume
      setTimeout(async () => {
        this.cooldown = false;
      }, 3000);
    },

    /* ================================================================
       MANUAL VALIDATION — Entered or scanned code
       ================================================================ */
    async handleManualValidation() {
      // Variabile codice: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const codice = document.getElementById('codice-input')?.value?.trim();
      if (!codice) {
        showToast('Inserisci un codice biglietto', 'warning');
        return;
      }

      // Variabile validateBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const validateBtn = document.getElementById('validate-btn');
      // Variabile originalText: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const originalText = validateBtn.innerHTML;
      validateBtn.disabled = true;
      validateBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-1"></i>Validazione...';

      try {
        // Step 1: Lookup ticket
        const lookup = await this.lookupTicket(codice);

        if (!lookup) {
          this.showResult('error', 'Biglietto non trovato', `Nessun biglietto corrisponde al codice <strong>${this.escapeHtml(codice)}</strong>`);
          return;
        }

        // Step 2: Check if already validated
        if (lookup.stato === 'Validated') {
          this.showResult('already', 'Già validato', this.buildTicketInfo(lookup),
            `Validato il ${this.formatDateTime(lookup.validatoAtUtc)}`);
          this.addToHistory(codice, 'already', lookup);
          return;
        }

        if (lookup.stato === 'Cancelled') {
          this.showResult('error', 'Biglietto annullato', this.buildTicketInfo(lookup),
            'Questo biglietto è stato annullato e non può essere validato.');
          this.addToHistory(codice, 'cancelled', lookup);
          return;
        }

        // Step 3: Validate the ticket (pass CinemaId from lookup)
        const result = await this.validateTicket(codice, lookup.cinemaId || lookup.cinema_id || 0);

        if (result.success) {
          this.showResult('success', '✅ Biglietto Validato!', this.buildTicketInfo(result.ticket || lookup));
          this.addToHistory(codice, 'success', result.ticket || lookup);
          showToast('Biglietto validato con successo!', 'success');
        } else {
          this.showResult('error', 'Validazione fallita', null, result.message || 'Errore durante la validazione');
          this.addToHistory(codice, 'error', null);
        }

      } catch (err) {
        console.error('Validation error:', err);
        // Variabile msg: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const msg = err?.message || 'Errore di connessione al server';
        this.showResult('error', 'Errore', null, msg);
        this.addToHistory(codice, 'error', null);
      } finally {
        validateBtn.disabled = false;
        validateBtn.innerHTML = originalText;
        document.getElementById('codice-input').value = '';
        document.getElementById('codice-input').focus();
      }
    },

    /* ================================================================
       API CALLS
       ================================================================ */
    async lookupTicket(codice) {
      try {
        // Variabile url: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const url = `${API.baseUrl}/admin/tickets/validate/${encodeURIComponent(codice)}`;
        // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
        const res = await fetch(url, { headers: API.getAuthHeaders() });
        if (res.status === 404) return null;
        if (!res.ok) {
          // Variabile err: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const err = await res.json().catch(() => ({}));
          throw new Error(err.message || `Errore server (${res.status})`);
        }
        return await res.json();
      } catch (err) {
        if (err.message) throw err;
        throw new Error('Backend non raggiungibile');
      }
    },

    async validateTicket(codice, cinemaId = 0) {
      // Variabile body: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const body = { CodiceBiglietto: codice, CinemaId: cinemaId };
      // Variabile url: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const url = `${API.baseUrl}/admin/tickets/validate`;
      // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
      const res = await fetch(url, {
        method: 'POST',
        headers: { ...API.getAuthHeaders(), 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      if (!res.ok) {
        // Variabile err: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const err = await res.json().catch(() => ({}));
        throw new Error(err.message || `Errore validazione (${res.status})`);
      }
      return await res.json();
    },

    /* ================================================================
       RESULT DISPLAY
       ================================================================ */
    showResult(type, title, ticketInfo, errorMsg) {
      // Variabile card: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const card = document.getElementById('result-card');
      // Variabile content: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const content = document.getElementById('result-content');
      card.classList.remove('hidden');

      // Variabile colors: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const colors = {
        success: { bg: 'rgba(3,144,74,0.1)', border: 'border-ferrari-semantic-success', icon: 'fa-circle-check text-ferrari-semantic-success' },
        already: { bg: 'rgba(76,152,185,0.1)', border: 'border-ferrari-semantic-info', icon: 'fa-circle-info text-ferrari-semantic-info' },
        error: { bg: 'rgba(241,58,44,0.1)', border: 'border-ferrari-semantic-warning', icon: 'fa-circle-exclamation text-ferrari-semantic-warning' },
        cancelled: { bg: 'rgba(241,58,44,0.1)', border: 'border-ferrari-semantic-warning', icon: 'fa-ban text-ferrari-semantic-warning' },
      };

      const c = colors[type] || colors.error;

      content.innerHTML = `
        <div style="background:${c.bg}" class="border ${c.border} p-4 mb-4">
          <div class="flex items-center gap-2 mb-2">
            <i class="fa-solid ${c.icon} text-xl"></i>
            <span class="font-semibold text-ink">${title}</span>
          </div>
          ${ticketInfo || ''}
          ${errorMsg ? `<p class="text-sm text-body mt-2">${errorMsg}</p>` : ''}
        </div>
      `;

      // Scroll result into view
      card.scrollIntoView({ behavior: 'smooth', block: 'nearest' });

      // Auto-hide after 10 seconds
      clearTimeout(this._resultTimer);
      this._resultTimer = setTimeout(() => {
        card.classList.add('hidden');
      }, 10000);
    },

    buildTicketInfo(ticket) {
      if (!ticket) return '';
      // Variabile statusBadge: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const statusBadge = ticket.stato === 'Validated'
        ? '<span class="badge-ferrari text-xs" style="background:rgba(3,144,74,0.15);color:#03904a">Validato</span>'
        : ticket.stato === 'Cancelled'
        ? '<span class="badge-ferrari text-xs" style="background:rgba(241,58,44,0.15);color:#f13a2c">Annullato</span>'
        : '<span class="badge-ferrari text-xs">Attivo</span>';

      return `
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm mt-3">
          <div><span class="label-caps">Codice</span><p class="text-ink font-mono">${this.escapeHtml(ticket.codiceBiglietto || '-')}</p></div>
          <div><span class="label-caps">Stato</span><p>${statusBadge}</p></div>
          ${ticket.filmTitolo ? `<div class="sm:col-span-2"><span class="label-caps">Film</span><p class="text-ink font-semibold">${this.escapeHtml(ticket.filmTitolo)}</p></div>` : ''}
          ${ticket.cinemaNome ? `<div><span class="label-caps">Cinema</span><p class="text-ink">${this.escapeHtml(ticket.cinemaNome)}</p></div>` : ''}
          ${ticket.salaNome ? `<div><span class="label-caps">Sala / Posto</span><p class="text-ink">${this.escapeHtml(ticket.salaNome)}${ticket.postoInfo ? ' — ' + this.escapeHtml(ticket.postoInfo) : ''}</p></div>` : ''}
          ${ticket.showDate ? `<div><span class="label-caps">Data/Ora Show</span><p class="text-ink">${this.formatDateTime(ticket.showDate)}</p></div>` : ''}
          ${ticket.prezzoTotale ? `<div><span class="label-caps">Prezzo</span><p class="text-ink font-semibold">${this.formatCurrency(ticket.prezzoTotale)}</p></div>` : ''}
          ${ticket.validatoAtUtc ? `<div class="sm:col-span-2"><span class="label-caps">Validato il</span><p class="text-ink">${this.formatDateTime(ticket.validatoAtUtc)}</p></div>` : ''}
        </div>
      `;
    },

    /* ================================================================
       HISTORY MANAGEMENT (localStorage)
       ================================================================ */
    addToHistory(codice, status, ticket) {
      this.history.unshift({
        codice,
        status,
        titolo: ticket?.filmTitolo || null,
        cinema: ticket?.cinemaNome || null,
        timestamp: new Date().toISOString(),
      });
      if (this.history.length > 50) this.history.pop();
      this.saveHistory();
      this.renderHistory();
    },

    loadHistory() {
      try {
        this.history = JSON.parse(localStorage.getItem('cb_validazione_history') || '[]');
      } catch { this.history = []; }
      this.renderHistory();
    },

    saveHistory() {
      localStorage.setItem('cb_validazione_history', JSON.stringify(this.history));
    },

    clearHistory() {
      this.history = [];
      this.saveHistory();
      this.renderHistory();
      showToast('Cronologia cancellata', 'info');
    },

    renderHistory() {
      // Variabile list: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const list = document.getElementById('history-list');
      if (!list) return;

      if (!this.history.length) {
        list.innerHTML = `
          <div class="px-6 py-8 text-center text-body">
            <i class="fa-solid fa-ticket-simple text-3xl mb-2 block opacity-30"></i>
            <p>Nessuna validazione recente</p>
            <p class="text-xs text-muted mt-1">I biglietti validati appariranno qui</p>
          </div>`;
        return;
      }

      list.innerHTML = this.history.map((h, i) => {
        // Variabile time: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const time = new Date(h.timestamp);
        // Variabile timeStr: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const timeStr = time.toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' });
        // Variabile dateStr: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const dateStr = time.toLocaleDateString('it-IT', { day: '2-digit', month: 'short' });
        // Variabile icon: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const icon = h.status === 'success' ? 'fa-circle-check text-ferrari-semantic-success'
          : h.status === 'already' ? 'fa-circle-info text-ferrari-semantic-info'
          : h.status === 'cancelled' ? 'fa-ban text-ferrari-semantic-warning'
          : 'fa-circle-exclamation text-ferrari-semantic-warning';

        return `
          <div class="px-6 py-3 flex items-center justify-between gap-4 hover:bg-white/[0.02] transition-colors">
            <div class="flex items-center gap-3 min-w-0">
              <i class="fa-solid ${icon} text-lg flex-shrink-0"></i>
              <div class="min-w-0">
                <p class="text-ink font-mono text-sm truncate">${this.escapeHtml(h.codice)}</p>
                ${h.titolo ? `<p class="text-body text-xs truncate">${this.escapeHtml(h.titolo)}${h.cinema ? ' — ' + this.escapeHtml(h.cinema) : ''}</p>` : ''}
              </div>
            </div>
            <span class="text-muted text-xs flex-shrink-0">${dateStr} ${timeStr}</span>
          </div>`;
      }).join('');
    },

    /* ================================================================
       UI STATE MANAGEMENT
       ================================================================ */
    updateUIState(state) {
      // Variabile startBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const startBtn = document.getElementById('start-scan-btn');
      // Variabile stopBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const stopBtn = document.getElementById('stop-scan-btn');
      // Variabile statusEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const statusEl = document.getElementById('scan-status');
      // Variabile overlay: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const overlay = document.getElementById('scan-overlay');

      if (state === 'scanning') {
        startBtn.classList.add('hidden');
        stopBtn.classList.remove('hidden');
        statusEl.innerHTML = '<span class="w-2 h-2 rounded-full bg-ferrari-semantic-success animate-pulse inline-block"></span> Scansione attiva';
        overlay?.classList.add('hidden');
      } else {
        startBtn.classList.remove('hidden');
        stopBtn.classList.add('hidden');
        statusEl.innerHTML = '<span class="w-2 h-2 rounded-full bg-ferrari-muted inline-block"></span> Fotocamera inattiva';
        overlay?.classList.remove('hidden');
      }
    },

    /* ================================================================
       UTILITY
       ================================================================ */
    formatDateTime(isoString) {
      if (!isoString) return '-';
      const d = new Date(isoString);
      return d.toLocaleString('it-IT', {
        day: '2-digit', month: '2-digit', year: 'numeric',
        hour: '2-digit', minute: '2-digit',
      });
    },

    formatCurrency(amount) {
      return new Intl.NumberFormat('it-IT', { style: 'currency', currency: 'EUR' }).format(amount);
    },

    escapeHtml(str) {
      if (!str) return '';
      // Variabile div: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const div = document.createElement('div');
      div.textContent = str;
      return div.innerHTML;
    },
  };

  // Initialize on DOM ready
  if (document.readyState === 'loading') {
    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    document.addEventListener('DOMContentLoaded', () => Validazione.init());
  } else {
    Validazione.init();
  }
})();
