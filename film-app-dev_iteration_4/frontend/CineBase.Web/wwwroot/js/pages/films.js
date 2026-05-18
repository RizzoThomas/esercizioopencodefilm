// Films Page JavaScript
let allFilms = [];
// Variabile allRegisti: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let allRegisti = [];
// Variabile allCategorie: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let allCategorie = [];
// Variabile isUploading: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let isUploading = false;
// Variabile currentPage: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let currentPage = 1;
// Variabile pageSize: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const pageSize = 10;
// Variabile totalPages: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let totalPages = 1;
// Variabile totalFilmsCount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let totalFilmsCount = 0;
// Variabile currentSearch: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let currentSearch = '';
// Variabile currentGenre: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let currentGenre = 'all';

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', async () => {
  await Promise.all([
    loadRegistiList(),
    loadCategorieList()
  ]);
  populateRegistiSelect();
  populateCategorieCheckboxes();
  setupFilters();
  setupFormSubmit();
  await loadFilms();
});

// Funzione normalizeCollection: normalizza il valore in ingresso per confronti stabili. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

// Funzione loadFilms: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadFilms() {
  // Variabile tableBody: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tableBody = document.getElementById('films-table-body');
  if (!tableBody) return;
  
  try {
    // Variabile response: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const response = await API.getFilms({
      page: currentPage,
      pageSize,
      search: currentSearch || undefined
    });

    // Variabile paged: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const paged = normalizePagedFilms(response);
    totalPages = paged.totalPages;
    totalFilmsCount = paged.totalCount;
    currentPage = paged.page;

    allFilms = applyGenreFilter(paged.items);
    renderFilms(allFilms);
    updateStats(totalFilmsCount, allFilms);
    renderPagination(allFilms.length);
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="7" class="px-6 py-4 text-center text-ferrari-semantic-warning">Errore nel caricamento dei film</td></tr>';
    renderPagination(0);
  }
}

// Funzione normalizePagedFilms: normalizza il valore in ingresso per confronti stabili. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function normalizePagedFilms(data) {
  if (Array.isArray(data) || Array.isArray(data?.$values)) {
    // Variabile items: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const items = normalizeCollection(data);
    return {
      items,
      page: 1,
      pageSize: items.length || pageSize,
      totalCount: items.length,
      totalPages: 1
    };
  }

  // Variabile items: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const items = normalizeCollection(data?.items ?? data?.Items ?? []);
  // Variabile resolvedPage: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const resolvedPage = Number(data?.page ?? data?.Page ?? 1);
  // Variabile resolvedPageSize: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const resolvedPageSize = Number(data?.pageSize ?? data?.PageSize ?? pageSize);
  // Variabile resolvedTotalCount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const resolvedTotalCount = Number(data?.totalCount ?? data?.TotalCount ?? items.length);
  // Variabile resolvedTotalPages: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const resolvedTotalPages = Number(data?.totalPages ?? data?.TotalPages ?? 1);

  return {
    items,
    page: Number.isFinite(resolvedPage) && resolvedPage > 0 ? resolvedPage : 1,
    pageSize: Number.isFinite(resolvedPageSize) && resolvedPageSize > 0 ? resolvedPageSize : pageSize,
    totalCount: Number.isFinite(resolvedTotalCount) && resolvedTotalCount >= 0 ? resolvedTotalCount : items.length,
    totalPages: Number.isFinite(resolvedTotalPages) && resolvedTotalPages > 0 ? resolvedTotalPages : 1
  };
}

// Funzione applyGenreFilter: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function applyGenreFilter(films) {
  if (!currentGenre || currentGenre === 'all') return films;
  return films.filter(f => {
    if (!f.categorie || !Array.isArray(f.categorie)) return false;
    return f.categorie.some(c => String(c.id) === currentGenre || c.nome?.toLowerCase() === currentGenre.toLowerCase());
  });
}

