/**
 * CineBase Chat Widget v2 — Minimal, robust, fixed overlay
 */
(function () {
  'use strict';
  if (document.getElementById('cb-chat-root')) return;

  const API_BASE = window.API_BASE_URL || 'http://localhost:5000';
  let failed = 0, open = false;

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
    fab.setAttribute('style',
      'position:fixed!important;bottom:24px!important;right:24px!important;z-index:2147483647!important;' +
      'width:56px;height:56px;border-radius:50%!important;border:2px solid rgba(255,255,255,0.2)!important;cursor:pointer;' +
      'background:linear-gradient(135deg,rgba(218,41,28,0.9),rgba(180,30,20,0.9))!important;color:#fff;font-size:22px;' +
      'display:flex;align-items:center;justify-content:center;position:relative;overflow:visible;' +
      'box-shadow:0 0 20px rgba(218,41,28,0.7),0 0 40px rgba(180,30,20,0.5),0 0 60px rgba(150,20,10,0.3)!important;' +
      'transition:transform 0.3s,box-shadow 0.3s');
    
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
    iconWrap.setAttribute('style', 'position:relative;z-index:10');
    iconWrap.innerHTML = '<i class="fa-solid fa-message"></i>';
    fab.appendChild(iconWrap);
    
    fab.title = 'Assistenza CineBase';
    fab.onmouseenter = () => { fab.style.transform = 'scale(1.12) rotate(5deg)'; fab.style.boxShadow = '0 0 35px rgba(218,41,28,0.9),0 0 55px rgba(180,30,20,0.7),0 0 75px rgba(150,20,10,0.5)!important'; };
    fab.onmouseleave = () => { fab.style.transform = 'scale(1) rotate(0deg)'; fab.style.boxShadow = '0 0 20px rgba(218,41,28,0.7),0 0 40px rgba(180,30,20,0.5),0 0 60px rgba(150,20,10,0.3)!important'; };

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
    msgs.innerHTML = '<div style="max-width:85%;align-self:flex-start;padding:10px 12px;background:var(--ferrari-canvas-elevated,#303030);color:var(--ferrari-ink,#fff);font-size:13px;line-height:1.5">Ciao! 👋 Sono l\'assistente di <b>CineBase</b>. Chiedimi informazioni su programmazione, biglietti, offerte e altro!</div>';
    panel.appendChild(msgs);

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
    panel.appendChild(tf);

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
        ? 'max-width:85%;align-self:flex-end;padding:10px 12px;background:#da291c;color:#fff;font-size:13px;line-height:1.5'
        : 'max-width:85%;align-self:flex-start;padding:10px 12px;background:var(--ferrari-canvas-elevated,#303030);color:var(--ferrari-ink,#fff);font-size:13px;line-height:1.5';
      d.innerHTML = text;
      msgs.appendChild(d);
      scroll();
    }

    send.onclick = async () => {
      const txt = inp.value.trim();
      if (!txt) return;
      addMsg(txt, 'user');
      inp.value = '';
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
