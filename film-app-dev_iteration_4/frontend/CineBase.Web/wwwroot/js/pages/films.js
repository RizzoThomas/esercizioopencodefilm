// Films Page JavaScript
let allFilms = [];
let allRegisti = [];
let allCategorie = [];
let isUploading = false;
let currentPage = 1;
const pageSize = 10;
let totalPages = 1;
let totalFilmsCount = 0;
let currentSearch = '';
let currentGenre = 'all';

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

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

async function loadFilms() {
  const tableBody = document.getElementById('films-table-body');
  if (!tableBody) return;
  
  try {
    const response = await API.getFilms({
      page: currentPage,
      pageSize,
      search: currentSearch || undefined
    });

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
    tableBody.innerHTML = '<tr><td colspan="7" class="px-6 py-4 text-center text-brand-error">Errore nel caricamento dei film</td></tr>';
    renderPagination(0);
  }
}

function normalizePagedFilms(data) {
  if (Array.isArray(data) || Array.isArray(data?.$values)) {
    const items = normalizeCollection(data);
    return {
      items,
      page: 1,
      pageSize: items.length || pageSize,
      totalCount: items.length,
      totalPages: 1
    };
  }

  const items = normalizeCollection(data?.items ?? data?.Items ?? []);
  const resolvedPage = Number(data?.page ?? data?.Page ?? 1);
  const resolvedPageSize = Number(data?.pageSize ?? data?.PageSize ?? pageSize);
  const resolvedTotalCount = Number(data?.totalCount ?? data?.TotalCount ?? items.length);
  const resolvedTotalPages = Number(data?.totalPages ?? data?.TotalPages ?? 1);

  return {
    items,
    page: Number.isFinite(resolvedPage) && resolvedPage > 0 ? resolvedPage : 1,
    pageSize: Number.isFinite(resolvedPageSize) && resolvedPageSize > 0 ? resolvedPageSize : pageSize,
    totalCount: Number.isFinite(resolvedTotalCount) && resolvedTotalCount >= 0 ? resolvedTotalCount : items.length,
    totalPages: Number.isFinite(resolvedTotalPages) && resolvedTotalPages > 0 ? resolvedTotalPages : 1
  };
}

function applyGenreFilter(films) {
  if (!currentGenre || currentGenre === 'all') return films;
  return films.filter(f => {
    if (!f.categorie || !Array.isArray(f.categorie)) return false;
    return f.categorie.some(c => String(c.id) === currentGenre || c.nome?.toLowerCase() === currentGenre.toLowerCase());
  });
}

async function loadRegistiList() {
  try {
    allRegisti = normalizeCollection(await API.getRegisti());
  } catch (error) {
    console.error('Error loading registi:', error);
  }
}

async function loadCategorieList() {
  try {
    allCategorie = normalizeCollection(await API.getCategorie());
  } catch (error) {
    console.error('Error loading categorie:', error);
  }
}

function populateRegistiSelect() {
  const select = document.getElementById('regista-select');
  if (!select) return;

  select.innerHTML = '<option value="">Seleziona regista</option>';
  allRegisti.forEach(regista => {
    const option = document.createElement('option');
    option.value = String(regista.id);
    option.textContent = `${regista.nome} ${regista.cognome}`;
    select.appendChild(option);
  });
}

function populateCategorieCheckboxes() {
  const container = document.getElementById('categorie-checkboxes');
  if (!container) return;

  container.innerHTML = allCategorie.map(cat => `
    <label class="inline-flex items-center gap-1 cursor-pointer">
      <input type="checkbox" name="categoria" value="${cat.id}" class="w-4 h-4 rounded border-brand-outline text-brand-gold focus:ring-brand-gold">
      <span class="text-sm text-brand-on-surface">${cat.nome}</span>
    </label>
  `).join('');
}

