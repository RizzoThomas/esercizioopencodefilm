// Home Page JavaScript
let featuredInterval;
// Variabile currentFeaturedIndex: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let currentFeaturedIndex = 0;
// Variabile featuredEntries: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let featuredEntries = [];

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener("DOMContentLoaded", async () => {
  // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const params = new URLSearchParams(window.location.search);
  if (params.get("forbidden") === "true") {
    showToast("Non hai i permessi per accedere all'area admin", "warning");
    params.delete("forbidden");
    // Variabile newQuery: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const newQuery = params.toString();
    // Variabile newUrl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const newUrl = `${window.location.pathname}${newQuery ? `?${newQuery}` : ""}`;
    window.history.replaceState({}, "", newUrl);
  }

  await loadFeaturedFilms();
});

// Funzione loadFeaturedFilms: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadFeaturedFilms() {
  // Variabile featuredGrid: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const featuredGrid = document.getElementById("featured-grid");
  if (!featuredGrid) return;

  try {
    const [filmsResponse, proiezioniResponse] = await Promise.all([
      API.getFilms({ page: 1, pageSize: 100 }),
      API.getProiezioni()
    ]);

    // Variabile films: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const films = Array.isArray(filmsResponse)
      ? filmsResponse
      : Array.isArray(filmsResponse?.items)
        ? filmsResponse.items
        : Array.isArray(filmsResponse?.$values)
          ? filmsResponse.$values
          : [];

    // Variabile proiezioni: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const proiezioni = Array.isArray(proiezioniResponse)
      ? proiezioniResponse
      : Array.isArray(proiezioniResponse?.items)
        ? proiezioniResponse.items
        : Array.isArray(proiezioniResponse?.$values)
          ? proiezioniResponse.$values
          : [];

    // Variabile featured: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const featured = buildFeaturedSelection(films, proiezioni);
    initFeaturedFilms(featured);
  } catch (error) {
    handleApiError(error);
    featuredGrid.innerHTML =
      '<p class="text-ink col-span-full text-center">Errore nel caricamento dei film in evidenza</p>';
  }
}

// Funzione buildFeaturedSelection: costruisce una struttura dati o una selezione ordinata per la UI. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function buildFeaturedSelection(films, proiezioni) {
  // Variabile next7Days: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const next7Days = new Date();
  next7Days.setDate(next7Days.getDate() + 7);

  // Variabile upcoming: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const upcoming = proiezioni.filter((p) => {
    // Variabile date: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const date = new Date(p.data || p.ora);
    return Number.isFinite(date.getTime()) && date >= new Date() && date <= next7Days;
  });

  // Variabile countByFilm: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const countByFilm = new Map();
  upcoming.forEach((p) => {
    // Variabile filmId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const filmId = Number(p.filmId);
    countByFilm.set(filmId, (countByFilm.get(filmId) || 0) + 1);
  });

  // Variabile filmsWithScore: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const filmsWithScore = films
    .map((film) => ({
      film,
      score: countByFilm.get(Number(film.id)) || 0,
      releaseDate: new Date(film.dataProduzione || 0)
    }))
    .sort((a, b) => {
      // Priorita' ai film in programmazione rispetto a quelli non programmati
      if (b.score !== a.score) return b.score - a.score;
      // Parita' di programmazione: piu' recenti
      return b.releaseDate - a.releaseDate;
    });

  // Prendi i top 5 per riempire il grid (1 hero + 4 compatti)
  return filmsWithScore.slice(0, 5);
}


// Funzione getDirectorName: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getDirectorName(film) {
  // Variabile flatName: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const flatName = [film?.registaNome, film?.registaCognome]
    .filter(Boolean)
    .join(" ")
    .trim();
  if (flatName) return flatName;

  // Variabile nestedName: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const nestedName = [film?.regista?.nome, film?.regista?.cognome]
    .filter(Boolean)
    .join(" ")
    .trim();
  return nestedName || "Regista sconosciuto";
}

// Funzione initFeaturedFilms: inizializza stato, timer o interfaccia della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function initFeaturedFilms(entries) {
  featuredEntries = entries;

  // Hide skeleton, show grid
  const skeleton = document.getElementById('featured-skeleton');
  // Variabile grid: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const grid = document.getElementById('featured-grid');
  if (skeleton) skeleton.classList.add('hidden');
  if (grid) grid.classList.remove('hidden');

  if (!featuredEntries.length) {
    grid.innerHTML =
      '<p class="text-ink col-span-full text-center py-12">Nessun film disponibile</p>';
    return;
  }

  updateFeaturedDisplay(0);

  if (featuredInterval) clearInterval(featuredInterval);
  if (featuredEntries.length > 1) {
    featuredInterval = setInterval(() => {
      currentFeaturedIndex = (currentFeaturedIndex + 1) % featuredEntries.length;
      updateFeaturedDisplay(currentFeaturedIndex);
    }, 6000); // Cambia ogni 6 secondi
  }
}