// Funzione loadRegistiList: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadRegistiList() {
  try {
    allRegisti = normalizeCollection(await API.getRegisti());
  } catch (error) {
    console.error('Error loading registi:', error);
  }
}

// Funzione loadCategorieList: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadCategorieList() {
  try {
    allCategorie = normalizeCollection(await API.getCategorie());
  } catch (error) {
    console.error('Error loading categorie:', error);
  }
}

// Funzione populateRegistiSelect: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function populateRegistiSelect() {
  // Variabile select: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const select = document.getElementById('regista-select');
  if (!select) return;

  select.innerHTML = '<option value="">Seleziona regista</option>';
  allRegisti.forEach(regista => {
    // Variabile option: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const option = document.createElement('option');
    option.value = String(regista.id);
    option.textContent = `${regista.nome} ${regista.cognome}`;
    select.appendChild(option);
  });
}

// Funzione populateCategorieCheckboxes: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function populateCategorieCheckboxes() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('categorie-checkboxes');
  if (!container) return;

  container.innerHTML = allCategorie.map(cat => `
    <label class="inline-flex items-center gap-1 cursor-pointer">
      <input type="checkbox" name="categoria" value="${cat.id}" class="w-4 h-4 rounded border-hairline text-ferrari-primary focus:ring-ferrari-primary">
      <span class="text-sm text-ink">${cat.nome}</span>
    </label>
  `).join('');
}

// Funzione renderFilms: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderFilms(films) {
  // Variabile tableBody: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tableBody = document.getElementById('films-table-body');
  if (!tableBody) return;
  