function renderFilms(films) {
  const tableBody = document.getElementById('films-table-body');
  if (!tableBody) return;
  
if (!films.length) {
        tableBody.innerHTML = '<tr><td colspan="8" class="px-6 py-4 text-center text-brand-on-surface-variant">Nessun film trovato</td></tr>';
        return;
    }

    tableBody.innerHTML = films.map(film => {
      const categorie = film.categorie || [];
      const categorieBadges = categorie.length
        ? categorie.map(c => `<span class="inline-block bg-brand-surface-container text-brand-on-surface text-xs px-1.5 py-0.5 rounded mr-1">${c.nome}</span>`).join('')
        : '<span class="text-brand-on-surface-variant text-xs">-</span>';
      return `
        <tr class="row-hover">
            <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface-variant">${film.id}</td>
            <td class="px-6 py-4 whitespace-nowrap">
                <div class="h-10 w-8 flex-shrink-0 bg-brand-surface-container rounded overflow-hidden">
                    <img class="h-full w-full object-cover" src="${film.copertinaPath?.startsWith('/media/') ? `http://localhost:5000${film.copertinaPath}` : (film.copertinaPath || '/assets/images/defaults/cover-default.jpg')}" alt="${film.titolo}">
                </div>
            </td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-brand-on-surface">${film.titolo}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface-variant">${formatDate(film.dataProduzione)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface-variant">${getRegistaName(film)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface-variant">${film.durata || '-'} min</td>
      <td class="px-6 py-4 whitespace-nowrap">${categorieBadges}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
        <button onclick="editFilm(${film.id})" class="text-brand-gold hover:text-brand-gold-dark mr-3">
          <i class="fa-solid fa-pencil"></i>
        </button>
        <button onclick="deleteFilm(${film.id}, '${escapeHtml(film.titolo)}')" class="text-red-600 hover:text-red-900">
          <i class="fa-solid fa-trash"></i>
        </button>
      </td>
    </tr>
  `}).join('');
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

 function getRegistaName(film) {
  if (film.registaNome || film.registaCognome) {
    return `${film.registaNome || ''} ${film.registaCognome || ''}`.trim();
  }

  const regista = allRegisti.find(r => Number(r.id) === Number(film.registaId));
  return regista ? `${regista.nome} ${regista.cognome}` : `ID ${film.registaId}`;
}

function updateStats(totalCount, films) {
  const totalFilmsEl = document.getElementById('total-films');
  if (totalFilmsEl) totalFilmsEl.textContent = String(totalCount);

  const newReleases = films.filter(f => f.dataUscita && new Date(f.dataUscita) > new Date(Date.now() - 90 * 24 * 60 * 60 * 1000)).length;
  const newReleasesEl = document.getElementById('new-releases');
  if (newReleasesEl) newReleasesEl.textContent = String(newReleases);
}

function setupFilters() {
  const searchInput = document.getElementById('search-input');
  const categoriaFilter = document.getElementById('categoria-filter');
  
  populateCategoriaFilter();

  searchInput?.addEventListener('input', async (e) => {
    currentSearch = (e.target.value || '').trim();
    currentPage = 1;
    await loadFilms();
  });

  categoriaFilter?.addEventListener('change', async (e) => {
    currentGenre = e.target.value || 'all';
    currentPage = 1;
    await loadFilms();
  });
}

function populateCategoriaFilter() {
  const select = document.getElementById('categoria-filter');
  if (!select || !allCategorie.length) return;

  select.innerHTML = '<option value="all">Tutte le Categorie</option>';
  allCategorie.forEach(cat => {
    const option = document.createElement('option');
    option.value = String(cat.id);
    option.textContent = cat.nome;
    select.appendChild(option);
  });
}

function renderPagination(serverItemsCount) {
  const paginationInfo = document.getElementById('pagination-info');
  const pageIndicator = document.getElementById('page-indicator');
  const firstBtn = document.getElementById('pagination-first');
  const prevBtn = document.getElementById('pagination-prev');
  const nextBtn = document.getElementById('pagination-next');
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

  const startItem = ((currentPage - 1) * pageSize) + 1;
  const endItem = Math.min(currentPage * pageSize, totalFilmsCount);

  paginationInfo.textContent = `Mostrando ${startItem}-${endItem} di ${totalFilmsCount} film`;
  pageIndicator.textContent = `Pagina ${currentPage} di ${totalPages}`;

  firstBtn.disabled = currentPage <= 1;
  prevBtn.disabled = currentPage <= 1;
  nextBtn.disabled = currentPage >= totalPages;
  lastBtn.disabled = currentPage >= totalPages;
}

async function goToPage(page) {
  if (page < 1 || page > totalPages || page === currentPage) return;
  currentPage = page;
  await loadFilms();
}

function goToFirstPage() {
  return goToPage(1);
}

function goToPrevPage() {
  return goToPage(currentPage - 1);
}

function goToNextPage() {
  return goToPage(currentPage + 1);
}

function goToLastPage() {
  return goToPage(totalPages);
}

function setupFormSubmit() {
    const form = document.getElementById('film-form');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        if (isUploading) return;

        const submitBtn = document.querySelector('#film-modal button[form="film-form"]');
        const originalBtnText = submitBtn?.innerHTML;
        const copertinaFile = document.getElementById('copertina-file')?.files[0];
        const copertinaPathInput = document.getElementById('copertina-path');

        try {
            let copertinaPath = copertinaPathInput?.value || '';

            if (copertinaFile) {
                isUploading = true;
                if (submitBtn) {
                    submitBtn.disabled = true;
                    submitBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-2"></i>Caricamento...';
                }

                try {
                    const uploadResult = await API.uploadCover(copertinaFile);
                    copertinaPath = uploadResult.path;
                    if (copertinaPathInput) copertinaPathInput.value = copertinaPath;
                } catch (uploadError) {
                    handleApiError(uploadError);
                    return;
                }
            }

            const data = serializeForm('film-form');
            data.copertinaPath = copertinaPath;
            if (data.registaId) data.registaId = Number(data.registaId);
            if (data.durata) data.durata = Number(data.durata);
            delete data.copertinaFile;
            delete data.categoria;

            const selectedCats = Array.from(form.querySelectorAll('input[name="categoria"]:checked'))
              .map(cb => Number(cb.value));
            if (selectedCats.length > 0) {
              data.categorieIds = selectedCats;
            } else {
              data.categorieIds = [];
            }

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

async function editFilm(id) {
    try {
        const film = await API.getFilm(id);
        openModal('film-modal', 'Modifica Film');

        const form = document.getElementById('film-form');
        form.dataset.editId = id;

        form.querySelector('[name="titolo"]').value = film.titolo || '';
        form.querySelector('[name="dataProduzione"]').value = formatDateForInput(film.dataProduzione);
        form.querySelector('[name="durata"]').value = film.durata || '';
        form.querySelector('[name="registaId"]').value = film.registaId || '';
        form.querySelector('[name="filmatoPath"]').value = film.filmatoPath || '';
        
        const copertinaPathInput = document.getElementById('copertina-path');
        if (copertinaPathInput) copertinaPathInput.value = film.copertinaPath || '';
        
        const copertinaFileInput = document.getElementById('copertina-file');
        if (copertinaFileInput) copertinaFileInput.value = '';

        const filmCats = film.categorie || [];
        const filmCatIds = filmCats.map(c => Number(c.id));
        form.querySelectorAll('input[name="categoria"]').forEach(cb => {
          cb.checked = filmCatIds.includes(Number(cb.value));
        });
    } catch (error) {
        handleApiError(error);
    }
}

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
