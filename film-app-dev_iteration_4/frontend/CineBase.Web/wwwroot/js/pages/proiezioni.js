// Proiezioni Page JavaScript
let allCinemas = [];
let allFilms = [];
let currentPage = 1;
const pageSize = 10;
let totalPages = 1;
let totalProiezioniCount = 0;
let currentSearch = '';

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

document.addEventListener('DOMContentLoaded', async () => {
  bindSearch();
  await Promise.all([loadCinemasList(), loadFilmsList()]);
  populateSelects();
  await loadProiezioni();
  setupFormSubmit();
});

function normalizePaged(data) {
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

async function loadProiezioni() {
  const tableBody = document.getElementById('proiezioni-table-body');
  if (!tableBody) return;
  
  try {
    const response = await API.getProiezioni({
      page: currentPage,
      pageSize,
      search: currentSearch || undefined
    });

    const paged = normalizePaged(response);
    totalPages = paged.totalPages;
    totalProiezioniCount = paged.totalCount;
    currentPage = paged.page;

    renderProiezioni(paged.items);
    updateStats(totalProiezioniCount);
    renderPagination(paged.items.length);
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="7" class="px-6 py-4 text-center text-brand-error">Errore nel caricamento delle proiezioni</td></tr>';
    renderPagination(0);
  }
}

async function loadCinemasList() {
  try {
    allCinemas = normalizeCollection(await API.getCinemas());
  } catch (error) {
    console.error('Error loading cinemas:', error);
  }
}

async function loadFilmsList() {
  try {
    allFilms = normalizeCollection(await API.getFilms());
  } catch (error) {
    console.error('Error loading films:', error);
  }
}

function populateSelects() {
  populateSelect('cinema-select', allCinemas, 'id', ['nome'], 'Seleziona Cinema');
  populateSelect('film-select', allFilms, 'id', ['titolo'], 'Seleziona Film');
}

function renderProiezioni(proiezioni) {
  const tableBody = document.getElementById('proiezioni-table-body');
  if (!tableBody) return;
  
  if (!proiezioni.length) {
    tableBody.innerHTML = '<tr><td colspan="7" class="px-6 py-4 text-center text-brand-on-surface-variant">Nessuna proiezione trovata</td></tr>';
    return;
  }
  
  tableBody.innerHTML = proiezioni.map(proiezione => `
    <tr class="row-hover">
      <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface-variant">${proiezione.id}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface">${getCinemaLabel(proiezione)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface">${getFilmLabel(proiezione)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface-variant">${formatDate(proiezione.data)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface-variant">${formatTime(proiezione.ora)}</td>
      <td class="px-6 py-4 whitespace-nowrap">
        ${renderProiezioneStatus(proiezione)}
      </td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
        <button onclick="editProiezione(${proiezione.id})" class="text-brand-gold hover:text-brand-gold-dark mr-3">
          <i class="fa-solid fa-pencil"></i>
        </button>
        <button onclick="deleteProiezione(${proiezione.id}, '${getFilmTitle(proiezione.filmId)}')" class="text-red-600 hover:text-red-900">
          <i class="fa-solid fa-trash"></i>
        </button>
      </td>
    </tr>
  `).join('');
}

function getStatusMeta(proiezione) {
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());

  let proiezioneDate = null;
  if (proiezione.data) {
    const datePart = String(proiezione.data).split('T')[0];
    const [year, month, day] = datePart.split('-').map(Number);
    if (year && month && day) {
      proiezioneDate = new Date(year, month - 1, day);
    }
  }

  const isPast = proiezioneDate && proiezioneDate < today;
  return {
    className: isPast ? 'chip-past' : 'chip-active',
    text: isPast ? 'Passata' : 'In programma'
  };
}

function renderProiezioneStatus(proiezione) {
  const status = getStatusMeta(proiezione);
  return `<span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${status.className}">${status.text}</span>`;
}

function getCinemaLabel(proiezione) {
  const cinemaName = proiezione?.cinema?.nome || getCinemaName(proiezione.cinemaId);
  if (cinemaName.startsWith('ID ')) {
    return `<span class="text-brand-on-surface-variant text-[11px] font-normal opacity-70">${cinemaName}</span>`;
  }
  return `${cinemaName} <span class="ml-1 text-[11px] font-normal text-brand-on-surface-variant opacity-70">(ID ${proiezione.cinemaId})</span>`;
}

