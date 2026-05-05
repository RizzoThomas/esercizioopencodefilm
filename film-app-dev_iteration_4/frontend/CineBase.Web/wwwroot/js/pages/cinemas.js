// Cinemas Page JavaScript
let currentPage = 1;
const pageSize = 10;
let totalPages = 1;
let totalCinemasCount = 0;
let currentSearch = '';

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

document.addEventListener('DOMContentLoaded', async () => {
  bindSearch();
  setupFormSubmit();
  await loadCinemas();
});

async function loadCinemas() {
  const tableBody = document.getElementById('cinemas-table-body');
  if (!tableBody) return;

  try {
    const response = await API.getCinemas({
      page: currentPage,
      pageSize,
      search: currentSearch || undefined
    });

    const paged = normalizePaged(response);
    totalPages = paged.totalPages;
    totalCinemasCount = paged.totalCount;
    currentPage = paged.page;

    renderCinemas(paged.items);
    updateStats(totalCinemasCount);
    renderPagination(paged.items.length);
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="5" class="px-6 py-4 text-center text-ferrari-semantic-warning">Errore nel caricamento dei cinema</td></tr>';
    renderPagination(0);
  }
}

function renderCinemas(cinemas) {
  const tableBody = document.getElementById('cinemas-table-body');
  if (!tableBody) return;

  if (!cinemas.length) {
    tableBody.innerHTML = '<tr><td colspan="5" class="px-6 py-4 text-center text-body">Nessun cinema trovato</td></tr>';
    return;
  }

  tableBody.innerHTML = cinemas.map(cinema => `
    <tr class="row-hover">
      <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${cinema.id}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-ink">${cinema.nome}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${cinema.indirizzo || '-'}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${cinema.citta || '-'}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
        <button onclick="editCinema(${cinema.id})" class="text-ferrari-primary hover:text-ferrari-primary-hover mr-3">
          <i class="fa-solid fa-pencil"></i>
        </button>
        <button onclick="deleteCinema(${cinema.id}, '${escapeHtml(cinema.nome || '')}')" class="text-red-600 hover:text-red-900">
          <i class="fa-solid fa-trash"></i>
        </button>
      </td>
    </tr>
  `).join('');
}

function renderPagination(serverItemsCount) {
  const paginationInfo = document.getElementById('pagination-info');
  const pageIndicator = document.getElementById('page-indicator');
  const firstBtn = document.getElementById('pagination-first');
  const prevBtn = document.getElementById('pagination-prev');
  const nextBtn = document.getElementById('pagination-next');
  const lastBtn = document.getElementById('pagination-last');

  if (!paginationInfo || !pageIndicator || !firstBtn || !prevBtn || !nextBtn || !lastBtn) return;

  if (totalCinemasCount === 0 || serverItemsCount === 0) {
    paginationInfo.textContent = 'Nessun risultato';
    pageIndicator.textContent = 'Pagina 1 di 1';
    firstBtn.disabled = true;
    prevBtn.disabled = true;
    nextBtn.disabled = true;
    lastBtn.disabled = true;
    return;
  }

  const startItem = ((currentPage - 1) * pageSize) + 1;
  const endItem = Math.min(currentPage * pageSize, totalCinemasCount);

  paginationInfo.textContent = `Mostrando ${startItem}-${endItem} di ${totalCinemasCount} cinema`;
  pageIndicator.textContent = `Pagina ${currentPage} di ${totalPages}`;

  firstBtn.disabled = currentPage <= 1;
  prevBtn.disabled = currentPage <= 1;
  nextBtn.disabled = currentPage >= totalPages;
  lastBtn.disabled = currentPage >= totalPages;
}

function updateStats(totalCount) {
  const totalCinemasEl = document.getElementById('total-cinemas');
  if (totalCinemasEl) totalCinemasEl.textContent = String(totalCount);
}

function bindSearch() {
  const searchInput = document.getElementById('search-input');
  if (!searchInput) return;

  searchInput.addEventListener('input', async (e) => {
    currentSearch = (e.target.value || '').trim();
    currentPage = 1;
    await loadCinemas();
  });
}

async function goToPage(page) {
  if (page < 1 || page > totalPages || page === currentPage) return;
  currentPage = page;
  await loadCinemas();
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
  const form = document.getElementById('cinema-form');
  if (!form) return;

  form.addEventListener('submit', async (e) => {
    e.preventDefault();

    const data = serializeForm('cinema-form');
    delete data.telefono;
    const editId = form.dataset.editId;

    try {
      if (editId) {
        await API.updateCinema(editId, data);
        showToast('Cinema aggiornato con successo');
      } else {
        await API.createCinema(data);
        showToast('Cinema creato con successo');
      }

      closeModal('cinema-modal');
      await loadCinemas();
    } catch (error) {
      handleApiError(error);
    }
  });
}

async function editCinema(id) {
  try {
    const cinema = await API.getCinema(id);

    if (!cinema) {
      showToast('Cinema non trovato', 'danger');
      return;
    }

    openModal('cinema-modal', 'Modifica Cinema');

    const form = document.getElementById('cinema-form');
    if (!form) return;

    form.dataset.editId = id;

    const nomeInput = form.querySelector('[name="nome"]');
    const indirizzoInput = form.querySelector('[name="indirizzo"]');
    const cittaInput = form.querySelector('[name="citta"]');

    if (nomeInput) nomeInput.value = cinema.nome || '';
    if (indirizzoInput) indirizzoInput.value = cinema.indirizzo || '';
    if (cittaInput) cittaInput.value = cinema.citta || '';
  } catch (error) {
    handleApiError(error);
  }
}

async function deleteCinema(id, name) {
  openDeleteModal(name, async () => {
    try {
      await API.deleteCinema(id);
      showToast('Cinema eliminato con successo');
      await loadCinemas();
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
