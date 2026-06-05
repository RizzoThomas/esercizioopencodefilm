/**
 * CineBase Chat Widget v3 — Minimal, robust, fixed overlay
 * + Quick replies with stock answers
 */
(function () {
  'use strict';
  if (document.getElementById('cb-chat-root')) return;

  // Variabile API_BASE: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const API_BASE = window.API_BASE_URL || 'http://localhost:5000';
  // Variabile failed: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  let failed = 0, open = false;

  // ── Stock FAQ ─────────────────────────────────────
  const STOCK_QA = [
    {
      q: '🎬 Film in programmazione',
      a: 'Puoi consultare la programmazione completa nella sezione <b>Programmazione</b> del nostro sito. Troverai tutti i film attualmente in sala, orari e tipologia di sala (2D, 3D, IMAX).<br><br>👉 <a href="/programmazione.html" style="color:#da291c;text-decoration:underline">Vai alla programmazione</a>'
    },
    {
      q: '🎟️ Come acquistare un biglietto',
      a: 'Per acquistare un biglietto:<br>1. Scegli il film dalla <b>Programmazione</b><br>2. Seleziona data e orario<br>3. Scegli il posto in sala<br>4. Procedi al pagamento<br><br>Puoi pagare con carta di credito, debito o PayPal.'
    },
    {
      q: '🍿 Offerte e promozioni',
      a: 'Abbiamo diverse offerte attive:<br>• <b>Combo Cinema + Snack</b>: biglietto + popcorn + bevanda a prezzo speciale<br>• <b>Offerte famiglia</b>: sconti per gruppi<br>• <b>Abbonamento CineBase</b>: cinema illimitato ogni mese<br><br>👉 <a href="/offerte.html" style="color:#da291c;text-decoration:underline">Scopri le offerte</a>'
    },
    {
      q: '📍 Dove si trovano i cinema',
      a: 'CineBase ha <b>3 cinema</b> in tutta Italia. Puoi trovare indirizzi, mappe e contatti nella sezione <b>I Nostri Cinema</b> del sito.<br><br>👉 <a href="/my-cinemas.html" style="color:#da291c;text-decoration:underline">Vedi i cinema</a>'
    },
    {
      q: '📝 Come registrarsi',
      a: 'La registrazione è gratuita e veloce! Clicca su <b>Registrati</b> nel menu in alto, inserisci email e password, e in pochi secondi avrai accesso a tutte le funzionalità: prenotazioni, cronologia acquisti e offerte personalizzate.<br><br>👉 <a href="/registrazione.html" style="color:#da291c;text-decoration:underline">Registrati ora</a>'
    }
  ];

  // Funzione init: inizializza stato, timer o interfaccia della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function init() {
    if (!document.body) { setTimeout(init, 30); return; }

    // Inject ping animation
    if (!document.getElementById('cb-ping-style')) {
      const s = document.createElement('style');
      s.id = 'cb-ping-style';
      s.textContent = '@keyframes cbPing{0%{transform:scale(1);opacity:0.4}75%,100%{transform:scale(1.6);opacity:0}}';
      document.head.appendChild(s);
    }

    // FAB button — 21st.dev glowing style
    const fab = document.createElement('button');
    fab.id = 'cb-fab';
    // Use transform for position so dragging doesn't break layout
    fab.style.cssText =
      'position:fixed!important;bottom:24px!important;right:24px!important;z-index:2147483647;' +
      'width:56px;height:56px;border-radius:50%!important;border:2px solid rgba(255,255,255,0.2)!important;cursor:grab;' +
      'background:linear-gradient(135deg,rgba(218,41,28,0.9),rgba(180,30,20,0.9))!important;color:#fff;font-size:22px;' +
      'display:flex;align-items:center;justify-content:center;position:relative;overflow:visible;' +
      'box-shadow:0 0 20px rgba(218,41,28,0.7),0 0 40px rgba(180,30,20,0.5),0 0 60px rgba(150,20,10,0.3)!important;' +
      'touch-action:none;user-select:none;-webkit-user-select:none;transition:box-shadow 0.3s';
    fab.title = 'Assistenza CineBase — trascinami!';

    // Inner 3D overlay
    const innerGlow = document.createElement('div');
    innerGlow.setAttribute('style',
      'position:absolute;inset:0;border-radius:50%;' +
      'background:linear-gradient(to bottom,rgba(255,255,255,0.2),transparent);opacity:0.3;pointer-events:none');
    fab.appendChild(innerGlow);

    // Ping ring
    const pingRing = document.createElement('div');
    pingRing.setAttribute('style',
      'position:absolute;inset:-4px;border-radius:50%;' +
      'background:rgba(218,41,28,0.2);animation:cbPing 2s ease-out infinite;pointer-events:none');
    fab.appendChild(pingRing);

    // Icon wrapper
    const iconWrap = document.createElement('span');
    iconWrap.setAttribute('style', 'position:relative;z-index:10;pointer-events:none');
    iconWrap.innerHTML = '<i class="fa-solid fa-message"></i>';
    fab.appendChild(iconWrap);

    // ── Drag Physics (long-press to drag) ──────
    var fabOffsetX = 24, fabOffsetY = 24; // offset from right/bottom
    // Variabile dragStartX: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var dragStartX = 0, dragStartY = 0;
    // Variabile dragStartOffsetX: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var dragStartOffsetX = 0, dragStartOffsetY = 0;
    // Variabile isDragging: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var isDragging = false;
    // Variabile hasMoved: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var hasMoved = false;
    // Variabile velX: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var velX = 0, velY = 0;
    // Variabile physicsId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var physicsId = null;
    // Variabile holdTimer: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var holdTimer = null;
    // Variabile dragEnabled: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var dragEnabled = false;
    // Variabile prevX: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var prevX = 0, prevY = 0; // for per-frame velocity

    // Funzione updateFabPosition: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function updateFabPosition() {
      fab.style.right = fabOffsetX + 'px';
      fab.style.bottom = fabOffsetY + 'px';
    }

    // Funzione fabToViewport: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function fabToViewport() {
      // Convert right/bottom offsets to viewport coordinates
      return {
        x: window.innerWidth - fabOffsetX - 56,
        y: window.innerHeight - fabOffsetY - 56
      };
    }

    // Funzione clampToViewport: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function clampToViewport() {
      // Variabile maxX: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var maxX = window.innerWidth - 56 - 8;
      // Variabile maxY: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var maxY = window.innerHeight - 56 - 8;
      fabOffsetX = Math.max(8, Math.min(maxX, fabOffsetX));
      fabOffsetY = Math.max(8, Math.min(maxY, fabOffsetY));
    }

    // Funzione startPhysics: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function startPhysics() {
      if (physicsId) cancelAnimationFrame(physicsId);
      // Variabile friction: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var friction = 0.88;
      // Variabile snapSpeed: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var snapSpeed = 0.18;

      // Funzione step: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
      function step() {
        velX *= friction;
        velY *= friction;

        fabOffsetX -= velX;
        fabOffsetY -= velY;

        // Variabile minX: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        var minX = 8, maxX = window.innerWidth - 56 - 8;
        // Variabile minY: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        var minY = 8, maxY = window.innerHeight - 56 - 8;

        if (fabOffsetX < minX) { fabOffsetX = minX; velX *= -0.3; }
        if (fabOffsetX > maxX) { fabOffsetX = maxX; velX *= -0.3; }
        if (fabOffsetY < minY) { fabOffsetY = minY; velY *= -0.3; }
        if (fabOffsetY > maxY) { fabOffsetY = maxY; velY *= -0.3; }

        // Snap to nearest edge when velocity is low
        if (Math.abs(velX) < 0.4 && Math.abs(velY) < 0.4) {
          // Variabile targetX: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          var targetX = fabOffsetX < (maxX - minX) / 2 ? minX : maxX;
          fabOffsetX += (targetX - fabOffsetX) * snapSpeed;
          fabOffsetY += ((fabOffsetY < (maxY - minY) / 2 ? minY : maxY) - fabOffsetY) * snapSpeed * 0.6;

          if (Math.abs(velX) < 0.02 && Math.abs(velY) < 0.02) {
            fabOffsetX = Math.round(fabOffsetX < (maxX - minX) / 2 ? minX : maxX);
            fabOffsetY = Math.round(fabOffsetY);
            velX = 0; velY = 0;
            updateFabPosition();
            physicsId = null;
            return;
          }
        }

        updateFabPosition();
        physicsId = requestAnimationFrame(step);
      }
      physicsId = requestAnimationFrame(step);
    }

    // Funzione positionPanel: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function positionPanel() {
      // Variabile vp: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var vp = fabToViewport();
      // Variabile ww: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var ww = window.innerWidth;
      // Variabile wh: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var wh = window.innerHeight;
      // Variabile isLeft: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var isLeft = vp.x < ww / 2;
      // Variabile isTop: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var isTop = vp.y < wh / 2;
      // Variabile isMob: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var isMob = ww < 640;

      // Reset all position properties first
      panel.style.left = ''; panel.style.right = '';
      panel.style.top = ''; panel.style.bottom = '';
      panel.style.width = isMob ? 'auto' : '370px';
      panel.style.maxWidth = 'calc(100vw - 24px)';
      panel.style.maxHeight = 'calc(100vh - 130px)';
      panel.style.height = isMob ? '420px' : '440px';

      // Variabile gap: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var gap = 10; // gap between FAB and panel

      if (isMob) {
        // Full-width on mobile, position above or below FAB
        panel.style.left = '12px';
        panel.style.right = '12px';
        if (isTop) {
          panel.style.top = (vp.y + 56 + gap) + 'px';
          panel.style.bottom = 'auto';
        } else {
          panel.style.bottom = (wh - vp.y + gap) + 'px';
          panel.style.top = 'auto';
        }
      } else {
        // Desktop: panel to the side opposite to FAB's horizontal position
        if (isLeft) {
          panel.style.left = (vp.x + 56 + gap) + 'px';
          panel.style.right = 'auto';
        } else {
          panel.style.right = (ww - vp.x + gap) + 'px';
          panel.style.left = 'auto';
        }
        if (isTop) {
          panel.style.top = Math.max(8, vp.y) + 'px';
          panel.style.bottom = 'auto';
        } else {
          panel.style.bottom = Math.max(8, wh - vp.y - 56) + 'px';
          panel.style.top = 'auto';
        }
      }
    }

    // ── Pointer Events ────────────────────────
    fab.addEventListener('pointerdown', function(e) {
      fab.setPointerCapture(e.pointerId);
      dragStartX = e.clientX;
      dragStartY = e.clientY;
      prevX = e.clientX;
      prevY = e.clientY;
      dragStartOffsetX = fabOffsetX;
      dragStartOffsetY = fabOffsetY;
      isDragging = true;
      hasMoved = false;
      dragEnabled = false;
      velX = 0; velY = 0;
      if (physicsId) { cancelAnimationFrame(physicsId); physicsId = null; }

      // Long-press to enable drag (400ms hold)
      clearTimeout(holdTimer);
      holdTimer = setTimeout(function() {
        dragEnabled = true;
        fab.style.cursor = 'grabbing';
        fab.style.transition = 'none';
      }, 400);

      e.preventDefault();
    });

    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    fab.addEventListener('pointermove', function(e) {
      if (!isDragging) return;
      // Variabile dx: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var dx = dragStartX - e.clientX;
      // Variabile dy: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var dy = dragStartY - e.clientY;
      // Variabile dragThreshold: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var dragThreshold = ('ontouchstart' in window || navigator.maxTouchPoints > 0) ? 12 : 5;
      if (Math.abs(dx) > dragThreshold || Math.abs(dy) > dragThreshold) hasMoved = true;
      if (!hasMoved) return;
      if (!dragEnabled) return;

      // Per-frame velocity from instantaneous movement
      velX = (prevX - e.clientX) * 0.7 + velX * 0.3;
      velY = (prevY - e.clientY) * 0.7 + velY * 0.3;
      prevX = e.clientX;
      prevY = e.clientY;

      fabOffsetX = dragStartOffsetX + dx;
      fabOffsetY = dragStartOffsetY + dy;
      clampToViewport();
      updateFabPosition();

      // Close panel while dragging
      if (open) {
        open = false;
        panel.style.opacity = '0';
        panel.style.visibility = 'hidden';
        panel.style.pointerEvents = 'none';
        panel.style.transform = 'translateY(12px) scale(0.95)';
        iconWrap.innerHTML = '<i class="fa-solid fa-message"></i>';
      }
    });

    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    fab.addEventListener('pointerup', function(e) {
      clearTimeout(holdTimer);
      isDragging = false;
      fab.style.cursor = 'grab';
      fab.style.transition = '';

      if (!hasMoved || !dragEnabled) {
        // It was a click (or not enough hold time)
        fabOffsetX = dragStartOffsetX;
        fabOffsetY = dragStartOffsetY;
        updateFabPosition();
        open = !open;
        if (open) {
          positionPanel();
          panel.style.opacity = '1';
          panel.style.visibility = 'visible';
          panel.style.pointerEvents = 'all';
          panel.style.transform = 'translateY(0) scale(1)';
          iconWrap.innerHTML = '<i class="fa-solid fa-times"></i>';
          inp.focus();
          qrWrap.style.display = 'flex';
          showQrBtn.style.display = 'none';
        } else {
          panel.style.opacity = '0';
          panel.style.visibility = 'hidden';
          panel.style.pointerEvents = 'none';
          panel.style.transform = 'translateY(12px) scale(0.95)';
          iconWrap.innerHTML = '<i class="fa-solid fa-message"></i>';
        }
        return;
      }

      // Apply velocity cap
      var maxVel = 40;
      velX = Math.max(-maxVel, Math.min(maxVel, velX));
      velY = Math.max(-maxVel, Math.min(maxVel, velY));
      startPhysics();
    });

    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    fab.addEventListener('pointercancel', function() {
      clearTimeout(holdTimer);
      isDragging = false;
      dragEnabled = false;
      fab.style.cursor = 'grab';
      fab.style.transition = '';
      if (hasMoved) startPhysics();
    });

    // Chat panel
    const panel = document.createElement('div');
    panel.setAttribute('style',
      'position:fixed!important;z-index:2147483646!important;' +
      'background:var(--ferrari-canvas,#181818);border:1px solid var(--ferrari-hairline,#303030);border-radius:16px;' +
      'display:flex;flex-direction:column;overflow:hidden;' +
      'transform:translateY(12px) scale(0.95);opacity:0;visibility:hidden;pointer-events:none;' +
      'transition:transform 0.25s,opacity 0.2s,visibility 0.2s;' +
      "font-family:'Geist','Inter',sans-serif");

    // Header
    const hdr = document.createElement('div');
    Object.assign(hdr.style, {
      padding:'14px 18px', background:'#da291c', color:'#fff',
      fontSize:'14px', fontWeight:'600', display:'flex', alignItems:'center', gap:'8px'
    });
    hdr.innerHTML = '<i class="fa-solid fa-headset"></i> Assistenza CineBase';
    panel.appendChild(hdr);

    // Messages area
    const msgs = document.createElement('div');
    Object.assign(msgs.style, {
      flex:'1', overflowY:'auto', padding:'14px', display:'flex', flexDirection:'column', gap:'8px'
    });
    msgs.innerHTML = '<div style="max-width:85%;align-self:flex-start;padding:10px 12px;background:var(--ferrari-canvas-elevated,#303030);color:var(--ferrari-ink,#fff);font-size:13px;line-height:1.5">Ciao! 👋 Sono l\'assistente di <b>CineBase</b>. Scegli una domanda qui sotto o scrivimi liberamente!</div>';
    panel.appendChild(msgs);

    // ── Quick reply chips ──────────────────────────
    const qrWrap = document.createElement('div');
    Object.assign(qrWrap.style, {
      padding:'8px 10px 10px', display:'flex', flexWrap:'wrap', gap:'5px',
      borderTop:'1px solid var(--ferrari-hairline,#303030)',
      background:'var(--ferrari-canvas,#181818)',
      maxHeight:'112px', overflowY:'auto'
    });
    STOCK_QA.forEach(item => {
      // Variabile chip: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const chip = document.createElement('button');
      Object.assign(chip.style, {
        padding:'5px 10px', border:'1px solid var(--ferrari-hairline,#303030)',
        background:'var(--ferrari-canvas-elevated,#303030)',
        color:'var(--ferrari-ink,#fff)',
        fontSize:'11px', fontWeight:'500', lineHeight:'1.3',
        cursor:'pointer', borderRadius:'9999px', whiteSpace:'nowrap',
        fontFamily:'inherit', transition:'all 0.15s'
      });
      chip.textContent = item.q;
      chip.onmouseenter = () => { chip.style.borderColor = '#da291c'; chip.style.background = 'rgba(218,41,28,0.15)'; };
      chip.onmouseleave = () => { chip.style.borderColor = 'var(--ferrari-hairline,#303030)'; chip.style.background = 'var(--ferrari-canvas-elevated,#303030)'; };
      chip.onclick = () => {
        handleStockClick(item);
        // Close chips after click
        qrWrap.style.display = 'none';
      };
      qrWrap.appendChild(chip);
    });

    // Button to re-show chips
    const showQrBtn = document.createElement('button');
    Object.assign(showQrBtn.style, {
      display:'none', alignSelf:'flex-start', marginTop:'4px',
      padding:'4px 10px', border:'1px dashed var(--ferrari-hairline,#303030)',
      background:'transparent', color:'var(--ferrari-body,#969696)',
      fontSize:'11px', cursor:'pointer', borderRadius:'9999px',
      fontFamily:'inherit', transition:'all 0.15s'
    });
    showQrBtn.textContent = '+ Domande frequenti';
    showQrBtn.onmouseenter = () => { showQrBtn.style.borderColor = '#da291c'; showQrBtn.style.color = '#da291c'; };
    showQrBtn.onmouseleave = () => { showQrBtn.style.borderColor = 'var(--ferrari-hairline,#303030)'; showQrBtn.style.color = 'var(--ferrari-body,#969696)'; };
    showQrBtn.onclick = () => {
      qrWrap.style.display = 'flex';
      showQrBtn.style.display = 'none';
    };

    panel.appendChild(qrWrap);

    // "More questions" button (hidden until first interaction)
    showQrBtn.style.display = 'none';
    msgs.appendChild(showQrBtn);

    // Input row
    const row = document.createElement('div');
    Object.assign(row.style, {
      display:'flex', borderTop:'1px solid var(--ferrari-hairline, #303030)'
    });
    // Variabile inp: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const inp = document.createElement('input');
    Object.assign(inp.style, {
      flex:'1', padding:'10px 14px', border:'none', outline:'none',
      background:'var(--ferrari-canvas-elevated,#303030)',
      color:'var(--ferrari-ink,#fff)', fontSize:'13px'
    });
    inp.placeholder = 'Scrivi un messaggio...';
    // Variabile send: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const send = document.createElement('button');
    Object.assign(send.style, {
      padding:'10px 16px', border:'none', cursor:'pointer',
      background:'#da291c', color:'#fff', fontSize:'14px'
    });
    send.innerHTML = '<i class="fa-solid fa-paper-plane"></i>';
    row.appendChild(inp);
    row.appendChild(send);
    panel.appendChild(row);

    // Ticket form (hidden)
    const tf = document.createElement('div');
    Object.assign(tf.style, { display:'none', flexDirection:'column', gap:'6px', padding:'10px 14px', borderTop:'1px solid var(--ferrari-hairline,#303030)' });
    // Variabile tSub: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const tSub = document.createElement('input');
    Object.assign(tSub.style, { padding:'7px 10px', background:'var(--ferrari-canvas-elevated,#303030)', color:'var(--ferrari-ink,#fff)', border:'1px solid var(--ferrari-hairline,#303030)', fontSize:'12px', outline:'none' });
    tSub.placeholder = 'Oggetto (opzionale)';
    // Variabile tMsg: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const tMsg = document.createElement('textarea');
    Object.assign(tMsg.style, { padding:'7px 10px', background:'var(--ferrari-canvas-elevated,#303030)', color:'var(--ferrari-ink,#fff)', border:'1px solid var(--ferrari-hairline,#303030)', fontSize:'12px', outline:'none', resize:'none', fontFamily:'inherit', height:'50px' });
    tMsg.placeholder = 'Descrivi il problema...';
    // Variabile tBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const tBtn = document.createElement('button');
    Object.assign(tBtn.style, { padding:'8px', border:'none', cursor:'pointer', background:'#da291c', color:'#fff', fontSize:'12px', fontWeight:'600', textTransform:'uppercase' });
    tBtn.textContent = 'Invia Ticket';
    tf.appendChild(tSub);
    tf.appendChild(tMsg);
    tf.appendChild(tBtn);

    // Insert ticket form AFTER messages area
    panel.insertBefore(tf, qrWrap);

    document.documentElement.appendChild(fab);
    document.documentElement.appendChild(panel);

    // ── Events ──────────────────────────────────────
