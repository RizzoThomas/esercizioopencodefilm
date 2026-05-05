// Registi Page JavaScript
let allFilms = [];
let currentPage = 1;
const pageSize = 10;
let totalPages = 1;
let totalRegistiCount = 0;
let currentSearch = '';

document.addEventListener('DOMContentLoaded', async () => {
  bindSearch();
  setupFormSubmit();
  await loadRegisti();
});

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

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

async function loadRegisti() {
  const tableBody = document.getElementById('registi-table-body');
  if (!tableBody) return;

  try {
    const [registiResponse, filmsResponse] = await Promise.all([
      API.getRegisti({
        page: currentPage,
        pageSize,
        search: currentSearch || undefined
      }),
      API.getFilms()
    ]);

    const paged = normalizePaged(registiResponse);
    totalPages = paged.totalPages;
    totalRegistiCount = paged.totalCount;
    currentPage = paged.page;

    allFilms = normalizeCollection(filmsResponse);
    renderRegisti(paged.items);
    updateStats(totalRegistiCount, paged.items);
    renderPagination(paged.items.length);
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="6" class="px-6 py-4 text-center text-ferrari-semantic-warning">Errore nel caricamento dei registi</td></tr>';
    renderPagination(0);
  }
}

function renderRegisti(registi) {
  const tableBody = document.getElementById('registi-table-body');
  if (!tableBody) return;

  if (!registi.length) {
    tableBody.innerHTML = '<tr><td colspan="6" class="px-6 py-4 text-center text-body">Nessun regista trovato</td></tr>';
    return;
  }

  const filmCountByRegista = {};
  allFilms.forEach(film => {
    if (film.registaId || film.registaId === 0) {
      const key = String(film.registaId);
      filmCountByRegista[key] = (filmCountByRegista[key] || 0) + 1;
    }
  });

  tableBody.innerHTML = registi.map(regista => {
    const filmCount = filmCountByRegista[String(regista.id)] || 0;
    return `
      <tr class="row-hover">
        <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${regista.id}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-ink">${regista.nome}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-ink">${regista.cognome}</td>
        <td class="px-6 py-4 whitespace-nowrap">
          <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium chip-active">
            ${regista.nazionalita || '-'}
          </span>
        </td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${filmCount}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
          <button onclick="editRegista(${regista.id})" class="text-ferrari-primary hover:text-ferrari-primary-hover mr-3">
            <i class="fa-solid fa-pencil"></i>
          </button>
          <button onclick="deleteRegista(${regista.id}, '${escapeHtml(`${regista.nome} ${regista.cognome}`)}')" class="text-red-600 hover:text-red-900">
            <i class="fa-solid fa-trash"></i>
          </button>
        </td>
      </tr>
    `;
  }).join('');
}

function updateStats(totalCount, visibleItems) {
  const totalRegistiEl = document.getElementById('total-registi');
  if (totalRegistiEl) totalRegistiEl.textContent = String(totalCount);

  const nationalities = new Set(visibleItems.map(r => r.nazionalita).filter(Boolean));
  const totalNationalitiesEl = document.getElementById('total-nationalities');
  if (totalNationalitiesEl) totalNationalitiesEl.textContent = String(nationalities.size);

  const totalFilmsAssociatedEl = document.getElementById('total-films-associated');
  if (totalFilmsAssociatedEl) totalFilmsAssociatedEl.textContent = String(allFilms.length);
}

function bindSearch() {
  const searchInput = document.getElementById('search-input');
  if (!searchInput) return;

  searchInput.addEventListener('input', async (e) => {
    currentSearch = (e.target.value || '').trim();
    currentPage = 1;
    await loadRegisti();
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

  if (totalRegistiCount === 0 || serverItemsCount === 0) {
    paginationInfo.textContent = 'Nessun risultato';
    pageIndicator.textContent = 'Pagina 1 di 1';
    firstBtn.disabled = true;
    prevBtn.disabled = true;
    nextBtn.disabled = true;
    lastBtn.disabled = true;
    return;
  }

  const startItem = ((currentPage - 1) * pageSize) + 1;
  const endItem = Math.min(currentPage * pageSize, totalRegistiCount);

  paginationInfo.textContent = `Mostrando ${startItem}-${endItem} di ${totalRegistiCount} registi`;
  pageIndicator.textContent = `Pagina ${currentPage} di ${totalPages}`;

  firstBtn.disabled = currentPage <= 1;
  prevBtn.disabled = currentPage <= 1;
  nextBtn.disabled = currentPage >= totalPages;
  lastBtn.disabled = currentPage >= totalPages;
}

async function goToPage(page) {
  if (page < 1 || page > totalPages || page === currentPage) return;
  currentPage = page;
  await loadRegisti();
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

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

function setupFormSubmit() {
  const form = document.getElementById('regista-form');
  if (!form) return;

  form.addEventListener('submit', async (e) => {
    e.preventDefault();

    const data = serializeForm('regista-form');
    const editId = form.dataset.editId;

    try {
      if (editId) {
        await API.updateRegista(editId, data);
        showToast('Regista aggiornato con successo');
      } else {
        await API.createRegista(data);
        showToast('Regista creato con successo');
      }

      closeModal('regista-modal');
      await loadRegisti();
    } catch (error) {
      handleApiError(error);
    }
  });
}

async function editRegista(id) {
  try {
    const regista = await API.getRegista(id);
    openModal('regista-modal', 'Modifica Regista');

    const form = document.getElementById('regista-form');
    form.dataset.editId = id;

    form.querySelector('[name="nome"]').value = regista.nome || '';
    form.querySelector('[name="cognome"]').value = regista.cognome || '';
    form.querySelector('[name="nazionalita"]').value = regista.nazionalita || '';
  } catch (error) {
    handleApiError(error);
  }
}

async function deleteRegista(id, name) {
  openDeleteModal(name, async () => {
    try {
      await API.deleteRegista(id);
      showToast('Regista eliminato con successo');
      await loadRegisti();
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