window.setActiveFeatured = function (index) {
  if (featuredInterval) clearInterval(featuredInterval);
  currentFeaturedIndex = index;
  updateFeaturedDisplay(currentFeaturedIndex);

  // Riavvia l'intervallo
  if (featuredEntries.length > 1) {
    featuredInterval = setInterval(() => {
      currentFeaturedIndex = (currentFeaturedIndex + 1) % featuredEntries.length;
      updateFeaturedDisplay(currentFeaturedIndex);
    }, 6000);
  }
};

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
window.addEventListener("resize", () => {
  if (!featuredEntries.length) return;
  updateFeaturedDisplay(currentFeaturedIndex);
});

// Funzione updateFeaturedDisplay: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function updateFeaturedDisplay(activeIndex) {
  // Variabile featuredGrid: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const featuredGrid = document.getElementById("featured-grid");
  if (!featuredGrid) return;

  // Variabile heroEntry: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const heroEntry = featuredEntries[activeIndex];
  // Variabile sideEntries: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const sideEntries = featuredEntries.filter((_, idx) => idx !== activeIndex);
  featuredGrid.innerHTML = `
    ${renderHeroCard(heroEntry.film, heroEntry.score)}
    <div class="lg:col-span-1 flex flex-col gap-4 lg:gap-[18px] h-full">
      ${sideEntries.map((entry, idx) => {
        // Re-calcoliamo l'indice originale per il click handler
        const originalIndex = featuredEntries.indexOf(entry);
        return renderCompactCard(entry.film, entry.score, originalIndex);
      }).join("")}
    </div>
  `;
}

// Funzione renderHeroCard: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderHeroCard(film, score) {
  // Variabile badge: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const badge = score > 0 ? "Top della Settimana" : "Nuovo Arrivo";
  // Variabile subBadge: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const subBadge = score > 0 ? `${score} proiezioni` : "";
  // Variabile categorie: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const categorie = film.categorie || [];
  // Variabile badgeHtml: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const badgeHtml = categorie.length
    ? categorie.map(c => `<span class="bg-canvas/80 backdrop-blur-sm text-ink text-xs px-2 py-0.5 rounded">${c.nome}</span>`).join('')
    : `<span class="bg-ferrari-primary text-xs font-bold px-2 py-1 rounded">${film.genere || "Film"}</span>`;
  
  // Variabile isLoggedIn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const isLoggedIn = typeof Auth !== 'undefined' && Auth?.isLoggedIn?.() || false;
  // Variabile cta: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const cta = isLoggedIn
    ? `<a href="/programmazione.html" class="btn-primary text-sm">Vai alla Programmazione</a>`
    : `<a href="/programmazione.html" class="btn-outline text-sm">Scopri Orari</a>`;

  return `
    <div class="focus-card card-ferrari cine-card lg:col-span-2 relative w-full max-w-full h-[118vw] min-h-[420px] max-h-[780px] lg:h-[930px] lg:max-h-none animate-fade-in">
      <div class="absolute inset-0 bg-slate-950">
        <img src="${getCoverImage(film.copertinaPath)}"
             alt="${film.titolo}"
             class="w-full h-full object-cover object-top opacity-90">
        <div class="cine-card-overlay"></div>
      </div>

      <div class="absolute top-4 left-4 right-4 flex items-center justify-between z-10">
        <span class="bg-ferrari-primary text-sm font-bold px-3 py-1 rounded shadow-md text-white">${badge}</span>
        ${subBadge ? `<span class="bg-black/60 backdrop-blur-md text-white text-xs px-2 py-1 rounded border border-white/10">${subBadge}</span>` : ""}
      </div>

      <div class="cine-card-body absolute bottom-0 left-0 right-0 p-6 lg:p-10 z-10">
        <div class="flex flex-wrap gap-2 mb-3">
          ${badgeHtml}
        </div>
        <h3 class="text-white font-bold text-3xl lg:text-5xl mb-3 drop-shadow-xl leading-tight line-clamp-2">${film.titolo}</h3>
        <p class="text-gray-100 text-xl mb-6 flex items-center gap-4 font-medium drop-shadow-lg">
          <span><i class="fa-solid fa-video mr-2 text-ferrari-primary"></i>${getDirectorName(film)}</span>
          ${film.durata ? `<span><i class="fa-regular fa-clock mr-1"></i>${film.durata} min</span>` : ""}
        </p>
      </div>

      <div class="cine-card-action z-20">
        ${cta}
      </div>
    </div>
  `;
}