// Funzione scroll: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
    function scroll() { setTimeout(() => { msgs.scrollTop = msgs.scrollHeight; }, 50); }
    // Funzione addMsg: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function addMsg(text, cls) {
      const d = document.createElement('div');
      d.style.cssText = cls === 'user'
        ? 'max-width:97%;align-self:flex-end;padding:10px 12px;background:#da291c;color:#fff;font-size:13px;line-height:1.5;border-radius:8px 8px 4px 8px'
        : 'max-width:97%;align-self:flex-start;padding:10px 12px;background:var(--ferrari-canvas-elevated,#303030);color:var(--ferrari-ink,#fff);font-size:13px;line-height:1.5;border-radius:8px 8px 8px 4px';
      d.innerHTML = text;
      msgs.appendChild(d);
      scroll();
    }

    // Funzione handleStockClick: gestisce un evento o una risposta utente. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function handleStockClick(item) {
      addMsg(item.q, 'user');
      addMsg(item.a, 'bot');
      // Show "altre domande" button
      showQrBtn.style.display = 'inline-block';
    }

    send.onclick = async () => {
      // Variabile txt: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const txt = inp.value.trim();
      if (!txt) return;
      addMsg(txt, 'user');
      inp.value = '';
      // Hide chips on custom message
      qrWrap.style.display = 'none';
      // Show "domande frequenti" button
      showQrBtn.style.display = 'inline-block';
      try {
        // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
        const r = await fetch(API_BASE + '/api/chat', {
          method:'POST', headers:{'Content-Type':'application/json'},
          body: JSON.stringify({ message: txt, failedAttempts: failed })
        });
        const d = await r.json();
        if (!d.isResolved) failed++; else failed = 0;
        addMsg(d.reply, 'bot');
        if (d.showTicketButton) {
          tf.style.display = 'flex';
          addMsg('Se preferisci, invia un ticket qui sotto ⬇️', 'bot');
        }
      } catch { addMsg('⚠️ Errore di connessione. Riprova.', 'bot'); }
    };
    inp.onkeydown = e => { if (e.key === 'Enter') send.onclick(); };

    tBtn.onclick = async () => {
      const m = tMsg.value.trim();
      if (!m) { addMsg('⚠️ Descrivi il problema prima di inviare.', 'bot'); return; }
      tBtn.disabled = true; tBtn.textContent = 'Invio...';
      try {
        // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
        const r = await fetch(API_BASE + '/api/tickets', {
          method:'POST', headers:{'Content-Type':'application/json'},
          body: JSON.stringify({ oggetto: tSub.value.trim(), messaggio: m, emailContatto: '' })
        });
        if (r.ok) { addMsg('✅ Ticket inviato! Ti contatteremo.', 'bot'); tf.style.display = 'none'; failed = 0; }
        else addMsg('⚠️ Errore invio ticket.', 'bot');
      } catch { addMsg('⚠️ Errore di connessione.', 'bot'); }
      tBtn.disabled = false; tBtn.textContent = 'Invia Ticket';
    };
  }

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
