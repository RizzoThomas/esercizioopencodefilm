// Home Page JavaScript
document.addEventListener('DOMContentLoaded', async () => {
  await loadFilms();
});

async function loadFilms() {
  const filmsGrid = document.getElementById('films-grid');
  if (!filmsGrid) return;
  
  try {
    const films = await API.getFilms();
    renderFilms(films);
  } catch (error) {
    handleApiError(error);
    filmsGrid.innerHTML = '<p class="text-white">Errore nel caricamento dei film</p>';
  }
}

function renderFilms(films) {
  const filmsGrid = document.getElementById('films-grid');
  if (!filmsGrid || !films.length) {
    filmsGrid.innerHTML = '<p class="text-white col-span-full text-center">Nessun film in programmazione</p>';
    return;
  }
  
  filmsGrid.innerHTML = films.map(film => `
    <div class="bg-brand-dark-card rounded-2xl overflow-hidden border border-white/10 group transition-all hover:border-brand-orange/50">
      <div class="aspect-[2/3] bg-slate-700 relative overflow-hidden">
        <img src="${film.locandina || '/assets/images/defaults/cover-default.jpg'}" 
             alt="${film.titolo}" 
             class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300">
        <div class="absolute inset-0 bg-gradient-to-t from-black/80 via-transparent to-transparent"></div>
        <div class="absolute bottom-3 left-3 right-3">
          <span class="bg-brand-orange text-xs font-bold px-2 py-1 rounded">${film.genere || 'Film'}</span>
        </div>
      </div>
      <div class="p-4">
        <h3 class="text-white font-semibold text-lg mb-1 truncate">${film.titolo}</h3>
        <p class="text-gray-400 text-sm mb-3">${film.regista ? film.regista.nome + ' ' + film.regista.cognome : 'Regista sconosciuto'}</p>
        <div class="flex items-center justify-between">
          <span class="text-gray-400 text-sm"><i class="fa-regular fa-clock mr-1"></i>${film.durata || '-'} min</span>
          <button class="bg-brand-orange hover:bg-brand-orange-dark text-white px-4 py-2 rounded-lg text-sm font-medium transition-colors">
            Prenota
          </button>
        </div>
      </div>
    </div>
  `).join('');
}
