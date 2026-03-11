// Basic UI glue for the redesigned dashboard and CRUD
async function apiGet(path){ const res = await fetch(path); if(!res.ok) throw new Error('API error '+res.status); return res.json(); }
async function apiPost(path, body){ const res = await fetch(path,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)}); if(!res.ok) { const txt = await res.text(); throw new Error(txt||res.status); } return res.json(); }

async function loadRegisti(){ const data = await apiGet('/registi'); const tbody = document.querySelector('#registi-table tbody'); tbody.innerHTML = data.map(r=>`<tr><td>${r.id}</td><td>${r.nome}</td><td>${r.cognome}</td><td>${r.nazionalita||''}</td><td><button class="btn" onclick="delReg(${r.id})">Elimina</button></td></tr>`).join(''); const sel = document.getElementById('film-regista'); if(sel) sel.innerHTML = data.map(r=>`<option value="${r.id}">${r.nome} ${r.cognome}</option>`).join(''); }
async function delReg(id){ await fetch('/registi/'+id,{method:'DELETE'}); await refreshAll(); }

document.getElementById('regista-form').addEventListener('submit', async e=>{ e.preventDefault(); const body={nome:document.getElementById('reg-nome').value,cognome:document.getElementById('reg-cognome').value,nazionalita:document.getElementById('reg-nazionalita').value}; await apiPost('/registi',body); await refreshAll(); });

document.getElementById('film-form').addEventListener('submit', async e=>{ e.preventDefault(); const body={titolo:document.getElementById('film-titolo').value,dataProduzione:document.getElementById('film-data').value?new Date(document.getElementById('film-data').value).toISOString():null,durata:parseInt(document.getElementById('film-durata').value||'0')}; const regId = parseInt(document.getElementById('film-regista').value||'0'); const res = await fetch('/registi/'+regId+'/films',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)}); if(!res.ok){alert('Errore');return;} await refreshAll(); });

document.getElementById('cinema-form').addEventListener('submit', async e=>{ e.preventDefault(); const body={nome:document.getElementById('cinema-nome').value,indirizzo:document.getElementById('cinema-indirizzo').value,citta:document.getElementById('cinema-citta').value}; await apiPost('/cinemas',body); await refreshAll(); });

document.getElementById('proiezione-form').addEventListener('submit', async e=>{ e.preventDefault(); const body={filmId:parseInt(document.getElementById('proj-film').value||'0'),cinemaId:parseInt(document.getElementById('proj-cinema').value||'0'),data:document.getElementById('proj-data').value?new Date(document.getElementById('proj-data').value).toISOString():null,ora:document.getElementById('proj-ora').value?new Date('1970-01-01T'+document.getElementById('proj-ora').value).toISOString():null}; const res = await fetch('/proiezioni',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)}); if(!res.ok){const t=await res.text();alert('Errore: '+t);return;} alert('Proiezione creata'); await refreshAll(); });

async function loadFilms(){ const data = await apiGet('/films'); document.querySelector('#films-table tbody').innerHTML = data.map(f=>`<tr><td>${f.id}</td><td>${f.titolo}</td><td>${new Date(f.dataProduzione).toLocaleDateString()}</td><td>${f.durata}</td><td>${f.registaId}</td><td><button class="btn" onclick="delFilm(${f.id})">Elimina</button></td></tr>`).join(''); const pf = document.getElementById('proj-film'); if(pf) pf.innerHTML = data.map(f=>`<option value="${f.id}">${f.titolo}</option>`).join(''); }
async function delFilm(id){ await fetch('/films/'+id,{method:'DELETE'}); await refreshAll(); }

async function loadCinemas(){ const data = await apiGet('/cinemas'); document.querySelector('#cinemas-table tbody').innerHTML = data.map(c=>`<tr><td>${c.id}</td><td>${c.nome}</td><td>${c.indirizzo}</td><td>${c.citta}</td><td><button class="btn" onclick="delCinema(${c.id})">Elimina</button></td></tr>`).join(''); const pc = document.getElementById('proj-cinema'); if(pc) pc.innerHTML = data.map(c=>`<option value="${c.id}">${c.nome}</option>`).join(''); }
async function delCinema(id){ await fetch('/cinemas/'+id,{method:'DELETE'}); await refreshAll(); }

async function loadProiezioni(){ const data = await apiGet('/proiezioni'); document.querySelector('#proiezioni-table tbody').innerHTML = data.map(p=>`<tr><td>${p.id}</td><td>${p.filmTitolo}</td><td>${p.cinemaNome}</td><td>${new Date(p.data).toLocaleDateString()}</td><td>${new Date(p.ora).toLocaleTimeString()}</td><td><button class="btn" onclick="delPro(${p.id})">Elimina</button></td></tr>`).join(''); }
async function delPro(id){ await fetch('/proiezioni/'+id,{method:'DELETE'}); await refreshAll(); }

async function refreshAll(){ await Promise.all([loadRegisti(), loadFilms(), loadCinemas(), loadProiezioni()]); }

window.addEventListener('load', ()=>{ refreshAll(); });
