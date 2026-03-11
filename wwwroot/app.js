const apiBase = '';

function escapeHtml(s){ return (s||'').replace(/[&<>\"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c])); }

function fmtDate(d){ if(!d) return ''; const date = new Date(d); return date.toLocaleDateString(); }

async function refreshRegisti(){
  const res = await fetch('/registi');
  const data = await res.json();
  const t = document.querySelector('#registi-table tbody');
  t.innerHTML = data.map(r=>`<tr><td>${r.id}</td><td>${escapeHtml(r.nome)}</td><td>${escapeHtml(r.cognome)}</td><td>${escapeHtml(r.nazionalita||'')}</td><td><button onclick="delReg(${r.id})">Elimina</button></td></tr>`).join('');
  const sel = document.getElementById('film-regista'); sel.innerHTML = data.map(r=>`<option value="${r.id}">${escapeHtml(r.nome)} ${escapeHtml(r.cognome)}</option>`).join('');
}

async function refreshFilms(){
  const res = await fetch('/films'); const data = await res.json();
  const t = document.querySelector('#films-table tbody');
  t.innerHTML = data.map(f=>`<tr><td>${f.id}</td><td>${escapeHtml(f.titolo)}</td><td>${fmtDate(f.dataProduzione)}</td><td>${f.durata}</td><td>${f.registaId}</td><td></td></tr>`).join('');
  const pf = document.getElementById('proj-film'); pf.innerHTML = data.map(f=>`<option value="${f.id}">${escapeHtml(f.titolo)}</option>`).join('');
}

async function refreshCinemas(){ const res = await fetch('/cinemas'); const data = await res.json(); document.querySelector('#cinemas-table tbody').innerHTML = data.map(c=>`<tr><td>${c.id}</td><td>${escapeHtml(c.nome)}</td><td>${escapeHtml(c.indirizzo||'')}</td><td>${escapeHtml(c.citta||'')}</td></tr>`).join('');
  document.getElementById('proj-cinema').innerHTML = data.map(c=>`<option value="${c.id}">${escapeHtml(c.nome)}</option>`).join(''); }

async function refreshProiezioni(){
  const res = await fetch('/proiezioni');
  if(!res.ok){ document.querySelector('#proiezioni-table tbody').innerHTML = '<tr><td colspan="5" class="muted">Errore caricamento proiezioni</td></tr>'; return; }
  const data = await res.json();
  document.querySelector('#proiezioni-table tbody').innerHTML = data.map(p=>`<tr><td>${p.id}</td><td>${escapeHtml(p.filmTitolo)}</td><td>${escapeHtml(p.cinemaNome)}</td><td>${fmtDate(p.data)}</td><td>${fmtDate(p.ora)}</td></tr>`).join('');
}

async function delReg(id){ if(!confirm('Eliminare regista '+id+'?')) return; await fetch(`/registi/${id}`,{method:'DELETE'}); refreshAll(); }

document.getElementById('regista-form').addEventListener('submit', async e=>{ e.preventDefault();
  const body = { nome: document.getElementById('reg-nome').value, cognome: document.getElementById('reg-cognome').value, nazionalita: document.getElementById('reg-nazionalita').value };
  const res = await fetch('/registi',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});
  if(!res.ok){ alert('Errore creazione regista'); return; } await refreshAll();
});

document.getElementById('film-form').addEventListener('submit', async e=>{ e.preventDefault();
  const body = { titolo: document.getElementById('film-titolo').value, dataProduzione: document.getElementById('film-data').value ? new Date(document.getElementById('film-data').value).toISOString() : null, durata: parseInt(document.getElementById('film-durata').value||'0'), registaId: parseInt(document.getElementById('film-regista').value||'0') };
  // POST to /registi/{id}/films to assign regista
  const res = await fetch(`/registi/${body.registaId}/films`,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});
  if(!res.ok){ alert('Errore creazione film'); return; } await refreshAll();
});

document.getElementById('cinema-form').addEventListener('submit', async e=>{ e.preventDefault(); const body = { nome: document.getElementById('cinema-nome').value, indirizzo: document.getElementById('cinema-indirizzo').value, citta: document.getElementById('cinema-citta').value }; const res = await fetch('/cinemas',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)}); if(!res.ok){ alert('Errore creazione cinema'); return; } await refreshAll(); });

document.getElementById('proiezione-form').addEventListener('submit', async e=>{ e.preventDefault(); const body = { filmId: parseInt(document.getElementById('proj-film').value||'0'), cinemaId: parseInt(document.getElementById('proj-cinema').value||'0'), data: document.getElementById('proj-data').value ? new Date(document.getElementById('proj-data').value).toISOString() : null, ora: document.getElementById('proj-ora').value ? new Date('1970-01-01T'+document.getElementById('proj-ora').value).toISOString() : null }; const res = await fetch('/proiezioni',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)}); if(!res.ok){ const t=await res.text(); alert('Errore proiezione: '+t); return; } alert('Proiezione creata'); refreshAll(); });

async function refreshAll(){ await Promise.all([refreshRegisti(), refreshFilms(), refreshCinemas(), refreshProiezioni()]); }

refreshAll();
