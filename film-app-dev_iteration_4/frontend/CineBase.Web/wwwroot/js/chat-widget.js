/**
 * CineBase Chat Widget v3 — Minimal, robust, fixed overlay
 * + Quick replies with stock answers
 */
(function () {
  'use strict';
  if (document.getElementById('cb-chat-root')) return;

  const API_BASE = window.API_BASE_URL || 'http://localhost:5000';
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

    // ── Drag Physics ─────────────────────────────
    var fabOffsetX = 24, fabOffsetY = 24; // current offset from right/bottom edges
    var dragStartX = 0, dragStartY = 0;
    var dragStartOffsetX = 0, dragStartOffsetY = 0;
    var isDragging = false;
    var hasMoved = false;
    var velX = 0, velY = 0;
    var physicsId = null;

    function updateFabPosition() {
      fab.style.right = fabOffsetX + 'px';
      fab.style.bottom = fabOffsetY + 'px';
    }

    function clampToViewport() {
      var maxX = window.innerWidth - 56 - 8;
      var maxY = window.innerHeight - 56 - 8;
      fabOffsetX = Math.max(8, Math.min(maxX, fabOffsetX));
      fabOffsetY = Math.max(8, Math.min(maxY, fabOffsetY));
    }

    function startPhysics() {
      if (physicsId) cancelAnimationFrame(physicsId);
      var friction = 0.92;
      var edgeSnap = 4; // pixels from edge to snap
      var snapSpeed = 0.15;

      function step() {
        velX *= friction;
        velY *= friction;

        fabOffsetX -= velX;
        fabOffsetY -= velY;

        // Bounce off edges
        var minX = 8, maxX = window.innerWidth - 56 - 8;
        var minY = 8, maxY = window.innerHeight - 56 - 8;

        if (fabOffsetX < minX) { fabOffsetX = minX; velX *= -0.4; }
        if (fabOffsetX > maxX) { fabOffsetX = maxX; velX *= -0.4; }
        if (fabOffsetY < minY) { fabOffsetY = minY; velY *= -0.4; }
        if (fabOffsetY > maxY) { fabOffsetY = maxY; velY *= -0.4; }

        // Snap to nearest edge when slow
        if (Math.abs(velX) < 0.3 && Math.abs(velY) < 0.3) {
          // Snap to nearest horizontal edge
          if (fabOffsetX < (maxX - minX) / 2) {
            fabOffsetX += (minX - fabOffsetX) * snapSpeed;
          } else {
            fabOffsetX += (maxX - fabOffsetX) * snapSpeed;
          }
          // Mild vertical snap too
          if (fabOffsetY < (maxY - minY) / 2) {
            fabOffsetY += (minY - fabOffsetY) * snapSpeed * 0.5;
          } else {
            fabOffsetY += (maxY - fabOffsetY) * snapSpeed * 0.5;
          }

          if (Math.abs(velX) < 0.01 && Math.abs(velY) < 0.01 &&
              Math.abs(fabOffsetX - minX) < 0.5 || Math.abs(fabOffsetX - maxX) < 0.5) {
            fabOffsetX = fabOffsetX < (maxX - minX) / 2 ? minX : maxX;
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

    fab.addEventListener('pointerdown', function(e) {
      fab.setPointerCapture(e.pointerId);
      dragStartX = e.clientX;
      dragStartY = e.clientY;
      dragStartOffsetX = fabOffsetX;
      dragStartOffsetY = fabOffsetY;
      isDragging = true;
      hasMoved = false;
      velX = 0; velY = 0;
      if (physicsId) { cancelAnimationFrame(physicsId); physicsId = null; }
      fab.style.cursor = 'grabbing';
      fab.style.transition = 'none';
      e.preventDefault();
    });

    fab.addEventListener('pointermove', function(e) {
      if (!isDragging) return;
      var dx = dragStartX - e.clientX;
      var dy = dragStartY - e.clientY;
      if (Math.abs(dx) > 3 || Math.abs(dy) > 3) hasMoved = true;
      if (!hasMoved) return;

      // Calculate velocity (smoothed)
      velX = velX * 0.5 + dx * 0.5;
      velY = velY * 0.5 + dy * 0.5;

      fabOffsetX = dragStartOffsetX + dx;
      fabOffsetY = dragStartOffsetY + dy;
      clampToViewport();
      updateFabPosition();
    });

    fab.addEventListener('pointerup', function(e) {
      isDragging = false;
      fab.style.cursor = 'grab';
      if (!hasMoved) {
        // It was a click
        fabOffsetX = dragStartOffsetX;
        fabOffsetY = dragStartOffsetY;
        updateFabPosition();
        // Trigger click handler
        open = !open;
        if (open) {
          panel.style.opacity = '1';
          panel.style.visibility = 'visible';
          panel.style.pointerEvents = 'all';
          panel.style.transform = 'translateY(0) scale(1)';
          fab.innerHTML = '<span style="position:relative;z-index:10;pointer-events:none"><i class="fa-solid fa-times"></i></span>';
          inp.focus();
          qrWrap.style.display = 'flex';
          showQrBtn.style.display = 'none';
        } else {
          panel.style.opacity = '0';
          panel.style.visibility = 'hidden';
          panel.style.pointerEvents = 'none';
          panel.style.transform = 'translateY(20px) scale(0.95)';
          fab.innerHTML = '<span style="position:relative;z-index:10;pointer-events:none"><i class="fa-solid fa-message"></i></span>';
        }
        return;
      }
      // Start physics
      startPhysics();
    });

    fab.addEventListener('pointercancel', function() {
      isDragging = false;
      fab.style.cursor = 'grab';
      if (hasMoved) startPhysics();
    });

    // Chat panel
    const panel = document.createElement('div');
    panel.setAttribute('style',
      'position:fixed!important;bottom:88px!important;right:24px!important;z-index:2147483646!important;' +
      'width:370px;max-width:calc(100vw - 48px);height:440px;max-height:calc(100vh - 130px);' +
      'background:var(--ferrari-canvas,#181818);border:1px solid var(--ferrari-hairline,#303030);' +
      'display:flex;flex-direction:column;' +
      'transform:translateY(20px) scale(0.95);opacity:0;visibility:hidden;pointer-events:none;' +
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
    const inp = document.createElement('input');
    Object.assign(inp.style, {
      flex:'1', padding:'10px 14px', border:'none', outline:'none',
      background:'var(--ferrari-canvas-elevated,#303030)',
      color:'var(--ferrari-ink,#fff)', fontSize:'13px'
    });
    inp.placeholder = 'Scrivi un messaggio...';
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
    const tSub = document.createElement('input');
    Object.assign(tSub.style, { padding:'7px 10px', background:'var(--ferrari-canvas-elevated,#303030)', color:'var(--ferrari-ink,#fff)', border:'1px solid var(--ferrari-hairline,#303030)', fontSize:'12px', outline:'none' });
    tSub.placeholder = 'Oggetto (opzionale)';
    const tMsg = document.createElement('textarea');
    Object.assign(tMsg.style, { padding:'7px 10px', background:'var(--ferrari-canvas-elevated,#303030)', color:'var(--ferrari-ink,#fff)', border:'1px solid var(--ferrari-hairline,#303030)', fontSize:'12px', outline:'none', resize:'none', fontFamily:'inherit', height:'50px' });
    tMsg.placeholder = 'Descrivi il problema...';
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
    fab.onclick = () => {
      open = !open;
      if (open) {
        panel.style.opacity = '1';
        panel.style.visibility = 'visible';
        panel.style.pointerEvents = 'all';
        panel.style.transform = 'translateY(0) scale(1)';
        fab.innerHTML = '<i class="fa-solid fa-times"></i>';
        inp.focus();
        // Reset chips visibility
        qrWrap.style.display = 'flex';
        showQrBtn.style.display = 'none';
      } else {
        panel.style.opacity = '0';
        panel.style.visibility = 'hidden';
        panel.style.pointerEvents = 'none';
        panel.style.transform = 'translateY(20px) scale(0.95)';
        fab.innerHTML = '<i class="fa-solid fa-message"></i>';
      }
    };

    function scroll() { setTimeout(() => { msgs.scrollTop = msgs.scrollHeight; }, 50); }
    function addMsg(text, cls) {
      const d = document.createElement('div');
      d.style.cssText = cls === 'user'
        ? 'max-width:97%;align-self:flex-end;padding:10px 12px;background:#da291c;color:#fff;font-size:13px;line-height:1.5;border-radius:8px 8px 4px 8px'
        : 'max-width:97%;align-self:flex-start;padding:10px 12px;background:var(--ferrari-canvas-elevated,#303030);color:var(--ferrari-ink,#fff);font-size:13px;line-height:1.5;border-radius:8px 8px 8px 4px';
      d.innerHTML = text;
      msgs.appendChild(d);
      scroll();
    }

    function handleStockClick(item) {
      addMsg(item.q, 'user');
      addMsg(item.a, 'bot');
      // Show "altre domande" button
      showQrBtn.style.display = 'inline-block';
    }

    send.onclick = async () => {
      const txt = inp.value.trim();
      if (!txt) return;
      addMsg(txt, 'user');
      inp.value = '';
      // Hide chips on custom message
      qrWrap.style.display = 'none';
      // Show "domande frequenti" button
      showQrBtn.style.display = 'inline-block';
      try {
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

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
