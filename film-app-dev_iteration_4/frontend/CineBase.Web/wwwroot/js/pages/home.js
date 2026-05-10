// Home Page JavaScript
let featuredInterval;
let currentFeaturedIndex = 0;
let featuredEntries = [];

document.addEventListener("DOMContentLoaded", async () => {
  const params = new URLSearchParams(window.location.search);
  if (params.get("forbidden") === "true") {
    showToast("Non hai i permessi per accedere all'area admin", "warning");
    params.delete("forbidden");
    const newQuery = params.toString();
    const newUrl = `${window.location.pathname}${newQuery ? `?${newQuery}` : ""}`;
    window.history.replaceState({}, "", newUrl);
  }

  await loadFeaturedFilms();
});

async function loadFeaturedFilms() {
  const featuredGrid = document.getElementById("featured-grid");
  if (!featuredGrid) return;

  try {
    const [filmsResponse, proiezioniResponse] = await Promise.all([
      API.getFilms({ page: 1, pageSize: 100 }),
      API.getProiezioni()
    ]);

    const films = Array.isArray(filmsResponse)
      ? filmsResponse
      : Array.isArray(filmsResponse?.items)
        ? filmsResponse.items
        : Array.isArray(filmsResponse?.$values)
          ? filmsResponse.$values
          : [];

    const proiezioni = Array.isArray(proiezioniResponse)
      ? proiezioniResponse
      : Array.isArray(proiezioniResponse?.items)
        ? proiezioniResponse.items
        : Array.isArray(proiezioniResponse?.$values)
          ? proiezioniResponse.$values
          : [];

    const featured = buildFeaturedSelection(films, proiezioni);
    initFeaturedFilms(featured);
  } catch (error) {
    handleApiError(error);
    featuredGrid.innerHTML =
      '<p class="text-ink col-span-full text-center">Errore nel caricamento dei film in evidenza</p>';
  }
}

function buildFeaturedSelection(films, proiezioni) {
  const next7Days = new Date();
  next7Days.setDate(next7Days.getDate() + 7);

  const upcoming = proiezioni.filter((p) => {
    const date = new Date(p.data || p.ora);
    return Number.isFinite(date.getTime()) && date >= new Date() && date <= next7Days;
  });

  const countByFilm = new Map();
  upcoming.forEach((p) => {
    const filmId = Number(p.filmId);
    countByFilm.set(filmId, (countByFilm.get(filmId) || 0) + 1);
  });

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


function getDirectorName(film) {
  const flatName = [film?.registaNome, film?.registaCognome]
    .filter(Boolean)
    .join(" ")
    .trim();
  if (flatName) return flatName;

  const nestedName = [film?.regista?.nome, film?.regista?.cognome]
    .filter(Boolean)
    .join(" ")
    .trim();
  return nestedName || "Regista sconosciuto";
}

function initFeaturedFilms(entries) {
  featuredEntries = entries;

  // Hide skeleton, show grid
  const skeleton = document.getElementById('featured-skeleton');
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

window.addEventListener("resize", () => {
  if (!featuredEntries.length) return;
  updateFeaturedDisplay(currentFeaturedIndex);
});

function updateFeaturedDisplay(activeIndex) {
  const featuredGrid = document.getElementById("featured-grid");
  if (!featuredGrid) return;

  const heroEntry = featuredEntries[activeIndex];
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

function renderHeroCard(film, score) {
  const badge = score > 0 ? "Top della Settimana" : "Nuovo Arrivo";
  const subBadge = score > 0 ? `${score} proiezioni` : "";
  const categorie = film.categorie || [];
  const badgeHtml = categorie.length
    ? categorie.map(c => `<span class="bg-canvas/80 backdrop-blur-sm text-ink text-xs px-2 py-0.5 rounded">${c.nome}</span>`).join('')
    : `<span class="bg-ferrari-primary text-xs font-bold px-2 py-1 rounded">${film.genere || "Film"}</span>`;
  
  const isLoggedIn = typeof Auth !== 'undefined' && Auth?.isLoggedIn?.() || false;
  const cta = isLoggedIn
    ? `<a href="/programmazione.html" class="btn-gold shadow-lg transform transition-transform hover:scale-105">Vai alla Programmazione</a>`
    : `<a href="/programmazione.html" class="btn-outline-brand-light transform transition-transform hover:scale-105 backdrop-blur-sm">Scopri Orari</a>`;

  return `
    <div class="focus-card card-ferrari overflow-hidden group transition-all lg:col-span-2 relative w-full max-w-full h-[118vw] min-h-[420px] max-h-[780px] lg:h-[930px] lg:max-h-none animate-fade-in card-glow-border tilt-card">
      <div class="tilt-card-shine"></div>
      <div class="absolute inset-0 bg-slate-950">
        <img src="${getCoverImage(film.copertinaPath)}"
             alt="${film.titolo}"
              class="w-full h-full object-contain sm:object-cover object-top group-hover:scale-105 transition-transform duration-700 ease-out opacity-90 glitch-hover">
        <!-- Gradiente scuro sul fondo per leggibilita migliorata anche col tema chiaro -->
        <div class="absolute inset-0 bg-gradient-to-t from-gray-950 via-gray-950/30 to-transparent lg:via-transparent lg:bg-gradient-to-r lg:from-gray-950 lg:to-transparent opacity-50"></div>
        <div class="absolute inset-0 bg-gradient-to-t from-gray-950/50 via-gray-950/20 to-transparent opacity-40"></div>
      </div>
      
      <div class="absolute top-4 left-4 right-4 flex items-center justify-between z-10">
        <span class="bg-ferrari-primary text-sm font-bold px-3 py-1 rounded shadow-md text-ink">${badge}</span>
        ${subBadge ? `<span class="bg-black/60 backdrop-blur-md text-white text-xs px-2 py-1 rounded border border-white/10">${subBadge}</span>` : ""}
      </div>

      <div class="absolute bottom-0 left-0 right-0 p-6 lg:p-10 z-10 flex flex-col justify-end h-full">
        <div class="flex flex-wrap gap-2 mb-3">
          ${badgeHtml}
        </div>
        <h3 class="text-white font-bold text-3xl lg:text-5xl mb-3 drop-shadow-xl leading-tight line-clamp-2">${film.titolo}</h3>
        <p class="text-gray-100 text-xl mb-6 flex items-center gap-4 font-medium drop-shadow-lg">
          <span><i class="fa-solid fa-video mr-2 text-ferrari-primary"></i>${getDirectorName(film)}</span>
          ${film.durata ? `<span><i class="fa-regular fa-clock mr-1"></i>${film.durata} min</span>` : ""}
        </p>
        <div class="flex">
          ${cta}
        </div>
      </div>
    </div>
  `;
}

function renderCompactCard(film, score, originalIndex) {
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