function getFilmLabel(proiezione) {
  const filmTitle = proiezione?.film?.titolo || getFilmTitle(proiezione.filmId);
  if (filmTitle.startsWith('ID ')) {
    return `<span class="text-brand-on-surface-variant text-[11px] font-normal opacity-70">${filmTitle}</span>`;
  }
  return `${filmTitle} <span class="ml-1 text-[11px] font-normal text-brand-on-surface-variant opacity-70">(ID ${proiezione.filmId})</span>`;
}

function getCinemaName(cinemaId) {
  const cinema = allCinemas.find(c => Number(c.id) === Number(cinemaId));
  return cinema ? cinema.nome : `ID ${cinemaId}`;
}

function getFilmTitle(filmId) {
  const film = allFilms.find(f => Number(f.id) === Number(filmId));
  return film ? film.titolo : `ID ${filmId}`;
}

function updateStats(totalCount) {
  const totalProiezioniEl = document.getElementById('total-proiezioni');
  if (totalProiezioniEl) totalProiezioniEl.textContent = String(totalCount);
}

function bindSearch() {
  const searchInput = document.getElementById('search-input');
  if (!searchInput) return;

  searchInput.addEventListener('input', async (e) => {
    currentSearch = (e.target.value || '').trim();
    currentPage = 1;
    await loadProiezioni();
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

  if (totalProiezioniCount === 0 || serverItemsCount === 0) {
    paginationInfo.textContent = 'Nessun risultato';
    pageIndicator.textContent = 'Pagina 1 di 1';
    firstBtn.disabled = true;
    prevBtn.disabled = true;
    nextBtn.disabled = true;
    lastBtn.disabled = true;
    return;
  }

  const startItem = ((currentPage - 1) * pageSize) + 1;
  const endItem = Math.min(currentPage * pageSize, totalProiezioniCount);

  paginationInfo.textContent = `Mostrando ${startItem}-${endItem} di ${totalProiezioniCount} proiezioni`;
  pageIndicator.textContent = `Pagina ${currentPage} di ${totalPages}`;

  firstBtn.disabled = currentPage <= 1;
  prevBtn.disabled = currentPage <= 1;
  nextBtn.disabled = currentPage >= totalPages;
  lastBtn.disabled = currentPage >= totalPages;
}

async function goToPage(page) {
  if (page < 1 || page > totalPages || page === currentPage) return;
  currentPage = page;
  await loadProiezioni();
}

function goToFirstPage() {
  goToPage(1);
}

function goToPrevPage() {
  goToPage(currentPage - 1);
}

function goToNextPage() {
  goToPage(currentPage + 1);
}

function goToLastPage() {
  goToPage(totalPages);
}

function setupFormSubmit() {
  const form = document.getElementById('proiezione-form');
  if (!form) return;
  
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const data = serializeForm('proiezione-form');
    delete data.postiTotali;
    if (data.cinemaId) data.cinemaId = Number(data.cinemaId);
    if (data.filmId) data.filmId = Number(data.filmId);

    if (data.data && data.ora) {
      data.ora = `${data.data}T${data.ora}:00`;
    }

    const editId = form.dataset.editId;
    
    try {
      if (editId) {
        await API.updateProiezione(editId, data);
        showToast('Proiezione aggiornata con successo');
      } else {
        await API.createProiezione(data);
        showToast('Proiezione creata con successo');
      }
      
      closeModal('proiezione-modal');
      loadProiezioni();
    } catch (error) {
      handleApiError(error);
    }
  });
}

async function editProiezione(id) {
  try {
    const proiezione = await API.getProiezione(id);
    
    if (!proiezione) {
      showToast('Proiezione non trovata', 'danger');
      return;
    }
    
    openModal('proiezione-modal', 'Modifica Proiezione');

    const form = document.getElementById('proiezione-form');
    if (!form) return;
    
    form.dataset.editId = id;

    const cinemaIdInput = form.querySelector('[name="cinemaId"]');
    const filmIdInput = form.querySelector('[name="filmId"]');
    const dataInput = form.querySelector('[name="data"]');
    const oraInput = form.querySelector('[name="ora"]');

    if (cinemaIdInput) cinemaIdInput.value = proiezione.cinemaId || '';
    if (filmIdInput) filmIdInput.value = proiezione.filmId || '';
    if (dataInput) dataInput.value = formatDateForInput(proiezione.data);
    if (oraInput) oraInput.value = formatTime(proiezione.ora);
  } catch (error) {
    console.error('Error in editProiezione:', error);
    handleApiError(error);
  }
}

async function deleteProiezione(id, title) {
  openDeleteModal(title, async () => {
    try {
      await API.deleteProiezione(id);
      showToast('Proiezione eliminata con successo');
      loadProiezioni();
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