// Funzione renderCompactCard: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderCompactCard(film, score, originalIndex) {
  // Variabile badge: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const badge = score > 0 ? "In Programmazione" : "Novità";
  
  return `
    <div class="focus-card card-ferrari flex overflow-hidden group transition-all hover:ring-2 hover:ring-ferrari-primary/70 cursor-pointer animate-fade-in bg-canvas h-[180px] sm:h-[198px] lg:h-[213px]" onclick="window.location.href='/scheda-film.html?id=${film.id}'">
      <div class="w-[28%] lg:w-[32%] bg-slate-800 relative overflow-hidden flex-shrink-0">
        <img src="${getCoverImage(film.copertinaPath)}"
             alt="${film.titolo}"
             class="w-full h-full object-cover object-center group-hover:scale-110 transition-transform duration-500">
      </div>
      <div class="p-3 lg:p-4 flex flex-col justify-center flex-1 overflow-hidden lg:justify-between">
        <div class="flex justify-between items-start mb-1 lg:mb-2">
          <span class="text-[10px] lg:text-[11px] uppercase tracking-wider text-ferrari-primary font-bold truncate pr-1">${badge}</span>
          ${score > 0 ? `<span class="text-[10px] lg:text-[11px] text-body font-medium flex-shrink-0"><i class="fa-solid fa-calendar-day mr-1"></i>${score}</span>` : ""}
        </div>
        <h3 class="text-ink font-bold text-sm sm:text-base lg:text-[1.1rem] mb-1 line-clamp-2 group-hover:text-ferrari-primary transition-colors leading-tight">${film.titolo}</h3>
        <p class="text-body text-xs lg:text-[13px] font-medium truncate mt-auto"><i class="fa-solid fa-video text-[10px] mr-1 opacity-70"></i> ${getDirectorName(film)}</p>
      </div>
    </div>
  `;
}

window.handlePrenotaFilm = function(filmId) {
  window.location.href = `/programmazione.html`;
};

// ─── AI Recommendations ──────────────────────────────────
// Funzione loadRecommendations: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
async function loadRecommendations() {
  // Wait for auth
  var tries = 0;
  // Variabile auth: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  var auth = null;
  while (tries < 20) {
    auth = getAuthSafe();
    if (auth && typeof auth.isLoggedIn === 'function') break;
    await new Promise(r => setTimeout(r, 250));
    tries++;
  }

  if (!auth || !auth.isLoggedIn()) return;

  // Variabile section: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const section = document.getElementById('recommendations-section');
  if (!section) return;

  section.classList.remove('hidden');

  try {
    // Variabile token: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var token = auth.getAccessToken();
    if (!token) { section.classList.add('hidden'); return; }

    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    const response = await fetch((window.API_BASE_URL || 'http://localhost:5000') + '/recommendations', {
      headers: { 'Authorization': 'Bearer ' + token }
    });
    if (!response.ok) throw new Error('Not available');

    // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const data = await response.json();
    // Variabile items: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const items = data.items || [];
    // Variabile subtitle: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const subtitle = document.getElementById('recommendations-subtitle');
    if (subtitle && data.source === 'personalized') {
      subtitle.textContent = 'Suggerimenti basati sui tuoi gusti cinematografici';
    }

    // Variabile grid: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const grid = document.getElementById('recommendations-grid');
    if (items.length === 0) {
      grid.innerHTML = '<p class="text-body text-sm col-span-full text-center py-4">Nessun suggerimento al momento. Guarda qualche film per ricevere consigli personalizzati!</p>';
      return;
    }

    grid.innerHTML = items.slice(0, 5).map(f => `
      <div class="card-ferrari overflow-hidden group cursor-pointer"
           onclick="window.location.href='/scheda-film.html?id=${f.id}'">
        <div class="aspect-[2/3] overflow-hidden">
          <img src="${getCoverImage(f.copertina)}" alt="${f.titolo}"
            class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
            loading="lazy" decoding="async">
        </div>
        <div class="p-3">
          <h3 class="font-semibold text-sm text-ink group-hover:text-ferrari-primary transition-colors line-clamp-2">${f.titolo}</h3>
          <p class="text-xs text-body mt-1">${f.regista || '—'}</p>
          <p class="text-[10px] text-ferrari-primary mt-2 italic">✦ ${f.motivo}</p>
        </div>
      </div>
    `).join('');
  } catch (e) {
    // Backend not available or no data - hide gracefully
    section.classList.add('hidden');
  }
}

// Start loading after a delay so auth is ready
setTimeout(loadRecommendations, 1000);