if (!films.length) {
        tableBody.innerHTML = '<tr><td colspan="8" class="px-6 py-4 text-center text-body">Nessun film trovato</td></tr>';
        return;
    }

    tableBody.innerHTML = films.map(film => {
      // Variabile categorie: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const categorie = film.categorie || [];
      // Variabile categorieBadges: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const categorieBadges = categorie.length
        ? categorie.map(c => `<span class="inline-block bg-canvas-elevated text-ink text-xs px-1.5 py-0.5 rounded mr-1">${c.nome}</span>`).join('')
        : '<span class="text-body text-xs">-</span>';
      return `
        <tr class="row-hover">
            <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${film.id}</td>
            <td class="px-6 py-4 whitespace-nowrap">
                <div class="h-10 w-8 flex-shrink-0 bg-canvas-elevated rounded overflow-hidden">
                    <img class="h-full w-full object-cover" src="${film.copertinaPath?.startsWith('/media/') ? `http://localhost:5000${film.copertinaPath}` : (film.copertinaPath || '/assets/images/defaults/cover-default.jpg')}" alt="${film.titolo}">
                </div>
            </td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-ink">${film.titolo}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${formatDate(film.dataProduzione)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${getRegistaName(film)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${film.durata || '-'} min</td>
      <td class="px-6 py-4 whitespace-nowrap">${categorieBadges}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
        <button onclick="editFilm(${film.id})" class="text-ferrari-primary hover:text-ferrari-primary-hover mr-3">
          <i class="fa-solid fa-pencil"></i>
        </button>
        <button onclick="deleteFilm(${film.id}, '${escapeHtml(film.titolo)}')" class="text-red-600 hover:text-red-900">
          <i class="fa-solid fa-trash"></i>
        </button>
      </td>
    </tr>
  `}).join('');
}

// Funzione escapeHtml: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function escapeHtml(text) {
  // Variabile div: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

 // Funzione getRegistaName: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
 function getRegistaName(film) {
  if (film.registaNome || film.registaCognome) {
    return `${film.registaNome || ''} ${film.registaCognome || ''}`.trim();
  }

  // Variabile regista: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const regista = allRegisti.find(r => Number(r.id) === Number(film.registaId));
  return regista ? `${regista.nome} ${regista.cognome}` : `ID ${film.registaId}`;
}

// Funzione updateStats: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function updateStats(totalCount, films) {
  // Variabile totalFilmsEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const totalFilmsEl = document.getElementById('total-films');
  if (totalFilmsEl) totalFilmsEl.textContent = String(totalCount);

  // Variabile newReleases: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const newReleases = films.filter(f => f.dataUscita && new Date(f.dataUscita) > new Date(Date.now() - 90 * 24 * 60 * 60 * 1000)).length;
  // Variabile newReleasesEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const newReleasesEl = document.getElementById('new-releases');
  if (newReleasesEl) newReleasesEl.textContent = String(newReleases);
}

// Funzione setupFilters: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupFilters() {
  // Variabile searchInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const searchInput = document.getElementById('search-input');
  // Variabile categoriaFilter: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const categoriaFilter = document.getElementById('categoria-filter');
  
  populateCategoriaFilter();

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  searchInput?.addEventListener('input', async (e) => {
    currentSearch = (e.target.value || '').trim();
    currentPage = 1;
    await loadFilms();
  });

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  categoriaFilter?.addEventListener('change', async (e) => {
    currentGenre = e.target.value || 'all';
    currentPage = 1;
    await loadFilms();
  });
}

// Funzione populateCategoriaFilter: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function populateCategoriaFilter() {
  // Variabile select: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const select = document.getElementById('categoria-filter');
  if (!select || !allCategorie.length) return;

  select.innerHTML = '<option value="all">Tutte le Categorie</option>';
  allCategorie.forEach(cat => {
    // Variabile option: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const option = document.createElement('option');
    option.value = String(cat.id);
    option.textContent = cat.nome;
    select.appendChild(option);
  });
}

// Funzione renderPagination: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderPagination(serverItemsCount) {
  // Variabile paginationInfo: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const paginationInfo = document.getElementById('pagination-info');
  // Variabile pageIndicator: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const pageIndicator = document.getElementById('page-indicator');
  // Variabile firstBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const firstBtn = document.getElementById('pagination-first');
  // Variabile prevBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const prevBtn = document.getElementById('pagination-prev');
  // Variabile nextBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const nextBtn = document.getElementById('pagination-next');
  // Variabile lastBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const lastBtn = document.getElementById('pagination-last');

  if (!paginationInfo || !pageIndicator || !firstBtn || !prevBtn || !nextBtn || !lastBtn) return;

  if (totalFilmsCount === 0 || serverItemsCount === 0) {
    paginationInfo.textContent = 'Nessun risultato';
    pageIndicator.textContent = 'Pagina 1 di 1';
    firstBtn.disabled = true;
    prevBtn.disabled = true;
    nextBtn.disabled = true;
    lastBtn.disabled = true;
    return;
  }

  // Variabile startItem: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const startItem = ((currentPage - 1) * pageSize) + 1;
  // Variabile endItem: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const endItem = Math.min(currentPage * pageSize, totalFilmsCount);

  paginationInfo.textContent = `Mostrando ${startItem}-${endItem} di ${totalFilmsCount} film`;
  pageIndicator.textContent = `Pagina ${currentPage} di ${totalPages}`;

  firstBtn.disabled = currentPage <= 1;
  prevBtn.disabled = currentPage <= 1;
  nextBtn.disabled = currentPage >= totalPages;
  lastBtn.disabled = currentPage >= totalPages;
}

// Funzione goToPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function goToPage(page) {
  if (page < 1 || page > totalPages || page === currentPage) return;
  currentPage = page;
  await loadFilms();
}

// Funzione goToFirstPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToFirstPage() {
  return goToPage(1);
}

// Funzione goToPrevPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToPrevPage() {
  return goToPage(currentPage - 1);
}

// Funzione goToNextPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToNextPage() {
  return goToPage(currentPage + 1);
}

// Funzione goToLastPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToLastPage() {
  return goToPage(totalPages);
}

// Funzione setupFormSubmit: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupFormSubmit() {
    // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const form = document.getElementById('film-form');
    if (!form) return;

    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        if (isUploading) return;

        // Variabile submitBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const submitBtn = document.querySelector('#film-modal button[form="film-form"]');
        // Variabile originalBtnText: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const originalBtnText = submitBtn?.innerHTML;
        // Variabile copertinaFile: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const copertinaFile = document.getElementById('copertina-file')?.files[0];
        // Variabile copertinaPathInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const copertinaPathInput = document.getElementById('copertina-path');

        try {
            // Variabile copertinaPath: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            let copertinaPath = copertinaPathInput?.value || '';

            if (copertinaFile) {
                isUploading = true;
                if (submitBtn) {
                    submitBtn.disabled = true;
                    submitBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-2"></i>Caricamento...';
                }

                try {
                    // Variabile uploadResult: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
                    const uploadResult = await API.uploadCover(copertinaFile);
                    copertinaPath = uploadResult.path;
                    if (copertinaPathInput) copertinaPathInput.value = copertinaPath;
                } catch (uploadError) {
                    handleApiError(uploadError);
                    return;
                }
            }

            // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const data = serializeForm('film-form');
            data.copertinaPath = copertinaPath;
            if (data.registaId) data.registaId = Number(data.registaId);
            if (data.durata) data.durata = Number(data.durata);
            delete data.copertinaFile;
            delete data.categoria;

            // Variabile selectedCats: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const selectedCats = Array.from(form.querySelectorAll('input[name="categoria"]:checked'))
              .map(cb => Number(cb.value));
            if (selectedCats.length > 0) {
              data.categorieIds = selectedCats;
            } else {
              data.categorieIds = [];
            }

            // Variabile editId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const editId = form.dataset.editId;

            try {
                if (editId) {
                    await API.updateFilm(editId, data);
                    showToast('Film aggiornato con successo');
                } else {
                    await API.createFilm(data);
                    showToast('Film creato con successo');
                }

                closeModal('film-modal');
                await loadFilms();
            } catch (error) {
                handleApiError(error);
            }
        } finally {
            isUploading = false;
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalBtnText || 'Salva';
            }
        }
    });
}

// Funzione editFilm: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function editFilm(id) {
    try {
        // Variabile film: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const film = await API.getFilm(id);
        openModal('film-modal', 'Modifica Film');

        // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const form = document.getElementById('film-form');
        form.dataset.editId = id;

        form.querySelector('[name="titolo"]').value = film.titolo || '';
        form.querySelector('[name="dataProduzione"]').value = formatDateForInput(film.dataProduzione);
        form.querySelector('[name="durata"]').value = film.durata || '';
        form.querySelector('[name="registaId"]').value = film.registaId || '';
        form.querySelector('[name="filmatoPath"]').value = film.filmatoPath || '';
        
        // Variabile copertinaPathInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const copertinaPathInput = document.getElementById('copertina-path');
        if (copertinaPathInput) copertinaPathInput.value = film.copertinaPath || '';
        
        // Variabile copertinaFileInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const copertinaFileInput = document.getElementById('copertina-file');
        if (copertinaFileInput) copertinaFileInput.value = '';

        // Variabile filmCats: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const filmCats = film.categorie || [];
        // Variabile filmCatIds: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const filmCatIds = filmCats.map(c => Number(c.id));
        form.querySelectorAll('input[name="categoria"]').forEach(cb => {
          cb.checked = filmCatIds.includes(Number(cb.value));
        });
    } catch (error) {
        handleApiError(error);
    }
}

// Funzione deleteFilm: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function deleteFilm(id, title) {
  openDeleteModal(title, async () => {
    try {
      await API.deleteFilm(id);
      showToast('Film eliminato con successo');
      await loadFilms();
    } catch (error) {
      handleApiError(error);
    }
  });
}

window.goToPage = goToPage;
window.goToFirstPage = goToFirstPage;
window.goToPrevPage = goToPrevPage;
window.goToNextPage = goToNextPage;
window.goToLastPage = goToLastPage;
